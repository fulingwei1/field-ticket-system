namespace FieldTicket.Domain.Entities;

/// <summary>
/// 知识版本关联实体
/// </summary>
public class KnowledgeVersionRelation
{
    public Guid RelationId { get; set; }
    
    // 关联版本
    public Guid SourceVersionId { get; set; }
    public Guid TargetVersionId { get; set; }
    
    // 关联类型
    public string RelationType { get; set; } = string.Empty; // 'evolves_from', 'replaces', 'related_to'
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public KnowledgeVersion SourceVersion { get; set; } = null!;
    public KnowledgeVersion TargetVersion { get; set; } = null!;
}

