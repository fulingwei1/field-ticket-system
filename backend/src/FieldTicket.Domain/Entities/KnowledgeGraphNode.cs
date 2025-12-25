using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 知识图谱节点实体
/// </summary>
public class KnowledgeGraphNode
{
    public Guid NodeId { get; set; }
    
    // 节点类型
    public string NodeType { get; set; } = string.Empty; // 'symptom', 'root_cause', 'solution', 'device', 'step'
    
    // 节点标识
    public string NodeCode { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    
    // 节点属性
    public JsonDocument? PropertiesJson { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public List<KnowledgeGraphEdge> OutgoingEdges { get; set; } = new();
    public List<KnowledgeGraphEdge> IncomingEdges { get; set; } = new();
}

