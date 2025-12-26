using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 工单模板实体
/// </summary>
public class TicketTemplate
{
    public Guid TemplateId { get; set; }
    
    // 模板基本信息
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; } // 分类：如 "常见问题"、"设备故障" 等
    
    // 模板创建者
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 使用统计
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // 是否公开（所有用户可见）
    public bool IsPublic { get; set; }
    
    // 模板数据（JSONB）
    public JsonDocument TemplateData { get; set; } = JsonDocument.Parse("{}");
    /*
    示例结构：
    {
      "domain": "A",
      "stepCode": "Step_120",
      "stepName": "步骤名称",
      "symptomTitle": "症状标题",
      "symptomDetail": "症状详情",
      "reproRate": 80,
      "rebootRecovers": false,
      "envRelated": false,
      "swVersion": "1.0.0",
      "plcVersion": "2.0.0",
      "paramVersion": "1.5.0",
      "factsJson": {
        "fact1": "value1",
        "fact2": "value2"
      },
      "actionsTaken": ["action1", "action2"],
      "actionsTakenNote": "备注",
      "alarmCode": "ALM001"
    }
    */
    
    // 软删除
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}









