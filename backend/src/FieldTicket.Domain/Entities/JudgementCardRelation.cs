using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 判断卡关系实体
/// </summary>
public class JudgementCardRelation
{
    public Guid RelationId { get; set; }
    
    // 关联判断卡
    public string SourceJcCode { get; set; } = string.Empty;
    public string TargetJcCode { get; set; } = string.Empty;
    
    // 关系类型：similar, depends_on, evolves_from, related_to
    public string RelationType { get; set; } = string.Empty;
    
    // 关系强度（0.0-1.0）
    public decimal RelationStrength { get; set; } = 0.5m;
    
    // 元数据（JSONB）
    public JsonDocument? Metadata { get; set; }
    
    // 创建时间
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public JudgementCard? SourceJudgementCard { get; set; }
    public JudgementCard? TargetJudgementCard { get; set; }
}









