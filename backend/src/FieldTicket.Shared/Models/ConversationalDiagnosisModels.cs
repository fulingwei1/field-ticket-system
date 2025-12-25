using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 开始诊断对话请求
/// </summary>
public class StartConversationRequest
{
    // 可以添加额外的参数，目前只需要 ticketId
}

/// <summary>
/// 诊断对话DTO
/// </summary>
public class DiagnosisConversationDto
{
    public Guid ConversationId { get; set; }
    public Guid TicketId { get; set; }
    public string? CurrentHypothesis { get; set; }
    public decimal? CurrentConfidence { get; set; }
    public int ConversationRound { get; set; }
    public string Status { get; set; } = string.Empty;
    public JsonDocument? DiagnosisPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 假设DTO
/// </summary>
public class HypothesisDto
{
    public string HypothesisId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public List<string> Evidence { get; set; } = new();
    public List<string> SupportingKnowledgeIds { get; set; } = new();
}

/// <summary>
/// 生成验证步骤请求
/// </summary>
public class GenerateVerificationStepsRequest
{
    public string HypothesisId { get; set; } = string.Empty;
}

/// <summary>
/// 验证步骤DTO
/// </summary>
public class VerificationStepDto
{
    public Guid StepId { get; set; }
    public Guid ConversationId { get; set; }
    public string? HypothesisId { get; set; }
    public string StepDescription { get; set; } = string.Empty;
    public string? StepType { get; set; }
    public string? ExpectedResult { get; set; }
    public string? ActualResult { get; set; }
    public string VerificationStatus { get; set; } = "pending";
    public string? VerificationNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 提交验证结果请求
/// </summary>
public class SubmitVerificationResultRequest
{
    public Guid StepId { get; set; }
    public string ActualResult { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "pending"; // 'passed', 'failed', 'inconclusive'
    public string? VerificationNotes { get; set; }
}

/// <summary>
/// 验证结果DTO
/// </summary>
public class VerificationResultDto
{
    public Guid StepId { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? ActualResult { get; set; }
    public string? VerificationNotes { get; set; }
}

/// <summary>
/// 调整假设请求
/// </summary>
public class AdjustHypothesisRequest
{
    public string HypothesisId { get; set; } = string.Empty;
    public List<VerificationResultDto> VerificationResults { get; set; } = new();
}

/// <summary>
/// 对话结果DTO
/// </summary>
public class ConversationResultDto
{
    public Guid ConversationId { get; set; }
    public HypothesisDto? AdjustedHypothesis { get; set; }
    public bool ShouldContinue { get; set; }
    public string? NextAction { get; set; }
}

/// <summary>
/// 诊断结果DTO
/// </summary>
public class DiagnosisResultDto
{
    public Guid ConversationId { get; set; }
    public string FinalHypothesis { get; set; } = string.Empty;
    public decimal FinalConfidence { get; set; }
    public JsonDocument? DiagnosisPath { get; set; }
    public int TotalRounds { get; set; }
}

/// <summary>
/// 诊断路径DTO
/// </summary>
public class DiagnosisPathDto
{
    public List<DiagnosisRoundDto> Rounds { get; set; } = new();
    public string? FinalHypothesis { get; set; }
    public decimal? FinalConfidence { get; set; }
}

/// <summary>
/// 诊断轮次DTO
/// </summary>
public class DiagnosisRoundDto
{
    public int Round { get; set; }
    public string Hypothesis { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public List<VerificationStepDto> VerificationSteps { get; set; } = new();
    public string? AdjustedHypothesis { get; set; }
    public decimal? AdjustedConfidence { get; set; }
}


