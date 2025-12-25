using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 智能阈值相关 API 端点
/// </summary>
public static class SmartThresholdEndpoints
{
    public static void MapSmartThresholdEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/thresholds").WithTags("SmartThreshold").RequireAuthorization();

        // 学习最优阈值
        group.MapPost("/learn-optimal", async (
            [FromBody] LearnOptimalThresholdRequest request,
            HttpContext context,
            ISmartThresholdService smartThresholdService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：需要管理员权限
            if (!await IsAdminAsync(context, userId.Value))
            {
                return Results.Forbid();
            }

            try
            {
                var config = await smartThresholdService.LearnOptimalThresholdAsync(request);
                return Results.Ok(config);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("LearnOptimalThreshold")
        .WithSummary("学习最优阈值")
        .Produces<ThresholdConfigDto>();

        // 获取场景阈值
        group.MapGet("/scenario", async (
            [FromQuery] Guid ticketId,
            HttpContext context,
            ISmartThresholdService smartThresholdService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var config = await smartThresholdService.GetScenarioThresholdAsync(ticketId);
                if (config == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(config);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("GetScenarioThreshold")
        .WithSummary("获取场景阈值")
        .Produces<ThresholdConfigDto>();

        // 评估阈值效果
        group.MapGet("/{configId:guid}/evaluate", async (
            Guid configId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            HttpContext context,
            ISmartThresholdService smartThresholdService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：需要管理员权限
            if (!await IsAdminAsync(context, userId.Value))
            {
                return Results.Forbid();
            }

            try
            {
                var evaluation = await smartThresholdService.EvaluateThresholdAsync(configId, fromDate, toDate);
                return Results.Ok(evaluation);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("EvaluateThreshold")
        .WithSummary("评估阈值效果")
        .Produces<ThresholdEvaluation>();

        // 自动优化阈值
        group.MapPost("/{configId:guid}/auto-optimize", async (
            Guid configId,
            [FromBody] AutoOptimizeThresholdRequest request,
            HttpContext context,
            ISmartThresholdService smartThresholdService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：需要管理员权限
            if (!await IsAdminAsync(context, userId.Value))
            {
                return Results.Forbid();
            }

            if (request.ConfigId != configId)
            {
                return Results.BadRequest("ConfigId mismatch");
            }

            try
            {
                var config = await smartThresholdService.AutoOptimizeThresholdAsync(request);
                return Results.Ok(config);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("AutoOptimizeThreshold")
        .WithSummary("自动优化阈值")
        .Produces<ThresholdConfigDto>();

        // 获取阈值配置列表
        group.MapGet("", async (
            [FromQuery] string? scenarioType,
            [FromQuery] bool? isActive,
            HttpContext context,
            ISmartThresholdService smartThresholdService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：需要管理员权限
            if (!await IsAdminAsync(context, userId.Value))
            {
                return Results.Forbid();
            }

            try
            {
                var configs = await smartThresholdService.GetThresholdConfigsAsync(scenarioType, isActive);
                return Results.Ok(configs);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("GetThresholdConfigs")
        .WithSummary("获取阈值配置列表")
        .Produces<List<ThresholdConfigDto>>();
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

    private static async Task<bool> IsAdminAsync(HttpContext context, Guid userId)
    {
        var dbContext = context.RequestServices.GetRequiredService<FieldTicket.Infrastructure.Data.ApplicationDbContext>();
        var user = await dbContext.Users.FindAsync(userId);
        return user?.Role == "Admin" || user?.Role == "Manager";
    }
}

