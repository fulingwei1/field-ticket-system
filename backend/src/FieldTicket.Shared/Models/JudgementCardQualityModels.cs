namespace FieldTicket.Shared.Models;

/// <summary>
/// 判断卡质量评分DTO
/// </summary>
public class JudgementCardQualityScoreDto
{
    public string JcCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CompletenessScore { get; set; }
    public int LogicConsistencyScore { get; set; }
    public int VerifiabilityScore { get; set; }
    public int EvidenceScore { get; set; }
    public int TotalScore { get; set; }
    public string Level { get; set; } = string.Empty; // Poor/Fair/Good/Excellent
    public List<QualityIssueDto> Issues { get; set; } = new();
    public DateTime ScoredAt { get; set; }
}

/// <summary>
/// 质量问题DTO
/// </summary>
public class QualityIssueDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low/Medium/High/Critical
    public string? Suggestion { get; set; }
}

/// <summary>
/// 批量评分请求
/// </summary>
public class BatchScoreRequest
{
    public List<string> JcCodes { get; set; } = new();
}

