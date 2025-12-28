using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 判断卡推荐相关端点
/// </summary>
public static class JudgementCardRecommendationEndpoints
{
    public static void MapJudgementCardRecommendationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/judgement-cards")
            .WithTags("JudgementCard")
            .RequireAuthorization();

        // 推荐判断卡（根据参数）
        group.MapGet("/recommend", RecommendJudgementCards)
            .WithName("RecommendJudgementCards")
            .WithSummary("推荐判断卡（根据参数）");

        // 根据工单推荐判断卡
        group.MapGet("/recommend/tickets/{ticketId:guid}", RecommendJudgementCardsByTicket)
            .WithName("RecommendJudgementCardsByTicket")
            .WithSummary("根据工单推荐判断卡");
    }

    /// <summary>
    /// 推荐判断卡
    /// </summary>
    private static async Task<IResult> RecommendJudgementCards(
        IJudgementCardRecommendationService service,
        [FromQuery] char? domain,
        [FromQuery] string? stepCode,
        [FromQuery] string? symptomTitle,
        [FromQuery] int topK = 5)
    {
        try
        {
            var request = new RecommendJudgementCardsRequest
            {
                Domain = domain,
                StepCode = stepCode,
                SymptomTitle = symptomTitle,
                TopK = topK
            };

            var response = await service.RecommendJudgementCardsAsync(request);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 根据工单推荐判断卡
    /// </summary>
    private static async Task<IResult> RecommendJudgementCardsByTicket(
        Guid ticketId,
        IJudgementCardRecommendationService service,
        [FromQuery] int topK = 5)
    {
        try
        {
            var response = await service.RecommendJudgementCardsByTicketAsync(ticketId, topK);
            return Results.Ok(response);
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
