using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 用户填写历史实体
/// </summary>
public class UserFillingHistory
{
    public Guid HistoryId { get; set; }
    public Guid UserId { get; set; }
    public Guid TicketId { get; set; }
    
    // 填写数据（JSONB）
    public JsonDocument FilledFields { get; set; } = JsonDocument.Parse("{}");
    public int? FillingTime { get; set; }  // 秒
    public JsonDocument? SkippedFields { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public User? User { get; set; }
    public Ticket? Ticket { get; set; }
}

