using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 统计相关端点
/// </summary>
public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/stats")
            .WithTags("Statistics")
            .RequireAuthorization();

        // 获取统计概览
        group.MapGet("/overview", GetOverview)
            .WithName("GetStatisticsOverview")
            .WithSummary("获取统计概览");

        // 获取Top问题域统计
        group.MapGet("/top-domains", GetTopDomains)
            .WithName("GetTopDomains")
            .WithSummary("获取Top问题域统计");

        // 获取闭环时间分布
        group.MapGet("/closure-time", GetClosureTimeDistribution)
            .WithName("GetClosureTimeDistribution")
            .WithSummary("获取闭环时间分布");

        // 获取工单状态统计
        group.MapGet("/status", GetStatusStatistics)
            .WithName("GetStatusStatistics")
            .WithSummary("获取工单状态统计");

        // 获取趋势数据
        group.MapGet("/trends", GetTrendData)
            .WithName("GetTrendData")
            .WithSummary("获取趋势数据");
    }

    /// <summary>
    /// 获取统计概览
    /// </summary>
    private static async Task<IResult> GetOverview(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IStatisticsService service)
    {
        try
        {
            var overview = await service.GetOverviewAsync(fromDate, toDate);
            return Results.Ok(overview);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取Top问题域统计
    /// </summary>
    private static async Task<IResult> GetTopDomains(
        [FromQuery] int topN = 5,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        IStatisticsService service)
    {
        try
        {
            var domains = await service.GetTopDomainsAsync(topN, fromDate, toDate);
            return Results.Ok(domains);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取闭环时间分布
    /// </summary>
    private static async Task<IResult> GetClosureTimeDistribution(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        IStatisticsService service)
    {
        try
        {
            var distribution = await service.GetClosureTimeDistributionAsync(fromDate, toDate);
            return Results.Ok(distribution);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取工单状态统计
    /// </summary>
    private static async Task<IResult> GetStatusStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        IStatisticsService service)
    {
        try
        {
            var statusStats = await service.GetStatusStatisticsAsync(fromDate, toDate);
            return Results.Ok(statusStats);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取趋势数据
    /// </summary>
    private static async Task<IResult> GetTrendData(
        [FromQuery] string metricType,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string groupBy = "day",
        IStatisticsService service)
    {
        try
        {
            var trends = await service.GetTrendDataAsync(metricType, fromDate, toDate, groupBy);
            return Results.Ok(trends);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

