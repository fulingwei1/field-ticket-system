namespace FieldTicket.Core.Services;

/// <summary>
/// 判断卡质量评分服务接口
/// </summary>
public interface IJudgementCardQualityService
{
    /// <summary>
    /// 对判断卡进行质量评分
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <returns>质量评分结果</returns>
    Task<JudgementCardQualityScore> ScoreAsync(string jcCode);

    /// <summary>
    /// 批量评分判断卡
    /// </summary>
    /// <param name="jcCodes">判断卡编号列表</param>
    /// <returns>质量评分结果列表</returns>
    Task<List<JudgementCardQualityScore>> BatchScoreAsync(List<string> jcCodes);

    /// <summary>
    /// 获取判断卡的质量问题列表
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <returns>质量问题列表</returns>
    Task<List<QualityIssue>> GetQualityIssuesAsync(string jcCode);
}

/// <summary>
/// 判断卡质量评分结果
/// </summary>
public class JudgementCardQualityScore
{
    public string JcCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 完整性得分（0-30分）
    /// </summary>
    public int CompletenessScore { get; set; }
    
    /// <summary>
    /// 逻辑一致性得分（0-30分）
    /// </summary>
    public int LogicConsistencyScore { get; set; }
    
    /// <summary>
    /// 可验证性得分（0-20分）
    /// </summary>
    public int VerifiabilityScore { get; set; }
    
    /// <summary>
    /// 证据支撑得分（0-20分）
    /// </summary>
    public int EvidenceScore { get; set; }
    
    /// <summary>
    /// 总分（0-100分）
    /// </summary>
    public int TotalScore => CompletenessScore + LogicConsistencyScore + 
                            VerifiabilityScore + EvidenceScore;
    
    /// <summary>
    /// 质量等级
    /// </summary>
    public QualityLevel Level => TotalScore switch
    {
        >= 80 => QualityLevel.Excellent,
        >= 60 => QualityLevel.Good,
        >= 40 => QualityLevel.Fair,
        _ => QualityLevel.Poor
    };
    
    /// <summary>
    /// 质量问题列表
    /// </summary>
    public List<QualityIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// 评分时间
    /// </summary>
    public DateTime ScoredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 质量等级
/// </summary>
public enum QualityLevel
{
    Poor = 0,      // 差（0-39分）
    Fair = 1,      // 一般（40-59分）
    Good = 2,      // 良好（60-79分）
    Excellent = 3  // 优秀（80-100分）
}

/// <summary>
/// 质量问题
/// </summary>
public class QualityIssue
{
    /// <summary>
    /// 问题类型
    /// </summary>
    public QualityIssueType Type { get; set; }
    
    /// <summary>
    /// 问题描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 严重程度
    /// </summary>
    public QualityIssueSeverity Severity { get; set; }
    
    /// <summary>
    /// 建议修复方案
    /// </summary>
    public string? Suggestion { get; set; }
}

/// <summary>
/// 质量问题类型
/// </summary>
public enum QualityIssueType
{
    MissingHypothesis,           // 缺少假设
    MissingEliminatedCauses,     // 有假设但无排除原因
    HighConfidenceNoEvidence,    // 高置信度但无证据
    DomainActionConflict,        // 问题域与动作冲突
    IncompleteDecisionBoundary,  // 判断边界不完整
    MissingKeyChecks,            // 缺少关键检查项
    MissingFailureModes,         // 缺少失效模式
    UnverifiableAction,          // 动作不可验证
    IncompleteChecklist,         // 验证清单不完整
    UnclearAcceptanceCriteria,   // 验收标准不明确
    NoHistoricalData,            // 无历史数据
    NoSuccessCases,              // 无成功案例
    NoFailureCases               // 无失败案例
}

/// <summary>
/// 质量问题严重程度
/// </summary>
public enum QualityIssueSeverity
{
    Low,      // 低
    Medium,   // 中
    High,     // 高
    Critical  // 严重
}

