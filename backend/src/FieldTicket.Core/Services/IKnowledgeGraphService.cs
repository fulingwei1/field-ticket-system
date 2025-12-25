using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 知识图谱服务接口
/// </summary>
public interface IKnowledgeGraphService
{
    /// <summary>
    /// 构建知识图谱
    /// </summary>
    Task<KnowledgeGraphDto> BuildKnowledgeGraphAsync();

    /// <summary>
    /// 挖掘知识关系
    /// </summary>
    Task<List<KnowledgeRelationDto>> MineKnowledgeRelationsAsync();

    /// <summary>
    /// 知识检索（基于图谱）
    /// </summary>
    Task<List<KnowledgeNodeDto>> SearchKnowledgeAsync(SearchKnowledgeRequest request);

    /// <summary>
    /// 知识推荐
    /// </summary>
    Task<List<KnowledgeNodeDto>> RecommendKnowledgeAsync(Guid ticketId);

    /// <summary>
    /// 获取知识关系
    /// </summary>
    Task<List<KnowledgeRelationDto>> GetKnowledgeRelationsAsync(Guid? nodeId);
}

