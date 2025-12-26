using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工程师负载统计相关端点
/// </summary>
public static class EngineerLoadStatEndpoints
{
    public static void MapEngineerLoadStatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engineer-load-stats")
            .WithTags("EngineerLoadStats")
            .RequireAuthorization();

        // 获取个人负载报告（仅本人可见）
        group.MapGet("/personal", GetPersonalLoadReport)
            .WithName("GetPersonalLoadReport")
            .WithSummary("获取个人负载报告（仅本人可见）");

        // 获取团队负载分布（仅主管可见）
        group.MapGet("/team", GetTeamLoadDistribution)
            .WithName("GetTeamLoadDistribution")
            .WithSummary("获取团队负载分布（仅主管可见）");

        // 获取负载趋势
        group.MapGet("/trend", GetLoadTrend)
            .WithName("GetLoadTrend")
            .WithSummary("获取负载趋势分析");

        // 计算并更新统计（后台任务调用）
        group.MapPost("/calculate", CalculateStats)
            .WithName("CalculateEngineerLoadStats")
            .WithSummary("计算并更新工程师负载统计");
    }

    /// <summary>
    /// 获取个人负载报告
    /// </summary>
    private static async Task<IResult> GetPersonalLoadReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        HttpContext context,
        IEngineerLoadStatService service)
    {
        try
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var report = await service.GetPersonalLoadReportAsync(userId.Value, start, end);
            return Results.Ok(report);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取团队负载分布
    /// </summary>
    private static async Task<IResult> GetTeamLoadDistribution(
        [FromQuery] Guid? teamId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        HttpContext context,
        IEngineerLoadStatService service)
    {
        try
        {
            // TODO: 验证用户是否有权限查看团队负载（仅主管可见）
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var distribution = await service.GetTeamLoadDistributionAsync(teamId, start, end);
            return Results.Ok(distribution);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取负载趋势
    /// </summary>
    private static async Task<IResult> GetLoadTrend(
        [FromQuery] Guid? engineerId,
        HttpContext context,
        IEngineerLoadStatService service,
        [FromQuery] string periodType = "daily",
        [FromQuery] int periods = 30)
    {
        try
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 只能查看自己的趋势，除非是主管
            var targetEngineerId = engineerId ?? userId.Value;
            // TODO: 验证权限（主管可以查看其他人的趋势）

            var trend = await service.GetLoadTrendAsync(targetEngineerId, periodType, periods);
            return Results.Ok(trend);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 计算并更新统计
    /// </summary>
    private static async Task<IResult> CalculateStats(
        [FromQuery] Guid? engineerId,
        [FromQuery] DateOnly? statDate,
        HttpContext context,
        IEngineerLoadStatService service)
    {
        try
        {
            // TODO: 验证用户是否有权限（仅后台任务或管理员）
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var date = statDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            
            if (engineerId.HasValue)
            {
                var stat = await service.CalculateAndUpdateStatsAsync(engineerId.Value, date);
                return Results.Ok(stat);
            }
            else
            {
                var count = await service.CalculateAllEngineersStatsAsync(date);
                return Results.Ok(new { count, message = $"已计算 {count} 个工程师的负载统计" });
            }
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("user_id");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}







