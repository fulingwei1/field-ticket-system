using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单关联分析相关端点
/// </summary>
public static class TicketAssociationEndpoints
{
    public static void MapTicketAssociationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets/{ticketId}/associations")
            .WithTags("TicketAssociation")
            .RequireAuthorization();

        // 获取所有关联工单
        group.MapGet("", GetAllAssociations)
            .WithName("GetAllAssociations")
            .WithSummary("获取工单的所有关联工单");

        // 获取相似工单
        group.MapGet("/similar", GetSimilarTickets)
            .WithName("GetSimilarTickets")
            .WithSummary("获取相似工单");

        // 获取设备关联工单
        group.MapGet("/device", GetDeviceRelatedTickets)
            .WithName("GetDeviceRelatedTickets")
            .WithSummary("获取设备关联工单");

        // 获取客户关联工单
        group.MapGet("/customer", GetCustomerRelatedTickets)
            .WithName("GetCustomerRelatedTickets")
            .WithSummary("获取客户关联工单");

        // 获取问题域关联工单
        group.MapGet("/domain", GetDomainRelatedTickets)
            .WithName("GetDomainRelatedTickets")
            .WithSummary("获取问题域关联工单");
    }

    /// <summary>
    /// 获取所有关联工单
    /// </summary>
    private static async Task<IResult> GetAllAssociations(
        Guid ticketId,
        ITicketAssociationService service)
    {
        try
        {
            var associations = await service.GetAssociationsAsync(ticketId);
            return Results.Ok(associations);
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
    /// 获取相似工单
    /// </summary>
    private static async Task<IResult> GetSimilarTickets(
        Guid ticketId,
        ITicketAssociationService service = null!,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            var tickets = await service.GetSimilarTicketsAsync(ticketId, maxResults);
            return Results.Ok(tickets);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取设备关联工单
    /// </summary>
    private static async Task<IResult> GetDeviceRelatedTickets(
        Guid ticketId,
        ITicketAssociationService service = null!,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            var tickets = await service.GetDeviceRelatedTicketsAsync(ticketId, maxResults);
            return Results.Ok(tickets);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取客户关联工单
    /// </summary>
    private static async Task<IResult> GetCustomerRelatedTickets(
        Guid ticketId,
        ITicketAssociationService service = null!,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            var tickets = await service.GetCustomerRelatedTicketsAsync(ticketId, maxResults);
            return Results.Ok(tickets);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取问题域关联工单
    /// </summary>
    private static async Task<IResult> GetDomainRelatedTickets(
        Guid ticketId,
        ITicketAssociationService service = null!,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            var tickets = await service.GetDomainRelatedTicketsAsync(ticketId, maxResults);
            return Results.Ok(tickets);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}









