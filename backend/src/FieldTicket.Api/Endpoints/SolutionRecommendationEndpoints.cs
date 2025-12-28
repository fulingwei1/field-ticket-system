using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 解决方案推荐API端点
/// </summary>
public static class SolutionRecommendationEndpoints
{
    public static void MapSolutionRecommendationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/solutions/recommendations")
            .WithTags("解决方案推荐")
            .RequireAuthorization();

        // 推荐解决方案
        group.MapPost("", RecommendSolutions)
            .WithName("RecommendSolutions")
            .WithDescription("为工单推荐解决方案")
            .Produces<RecommendSolutionsResponse>(200)
            .Produces(400)
            .Produces(401);

        // 获取解决方案统计信息
        group.MapGet("/{solutionId:guid}/statistics", GetSolutionStatistics)
            .WithName("GetSolutionStatistics")
            .WithDescription("获取解决方案统计信息")
            .Produces<SolutionStatistics>(200)
            .Produces(404)
            .Produces(401);

        // 检查版本兼容性
        group.MapPost("/check-compatibility", CheckVersionCompatibility)
            .WithName("CheckSolutionCompatibility")
            .WithDescription("检查解决方案与工单的版本兼容性")
            .Produces<VersionCompatibilityResponse>(200)
            .Produces(400)
            .Produces(401);

        // 计算工单相似度
        group.MapPost("/calculate-similarity", CalculateTicketSimilarity)
            .WithName("CalculateTicketSimilarity")
            .WithDescription("计算两个工单的相似度")
            .Produces<TicketSimilarityResponse>(200)
            .Produces(400)
            .Produces(401);
    }

    /// <summary>
    /// 推荐解决方案
    /// </summary>
    [Authorize]
    private static async Task<IResult> RecommendSolutions(
        [FromBody] RecommendSolutionsRequest request,
        [FromServices] ISolutionRecommendationService recommendationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Recommending solutions for ticket: {TicketId}", request.TicketId);

            var response = await recommendationService.RecommendSolutionsAsync(request);

            logger.LogInformation("Recommended {Count} solutions for ticket {TicketId}",
                response.Recommendations.Count, request.TicketId);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recommending solutions for ticket {TicketId}", request.TicketId);
            return Results.Problem(
                title: "推荐解决方案失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取解决方案统计信息
    /// </summary>
    [Authorize]
    private static async Task<IResult> GetSolutionStatistics(
        Guid solutionId,
        [FromServices] ISolutionRecommendationService recommendationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting statistics for solution: {SolutionId}", solutionId);

            var statistics = await recommendationService.GetSolutionStatisticsAsync(solutionId);

            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting statistics for solution {SolutionId}", solutionId);
            return Results.Problem(
                title: "获取统计信息失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 检查版本兼容性
    /// </summary>
    [Authorize]
    private static async Task<IResult> CheckVersionCompatibility(
        [FromBody] CheckCompatibilityRequest request,
        [FromServices] ISolutionRecommendationService recommendationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Checking compatibility: Solution={SolutionId}, Ticket={TicketId}",
                request.SolutionId, request.TicketId);

            var (isCompatible, message) = await recommendationService.CheckVersionCompatibilityAsync(
                request.SolutionId,
                request.TicketId);

            var response = new VersionCompatibilityResponse
            {
                SolutionId = request.SolutionId,
                TicketId = request.TicketId,
                IsCompatible = isCompatible,
                Message = message
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking compatibility");
            return Results.Problem(
                title: "检查兼容性失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 计算工单相似度
    /// </summary>
    [Authorize]
    private static async Task<IResult> CalculateTicketSimilarity(
        [FromBody] TicketSimilarityRequest request,
        [FromServices] ISolutionRecommendationService recommendationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Calculating similarity between tickets: {Ticket1} and {Ticket2}",
                request.TicketId1, request.TicketId2);

            var similarity = await recommendationService.CalculateTicketSimilarityAsync(
                request.TicketId1,
                request.TicketId2);

            var response = new TicketSimilarityResponse
            {
                TicketId1 = request.TicketId1,
                TicketId2 = request.TicketId2,
                SimilarityScore = similarity
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating ticket similarity");
            return Results.Problem(
                title: "计算相似度失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

/// <summary>
/// 检查兼容性请求
/// </summary>
public record CheckCompatibilityRequest(Guid SolutionId, Guid TicketId);

/// <summary>
/// 版本兼容性响应
/// </summary>
public record VersionCompatibilityResponse
{
    public Guid SolutionId { get; init; }
    public Guid TicketId { get; init; }
    public bool IsCompatible { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// 工单相似度请求
/// </summary>
public record TicketSimilarityRequest(Guid TicketId1, Guid TicketId2);

/// <summary>
/// 工单相似度响应
/// </summary>
public record TicketSimilarityResponse
{
    public Guid TicketId1 { get; init; }
    public Guid TicketId2 { get; init; }
    public double SimilarityScore { get; init; }
}
