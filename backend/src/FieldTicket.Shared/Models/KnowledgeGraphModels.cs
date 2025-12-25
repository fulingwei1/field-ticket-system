using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 知识节点DTO
/// </summary>
public class KnowledgeNodeDto
{
    public Guid NodeId { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string NodeCode { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public JsonDocument? Properties { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 知识关系DTO
/// </summary>
public class KnowledgeRelationDto
{
    public Guid EdgeId { get; set; }
    public Guid SourceNodeId { get; set; }
    public string SourceNodeName { get; set; } = string.Empty;
    public Guid TargetNodeId { get; set; }
    public string TargetNodeName { get; set; } = string.Empty;
    public string EdgeType { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public JsonDocument? Metadata { get; set; }
}

/// <summary>
/// 知识图谱DTO
/// </summary>
public class KnowledgeGraphDto
{
    public List<KnowledgeNodeDto> Nodes { get; set; } = new();
    public List<KnowledgeRelationDto> Edges { get; set; } = new();
    public int TotalNodes { get; set; }
    public int TotalEdges { get; set; }
}

/// <summary>
/// 知识检索请求
/// </summary>
public class SearchKnowledgeRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 10;
    public string? NodeType { get; set; }
}

