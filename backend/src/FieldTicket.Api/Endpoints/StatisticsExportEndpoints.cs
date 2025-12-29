using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 统计数据导出相关端点
/// </summary>
public static class StatisticsExportEndpoints
{
    public static void MapStatisticsExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/stats/export")
            .WithTags("StatisticsExport")
            .RequireAuthorization();

        // 导出统计概览
        group.MapGet("overview", ExportOverview)
            .WithName("ExportStatisticsOverview")
            .WithSummary("导出统计概览");

        // 导出Top问题域
        group.MapGet("top-domains", ExportTopDomains)
            .WithName("ExportTopDomains")
            .WithSummary("导出Top问题域统计");

        // 导出闭环时间分布
        group.MapGet("closure-time", ExportClosureTimeDistribution)
            .WithName("ExportClosureTimeDistribution")
            .WithSummary("导出闭环时间分布");

        // 导出状态统计
        group.MapGet("status", ExportStatusStatistics)
            .WithName("ExportStatusStatistics")
            .WithSummary("导出状态统计");

        // 导出趋势数据
        group.MapGet("trends", ExportTrendData)
            .WithName("ExportTrendData")
            .WithSummary("导出趋势数据");

        // 导出完整报告
        group.MapGet("full-report", ExportFullReport)
            .WithName("ExportFullStatisticsReport")
            .WithSummary("导出完整统计报告");
    }

    /// <summary>
    /// 导出统计概览
    /// </summary>
    private static async Task<IResult> ExportOverview(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IStatisticsExportService service,
        [FromQuery] string format = "csv")
    {
        try
        {
            var data = await service.ExportOverviewAsync(fromDate, toDate, format);
            var fileName = $"统计概览_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(data, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出Top问题域
    /// </summary>
    private static async Task<IResult> ExportTopDomains(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IStatisticsExportService service,
        [FromQuery] int topN = 10,
        [FromQuery] string format = "csv")
    {
        try
        {
            var data = await service.ExportTopDomainsAsync(topN, fromDate, toDate, format);
            var fileName = $"Top问题域统计_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(data, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出闭环时间分布
    /// </summary>
    private static async Task<IResult> ExportClosureTimeDistribution(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IStatisticsExportService service,
        [FromQuery] string format = "csv")
    {
        try
        {
            var data = await service.ExportClosureTimeDistributionAsync(fromDate, toDate, format);
            var fileName = $"闭环时间分布_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(data, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出状态统计
    /// </summary>
    private static async Task<IResult> ExportStatusStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IStatisticsExportService service,
        [FromQuery] string format = "csv")
    {
        try
        {
            var data = await service.ExportStatusStatisticsAsync(fromDate, toDate, format);
            var fileName = $"状态统计_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(data, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出趋势数据
    /// </summary>
    private static async Task<IResult> ExportTrendData(
        [FromQuery] string metricType,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        IStatisticsExportService service,
        [FromQuery] string groupBy = "day",
        [FromQuery] string format = "csv")
    {
        try
        {
            var data = await service.ExportTrendDataAsync(metricType, fromDate, toDate, groupBy, format);
            var fileName = $"趋势数据_{metricType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(data, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出完整报告
    /// </summary>
    private static async Task<IResult> ExportFullReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IStatisticsExportService service,
        [FromQuery] string format = "csv")
    {
        try
        {
            var data = await service.ExportFullReportAsync(fromDate, toDate, format);
            var fileName = $"统计报告_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(data, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

















