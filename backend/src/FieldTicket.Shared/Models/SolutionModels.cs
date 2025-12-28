using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 创建解决方案请求
/// </summary>
public class CreateSolutionRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SolutionType { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    public JsonDocument ChangeDetailJson { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument VerificationChecklistJson { get; set; } = JsonDocument.Parse("{}");
    
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; }
    
    public string RiskLevel { get; set; } = "medium";
    public string? RiskDescription { get; set; }
    
    public bool RollbackPossible { get; set; } = true;
    public string? RollbackProcedure { get; set; }
}

/// <summary>
/// 更新解决方案请求
/// </summary>
public class UpdateSolutionRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? SolutionType { get; set; }
    public string? ReleaseType { get; set; }
    
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    public JsonDocument? ChangeDetailJson { get; set; }
    public JsonDocument? VerificationChecklistJson { get; set; }
    
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; }
    
    public string? RiskLevel { get; set; }
    public string? RiskDescription { get; set; }
    
    public bool? RollbackPossible { get; set; }
    public string? RollbackProcedure { get; set; }
}

/// <summary>
/// 解决方案DTO
/// </summary>
public class SolutionDto
{
    public Guid SolutionId { get; set; }
    public string SolutionCode { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SolutionType { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    public JsonDocument ChangeDetailJson { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument VerificationChecklistJson { get; set; } = JsonDocument.Parse("{}");
    
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; }
    
    public string RiskLevel { get; set; } = string.Empty;
    public string? RiskDescription { get; set; }
    
    public bool RollbackPossible { get; set; }
    public string? RollbackProcedure { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
}

/// <summary>
/// 推荐解决方案请求
/// </summary>
public class RecommendSolutionsRequest
{
    public Guid TicketId { get; set; }
    public int TopK { get; set; } = 3; // 返回Top-K个推荐
    public bool IncludeExpired { get; set; } = false; // 是否包含过期方案
    public double MinSimilarityScore { get; set; } = 0.3; // 最小相似度阈值
}

/// <summary>
/// 解决方案推荐结果
/// </summary>
public class SolutionRecommendationDto
{
    public Guid SolutionId { get; set; }
    public string SolutionCode { get; set; } = string.Empty;
    public Guid SourceTicketId { get; set; }
    public string SourceTicketNo { get; set; } = string.Empty;

    // 解决方案基本信息
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SolutionType { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;

    // 版本信息
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }

    // 推荐分数和原因
    public double MatchScore { get; set; } // 0.0-1.0
    public List<SolutionMatchReason> MatchReasons { get; set; } = new();
    public Dictionary<string, double> ScoreBreakdown { get; set; } = new();

    // 统计信息
    public SolutionStatistics Statistics { get; set; } = new();

    // 版本兼容性
    public bool IsVersionCompatible { get; set; }
    public string? VersionCompatibilityMessage { get; set; }

    // 时间信息
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
}

/// <summary>
/// 匹配原因
/// </summary>
public class SolutionMatchReason
{
    public string ReasonType { get; set; } = string.Empty; // 'domain_match', 'symptom_similarity', 'high_success_rate', 'version_compatible'
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; } // 该原因对总分的贡献权重
}

/// <summary>
/// 解决方案统计信息
/// </summary>
public class SolutionStatistics
{
    public int TotalUsageCount { get; set; } // 总使用次数
    public int VerificationCount { get; set; } // 验证次数
    public int SuccessCount { get; set; } // 成功次数
    public int FailureCount { get; set; } // 失败次数
    public double SuccessRate { get; set; } // 成功率 (0.0-1.0)
    public int DaysSinceLastUse { get; set; } // 距离上次使用的天数
}

/// <summary>
/// 推荐解决方案响应
/// </summary>
public class RecommendSolutionsResponse
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public List<SolutionRecommendationDto> Recommendations { get; set; } = new();
    public int TotalCandidates { get; set; } // 候选方案总数
    public string? Message { get; set; } // 额外说明信息
}


