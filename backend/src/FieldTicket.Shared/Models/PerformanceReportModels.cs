namespace FieldTicket.Shared.Models;

/// <summary>
/// 生成绩效报告请求
/// </summary>
public class GeneratePerformanceReportRequest
{
    public Guid? EngineerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public List<string>? IncludeSections { get; set; }
    public string ReportFormat { get; set; } = "json"; // json, pdf, excel
}

/// <summary>
/// 绩效报告DTO
/// </summary>
public class PerformanceReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public PerformanceMetricsDto? EngineerMetrics { get; set; }
    public List<PerformanceMetricsDto>? TeamMetrics { get; set; }
    public List<PerformanceRankingDto>? Ranking { get; set; }
    public List<PerformanceTrendDto>? Trends { get; set; }
    public List<ImprovementSuggestionDto>? ImprovementSuggestions { get; set; }
    public Dictionary<string, object>? Summary { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Guid GeneratedBy { get; set; }
}

/// <summary>
/// 改进建议DTO
/// </summary>
public class ImprovementSuggestionDto
{
    public string Category { get; set; } = string.Empty; // efficiency, quality, knowledge, collaboration
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // high, medium, low
    public List<string>? ActionItems { get; set; }
    public decimal? ExpectedImprovement { get; set; } // 预期改进百分比
}

/// <summary>
/// 导出绩效数据请求
/// </summary>
public class ExportPerformanceDataRequest
{
    public Guid? EngineerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly? PeriodStartFrom { get; set; }
    public DateOnly? PeriodStartTo { get; set; }
    public string ExportFormat { get; set; } = "excel"; // excel, csv, json
    public List<string>? IncludeFields { get; set; }
}

