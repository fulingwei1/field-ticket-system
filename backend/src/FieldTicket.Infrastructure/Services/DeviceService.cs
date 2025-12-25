using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 设备服务实现
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(
        ApplicationDbContext dbContext,
        ILogger<DeviceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<DeviceDto>> GetDevicesAsync(int page = 1, int pageSize = 50)
    {
        // 从工单中提取设备信息
        var devices = await _dbContext.Tickets
            .Where(t => !string.IsNullOrEmpty(t.DeviceSn))
            .GroupBy(t => new { t.DeviceId, t.DeviceSn })
            .Select(g => new DeviceDto
            {
                DeviceId = g.Key.DeviceId,
                DeviceSn = g.Key.DeviceSn ?? string.Empty,
                DeviceName = g.Key.DeviceSn, // 使用设备SN作为名称
                TicketCount = g.Count(),
                LastTicketAt = g.Max(t => t.CreatedAt)
            })
            .OrderByDescending(d => d.LastTicketAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return devices;
    }

    public async Task<DeviceDto?> GetDeviceAsync(Guid deviceId)
    {
        var device = await _dbContext.Tickets
            .Where(t => t.DeviceId == deviceId)
            .GroupBy(t => new { t.DeviceId, t.DeviceSn })
            .Select(g => new DeviceDto
            {
                DeviceId = g.Key.DeviceId,
                DeviceSn = g.Key.DeviceSn ?? string.Empty,
                DeviceName = g.Key.DeviceSn,
                TicketCount = g.Count(),
                LastTicketAt = g.Max(t => t.CreatedAt)
            })
            .FirstOrDefaultAsync();

        return device;
    }

    public async Task<List<DeviceDto>> SearchDevicesAsync(string keyword, int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await GetDevicesAsync(page, pageSize);
        }

        var devices = await _dbContext.Tickets
            .Where(t => !string.IsNullOrEmpty(t.DeviceSn) &&
                       (t.DeviceSn.Contains(keyword) || 
                        (t.DeviceName != null && t.DeviceName.Contains(keyword))))
            .GroupBy(t => new { t.DeviceId, t.DeviceSn })
            .Select(g => new DeviceDto
            {
                DeviceId = g.Key.DeviceId,
                DeviceSn = g.Key.DeviceSn ?? string.Empty,
                DeviceName = g.Key.DeviceSn,
                TicketCount = g.Count(),
                LastTicketAt = g.Max(t => t.CreatedAt)
            })
            .OrderByDescending(d => d.LastTicketAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return devices;
    }
}







