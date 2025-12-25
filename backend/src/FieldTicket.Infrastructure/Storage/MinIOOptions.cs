namespace FieldTicket.Infrastructure.Storage;

/// <summary>
/// MinIO 配置选项
/// </summary>
public class MinIOOptions
{
    public const string SectionName = "MinIO";

    /// <summary>
    /// MinIO 端点
    /// </summary>
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>
    /// 访问密钥
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// 密钥
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string Bucket { get; set; } = "attachments";

    /// <summary>
    /// 是否使用 SSL
    /// </summary>
    public bool UseSSL { get; set; } = false;
}

