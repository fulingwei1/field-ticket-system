using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 知识图谱相关端点
/// </summary>
public static class KnowledgeGraphEndpoints
{
    public static void MapKnowledgeGraphEndpoints(this WebApplication app)
    {
        // 构建知识图谱
        app.MapPost("/api/knowledge-graph/build", BuildKnowledgeGraph)
            .WithName("BuildKnowledgeGraph")
            .WithSummary("构建知识图谱")
            .WithTags("KnowledgeGraph")
            .RequireAuthorization("Admin");

        // 知识检索
        app.MapGet("/api/knowledge-graph/search", SearchKnowledge)
            .WithName("SearchKnowledge")
            .WithSummary("知识检索")
            .WithTags("KnowledgeGraph")
            .RequireAuthorization();

        // 知识推荐
        app.MapGet("/api/knowledge-graph/recommend", RecommendKnowledge)
            .WithName("RecommendKnowledge")
            .WithSummary("知识推荐")
            .WithTags("KnowledgeGraph")
            .RequireAuthorization();

        // 获取知识关系
        app.MapGet("/api/knowledge-graph/relations", GetKnowledgeRelations)
            .WithName("GetKnowledgeRelations")
            .WithSummary("获取知识关系")
            .WithTags("KnowledgeGraph")
            .RequireAuthorization();

        // 挖掘知识关系
        app.MapPost("/api/knowledge-graph/mine-relations", MineKnowledgeRelations)
            .WithName("MineKnowledgeRelations")
            .WithSummary("挖掘知识关系")
            .WithTags("KnowledgeGraph")
            .RequireAuthorization("Admin");
    }

    /// <summary>
    /// 构建知识图谱
    /// </summary>
    private static async Task<IResult> BuildKnowledgeGraph(
        IKnowledgeGraphService service)
    {
        try
        {
            var graph = await service.BuildKnowledgeGraphAsync();
            return Results.Ok(graph);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 知识检索
    /// </summary>
    private static async Task<IResult> SearchKnowledge(
        [FromQuery] string query,
        [FromQuery] int topK = 10,
        [FromQuery] string? nodeType = null,
        IKnowledgeGraphService service)
    {
        try
        {
            var request = new SearchKnowledgeRequest
            {
                Query = query,
                TopK = topK,
                NodeType = nodeType
            };

            var results = await service.SearchKnowledgeAsync(request);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 知识推荐
    /// </summary>
    private static async Task<IResult> RecommendKnowledge(
        [FromQuery] Guid ticketId,
        IKnowledgeGraphService service)
    {
        try
        {
            var recommendations = await service.RecommendKnowledgeAsync(ticketId);
            return Results.Ok(recommendations);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取知识关系
    /// </summary>
    private static async Task<IResult> GetKnowledgeRelations(
        [FromQuery] Guid? nodeId,
        IKnowledgeGraphService service)
    {
        try
        {
            var relations = await service.GetKnowledgeRelationsAsync(nodeId);
            return Results.Ok(relations);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 挖掘知识关系
    /// </summary>
    private static async Task<IResult> MineKnowledgeRelations(
        IKnowledgeGraphService service)
    {
        try
        {
            var relations = await service.MineKnowledgeRelationsAsync();
            return Results.Ok(relations);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

