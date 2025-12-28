using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
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

    public async Task<List<DeviceDto>> GetDevicesAsync(int page = 1, int pageSize = 50, Guid? projectId = null)
    {
        var query = _dbContext.Devices.AsQueryable();

        // 如果指定了项目ID，只查询该项目下的设备
        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        var devices = await query
            .Select(d => new DeviceDto
            {
                DeviceId = d.DeviceId,
                DeviceSn = d.DeviceSn ?? string.Empty,
                DeviceName = d.DeviceName,
                ProjectId = d.ProjectId,
                TicketCount = _dbContext.Tickets.Count(t => t.DeviceId == d.DeviceId),
                LastTicketAt = _dbContext.Tickets
                    .Where(t => t.DeviceId == d.DeviceId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (DateTime?)t.CreatedAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(d => d.LastTicketAt ?? DateTime.MinValue)
            .ThenBy(d => d.DeviceName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return devices;
    }

    public async Task<DeviceDto?> GetDeviceAsync(Guid deviceId)
    {
        var device = await _dbContext.Devices
            .Where(d => d.DeviceId == deviceId)
            .Select(d => new DeviceDto
            {
                DeviceId = d.DeviceId,
                DeviceSn = d.DeviceSn ?? string.Empty,
                DeviceName = d.DeviceName,
                ProjectId = d.ProjectId,
                TicketCount = _dbContext.Tickets.Count(t => t.DeviceId == d.DeviceId),
                LastTicketAt = _dbContext.Tickets
                    .Where(t => t.DeviceId == d.DeviceId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (DateTime?)t.CreatedAt)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return device;
    }

    public async Task<List<DeviceDto>> SearchDevicesAsync(string keyword, int page = 1, int pageSize = 50, Guid? projectId = null)
    {
        var query = _dbContext.Devices.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(d => 
                (d.DeviceSn != null && d.DeviceSn.Contains(keyword)) ||
                d.DeviceName.Contains(keyword));
        }

        var devices = await query
            .Select(d => new DeviceDto
            {
                DeviceId = d.DeviceId,
                DeviceSn = d.DeviceSn ?? string.Empty,
                DeviceName = d.DeviceName,
                ProjectId = d.ProjectId,
                TicketCount = _dbContext.Tickets.Count(t => t.DeviceId == d.DeviceId),
                LastTicketAt = _dbContext.Tickets
                    .Where(t => t.DeviceId == d.DeviceId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (DateTime?)t.CreatedAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(d => d.LastTicketAt ?? DateTime.MinValue)
            .ThenBy(d => d.DeviceName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return devices;
    }
}













