using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 分诊相关端点
/// </summary>
public static class TriageEndpoints
{
    public static void MapTriageEndpoints(this WebApplication app)
    {
        // 分诊工单（在工单端点下）
        app.MapPost("/api/tickets/{ticketId}/triage", TriageTicket)
            .WithName("TriageTicket")
            .WithSummary("分诊工单")
            .WithTags("Triage")
            .RequireAuthorization("SeniorEngineer");

        // 获取判断卡列表
        app.MapGet("/api/judgement-cards", GetJudgementCards)
            .WithName("GetJudgementCards")
            .WithSummary("获取判断卡列表")
            .WithTags("Triage")
            .RequireAuthorization();

        // 获取判断卡详情
        app.MapGet("/api/judgement-cards/{jcCode}", GetJudgementCard)
            .WithName("GetJudgementCard")
            .WithSummary("获取判断卡详情")
            .WithTags("Triage")
            .RequireAuthorization();
    }

    /// <summary>
    /// 分诊工单
    /// </summary>
    private static async Task<IResult> TriageTicket(
        Guid ticketId,
        [FromBody] TriageTicketRequest request,
        ITriageService triageService,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            var result = await triageService.TriageTicketAsync(ticketId, request, userId);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
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
    /// 获取判断卡列表
    /// </summary>
    private static async Task<IResult> GetJudgementCards(
        [FromQuery] char? domain,
        [FromQuery] string? status,
        ITriageService triageService)
    {
        try
        {
            var cards = await triageService.GetJudgementCardsAsync(domain, status);
            return Results.Ok(cards);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取判断卡详情
    /// </summary>
    private static async Task<IResult> GetJudgementCard(
        string jcCode,
        ITriageService triageService)
    {
        try
        {
            var card = await triageService.GetJudgementCardAsync(jcCode);
            if (card == null)
            {
                return Results.NotFound(new { message = $"判断卡 {jcCode} 不存在" });
            }
            return Results.Ok(card);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value 
            ?? httpContext.User.FindFirst("user_id")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("无法获取用户ID");
        }

        return userId;
    }
}

