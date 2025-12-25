namespace FieldTicket.Domain.Entities;

/// <summary>
/// 假设验证步骤实体
/// </summary>
public class HypothesisVerificationStep
{
    public Guid StepId { get; set; }
    
    // 关联对话
    public Guid ConversationId { get; set; }
    
    // 假设标识
    public string? HypothesisId { get; set; }
    
    // 验证步骤
    public string StepDescription { get; set; } = string.Empty;
    public string? StepType { get; set; } // 'check', 'test', 'observe', 'measure'
    public string? ExpectedResult { get; set; }
    public string? ActualResult { get; set; }
    
    // 验证结果
    public string VerificationStatus { get; set; } = "pending"; // 'pending', 'passed', 'failed', 'inconclusive'
    public string? VerificationNotes { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public DiagnosisConversation Conversation { get; set; } = null!;
}


