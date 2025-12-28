using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 现场问题自动生成服务接口
/// </summary>
public interface IFieldProblemAutoGenerationService
{
    /// <summary>
    /// 从工单自动生成FieldProblem记录
    /// </summary>
    /// <param name="ticketId">工单ID</param>
    /// <returns>生成结果</returns>
    Task<AutoGenerateProblemResult> GenerateFromTicketAsync(Guid ticketId);

    /// <summary>
    /// 检测是否为重复问题
    /// </summary>
    /// <param name="ticketId">工单ID</param>
    /// <param name="similarityThreshold">相似度阈值（默认0.7）</param>
    /// <returns>是否重复及相关历史问题ID</returns>
    Task<(bool IsRepeat, Guid? RelatedProblemId, double SimilarityScore)> DetectRepeatProblemAsync(
        Guid ticketId,
        double similarityThreshold = 0.7);

    /// <summary>
    /// 获取问题统计信息
    /// </summary>
    /// <param name="request">统计请求</param>
    /// <returns>统计结果</returns>
    Task<ProblemStatisticsResponse> GetStatisticsAsync(ProblemStatisticsRequest request);

    /// <summary>
    /// 获取问题热点（高频问题Top-N）
    /// </summary>
    /// <param name="projectId">项目ID（可选）</param>
    /// <param name="topN">返回前N个</param>
    /// <param name="days">统计最近N天（默认90天）</param>
    /// <returns>热点问题列表</returns>
    Task<List<ProblemHotspotDto>> GetHotspotsAsync(Guid? projectId = null, int topN = 10, int days = 90);

    /// <summary>
    /// 根据工单ID获取关联的问题记录
    /// </summary>
    /// <param name="ticketId">工单ID</param>
    /// <returns>问题记录DTO，如果不存在则返回null</returns>
    Task<FieldProblemDto?> GetProblemByTicketIdAsync(Guid ticketId);
}
