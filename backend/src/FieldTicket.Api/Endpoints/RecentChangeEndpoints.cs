using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 最近变更关联相关端点
/// </summary>
public static class RecentChangeEndpoints
{
    public static void MapRecentChangeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recent-changes")
            .WithTags("RecentChanges")
            .RequireAuthorization();

        // 获取工单的相关变更
        group.MapGet("/tickets/{ticketId}", GetTicketRelatedChanges)
            .WithName("GetTicketRelatedChanges")
            .WithSummary("获取工单相关的最近变更");

        // 获取设备的变更历史
        group.MapGet("/devices/{deviceId}", GetDeviceChanges)
            .WithName("GetDeviceChanges")
            .WithSummary("获取设备的变更历史");

        // 记录设备变更
        group.MapPost("/devices", RecordDeviceChange)
            .WithName("RecordDeviceChange")
            .WithSummary("记录设备变更");
    }

    /// <summary>
    /// 获取工单相关的最近变更
    /// </summary>
    private static async Task<IResult> GetTicketRelatedChanges(
        Guid ticketId,
        [FromQuery] int? daysBefore,
        [FromQuery] int? daysAfter,
        IRecentChangeAssociationService service)
    {
        try
        {
            var changes = await service.GetRelatedChangesAsync(
                ticketId,
                daysBefore ?? 7,
                daysAfter ?? 7);
            return Results.Ok(changes);
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
    /// 获取设备的变更历史
    /// </summary>
    private static async Task<IResult> GetDeviceChanges(
        Guid deviceId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IRecentChangeAssociationService service)
    {
        try
        {
            var changes = await service.GetDeviceChangesAsync(deviceId, fromDate, toDate);
            return Results.Ok(changes);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 记录设备变更
    /// </summary>
    private static async Task<IResult> RecordDeviceChange(
        [FromBody] CreateDeviceChangeRequest request,
        IRecentChangeAssociationService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var change = await service.RecordChangeAsync(request, userId.Value);
            return Results.Created($"/api/recent-changes/devices/{request.DeviceId}", change);
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

