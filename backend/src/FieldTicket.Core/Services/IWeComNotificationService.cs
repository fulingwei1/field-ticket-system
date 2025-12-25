namespace FieldTicket.Core.Services;

/// <summary>
/// 企业微信通知服务接口
/// </summary>
public interface IWeComNotificationService
{
    /// <summary>
    /// 发送通知
    /// </summary>
    /// <param name="toUserIds">接收人企业微信用户ID列表</param>
    /// <param name="content">通知内容</param>
    /// <param name="retryCount">重试次数（默认3次）</param>
    Task SendNotificationAsync(string[] toUserIds, string content, int retryCount = 3);
    
    /// <summary>
    /// 发送工单提交通知
    /// </summary>
    Task NotifyTicketSubmittedAsync(Guid ticketId);
    
    /// <summary>
    /// 发送解决方案发布通知
    /// </summary>
    Task NotifySolutionPublishedAsync(Guid solutionId);
}


