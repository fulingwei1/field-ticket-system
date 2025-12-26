namespace FieldTicket.Core.Services;

/// <summary>
/// 工单状态历史服务接口
/// </summary>
public interface ITicketStatusHistoryService
{
    /// <summary>
    /// 记录状态变更
    /// </summary>
    Task RecordStatusChangeAsync(
        Guid ticketId,
        string fromStatus,
        string toStatus,
        Guid changedBy,
        string? changedByName = null,
        string? changeReason = null,
        string changeType = "manual",
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? notes = null);

    /// <summary>
    /// 获取工单的状态历史
    /// </summary>
    Task<List<TicketStatusHistoryDto>> GetStatusHistoryAsync(Guid ticketId);

    /// <summary>
    /// 获取状态流转路径（可视化用）
    /// </summary>
    Task<TicketStatusFlowDto> GetStatusFlowAsync(Guid ticketId);
}

/// <summary>
/// 工单状态历史DTO
/// </summary>
public class TicketStatusHistoryDto
{
    public Guid HistoryId { get; set; }
    public Guid TicketId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public Guid ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangeType { get; set; } = "manual";
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// 工单状态流转DTO（可视化用）
/// </summary>
public class TicketStatusFlowDto
{
    public Guid TicketId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public List<StatusFlowNodeDto> Nodes { get; set; } = new();
    public List<StatusFlowEdgeDto> Edges { get; set; } = new();
    public TimeSpan? TotalDuration { get; set; }
}

/// <summary>
/// 状态流转节点
/// </summary>
public class StatusFlowNodeDto
{
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public DateTime? EnteredAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// 状态流转边（连接）
/// </summary>
public class StatusFlowEdgeDto
{
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangeReason { get; set; }
    public Guid ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public string ChangeType { get; set; } = "manual";
}











