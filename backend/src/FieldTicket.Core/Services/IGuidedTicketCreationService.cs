using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// AI引导式工单创建服务接口
/// </summary>
public interface IGuidedTicketCreationService
{
    /// <summary>
    /// 创建新的引导式工单创建会话
    /// </summary>
    Task<GuidedTicketCreationSession> CreateSessionAsync(Guid userId);

    /// <summary>
    /// 提交初始信息（文字+图片）
    /// </summary>
    Task<GuidedQuestionResponse> SubmitInitialInfoAsync(
        Guid sessionId,
        string textDescription,
        List<Stream>? images = null);

    /// <summary>
    /// 回答引导性问题
    /// </summary>
    Task<GuidedQuestionResponse> AnswerQuestionAsync(
        Guid sessionId,
        string questionId,
        string answer,
        List<Stream>? additionalImages = null);

    /// <summary>
    /// 生成工单内容（返回AI生成的内容，用于填充工单表单）
    /// </summary>
    Task<GuidedTicketContent> GenerateTicketContentAsync(Guid sessionId);

    /// <summary>
    /// 使用AI生成的内容创建工单草稿（需要提供设备ID）
    /// </summary>
    Task<TicketDto> CreateTicketFromSessionAsync(Guid sessionId, Guid deviceId);

    /// <summary>
    /// 获取会话状态
    /// </summary>
    Task<GuidedTicketCreationSession> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// 清理过期的会话
    /// </summary>
    Task<int> CleanupExpiredSessionsAsync(TimeSpan? expirationTime = null);
}

