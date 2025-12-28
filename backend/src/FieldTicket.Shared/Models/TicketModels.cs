using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 创建工单请求
/// </summary>
public class CreateTicketRequest
{
    public string? LocalDraftId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid? StationId { get; set; }
    public char Domain { get; set; } // A/B/C/D/E
    public string StepCode { get; set; } = string.Empty;
    public string? StepName { get; set; }
    public string SymptomTitle { get; set; } = string.Empty;
    public string? SymptomDetail { get; set; }
    public int? ReproRate { get; set; }
    public bool? RebootRecovers { get; set; }
    public bool? EnvRelated { get; set; }
    public string SwVersion { get; set; } = string.Empty;
    public string PlcVersion { get; set; } = string.Empty;
    public string ParamVersion { get; set; } = string.Empty;
    public JsonDocument FactsJson { get; set; } = JsonDocument.Parse("{}");
    public List<string> ActionsTaken { get; set; } = new();
    public string? ActionsTakenNote { get; set; }
    public string? AlarmCode { get; set; }
    public bool ConfirmedAsFact { get; set; }
}

/// <summary>
/// 更新工单请求
/// </summary>
public class UpdateTicketRequest
{
    public char? Domain { get; set; }
    public string? StepCode { get; set; }
    public string? StepName { get; set; }
    public string? SymptomTitle { get; set; }
    public string? SymptomDetail { get; set; }
    public int? ReproRate { get; set; }
    public bool? RebootRecovers { get; set; }
    public bool? EnvRelated { get; set; }
    public string? SwVersion { get; set; }
    public string? PlcVersion { get; set; }
    public string? ParamVersion { get; set; }
    public JsonDocument? FactsJson { get; set; }
    public List<string>? ActionsTaken { get; set; }
    public string? ActionsTakenNote { get; set; }
    public string? AlarmCode { get; set; }
    public bool? ConfirmedAsFact { get; set; }
}

/// <summary>
/// 工单DTO
/// </summary>
public class TicketDto
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid DeviceId { get; set; }
    public string? DeviceSn { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public char Domain { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string? StepName { get; set; }
    public string SymptomTitle { get; set; } = string.Empty;
    public string? SymptomDetail { get; set; }
    public int? ReproRate { get; set; }
    public bool? RebootRecovers { get; set; }
    public bool? EnvRelated { get; set; }
    public string SwVersion { get; set; } = string.Empty;
    public string PlcVersion { get; set; } = string.Empty;
    public string ParamVersion { get; set; } = string.Empty;
    public JsonDocument FactsJson { get; set; } = JsonDocument.Parse("{}");
    public List<string> ActionsTaken { get; set; } = new();
    public string? ActionsTakenNote { get; set; }
    public string? AlarmCode { get; set; }
    public bool ConfirmedAsFact { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? CurrentJcCode { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int AttachmentCount { get; set; }
}

/// <summary>
/// 工单列表项DTO
/// </summary>
public class TicketListItemDto
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DeviceSn { get; set; } = string.Empty;
    public char Domain { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string SymptomTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

/// <summary>
/// 校验错误
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 校验结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}


