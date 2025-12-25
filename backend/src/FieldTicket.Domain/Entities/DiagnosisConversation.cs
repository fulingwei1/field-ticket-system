using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 诊断对话实体
/// </summary>
public class DiagnosisConversation
{
    public Guid ConversationId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 对话状态
    public string? CurrentHypothesis { get; set; }
    public decimal? CurrentConfidence { get; set; }
    public int ConversationRound { get; set; } = 0;
    public string Status { get; set; } = "active"; // 'active', 'completed', 'abandoned'
    
    // 诊断路径（JSONB）
    public JsonDocument? DiagnosisPathJson { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public Ticket Ticket { get; set; } = null!;
    public List<HypothesisVerificationStep> VerificationSteps { get; set; } = new();
}


