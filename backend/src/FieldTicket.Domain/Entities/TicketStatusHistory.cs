namespace FieldTicket.Domain.Entities;

/// <summary>
/// 工单状态历史记录实体
/// </summary>
public class TicketStatusHistory
{
    public Guid HistoryId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 状态变更信息
    public string FromStatus { get; set; } = string.Empty; // 原状态
    public string ToStatus { get; set; } = string.Empty; // 新状态
    
    // 变更原因
    public string? ChangeReason { get; set; } // 状态变更原因
    
    // 变更人
    public Guid ChangedBy { get; set; } // 操作人ID
    public string? ChangedByName { get; set; } // 操作人姓名（冗余字段，便于查询）
    
    // 变更时间
    public DateTime ChangedAt { get; set; }
    
    // 变更类型
    public string ChangeType { get; set; } = "manual"; // manual（手动）/auto（自动）/system（系统）
    
    // 关联信息（可选）
    public Guid? RelatedEntityId { get; set; } // 关联实体ID（如分诊ID、解决方案ID等）
    public string? RelatedEntityType { get; set; } // 关联实体类型（如Triage, Solution, Verification等）
    
    // 备注
    public string? Notes { get; set; }
}










