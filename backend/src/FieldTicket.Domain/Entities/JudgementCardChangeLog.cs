namespace FieldTicket.Domain.Entities;

/// <summary>
/// 判断卡变更记录实体
/// </summary>
public class JudgementCardChangeLog
{
    public Guid ChangeLogId { get; set; }
    
    // 关联判断卡
    public string JcCode { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid? PreviousVersionId { get; set; } // 上一版本的 JudgementCardId
    
    // 变更原因（必须填写）
    public string ChangeReason { get; set; } = string.Empty;
    
    // 变更内容摘要
    public string? ChangeSummary { get; set; }
    
    // 变更人
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    
    // 是否推翻上一版本
    public bool IsOverturned { get; set; } = false;
    public Guid? OverturnedBy { get; set; }
    public DateTime? OverturnedAt { get; set; }
}

