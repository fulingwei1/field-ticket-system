using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 现场问题API端点
/// </summary>
public static class FieldProblemEndpoints
{
    public static void MapFieldProblemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/field-problems")
            .WithTags("现场问题")
            .RequireAuthorization();

        // 从工单自动生成问题
        group.MapPost("/generate-from-ticket/{ticketId:guid}", GenerateFromTicket)
            .WithName("GenerateFieldProblemFromTicket")
            .WithDescription("从工单自动生成现场问题记录")
            .Produces<AutoGenerateProblemResult>(200)
            .Produces(401);

        // 检测重复问题
        group.MapPost("/{ticketId:guid}/detect-repeat", DetectRepeatProblem)
            .WithName("DetectRepeatProblem")
            .WithDescription("检测是否为重复问题")
            .Produces<RepeatDetectionResponse>(200)
            .Produces(401);

        // 获取问题统计
        group.MapPost("/statistics", GetStatistics)
            .WithName("GetProblemStatistics")
            .WithDescription("获取问题统计信息")
            .Produces<ProblemStatisticsResponse>(200)
            .Produces(401);

        // 获取问题热点
        group.MapGet("/hotspots", GetHotspots)
            .WithName("GetProblemHotspots")
            .WithDescription("获取问题热点（高频问题Top-N）")
            .Produces<List<ProblemHotspotDto>>(200)
            .Produces(401);
    }

    /// <summary>
    /// 从工单自动生成问题
    /// </summary>
    [Authorize(Roles = "Admin,Engineer")]
    private static async Task<IResult> GenerateFromTicket(
        Guid ticketId,
        [FromServices] IFieldProblemAutoGenerationService autoGenerationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Generating FieldProblem from ticket: {TicketId}", ticketId);

            var result = await autoGenerationService.GenerateFromTicketAsync(ticketId);

            if (result.Success)
            {
                logger.LogInformation(
                    "Successfully generated FieldProblem {ProblemId} from ticket {TicketId}",
                    result.ProblemId, ticketId);

                return Results.Ok(result);
            }
            else
            {
                logger.LogWarning("Failed to generate FieldProblem from ticket {TicketId}: {Message}",
                    ticketId, result.Message);

                return Results.BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating FieldProblem from ticket {TicketId}", ticketId);
            return Results.Problem(
                title: "生成问题记录失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 检测重复问题
    /// </summary>
    [Authorize]
    private static async Task<IResult> DetectRepeatProblem(
        Guid ticketId,
        [FromQuery] double? threshold,
        [FromServices] IFieldProblemAutoGenerationService autoGenerationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Detecting repeat problem for ticket: {TicketId}", ticketId);

            var (isRepeat, relatedProblemId, similarityScore) =
                await autoGenerationService.DetectRepeatProblemAsync(
                    ticketId,
                    threshold ?? 0.7);

            var response = new RepeatDetectionResponse
            {
                TicketId = ticketId,
                IsRepeat = isRepeat,
                RelatedProblemId = relatedProblemId,
                SimilarityScore = similarityScore
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error detecting repeat problem for ticket {TicketId}", ticketId);
            return Results.Problem(
                title: "检测重复问题失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取问题统计
    /// </summary>
    [Authorize]
    private static async Task<IResult> GetStatistics(
        [FromBody] ProblemStatisticsRequest request,
        [FromServices] IFieldProblemAutoGenerationService autoGenerationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting problem statistics");

            var response = await autoGenerationService.GetStatisticsAsync(request);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting problem statistics");
            return Results.Problem(
                title: "获取统计信息失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取问题热点
    /// </summary>
    [Authorize]
    private static async Task<IResult> GetHotspots(
        [FromQuery] Guid? projectId,
        [FromQuery] int? topN,
        [FromQuery] int? days,
        [FromServices] IFieldProblemAutoGenerationService autoGenerationService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting problem hotspots: ProjectId={ProjectId}, TopN={TopN}, Days={Days}",
                projectId, topN, days);

            var hotspots = await autoGenerationService.GetHotspotsAsync(
                projectId,
                topN ?? 10,
                days ?? 90);

            return Results.Ok(hotspots);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting problem hotspots");
            return Results.Problem(
                title: "获取问题热点失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

/// <summary>
/// 重复问题检测响应
/// </summary>
public record RepeatDetectionResponse
{
    public Guid TicketId { get; init; }
    public bool IsRepeat { get; init; }
    public Guid? RelatedProblemId { get; init; }
    public double SimilarityScore { get; init; }
}
