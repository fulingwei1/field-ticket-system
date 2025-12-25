using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 校准置信度请求
/// </summary>
public class CalibrateConfidenceRequest
{
    public decimal OriginalConfidence { get; set; }
    public Guid TicketId { get; set; }
    public string? HypothesisId { get; set; }
}

/// <summary>
/// 校准置信度响应
/// </summary>
public class CalibrateConfidenceResponse
{
    public decimal OriginalConfidence { get; set; }
    public decimal CalibratedConfidence { get; set; }
    public string CalibrationMethod { get; set; } = string.Empty;
    public JsonDocument? CalibrationFactors { get; set; }
}

/// <summary>
/// 训练校准模型请求
/// </summary>
public class TrainCalibrationModelRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string ModelType { get; set; } = "linear"; // 'linear', 'ml', 'ensemble'
}

/// <summary>
/// 校准模型DTO
/// </summary>
public class CalibrationModelDto
{
    public Guid ModelId { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public JsonDocument? ModelParameters { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? PrecisionScore { get; set; }
    public decimal? RecallScore { get; set; }
    public decimal? F1Score { get; set; }
    public int? TrainingDataCount { get; set; }
    public DateTime? TrainedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 评估校准效果请求
/// </summary>
public class EvaluateCalibrationRequest
{
    public Guid ModelId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// 校准评估结果DTO
/// </summary>
public class CalibrationEvaluationDto
{
    public Guid ModelId { get; set; }
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public int TestDataCount { get; set; }
    public Dictionary<string, decimal> MetricsByConfidenceRange { get; set; } = new();
}

/// <summary>
/// 置信度分布DTO
/// </summary>
public class ConfidenceDistributionDto
{
    public Dictionary<string, int> DistributionByRange { get; set; } = new();
    public decimal AverageConfidence { get; set; }
    public decimal MedianConfidence { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// 提交校准反馈请求
/// </summary>
public class SubmitCalibrationFeedbackRequest
{
    public Guid TicketId { get; set; }
    public string? HypothesisId { get; set; }
    public decimal OriginalConfidence { get; set; }
    public decimal CalibratedConfidence { get; set; }
    public string ActualResult { get; set; } = string.Empty; // 'correct', 'incorrect', 'partial'
}

/// <summary>
/// 校准因子DTO
/// </summary>
public class CalibrationFactorsDto
{
    public decimal HistoricalAccuracy { get; set; }
    public decimal ContextMatch { get; set; }
    public decimal EvidenceStrength { get; set; }
    public decimal? UserFeedback { get; set; }
}

