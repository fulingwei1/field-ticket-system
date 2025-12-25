using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 设备变更记录实体
/// </summary>
public class DeviceChangeLog
{
    public Guid ChangeId { get; set; }
    
    // 关联设备
    public Guid DeviceId { get; set; }
    
    // 变更类型
    public string ChangeType { get; set; } = string.Empty; // program_upgrade, param_change, component_replacement, config_change
    
    // 变更时间
    public DateTime ChangeDate { get; set; }
    
    // 变更详情（JSONB）
    public JsonDocument ChangeDetail { get; set; } = JsonDocument.Parse("{}");
    /*
    示例：
    {
      "sw_version": { "old": "v1.2.3", "new": "v1.3.0" },
      "plc_version": { "old": "v2.0.1", "new": "v2.0.2" },
      "param_version": { "old": "v1.0", "new": "v1.1" },
      "component": { "type": "sensor", "old": "A001", "new": "A002" },
      "description": "升级软件版本修复超时问题"
    }
    */
    
    // 影响范围（JSONB）
    public JsonDocument? ImpactScope { get; set; }
    /*
    示例：
    {
      "affected_steps": ["Step_120", "Step_150"],
      "affected_domains": ["A", "B"],
      "risk_level": "medium"
    }
    */
    
    // 创建信息
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

