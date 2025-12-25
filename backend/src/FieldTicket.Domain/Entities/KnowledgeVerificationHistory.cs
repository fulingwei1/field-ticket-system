namespace FieldTicket.Domain.Entities;

/// <summary>
/// 知识验证历史实体
/// </summary>
public class KnowledgeVerificationHistory
{
    public Guid VerificationId { get; set; }
    
    // 关联知识
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty; // 'judgement_card', 'solution'
    
    // 验证人
    public Guid VerifiedBy { get; set; }
    
    // 验证时间
    public DateTime VerifiedAt { get; set; }
    
    // 验证备注
    public string? VerificationNote { get; set; }
    
    // 导航属性
    public User? Verifier { get; set; }
}






