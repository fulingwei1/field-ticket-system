namespace FieldTicket.Shared.Models;

/// <summary>
/// 深度分析结果
/// </summary>
public class DeepAnalysisResult
{
    /// <summary>
    /// 明确缺失的信息（基于判断卡要求）
    /// </summary>
    public List<MissingInfoItem> ExplicitMissing { get; set; } = new();

    /// <summary>
    /// 隐含缺失的信息（基于上下文分析）
    /// </summary>
    public List<ImplicitInfoRequirement> ImplicitMissing { get; set; } = new();

    /// <summary>
    /// 可能有助于诊断的额外信息
    /// </summary>
    public List<AdditionalInfoSuggestion> AdditionalInfo { get; set; } = new();

    /// <summary>
    /// 上下文理解结果
    /// </summary>
    public ContextInfo? ContextInfo { get; set; }

    /// <summary>
    /// 个性化问题清单
    /// </summary>
    public List<PersonalizedQuestion> PersonalizedQuestions { get; set; } = new();
}

/// <summary>
/// 隐含信息需求
/// </summary>
public class ImplicitInfoRequirement
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 问题描述
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 问题类型
    /// </summary>
    public string Type { get; set; } = "yes_no";

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 置信度（0-5）
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// 理由说明
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 关联的历史工单ID（用于证据引用）
    /// </summary>
    public List<Guid>? RelatedTicketIds { get; set; }
}

/// <summary>
/// 额外信息建议
/// </summary>
public class AdditionalInfoSuggestion
{
    /// <summary>
    /// 建议的字段名
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 建议的问题
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 问题类型
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// 建议理由
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 上下文信息
/// </summary>
public class ContextInfo
{
    /// <summary>
    /// 语义分析结果
    /// </summary>
    public SemanticInfo? SemanticInfo { get; set; }

    /// <summary>
    /// 关键信息提取
    /// </summary>
    public List<KeyInfo> KeyInfo { get; set; } = new();

    /// <summary>
    /// 关联的历史工单
    /// </summary>
    public List<RelatedTicket> RelatedTickets { get; set; } = new();

    /// <summary>
    /// 设备上下文
    /// </summary>
    public DeviceContext? DeviceContext { get; set; }
}

/// <summary>
/// 语义分析结果
/// </summary>
public class SemanticInfo
{
    /// <summary>
    /// 问题严重程度（1-5）
    /// </summary>
    public int Severity { get; set; }

    /// <summary>
    /// 问题类型分类
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// 关键实体提取
    /// </summary>
    public List<string> Entities { get; set; } = new();

    /// <summary>
    /// 情感分析（positive/neutral/negative）
    /// </summary>
    public string Sentiment { get; set; } = "neutral";
}

/// <summary>
/// 关键信息
/// </summary>
public class KeyInfo
{
    /// <summary>
    /// 信息类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 信息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 重要性（1-5）
    /// </summary>
    public int Importance { get; set; }
}

/// <summary>
/// 关联工单
/// </summary>
public class RelatedTicket
{
    /// <summary>
    /// 工单ID
    /// </summary>
    public Guid TicketId { get; set; }

    /// <summary>
    /// 工单号
    /// </summary>
    public string TicketNo { get; set; } = string.Empty;

    /// <summary>
    /// 相似度（0-1）
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// 相似原因
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 设备上下文
/// </summary>
public class DeviceContext
{
    /// <summary>
    /// 设备类型
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// 历史问题统计
    /// </summary>
    public Dictionary<string, int>? HistoricalIssues { get; set; }

    /// <summary>
    /// 常见问题模式
    /// </summary>
    public List<string>? CommonPatterns { get; set; }
}

/// <summary>
/// 个性化问题
/// </summary>
public class PersonalizedQuestion
{
    /// <summary>
    /// 问题ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 问题文本
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 问题类型
    /// </summary>
    public string Type { get; set; } = "yes_no";

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 选项列表（用于select类型）
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// 提示信息
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// 关联的字段名
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// 优先级（1-5，5最高）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 个性化原因
    /// </summary>
    public string? PersonalizationReason { get; set; }

    /// <summary>
    /// 相关性得分
    /// </summary>
    public decimal RelevanceScore { get; set; }

    /// <summary>
    /// 理由说明
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 对话结果
/// </summary>
public class ConversationResult
{
    /// <summary>
    /// 下一轮问题（如果有）
    /// </summary>
    public PersonalizedQuestion? NextQuestion { get; set; }

    /// <summary>
    /// 对话是否完成
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// 已收集的信息
    /// </summary>
    public Dictionary<string, object> CollectedInfo { get; set; } = new();

    /// <summary>
    /// 对话历史
    /// </summary>
    public List<ConversationTurn> History { get; set; } = new();
}

/// <summary>
/// 对话轮次
/// </summary>
public class ConversationTurn
{
    /// <summary>
    /// 问题ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 问题文本
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 用户回答
    /// </summary>
    public string? Answer { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}

// JudgementCardDto 已在 TriageModels.cs 中定义，这里不再重复定义

