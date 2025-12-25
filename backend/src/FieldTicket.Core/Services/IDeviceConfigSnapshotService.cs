namespace FieldTicket.Core.Services;

/// <summary>
/// 设备配置快照服务接口
/// </summary>
public interface IDeviceConfigSnapshotService
{
    /// <summary>
    /// 创建配置快照
    /// </summary>
    /// <param name="request">快照创建请求</param>
    /// <param name="userId">用户ID</param>
    /// <returns>快照ID</returns>
    Task<Guid> CreateSnapshotAsync(
        CreateConfigSnapshotRequest request,
        Guid userId);

    /// <summary>
    /// 检查配置差异
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="currentConfig">当前配置</param>
    /// <returns>差异列表</returns>
    Task<List<ConfigDifference>> CheckConfigDifferencesAsync(
        Guid deviceId,
        Dictionary<string, object>? currentConfig = null);

    /// <summary>
    /// 获取设备的配置快照历史
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="snapshotType">快照类型（可选）</param>
    /// <param name="limit">限制数量</param>
    /// <returns>快照列表</returns>
    Task<List<ConfigSnapshotDto>> GetSnapshotHistoryAsync(
        Guid deviceId,
        string? snapshotType = null,
        int limit = 50);

    /// <summary>
    /// 获取标准配置
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>标准配置</returns>
    Task<Dictionary<string, object>?> GetStandardConfigAsync(Guid deviceId);

    /// <summary>
    /// 获取当前配置
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>当前配置</returns>
    Task<Dictionary<string, object>> GetCurrentConfigAsync(Guid deviceId);

    /// <summary>
    /// 对比两个配置
    /// </summary>
    /// <param name="config1">配置1</param>
    /// <param name="config2">配置2</param>
    /// <returns>差异列表</returns>
    List<ConfigDifference> CompareConfigs(
        Dictionary<string, object> config1,
        Dictionary<string, object> config2);
}

/// <summary>
/// 创建配置快照请求
/// </summary>
public class CreateConfigSnapshotRequest
{
    public Guid DeviceId { get; set; }
    public string SnapshotType { get; set; } = string.Empty; // delivery, change, problem
    public DateTime SnapshotAt { get; set; }
    public Dictionary<string, object> ConfigJson { get; set; } = new();
    public Dictionary<string, object>? StandardConfigJson { get; set; }
    public List<ConfigDifference>? Differences { get; set; }
}

/// <summary>
/// 配置差异
/// </summary>
public class ConfigDifference
{
    public string Field { get; set; } = string.Empty;
    public object? Standard { get; set; }
    public object? Actual { get; set; }
    public string? Impact { get; set; }
    public string DifferenceType { get; set; } = string.Empty; // added, removed, changed
}

/// <summary>
/// 配置快照DTO
/// </summary>
public class ConfigSnapshotDto
{
    public Guid SnapshotId { get; set; }
    public Guid DeviceId { get; set; }
    public string SnapshotType { get; set; } = string.Empty;
    public DateTime SnapshotAt { get; set; }
    public Dictionary<string, object> ConfigJson { get; set; } = new();
    public Dictionary<string, object>? StandardConfigJson { get; set; }
    public List<ConfigDifference>? Differences { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

