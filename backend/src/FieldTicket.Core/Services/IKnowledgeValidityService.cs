namespace FieldTicket.Core.Services;

/// <summary>
/// 知识有效期和版本绑定服务接口
/// </summary>
public interface IKnowledgeValidityService
{
    /// <summary>
    /// 检查并更新过期知识标记
    /// </summary>
    Task<int> CheckAndUpdateExpiredKnowledgeAsync();

    /// <summary>
    /// 检查知识是否适用于指定版本
    /// </summary>
    Task<bool> IsKnowledgeApplicableAsync(
        Guid knowledgeId,
        string knowledgeType,
        string? swVersion = null,
        string? hwVersion = null);

    /// <summary>
    /// 检查知识是否过期
    /// </summary>
    Task<bool> IsKnowledgeExpiredAsync(Guid knowledgeId, string knowledgeType);

    /// <summary>
    /// 获取过期知识列表
    /// </summary>
    Task<List<ExpiredKnowledgeInfoDto>> GetExpiredKnowledgeAsync(
        string? knowledgeType = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 更新知识的版本绑定
    /// </summary>
    Task UpdateKnowledgeVersionBindingAsync(
        Guid knowledgeId,
        string knowledgeType,
        UpdateVersionBindingRequest request);

    /// <summary>
    /// 检查工单版本与知识版本的匹配情况
    /// </summary>
    Task<VersionMatchResultDto> CheckVersionMatchAsync(
        Guid ticketId,
        Guid knowledgeId,
        string knowledgeType);
}

/// <summary>
/// 更新版本绑定请求
/// </summary>
public class UpdateVersionBindingRequest
{
    public List<string>? ApplicableSwVersions { get; set; }
    public List<string>? ApplicableHwVersions { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// 过期知识信息DTO
/// </summary>
public class ExpiredKnowledgeInfoDto
{
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public string KnowledgeCode { get; set; } = string.Empty;
    public string KnowledgeName { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int DaysSinceExpiry { get; set; }
    public List<string> ApplicableSwVersions { get; set; } = new();
    public List<string> ApplicableHwVersions { get; set; } = new();
}

/// <summary>
/// 版本匹配结果DTO
/// </summary>
public class VersionMatchResultDto
{
    public bool IsApplicable { get; set; }
    public bool IsExpired { get; set; }
    public string? WarningMessage { get; set; }
    public Dictionary<string, object> MatchDetails { get; set; } = new();
}



















