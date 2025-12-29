using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 责任归因服务实现
/// </summary>
public class ResponsibilityAttributionService : IResponsibilityAttributionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ResponsibilityAttributionService> _logger;

    private static readonly Dictionary<string, string> ResponsibilityNames = new()
    {
        { "design", "设计问题" },
        { "software", "软件问题" },
        { "parameter", "参数问题" },
        { "assembly", "装配问题" },
        { "documentation", "文档问题" },
        { "other", "其他" },
        { "unknown", "未知" }
    };

    public ResponsibilityAttributionService(
        ApplicationDbContext dbContext,
        ILogger<ResponsibilityAttributionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task AttributeResponsibilityAsync(
        Guid ticketId,
        string rootResponsibility,
        bool isPreventable,
        string? notes,
        Guid userId)
    {
        _logger.LogInformation("Attributing responsibility for ticket {TicketId}: {Responsibility}, Preventable: {IsPreventable}",
            ticketId, rootResponsibility, isPreventable);

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 验证根因分类
        if (!ResponsibilityNames.ContainsKey(rootResponsibility))
        {
            throw new ArgumentException($"无效的根因分类: {rootResponsibility}");
        }

        ticket.RootResponsibility = rootResponsibility;
        ticket.IsPreventable = isPreventable;
        ticket.ResponsibilityNotes = notes;
        ticket.AttributedBy = userId;
        ticket.AttributedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Responsibility attributed successfully for ticket {TicketId}", ticketId);
    }

    public async Task<ResponsibilityAttributionDto?> GetAttributionAsync(Guid ticketId)
    {
        var ticket = await _dbContext.Tickets
            .Where(t => t.TicketId == ticketId)
            .Select(t => new
            {
                t.TicketId,
                t.RootResponsibility,
                t.IsPreventable,
                t.ResponsibilityNotes,
                t.AttributedBy,
                t.AttributedAt
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
        {
            return null;
        }

        var dto = new ResponsibilityAttributionDto
        {
            TicketId = ticket.TicketId,
            RootResponsibility = ticket.RootResponsibility,
            RootResponsibilityName = ticket.RootResponsibility != null
                ? ResponsibilityNames.GetValueOrDefault(ticket.RootResponsibility, ticket.RootResponsibility)
                : null,
            IsPreventable = ticket.IsPreventable,
            ResponsibilityNotes = ticket.ResponsibilityNotes,
            AttributedBy = ticket.AttributedBy,
            AttributedAt = ticket.AttributedAt
        };

        // 获取归因人名称
        if (ticket.AttributedBy.HasValue)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == ticket.AttributedBy.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            dto.AttributedByName = user;
        }

        return dto;
    }

    public async Task<ResponsibilityStatisticsDto> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbContext.Tickets.AsQueryable();

        // 时间范围筛选
        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        var totalTickets = await query.CountAsync();
        var attributedTickets = await query
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
            .CountAsync();

        // 责任分布统计
        var distribution = await query
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
            .GroupBy(t => t.RootResponsibility!)
            .Select(g => new
            {
                RootResponsibility = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var distributionList = distribution
            .Select(d => new ResponsibilityDistributionDto
            {
                RootResponsibility = d.RootResponsibility,
                RootResponsibilityName = ResponsibilityNames.GetValueOrDefault(d.RootResponsibility, d.RootResponsibility),
                Count = d.Count,
                Percentage = attributedTickets > 0 ? (decimal)d.Count / attributedTickets * 100 : 0
            })
            .OrderByDescending(d => d.Count)
            .ToList();

        // 可预防性统计
        var preventabilityStats = await query
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility) && t.IsPreventable.HasValue)
            .GroupBy(t => t.IsPreventable!.Value)
            .Select(g => new
            {
                IsPreventable = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var preventabilityList = preventabilityStats
            .Select(p => new PreventabilityStatisticsDto
            {
                IsPreventable = p.IsPreventable,
                Label = p.IsPreventable ? "可预防" : "不可预防",
                Count = p.Count,
                Percentage = attributedTickets > 0 ? (decimal)p.Count / attributedTickets * 100 : 0
            })
            .ToList();

        return new ResponsibilityStatisticsDto
        {
            Distribution = distributionList,
            PreventabilityStats = preventabilityList,
            TotalAttributed = attributedTickets,
            TotalTickets = totalTickets,
            AttributionRate = totalTickets > 0 ? (decimal)attributedTickets / totalTickets * 100 : 0
        };
    }
}



















