using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 阈值配置DTO
/// </summary>
public class ThresholdConfigDto
{
    public Guid ConfigId { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string ScenarioType { get; set; } = string.Empty;
    public string? ScenarioValue { get; set; }
    public int TimeWindowDays { get; set; }
    public int TriggerCount { get; set; }
    public JsonDocument MatchCriteria { get; set; } = JsonDocument.Parse("{}");
    public decimal? TriggerRate { get; set; }
    public decimal? AccuracyRate { get; set; }
    public decimal? FalsePositiveRate { get; set; }
    public bool IsActive { get; set; }
    public bool IsAutoOptimized { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 阈值评估结果
/// </summary>
public class ThresholdEvaluation
{
    public Guid ConfigId { get; set; }
    public int TotalTriggers { get; set; }
    public int CorrectTriggers { get; set; }
    public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
    public decimal AccuracyRate { get; set; }
    public decimal FalsePositiveRate { get; set; }
    public decimal FalseNegativeRate { get; set; }
    public decimal Score { get; set; }
}

/// <summary>
/// 学习最优阈值请求
/// </summary>
public class LearnOptimalThresholdRequest
{
    public string ScenarioType { get; set; } = string.Empty;
    public string? ScenarioValue { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// 自动优化阈值请求
/// </summary>
public class AutoOptimizeThresholdRequest
{
    public Guid ConfigId { get; set; }
    public bool ApplyOptimization { get; set; } = false;
}

