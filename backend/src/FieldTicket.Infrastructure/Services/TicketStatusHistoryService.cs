using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单状态历史服务实现
/// </summary>
public class TicketStatusHistoryService : ITicketStatusHistoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TicketStatusHistoryService> _logger;

    public TicketStatusHistoryService(
        ApplicationDbContext dbContext,
        ILogger<TicketStatusHistoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordStatusChangeAsync(
        Guid ticketId,
        string fromStatus,
        string toStatus,
        Guid changedBy,
        string? changedByName = null,
        string? changeReason = null,
        string changeType = "manual",
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? notes = null)
    {
        // 如果状态没有变化，不记录
        if (fromStatus == toStatus)
        {
            return;
        }

        var history = new TicketStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            TicketId = ticketId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangeReason = changeReason,
            ChangedBy = changedBy,
            ChangedByName = changedByName,
            ChangedAt = DateTime.UtcNow,
            ChangeType = changeType,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            Notes = notes
        };

        _dbContext.TicketStatusHistories.Add(history);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "记录工单 {TicketId} 状态变更：{FromStatus} -> {ToStatus}，操作人：{ChangedBy}",
            ticketId, fromStatus, toStatus, changedBy);
    }

    public async Task<List<TicketStatusHistoryDto>> GetStatusHistoryAsync(Guid ticketId)
    {
        var histories = await _dbContext.TicketStatusHistories
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        return histories.Select(h => new TicketStatusHistoryDto
        {
            HistoryId = h.HistoryId,
            TicketId = h.TicketId,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangeReason = h.ChangeReason,
            ChangedBy = h.ChangedBy,
            ChangedByName = h.ChangedByName,
            ChangedAt = h.ChangedAt,
            ChangeType = h.ChangeType,
            RelatedEntityId = h.RelatedEntityId,
            RelatedEntityType = h.RelatedEntityType,
            Notes = h.Notes
        }).ToList();
    }

    public async Task<TicketStatusFlowDto> GetStatusFlowAsync(Guid ticketId)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        var histories = await GetStatusHistoryAsync(ticketId);

        // 构建节点和边
        var nodes = new List<StatusFlowNodeDto>();
        var edges = new List<StatusFlowEdgeDto>();

        // 状态映射
        var statusMap = new Dictionary<string, string>
        {
            { "Draft", "草稿" },
            { "Submitted", "已提交" },
            { "Triage", "分诊中" },
            { "SolutionIssued", "方案已发布" },
            { "Verifying", "验证中" },
            { "Closed", "已关闭" },
            { "Reopened", "已重开" }
        };

        // 添加初始状态节点（创建时）
        var initialNode = new StatusFlowNodeDto
        {
            Status = "Draft",
            Label = statusMap.GetValueOrDefault("Draft", "Draft"),
            EnteredAt = ticket.CreatedAt,
            IsCurrent = ticket.Status == "Draft"
        };
        nodes.Add(initialNode);

        // 处理状态变更历史
        DateTime? lastExitTime = null;
        foreach (var history in histories)
        {
            // 添加边
            edges.Add(new StatusFlowEdgeDto
            {
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                ChangedAt = history.ChangedAt,
                ChangeReason = history.ChangeReason,
                ChangedBy = history.ChangedBy,
                ChangedByName = history.ChangedByName,
                ChangeType = history.ChangeType
            });

            // 更新上一个节点的退出时间
            var fromNode = nodes.FirstOrDefault(n => n.Status == history.FromStatus && n.ExitedAt == null);
            if (fromNode != null)
            {
                fromNode.ExitedAt = history.ChangedAt;
                if (fromNode.EnteredAt.HasValue)
                {
                    fromNode.Duration = history.ChangedAt - fromNode.EnteredAt.Value;
                }
            }

            // 添加新状态节点
            var toNode = nodes.FirstOrDefault(n => n.Status == history.ToStatus && n.EnteredAt == null);
            if (toNode == null)
            {
                toNode = new StatusFlowNodeDto
                {
                    Status = history.ToStatus,
                    Label = statusMap.GetValueOrDefault(history.ToStatus, history.ToStatus),
                    EnteredAt = history.ChangedAt,
                    ChangedBy = history.ChangedBy,
                    ChangedByName = history.ChangedByName,
                    IsCurrent = ticket.Status == history.ToStatus
                };
                nodes.Add(toNode);
            }
            else
            {
                toNode.EnteredAt = history.ChangedAt;
                toNode.ChangedBy = history.ChangedBy;
                toNode.ChangedByName = history.ChangedByName;
                toNode.IsCurrent = ticket.Status == history.ToStatus;
            }

            lastExitTime = history.ChangedAt;
        }

        // 更新当前节点的退出时间（如果已关闭）
        if (ticket.Status == "Closed" && ticket.ClosedAt.HasValue)
        {
            var closedNode = nodes.FirstOrDefault(n => n.Status == "Closed");
            if (closedNode != null && closedNode.EnteredAt.HasValue)
            {
                closedNode.ExitedAt = ticket.ClosedAt.Value;
                closedNode.Duration = ticket.ClosedAt.Value - closedNode.EnteredAt.Value;
            }
        }

        // 计算总时长
        TimeSpan? totalDuration = null;
        if (ticket.ClosedAt.HasValue)
        {
            totalDuration = ticket.ClosedAt.Value - ticket.CreatedAt;
        }
        else if (lastExitTime.HasValue)
        {
            totalDuration = DateTime.UtcNow - ticket.CreatedAt;
        }

        return new TicketStatusFlowDto
        {
            TicketId = ticketId,
            CurrentStatus = ticket.Status,
            Nodes = nodes.OrderBy(n => n.EnteredAt ?? DateTime.MinValue).ToList(),
            Edges = edges,
            TotalDuration = totalDuration
        };
    }
}












