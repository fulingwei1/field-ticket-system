using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 知识图谱边实体
/// </summary>
public class KnowledgeGraphEdge
{
    public Guid EdgeId { get; set; }
    
    // 关联节点
    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }
    
    // 边类型
    public string EdgeType { get; set; } = string.Empty; // 'causes', 'solves', 'related_to', 'depends_on'
    
    // 权重
    public decimal Weight { get; set; } = 1.0m;
    
    // 元数据
    public JsonDocument? MetadataJson { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public KnowledgeGraphNode SourceNode { get; set; } = null!;
    public KnowledgeGraphNode TargetNode { get; set; } = null!;
}

