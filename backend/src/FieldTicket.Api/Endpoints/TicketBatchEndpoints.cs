using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单批量操作相关端点
/// </summary>
public static class TicketBatchEndpoints
{
    public static void MapTicketBatchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets/batch")
            .WithTags("TicketBatch")
            .RequireAuthorization();

        // 批量更新状态
        group.MapPost("update-status", BatchUpdateStatus)
            .WithName("BatchUpdateStatus")
            .WithSummary("批量更新工单状态");

        // 批量分配工程师
        group.MapPost("assign-engineer", BatchAssignEngineer)
            .WithName("BatchAssignEngineer")
            .WithSummary("批量分配工程师");

        // 批量删除
        group.MapPost("delete", BatchDelete)
            .WithName("BatchDelete")
            .WithSummary("批量删除工单");

        // 批量更新优先级
        group.MapPost("update-priority", BatchUpdatePriority)
            .WithName("BatchUpdatePriority")
            .WithSummary("批量更新优先级");

        // 批量标记
        group.MapPost("tag", BatchTag)
            .WithName("BatchTag")
            .WithSummary("批量标记");
    }

    /// <summary>
    /// 批量更新状态
    /// </summary>
    private static async Task<IResult> BatchUpdateStatus(
        [FromBody] BatchUpdateStatusRequest request,
        ITicketBatchService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await service.BatchUpdateStatusAsync(
                request.TicketIds,
                request.NewStatus,
                request.Reason,
                userId.Value);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 批量分配工程师
    /// </summary>
    private static async Task<IResult> BatchAssignEngineer(
        [FromBody] BatchAssignEngineerRequest request,
        ITicketBatchService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await service.BatchAssignEngineerAsync(
                request.TicketIds,
                request.EngineerId,
                userId.Value);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    private static async Task<IResult> BatchDelete(
        [FromBody] BatchDeleteRequest request,
        ITicketBatchService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await service.BatchDeleteAsync(
                request.TicketIds,
                userId.Value);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 批量更新优先级
    /// </summary>
    private static async Task<IResult> BatchUpdatePriority(
        [FromBody] BatchUpdatePriorityRequest request,
        ITicketBatchService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await service.BatchUpdatePriorityAsync(
                request.TicketIds,
                request.Priority,
                userId.Value);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 批量标记
    /// </summary>
    private static async Task<IResult> BatchTag(
        [FromBody] BatchTagRequest request,
        ITicketBatchService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var result = await service.BatchTagAsync(
                request.TicketIds,
                request.Tags,
                userId.Value);

            return Results.Ok(result);
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
/// 批量更新状态请求
/// </summary>
public class BatchUpdateStatusRequest
{
    public List<Guid> TicketIds { get; set; } = new();
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// 批量分配工程师请求
/// </summary>
public class BatchAssignEngineerRequest
{
    public List<Guid> TicketIds { get; set; } = new();
    public Guid EngineerId { get; set; }
}

/// <summary>
/// 批量删除请求
/// </summary>
public class BatchDeleteRequest
{
    public List<Guid> TicketIds { get; set; } = new();
}

/// <summary>
/// 批量更新优先级请求
/// </summary>
public class BatchUpdatePriorityRequest
{
    public List<Guid> TicketIds { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
/// 批量标记请求
/// </summary>
public class BatchTagRequest
{
    public List<Guid> TicketIds { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}









