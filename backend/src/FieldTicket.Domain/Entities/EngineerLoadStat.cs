namespace FieldTicket.Domain.Entities;

/// <summary>
/// 工程师负载统计实体
/// </summary>
public class EngineerLoadStat
{
    public Guid StatId { get; set; }
    
    // 关联工程师
    public Guid EngineerId { get; set; }
    
    // 统计日期
    public DateOnly StatDate { get; set; }
    
    // 负载指标
    public int MentionedCount { get; set; } = 0; // 被@次数
    public int EscalationTakenCount { get; set; } = 0; // 升级接手次数
    public int JudgementReusedCount { get; set; } = 0; // 判断被复用次数
    public int LowConfidenceTakenCount { get; set; } = 0; // 低置信度工单接手次数
    public int TicketsAssigned { get; set; } = 0; // 分配的工单数
    public int TicketsClosed { get; set; } = 0; // 关闭的工单数
    
    // 创建时间
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public User? Engineer { get; set; }
}






