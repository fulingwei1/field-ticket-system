using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 判断卡推荐服务接口
/// </summary>
public interface IJudgementCardRecommendationService
{
    /// <summary>
    /// 推荐判断卡
    /// </summary>
    Task<RecommendJudgementCardsResponse> RecommendJudgementCardsAsync(
        RecommendJudgementCardsRequest request);

    /// <summary>
    /// 根据工单推荐判断卡
    /// </summary>
    Task<RecommendJudgementCardsResponse> RecommendJudgementCardsByTicketAsync(
        Guid ticketId,
        int topK = 5);
}

