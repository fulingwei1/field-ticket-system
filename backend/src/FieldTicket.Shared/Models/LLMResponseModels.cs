using System.Text.Json.Serialization;

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

/// <summary>
/// LLM技能水平分析响应
/// </summary>
public class SkillLevelAnalysisResponse
{
    [JsonPropertyName("overallSkillLevel")]
    public string OverallSkillLevel { get; set; } = string.Empty;
    
    [JsonPropertyName("skillScore")]
    public decimal SkillScore { get; set; }
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    [JsonPropertyName("strengths")]
    public List<string>? Strengths { get; set; }
    
    [JsonPropertyName("improvementAreas")]
    public List<string>? ImprovementAreas { get; set; }
    
    [JsonPropertyName("skillDimensions")]
    public List<SkillDimensionResponse>? SkillDimensions { get; set; }
}

/// <summary>
/// 技能维度响应
/// </summary>
public class SkillDimensionResponse
{
    [JsonPropertyName("dimensionName")]
    public string DimensionName { get; set; } = string.Empty;
    
    [JsonPropertyName("score")]
    public decimal Score { get; set; }
    
    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("evidence")]
    public List<string>? Evidence { get; set; }
}

/// <summary>
/// LLM发展建议响应
/// </summary>
public class DevelopmentSuggestionResponse
{
    [JsonPropertyName("developmentCharacteristics")]
    public string? DevelopmentCharacteristics { get; set; }
    
    [JsonPropertyName("suggestions")]
    public List<DevelopmentSuggestionItemResponse>? Suggestions { get; set; }
    
    [JsonPropertyName("shortTermGoals")]
    public List<string>? ShortTermGoals { get; set; }
    
    [JsonPropertyName("mediumTermGoals")]
    public List<string>? MediumTermGoals { get; set; }
    
    [JsonPropertyName("longTermGoals")]
    public List<string>? LongTermGoals { get; set; }
    
    [JsonPropertyName("recommendedResources")]
    public List<string>? RecommendedResources { get; set; }
}

/// <summary>
/// 发展建议项响应
/// </summary>
public class DevelopmentSuggestionItemResponse
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;
    
    [JsonPropertyName("actionItems")]
    public List<string>? ActionItems { get; set; }
    
    [JsonPropertyName("expectedOutcome")]
    public string ExpectedOutcome { get; set; } = string.Empty;
}

/// <summary>
/// LLM绩效评价响应
/// </summary>
public class PerformanceEvaluationResponse
{
    [JsonPropertyName("evaluationSummary")]
    public string? EvaluationSummary { get; set; }
    
    [JsonPropertyName("performanceLevel")]
    public string? PerformanceLevel { get; set; }
    
    [JsonPropertyName("overallScore")]
    public decimal OverallScore { get; set; }
    
    [JsonPropertyName("dimensionEvaluations")]
    public List<DimensionEvaluationResponse>? DimensionEvaluations { get; set; }
    
    [JsonPropertyName("highlights")]
    public List<string>? Highlights { get; set; }
    
    [JsonPropertyName("areasForImprovement")]
    public List<string>? AreasForImprovement { get; set; }
    
    [JsonPropertyName("evidence")]
    public List<PerformanceEvidenceResponse>? Evidence { get; set; }
    
    [JsonPropertyName("comparison")]
    public ComparisonResponse? Comparison { get; set; }
}

/// <summary>
/// 维度评价响应
/// </summary>
public class DimensionEvaluationResponse
{
    [JsonPropertyName("dimensionName")]
    public string DimensionName { get; set; } = string.Empty;
    
    [JsonPropertyName("score")]
    public decimal Score { get; set; }
    
    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;
    
    [JsonPropertyName("evaluation")]
    public string Evaluation { get; set; } = string.Empty;
    
    [JsonPropertyName("strengths")]
    public List<string>? Strengths { get; set; }
    
    [JsonPropertyName("weaknesses")]
    public List<string>? Weaknesses { get; set; }
}

/// <summary>
/// 绩效证据响应
/// </summary>
public class PerformanceEvidenceResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("referenceId")]
    public string? ReferenceId { get; set; }
    
    [JsonPropertyName("occurredAt")]
    public DateTime? OccurredAt { get; set; }
}

/// <summary>
/// 对比响应
/// </summary>
public class ComparisonResponse
{
    [JsonPropertyName("comparisonSummary")]
    public string? ComparisonSummary { get; set; }
    
    [JsonPropertyName("advantages")]
    public List<string>? Advantages { get; set; }
    
    [JsonPropertyName("gaps")]
    public List<string>? Gaps { get; set; }
}

