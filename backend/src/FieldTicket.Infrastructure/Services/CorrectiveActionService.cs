using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 整改任务服务实现
/// </summary>
public class CorrectiveActionService : ICorrectiveActionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CorrectiveActionService> _logger;
    private readonly ICorrectiveActionTriggerService _triggerService;

    private static readonly Dictionary<string, string> ResponsibilityNames = new()
    {
        { "design", "设计问题" },
        { "software", "软件问题" },
        { "parameter", "参数问题" },
        { "assembly", "装配问题" },
        { "documentation", "文档问题" },
        { "other", "其他" },
        { "unknown", "未知" }
    };

    public CorrectiveActionService(
        ApplicationDbContext dbContext,
        ILogger<CorrectiveActionService> logger,
        ICorrectiveActionTriggerService triggerService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _triggerService = triggerService;
    }

    public async Task<List<CorrectiveActionTriggerDto>> CheckTriggersAsync(Guid ticketId)
    {
        _logger.LogInformation("Checking corrective action triggers for ticket {TicketId}", ticketId);

        var triggers = await _triggerService.CheckTriggersAsync(ticketId);
        return triggers;
    }

    public async Task<Guid> CreateActionAsync(CreateCorrectiveActionRequest request, Guid userId)
    {
        _logger.LogInformation("Creating corrective action, trigger type: {TriggerType}", request.TriggerType);

        // 生成整改任务编号
        var actionCode = await GenerateActionCodeAsync();

        var action = new CorrectiveAction
        {
            ActionId = Guid.NewGuid(),
            ActionCode = actionCode,
            TriggerType = request.TriggerType,
            TriggerRule = request.TriggerRule != null
                ? JsonDocument.Parse(JsonSerializer.Serialize(request.TriggerRule))
                : null,
            RelatedTicketIds = request.RelatedTicketIds,
            ProblemDescription = request.ProblemDescription,
            RootResponsibility = request.RootResponsibility,
            ActionPlan = request.ActionPlan,
            ResponsiblePersonId = request.ResponsiblePersonId,
            TargetCompletionDate = request.TargetCompletionDate,
            Status = "open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.CorrectiveActions.Add(action);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created corrective action {ActionCode}", actionCode);

        return action.ActionId;
    }

    public async Task<(List<CorrectiveActionDto> Items, int Total)> GetActionsAsync(
        CorrectiveActionQueryFilter filter,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbContext.CorrectiveActions.AsQueryable();

        // 应用筛选
        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(a => a.Status == filter.Status);
        }

        if (filter.ResponsiblePersonId.HasValue)
        {
            query = query.Where(a => a.ResponsiblePersonId == filter.ResponsiblePersonId.Value);
        }

        if (!string.IsNullOrEmpty(filter.RootResponsibility))
        {
            query = query.Where(a => a.RootResponsibility == filter.RootResponsibility);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= filter.CreatedTo.Value);
        }

        if (filter.RelatedTicketId.HasValue)
        {
            query = query.Where(a => a.RelatedTicketIds.Contains(filter.RelatedTicketId.Value));
        }

        var total = await query.CountAsync();

        var actions = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<CorrectiveActionDto>();

        foreach (var action in actions)
        {
            var dto = await MapToDtoAsync(action);
            items.Add(dto);
        }

        return (items, total);
    }

    public async Task<CorrectiveActionDto?> GetActionAsync(Guid actionId)
    {
        var action = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId);

        return action != null ? await MapToDtoAsync(action) : null;
    }

    public async Task UpdateActionStatusAsync(Guid actionId, string status, string? notes, Guid userId)
    {
        var action = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId);

        if (action == null)
        {
            throw new KeyNotFoundException($"整改任务 {actionId} 不存在");
        }

        action.Status = status;
        action.ExecutionNotes = notes;
        action.UpdatedAt = DateTime.UtcNow;

        if (status == "completed" || status == "closed")
        {
            action.CompletedAt = DateTime.UtcNow;
            action.CompletedBy = userId;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated corrective action {ActionId} status to {Status}", actionId, status);
    }

    public async Task UpdateActionAsync(Guid actionId, UpdateCorrectiveActionRequest request, Guid userId)
    {
        var action = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId);

        if (action == null)
        {
            throw new KeyNotFoundException($"整改任务 {actionId} 不存在");
        }

        if (request.ProblemDescription != null)
        {
            action.ProblemDescription = request.ProblemDescription;
        }

        if (request.RootResponsibility != null)
        {
            action.RootResponsibility = request.RootResponsibility;
        }

        if (request.ActionPlan != null)
        {
            action.ActionPlan = request.ActionPlan;
        }

        if (request.ResponsiblePersonId.HasValue)
        {
            action.ResponsiblePersonId = request.ResponsiblePersonId.Value;
        }

        if (request.TargetCompletionDate.HasValue)
        {
            action.TargetCompletionDate = request.TargetCompletionDate.Value;
        }

        if (request.ExecutionNotes != null)
        {
            action.ExecutionNotes = request.ExecutionNotes;
        }

        action.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated corrective action {ActionId}", actionId);
    }

    public async Task EvaluateEffectivenessAsync(Guid actionId, EffectivenessCheckRequest request, Guid userId)
    {
        var action = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId);

        if (action == null)
        {
            throw new KeyNotFoundException($"整改任务 {actionId} 不存在");
        }

        var effectivenessCheck = new Dictionary<string, object>
        {
            { "check_date", request.CheckDate },
            { "check_result", request.CheckResult },
            { "related_tickets_after", request.RelatedTicketsAfter ?? new List<Guid>() },
            { "notes", request.Notes ?? string.Empty }
        };

        action.EffectivenessCheck = JsonDocument.Parse(JsonSerializer.Serialize(effectivenessCheck));
        action.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Evaluated effectiveness for corrective action {ActionId}", actionId);
    }

    public async Task DeleteActionAsync(Guid actionId)
    {
        var action = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId);

        if (action == null)
        {
            throw new KeyNotFoundException($"整改任务 {actionId} 不存在");
        }

        _dbContext.CorrectiveActions.Remove(action);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted corrective action {ActionId}", actionId);
    }

    private async Task<string> GenerateActionCodeAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"CAPA-{year}-";

        var lastCode = await _dbContext.CorrectiveActions
            .Where(a => a.ActionCode.StartsWith(prefix))
            .OrderByDescending(a => a.ActionCode)
            .Select(a => a.ActionCode)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (!string.IsNullOrEmpty(lastCode))
        {
            var lastSequenceStr = lastCode.Substring(prefix.Length);
            if (int.TryParse(lastSequenceStr, out var lastSequence))
            {
                sequence = lastSequence + 1;
            }
        }

        return $"{prefix}{sequence:D3}";
    }

    private async Task<CorrectiveActionDto> MapToDtoAsync(CorrectiveAction action)
    {
        var dto = new CorrectiveActionDto
        {
            ActionId = action.ActionId,
            ActionCode = action.ActionCode,
            TriggerType = action.TriggerType,
            RelatedTicketIds = action.RelatedTicketIds,
            ProblemDescription = action.ProblemDescription,
            RootResponsibility = action.RootResponsibility,
            RootResponsibilityName = action.RootResponsibility != null
                ? ResponsibilityNames.GetValueOrDefault(action.RootResponsibility, action.RootResponsibility)
                : null,
            ActionPlan = action.ActionPlan,
            ResponsiblePersonId = action.ResponsiblePersonId,
            TargetCompletionDate = action.TargetCompletionDate,
            Status = action.Status,
            ExecutionNotes = action.ExecutionNotes,
            CompletedAt = action.CompletedAt,
            CompletedBy = action.CompletedBy,
            CreatedAt = action.CreatedAt,
            UpdatedAt = action.UpdatedAt
        };

        // 获取关联工单编号
        if (action.RelatedTicketIds.Any())
        {
            var tickets = await _dbContext.Tickets
                .Where(t => action.RelatedTicketIds.Contains(t.TicketId))
                .Select(t => t.TicketNo)
                .ToListAsync();
            dto.RelatedTicketNos = tickets;
        }

        // 获取负责人名称
        if (action.ResponsiblePersonId.HasValue)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == action.ResponsiblePersonId.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            dto.ResponsiblePersonName = user;
        }

        // 获取完成人名称
        if (action.CompletedBy.HasValue)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == action.CompletedBy.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            dto.CompletedByName = user;
        }

        // 解析效果评估
        if (action.EffectivenessCheck != null)
        {
            dto.EffectivenessCheck = JsonSerializer.Deserialize<Dictionary<string, object>>(
                action.EffectivenessCheck.RootElement.GetRawText());
        }

        return dto;
    }
}



















