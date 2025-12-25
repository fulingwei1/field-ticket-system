using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 通知规则配置实体
/// </summary>
public class NotificationRule
{
    public Guid RuleId { get; set; }
    
    // 配置层级
    public string RuleLevel { get; set; } = string.Empty; // customer, project, device
    
    // 关联ID（根据层级不同，只有一个有值）
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DeviceId { get; set; }
    
    // 通知场景
    public string TriggerEvent { get; set; } = string.Empty; 
    // ticket_submitted, ticket_triaged, solution_published, verification_completed, ticket_closed
    
    // 通知对象配置（JSONB）
    public JsonDocument RecipientsConfig { get; set; } = JsonDocument.Parse("{}");
    /*
    示例：
    {
      "roles": ["project_manager", "sales", "engineer"],
      "userIds": ["userid1", "userid2"],
      "chatIds": ["chatid1"],
      "departments": ["dept1", "dept2"]
    }
    */
    
    // 通知模板（可选，覆盖默认模板）
    public string? TemplateOverride { get; set; }
    
    // 状态
    public bool IsActive { get; set; } = true;
    
    // 审计
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

