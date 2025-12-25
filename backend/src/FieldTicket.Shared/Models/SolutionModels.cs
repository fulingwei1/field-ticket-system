using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 创建解决方案请求
/// </summary>
public class CreateSolutionRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SolutionType { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    public JsonDocument ChangeDetailJson { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument VerificationChecklistJson { get; set; } = JsonDocument.Parse("{}");
    
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; }
    
    public string RiskLevel { get; set; } = "medium";
    public string? RiskDescription { get; set; }
    
    public bool RollbackPossible { get; set; } = true;
    public string? RollbackProcedure { get; set; }
}

/// <summary>
/// 更新解决方案请求
/// </summary>
public class UpdateSolutionRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? SolutionType { get; set; }
    public string? ReleaseType { get; set; }
    
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    public JsonDocument? ChangeDetailJson { get; set; }
    public JsonDocument? VerificationChecklistJson { get; set; }
    
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; }
    
    public string? RiskLevel { get; set; }
    public string? RiskDescription { get; set; }
    
    public bool? RollbackPossible { get; set; }
    public string? RollbackProcedure { get; set; }
}

/// <summary>
/// 解决方案DTO
/// </summary>
public class SolutionDto
{
    public Guid SolutionId { get; set; }
    public string SolutionCode { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SolutionType { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    public JsonDocument ChangeDetailJson { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument VerificationChecklistJson { get; set; } = JsonDocument.Parse("{}");
    
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; }
    
    public string RiskLevel { get; set; } = string.Empty;
    public string? RiskDescription { get; set; }
    
    public bool RollbackPossible { get; set; }
    public string? RollbackProcedure { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
}


