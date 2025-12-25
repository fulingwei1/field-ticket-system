using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 通知规则服务接口
/// </summary>
public interface INotificationRuleService
{
    /// <summary>
    /// 获取通知规则（按优先级：设备 > 项目 > 客户）
    /// </summary>
    Task<List<NotificationRuleDto>> GetNotificationRulesAsync(
        Guid? customerId,
        Guid? projectId,
        Guid? deviceId,
        string? triggerEvent = null);

    /// <summary>
    /// 创建/更新通知规则
    /// </summary>
    Task<NotificationRuleDto> SaveNotificationRuleAsync(SaveNotificationRuleRequest request, Guid userId);

    /// <summary>
    /// 删除通知规则
    /// </summary>
    Task DeleteNotificationRuleAsync(Guid ruleId);

    /// <summary>
    /// 执行通知（根据规则发送）
    /// </summary>
    Task<NotificationResult> ExecuteNotificationAsync(
        Guid ticketId,
        string triggerEvent,
        Dictionary<string, object>? context = null);
}

