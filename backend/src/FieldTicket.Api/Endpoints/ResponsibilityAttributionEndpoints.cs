using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 责任归因相关端点
/// </summary>
public static class ResponsibilityAttributionEndpoints
{
    public static void MapResponsibilityAttributionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("ResponsibilityAttribution")
            .RequireAuthorization();

        // 归因工单
        group.MapPost("{ticketId:guid}/attribute-responsibility", AttributeResponsibility)
            .WithName("AttributeResponsibility")
            .WithSummary("归因工单问题");

        // 获取工单归因信息
        group.MapGet("{ticketId:guid}/attribution", GetAttribution)
            .WithName("GetTicketAttribution")
            .WithSummary("获取工单的责任归因信息");

        // 责任归因统计
        var statsGroup = app.MapGroup("/api/statistics")
            .WithTags("Statistics")
            .RequireAuthorization();

        statsGroup.MapGet("responsibility", GetResponsibilityStatistics)
            .WithName("GetResponsibilityStatistics")
            .WithSummary("获取责任归因统计");
    }

    private static Guid GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("用户未认证");
        }
        return userId;
    }

    /// <summary>
    /// 归因工单
    /// </summary>
    private static async Task<IResult> AttributeResponsibility(
        Guid ticketId,
        [FromBody] AttributeResponsibilityRequest request,
        HttpContext context,
        IResponsibilityAttributionService service)
    {
        try
        {
            var userId = GetUserId(context);
            await service.AttributeResponsibilityAsync(
                ticketId,
                request.RootResponsibility,
                request.IsPreventable,
                request.Notes,
                userId);

            return Results.Ok(new { message = "责任归因成功" });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取工单归因信息
    /// </summary>
    private static async Task<IResult> GetAttribution(
        Guid ticketId,
        IResponsibilityAttributionService service)
    {
        try
        {
            var attribution = await service.GetAttributionAsync(ticketId);
            if (attribution == null)
            {
                return Results.NotFound(new { message = $"工单 {ticketId} 未找到或未归因" });
            }
            return Results.Ok(attribution);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取责任归因统计
    /// </summary>
    private static async Task<IResult> GetResponsibilityStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IResponsibilityAttributionService service)
    {
        try
        {
            var statistics = await service.GetStatisticsAsync(fromDate, toDate);
            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

/// <summary>
/// 归因请求
/// </summary>
public class AttributeResponsibilityRequest
{
    public string RootResponsibility { get; set; } = string.Empty;
    public bool IsPreventable { get; set; }
    public string? Notes { get; set; }
}








