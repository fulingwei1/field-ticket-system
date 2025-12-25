using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Storage;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 附件服务实现
/// </summary>
public class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMinIOService _minioService;
    private readonly ILogger<AttachmentService> _logger;

    // 文件大小限制（字节）
    private const long MaxPhotoSize = 10 * 1024 * 1024; // 10MB
    private const long MaxVideoSize = 200 * 1024 * 1024; // 200MB
    private const long MaxLogSize = 20 * 1024 * 1024; // 20MB
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public AttachmentService(
        ApplicationDbContext dbContext,
        IMinIOService minioService,
        ILogger<AttachmentService> logger)
    {
        _dbContext = dbContext;
        _minioService = minioService;
        _logger = logger;
    }

    public async Task<AttachmentDto> UploadAsync(
        Guid ticketId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string fileType,
        Guid userId)
    {
        // 验证文件大小
        ValidateFileSize(fileType, fileSize);

        // 验证文件类型
        ValidateFileType(fileType, contentType);

        // 计算文件SHA256（用于去重和校验）
        var sha256 = await ComputeSha256Async(fileStream);
        fileStream.Position = 0; // 重置流位置

        // 生成对象存储Key
        var objectKey = GenerateObjectKey(ticketId, fileName);

        // 上传到MinIO
        await _minioService.UploadFileAsync(objectKey, fileStream, contentType, fileSize);

        // 保存附件记录
        var attachment = new Attachment
        {
            AttachmentId = Guid.NewGuid(),
            TicketId = ticketId,
            UploadedBy = userId,
            FileType = fileType,
            FileName = fileName,
            FileKey = objectKey,
            FileSize = fileSize,
            MimeType = contentType,
            Sha256 = sha256,
            UploadStatus = "completed",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Attachments.Add(attachment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("附件上传成功：{AttachmentId}，工单：{TicketId}，文件：{FileName}",
            attachment.AttachmentId, ticketId, fileName);

        return await MapToDtoAsync(attachment);
    }

    public async Task<string> GetDownloadUrlAsync(Guid attachmentId, int expirySeconds = 900)
    {
        var attachment = await _dbContext.Attachments
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

        if (attachment == null)
        {
            throw new KeyNotFoundException($"附件 {attachmentId} 不存在");
        }

        return await _minioService.GetPresignedDownloadUrlAsync(attachment.FileKey, expirySeconds);
    }

    public async Task DeleteAsync(Guid attachmentId, Guid userId)
    {
        var attachment = await _dbContext.Attachments
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

        if (attachment == null)
        {
            throw new KeyNotFoundException($"附件 {attachmentId} 不存在");
        }

        // 权限检查：只能删除自己上传的附件或工单创建者
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == attachment.TicketId);

        if (ticket == null || (attachment.UploadedBy != userId && ticket.CreatedByUserId != userId))
        {
            throw new UnauthorizedAccessException("无权删除此附件");
        }

        // 从MinIO删除文件
        await _minioService.DeleteFileAsync(attachment.FileKey);

        // 从数据库删除记录
        _dbContext.Attachments.Remove(attachment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("附件删除成功：{AttachmentId}", attachmentId);
    }

    public async Task<List<AttachmentDto>> GetTicketAttachmentsAsync(Guid ticketId)
    {
        var attachments = await _dbContext.Attachments
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var dtos = new List<AttachmentDto>();
        foreach (var attachment in attachments)
        {
            var dto = await MapToDtoAsync(attachment);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<AttachmentDto?> GetAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _dbContext.Attachments
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

        if (attachment == null)
        {
            return null;
        }

        return await MapToDtoAsync(attachment);
    }

    private void ValidateFileSize(string fileType, long fileSize)
    {
        var maxSize = fileType switch
        {
            "photo" => MaxPhotoSize,
            "video" => MaxVideoSize,
            "log" => MaxLogSize,
            _ => MaxFileSize
        };

        if (fileSize > maxSize)
        {
            throw new ArgumentException($"文件大小超过限制：{fileType} 类型文件最大 {maxSize / 1024 / 1024}MB");
        }
    }

    private void ValidateFileType(string fileType, string contentType)
    {
        var allowedTypes = fileType switch
        {
            "photo" => new[] { "image/jpeg", "image/png", "image/gif", "image/webp" },
            "video" => new[] { "video/mp4", "video/quicktime", "video/x-msvideo" },
            "log" => new[] { "text/plain", "text/log", "application/json", "text/csv" },
            _ => Array.Empty<string>()
        };

        if (allowedTypes.Length > 0 && !allowedTypes.Contains(contentType.ToLower()))
        {
            throw new ArgumentException($"不支持的文件类型：{contentType}，{fileType} 类型仅支持：{string.Join(", ", allowedTypes)}");
        }
    }

    private async Task<string> ComputeSha256Async(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private string GenerateObjectKey(Guid ticketId, string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sanitizedFileName = SanitizeFileName(fileName);
        return $"attachments/{ticketId}/{timestamp}_{sanitizedFileName}";
    }

    private string SanitizeFileName(string fileName)
    {
        // 移除路径分隔符和特殊字符
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 200 ? sanitized.Substring(0, 200) : sanitized;
    }

    private async Task<AttachmentDto> MapToDtoAsync(Attachment attachment)
    {
        var uploadedByUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == attachment.UploadedBy);

        return new AttachmentDto
        {
            AttachmentId = attachment.AttachmentId,
            TicketId = attachment.TicketId,
            UploadedBy = attachment.UploadedBy,
            UploadedByName = uploadedByUser?.Name,
            FileType = attachment.FileType,
            FileName = attachment.FileName,
            FileSize = attachment.FileSize,
            MimeType = attachment.MimeType,
            UploadStatus = attachment.UploadStatus,
            Description = attachment.Description,
            CreatedAt = attachment.CreatedAt
        };
    }
}

