using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 阈值配置实体
/// </summary>
public class ThresholdConfig
{
    public Guid ConfigId { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    
    // 场景配置
    public string ScenarioType { get; set; } = string.Empty;  // 'device_type', 'problem_type', 'severity'
    public string? ScenarioValue { get; set; }
    
    // 阈值参数
    public int TimeWindowDays { get; set; } = 30;
    public int TriggerCount { get; set; } = 3;
    public JsonDocument MatchCriteria { get; set; } = JsonDocument.Parse("{}");
    /*
    {
      "same_device": true,
      "same_symptom": true,
      "same_root_responsibility": true
    }
    */
    
    // 效果评估
    public decimal? TriggerRate { get; set; }
    public decimal? AccuracyRate { get; set; }
    public decimal? FalsePositiveRate { get; set; }
    
    // 状态
    public bool IsActive { get; set; } = true;
    public bool IsAutoOptimized { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

