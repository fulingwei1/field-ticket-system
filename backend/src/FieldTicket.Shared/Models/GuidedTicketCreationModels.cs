using System.Text.Json.Serialization;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 引导式工单创建会话
/// </summary>
public class GuidedTicketCreationSession
{
    public Guid SessionId { get; set; }
    public Guid? TicketId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// 会话状态：collecting, analyzing, guiding, completing, completed
    /// </summary>
    public string Status { get; set; } = "collecting";

    public int TurnCount { get; set; }
    public int MaxTurns { get; set; } = 3;

    /// <summary>
    /// 初始文字描述
    /// </summary>
    public string? InitialText { get; set; }

    /// <summary>
    /// 图片附件ID列表
    /// </summary>
    public List<Guid> ImageAttachmentIds { get; set; } = new();

    /// <summary>
    /// 文本分析结果
    /// </summary>
    public TextAnalysisResult? TextAnalysis { get; set; }

    /// <summary>
    /// 图片分析结果列表
    /// </summary>
    public List<ImageAnalysisResult> ImageAnalyses { get; set; } = new();

    /// <summary>
    /// 综合分析结果
    /// </summary>
    public ComprehensiveAnalysisResult? ComprehensiveAnalysis { get; set; }

    /// <summary>
    /// 对话历史
    /// </summary>
    public List<GuidedConversationTurn> ConversationHistory { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 引导式对话轮次（扩展版，包含更多字段）
/// </summary>
public class GuidedConversationTurn
{
    public int TurnNumber { get; set; }
    public string Role { get; set; } = string.Empty; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public List<Guid>? AttachmentIds { get; set; }
    public List<GuidedQuestion>? Questions { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 引导性问题
/// </summary>
public class GuidedQuestion
{
    public string QuestionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // yes_no, text, number, select, file
    public List<string>? Options { get; set; }
    public string? Hint { get; set; }
    public string? ProfessionalTermExample { get; set; }
    public string? WhyImportant { get; set; }
}

/// <summary>
/// 引导性问题响应
/// </summary>
public class GuidedQuestionResponse
{
    public GuidedTicketCreationSession Session { get; set; } = new();
    public bool IsComplete { get; set; }
    public List<GuidedQuestion> Questions { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public string? NextStepHint { get; set; }
}

/// <summary>
/// 图片分析结果
/// </summary>
public class ImageAnalysisResult
{
    public DeviceInfo? DeviceInfo { get; set; }
    public List<ProblemPhenomenon> ProblemPhenomena { get; set; } = new();
    public List<string> KeyInformation { get; set; } = new();
    public string? OcrText { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 设备信息
/// </summary>
public class DeviceInfo
{
    public string? Type { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// 问题现象
/// </summary>
public class ProblemPhenomenon
{
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ProfessionalTerm { get; set; }
}

/// <summary>
/// 文本分析结果
/// </summary>
public class TextAnalysisResult
{
    public string Domain { get; set; } = string.Empty; // A, B, C, D, E
    public ProfessionalDescription ProfessionalDescription { get; set; } = new();
    public KeyInformation KeyInformation { get; set; } = new();
    public List<MissingInfoItem> MissingInfo { get; set; } = new();
    public List<TerminologySuggestion> TerminologySuggestions { get; set; } = new();
}

/// <summary>
/// 专业描述
/// </summary>
public class ProfessionalDescription
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// 关键信息
/// </summary>
public class KeyInformation
{
    public string? DeviceModel { get; set; }
    public string? Symptom { get; set; }
    public string? Frequency { get; set; }
    public string? Environment { get; set; }
}

// MissingInfoItem 已在 MissingInfoModels.cs 中定义
// 这里使用已有的类，但需要添加 Reason 字段的映射

/// <summary>
/// 术语建议
/// </summary>
public class TerminologySuggestion
{
    public string Original { get; set; } = string.Empty;
    public string Professional { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// 综合分析结果
/// </summary>
public class ComprehensiveAnalysisResult
{
    public string Domain { get; set; } = string.Empty;
    public ProfessionalDescription ProfessionalDescription { get; set; } = new();
    public KeyInformation KeyInformation { get; set; } = new();
    public List<MissingInfoItem> MissingInfo { get; set; } = new();
    public List<TerminologySuggestion> TerminologySuggestions { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 工单总结
/// </summary>
public class TicketSummary
{
    public string SymptomTitle { get; set; } = string.Empty;
    public string SymptomDetail { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string? StepCode { get; set; }
    public string? StepName { get; set; }
    public Dictionary<string, string> FactsJson { get; set; } = new();
    
    /// <summary>
    /// 状态字段（用于兼容，实际不使用）
    /// </summary>
    [JsonIgnore]
    public string? Status { get; set; }
    public VersionInfo? VersionInfo { get; set; }
    public ConfidenceScore Confidence { get; set; } = new();
}

/// <summary>
/// 版本信息
/// </summary>
public class VersionInfo
{
    public string? SwVersion { get; set; }
    public string? PlcVersion { get; set; }
    public string? ParamVersion { get; set; }
}

/// <summary>
/// 置信度评分
/// </summary>
public class ConfidenceScore
{
    public int Overall { get; set; }
    public int Title { get; set; }
    public int Detail { get; set; }
    public int Facts { get; set; }
}

/// <summary>
/// 引导式工单创建生成的内容
/// </summary>
public class GuidedTicketContent
{
    public TicketSummary Summary { get; set; } = new();
    public List<Guid> ImageAttachmentIds { get; set; } = new();
    public List<TerminologySuggestion> TerminologySuggestions { get; set; } = new();
    public ConfidenceScore Confidence { get; set; } = new();
}

