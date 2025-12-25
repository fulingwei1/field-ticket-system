namespace FieldTicket.Core.Services;

/// <summary>
/// 新人成长曲线服务接口
/// </summary>
public interface INewcomerGrowthService
{
    /// <summary>
    /// 获取个人成长曲线数据
    /// </summary>
    Task<NewcomerGrowthCurveDto> GetPersonalGrowthCurveAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// 获取团队平均成长曲线（用于对比）
    /// </summary>
    Task<TeamAverageGrowthCurveDto> GetTeamAverageGrowthCurveAsync(
        Guid? teamId,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// 获取成长里程碑
    /// </summary>
    Task<List<GrowthMilestoneDto>> GetGrowthMilestonesAsync(
        Guid engineerId);

    /// <summary>
    /// 计算成长指标
    /// </summary>
    Task<GrowthMetricsDto> CalculateGrowthMetricsAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate);
}

/// <summary>
/// 新人成长曲线DTO
/// </summary>
public class NewcomerGrowthCurveDto
{
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<GrowthDataPointDto> DataPoints { get; set; } = new();
    public GrowthSummaryDto Summary { get; set; } = null!;
}

/// <summary>
/// 成长数据点DTO
/// </summary>
public class GrowthDataPointDto
{
    public DateOnly Date { get; set; }
    public decimal JudgementCardQualityScore { get; set; } // 判断卡质量得分
    public decimal ConfidenceAccuracy { get; set; } // 置信度准确性（confidence vs 实际正确率）
    public decimal AiAdoptionRate { get; set; } // AI建议采纳率
    public decimal FirstTimeResolutionRate { get; set; } // 一次解决率
    public int TicketsHandled { get; set; } // 处理的工单数
    public int JudgementCardsCreated { get; set; } // 创建的判断卡数
}

/// <summary>
/// 成长摘要DTO
/// </summary>
public class GrowthSummaryDto
{
    public decimal AverageJudgementCardQuality { get; set; }
    public decimal AverageConfidenceAccuracy { get; set; }
    public decimal AverageAiAdoptionRate { get; set; }
    public decimal AverageFirstTimeResolutionRate { get; set; }
    public int TotalTicketsHandled { get; set; }
    public int TotalJudgementCardsCreated { get; set; }
    public string GrowthTrend { get; set; } = string.Empty; // improving, stable, declining
}

/// <summary>
/// 团队平均成长曲线DTO
/// </summary>
public class TeamAverageGrowthCurveDto
{
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<GrowthDataPointDto> AverageDataPoints { get; set; } = new();
}

/// <summary>
/// 成长里程碑DTO
/// </summary>
public class GrowthMilestoneDto
{
    public string MilestoneType { get; set; } = string.Empty; // first_ticket, first_judgement_card, quality_threshold, etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly AchievedDate { get; set; }
    public decimal? Score { get; set; }
}

/// <summary>
/// 成长指标DTO
/// </summary>
public class GrowthMetricsDto
{
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal JudgementCardQualityTrend { get; set; } // 趋势斜率
    public decimal ConfidenceAccuracyTrend { get; set; }
    public decimal AiAdoptionTrend { get; set; }
    public decimal FirstTimeResolutionTrend { get; set; }
    public string OverallGrowthAssessment { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
}






