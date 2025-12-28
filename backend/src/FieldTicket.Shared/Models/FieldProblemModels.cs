namespace FieldTicket.Shared.Models;

/// <summary>
/// 自动生成FieldProblem的结果
/// </summary>
public class AutoGenerateProblemResult
{
    public bool Success { get; set; }
    public Guid? ProblemId { get; set; }
    public string? Message { get; set; }
    public bool IsRepeatProblem { get; set; }
    public Guid? RelatedHistoryProblemId { get; set; }
    public double? SimilarityScore { get; set; }
}

/// <summary>
/// 问题热点统计
/// </summary>
public class ProblemHotspotDto
{
    public string ProblemCategory { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<string> TopSymptoms { get; set; } = new();
    public double AverageProcessingDays { get; set; }
    public int RepeatCount { get; set; }
}

/// <summary>
/// 问题趋势分析
/// </summary>
public class ProblemTrendDto
{
    public DateTime Date { get; set; }
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int ResolvedCount { get; set; }
    public int RepeatCount { get; set; }
    public double AverageProcessingDays { get; set; }
}

/// <summary>
/// 问题统计查询请求
/// </summary>
public class ProblemStatisticsRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProblemCategory { get; set; }
    public int TopN { get; set; } = 10;
}

/// <summary>
/// 问题统计响应
/// </summary>
public class ProblemStatisticsResponse
{
    public int TotalCount { get; set; }
    public int ResolvedCount { get; set; }
    public int RepeatProblemCount { get; set; }
    public double RepeatRate { get; set; }
    public double AverageProcessingDays { get; set; }
    public List<ProblemHotspotDto> Hotspots { get; set; } = new();
    public List<ProblemTrendDto> Trends { get; set; } = new();
}
