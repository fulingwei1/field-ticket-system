namespace FieldTicket.Core.Services;

/// <summary>
/// 工单批量操作服务接口
/// </summary>
public interface ITicketBatchService
{
    /// <summary>
    /// 批量更新工单状态
    /// </summary>
    Task<BatchOperationResult> BatchUpdateStatusAsync(
        List<Guid> ticketIds,
        string newStatus,
        string? reason,
        Guid userId);

    /// <summary>
    /// 批量分配工程师
    /// </summary>
    Task<BatchOperationResult> BatchAssignEngineerAsync(
        List<Guid> ticketIds,
        Guid engineerId,
        Guid userId);

    /// <summary>
    /// 批量删除工单（软删除）
    /// </summary>
    Task<BatchOperationResult> BatchDeleteAsync(
        List<Guid> ticketIds,
        Guid userId);

    /// <summary>
    /// 批量更新优先级
    /// </summary>
    Task<BatchOperationResult> BatchUpdatePriorityAsync(
        List<Guid> ticketIds,
        string priority,
        Guid userId);

    /// <summary>
    /// 批量标记
    /// </summary>
    Task<BatchOperationResult> BatchTagAsync(
        List<Guid> ticketIds,
        List<string> tags,
        Guid userId);
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BatchOperationResult
{
    public bool Success { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationError> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 批量操作错误
/// </summary>
public class BatchOperationError
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}











