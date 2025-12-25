using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 保存通知规则请求
/// </summary>
public class SaveNotificationRuleRequest
{
    public Guid? RuleId { get; set; } // 更新时提供
    
    public string RuleLevel { get; set; } = string.Empty; // customer, project, device
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DeviceId { get; set; }
    
    public string TriggerEvent { get; set; } = string.Empty;
    
    public JsonDocument RecipientsConfig { get; set; } = JsonDocument.Parse("{}");
    
    public string? TemplateOverride { get; set; }
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 通知规则DTO
/// </summary>
public class NotificationRuleDto
{
    public Guid RuleId { get; set; }
    public string RuleLevel { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DeviceId { get; set; }
    public string TriggerEvent { get; set; } = string.Empty;
    public JsonDocument RecipientsConfig { get; set; } = JsonDocument.Parse("{}");
    public string? TemplateOverride { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 通知执行结果
/// </summary>
public class NotificationResult
{
    public bool Success { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

