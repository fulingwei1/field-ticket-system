using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单去重检测相关端点
/// </summary>
public static class DuplicateDetectionEndpoints
{
    public static void MapDuplicateDetectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        // 检测重复工单
        group.MapPost("{ticketId:guid}/check-duplicates", CheckDuplicates)
            .WithName("CheckDuplicates")
            .WithSummary("检测重复工单");

        // 合并工单
        group.MapPost("{sourceId:guid}/merge-into/{targetId:guid}", MergeTickets)
            .WithName("MergeTickets")
            .WithSummary("合并工单");

        // 获取合并历史
        group.MapGet("{ticketId:guid}/merge-history", GetMergeHistory)
            .WithName("GetMergeHistory")
            .WithSummary("获取合并历史");
    }

    /// <summary>
    /// 检测重复工单
    /// </summary>
    private static async Task<IResult> CheckDuplicates(
        Guid ticketId,
        [FromQuery] int maxResults = 10,
        IDuplicateDetectionService service)
    {
        try
        {
            var candidates = await service.DetectDuplicatesAsync(ticketId, maxResults);
            return Results.Ok(candidates);
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
    /// 合并工单
    /// </summary>
    private static async Task<IResult> MergeTickets(
        Guid sourceId,
        Guid targetId,
        [FromBody] MergeTicketsRequest request,
        IDuplicateDetectionService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await service.MergeTicketsAsync(sourceId, targetId, request.Reason, userId.Value);
            
            if (result.Success)
            {
                return Results.Ok(result);
            }
            else
            {
                return Results.BadRequest(new { message = result.Message });
            }
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取合并历史
    /// </summary>
    private static async Task<IResult> GetMergeHistory(
        Guid ticketId,
        IDuplicateDetectionService service)
    {
        try
        {
            var history = await service.GetMergeHistoryAsync(ticketId);
            return Results.Ok(history);
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

/// <summary>
/// 合并工单请求
/// </summary>
public class MergeTicketsRequest
{
    public string Reason { get; set; } = string.Empty;
}

