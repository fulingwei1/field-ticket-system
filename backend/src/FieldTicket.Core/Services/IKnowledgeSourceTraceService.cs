namespace FieldTicket.Core.Services;

/// <summary>
/// 知识来源追溯服务接口
/// </summary>
public interface IKnowledgeSourceTraceService
{
    /// <summary>
    /// 获取知识来源信息
    /// </summary>
    Task<KnowledgeSourceTraceDto?> GetSourceTraceAsync(Guid knowledgeId, string knowledgeType);

    /// <summary>
    /// 记录知识验证
    /// </summary>
    Task RecordVerificationAsync(
        Guid knowledgeId,
        string knowledgeType,
        Guid verifiedBy,
        string? verificationNote = null);

    /// <summary>
    /// 更新知识来源
    /// </summary>
    Task UpdateSourceAsync(
        Guid knowledgeId,
        string knowledgeType,
        UpdateKnowledgeSourceRequest request);

    /// <summary>
    /// 获取知识的验证历史
    /// </summary>
    Task<List<KnowledgeVerificationHistoryDto>> GetVerificationHistoryAsync(
        Guid knowledgeId,
        string knowledgeType,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 获取可信度评估
    /// </summary>
    Task<KnowledgeCredibilityDto> GetCredibilityAsync(Guid knowledgeId, string knowledgeType);
}

/// <summary>
/// 知识来源追溯DTO
/// </summary>
public class KnowledgeSourceTraceDto
{
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public string KnowledgeCode { get; set; } = string.Empty;
    public string KnowledgeName { get; set; } = string.Empty;
    
    // 来源工单
    public Guid? SourceTicketId { get; set; }
    public string? SourceTicketNo { get; set; }
    
    // 创建人
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // 验证信息
    public Guid? VerifiedBy { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public int VerificationCount { get; set; }
}

/// <summary>
/// 更新知识来源请求
/// </summary>
public class UpdateKnowledgeSourceRequest
{
    public Guid? SourceTicketId { get; set; }
}

/// <summary>
/// 知识验证历史DTO
/// </summary>
public class KnowledgeVerificationHistoryDto
{
    public Guid VerificationId { get; set; }
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public Guid VerifiedBy { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string? VerificationNote { get; set; }
}

/// <summary>
/// 知识可信度DTO
/// </summary>
public class KnowledgeCredibilityDto
{
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public decimal CredibilityScore { get; set; } // 0-100
    public string CredibilityLevel { get; set; } = string.Empty; // high/medium/low
    public Dictionary<string, object> ScoreFactors { get; set; } = new();
    public string? Assessment { get; set; }
}









