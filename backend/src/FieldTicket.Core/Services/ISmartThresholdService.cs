using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 智能阈值服务接口
/// </summary>
public interface ISmartThresholdService
{
    /// <summary>
    /// 学习最优阈值
    /// </summary>
    Task<ThresholdConfigDto> LearnOptimalThresholdAsync(LearnOptimalThresholdRequest request);

    /// <summary>
    /// 获取场景阈值
    /// </summary>
    Task<ThresholdConfigDto?> GetScenarioThresholdAsync(Guid ticketId);

    /// <summary>
    /// 评估阈值效果
    /// </summary>
    Task<ThresholdEvaluation> EvaluateThresholdAsync(
        Guid configId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// 自动优化阈值
    /// </summary>
    Task<ThresholdConfigDto> AutoOptimizeThresholdAsync(AutoOptimizeThresholdRequest request);

    /// <summary>
    /// 获取阈值配置列表
    /// </summary>
    Task<List<ThresholdConfigDto>> GetThresholdConfigsAsync(
        string? scenarioType = null,
        bool? isActive = null);
}

