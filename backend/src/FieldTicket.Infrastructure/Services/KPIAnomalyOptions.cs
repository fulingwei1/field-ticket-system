namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// KPI 异常检测阈值配置
/// </summary>
public class KPIAnomalyOptions
{
    /// <summary>
    /// 高置信度阈值（默认 4.5）
    /// </summary>
    public decimal HighConfidenceThreshold { get; set; } = 4.5m;

    /// <summary>
    /// 可疑解决率阈值（默认 0.9）
    /// </summary>
    public decimal SuspiciousResolutionRateThreshold { get; set; } = 0.9m;

    /// <summary>
    /// 重复问题率阈值 - 高风险（默认 0.2）
    /// </summary>
    public decimal HighRepeatProblemRateThreshold { get; set; } = 0.2m;

    /// <summary>
    /// 重复问题率阈值 - 中等风险（默认 0.15）
    /// </summary>
    public decimal MediumRepeatProblemRateThreshold { get; set; } = 0.15m;

    /// <summary>
    /// 快速结案时间阈值（小时，默认 24）
    /// </summary>
    public decimal FastClosureTimeHours { get; set; } = 24m;

    /// <summary>
    /// 最小分诊次数（用于置信度分布检测，默认 10）
    /// </summary>
    public int MinTriageCountForDistributionCheck { get; set; } = 10;

    /// <summary>
    /// 高置信度比例异常阈值（默认 0.95）
    /// </summary>
    public decimal HighConfidenceRateAnomalyThreshold { get; set; } = 0.95m;
}
