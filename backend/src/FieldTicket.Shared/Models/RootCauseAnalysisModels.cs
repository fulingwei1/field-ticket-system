namespace FieldTicket.Shared.Models;

/// <summary>
/// 根本原因分析DTO
/// </summary>
public class RootCauseAnalysisDto
{
    public Guid AnalysisId { get; set; }
    public Guid ProblemId { get; set; }
    public string? Why1 { get; set; }
    public string? Why2 { get; set; }
    public string? Why3 { get; set; }
    public string? Why4 { get; set; }
    public string? Why5 { get; set; }
    public string RootCause { get; set; } = string.Empty;
    public string? RootCauseCategory { get; set; }
    public string? PreventiveMeasures { get; set; }
    public string? VerificationMethod { get; set; }
    public Guid? AnalyzedBy { get; set; }
    public string? AnalyzedByName { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建根本原因分析请求
/// </summary>
public class CreateRootCauseAnalysisRequest
{
    public string? Why1 { get; set; }
    public string? Why2 { get; set; }
    public string? Why3 { get; set; }
    public string? Why4 { get; set; }
    public string? Why5 { get; set; }
    public string RootCause { get; set; } = string.Empty;
    public string? RootCauseCategory { get; set; }
    public string? PreventiveMeasures { get; set; }
    public string? VerificationMethod { get; set; }
}

/// <summary>
/// 5Why分析模板
/// </summary>
public class FiveWhyTemplate
{
    public string ProblemCategory { get; set; } = string.Empty;
    public List<string> WhyQuestions { get; set; } = new();
    public List<string> CommonRootCauses { get; set; } = new();
    public List<string> SuggestedPreventiveMeasures { get; set; } = new();
}

/// <summary>
/// 预防措施建议
/// </summary>
public class PreventiveMeasureSuggestion
{
    public string Category { get; set; } = string.Empty;
    public string Measure { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? VerificationMethod { get; set; }
    public int Priority { get; set; } // 1-5，优先级
}


