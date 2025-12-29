using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// AI分析结果DTO
/// </summary>
public class AiAnalysisResultDto
{
    public Guid AnalysisId { get; set; }
    public string AnalysisType { get; set; } = string.Empty;
    public DateOnly AnalysisDate { get; set; }
    public Guid? EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public JsonDocument? KeyInsights { get; set; }
    public JsonDocument? Suggestions { get; set; }
    public JsonDocument? PerformanceAnalysis { get; set; }
    public string? AiModel { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>
/// 生成每日总结请求
/// </summary>
public class GenerateDailySummaryRequest
{
    public Guid EngineerId { get; set; }
    public DateOnly AnalysisDate { get; set; }
}

/// <summary>
/// 生成每周总结请求
/// </summary>
public class GenerateWeeklySummaryRequest
{
    public Guid EngineerId { get; set; }
    public DateOnly WeekStart { get; set; }
}

/// <summary>
/// 生成团队分析请求
/// </summary>
public class GenerateTeamAnalysisRequest
{
    public Guid? DepartmentId { get; set; }
    public DateOnly AnalysisDate { get; set; }
    public string PeriodType { get; set; } = "monthly";
}

/// <summary>
/// 生成人员安排建议请求
/// </summary>
public class GenerateSchedulingSuggestionRequest
{
    public Guid? DepartmentId { get; set; }
    public DateOnly AnalysisDate { get; set; }
}

/// <summary>
/// 分析技能水平请求
/// </summary>
public class AnalyzeSkillLevelRequest
{
    public Guid EngineerId { get; set; }
    public string PeriodType { get; set; } = "monthly"; // daily/weekly/monthly/quarterly/yearly
    public DateOnly PeriodStart { get; set; }
}

/// <summary>
/// 生成发展建议请求
/// </summary>
public class GenerateDevelopmentSuggestionRequest
{
    public Guid EngineerId { get; set; }
    public string PeriodType { get; set; } = "monthly";
    public DateOnly PeriodStart { get; set; }
}

/// <summary>
/// 生成绩效评价请求
/// </summary>
public class GeneratePerformanceEvaluationRequest
{
    public Guid EngineerId { get; set; }
    public string PeriodType { get; set; } = "monthly";
    public DateOnly PeriodStart { get; set; }
}

/// <summary>
/// 技能水平分析DTO
/// </summary>
public class SkillLevelAnalysisDto
{
    public Guid AnalysisId { get; set; }
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    /// <summary>
    /// 技能水平评估（JSON格式）
    /// </summary>
    public JsonDocument? SkillAssessment { get; set; }
    
    /// <summary>
    /// 技能维度评分
    /// </summary>
    public Dictionary<string, SkillDimensionScore> SkillDimensions { get; set; } = new();
    
    /// <summary>
    /// 总体技能水平：初级/中级/高级/专家
    /// </summary>
    public string OverallSkillLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// 技能水平评分（0-100）
    /// </summary>
    public decimal SkillScore { get; set; }
    
    /// <summary>
    /// AI生成的技能分析总结
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// 关键优势
    /// </summary>
    public List<string> Strengths { get; set; } = new();
    
    /// <summary>
    /// 需要改进的领域
    /// </summary>
    public List<string> ImprovementAreas { get; set; } = new();
    
    public string? AiModel { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

/// <summary>
/// 技能维度评分
/// </summary>
public class SkillDimensionScore
{
    public string DimensionName { get; set; } = string.Empty;
    public decimal Score { get; set; } // 0-100
    public string Level { get; set; } = string.Empty; // 初级/中级/高级/专家
    public string Description { get; set; } = string.Empty;
    public List<string> Evidence { get; set; } = new(); // 支撑证据
}

/// <summary>
/// 发展建议DTO
/// </summary>
public class DevelopmentSuggestionDto
{
    public Guid AnalysisId { get; set; }
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    /// <summary>
    /// 个人发展特点分析
    /// </summary>
    public string DevelopmentCharacteristics { get; set; } = string.Empty;
    
    /// <summary>
    /// 发展建议列表
    /// </summary>
    public List<DevelopmentSuggestionItem> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 短期目标（3个月）
    /// </summary>
    public List<string> ShortTermGoals { get; set; } = new();
    
    /// <summary>
    /// 中期目标（6-12个月）
    /// </summary>
    public List<string> MediumTermGoals { get; set; } = new();
    
    /// <summary>
    /// 长期目标（1-2年）
    /// </summary>
    public List<string> LongTermGoals { get; set; } = new();
    
    /// <summary>
    /// 推荐的学习资源或培训
    /// </summary>
    public List<string> RecommendedResources { get; set; } = new();
    
    public string? AiModel { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

/// <summary>
/// 发展建议项
/// </summary>
public class DevelopmentSuggestionItem
{
    public string Category { get; set; } = string.Empty; // 技术能力/沟通能力/问题解决/知识贡献等
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // high/medium/low
    public List<string> ActionItems { get; set; } = new();
    public string ExpectedOutcome { get; set; } = string.Empty;
}

/// <summary>
/// 绩效评价DTO
/// </summary>
public class PerformanceEvaluationDto
{
    public Guid AnalysisId { get; set; }
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    /// <summary>
    /// 综合绩效评价总结
    /// </summary>
    public string EvaluationSummary { get; set; } = string.Empty;
    
    /// <summary>
    /// 绩效等级：优秀/良好/合格/待改进/不合格
    /// </summary>
    public string PerformanceLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// 综合评分（0-100）
    /// </summary>
    public decimal OverallScore { get; set; }
    
    /// <summary>
    /// 各维度评价
    /// </summary>
    public Dictionary<string, DimensionEvaluation> DimensionEvaluations { get; set; } = new();
    
    /// <summary>
    /// 工作亮点
    /// </summary>
    public List<string> Highlights { get; set; } = new();
    
    /// <summary>
    /// 需要改进的方面
    /// </summary>
    public List<string> AreasForImprovement { get; set; } = new();
    
    /// <summary>
    /// 具体案例和证据
    /// </summary>
    public List<PerformanceEvidence> Evidence { get; set; } = new();
    
    /// <summary>
    /// 与团队/部门平均水平的对比
    /// </summary>
    public ComparisonWithAverage? Comparison { get; set; }
    
    public string? AiModel { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

/// <summary>
/// 维度评价
/// </summary>
public class DimensionEvaluation
{
    public string DimensionName { get; set; } = string.Empty;
    public decimal Score { get; set; } // 0-100
    public string Level { get; set; } = string.Empty;
    public string Evaluation { get; set; } = string.Empty; // 详细评价文字
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

/// <summary>
/// 绩效证据
/// </summary>
public class PerformanceEvidence
{
    public string Type { get; set; } = string.Empty; // ticket/judgement_card/solution/communication
    public string Description { get; set; } = string.Empty;
    public string? ReferenceId { get; set; } // 工单ID、判断卡ID等
    public DateTime? OccurredAt { get; set; }
}

/// <summary>
/// 与平均水平对比
/// </summary>
public class ComparisonWithAverage
{
    public decimal TeamAverageScore { get; set; }
    public decimal DepartmentAverageScore { get; set; }
    public string ComparisonSummary { get; set; } = string.Empty;
    public List<string> Advantages { get; set; } = new(); // 相对于平均水平的优势
    public List<string> Gaps { get; set; } = new(); // 与平均水平的差距
}


