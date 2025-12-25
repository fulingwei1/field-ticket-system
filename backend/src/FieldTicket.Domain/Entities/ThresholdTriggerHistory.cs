namespace FieldTicket.Domain.Entities;

/// <summary>
/// 阈值触发历史实体
/// </summary>
public class ThresholdTriggerHistory
{
    public Guid HistoryId { get; set; }
    public Guid ConfigId { get; set; }
    public Guid TicketId { get; set; }
    
    // 触发信息
    public DateTime TriggerTime { get; set; }
    public List<Guid> MatchedTickets { get; set; } = new();
    public string? ActualResult { get; set; }  // 'correct', 'false_positive', 'false_negative'
    
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public ThresholdConfig? Config { get; set; }
    public Ticket? Ticket { get; set; }
}

