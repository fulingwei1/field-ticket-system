using FieldTicket.Core.Services;
using FieldTicket.Core.Validators;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单服务实现
/// </summary>
public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TicketValidator _validator;
    private readonly TicketNumberService _ticketNumberService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TicketService> _logger;
    private readonly IWeComNotificationService? _notificationService;
    private readonly INotificationRuleService? _notificationRuleService;
    private readonly IDeviceConfigSnapshotService? _configSnapshotService;
    private readonly ITicketStatusHistoryService? _statusHistoryService;

    public TicketService(
        ApplicationDbContext dbContext,
        TicketValidator validator,
        TicketNumberService ticketNumberService,
        IDistributedCache cache,
        ILogger<TicketService> logger,
        IWeComNotificationService? notificationService = null,
        INotificationRuleService? notificationRuleService = null,
        IDeviceConfigSnapshotService? configSnapshotService = null,
        ITicketStatusHistoryService? statusHistoryService = null)
    {
        _dbContext = dbContext;
        _validator = validator;
        _ticketNumberService = ticketNumberService;
        _cache = cache;
        _logger = logger;
        _notificationService = notificationService;
        _notificationRuleService = notificationRuleService;
        _configSnapshotService = configSnapshotService;
        _statusHistoryService = statusHistoryService;
    }

    public async Task<TicketDto> CreateDraftAsync(CreateTicketRequest request, Guid userId, string? idempotencyKey = null)
    {
        // 幂等性检查
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingTicket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

            if (existingTicket != null)
            {
                return await MapToDtoAsync(existingTicket);
            }
        }

        // 从同一设备的其他工单中获取 CustomerId 和 ProjectId
        var (customerId, projectId) = await GetCustomerAndProjectFromDeviceAsync(request.DeviceId);

        // 生成工单编号（草稿时先不生成，提交时生成）
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            TicketNo = string.Empty, // 提交时生成
            CustomerId = customerId,
            ProjectId = projectId,
            DeviceId = request.DeviceId,
            StationId = request.StationId,
            CreatedByUserId = userId,
            Domain = request.Domain,
            StepCode = request.StepCode,
            StepName = request.StepName,
            SymptomTitle = request.SymptomTitle,
            SymptomDetail = request.SymptomDetail,
            ReproRate = request.ReproRate,
            RebootRecovers = request.RebootRecovers,
            EnvRelated = request.EnvRelated,
            SwVersion = request.SwVersion,
            PlcVersion = request.PlcVersion,
            ParamVersion = request.ParamVersion,
            FactsJson = request.FactsJson,
            ActionsTaken = request.ActionsTaken,
            ActionsTakenNote = request.ActionsTakenNote,
            AlarmCode = request.AlarmCode,
            ConfirmedAsFact = request.ConfirmedAsFact,
            Status = "Draft",
            Priority = "P3",
            LocalDraftId = request.LocalDraftId,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.ConfirmedAsFact)
        {
            ticket.ConfirmedAt = DateTime.UtcNow;
        }

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created draft ticket {TicketId} by user {UserId}", ticket.TicketId, userId);

        // 异步检测配置差异（不阻塞工单创建）
        if (_configSnapshotService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // 构建当前配置
                    var currentConfig = new Dictionary<string, object>
                    {
                        ["sw_version"] = ticket.SwVersion,
                        ["plc_version"] = ticket.PlcVersion,
                        ["param_version"] = ticket.ParamVersion
                    };

                    // 检查配置差异
                    var differences = await _configSnapshotService.CheckConfigDifferencesAsync(
                        ticket.DeviceId, currentConfig);

                    if (differences.Any())
                    {
                        _logger.LogInformation("工单 {TicketId} 检测到 {Count} 个配置差异", 
                            ticket.TicketId, differences.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检测配置差异失败，工单：{TicketId}", ticket.TicketId);
                }
            });
        }

        return await MapToDtoAsync(ticket);
    }

    public async Task<TicketDto> UpdateDraftAsync(Guid ticketId, UpdateTicketRequest request, Guid userId)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.CreatedByUserId == userId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"Ticket {ticketId} not found");
        }

        if (ticket.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft tickets can be updated");
        }

        // 更新字段
        if (request.Domain.HasValue) ticket.Domain = request.Domain.Value;
        if (!string.IsNullOrEmpty(request.StepCode)) ticket.StepCode = request.StepCode;
        if (request.StepName != null) ticket.StepName = request.StepName;
        if (!string.IsNullOrEmpty(request.SymptomTitle)) ticket.SymptomTitle = request.SymptomTitle;
        if (request.SymptomDetail != null) ticket.SymptomDetail = request.SymptomDetail;
        if (request.ReproRate.HasValue) ticket.ReproRate = request.ReproRate;
        if (request.RebootRecovers.HasValue) ticket.RebootRecovers = request.RebootRecovers;
        if (request.EnvRelated.HasValue) ticket.EnvRelated = request.EnvRelated;
        if (!string.IsNullOrEmpty(request.SwVersion)) ticket.SwVersion = request.SwVersion;
        if (!string.IsNullOrEmpty(request.PlcVersion)) ticket.PlcVersion = request.PlcVersion;
        if (!string.IsNullOrEmpty(request.ParamVersion)) ticket.ParamVersion = request.ParamVersion;
        if (request.FactsJson != null) ticket.FactsJson = request.FactsJson;
        if (request.ActionsTaken != null) ticket.ActionsTaken = request.ActionsTaken;
        if (request.ActionsTakenNote != null) ticket.ActionsTakenNote = request.ActionsTakenNote;
        if (request.AlarmCode != null) ticket.AlarmCode = request.AlarmCode;
        if (request.ConfirmedAsFact.HasValue)
        {
            ticket.ConfirmedAsFact = request.ConfirmedAsFact.Value;
            if (request.ConfirmedAsFact.Value)
            {
                ticket.ConfirmedAt = DateTime.UtcNow;
            }
        }

        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated draft ticket {TicketId} by user {UserId}", ticketId, userId);

        return await MapToDtoAsync(ticket);
    }

    public async Task<TicketDto> SubmitTicketAsync(Guid ticketId, Guid userId)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.CreatedByUserId == userId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"Ticket {ticketId} not found");
        }

        if (ticket.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft tickets can be submitted");
        }

        // 获取附件数量
        var attachmentCount = await _dbContext.Attachments
            .CountAsync(a => a.TicketId == ticket.TicketId);

        // 转换为 DTO 进行校验
        var ticketDto = await MapToDtoAsync(ticket);
        ticketDto.AttachmentCount = attachmentCount;

        // 校验
        var validationResult = _validator.ValidateForSubmit(ticketDto, attachmentCount);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Ticket validation failed", validationResult.Errors);
        }

        // 生成工单编号
        if (string.IsNullOrEmpty(ticket.TicketNo))
        {
            ticket.TicketNo = await _ticketNumberService.GenerateTicketNumberAsync();
        }

        // 记录状态变更
        var oldStatus = ticket.Status;
        
        // 更新状态
        ticket.Status = "Submitted";
        ticket.SubmittedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // 记录状态历史（异步，不阻塞主流程）
        if (_statusHistoryService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _statusHistoryService.RecordStatusChangeAsync(
                        ticketId: ticketId,
                        fromStatus: oldStatus,
                        toStatus: "Submitted",
                        changedBy: userId,
                        changeReason: "工单提交",
                        changeType: "manual"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "记录工单状态历史失败，工单 {TicketId}", ticketId);
                }
            });
        }

        _logger.LogInformation("Submitted ticket {TicketId} ({TicketNo}) by user {UserId}", 
            ticketId, ticket.TicketNo, userId);

        // 发送企业微信通知（异步，不阻塞主流程）
        // 优先使用通知规则系统，如果没有规则则使用默认通知
        _ = Task.Run(async () =>
        {
            try
            {
                // 优先使用通知规则系统
                if (_notificationRuleService != null)
                {
                    var notificationResult = await _notificationRuleService.ExecuteNotificationAsync(
                        ticketId,
                        "ticket_submitted",
                        new Dictionary<string, object>
                        {
                            ["ticket"] = ticket
                        });

                    if (notificationResult.SentCount > 0)
                    {
                        _logger.LogInformation("通过通知规则发送通知成功，工单 {TicketId}，发送数：{SentCount}",
                            ticketId, notificationResult.SentCount);
                        return;
                    }
                }

                // 回退到默认通知（如果没有配置通知规则）
                if (_notificationService != null)
                {
                    await _notificationService.NotifyTicketSubmittedAsync(ticketId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送工单提交通知失败，工单ID：{TicketId}", ticketId);
            }
        });

        return await MapToDtoAsync(ticket);
    }

    public async Task<TicketDto?> GetTicketAsync(Guid ticketId, Guid? userId = null)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return null;
        }

        // 权限检查：FieldEngineer 只能看自己创建的工单
        if (userId.HasValue && ticket.CreatedByUserId != userId.Value)
        {
            // 检查用户角色，如果是 FieldEngineer 则返回 null
            var user = await _dbContext.Users.FindAsync(userId.Value);
            if (user != null && user.Role == "FieldEngineer")
            {
                // FieldEngineer 只能查看自己创建的工单
                return null;
            }
            // 其他角色（Manager、Admin等）可以查看所有工单
        }

        return await MapToDtoAsync(ticket);
    }

    public async Task<(List<TicketListItemDto> Items, int Total)> GetTicketsAsync(
        TicketQueryFilter filter,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbContext.Tickets.AsQueryable();

        // 应用过滤器
        if (filter.Statuses != null && filter.Statuses.Any())
        {
            query = query.Where(t => filter.Statuses.Contains(t.Status));
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(t => t.CustomerId == filter.CustomerId.Value);
        }

        if (filter.Domain.HasValue)
        {
            query = query.Where(t => t.Domain == filter.Domain.Value);
        }

        if (!string.IsNullOrEmpty(filter.Priority))
        {
            query = query.Where(t => t.Priority == filter.Priority);
        }

        if (filter.CreatedBy.HasValue)
        {
            query = query.Where(t => t.CreatedByUserId == filter.CreatedBy.Value);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.DateTo.Value);
        }

        var total = await query.CountAsync();

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 批量查询关联数据以优化性能
        var ticketIds = tickets.Select(t => t.TicketId).ToList();
        var customerIds = tickets.Where(t => t.CustomerId != Guid.Empty).Select(t => t.CustomerId).Distinct().ToList();
        var projectIds = tickets.Where(t => t.ProjectId != Guid.Empty).Select(t => t.ProjectId).Distinct().ToList();
        var deviceIds = tickets.Select(t => t.DeviceId).Distinct().ToList();
        var userIds = tickets.Select(t => t.CreatedByUserId).Distinct().ToList();

        // 查询 Projects（用于获取 CustomerName）
        var projects = await _dbContext.Projects
            .Where(p => projectIds.Contains(p.ProjectId))
            .Select(p => new { p.ProjectId, p.CustomerName })
            .ToListAsync();

        var projectDict = projects.ToDictionary(p => p.ProjectId, p => p.CustomerName ?? string.Empty);

        // 查询 Users（用于获取 CreatedByName）
        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();

        var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? string.Empty);

        // 查询同一设备的其他工单（用于获取 DeviceSn）
        var deviceSnDict = new Dictionary<Guid, string>();
        var ticketsWithoutDeviceSn = tickets.Where(t => string.IsNullOrEmpty(t.DeviceSn)).ToList();
        if (ticketsWithoutDeviceSn.Any())
        {
            var deviceIdsToQuery = ticketsWithoutDeviceSn.Select(t => t.DeviceId).Distinct().ToList();
            var deviceSnFromTickets = await _dbContext.Tickets
                .Where(t => deviceIdsToQuery.Contains(t.DeviceId) && !string.IsNullOrEmpty(t.DeviceSn))
                .GroupBy(t => t.DeviceId)
                .Select(g => new { DeviceId = g.Key, DeviceSn = g.OrderByDescending(t => t.CreatedAt).First().DeviceSn })
                .ToListAsync();

            foreach (var item in deviceSnFromTickets)
            {
                deviceSnDict[item.DeviceId] = item.DeviceSn ?? string.Empty;
            }
        }

        var items = tickets.Select(t => new TicketListItemDto
        {
            TicketId = t.TicketId,
            TicketNo = t.TicketNo,
            CustomerName = !string.IsNullOrEmpty(t.CustomerName)
                ? t.CustomerName
                : (projectDict.TryGetValue(t.ProjectId, out var projectCustomerName) ? projectCustomerName : string.Empty),
            DeviceSn = !string.IsNullOrEmpty(t.DeviceSn)
                ? t.DeviceSn
                : (deviceSnDict.TryGetValue(t.DeviceId, out var deviceSn) ? deviceSn : string.Empty),
            Domain = t.Domain,
            StepCode = t.StepCode,
            SymptomTitle = t.SymptomTitle,
            Status = t.Status,
            Priority = t.Priority,
            CreatedByName = userDict.TryGetValue(t.CreatedByUserId, out var userName) ? userName : string.Empty,
            CreatedAt = t.CreatedAt,
            SubmittedAt = t.SubmittedAt,
            Tags = t.Tags ?? new List<string>()
        }).ToList();

        return (items, total);
    }

    /// <summary>
    /// 从同一设备的其他工单中获取 CustomerId 和 ProjectId
    /// </summary>
    private async Task<(Guid CustomerId, Guid ProjectId)> GetCustomerAndProjectFromDeviceAsync(Guid deviceId)
    {
        // 从同一设备的最新工单中获取 CustomerId 和 ProjectId
        var latestTicket = await _dbContext.Tickets
            .Where(t => t.DeviceId == deviceId &&
                       t.CustomerId != Guid.Empty &&
                       t.ProjectId != Guid.Empty)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestTicket != null)
        {
            return (latestTicket.CustomerId, latestTicket.ProjectId);
        }

        // 如果没有找到，返回空 GUID
        return (Guid.Empty, Guid.Empty);
    }

    private async Task<TicketDto> MapToDtoAsync(Ticket ticket)
    {
        var attachmentCount = ticket.TicketId != Guid.Empty 
            ? await _dbContext.Attachments.CountAsync(a => a.TicketId == ticket.TicketId)
            : 0;

        return new TicketDto
        {
            TicketId = ticket.TicketId,
            TicketNo = ticket.TicketNo,
            CustomerId = ticket.CustomerId,
            ProjectId = ticket.ProjectId,
            DeviceId = ticket.DeviceId,
            StationId = ticket.StationId,
            CreatedByUserId = ticket.CreatedByUserId,
            Domain = ticket.Domain,
            StepCode = ticket.StepCode,
            StepName = ticket.StepName,
            SymptomTitle = ticket.SymptomTitle,
            SymptomDetail = ticket.SymptomDetail,
            ReproRate = ticket.ReproRate,
            RebootRecovers = ticket.RebootRecovers,
            EnvRelated = ticket.EnvRelated,
            SwVersion = ticket.SwVersion,
            PlcVersion = ticket.PlcVersion,
            ParamVersion = ticket.ParamVersion,
            FactsJson = ticket.FactsJson,
            ActionsTaken = ticket.ActionsTaken,
            ActionsTakenNote = ticket.ActionsTakenNote,
            AlarmCode = ticket.AlarmCode,
            ConfirmedAsFact = ticket.ConfirmedAsFact,
            ConfirmedAt = ticket.ConfirmedAt,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CurrentJcCode = ticket.CurrentJcCode,
            AssignedTo = ticket.AssignedTo,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            SubmittedAt = ticket.SubmittedAt,
            ClosedAt = ticket.ClosedAt,
            AttachmentCount = attachmentCount,
            Tags = ticket.Tags ?? new List<string>()
        };
    }
}

/// <summary>
/// 校验异常
/// </summary>
public class ValidationException : Exception
{
    public List<ValidationError> Errors { get; }

    public ValidationException(string message, List<ValidationError> errors) : base(message)
    {
        Errors = errors;
    }
}

