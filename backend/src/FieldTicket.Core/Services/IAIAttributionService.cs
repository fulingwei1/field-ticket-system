using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// AI辅助归因服务接口
/// </summary>
public interface IAIAttributionService
{
    /// <summary>
    /// 生成归因建议
    /// </summary>
    Task<AttributionSuggestionDto> SuggestAttributionAsync(Guid ticketId);

    /// <summary>
    /// 检查归因一致性
    /// </summary>
    Task<ConsistencyCheckResultDto> CheckConsistencyAsync(
        Guid ticketId,
        string rootResponsibility,
        bool? isPreventable);

    /// <summary>
    /// 评估归因效果
    /// </summary>
    Task<AttributionEvaluationDto> EvaluateAttributionAsync(
        DateTime? fromDate,
        DateTime? toDate);

    /// <summary>
    /// 获取归因统计
    /// </summary>
    Task<AttributionStatisticsDto> GetAttributionStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate);
}

