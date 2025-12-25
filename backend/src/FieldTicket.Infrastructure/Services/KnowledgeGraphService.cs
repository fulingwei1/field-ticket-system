using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 知识图谱服务实现
/// </summary>
public class KnowledgeGraphService : IKnowledgeGraphService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KnowledgeGraphService> _logger;

    public KnowledgeGraphService(
        ApplicationDbContext dbContext,
        ILogger<KnowledgeGraphService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<KnowledgeGraphDto> BuildKnowledgeGraphAsync()
    {
        _logger.LogInformation("Building knowledge graph");

        // 从工单中提取知识关系
        var relations = await ExtractRelationsFromTicketsAsync();

        // 创建或更新节点
        var nodes = await CreateOrUpdateNodesAsync(relations);

        // 创建或更新边
        var edges = await CreateOrUpdateEdgesAsync(relations, nodes);

        return new KnowledgeGraphDto
        {
            Nodes = nodes.Select(MapToNodeDto).ToList(),
            Edges = edges.Select(MapToRelationDto).ToList(),
            TotalNodes = nodes.Count,
            TotalEdges = edges.Count
        };
    }

    public async Task<List<KnowledgeRelationDto>> MineKnowledgeRelationsAsync()
    {
        _logger.LogInformation("Mining knowledge relations");

        var relations = await ExtractRelationsFromTicketsAsync();
        return relations.Select(MapToRelationDto).ToList();
    }

    public async Task<List<KnowledgeNodeDto>> SearchKnowledgeAsync(SearchKnowledgeRequest request)
    {
        _logger.LogInformation("Searching knowledge: {Query}", request.Query);

        var query = _dbContext.KnowledgeGraphNodes.AsQueryable();

        // 按节点类型过滤
        if (!string.IsNullOrEmpty(request.NodeType))
        {
            query = query.Where(n => n.NodeType == request.NodeType);
        }

        // 按名称搜索
        if (!string.IsNullOrEmpty(request.Query))
        {
            query = query.Where(n => n.NodeName.Contains(request.Query) || n.NodeCode.Contains(request.Query));
        }

        var nodes = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.TopK)
            .ToListAsync();

        return nodes.Select(MapToNodeDto).ToList();
    }

    public async Task<List<KnowledgeNodeDto>> RecommendKnowledgeAsync(Guid ticketId)
    {
        _logger.LogInformation("Recommending knowledge for ticket {TicketId}", ticketId);

        // 获取工单
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 基于症状查找相关节点
        var symptomNode = await _dbContext.KnowledgeGraphNodes
            .FirstOrDefaultAsync(n => n.NodeType == "symptom" && n.NodeName == ticket.SymptomTitle);

        if (symptomNode == null)
        {
            return new List<KnowledgeNodeDto>();
        }

        // 查找相关节点（通过边）
        var relatedNodes = await _dbContext.KnowledgeGraphEdges
            .Where(e => e.SourceNodeId == symptomNode.NodeId || e.TargetNodeId == symptomNode.NodeId)
            .Select(e => e.SourceNodeId == symptomNode.NodeId ? e.TargetNode : e.SourceNode)
            .Distinct()
            .Take(10)
            .ToListAsync();

        return relatedNodes.Select(MapToNodeDto).ToList();
    }

    public async Task<List<KnowledgeRelationDto>> GetKnowledgeRelationsAsync(Guid? nodeId)
    {
        _logger.LogInformation("Getting knowledge relations for node {NodeId}", nodeId);

        var query = _dbContext.KnowledgeGraphEdges
            .Include(e => e.SourceNode)
            .Include(e => e.TargetNode)
            .AsQueryable();

        if (nodeId.HasValue)
        {
            query = query.Where(e => e.SourceNodeId == nodeId.Value || e.TargetNodeId == nodeId.Value);
        }

        var edges = await query.ToListAsync();
        return edges.Select(MapToRelationDto).ToList();
    }

    // 私有辅助方法

    private async Task<List<KnowledgeRelation>> ExtractRelationsFromTicketsAsync()
    {
        var tickets = await _dbContext.Tickets
            .Where(t => !string.IsNullOrEmpty(t.RootCause) && t.Status == "Closed")
            .ToListAsync();

        var relations = new List<KnowledgeRelation>();

        foreach (var ticket in tickets)
        {
            // 症状 → 根因
            if (!string.IsNullOrEmpty(ticket.SymptomTitle) && !string.IsNullOrEmpty(ticket.RootCause))
            {
                relations.Add(new KnowledgeRelation
                {
                    SourceType = "symptom",
                    SourceCode = ticket.SymptomTitle,
                    SourceName = ticket.SymptomTitle,
                    TargetType = "root_cause",
                    TargetCode = ticket.RootCause,
                    TargetName = ticket.RootCause,
                    EdgeType = "causes",
                    Weight = 1.0m
                });
            }

            // 根因 → 解决方案（如果有解决方案）
            if (!string.IsNullOrEmpty(ticket.RootCause))
            {
                var solution = await _dbContext.Solutions
                    .FirstOrDefaultAsync(s => s.TicketId == ticket.TicketId && s.Status == "Published");

                if (solution != null)
                {
                    relations.Add(new KnowledgeRelation
                    {
                        SourceType = "root_cause",
                        SourceCode = ticket.RootCause,
                        SourceName = ticket.RootCause,
                        TargetType = "solution",
                        TargetCode = solution.SolutionCode ?? solution.SolutionId.ToString(),
                        TargetName = solution.Title,
                        EdgeType = "solves",
                        Weight = 1.0m
                    });
                }
            }
        }

        // 合并重复关系，增加权重
        return MergeRelations(relations);
    }

    private List<KnowledgeRelation> MergeRelations(List<KnowledgeRelation> relations)
    {
        var merged = relations
            .GroupBy(r => new { r.SourceCode, r.TargetCode, r.EdgeType })
            .Select(g => new KnowledgeRelation
            {
                SourceType = g.First().SourceType,
                SourceCode = g.Key.SourceCode,
                SourceName = g.First().SourceName,
                TargetType = g.First().TargetType,
                TargetCode = g.Key.TargetCode,
                TargetName = g.First().TargetName,
                EdgeType = g.Key.EdgeType,
                Weight = g.Sum(r => r.Weight)
            })
            .ToList();

        return merged;
    }

    private async Task<List<KnowledgeGraphNode>> CreateOrUpdateNodesAsync(List<KnowledgeRelation> relations)
    {
        var nodes = new Dictionary<string, KnowledgeGraphNode>();

        // 从关系中提取所有节点
        foreach (var relation in relations)
        {
            var sourceKey = $"{relation.SourceType}:{relation.SourceCode}";
            var targetKey = $"{relation.TargetType}:{relation.TargetCode}";

            if (!nodes.ContainsKey(sourceKey))
            {
                var existingNode = await _dbContext.KnowledgeGraphNodes
                    .FirstOrDefaultAsync(n => n.NodeType == relation.SourceType && n.NodeCode == relation.SourceCode);

                if (existingNode == null)
                {
                    existingNode = new KnowledgeGraphNode
                    {
                        NodeId = Guid.NewGuid(),
                        NodeType = relation.SourceType,
                        NodeCode = relation.SourceCode,
                        NodeName = relation.SourceName,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.KnowledgeGraphNodes.Add(existingNode);
                }
                else
                {
                    existingNode.NodeName = relation.SourceName;
                }
                nodes[sourceKey] = existingNode;
            }

            if (!nodes.ContainsKey(targetKey))
            {
                var existingNode = await _dbContext.KnowledgeGraphNodes
                    .FirstOrDefaultAsync(n => n.NodeType == relation.TargetType && n.NodeCode == relation.TargetCode);

                if (existingNode == null)
                {
                    existingNode = new KnowledgeGraphNode
                    {
                        NodeId = Guid.NewGuid(),
                        NodeType = relation.TargetType,
                        NodeCode = relation.TargetCode,
                        NodeName = relation.TargetName,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.KnowledgeGraphNodes.Add(existingNode);
                }
                else
                {
                    existingNode.NodeName = relation.TargetName;
                }
                nodes[targetKey] = existingNode;
            }
        }

        await _dbContext.SaveChangesAsync();
        return nodes.Values.ToList();
    }

    private async Task<List<KnowledgeGraphEdge>> CreateOrUpdateEdgesAsync(
        List<KnowledgeRelation> relations,
        List<KnowledgeGraphNode> nodes)
    {
        var nodeDict = nodes.ToDictionary(n => $"{n.NodeType}:{n.NodeCode}");

        var edges = new List<KnowledgeGraphEdge>();

        foreach (var relation in relations)
        {
            var sourceKey = $"{relation.SourceType}:{relation.SourceCode}";
            var targetKey = $"{relation.TargetType}:{relation.TargetCode}";

            if (!nodeDict.ContainsKey(sourceKey) || !nodeDict.ContainsKey(targetKey))
            {
                continue;
            }

            var sourceNode = nodeDict[sourceKey];
            var targetNode = nodeDict[targetKey];

            var existingEdge = await _dbContext.KnowledgeGraphEdges
                .FirstOrDefaultAsync(e => e.SourceNodeId == sourceNode.NodeId &&
                                         e.TargetNodeId == targetNode.NodeId &&
                                         e.EdgeType == relation.EdgeType);

            if (existingEdge == null)
            {
                existingEdge = new KnowledgeGraphEdge
                {
                    EdgeId = Guid.NewGuid(),
                    SourceNodeId = sourceNode.NodeId,
                    TargetNodeId = targetNode.NodeId,
                    EdgeType = relation.EdgeType,
                    Weight = relation.Weight,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.KnowledgeGraphEdges.Add(existingEdge);
            }
            else
            {
                existingEdge.Weight = relation.Weight;
            }

            edges.Add(existingEdge);
        }

        await _dbContext.SaveChangesAsync();
        return edges;
    }

    private KnowledgeNodeDto MapToNodeDto(KnowledgeGraphNode node)
    {
        return new KnowledgeNodeDto
        {
            NodeId = node.NodeId,
            NodeType = node.NodeType,
            NodeCode = node.NodeCode,
            NodeName = node.NodeName,
            Properties = node.PropertiesJson,
            CreatedAt = node.CreatedAt
        };
    }

    private KnowledgeRelationDto MapToRelationDto(KnowledgeGraphEdge edge)
    {
        return new KnowledgeRelationDto
        {
            EdgeId = edge.EdgeId,
            SourceNodeId = edge.SourceNodeId,
            SourceNodeName = edge.SourceNode.NodeName,
            TargetNodeId = edge.TargetNodeId,
            TargetNodeName = edge.TargetNode.NodeName,
            EdgeType = edge.EdgeType,
            Weight = edge.Weight,
            Metadata = edge.MetadataJson
        };
    }

    private KnowledgeRelationDto MapToRelationDto(KnowledgeRelation relation)
    {
        return new KnowledgeRelationDto
        {
            EdgeId = Guid.Empty,
            SourceNodeId = Guid.Empty,
            SourceNodeName = relation.SourceName,
            TargetNodeId = Guid.Empty,
            TargetNodeName = relation.TargetName,
            EdgeType = relation.EdgeType,
            Weight = relation.Weight
        };
    }

    // 内部类：知识关系
    private class KnowledgeRelation
    {
        public string SourceType { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetCode { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string EdgeType { get; set; } = string.Empty;
        public decimal Weight { get; set; }
    }
}

