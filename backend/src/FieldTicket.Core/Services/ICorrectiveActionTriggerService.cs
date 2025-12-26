namespace FieldTicket.Core.Services;

/// <summary>
/// 整改任务触发服务接口
/// </summary>
public interface ICorrectiveActionTriggerService
{
    /// <summary>
    /// 检查触发条件
    /// </summary>
    Task<List<CorrectiveActionTriggerDto>> CheckTriggersAsync(Guid ticketId);
}









