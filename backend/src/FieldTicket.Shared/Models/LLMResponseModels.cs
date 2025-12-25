namespace FieldTicket.Shared.Models;

/// <summary>
/// LLM缺失信息分析响应
/// </summary>
public class LLMMissingInfoAnalysisResponse
{
    public List<LLMMissingInfoItem> ExplicitMissing { get; set; } = new();
    public List<LLMMissingInfoItem> ImplicitMissing { get; set; } = new();
    public List<LLMMissingInfoItem> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// LLM缺失信息项
/// </summary>
public class LLMMissingInfoItem
{
    public string Field { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Type { get; set; } = "yes_no";
    public bool Required { get; set; }
    public int? Confidence { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// LLM上下文理解响应
/// </summary>
public class LLMContextUnderstandingResponse
{
    public int Severity { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Entities { get; set; } = new();
    public string Sentiment { get; set; } = "neutral";
}

/// <summary>
/// LLM个性化问题响应
/// </summary>
public class LLMPersonalizedQuestionResponse
{
    public List<LLMPersonalizedQuestionItem> Questions { get; set; } = new();
}

/// <summary>
/// LLM个性化问题项
/// </summary>
public class LLMPersonalizedQuestionItem
{
    public string Field { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Type { get; set; } = "yes_no";
    public bool Required { get; set; }
    public int Priority { get; set; } = 3;
    public string? Hint { get; set; }
    public string? PersonalizationReason { get; set; }
}

/// <summary>
/// LLM对话响应
/// </summary>
public class LLMConversationResponse
{
    public LLMConversationQuestion? NextQuestion { get; set; }
    public bool IsComplete { get; set; }
    public Dictionary<string, object> CollectedInfo { get; set; } = new();
}

/// <summary>
/// LLM对话问题
/// </summary>
public class LLMConversationQuestion
{
    public string Field { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Type { get; set; } = "yes_no";
    public bool Required { get; set; }
}

