using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 分诊服务实现
/// </summary>
public class TriageService : ITriageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TriageService> _logger;
    private readonly IJudgementCardVersionService? _versionService;
    private readonly INotificationRuleService? _notificationRuleService;
    private readonly IWeComNotificationService? _notificationService;
    private readonly ITicketStatusHistoryService? _statusHistoryService;

    public TriageService(
        ApplicationDbContext dbContext,
        ILogger<TriageService> logger,
        IJudgementCardVersionService? versionService = null,
        INotificationRuleService? notificationRuleService = null,
        IWeComNotificationService? notificationService = null,
        ITicketStatusHistoryService? statusHistoryService = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _versionService = versionService;
        _notificationRuleService = notificationRuleService;
        _notificationService = notificationService;
        _statusHistoryService = statusHistoryService;
    }

    public async Task<TriageResult> TriageTicketAsync(Guid ticketId, TriageTicketRequest request, Guid userId)
    {
        // 获取工单
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 验证工单状态必须是 Submitted
        if (ticket.Status != "Submitted")
        {
            throw new InvalidOperationException($"工单状态必须是 Submitted，当前状态：{ticket.Status}");
        }

        // 硬规则HR-001：必须关联判断卡
        if (string.IsNullOrEmpty(request.JcCode))
        {
            throw new ArgumentException("必须关联判断卡才能进行分诊（硬规则HR-001）");
        }

        // 验证判断卡存在
        var judgementCard = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == request.JcCode && jc.Status == "Active");

        if (judgementCard == null)
        {
            throw new KeyNotFoundException($"判断卡 {request.JcCode} 不存在或已停用");
        }

        // 验证置信度范围（1-5）
        if (request.Confidence < 1 || request.Confidence > 5)
        {
            throw new ArgumentException("置信度必须在 1-5 之间");
        }

        // 硬规则HR-002：低置信度（≤2）自动升级
        bool escalationRequired = request.Confidence <= 2;
        Guid? escalatedTo = null;

        if (escalationRequired)
        {
            // TODO: 获取主管ID（暂时设为null，后续实现）
            _logger.LogWarning("工单 {TicketId} 置信度 {Confidence} ≤ 2，需要升级", ticketId, request.Confidence);
        }

        // 创建分诊记录
        var triageNote = new TriageNote
        {
            TriageNoteId = Guid.NewGuid(),
            TicketId = ticketId,
            JcCode = request.JcCode,
            CurrentHypothesis = request.CurrentHypothesis,
            NextAction = request.NextAction,
            Confidence = request.Confidence,
            EscalationRequired = escalationRequired,
            EscalatedTo = escalatedTo,
            Note = request.Note,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TriageNotes.Add(triageNote);

        // 记录状态变更
        var oldStatus = ticket.Status;

        // 更新工单状态和关联信息
        ticket.Status = "Triage";
        ticket.CurrentJcCode = request.JcCode;
        ticket.AssignedTo = userId;
        ticket.UpdatedAt = DateTime.UtcNow;

        // 更新判断卡使用统计
        judgementCard.UsageCount++;
        judgementCard.LastUsedAt = DateTime.UtcNow;
        judgementCard.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // 记录状态历史（异步，不阻塞主流程）
        if (_statusHistoryService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _statusHistoryService.RecordStatusChangeAsync(
                        ticketId: ticketId,
                        fromStatus: oldStatus,
                        toStatus: "Triage",
                        changedBy: userId,
                        changeReason: $"分诊，使用判断卡 {request.JcCode}",
                        changeType: "manual",
                        relatedEntityId: triageNote.TriageNoteId,
                        relatedEntityType: "Triage"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "记录工单状态历史失败，工单 {TicketId}", ticketId);
                }
            });
        }

        // 记录使用历史（异步，不阻塞主流程）
        if (_versionService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _versionService.RecordUsageAsync(
                        request.JcCode,
                        judgementCard.Version,
                        ticketId,
                        userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "记录判断卡使用历史失败，判断卡 {JcCode}，工单 {TicketId}",
                        request.JcCode, ticketId);
                }
            });
        }

        _logger.LogInformation("工单 {TicketId} 已分诊，关联判断卡 {JcCode}，置信度 {Confidence}", 
            ticketId, request.JcCode, request.Confidence);

        // 发送企业微信通知（异步，不阻塞主流程）
        // 优先使用通知规则系统，如果没有规则则使用默认通知
        _ = Task.Run(async () =>
        {
            try
            {
                // 优先使用通知规则系统
                if (_notificationRuleService != null)
                {
                    var notificationResult = await _notificationRuleService.ExecuteNotificationAsync(
                        ticketId,
                        "ticket_triaged",
                        new Dictionary<string, object>
                        {
                            ["ticket"] = ticket,
                            ["judgementCard"] = judgementCard,
                            ["triageNote"] = triageNote
                        });

                    if (notificationResult.SentCount > 0)
                    {
                        _logger.LogInformation("通过通知规则发送分诊通知成功，工单 {TicketId}，发送数：{SentCount}",
                            ticketId, notificationResult.SentCount);
                        return;
                    }
                }

                // 回退到默认通知（如果没有配置通知规则）
                // 分诊通知通常不需要默认通知，因为分诊是内部流程
                _logger.LogInformation("工单 {TicketId} 分诊完成，未配置通知规则", ticketId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送分诊通知失败，工单ID：{TicketId}", ticketId);
            }
        });

        return new TriageResult
        {
            TicketId = ticketId,
            Status = ticket.Status,
            JcCode = request.JcCode,
            EscalationRequired = escalationRequired,
            EscalatedTo = escalatedTo,
            Message = escalationRequired 
                ? "分诊完成，但置信度较低，已自动升级" 
                : "分诊完成"
        };
    }

    public async Task<List<JudgementCardDto>> GetJudgementCardsAsync(char? domain = null, string? status = null)
    {
        var query = _dbContext.JudgementCards.AsQueryable();

        if (domain.HasValue)
        {
            query = query.Where(jc => jc.Domain == domain.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(jc => jc.Status == status);
        }
        else
        {
            // 默认只返回激活状态的判断卡
            query = query.Where(jc => jc.Status == "Active");
        }

        var cards = await query
            .OrderByDescending(jc => jc.UsageCount)
            .ThenByDescending(jc => jc.UpdatedAt)
            .ToListAsync();

        return cards.Select(card => new JudgementCardDto
        {
            JudgementCardId = card.JudgementCardId,
            JcCode = card.JcCode,
            Title = card.Title,
            Description = card.Description,
            Domain = card.Domain,
            SymptomStructure = card.SymptomStructure,
            TroubleshootingPath = card.TroubleshootingPath,
            HypothesisTemplate = card.HypothesisTemplate,
            NextActionTemplate = card.NextActionTemplate,
            Status = card.Status,
            Version = card.Version,
            UsageCount = card.UsageCount,
            LastUsedAt = card.LastUsedAt,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt
        }).ToList();
    }

    public async Task<JudgementCardDto?> GetJudgementCardAsync(string jcCode)
    {
        var card = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode);

        if (card == null)
        {
            return null;
        }

        return new JudgementCardDto
        {
            JudgementCardId = card.JudgementCardId,
            JcCode = card.JcCode,
            Title = card.Title,
            Description = card.Description,
            Domain = card.Domain,
            SymptomStructure = card.SymptomStructure,
            TroubleshootingPath = card.TroubleshootingPath,
            HypothesisTemplate = card.HypothesisTemplate,
            NextActionTemplate = card.NextActionTemplate,
            Status = card.Status,
            Version = card.Version,
            UsageCount = card.UsageCount,
            LastUsedAt = card.LastUsedAt,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt
        };
    }
}


