namespace FieldTicket.Shared.Models;

/// <summary>
/// 附件DTO
/// </summary>
public class AttachmentDto
{
    public Guid AttachmentId { get; set; }
    public Guid TicketId { get; set; }
    public Guid UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
    public string FileType { get; set; } = string.Empty; // photo/video/log/file
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string UploadStatus { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DownloadUrl { get; set; } // 预签名URL（临时）
}

