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
        var query = from d in _dbContext.Devices
                    join p in _dbContext.Projects on d.ProjectId equals p.ProjectId into projectGroup
                    from project in projectGroup.DefaultIfEmpty()
                    join c in _dbContext.Customers on project.CustomerId equals c.CustomerId into customerGroup
                    from customer in customerGroup.DefaultIfEmpty()
                    where !projectId.HasValue || d.ProjectId == projectId.Value
                    select new
                    {
                        Device = d,
                        Project = project,
                        Customer = customer
                    };

        var devices = await query
            .Select(x => new DeviceDto
            {
                DeviceId = x.Device.DeviceId,
                DeviceSn = x.Device.DeviceSn ?? string.Empty,
                DeviceName = x.Device.DeviceName,
                ProjectId = x.Device.ProjectId,
                ProjectName = x.Project != null ? x.Project.ProjectName : null,
                CustomerId = x.Customer != null ? x.Customer.CustomerId : null,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : null,
                TicketCount = _dbContext.Tickets.Count(t => t.DeviceId == x.Device.DeviceId),
                LastTicketAt = _dbContext.Tickets
                    .Where(t => t.DeviceId == x.Device.DeviceId)
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
        var query = from d in _dbContext.Devices
                    join p in _dbContext.Projects on d.ProjectId equals p.ProjectId into projectGroup
                    from project in projectGroup.DefaultIfEmpty()
                    join c in _dbContext.Customers on project.CustomerId equals c.CustomerId into customerGroup
                    from customer in customerGroup.DefaultIfEmpty()
                    where d.DeviceId == deviceId
                    select new
                    {
                        Device = d,
                        Project = project,
                        Customer = customer
                    };

        var device = await query
            .Select(x => new DeviceDto
            {
                DeviceId = x.Device.DeviceId,
                DeviceSn = x.Device.DeviceSn ?? string.Empty,
                DeviceName = x.Device.DeviceName,
                ProjectId = x.Device.ProjectId,
                ProjectName = x.Project != null ? x.Project.ProjectName : null,
                CustomerId = x.Customer != null ? x.Customer.CustomerId : null,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : null,
                TicketCount = _dbContext.Tickets.Count(t => t.DeviceId == x.Device.DeviceId),
                LastTicketAt = _dbContext.Tickets
                    .Where(t => t.DeviceId == x.Device.DeviceId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => (DateTime?)t.CreatedAt)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return device;
    }

    public async Task<List<DeviceDto>> SearchDevicesAsync(string keyword, int page = 1, int pageSize = 50, Guid? projectId = null)
    {
        var query = from d in _dbContext.Devices
                    join p in _dbContext.Projects on d.ProjectId equals p.ProjectId into projectGroup
                    from project in projectGroup.DefaultIfEmpty()
                    join c in _dbContext.Customers on project.CustomerId equals c.CustomerId into customerGroup
                    from customer in customerGroup.DefaultIfEmpty()
                    where (!projectId.HasValue || d.ProjectId == projectId.Value) &&
                          (string.IsNullOrWhiteSpace(keyword) ||
                           (d.DeviceSn != null && d.DeviceSn.Contains(keyword)) ||
                           d.DeviceName.Contains(keyword))
                    select new
                    {
                        Device = d,
                        Project = project,
                        Customer = customer
                    };

        var devices = await query
            .Select(x => new DeviceDto
            {
                DeviceId = x.Device.DeviceId,
                DeviceSn = x.Device.DeviceSn ?? string.Empty,
                DeviceName = x.Device.DeviceName,
                ProjectId = x.Device.ProjectId,
                ProjectName = x.Project != null ? x.Project.ProjectName : null,
                CustomerId = x.Customer != null ? x.Customer.CustomerId : null,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : null,
                TicketCount = _dbContext.Tickets.Count(t => t.DeviceId == x.Device.DeviceId),
                LastTicketAt = _dbContext.Tickets
                    .Where(t => t.DeviceId == x.Device.DeviceId)
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













