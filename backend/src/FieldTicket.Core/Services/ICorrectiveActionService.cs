namespace FieldTicket.Core.Services;

/// <summary>
/// 整改任务服务接口
/// </summary>
public interface ICorrectiveActionService
{
    /// <summary>
    /// 检查是否需要生成整改任务
    /// </summary>
    Task<List<CorrectiveActionTriggerDto>> CheckTriggersAsync(Guid ticketId);

    /// <summary>
    /// 创建整改任务
    /// </summary>
    Task<Guid> CreateActionAsync(CreateCorrectiveActionRequest request, Guid userId);

    /// <summary>
    /// 获取整改任务列表
    /// </summary>
    Task<(List<CorrectiveActionDto> Items, int Total)> GetActionsAsync(
        CorrectiveActionQueryFilter filter,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 获取整改任务详情
    /// </summary>
    Task<CorrectiveActionDto?> GetActionAsync(Guid actionId);

    /// <summary>
    /// 更新整改任务状态
    /// </summary>
    Task UpdateActionStatusAsync(Guid actionId, string status, string? notes, Guid userId);

    /// <summary>
    /// 更新整改任务
    /// </summary>
    Task UpdateActionAsync(Guid actionId, UpdateCorrectiveActionRequest request, Guid userId);

    /// <summary>
    /// 效果评估
    /// </summary>
    Task EvaluateEffectivenessAsync(Guid actionId, EffectivenessCheckRequest request, Guid userId);

    /// <summary>
    /// 删除整改任务
    /// </summary>
    Task DeleteActionAsync(Guid actionId);
}

/// <summary>
/// 整改任务触发DTO
/// </summary>
public class CorrectiveActionTriggerDto
{
    public bool ShouldTrigger { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<Guid> RelatedTicketIds { get; set; } = new();
    public Dictionary<string, object> TriggerData { get; set; } = new();
}

/// <summary>
/// 创建整改任务请求
/// </summary>
public class CreateCorrectiveActionRequest
{
    public string TriggerType { get; set; } = "manual";
    public Dictionary<string, object>? TriggerRule { get; set; }
    public List<Guid> RelatedTicketIds { get; set; } = new();
    public string ProblemDescription { get; set; } = string.Empty;
    public string? RootResponsibility { get; set; }
    public string ActionPlan { get; set; } = string.Empty;
    public Guid? ResponsiblePersonId { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
}

/// <summary>
/// 更新整改任务请求
/// </summary>
public class UpdateCorrectiveActionRequest
{
    public string? ProblemDescription { get; set; }
    public string? RootResponsibility { get; set; }
    public string? ActionPlan { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public string? ExecutionNotes { get; set; }
}

/// <summary>
/// 效果评估请求
/// </summary>
public class EffectivenessCheckRequest
{
    public DateTime CheckDate { get; set; }
    public string CheckResult { get; set; } = string.Empty; // effective, ineffective, partial
    public List<Guid>? RelatedTicketsAfter { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// 整改任务查询筛选
/// </summary>
public class CorrectiveActionQueryFilter
{
    public string? Status { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
    public string? RootResponsibility { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public Guid? RelatedTicketId { get; set; }
}

/// <summary>
/// 整改任务DTO
/// </summary>
public class CorrectiveActionDto
{
    public Guid ActionId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public List<Guid> RelatedTicketIds { get; set; } = new();
    public List<string>? RelatedTicketNos { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string? RootResponsibility { get; set; }
    public string? RootResponsibilityName { get; set; }
    public string ActionPlan { get; set; } = string.Empty;
    public Guid? ResponsiblePersonId { get; set; }
    public string? ResponsiblePersonName { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExecutionNotes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public string? CompletedByName { get; set; }
    public Dictionary<string, object>? EffectivenessCheck { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}









