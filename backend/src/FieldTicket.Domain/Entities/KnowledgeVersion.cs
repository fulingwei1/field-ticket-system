using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 知识版本实体
/// </summary>
public class KnowledgeVersion
{
    public Guid VersionId { get; set; }
    
    // 关联知识
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty; // 'judgement_card', 'solution', 'faq'
    
    // 版本信息
    public string VersionNumber { get; set; } = string.Empty; // 'v1.0', 'v1.1', 'v2.0'
    public string? VersionDescription { get; set; }
    
    // 内容
    public JsonDocument ContentJson { get; set; } = JsonDocument.Parse("{}");
    
    // 版本元数据
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCurrent { get; set; } = false;
    
    // 变更信息
    public string? ChangeType { get; set; } // 'created', 'updated', 'deleted'
    public string? ChangeReason { get; set; }
    public string? ChangeSummary { get; set; }
    
    // 导航属性
    public User? Creator { get; set; }
    public List<KnowledgeVersionRelation> SourceRelations { get; set; } = new();
    public List<KnowledgeVersionRelation> TargetRelations { get; set; } = new();
}

