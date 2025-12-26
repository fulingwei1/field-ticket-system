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
/// 绩效管理相关 API 端点
/// </summary>
public static class PerformanceEndpoints
{
    public static void MapPerformanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/performance").WithTags("Performance").RequireAuthorization();

        // 获取绩效指标列表
        group.MapGet("/metrics", async (
            [FromQuery] Guid? engineerId,
            [FromQuery] Guid? departmentId,
            [FromQuery] string? periodType,
            [FromQuery] DateOnly? periodStartFrom,
            [FromQuery] DateOnly? periodStartTo,
            [FromQuery] decimal? minOverallScore,
            [FromQuery] string? performanceLevel,
            HttpContext context,
            IPerformanceService performanceService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：工程师只能查看自己的绩效
            if (engineerId.HasValue && engineerId.Value != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            var filter = new PerformanceQueryFilter
            {
                EngineerId = engineerId ?? userId,
                DepartmentId = departmentId,
                PeriodType = periodType,
                PeriodStartFrom = periodStartFrom,
                PeriodStartTo = periodStartTo,
                MinOverallScore = minOverallScore,
                PerformanceLevel = performanceLevel
            };

            var (items, total) = await performanceService.GetMetricsAsync(filter, page, pageSize);

            return Results.Ok(new { items, total, page, pageSize });
        })
        .WithName("GetPerformanceMetrics")
        .WithSummary("获取绩效指标列表")
        .Produces<object>();

        // 获取工程师绩效
        group.MapGet("/engineer/{engineerId:guid}", async (
            Guid engineerId,
            [FromQuery] string periodType,
            [FromQuery] DateOnly periodStart,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var metrics = await performanceService.GetEngineerMetricsAsync(
                    engineerId,
                    periodType,
                    periodStart,
                    userId);

                if (metrics == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(metrics);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetEngineerPerformance")
        .WithSummary("获取工程师绩效")
        .Produces<PerformanceMetricsDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // 获取团队绩效
        group.MapGet("/team", async (
            [FromQuery] Guid? departmentId,
            [FromQuery] string periodType,
            [FromQuery] DateOnly periodStart,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只有部门经理和管理员可以查看团队绩效
            var user = await GetCurrentUserAsync(context, userId.Value);
            if (user?.Role != "Manager" && user?.Role != "Admin")
            {
                return Results.Forbid();
            }

            try
            {
                var metrics = await performanceService.GetTeamMetricsAsync(
                    departmentId,
                    periodType,
                    periodStart,
                    userId);

                return Results.Ok(metrics);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetTeamPerformance")
        .WithSummary("获取团队绩效")
        .Produces<List<PerformanceMetricsDto>>()
        .Produces(StatusCodes.Status403Forbidden);

        // 获取绩效排名
        group.MapGet("/ranking", async (
            [FromQuery] string periodType,
            [FromQuery] DateOnly periodStart,
            [FromQuery] Guid? departmentId,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var ranking = await performanceService.GetRankingAsync(
                    periodType,
                    periodStart,
                    departmentId,
                    userId);

                return Results.Ok(ranking);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetPerformanceRanking")
        .WithSummary("获取绩效排名")
        .Produces<List<PerformanceRankingDto>>();

        // 手动触发绩效计算
        group.MapPost("/calculate", async (
            [FromBody] CalculatePerformanceRequest request,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只有管理员可以手动触发计算
            var user = await GetCurrentUserAsync(context, userId.Value);
            if (user?.Role != "Admin")
            {
                return Results.Forbid();
            }

            try
            {
                var metrics = await performanceService.CalculateMetricsAsync(
                    request.EngineerId,
                    request.PeriodType,
                    request.PeriodStart,
                    request.PeriodEnd);

                return Results.Ok(new { success = true, metrics });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("CalculatePerformance")
        .WithSummary("手动触发绩效计算")
        .Produces<object>()
        .Produces(StatusCodes.Status403Forbidden);

        // 获取绩效趋势
        group.MapGet("/trends", async (
            [FromQuery] Guid engineerId,
            [FromQuery] string periodType,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：工程师只能查看自己的趋势
            if (engineerId != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            try
            {
                var trends = await performanceService.GetTrendsAsync(
                    engineerId,
                    periodType,
                    fromDate,
                    toDate);

                return Results.Ok(trends);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetPerformanceTrends")
        .WithSummary("获取绩效趋势")
        .Produces<List<PerformanceTrendDto>>()
        .Produces(StatusCodes.Status403Forbidden);

        // 生成绩效报告
        group.MapPost("/reports/generate", async (
            [FromBody] GeneratePerformanceReportRequest request,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var report = await performanceService.GeneratePerformanceReportAsync(request, userId.Value);
                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GeneratePerformanceReport")
        .WithSummary("生成绩效报告")
        .Produces<PerformanceReportDto>();

        // 导出绩效数据
        group.MapPost("/export", async (
            [FromBody] ExportPerformanceDataRequest request,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var data = await performanceService.ExportPerformanceDataAsync(request, userId.Value);
                var contentType = request.ExportFormat.ToLower() switch
                {
                    "json" => "application/json",
                    "csv" => "text/csv",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/octet-stream"
                };

                var fileName = $"performance_export_{DateTime.UtcNow:yyyyMMddHHmmss}.{request.ExportFormat}";
                return Results.File(data, contentType, fileName);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("ExportPerformanceData")
        .WithSummary("导出绩效数据")
        .Produces<byte[]>();

        // 生成个性化改进建议
        group.MapGet("/engineer/{engineerId:guid}/improvement-suggestions", async (
            Guid engineerId,
            [FromQuery] string periodType,
            [FromQuery] DateOnly periodStart,
            HttpContext context,
            IPerformanceService performanceService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：工程师只能查看自己的建议
            if (engineerId != userId.Value)
            {
                var user = await GetCurrentUserAsync(context, userId.Value);
                if (user?.Role != "Manager" && user?.Role != "Admin")
                {
                    return Results.Forbid();
                }
            }

            try
            {
                var suggestions = await performanceService.GenerateImprovementSuggestionsAsync(
                    engineerId,
                    periodType,
                    periodStart,
                    userId.Value);

                return Results.Ok(suggestions);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetImprovementSuggestions")
        .WithSummary("生成个性化改进建议")
        .Produces<List<ImprovementSuggestionDto>>()
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

