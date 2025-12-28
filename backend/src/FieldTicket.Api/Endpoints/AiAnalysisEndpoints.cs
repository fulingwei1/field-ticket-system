using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// AI分析相关 API 端点
/// </summary>
public static class AiAnalysisEndpoints
{
    public static void MapAiAnalysisEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ai-analysis").WithTags("AI Analysis").RequireAuthorization();

        // 生成每日总结
        group.MapPost("/daily-summary", async (
            [FromBody] GenerateDailySummaryRequest request,
            HttpContext context,
            IAiAnalysisService aiAnalysisService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：工程师只能生成自己的总结
            if (request.EngineerId != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            try
            {
                var result = await aiAnalysisService.GenerateDailySummaryAsync(
                    request.EngineerId,
                    request.AnalysisDate,
                    userId);

                return Results.Ok(new { success = true, result });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GenerateDailySummary")
        .WithSummary("生成每日工作总结")
        .Produces<object>()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // 生成每周总结
        group.MapPost("/weekly-summary", async (
            [FromBody] GenerateWeeklySummaryRequest request,
            HttpContext context,
            IAiAnalysisService aiAnalysisService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查
            if (request.EngineerId != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            try
            {
                var result = await aiAnalysisService.GenerateWeeklySummaryAsync(
                    request.EngineerId,
                    request.WeekStart,
                    userId);

                return Results.Ok(new { success = true, result });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GenerateWeeklySummary")
        .WithSummary("生成每周工作总结")
        .Produces<object>()
        .Produces(StatusCodes.Status403Forbidden);

        // 生成团队分析
        group.MapPost("/team-analysis", async (
            [FromBody] GenerateTeamAnalysisRequest request,
            HttpContext context,
            IAiAnalysisService aiAnalysisService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只有部门经理和管理员可以生成团队分析
            var user = await GetCurrentUserAsync(context, userId.Value);
            if (user?.Role != "Manager" && user?.Role != "Admin")
            {
                return Results.Forbid();
            }

            try
            {
                var result = await aiAnalysisService.GenerateTeamAnalysisAsync(
                    request.DepartmentId,
                    request.AnalysisDate,
                    request.PeriodType,
                    userId);

                return Results.Ok(new { success = true, result });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GenerateTeamAnalysis")
        .WithSummary("生成团队分析")
        .Produces<object>()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);

        // 生成人员安排建议
        group.MapPost("/scheduling-suggestion", async (
            [FromBody] GenerateSchedulingSuggestionRequest request,
            HttpContext context,
            IAiAnalysisService aiAnalysisService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只有部门经理和管理员可以生成人员安排建议
            var user = await GetCurrentUserAsync(context, userId.Value);
            if (user?.Role != "Manager" && user?.Role != "Admin")
            {
                return Results.Forbid();
            }

            try
            {
                var result = await aiAnalysisService.GenerateSchedulingSuggestionAsync(
                    request.DepartmentId,
                    request.AnalysisDate,
                    userId);

                return Results.Ok(new { success = true, result });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GenerateSchedulingSuggestion")
        .WithSummary("生成人员安排建议")
        .Produces<object>()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);

        // 获取分析结果列表
        group.MapGet("/results", async (
            HttpContext context,
            IAiAnalysisService aiAnalysisService,
            [FromQuery] string? analysisType,
            [FromQuery] Guid? engineerId,
            [FromQuery] Guid? departmentId,
            [FromQuery] DateOnly? analysisDateFrom,
            [FromQuery] DateOnly? analysisDateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：工程师只能查看自己的分析结果
            if (engineerId.HasValue && engineerId.Value != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            var filter = new AiAnalysisQueryFilter
            {
                AnalysisType = analysisType,
                EngineerId = engineerId ?? userId,
                DepartmentId = departmentId,
                AnalysisDateFrom = analysisDateFrom,
                AnalysisDateTo = analysisDateTo,
            };

            var (items, total) = await aiAnalysisService.GetAnalysisResultsAsync(filter, page, pageSize);

            return Results.Ok(new { items, total, page, pageSize });
        })
        .WithName("GetAnalysisResults")
        .WithSummary("获取分析结果列表")
        .Produces<object>()
        .Produces(StatusCodes.Status403Forbidden);

        // 获取分析结果详情
        group.MapGet("/results/{analysisId:guid}", async (
            Guid analysisId,
            HttpContext context,
            IAiAnalysisService aiAnalysisService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await aiAnalysisService.GetAnalysisResultAsync(analysisId);

            if (result == null)
            {
                return Results.NotFound();
            }

            // 权限检查：工程师只能查看自己的分析结果
            if (result.EngineerId.HasValue && result.EngineerId.Value != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            return Results.Ok(result);
        })
        .WithName("GetAnalysisResult")
        .WithSummary("获取分析结果详情")
        .Produces<AiAnalysisResultDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }

    private static async Task<User?> GetCurrentUserAsync(HttpContext context, Guid userId)
    {
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Users.FindAsync(userId);
    }
}

