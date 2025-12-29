using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 缺失信息补全对话相关端点
/// </summary>
public static class MissingInfoConversationEndpoints
{
    public static void MapMissingInfoConversationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization();

        // 继续对话
        group.MapPost("{ticketId:guid}/conversation/continue", ContinueConversation)
            .WithName("ContinueMissingInfoConversation")
            .WithSummary("继续缺失信息补全对话");

        // 获取对话历史
        group.MapGet("{ticketId:guid}/conversation/history", GetConversationHistory)
            .WithName("GetMissingInfoConversationHistory")
            .WithSummary("获取缺失信息补全对话历史");
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
    /// 继续对话
    /// </summary>
    private static async Task<IResult> ContinueConversation(
        Guid ticketId,
        [FromBody] ContinueConversationRequest request,
        IAIDeepAnalysisService service)
    {
        try
        {
            var result = await service.ContinueConversationAsync(
                ticketId,
                request.UserAnswer,
                request.QuestionId);
            return Results.Ok(result);
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
    /// 获取对话历史
    /// </summary>
    private static async Task<IResult> GetConversationHistory(
        Guid ticketId,
        [FromServices] FieldTicket.Infrastructure.Data.ApplicationDbContext dbContext)
    {
        try
        {
            var histories = await dbContext.MissingInfoConversationHistories
                .Where(h => h.TicketId == ticketId)
                .OrderBy(h => h.RoundNumber)
                .ThenBy(h => h.CreatedAt)
                .Select(h => new
                {
                    h.ConversationId,
                    h.RoundNumber,
                    h.QuestionId,
                    h.Question,
                    h.QuestionType,
                    h.UserAnswer,
                    h.AnsweredAt,
                    h.IsAnswered,
                    h.IsSkipped,
                    h.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(histories);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

/// <summary>
/// 继续对话请求
/// </summary>
public class ContinueConversationRequest
{
    public string UserAnswer { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
}


















