using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 设备配置快照实体
/// </summary>
public class DeviceConfigSnapshot
{
    public Guid SnapshotId { get; set; }
    
    // 关联设备
    public Guid DeviceId { get; set; }
    
    // 快照类型
    public string SnapshotType { get; set; } = string.Empty; // delivery, change, problem
    
    // 快照时间
    public DateTime SnapshotAt { get; set; }
    
    // 配置内容（JSONB）
    public JsonDocument ConfigJson { get; set; } = JsonDocument.Parse("{}");
    /*
    示例：
    {
      "sw_version": "v2.1.3",
      "plc_version": "v1.2.6",
      "param_version": "v3.4",
      "key_parameters": {
        "timeout_step_120": "300ms",
        "filter_time": "50ms"
      }
    }
    */
    
    // 标准配置（用于对比）
    public JsonDocument? StandardConfigJson { get; set; }
    
    // 差异点（JSONB）
    public JsonDocument? Differences { get; set; }
    /*
    示例：
    [
      {
        "field": "timeout_step_120",
        "standard": "800ms",
        "actual": "300ms",
        "impact": "可能导致超时"
      }
    ]
    */
    
    // 创建信息
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

