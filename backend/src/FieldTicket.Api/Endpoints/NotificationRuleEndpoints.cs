using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 通知规则相关端点
/// </summary>
public static class NotificationRuleEndpoints
{
    public static void MapNotificationRuleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notification-rules")
            .WithTags("NotificationRules")
            .RequireAuthorization();

        // 获取通知规则列表
        group.MapGet("", GetNotificationRules)
            .WithName("GetNotificationRules")
            .WithSummary("获取通知规则列表");

        // 创建/更新通知规则
        group.MapPost("", CreateNotificationRule)
            .WithName("CreateNotificationRule")
            .WithSummary("创建通知规则");

        group.MapPut("/{ruleId}", UpdateNotificationRule)
            .WithName("UpdateNotificationRule")
            .WithSummary("更新通知规则");

        // 删除通知规则
        group.MapDelete("/{ruleId}", DeleteNotificationRule)
            .WithName("DeleteNotificationRule")
            .WithSummary("删除通知规则");

        // 获取通知日志
        group.MapGet("/tickets/{ticketId}/logs", GetNotificationLogs)
            .WithName("GetNotificationLogs")
            .WithSummary("获取工单的通知日志");
    }

    /// <summary>
    /// 获取通知规则列表
    /// </summary>
    private static async Task<IResult> GetNotificationRules(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? deviceId,
        [FromQuery] string? triggerEvent,
        INotificationRuleService service)
    {
        try
        {
            var rules = await service.GetNotificationRulesAsync(customerId, projectId, deviceId, triggerEvent);
            return Results.Ok(rules);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 创建通知规则
    /// </summary>
    private static async Task<IResult> CreateNotificationRule(
        [FromBody] SaveNotificationRuleRequest request,
        INotificationRuleService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }
            var rule = await service.SaveNotificationRuleAsync(request, userId.Value);
            return Results.Created($"/api/notification-rules/{rule.RuleId}", rule);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 更新通知规则
    /// </summary>
    private static async Task<IResult> UpdateNotificationRule(
        Guid ruleId,
        [FromBody] SaveNotificationRuleRequest request,
        INotificationRuleService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }
            request.RuleId = ruleId;
            var rule = await service.SaveNotificationRuleAsync(request, userId.Value);
            return Results.Ok(rule);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 删除通知规则
    /// </summary>
    private static async Task<IResult> DeleteNotificationRule(
        Guid ruleId,
        INotificationRuleService service)
    {
        try
        {
            await service.DeleteNotificationRuleAsync(ruleId);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取通知日志
    /// </summary>
    private static async Task<IResult> GetNotificationLogs(
        Guid ticketId,
        ApplicationDbContext dbContext)
    {
        try
        {
            var logs = await dbContext.NotificationLogs
                .Where(l => l.TicketId == ticketId)
                .OrderByDescending(l => l.SentAt)
                .ToListAsync();

            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}

