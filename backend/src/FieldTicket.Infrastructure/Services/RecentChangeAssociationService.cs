using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 最近变更自动关联服务实现
/// </summary>
public class RecentChangeAssociationService : IRecentChangeAssociationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RecentChangeAssociationService> _logger;

    public RecentChangeAssociationService(
        ApplicationDbContext dbContext,
        ILogger<RecentChangeAssociationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<RelatedChangeDto>> GetRelatedChangesAsync(
        Guid ticketId,
        int daysBefore = 7,
        int daysAfter = 7)
    {
        // 获取工单信息
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 确定时间范围
        var ticketDate = ticket.SubmittedAt ?? ticket.CreatedAt;
        var fromDate = ticketDate.AddDays(-daysBefore);
        var toDate = ticketDate.AddDays(daysAfter);

        // 查找设备的最近变更
        var changes = await _dbContext.DeviceChangeLogs
            .Where(cl => cl.DeviceId == ticket.DeviceId &&
                        cl.ChangeDate >= fromDate &&
                        cl.ChangeDate <= toDate)
            .OrderByDescending(cl => cl.ChangeDate)
            .ToListAsync();

        // 计算相关性评分
        var relatedChanges = new List<RelatedChangeDto>();

        foreach (var change in changes)
        {
            var relevance = CalculateRelevance(change, ticket);
            if (relevance.Score > 0) // 只返回有相关性的变更
            {
                relatedChanges.Add(new RelatedChangeDto
                {
                    ChangeId = change.ChangeId,
                    DeviceId = change.DeviceId,
                    ChangeType = change.ChangeType,
                    ChangeDate = change.ChangeDate,
                    ChangeDetail = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        change.ChangeDetail.RootElement.GetRawText()) ?? new(),
                    ImpactScope = change.ImpactScope != null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                            change.ImpactScope.RootElement.GetRawText())
                        : null,
                    RelevanceScore = relevance.Score,
                    RelevanceReasons = relevance.Reasons,
                    CreatedBy = change.CreatedBy,
                    CreatedAt = change.CreatedAt
                });
            }
        }

        // 按相关性评分排序（高相关性置顶）
        relatedChanges = relatedChanges
            .OrderByDescending(rc => rc.RelevanceScore)
            .ThenByDescending(rc => rc.ChangeDate)
            .ToList();

        _logger.LogInformation("工单 {TicketId} 找到 {Count} 个相关变更", ticketId, relatedChanges.Count);

        return relatedChanges;
    }

    public async Task<List<DeviceChangeLogDto>> GetDeviceChangesAsync(
        Guid deviceId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbContext.DeviceChangeLogs
            .Where(cl => cl.DeviceId == deviceId);

        if (fromDate.HasValue)
        {
            query = query.Where(cl => cl.ChangeDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(cl => cl.ChangeDate <= toDate.Value);
        }

        var changes = await query
            .OrderByDescending(cl => cl.ChangeDate)
            .ToListAsync();

        return changes.Select(MapToDto).ToList();
    }

    public async Task<DeviceChangeLogDto> RecordChangeAsync(
        CreateDeviceChangeRequest request,
        Guid userId)
    {
        var changeLog = new DeviceChangeLog
        {
            ChangeId = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            ChangeType = request.ChangeType,
            ChangeDate = request.ChangeDate,
            ChangeDetail = JsonDocument.Parse(JsonSerializer.Serialize(request.ChangeDetail)),
            ImpactScope = request.ImpactScope != null
                ? JsonDocument.Parse(JsonSerializer.Serialize(request.ImpactScope))
                : null,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.DeviceChangeLogs.Add(changeLog);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("记录设备 {DeviceId} 变更，类型：{ChangeType}，时间：{ChangeDate}",
            request.DeviceId, request.ChangeType, request.ChangeDate);

        return MapToDto(changeLog);
    }

    /// <summary>
    /// 计算变更与工单的相关性
    /// </summary>
    private (int Score, List<string> Reasons) CalculateRelevance(
        DeviceChangeLog change,
        Ticket ticket)
    {
        int score = 0;
        var reasons = new List<string>();

        var changeDetail = JsonSerializer.Deserialize<Dictionary<string, object>>(
            change.ChangeDetail.RootElement.GetRawText()) ?? new();

        // 1. 版本匹配（高相关性）
        if (changeDetail.TryGetValue("sw_version", out var swVersionObj))
        {
            var swVersion = swVersionObj?.ToString();
            if (swVersion != null && swVersion.Contains(ticket.SwVersion))
            {
                score += 30;
                reasons.Add($"软件版本相关：{swVersion}");
            }
        }

        if (changeDetail.TryGetValue("plc_version", out var plcVersionObj))
        {
            var plcVersion = plcVersionObj?.ToString();
            if (plcVersion != null && plcVersion.Contains(ticket.PlcVersion))
            {
                score += 30;
                reasons.Add($"PLC版本相关：{plcVersion}");
            }
        }

        if (changeDetail.TryGetValue("param_version", out var paramVersionObj))
        {
            var paramVersion = paramVersionObj?.ToString();
            if (paramVersion != null && paramVersion.Contains(ticket.ParamVersion))
            {
                score += 20;
                reasons.Add($"参数版本相关：{paramVersion}");
            }
        }

        // 2. 问题域匹配（中相关性）
        if (change.ImpactScope != null)
        {
            var impactScope = JsonSerializer.Deserialize<Dictionary<string, object>>(
                change.ImpactScope.RootElement.GetRawText()) ?? new();

            if (impactScope.TryGetValue("affected_domains", out var domainsObj))
            {
                var domains = domainsObj?.ToString();
                if (domains != null && domains.Contains(ticket.Domain))
                {
                    score += 15;
                    reasons.Add($"问题域匹配：{ticket.Domain}");
                }
            }

            if (impactScope.TryGetValue("affected_steps", out var stepsObj))
            {
                var steps = stepsObj?.ToString();
                if (steps != null && steps.Contains(ticket.StepCode))
                {
                    score += 15;
                    reasons.Add($"步骤代码匹配：{ticket.StepCode}");
                }
            }
        }

        // 3. 时间接近度（低相关性）
        var ticketDate = ticket.SubmittedAt ?? ticket.CreatedAt;
        var daysDiff = Math.Abs((change.ChangeDate - ticketDate).TotalDays);
        if (daysDiff <= 1)
        {
            score += 10;
            reasons.Add($"时间接近：{Math.Round(daysDiff * 24)} 小时内");
        }
        else if (daysDiff <= 3)
        {
            score += 5;
            reasons.Add($"时间接近：{Math.Round(daysDiff)} 天");
        }

        // 4. 变更类型相关性（中相关性）
        switch (change.ChangeType)
        {
            case "program_upgrade":
                if (ticket.Domain == 'C') // PLC/程序域
                {
                    score += 10;
                    reasons.Add("程序升级与PLC问题相关");
                }
                break;
            case "param_change":
                if (ticket.Domain == 'C' || ticket.Domain == 'D') // PLC或测试域
                {
                    score += 10;
                    reasons.Add("参数变更与问题相关");
                }
                break;
            case "component_replacement":
                if (ticket.Domain == 'A' || ticket.Domain == 'B') // 机械或电气域
                {
                    score += 10;
                    reasons.Add("换件与机械/电气问题相关");
                }
                break;
        }

        return (Math.Min(100, score), reasons);
    }

    /// <summary>
    /// 映射到DTO
    /// </summary>
    private DeviceChangeLogDto MapToDto(DeviceChangeLog change)
    {
        return new DeviceChangeLogDto
        {
            ChangeId = change.ChangeId,
            DeviceId = change.DeviceId,
            ChangeType = change.ChangeType,
            ChangeDate = change.ChangeDate,
            ChangeDetail = JsonSerializer.Deserialize<Dictionary<string, object>>(
                change.ChangeDetail.RootElement.GetRawText()) ?? new(),
            ImpactScope = change.ImpactScope != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                    change.ImpactScope.RootElement.GetRawText())
                : null,
            CreatedBy = change.CreatedBy,
            CreatedAt = change.CreatedAt
        };
    }
}

