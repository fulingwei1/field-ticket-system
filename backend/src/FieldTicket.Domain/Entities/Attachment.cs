namespace FieldTicket.Domain.Entities;

/// <summary>
/// 附件实体
/// </summary>
public class Attachment
{
    public Guid AttachmentId { get; set; }
    public Guid TicketId { get; set; }
    public Guid UploadedBy { get; set; }

    // 文件信息
    public string FileType { get; set; } = string.Empty; // photo/video/log/file
    public string FileName { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty; // 对象存储 Key
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? Sha256 { get; set; }

    // 上传状态
    public string UploadStatus { get; set; } = "completed"; // pending/uploading/completed/failed
    public string? UploadId { get; set; } // 分片上传ID
    public int UploadedChunks { get; set; }
    public int? TotalChunks { get; set; }

    // 标注
    public string? Tags { get; set; } // JSON 字符串
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}


