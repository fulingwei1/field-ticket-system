namespace FieldTicket.Infrastructure.WeCom;

/// <summary>
/// 企业微信 OAuth2 配置选项
/// </summary>
public class WeComOptions
{
    public const string SectionName = "WeCom";

    /// <summary>
    /// 企业ID
    /// </summary>
    public string CorpId { get; set; } = string.Empty;

    /// <summary>
    /// 自建应用ID
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// 应用密钥
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth回调URL
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Web前端基础URL（用于生成通知链接）
    /// </summary>
    public string WebBaseUrl { get; set; } = "http://localhost:3000";
}

