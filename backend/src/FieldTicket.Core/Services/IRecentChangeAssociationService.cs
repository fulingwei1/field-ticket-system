namespace FieldTicket.Core.Services;

/// <summary>
/// 最近变更自动关联服务接口
/// </summary>
public interface IRecentChangeAssociationService
{
    /// <summary>
    /// 获取工单相关的最近变更
    /// </summary>
    /// <param name="ticketId">工单ID</param>
    /// <param name="daysBefore">向前查找天数（默认7天）</param>
    /// <param name="daysAfter">向后查找天数（默认7天）</param>
    /// <returns>相关变更列表（按相关性排序）</returns>
    Task<List<RelatedChangeDto>> GetRelatedChangesAsync(
        Guid ticketId,
        int daysBefore = 7,
        int daysAfter = 7);

    /// <summary>
    /// 获取设备的最近变更
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="fromDate">开始日期</param>
    /// <param name="toDate">结束日期</param>
    /// <returns>变更列表</returns>
    Task<List<DeviceChangeLogDto>> GetDeviceChangesAsync(
        Guid deviceId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// 记录设备变更
    /// </summary>
    /// <param name="request">变更记录请求</param>
    /// <param name="userId">用户ID</param>
    /// <returns>变更记录</returns>
    Task<DeviceChangeLogDto> RecordChangeAsync(
        CreateDeviceChangeRequest request,
        Guid userId);
}

/// <summary>
/// 相关变更DTO
/// </summary>
public class RelatedChangeDto
{
    public Guid ChangeId { get; set; }
    public Guid DeviceId { get; set; }
    public string? DeviceSn { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime ChangeDate { get; set; }
    public Dictionary<string, object> ChangeDetail { get; set; } = new();
    public Dictionary<string, object>? ImpactScope { get; set; }
    
    /// <summary>
    /// 相关性评分（0-100）
    /// </summary>
    public int RelevanceScore { get; set; }
    
    /// <summary>
    /// 相关性原因
    /// </summary>
    public List<string> RelevanceReasons { get; set; } = new();
    
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 设备变更记录DTO
/// </summary>
public class DeviceChangeLogDto
{
    public Guid ChangeId { get; set; }
    public Guid DeviceId { get; set; }
    public string? DeviceSn { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime ChangeDate { get; set; }
    public Dictionary<string, object> ChangeDetail { get; set; } = new();
    public Dictionary<string, object>? ImpactScope { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建设备变更请求
/// </summary>
public class CreateDeviceChangeRequest
{
    public Guid DeviceId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime ChangeDate { get; set; }
    public Dictionary<string, object> ChangeDetail { get; set; } = new();
    public Dictionary<string, object>? ImpactScope { get; set; }
}

