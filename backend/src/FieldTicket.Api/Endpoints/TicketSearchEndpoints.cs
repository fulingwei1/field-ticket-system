using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单搜索相关端点
/// </summary>
public static class TicketSearchEndpoints
{
    public static void MapTicketSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets/search")
            .WithTags("TicketSearch")
            .RequireAuthorization();

        // 搜索工单
        group.MapGet("", SearchTickets)
            .WithName("SearchTickets")
            .WithSummary("全文搜索工单");

        // 获取搜索建议
        group.MapGet("suggestions", GetSearchSuggestions)
            .WithName("GetSearchSuggestions")
            .WithSummary("获取搜索建议（自动完成）");
    }

    /// <summary>
    /// 搜索工单
    /// </summary>
    private static async Task<IResult> SearchTickets(
        [FromQuery] string? q,
        [FromQuery] string? searchField,
        [FromQuery] string? statuses,
        [FromQuery] string? domain,
        [FromQuery] string? priority,
        [FromQuery] Guid? customerId,
        [FromQuery] string? deviceSn,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        HttpContext context,
        ITicketSearchService service,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId(context);
            var userRole = GetUserRole(context);

            var request = new SearchRequest
            {
                Query = q ?? string.Empty,
                SearchField = searchField,
                Statuses = string.IsNullOrEmpty(statuses) ? null : statuses.Split(',').ToList(),
                Domain = domain,
                Priority = priority,
                CustomerId = customerId,
                DeviceSn = deviceSn,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = pageSize
            };

            // 根据角色过滤搜索结果
            // FieldEngineer 只能搜索自己创建的工单
            if (userRole == "FieldEngineer" && userId.HasValue)
            {
                request.CreatedBy = userId.Value;
            }

            var result = await service.SearchTicketsAsync(request);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    private static async Task<IResult> GetSearchSuggestions(
        [FromQuery] string q,
        ITicketSearchService service,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.Ok(new List<string>());
            }

            var suggestions = await service.GetSearchSuggestionsAsync(q, limit);
            return Results.Ok(suggestions);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }

    private static string? GetUserRole(HttpContext context)
    {
        var roleClaim = context.User.FindFirst(ClaimTypes.Role);
        return roleClaim?.Value;
    }
}







