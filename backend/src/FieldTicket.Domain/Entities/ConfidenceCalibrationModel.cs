using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 置信度校准模型实体
/// </summary>
public class ConfidenceCalibrationModel
{
    public Guid ModelId { get; set; }
    
    // 模型版本
    public string ModelVersion { get; set; } = string.Empty;
    
    // 模型类型
    public string ModelType { get; set; } = string.Empty; // 'linear', 'ml', 'ensemble'
    
    // 模型参数
    public JsonDocument ModelParametersJson { get; set; } = JsonDocument.Parse("{}");
    
    // 模型性能
    public decimal? Accuracy { get; set; }
    public decimal? PrecisionScore { get; set; }
    public decimal? RecallScore { get; set; }
    public decimal? F1Score { get; set; }
    
    // 训练信息
    public int? TrainingDataCount { get; set; }
    public DateTime? TrainedAt { get; set; }
    public bool IsActive { get; set; } = false;
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
}

