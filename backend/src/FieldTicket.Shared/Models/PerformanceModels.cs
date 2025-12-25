namespace FieldTicket.Shared.Models;

/// <summary>
/// 绩效指标DTO
/// </summary>
public class PerformanceMetricsDto
{
    public Guid MetricId { get; set; }
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    // 工单相关指标
    public int TotalTickets { get; set; }
    public int TicketsResolved { get; set; }
    public int TicketsPending { get; set; }
    public TimeSpan? AverageResolutionTime { get; set; }
    public decimal? FirstTimeResolutionRate { get; set; }
    
    // 响应时间指标
    public TimeSpan? AverageResponseTime { get; set; }
    public TimeSpan? ResponseTimeP95 { get; set; }
    public decimal? OnTimeResponseRate { get; set; }
    
    // 设备故障率
    public int DevicesServiced { get; set; }
    public decimal? DeviceFailureRate { get; set; }
    public decimal? RepeatFailureRate { get; set; }
    
    // 工作活动完整性
    public int WorkActivityDays { get; set; }
    public decimal? WorkActivityCompleteness { get; set; }
    
    // 工单创建质量指标
    public decimal? TicketCreationCompleteness { get; set; }
    public decimal? FieldFeedbackTimelinessRate { get; set; }
    public decimal? QuestionReplyTimelinessRate { get; set; }
    
    // 问题解决能力指标
    public decimal? VerificationPassRate { get; set; }
    public decimal? RepeatProblemRate { get; set; }
    
    // 技术诊断能力指标
    public decimal? JudgementCardUsageAccuracy { get; set; }
    public decimal? JudgementCardHitRate { get; set; }
    public decimal? AiSuggestionAdoptionRate { get; set; }
    public decimal? LowConfidenceUpgradeTimeliness { get; set; }
    
    // 知识贡献指标
    public int JudgementCardsCreated { get; set; }
    public decimal? JudgementCardQualityScore { get; set; }
    public decimal? JudgementCardReuseContribution { get; set; }
    public int SolutionsContributed { get; set; }
    
    // 客户服务能力指标
    public decimal? CustomerCommunicationTimeliness { get; set; }
    public decimal? CustomerCommunicationQuality { get; set; }
    public decimal? CustomerSatisfactionScore { get; set; }
    public int CustomerFeedbackCount { get; set; }
    
    // 协作能力指标
    public decimal? TeamCollaborationActivity { get; set; }
    public decimal? KnowledgeSharingContribution { get; set; }
    
    // 工作规范性指标
    public decimal? TicketInformationCompleteness { get; set; }
    public decimal? RootCauseAttributionCompleteness { get; set; }
    
    // 综合评分
    public decimal? OverallScore { get; set; }
    public string? PerformanceLevel { get; set; }
    
    // 排名
    public int? RankInTeam { get; set; }
    public int? RankInDepartment { get; set; }
    
    public DateTime CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 绩效排名DTO
/// </summary>
public class PerformanceRankingDto
{
    public Guid EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public decimal? OverallScore { get; set; }
    public string? PerformanceLevel { get; set; }
    public int Rank { get; set; }
    public int TotalTickets { get; set; }
    public int TicketsResolved { get; set; }
    public decimal? FirstTimeResolutionRate { get; set; }
    public TimeSpan? AverageResolutionTime { get; set; }
}

/// <summary>
/// 绩效趋势DTO
/// </summary>
public class PerformanceTrendDto
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal? OverallScore { get; set; }
    public int TotalTickets { get; set; }
    public int TicketsResolved { get; set; }
    public decimal? FirstTimeResolutionRate { get; set; }
    public TimeSpan? AverageResolutionTime { get; set; }
    public decimal? OnTimeResponseRate { get; set; }
    public int? RankInTeam { get; set; }
    public int? RankInDepartment { get; set; }
}

/// <summary>
/// 计算绩效请求
/// </summary>
public class CalculatePerformanceRequest
{
    public Guid EngineerId { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
}


