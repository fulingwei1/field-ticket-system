namespace FieldTicket.Domain.Entities;

/// <summary>
/// 分诊记录实体
/// </summary>
public class TriageNote
{
    public Guid TriageNoteId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 关联判断卡
    public string? JcCode { get; set; }
    
    // 分诊结论
    public string? CurrentHypothesis { get; set; }
    public string? NextAction { get; set; }
    
    // 置信度（1-5）
    public int Confidence { get; set; } // 1-5
    
    // 是否升级
    public bool EscalationRequired { get; set; } = false;
    public Guid? EscalatedTo { get; set; }
    
    // 分诊备注
    public string? Note { get; set; }
    
    // 创建信息
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}


