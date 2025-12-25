namespace FieldTicket.Domain.Entities;

/// <summary>
/// 绩效指标实体（按周期统计）
/// </summary>
public class PerformanceMetrics
{
    public Guid MetricId { get; set; }
    
    // 关联信息
    public Guid EngineerId { get; set; }
    public string PeriodType { get; set; } = string.Empty; // 'daily', 'weekly', 'monthly', 'quarterly', 'yearly'
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    // 工单相关指标
    public int TotalTickets { get; set; } = 0;
    public int TicketsResolved { get; set; } = 0;
    public int TicketsPending { get; set; } = 0;
    public TimeSpan? AverageResolutionTime { get; set; }
    public decimal? FirstTimeResolutionRate { get; set; }
    
    // 响应时间指标
    public TimeSpan? AverageResponseTime { get; set; }
    public TimeSpan? ResponseTimeP95 { get; set; }
    public decimal? OnTimeResponseRate { get; set; }
    
    // 设备故障率
    public int DevicesServiced { get; set; } = 0;
    public decimal? DeviceFailureRate { get; set; }
    public decimal? RepeatFailureRate { get; set; }
    
    // 工作活动完整性（从工单处理记录自动生成）
    public int WorkActivityDays { get; set; } = 0; // 有工单处理记录的天数
    public decimal? WorkActivityCompleteness { get; set; } // 工作活动完整度
    
    // 工单创建质量指标
    public decimal? TicketCreationCompleteness { get; set; } // 工单创建完整度
    public decimal? FieldFeedbackTimelinessRate { get; set; } // 现场问题反馈及时率
    public decimal? QuestionReplyTimelinessRate { get; set; } // 追问回复及时性
    
    // 问题解决能力指标
    public decimal? VerificationPassRate { get; set; } // 验证通过率
    public decimal? RepeatProblemRate { get; set; } // 重复问题率
    
    // 技术诊断能力指标
    public decimal? JudgementCardUsageAccuracy { get; set; } // 判断卡使用准确率
    public decimal? JudgementCardHitRate { get; set; } // 判断卡命中率
    public decimal? AiSuggestionAdoptionRate { get; set; } // AI建议采纳率
    public decimal? LowConfidenceUpgradeTimeliness { get; set; } // 低置信度升级及时性
    
    // 知识贡献指标
    public int JudgementCardsCreated { get; set; } = 0; // 判断卡创建数量
    public decimal? JudgementCardQualityScore { get; set; } // 判断卡质量评分
    public decimal? JudgementCardReuseContribution { get; set; } // 判断卡复用贡献
    public int SolutionsContributed { get; set; } = 0; // 解决方案贡献
    
    // 客户服务能力指标
    public decimal? CustomerCommunicationTimeliness { get; set; } // 客户沟通及时性
    public decimal? CustomerCommunicationQuality { get; set; } // 客户沟通质量
    public decimal? CustomerSatisfactionScore { get; set; } // 客户满意度（1-5）
    public int CustomerFeedbackCount { get; set; } = 0;
    
    // 协作能力指标
    public decimal? TeamCollaborationActivity { get; set; } // 团队协作活跃度
    public decimal? KnowledgeSharingContribution { get; set; } // 知识分享贡献
    
    // 工作规范性指标
    public decimal? TicketInformationCompleteness { get; set; } // 工单信息完整性
    public decimal? RootCauseAttributionCompleteness { get; set; } // 责任归因完成度
    
    // 综合评分
    public decimal? OverallScore { get; set; } // 综合评分（0-100）
    public string? PerformanceLevel { get; set; } // 'excellent', 'good', 'average', 'below_average', 'poor'
    
    // 排名
    public int? RankInTeam { get; set; }
    public int? RankInDepartment { get; set; }
    
    // 审计字段
    public DateTime CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public User? Engineer { get; set; }
}


