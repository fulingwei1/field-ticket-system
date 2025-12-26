namespace FieldTicket.Core.Services;

/// <summary>
/// 设备服务接口
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// 获取设备列表（从工单中提取）
    /// </summary>
    Task<List<DeviceDto>> GetDevicesAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// 根据设备ID获取设备信息
    /// </summary>
    Task<DeviceDto?> GetDeviceAsync(Guid deviceId);

    /// <summary>
    /// 搜索设备（按设备SN或名称）
    /// </summary>
    Task<List<DeviceDto>> SearchDevicesAsync(string keyword, int page = 1, int pageSize = 50);
}

/// <summary>
/// 设备DTO
/// </summary>
public class DeviceDto
{
    public Guid DeviceId { get; set; }
    public string DeviceSn { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int TicketCount { get; set; }
    public DateTime? LastTicketAt { get; set; }
}










