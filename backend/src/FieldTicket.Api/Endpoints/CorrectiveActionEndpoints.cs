using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 整改任务相关端点
/// </summary>
public static class CorrectiveActionEndpoints
{
    public static void MapCorrectiveActionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/corrective-actions")
            .WithTags("CorrectiveActions")
            .RequireAuthorization();

        // 检查触发条件
        var ticketGroup = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        ticketGroup.MapPost("{ticketId:guid}/check-corrective-triggers", CheckTriggers)
            .WithName("CheckCorrectiveTriggers")
            .WithSummary("检查整改任务触发条件");

        // 创建整改任务
        group.MapPost("", CreateAction)
            .WithName("CreateCorrectiveAction")
            .WithSummary("创建整改任务");

        // 获取整改任务列表
        group.MapGet("", GetActions)
            .WithName("GetCorrectiveActions")
            .WithSummary("获取整改任务列表");

        // 获取整改任务详情
        group.MapGet("{actionId:guid}", GetAction)
            .WithName("GetCorrectiveAction")
            .WithSummary("获取整改任务详情");

        // 更新整改任务状态
        group.MapPut("{actionId:guid}/status", UpdateActionStatus)
            .WithName("UpdateCorrectiveActionStatus")
            .WithSummary("更新整改任务状态");

        // 更新整改任务
        group.MapPut("{actionId:guid}", UpdateAction)
            .WithName("UpdateCorrectiveAction")
            .WithSummary("更新整改任务");

        // 效果评估
        group.MapPost("{actionId:guid}/evaluate", EvaluateEffectiveness)
            .WithName("EvaluateCorrectiveAction")
            .WithSummary("效果评估");

        // 删除整改任务
        group.MapDelete("{actionId:guid}", DeleteAction)
            .WithName("DeleteCorrectiveAction")
            .WithSummary("删除整改任务");
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
    /// 检查触发条件
    /// </summary>
    private static async Task<IResult> CheckTriggers(
        Guid ticketId,
        ICorrectiveActionService service)
    {
        try
        {
            var triggers = await service.CheckTriggersAsync(ticketId);
            return Results.Ok(triggers);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 创建整改任务
    /// </summary>
    private static async Task<IResult> CreateAction(
        [FromBody] CreateCorrectiveActionRequest request,
        HttpContext context,
        ICorrectiveActionService service)
    {
        try
        {
            var userId = GetUserId(context);
            var actionId = await service.CreateActionAsync(request, userId);
            return Results.Created($"/api/corrective-actions/{actionId}", new { actionId });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取整改任务列表
    /// </summary>
    private static async Task<IResult> GetActions(
        [FromQuery] string? status,
        [FromQuery] Guid? responsiblePersonId,
        [FromQuery] string? rootResponsibility,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] Guid? relatedTicketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        ICorrectiveActionService service)
    {
        try
        {
            var filter = new CorrectiveActionQueryFilter
            {
                Status = status,
                ResponsiblePersonId = responsiblePersonId,
                RootResponsibility = rootResponsibility,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                RelatedTicketId = relatedTicketId
            };

            var (items, total) = await service.GetActionsAsync(filter, page, pageSize);
            return Results.Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取整改任务详情
    /// </summary>
    private static async Task<IResult> GetAction(
        Guid actionId,
        ICorrectiveActionService service)
    {
        try
        {
            var action = await service.GetActionAsync(actionId);
            if (action == null)
            {
                return Results.NotFound(new { message = $"整改任务 {actionId} 不存在" });
            }
            return Results.Ok(action);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 更新整改任务状态
    /// </summary>
    private static async Task<IResult> UpdateActionStatus(
        Guid actionId,
        [FromBody] UpdateActionStatusRequest request,
        HttpContext context,
        ICorrectiveActionService service)
    {
        try
        {
            var userId = GetUserId(context);
            await service.UpdateActionStatusAsync(actionId, request.Status, request.Notes, userId);
            return Results.Ok(new { message = "状态更新成功" });
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
    /// 更新整改任务
    /// </summary>
    private static async Task<IResult> UpdateAction(
        Guid actionId,
        [FromBody] UpdateCorrectiveActionRequest request,
        HttpContext context,
        ICorrectiveActionService service)
    {
        try
        {
            var userId = GetUserId(context);
            await service.UpdateActionAsync(actionId, request, userId);
            return Results.Ok(new { message = "更新成功" });
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
    /// 效果评估
    /// </summary>
    private static async Task<IResult> EvaluateEffectiveness(
        Guid actionId,
        [FromBody] EffectivenessCheckRequest request,
        HttpContext context,
        ICorrectiveActionService service)
    {
        try
        {
            var userId = GetUserId(context);
            await service.EvaluateEffectivenessAsync(actionId, request, userId);
            return Results.Ok(new { message = "效果评估成功" });
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
    /// 删除整改任务
    /// </summary>
    private static async Task<IResult> DeleteAction(
        Guid actionId,
        ICorrectiveActionService service)
    {
        try
        {
            await service.DeleteActionAsync(actionId);
            return Results.Ok(new { message = "删除成功" });
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
}

/// <summary>
/// 更新状态请求
/// </summary>
public class UpdateActionStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}






