namespace FieldTicket.Shared.Models;

/// <summary>
/// 归因建议DTO
/// </summary>
public class AttributionSuggestionDto
{
    public string RootResponsibility { get; set; } = string.Empty; // 'design', 'software', 'parameter', 'assembly', 'documentation', 'other'
    public bool? IsPreventable { get; set; }
    public decimal Confidence { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<SimilarTicketInfo> SimilarTickets { get; set; } = new();
    public Dictionary<string, decimal> AttributionPattern { get; set; } = new();
}

/// <summary>
/// 相似工单信息
/// </summary>
public class SimilarTicketInfo
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string RootResponsibility { get; set; } = string.Empty;
    public bool? IsPreventable { get; set; }
    public decimal SimilarityScore { get; set; }
}

/// <summary>
/// 一致性检查结果DTO
/// </summary>
public class ConsistencyCheckResultDto
{
    public bool IsConsistent { get; set; }
    public decimal ConsistencyScore { get; set; }
    public List<InconsistencyIssue> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 不一致问题
/// </summary>
public class InconsistencyIssue
{
    public string Field { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 归因评估结果DTO
/// </summary>
public class AttributionEvaluationDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalTickets { get; set; }
    public int AttributedTickets { get; set; }
    public decimal AttributionRate { get; set; }
    public Dictionary<string, int> ResponsibilityDistribution { get; set; } = new();
    public Dictionary<string, decimal> PreventabilityRate { get; set; } = new();
    public decimal AverageConfidence { get; set; }
    public List<AttributionTrend> Trends { get; set; } = new();
}

/// <summary>
/// 归因趋势
/// </summary>
public class AttributionTrend
{
    public DateTime Date { get; set; }
    public int TicketCount { get; set; }
    public Dictionary<string, int> ResponsibilityCount { get; set; } = new();
}

/// <summary>
/// 归因统计DTO
/// </summary>
public class AttributionStatisticsDto
{
    public int TotalTickets { get; set; }
    public int AttributedTickets { get; set; }
    public decimal AttributionRate { get; set; }
    public Dictionary<string, int> ResponsibilityDistribution { get; set; } = new();
    public Dictionary<string, decimal> ResponsibilityPercentage { get; set; } = new();
    public int PreventableCount { get; set; }
    public int NonPreventableCount { get; set; }
    public decimal PreventabilityRate { get; set; }
}

/// <summary>
/// 归因特征DTO
/// </summary>
public class AttributionFeaturesDto
{
    public string Domain { get; set; } = string.Empty;
    public string SymptomTitle { get; set; } = string.Empty;
    public string? RootCause { get; set; }
    public string? SolutionType { get; set; }
    public string? DeviceModel { get; set; }
    public List<string> Tags { get; set; } = new();
}

