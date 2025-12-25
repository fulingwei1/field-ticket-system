using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.WeCom;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 通知规则服务实现
/// </summary>
public class NotificationRuleService : INotificationRuleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWeComNotificationService _notificationService;
    private readonly WeComUserService _weComUserService;
    private readonly IWeComContactService? _contactService;
    private readonly ILogger<NotificationRuleService> _logger;

    public NotificationRuleService(
        ApplicationDbContext dbContext,
        IWeComNotificationService notificationService,
        WeComUserService weComUserService,
        ILogger<NotificationRuleService> logger,
        IWeComContactService? contactService = null)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _weComUserService = weComUserService;
        _logger = logger;
        _contactService = contactService;
    }

    public async Task<List<NotificationRuleDto>> GetNotificationRulesAsync(
        Guid? customerId,
        Guid? projectId,
        Guid? deviceId,
        string? triggerEvent = null)
    {
        var query = _dbContext.NotificationRules
            .Where(r => r.IsActive);

        // 按层级和ID筛选
        if (deviceId.HasValue)
        {
            query = query.Where(r => r.RuleLevel == "device" && r.DeviceId == deviceId.Value);
        }
        else if (projectId.HasValue)
        {
            query = query.Where(r => (r.RuleLevel == "project" && r.ProjectId == projectId.Value) ||
                                     (r.RuleLevel == "customer" && r.CustomerId == customerId));
        }
        else if (customerId.HasValue)
        {
            query = query.Where(r => r.RuleLevel == "customer" && r.CustomerId == customerId.Value);
        }

        // 按触发事件筛选
        if (!string.IsNullOrEmpty(triggerEvent))
        {
            query = query.Where(r => r.TriggerEvent == triggerEvent);
        }

        var rules = await query
            .OrderByDescending(r => r.RuleLevel == "device" ? 3 : r.RuleLevel == "project" ? 2 : 1) // 优先级排序
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rules.Select(r => MapToDto(r)).ToList();
    }

    public async Task<NotificationRuleDto> SaveNotificationRuleAsync(SaveNotificationRuleRequest request, Guid userId)
    {
        NotificationRule rule;

        if (request.RuleId.HasValue)
        {
            // 更新
            rule = await _dbContext.NotificationRules
                .FirstOrDefaultAsync(r => r.RuleId == request.RuleId.Value);

            if (rule == null)
            {
                throw new KeyNotFoundException($"通知规则 {request.RuleId} 不存在");
            }

            rule.RuleLevel = request.RuleLevel;
            rule.CustomerId = request.CustomerId;
            rule.ProjectId = request.ProjectId;
            rule.DeviceId = request.DeviceId;
            rule.TriggerEvent = request.TriggerEvent;
            rule.RecipientsConfig = request.RecipientsConfig;
            rule.TemplateOverride = request.TemplateOverride;
            rule.IsActive = request.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // 创建
            rule = new NotificationRule
            {
                RuleId = Guid.NewGuid(),
                RuleLevel = request.RuleLevel,
                CustomerId = request.CustomerId,
                ProjectId = request.ProjectId,
                DeviceId = request.DeviceId,
                TriggerEvent = request.TriggerEvent,
                RecipientsConfig = request.RecipientsConfig,
                TemplateOverride = request.TemplateOverride,
                IsActive = request.IsActive,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.NotificationRules.Add(rule);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("保存通知规则 {RuleId}，层级：{Level}，事件：{Event}", 
            rule.RuleId, rule.RuleLevel, rule.TriggerEvent);

        return MapToDto(rule);
    }

    public async Task DeleteNotificationRuleAsync(Guid ruleId)
    {
        var rule = await _dbContext.NotificationRules
            .FirstOrDefaultAsync(r => r.RuleId == ruleId);

        if (rule == null)
        {
            throw new KeyNotFoundException($"通知规则 {ruleId} 不存在");
        }

        _dbContext.NotificationRules.Remove(rule);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("删除通知规则 {RuleId}", ruleId);
    }

    public async Task<NotificationResult> ExecuteNotificationAsync(
        Guid ticketId,
        string triggerEvent,
        Dictionary<string, object>? context = null)
    {
        try
        {
            // 获取工单信息
            var ticket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"工单 {ticketId} 不存在");
            }

            // 获取通知规则（按优先级：设备 > 项目 > 客户）
            var rules = await GetNotificationRulesAsync(
                ticket.CustomerId,
                ticket.ProjectId,
                ticket.DeviceId,
                triggerEvent);

            if (rules.Count == 0)
            {
                _logger.LogInformation("工单 {TicketId} 事件 {Event} 没有匹配的通知规则", ticketId, triggerEvent);
                return new NotificationResult
                {
                    Success = true,
                    SentCount = 0,
                    FailedCount = 0
                };
            }

            // 取优先级最高的规则（已按优先级排序）
            var rule = rules.First();
            var ruleEntity = await _dbContext.NotificationRules
                .FirstOrDefaultAsync(r => r.RuleId == rule.RuleId);

            if (ruleEntity == null)
            {
                throw new KeyNotFoundException($"通知规则 {rule.RuleId} 不存在");
            }

            // 解析接收人配置
            var recipients = await ResolveRecipientsAsync(ruleEntity.RecipientsConfig);

            if (recipients.Count == 0)
            {
                _logger.LogWarning("通知规则 {RuleId} 没有解析到接收人", rule.RuleId);
                return new NotificationResult
                {
                    Success = true,
                    SentCount = 0,
                    FailedCount = 0
                };
            }

            // 生成通知内容
            string content;
            Solution? solution = null;
            if (context != null && context.TryGetValue("solution", out var solutionObj))
            {
                if (solutionObj is Solution sol)
                {
                    solution = sol;
                }
                else if (solutionObj is Guid solutionId)
                {
                    solution = await _dbContext.Solutions.FirstOrDefaultAsync(s => s.SolutionId == solutionId);
                }
            }

            if (solution != null)
            {
                content = await GenerateNotificationContentAsync(ruleEntity, ticket, solution, context);
            }
            else
            {
                content = GenerateNotificationContent(ruleEntity, ticket, context);
            }

            // 发送通知
            var sentCount = 0;
            var failedCount = 0;
            var errors = new List<string>();

            try
            {
                await _notificationService.SendNotificationAsync(recipients.ToArray(), content);
                sentCount = recipients.Count;
            }
            catch (Exception ex)
            {
                failedCount = recipients.Count;
                errors.Add(ex.Message);
                _logger.LogError(ex, "发送通知失败，工单 {TicketId}，规则 {RuleId}", ticketId, rule.RuleId);
            }

            // 记录通知日志
            var log = new NotificationLog
            {
                LogId = Guid.NewGuid(),
                TicketId = ticketId,
                TriggerEvent = triggerEvent,
                RuleId = rule.RuleId,
                Recipients = JsonDocument.Parse(JsonSerializer.Serialize(recipients)),
                SentCount = sentCount,
                FailedCount = failedCount,
                MessageContent = content,
                SentAt = DateTime.UtcNow
            };

            _dbContext.NotificationLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            return new NotificationResult
            {
                Success = failedCount == 0,
                SentCount = sentCount,
                FailedCount = failedCount,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行通知失败，工单 {TicketId}，事件 {Event}", ticketId, triggerEvent);
            return new NotificationResult
            {
                Success = false,
                SentCount = 0,
                FailedCount = 0,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// 解析接收人配置
    /// </summary>
    private async Task<List<string>> ResolveRecipientsAsync(JsonDocument recipientsConfig)
    {
        var recipients = new HashSet<string>();
        var root = recipientsConfig.RootElement;

        // 直接指定的用户ID
        if (root.TryGetProperty("userIds", out var userIdsElement))
        {
            foreach (var userId in userIdsElement.EnumerateArray())
            {
                recipients.Add(userId.GetString() ?? string.Empty);
            }
        }

        // 直接指定的群组ID
        if (root.TryGetProperty("chatIds", out var chatIdsElement))
        {
            foreach (var chatId in chatIdsElement.EnumerateArray())
            {
                recipients.Add($"@{chatId.GetString()}");
            }
        }

        // 按角色筛选（TODO: 需要实现企业微信通讯录服务）
        if (root.TryGetProperty("roles", out var rolesElement))
        {
            // 暂时跳过角色解析，后续实现
            _logger.LogWarning("角色解析功能待实现");
        }

        // 按部门筛选（TODO: 需要实现企业微信通讯录服务）
        if (root.TryGetProperty("departments", out var departmentsElement))
        {
            // 暂时跳过部门解析，后续实现
            _logger.LogWarning("部门解析功能待实现");
        }

        return recipients.Where(r => !string.IsNullOrEmpty(r)).ToList();
    }

    /// <summary>
    /// 生成通知内容
    /// </summary>
    private string GenerateNotificationContent(NotificationRule rule, Ticket ticket, Dictionary<string, object>? context)
    {
        // 如果有自定义模板，使用自定义模板
        if (!string.IsNullOrEmpty(rule.TemplateOverride))
        {
            return ReplaceTemplateVariables(rule.TemplateOverride, ticket, context);
        }

        // 使用默认模板
        var template = GetDefaultTemplate(rule.TriggerEvent);
        return ReplaceTemplateVariables(template, ticket, context);
    }

    /// <summary>
    /// 生成通知内容（支持解决方案）
    /// </summary>
    private async Task<string> GenerateNotificationContentAsync(NotificationRule rule, Ticket ticket, Solution? solution, Dictionary<string, object>? context)
    {
        // 如果有自定义模板，使用自定义模板
        if (!string.IsNullOrEmpty(rule.TemplateOverride))
        {
            return await ReplaceTemplateVariablesAsync(rule.TemplateOverride, ticket, solution, context);
        }

        // 使用默认模板
        var template = GetDefaultTemplate(rule.TriggerEvent);
        return await ReplaceTemplateVariablesAsync(template, ticket, solution, context);
    }

    /// <summary>
    /// 获取默认模板
    /// </summary>
    private string GetDefaultTemplate(string triggerEvent)
    {
        return triggerEvent switch
        {
            "ticket_submitted" => NotificationTemplates.TicketSubmitted,
            "solution_published" => NotificationTemplates.SolutionPublished,
            _ => $"【{triggerEvent}】工单 {triggerEvent} 事件触发"
        };
    }

    /// <summary>
    /// 替换模板变量
    /// </summary>
    private string ReplaceTemplateVariables(string template, Ticket ticket, Dictionary<string, object>? context)
    {
        var variables = new Dictionary<string, string>
        {
            { "ticket_no", ticket.TicketNo ?? "草稿" },
            { "symptom_title", ticket.SymptomTitle },
            { "priority", ticket.Priority },
            { "web_url", $"http://localhost:3000/tickets/{ticket.TicketId}" }
        };

        // 从 context 中获取额外变量
        if (context != null)
        {
            foreach (var kvp in context)
            {
                variables[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }
        }

        return NotificationTemplates.ReplaceVariables(template, variables);
    }

    /// <summary>
    /// 替换模板变量（支持解决方案）
    /// </summary>
    private async Task<string> ReplaceTemplateVariablesAsync(string template, Ticket ticket, Solution? solution, Dictionary<string, object>? context)
    {
        var variables = new Dictionary<string, string>
        {
            { "ticket_no", ticket.TicketNo ?? "草稿" },
            { "symptom_title", ticket.SymptomTitle },
            { "priority", ticket.Priority },
            { "web_url", $"http://localhost:3000/tickets/{ticket.TicketId}" }
        };

        // 添加解决方案相关变量
        if (solution != null)
        {
            variables["solution_code"] = solution.SolutionCode ?? "未发布";
            variables["summary"] = solution.Title;
            variables["app_deeplink"] = $"fieldticket://solution/{solution.SolutionId}";
        }

        // 从 context 中获取额外变量
        if (context != null)
        {
            foreach (var kvp in context)
            {
                if (kvp.Value != null && kvp.Key != "solution") // 排除 solution 对象
                {
                    variables[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
                }
            }
        }

        return NotificationTemplates.ReplaceVariables(template, variables);
    }

    private NotificationRuleDto MapToDto(NotificationRule rule)
    {
        return new NotificationRuleDto
        {
            RuleId = rule.RuleId,
            RuleLevel = rule.RuleLevel,
            CustomerId = rule.CustomerId,
            ProjectId = rule.ProjectId,
            DeviceId = rule.DeviceId,
            TriggerEvent = rule.TriggerEvent,
            RecipientsConfig = rule.RecipientsConfig,
            TemplateOverride = rule.TemplateOverride,
            IsActive = rule.IsActive,
            CreatedBy = rule.CreatedBy,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}

