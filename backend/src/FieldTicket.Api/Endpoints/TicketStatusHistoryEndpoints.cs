using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单状态历史相关端点
/// </summary>
public static class TicketStatusHistoryEndpoints
{
    public static void MapTicketStatusHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets/{ticketId}/status-history")
            .WithTags("TicketStatusHistory")
            .RequireAuthorization();

        // 获取状态历史
        group.MapGet("", GetStatusHistory)
            .WithName("GetStatusHistory")
            .WithSummary("获取工单状态历史");

        // 获取状态流转路径（可视化用）
        group.MapGet("/flow", GetStatusFlow)
            .WithName("GetStatusFlow")
            .WithSummary("获取工单状态流转路径（可视化）");
    }

    /// <summary>
    /// 获取状态历史
    /// </summary>
    private static async Task<IResult> GetStatusHistory(
        Guid ticketId,
        ITicketStatusHistoryService service)
    {
        try
        {
            var history = await service.GetStatusHistoryAsync(ticketId);
            return Results.Ok(new { history });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取状态流转路径
    /// </summary>
    private static async Task<IResult> GetStatusFlow(
        Guid ticketId,
        ITicketStatusHistoryService service)
    {
        try
        {
            var flow = await service.GetStatusFlowAsync(ticketId);
            return Results.Ok(flow);
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



















