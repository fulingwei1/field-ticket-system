namespace FieldTicket.Infrastructure.Storage;

/// <summary>
/// MinIO 服务接口
/// </summary>
public interface IMinIOService
{
    /// <summary>
    /// 初始化存储桶（如果不存在则创建）
    /// </summary>
    Task EnsureBucketExistsAsync();

    /// <summary>
    /// 上传文件
    /// </summary>
    Task<string> UploadFileAsync(string objectKey, Stream fileStream, string contentType, long fileSize);

    /// <summary>
    /// 获取预签名下载URL（有效期15分钟）
    /// </summary>
    Task<string> GetPresignedDownloadUrlAsync(string objectKey, int expirySeconds = 900);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task DeleteFileAsync(string objectKey);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    Task<bool> FileExistsAsync(string objectKey);
}

