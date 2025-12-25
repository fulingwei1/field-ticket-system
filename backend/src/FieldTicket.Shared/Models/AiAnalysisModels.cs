using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// AI分析结果DTO
/// </summary>
public class AiAnalysisResultDto
{
    public Guid AnalysisId { get; set; }
    public string AnalysisType { get; set; } = string.Empty;
    public DateOnly AnalysisDate { get; set; }
    public Guid? EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public JsonDocument? KeyInsights { get; set; }
    public JsonDocument? Suggestions { get; set; }
    public JsonDocument? PerformanceAnalysis { get; set; }
    public string? AiModel { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>
/// 生成每日总结请求
/// </summary>
public class GenerateDailySummaryRequest
{
    public Guid EngineerId { get; set; }
    public DateOnly AnalysisDate { get; set; }
}

/// <summary>
/// 生成每周总结请求
/// </summary>
public class GenerateWeeklySummaryRequest
{
    public Guid EngineerId { get; set; }
    public DateOnly WeekStart { get; set; }
}

/// <summary>
/// 生成团队分析请求
/// </summary>
public class GenerateTeamAnalysisRequest
{
    public Guid? DepartmentId { get; set; }
    public DateOnly AnalysisDate { get; set; }
    public string PeriodType { get; set; } = "monthly";
}

/// <summary>
/// 生成人员安排建议请求
/// </summary>
public class GenerateSchedulingSuggestionRequest
{
    public Guid? DepartmentId { get; set; }
    public DateOnly AnalysisDate { get; set; }
}


