using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// KPI反作弊预警相关端点
/// </summary>
public static class KPIAnomalyEndpoints
{
    public static void MapKPIAnomalyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/kpi-anomalies")
            .WithTags("KPIAnomalies")
            .RequireAuthorization();

        // 检测工程师异常
        group.MapGet("/engineers/{engineerId:guid}", DetectEngineerAnomalies)
            .WithName("DetectEngineerAnomalies")
            .WithSummary("检测工程师的KPI异常");

        // 检测团队异常
        group.MapGet("/team", DetectTeamAnomalies)
            .WithName("DetectTeamAnomalies")
            .WithSummary("检测团队的KPI异常");

        // 生成异常报告
        group.MapGet("/report", GenerateAnomalyReport)
            .WithName("GenerateAnomalyReport")
            .WithSummary("生成异常报告");
    }

    /// <summary>
    /// 检测工程师异常
    /// </summary>
    private static async Task<IResult> DetectEngineerAnomalies(
        Guid engineerId,
        IKPIAnomalyService service = null!,
        [FromQuery] int days = 30)
    {
        try
        {
            var anomalies = await service.DetectAnomaliesAsync(engineerId, days);
            return Results.Ok(anomalies);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 检测团队异常
    /// </summary>
    private static async Task<IResult> DetectTeamAnomalies(
        [FromQuery] Guid? teamId,
        IKPIAnomalyService service = null!,
        [FromQuery] int days = 30)
    {
        try
        {
            var reports = await service.DetectTeamAnomaliesAsync(teamId, days);
            return Results.Ok(reports);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 生成异常报告
    /// </summary>
    private static async Task<IResult> GenerateAnomalyReport(
        [FromQuery] Guid? engineerId,
        [FromQuery] Guid? teamId,
        IKPIAnomalyService service = null!,
        [FromQuery] int days = 30)
    {
        try
        {
            var report = await service.GenerateAnomalyReportAsync(engineerId, teamId, days);
            return Results.Ok(report);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}







