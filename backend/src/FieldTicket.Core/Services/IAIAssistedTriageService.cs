using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// AI辅助分诊服务接口
/// </summary>
public interface IAIAssistedTriageService
{
    /// <summary>
    /// AI辅助填写判断卡（结构化triage）
    /// </summary>
    Task<AIAssistedTriageResult> AssistTriageAsync(Guid ticketId);

    /// <summary>
    /// 生成Top-3假设（基于RAG）
    /// </summary>
    Task<List<HypothesisDto>> GenerateHypothesesAsync(Guid ticketId, string? jcCode = null);

    /// <summary>
    /// 生成下一步动作建议
    /// </summary>
    Task<List<ActionSuggestionDto>> GenerateActionSuggestionsAsync(
        Guid ticketId,
        string? jcCode = null,
        List<string>? hypothesisIds = null);

    /// <summary>
    /// 生成缺失信息问题清单
    /// </summary>
    Task<List<MissingInfoQuestionDto>> GenerateMissingInfoQuestionsAsync(
        Guid ticketId,
        string? jcCode = null);
}

/// <summary>
/// AI辅助分诊结果
/// </summary>
public class AIAssistedTriageResult
{
    /// <summary>
    /// 推荐的判断卡编号
    /// </summary>
    public string? RecommendedJcCode { get; set; }

    /// <summary>
    /// 推荐的判断卡标题
    /// </summary>
    public string? RecommendedJcTitle { get; set; }

    /// <summary>
    /// 推荐的假设
    /// </summary>
    public string? RecommendedHypothesis { get; set; }

    /// <summary>
    /// 推荐的下一步动作
    /// </summary>
    public string? RecommendedNextAction { get; set; }

    /// <summary>
    /// 置信度（1-5）
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// 置信度等级
    /// </summary>
    public string ConfidenceLevel => Confidence switch
    {
        >= 4 => "high",
        >= 3 => "medium",
        _ => "low"
    };

    /// <summary>
    /// 是否需要升级
    /// </summary>
    public bool EscalationRequired => Confidence <= 2;

    /// <summary>
    /// 推荐理由
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Top-3假设
    /// </summary>
    public List<HypothesisDto> Hypotheses { get; set; } = new();

    /// <summary>
    /// 动作建议
    /// </summary>
    public List<ActionSuggestionDto> ActionSuggestions { get; set; } = new();

    /// <summary>
    /// 缺失信息问题
    /// </summary>
    public List<MissingInfoQuestionDto> MissingInfoQuestions { get; set; } = new();
}

/// <summary>
/// 假设DTO
/// </summary>
public class HypothesisDto
{
    /// <summary>
    /// 假设ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 排名（1-3）
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 假设描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 置信度（high/medium/low）
    /// </summary>
    public string Confidence { get; set; } = "medium";

    /// <summary>
    /// 证据列表
    /// </summary>
    public List<string> Evidence { get; set; } = new();

    /// <summary>
    /// 支持的知识块ID
    /// </summary>
    public List<string> SupportingKnowledgeIds { get; set; } = new();

    /// <summary>
    /// 知识块引用
    /// </summary>
    public List<KnowledgeChunkReference> KnowledgeReferences { get; set; } = new();
}

/// <summary>
/// 知识块引用
/// </summary>
public class KnowledgeChunkReference
{
    /// <summary>
    /// 知识块ID
    /// </summary>
    public string ChunkId { get; set; } = string.Empty;

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容摘要
    /// </summary>
    public string ContentSummary { get; set; } = string.Empty;

    /// <summary>
    /// 来源类型（judgement_card/solution/case/faq）
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// 来源ID
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// 相似度得分
    /// </summary>
    public double SimilarityScore { get; set; }
}

/// <summary>
/// 动作建议DTO
/// </summary>
public class ActionSuggestionDto
{
    /// <summary>
    /// 动作ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 动作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 动作类型（check/action/verify）
    /// </summary>
    public string ActionType { get; set; } = "action";

    /// <summary>
    /// 是否可验证
    /// </summary>
    public bool IsVerifiable { get; set; }

    /// <summary>
    /// 验证方法
    /// </summary>
    public string? VerificationMethod { get; set; }

    /// <summary>
    /// 优先级（1-5）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 关联的假设ID
    /// </summary>
    public List<string> RelatedHypothesisIds { get; set; } = new();
}

/// <summary>
/// 缺失信息问题DTO
/// </summary>
public class MissingInfoQuestionDto
{
    /// <summary>
    /// 问题ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 问题文本
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 问题类型（explicit/implicit/additional）
    /// </summary>
    public string QuestionType { get; set; } = "explicit";

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 问题说明
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// 建议答案选项（可选）
    /// </summary>
    public List<string>? SuggestedAnswers { get; set; }
}
















