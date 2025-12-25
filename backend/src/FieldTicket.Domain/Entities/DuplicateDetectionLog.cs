using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 去重检测记录实体
/// </summary>
public class DuplicateDetectionLog
{
    public Guid LogId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 潜在重复工单列表
    public List<Guid> PotentialDuplicates { get; set; } = new();
    
    // 相似度评分（JSONB）
    public JsonDocument SimilarityScores { get; set; } = JsonDocument.Parse("{}");
    /*
    示例：
    {
      "ticket_id_1": {
        "overall": 0.85,
        "device": 1.0,
        "symptom": 0.75,
        "domain": 1.0,
        "time": 0.8
      }
    }
    */
    
    // 检测时间
    public DateTime DetectedAt { get; set; }
    
    // 检测类型
    public string DetectionType { get; set; } = string.Empty; // "auto", "manual"
}

