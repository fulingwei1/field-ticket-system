using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 通知记录实体（审计）
/// </summary>
public class NotificationLog
{
    public Guid LogId { get; set; }
    
    public Guid? TicketId { get; set; }
    
    public string TriggerEvent { get; set; } = string.Empty;
    public Guid? RuleId { get; set; }
    
    // 发送详情
    public JsonDocument Recipients { get; set; } = JsonDocument.Parse("{}"); // 实际发送给谁
    public int SentCount { get; set; } = 0;
    public int FailedCount { get; set; } = 0;
    
    // 消息内容
    public string? MessageContent { get; set; }
    
    public DateTime SentAt { get; set; }
}

