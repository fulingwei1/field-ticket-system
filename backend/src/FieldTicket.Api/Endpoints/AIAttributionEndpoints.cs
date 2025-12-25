using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// AI辅助归因相关端点
/// </summary>
public static class AIAttributionEndpoints
{
    public static void MapAIAttributionEndpoints(this WebApplication app)
    {
        // 生成归因建议
        app.MapPost("/api/tickets/{ticketId}/ai-attribution/suggest", SuggestAttribution)
            .WithName("SuggestAttribution")
            .WithSummary("生成归因建议")
            .WithTags("Attribution")
            .RequireAuthorization();

        // 检查一致性
        app.MapPost("/api/tickets/{ticketId}/ai-attribution/check-consistency", CheckConsistency)
            .WithName("CheckAttributionConsistency")
            .WithSummary("检查归因一致性")
            .WithTags("Attribution")
            .RequireAuthorization();

        // 评估归因效果
        app.MapGet("/api/attribution/evaluate", EvaluateAttribution)
            .WithName("EvaluateAttribution")
            .WithSummary("评估归因效果")
            .WithTags("Attribution")
            .RequireAuthorization("Admin");

        // 获取归因统计
        app.MapGet("/api/attribution/statistics", GetAttributionStatistics)
            .WithName("GetAttributionStatistics")
            .WithSummary("获取归因统计")
            .WithTags("Attribution")
            .RequireAuthorization();
    }

    /// <summary>
    /// 生成归因建议
    /// </summary>
    private static async Task<IResult> SuggestAttribution(
        Guid ticketId,
        IAIAttributionService service)
    {
        try
        {
            var suggestion = await service.SuggestAttributionAsync(ticketId);
            return Results.Ok(suggestion);
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
    /// 检查一致性
    /// </summary>
    private static async Task<IResult> CheckConsistency(
        Guid ticketId,
        [FromBody] CheckConsistencyRequest request,
        IAIAttributionService service)
    {
        try
        {
            var result = await service.CheckConsistencyAsync(
                ticketId,
                request.RootResponsibility,
                request.IsPreventable);
            return Results.Ok(result);
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
    /// 评估归因效果
    /// </summary>
    private static async Task<IResult> EvaluateAttribution(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IAIAttributionService service)
    {
        try
        {
            var evaluation = await service.EvaluateAttributionAsync(fromDate, toDate);
            return Results.Ok(evaluation);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取归因统计
    /// </summary>
    private static async Task<IResult> GetAttributionStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IAIAttributionService service)
    {
        try
        {
            var statistics = await service.GetAttributionStatisticsAsync(fromDate, toDate);
            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    // 内部请求类
    private class CheckConsistencyRequest
    {
        public string RootResponsibility { get; set; } = string.Empty;
        public bool? IsPreventable { get; set; }
    }
}

