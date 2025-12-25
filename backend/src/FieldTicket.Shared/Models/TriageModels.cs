using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 分诊工单请求
/// </summary>
public class TriageTicketRequest
{
    /// <summary>
    /// 判断卡编号（必填，硬规则HR-001）
    /// </summary>
    public string JcCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前假设
    /// </summary>
    public string? CurrentHypothesis { get; set; }
    
    /// <summary>
    /// 下一步动作
    /// </summary>
    public string? NextAction { get; set; }
    
    /// <summary>
    /// 置信度（1-5，必填）
    /// </summary>
    public int Confidence { get; set; } // 1-5
    
    /// <summary>
    /// 分诊备注
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// 分诊结果
/// </summary>
public class TriageResult
{
    public Guid TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? JcCode { get; set; }
    public bool EscalationRequired { get; set; }
    public Guid? EscalatedTo { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 判断卡DTO
/// </summary>
public class JudgementCardDto
{
    public Guid JudgementCardId { get; set; }
    public string JcCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public char Domain { get; set; }
    public JsonDocument SymptomStructure { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument TroubleshootingPath { get; set; } = JsonDocument.Parse("{}");
    public string? HypothesisTemplate { get; set; }
    public string? NextActionTemplate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


