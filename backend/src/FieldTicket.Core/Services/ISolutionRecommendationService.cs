using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 解决方案推荐服务接口
/// </summary>
public interface ISolutionRecommendationService
{
    /// <summary>
    /// 为工单推荐解决方案
    /// </summary>
    /// <param name="request">推荐请求</param>
    /// <returns>推荐结果列表</returns>
    Task<RecommendSolutionsResponse> RecommendSolutionsAsync(RecommendSolutionsRequest request);

    /// <summary>
    /// 计算两个工单的相似度
    /// </summary>
    /// <param name="ticketId1">工单ID1</param>
    /// <param name="ticketId2">工单ID2</param>
    /// <returns>相似度分数（0.0-1.0）</returns>
    Task<double> CalculateTicketSimilarityAsync(Guid ticketId1, Guid ticketId2);

    /// <summary>
    /// 获取解决方案的统计信息
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>统计信息</returns>
    Task<SolutionStatistics> GetSolutionStatisticsAsync(Guid solutionId);

    /// <summary>
    /// 检查解决方案与工单的版本兼容性
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <param name="ticketId">工单ID</param>
    /// <returns>是否兼容及兼容性消息</returns>
    Task<(bool IsCompatible, string? Message)> CheckVersionCompatibilityAsync(Guid solutionId, Guid ticketId);
}
