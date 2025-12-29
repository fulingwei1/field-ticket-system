namespace FieldTicket.Domain.Entities;

/// <summary>
/// 设备实体
/// </summary>
public class Device
{
    public Guid DeviceId { get; set; }
    public string? DeviceSn { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? DeviceType { get; set; }
    public string? Model { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}








