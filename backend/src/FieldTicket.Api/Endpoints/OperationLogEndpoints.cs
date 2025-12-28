using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 操作日志API端点
/// </summary>
public static class OperationLogEndpoints
{
    public static void MapOperationLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operation-logs")
            .WithTags("操作日志")
            .RequireAuthorization();

        // 查询操作日志
        group.MapPost("/query", QueryOperationLogs)
            .WithName("QueryOperationLogs")
            .WithDescription("查询操作日志")
            .Produces<OperationLogQueryResult>(200)
            .Produces(401)
            .Produces(403);

        // 获取日志详情
        group.MapGet("/{logId}", GetOperationLogDetail)
            .WithName("GetOperationLogDetail")
            .WithDescription("获取操作日志详情")
            .Produces<OperationLogDetailDto>(200)
            .Produces(404)
            .Produces(401);

        // 获取统计信息
        group.MapGet("/statistics", GetStatistics)
            .WithName("GetOperationLogStatistics")
            .WithDescription("获取操作日志统计信息")
            .Produces<OperationLogStatistics>(200)
            .Produces(401);

        // 删除旧日志（仅管理员）
        group.MapDelete("/cleanup", CleanupOldLogs)
            .WithName("CleanupOldLogs")
            .WithDescription("删除指定日期之前的操作日志")
            .Produces<int>(200)
            .Produces(401)
            .Produces(403);
    }

    /// <summary>
    /// 查询操作日志
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> QueryOperationLogs(
        [FromBody] OperationLogQueryRequest request,
        [FromServices] OperationLogService logService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Querying operation logs: Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);

            var result = await logService.QueryLogsAsync(request);

            logger.LogInformation("Found {Total} operation logs", result.Total);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying operation logs");
            return Results.Problem(
                title: "查询操作日志失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取日志详情
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetOperationLogDetail(
        Guid logId,
        [FromServices] OperationLogService logService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting operation log detail: {LogId}", logId);

            var detail = await logService.GetLogDetailAsync(logId);

            if (detail == null)
            {
                return Results.NotFound(new { message = $"未找到日志 {logId}" });
            }

            return Results.Ok(detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting operation log detail: {LogId}", logId);
            return Results.Problem(
                title: "获取日志详情失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromServices] OperationLogService logService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting operation log statistics: StartDate={StartDate}, EndDate={EndDate}",
                startDate, endDate);

            var statistics = await logService.GetStatisticsAsync(startDate, endDate);

            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting operation log statistics");
            return Results.Problem(
                title: "获取统计信息失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 清理旧日志
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> CleanupOldLogs(
        [FromQuery] DateTime beforeDate,
        [FromServices] OperationLogService logService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Cleaning up operation logs before {Date}", beforeDate);

            var deletedCount = await logService.DeleteLogsBeforeAsync(beforeDate);

            logger.LogInformation("Deleted {Count} operation logs", deletedCount);

            return Results.Ok(deletedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up operation logs");
            return Results.Problem(
                title: "清理日志失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
