using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 判断卡关系相关端点
/// </summary>
public static class JudgementCardRelationEndpoints
{
    public static void MapJudgementCardRelationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/judgement-cards")
            .WithTags("JudgementCards")
            .RequireAuthorization();

        // 挖掘判断卡关系
        group.MapPost("{jcCode}/mine-relations", MineRelations)
            .WithName("MineJudgementCardRelations")
            .WithSummary("挖掘判断卡关系");

        // 获取判断卡关系
        group.MapGet("{jcCode}/relations", GetRelations)
            .WithName("GetJudgementCardRelations")
            .WithSummary("获取判断卡关系列表");

        // 计算相似度
        group.MapGet("similarity", CalculateSimilarity)
            .WithName("CalculateJudgementCardSimilarity")
            .WithSummary("计算两个判断卡的相似度");

        // 批量挖掘所有关系
        var adminGroup = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization();

        adminGroup.MapPost("judgement-cards/mine-all-relations", MineAllRelations)
            .WithName("MineAllJudgementCardRelations")
            .WithSummary("批量挖掘所有判断卡关系");

        // 学习路径推荐
        var learningGroup = app.MapGroup("/api/learning-paths")
            .WithTags("LearningPaths")
            .RequireAuthorization();

        learningGroup.MapGet("recommend", RecommendLearningPaths)
            .WithName("RecommendLearningPaths")
            .WithSummary("推荐学习路径");
    }

    private static Guid GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("用户未认证");
        }
        return userId;
    }

    /// <summary>
    /// 挖掘判断卡关系
    /// </summary>
    private static async Task<IResult> MineRelations(
        string jcCode,
        IJudgementCardRelationService service)
    {
        try
        {
            var relations = await service.MineRelationsAsync(jcCode);
            return Results.Ok(new { relations, count = relations.Count });
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
    /// 获取判断卡关系
    /// </summary>
    private static async Task<IResult> GetRelations(
        string jcCode,
        [FromQuery] string? relationType,
        IJudgementCardRelationService service)
    {
        try
        {
            var relations = await service.GetRelationsAsync(jcCode, relationType);
            return Results.Ok(relations);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 计算相似度
    /// </summary>
    private static async Task<IResult> CalculateSimilarity(
        [FromQuery] string jcCode1,
        [FromQuery] string jcCode2,
        IJudgementCardRelationService service)
    {
        try
        {
            var similarity = await service.CalculateSimilarityAsync(jcCode1, jcCode2);
            return Results.Ok(new { similarity, jcCode1, jcCode2 });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 批量挖掘所有关系
    /// </summary>
    private static async Task<IResult> MineAllRelations(
        IJudgementCardRelationService service)
    {
        try
        {
            var count = await service.MineAllRelationsAsync();
            return Results.Ok(new { message = $"已挖掘 {count} 条关系", count });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 推荐学习路径
    /// </summary>
    private static async Task<IResult> RecommendLearningPaths(
        [FromQuery] Guid userId,
        HttpContext context,
        IJudgementCardRelationService service)
    {
        try
        {
            // 如果没有提供 userId，使用当前用户
            var targetUserId = userId == Guid.Empty ? GetUserId(context) : userId;
            var paths = await service.RecommendLearningPathsAsync(targetUserId);
            return Results.Ok(paths);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}









