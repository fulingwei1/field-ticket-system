using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 根本原因分析端点
/// </summary>
public static class RootCauseAnalysisEndpoints
{
    public static void MapRootCauseAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/root-cause-analysis")
            .WithTags("RootCauseAnalysis")
            .RequireAuthorization();

        // 创建或更新根本原因分析
        group.MapPost("/problems/{problemId:guid}", async (
            Guid problemId,
            [FromBody] CreateRootCauseAnalysisRequest request,
            HttpContext context,
            IRootCauseAnalysisService service) =>
        {
            try
            {
                var userId = GetUserId(context);
                if (userId == null)
                {
                    return Results.Unauthorized();
                }

                var analysis = await service.CreateOrUpdateAnalysisAsync(problemId, request);
                return Results.Ok(analysis);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("CreateOrUpdateRootCauseAnalysis")
        .WithSummary("创建或更新根本原因分析");

        // 获取问题的根本原因分析
        group.MapGet("/problems/{problemId:guid}", async (
            Guid problemId,
            IRootCauseAnalysisService service) =>
        {
            try
            {
                var analysis = await service.GetAnalysisByProblemIdAsync(problemId);
                if (analysis == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(analysis);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetRootCauseAnalysisByProblemId")
        .WithSummary("获取问题的根本原因分析");

        // 获取根本原因分析详情
        group.MapGet("/{analysisId:guid}", async (
            Guid analysisId,
            IRootCauseAnalysisService service) =>
        {
            try
            {
                var analysis = await service.GetAnalysisAsync(analysisId);
                if (analysis == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(analysis);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetRootCauseAnalysis")
        .WithSummary("获取根本原因分析详情");

        // 删除根本原因分析
        group.MapDelete("/{analysisId:guid}", async (
            Guid analysisId,
            IRootCauseAnalysisService service) =>
        {
            try
            {
                await service.DeleteAnalysisAsync(analysisId);
                return Results.Ok(new { success = true, message = "根本原因分析已删除" });
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("DeleteRootCauseAnalysis")
        .WithSummary("删除根本原因分析");

        // 获取5Why分析模板
        group.MapGet("/templates/five-why", async (
            [FromQuery] string problemCategory,
            IRootCauseAnalysisService service) =>
        {
            try
            {
                var template = await service.GetFiveWhyTemplateAsync(problemCategory);
                return Results.Ok(template);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetFiveWhyTemplate")
        .WithSummary("获取5Why分析模板");

        // 获取预防措施建议
        group.MapGet("/suggestions/preventive-measures", async (
            [FromQuery] string rootCauseCategory,
            IRootCauseAnalysisService service) =>
        {
            try
            {
                var suggestions = await service.GetPreventiveMeasureSuggestionsAsync(rootCauseCategory);
                return Results.Ok(suggestions);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetPreventiveMeasureSuggestions")
        .WithSummary("获取预防措施建议");
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}




