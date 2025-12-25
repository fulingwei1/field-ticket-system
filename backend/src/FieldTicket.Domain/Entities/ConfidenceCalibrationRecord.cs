using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 置信度校准记录实体
/// </summary>
public class ConfidenceCalibrationRecord
{
    public Guid RecordId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 假设标识
    public string? HypothesisId { get; set; }
    
    // 原始置信度
    public decimal OriginalConfidence { get; set; }
    
    // 校准后置信度
    public decimal? CalibratedConfidence { get; set; }
    
    // 实际结果
    public string? ActualResult { get; set; } // 'correct', 'incorrect', 'partial'
    
    // 校准信息
    public string? CalibrationMethod { get; set; } // 'historical', 'context', 'user_feedback'
    public JsonDocument? CalibrationFactorsJson { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public Ticket Ticket { get; set; } = null!;
}

