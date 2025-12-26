namespace FieldTicket.Core.Services;

/// <summary>
/// 工程师负载统计服务接口
/// </summary>
public interface IEngineerLoadStatService
{
    /// <summary>
    /// 计算并更新工程师负载统计
    /// </summary>
    Task<EngineerLoadStatDto> CalculateAndUpdateStatsAsync(
        Guid engineerId,
        DateOnly statDate);

    /// <summary>
    /// 获取工程师个人负载报告（仅本人可见）
    /// </summary>
    Task<EngineerLoadReportDto> GetPersonalLoadReportAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// 获取团队负载分布（仅主管可见）
    /// </summary>
    Task<TeamLoadDistributionDto> GetTeamLoadDistributionAsync(
        Guid? teamId,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// 获取负载趋势分析
    /// </summary>
    Task<List<LoadTrendDto>> GetLoadTrendAsync(
        Guid engineerId,
        string periodType, // daily, weekly, monthly
        int periods);

    /// <summary>
    /// 批量计算所有工程师的负载统计
    /// </summary>
    Task<int> CalculateAllEngineersStatsAsync(DateOnly statDate);
}

/// <summary>
/// 工程师负载统计DTO
/// </summary>
public class EngineerLoadStatDto
{
    public Guid StatId { get; set; }
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public DateOnly StatDate { get; set; }
    public int MentionedCount { get; set; }
    public int EscalationTakenCount { get; set; }
    public int JudgementReusedCount { get; set; }
    public int LowConfidenceTakenCount { get; set; }
    public int TicketsAssigned { get; set; }
    public int TicketsClosed { get; set; }
    public decimal TotalLoadScore { get; set; } // 综合负载分数
}

/// <summary>
/// 工程师负载报告DTO
/// </summary>
public class EngineerLoadReportDto
{
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public EngineerLoadStatDto TotalStats { get; set; } = null!;
    public List<EngineerLoadStatDto> DailyStats { get; set; } = new();
    public LoadAnalysisDto Analysis { get; set; } = null!;
}

/// <summary>
/// 团队负载分布DTO
/// </summary>
public class TeamLoadDistributionDto
{
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<EngineerLoadStatDto> EngineerStats { get; set; } = new();
    public LoadDistributionAnalysisDto DistributionAnalysis { get; set; } = null!;
}

/// <summary>
/// 负载趋势DTO
/// </summary>
public class LoadTrendDto
{
    public string Period { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal TotalLoadScore { get; set; }
    public int MentionedCount { get; set; }
    public int EscalationTakenCount { get; set; }
    public int JudgementReusedCount { get; set; }
    public int TicketsAssigned { get; set; }
    public int TicketsClosed { get; set; }
}

/// <summary>
/// 负载分析DTO
/// </summary>
public class LoadAnalysisDto
{
    public string LoadLevel { get; set; } = string.Empty; // low, normal, high, very_high
    public decimal LoadScore { get; set; }
    public string? Assessment { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 负载分布分析DTO
/// </summary>
public class LoadDistributionAnalysisDto
{
    public decimal AverageLoadScore { get; set; }
    public decimal LoadBalanceScore { get; set; } // 负载均衡分数（0-100）
    public List<string> Recommendations { get; set; } = new();
}









