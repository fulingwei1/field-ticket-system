using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 附件服务接口
/// </summary>
public interface IAttachmentService
{
    /// <summary>
    /// 上传附件
    /// </summary>
    Task<AttachmentDto> UploadAsync(Guid ticketId, Stream fileStream, string fileName, string contentType, long fileSize, string fileType, Guid userId);

    /// <summary>
    /// 获取附件下载URL
    /// </summary>
    Task<string> GetDownloadUrlAsync(Guid attachmentId, int expirySeconds = 900);

    /// <summary>
    /// 删除附件
    /// </summary>
    Task DeleteAsync(Guid attachmentId, Guid userId);

    /// <summary>
    /// 获取工单的附件列表
    /// </summary>
    Task<List<AttachmentDto>> GetTicketAttachmentsAsync(Guid ticketId);

    /// <summary>
    /// 获取附件详情
    /// </summary>
    Task<AttachmentDto?> GetAttachmentAsync(Guid attachmentId);
}

