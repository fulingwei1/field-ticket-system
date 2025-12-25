namespace FieldTicket.Domain.Entities;

/// <summary>
/// 判断卡使用历史实体
/// </summary>
public class JudgementCardUsageHistory
{
    public Guid UsageHistoryId { get; set; }
    
    // 关联判断卡
    public string JcCode { get; set; } = string.Empty;
    public int JcVersion { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 使用人
    public Guid UsedBy { get; set; }
    public DateTime UsedAt { get; set; }
    
    // 使用结果
    public string? Result { get; set; } // 'correct', 'incorrect', 'partial', null
    public string? Feedback { get; set; }
    
    // 是否有效（用于统计）
    public bool IsValid { get; set; } = true;
}

