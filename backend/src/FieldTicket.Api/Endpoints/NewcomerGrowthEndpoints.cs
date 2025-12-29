using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 新人成长曲线相关端点
/// </summary>
public static class NewcomerGrowthEndpoints
{
    public static void MapNewcomerGrowthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/newcomer-growth")
            .WithTags("NewcomerGrowth")
            .RequireAuthorization();

        // 获取个人成长曲线
        group.MapGet("/personal", GetPersonalGrowthCurve)
            .WithName("GetPersonalGrowthCurve")
            .WithSummary("获取个人成长曲线");

        // 获取团队平均成长曲线（用于对比）
        group.MapGet("/team-average", GetTeamAverageGrowthCurve)
            .WithName("GetTeamAverageGrowthCurve")
            .WithSummary("获取团队平均成长曲线（用于对比）");

        // 获取成长里程碑
        group.MapGet("/milestones", GetGrowthMilestones)
            .WithName("GetGrowthMilestones")
            .WithSummary("获取成长里程碑");

        // 计算成长指标
        group.MapGet("/metrics", CalculateGrowthMetrics)
            .WithName("CalculateGrowthMetrics")
            .WithSummary("计算成长指标");
    }

    /// <summary>
    /// 获取个人成长曲线
    /// </summary>
    private static async Task<IResult> GetPersonalGrowthCurve(
        [FromQuery] Guid? engineerId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        HttpContext context,
        INewcomerGrowthService service)
    {
        try
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var targetEngineerId = engineerId ?? userId.Value;
            // TODO: 验证权限（只能查看自己的，除非是主管）

            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var curve = await service.GetPersonalGrowthCurveAsync(targetEngineerId, start, end);
            return Results.Ok(curve);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取团队平均成长曲线
    /// </summary>
    private static async Task<IResult> GetTeamAverageGrowthCurve(
        [FromQuery] Guid? teamId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        HttpContext context,
        INewcomerGrowthService service)
    {
        try
        {
            // TODO: 验证用户是否有权限查看团队数据（仅主管可见）
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var curve = await service.GetTeamAverageGrowthCurveAsync(teamId, start, end);
            return Results.Ok(curve);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取成长里程碑
    /// </summary>
    private static async Task<IResult> GetGrowthMilestones(
        [FromQuery] Guid? engineerId,
        HttpContext context,
        INewcomerGrowthService service)
    {
        try
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var targetEngineerId = engineerId ?? userId.Value;
            // TODO: 验证权限

            var milestones = await service.GetGrowthMilestonesAsync(targetEngineerId);
            return Results.Ok(milestones);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 计算成长指标
    /// </summary>
    private static async Task<IResult> CalculateGrowthMetrics(
        [FromQuery] Guid? engineerId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        HttpContext context,
        INewcomerGrowthService service)
    {
        try
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var targetEngineerId = engineerId ?? userId.Value;
            // TODO: 验证权限

            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var metrics = await service.CalculateGrowthMetricsAsync(targetEngineerId, start, end);
            return Results.Ok(metrics);
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

















