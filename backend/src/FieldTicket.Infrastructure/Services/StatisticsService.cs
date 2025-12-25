using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 统计服务实现
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        ApplicationDbContext dbContext,
        ILogger<StatisticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StatisticsOverviewDto> GetOverviewAsync(
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
        var openTickets = await query.CountAsync(t => t.Status != "Closed");
        var closedTickets = await query.CountAsync(t => t.Status == "Closed");

        // 计算平均闭环时间（已关闭的工单）
        var closedTicketsWithTime = await query
            .Where(t => t.Status == "Closed" && t.ClosedAt.HasValue && t.CreatedAt != null)
            .Select(t => new
            {
                ClosureTime = t.ClosedAt!.Value - t.CreatedAt
            })
            .ToListAsync();

        TimeSpan? averageClosureTime = null;
        if (closedTicketsWithTime.Any())
        {
            averageClosureTime = TimeSpan.FromTicks(
                (long)closedTicketsWithTime.Average(ct => ct.ClosureTime.Ticks));
        }

        // 计算闭环率
        decimal? closureRate = totalTickets > 0
            ? (decimal)closedTickets / totalTickets * 100
            : null;

        // 计算重开工单率
        var reopenedTickets = await query
            .CountAsync(t => t.Status == "Reopened");
        decimal? reopenRate = closedTickets > 0
            ? (decimal)reopenedTickets / closedTickets * 100
            : null;

        return new StatisticsOverviewDto
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            ClosedTickets = closedTickets,
            AverageClosureTime = averageClosureTime,
            ClosureRate = closureRate,
            ReopenRate = reopenRate,
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    public async Task<List<DomainStatisticsDto>> GetTopDomainsAsync(
        int topN = 5,
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

        // 按问题域分组统计
        var domainStats = await query
            .GroupBy(t => t.Domain)
            .Select(g => new
            {
                Domain = g.Key,
                TicketCount = g.Count(),
                ClosedTickets = g.Where(t => t.Status == "Closed" && t.ClosedAt.HasValue && t.CreatedAt != null)
                    .Select(t => new { ClosureTime = t.ClosedAt!.Value - t.CreatedAt })
                    .ToList()
            })
            .ToListAsync();

        var result = domainStats
            .Select(ds => new DomainStatisticsDto
            {
                Domain = ds.Domain,
                DomainName = GetDomainName(ds.Domain),
                TicketCount = ds.TicketCount,
                Percentage = totalTickets > 0 ? (decimal)ds.TicketCount / totalTickets * 100 : 0,
                AverageClosureTime = ds.ClosedTickets.Any()
                    ? TimeSpan.FromTicks((long)ds.ClosedTickets.Average(ct => ct.ClosureTime.Ticks))
                    : null
            })
            .OrderByDescending(d => d.TicketCount)
            .Take(topN)
            .ToList();

        return result;
    }

    public async Task<ClosureTimeDistributionDto> GetClosureTimeDistributionAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbContext.Tickets
            .Where(t => t.Status == "Closed" && t.ClosedAt.HasValue && t.CreatedAt != null)
            .AsQueryable();

        // 时间范围筛选
        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        var closedTickets = await query
            .Select(t => new
            {
                ClosureTime = t.ClosedAt!.Value - t.CreatedAt
            })
            .ToListAsync();

        if (!closedTickets.Any())
        {
            return new ClosureTimeDistributionDto
            {
                Distribution = new List<TimeRangeCountDto>(),
                AverageTime = null,
                MedianTime = null,
                P95Time = null
            };
        }

        var totalCount = closedTickets.Count;
        var times = closedTickets.Select(ct => ct.ClosureTime).OrderBy(t => t.Ticks).ToList();

        // 计算分布
        var distribution = new List<TimeRangeCountDto>
        {
            new() { Range = "0-1天", Count = times.Count(t => t.TotalDays < 1) },
            new() { Range = "1-3天", Count = times.Count(t => t.TotalDays >= 1 && t.TotalDays < 3) },
            new() { Range = "3-7天", Count = times.Count(t => t.TotalDays >= 3 && t.TotalDays < 7) },
            new() { Range = "7-15天", Count = times.Count(t => t.TotalDays >= 7 && t.TotalDays < 15) },
            new() { Range = "15-30天", Count = times.Count(t => t.TotalDays >= 15 && t.TotalDays < 30) },
            new() { Range = "30+天", Count = times.Count(t => t.TotalDays >= 30) }
        };

        foreach (var item in distribution)
        {
            item.Percentage = totalCount > 0 ? (decimal)item.Count / totalCount * 100 : 0;
        }

        // 计算平均值、中位数、P95
        var averageTime = TimeSpan.FromTicks((long)times.Average(t => t.Ticks));
        var medianTime = times[times.Count / 2];
        var p95Index = (int)(times.Count * 0.95);
        var p95Time = p95Index < times.Count ? times[p95Index] : times.Last();

        return new ClosureTimeDistributionDto
        {
            Distribution = distribution,
            AverageTime = averageTime,
            MedianTime = medianTime,
            P95Time = p95Time
        };
    }

    public async Task<List<StatusStatisticsDto>> GetStatusStatisticsAsync(
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

        var statusStats = await query
            .GroupBy(t => t.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var result = statusStats
            .Select(ss => new StatusStatisticsDto
            {
                Status = ss.Status,
                StatusName = GetStatusName(ss.Status),
                Count = ss.Count,
                Percentage = totalTickets > 0 ? (decimal)ss.Count / totalTickets * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        return result;
    }

    public async Task<List<TrendDataPointDto>> GetTrendDataAsync(
        string metricType,
        DateTime fromDate,
        DateTime toDate,
        string groupBy = "day")
    {
        var query = _dbContext.Tickets
            .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
            .AsQueryable();

        var result = new List<TrendDataPointDto>();

        switch (metricType.ToLower())
        {
            case "tickets":
                // 按时间分组统计工单数
                if (groupBy == "day")
                {
                    var dailyStats = await query
                        .GroupBy(t => t.CreatedAt.Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .OrderBy(s => s.Date)
                        .ToListAsync();

                    result = dailyStats.Select(s => new TrendDataPointDto
                    {
                        Date = s.Date,
                        Value = s.Count,
                        Label = s.Date.ToString("yyyy-MM-dd")
                    }).ToList();
                }
                else if (groupBy == "week")
                {
                    var weeklyStats = await query
                        .GroupBy(t => new { Year = t.CreatedAt.Year, Week = GetWeekOfYear(t.CreatedAt) })
                        .Select(g => new { Year = g.Key.Year, Week = g.Key.Week, Count = g.Count(), FirstDate = g.Min(t => t.CreatedAt.Date) })
                        .OrderBy(s => s.Year).ThenBy(s => s.Week)
                        .ToListAsync();

                    result = weeklyStats.Select(s => new TrendDataPointDto
                    {
                        Date = s.FirstDate,
                        Value = s.Count,
                        Label = $"{s.Year}年第{s.Week}周"
                    }).ToList();
                }
                else if (groupBy == "month")
                {
                    var monthlyStats = await query
                        .GroupBy(t => new { Year = t.CreatedAt.Year, Month = t.CreatedAt.Month })
                        .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count(), FirstDate = g.Min(t => t.CreatedAt.Date) })
                        .OrderBy(s => s.Year).ThenBy(s => s.Month)
                        .ToListAsync();

                    result = monthlyStats.Select(s => new TrendDataPointDto
                    {
                        Date = s.FirstDate,
                        Value = s.Count,
                        Label = $"{s.Year}年{s.Month}月"
                    }).ToList();
                }
                break;

            case "closure_time":
                // 计算平均闭环时间趋势
                var closedTickets = await query
                    .Where(t => t.Status == "Closed" && t.ClosedAt.HasValue)
                    .Select(t => new
                    {
                        Date = t.CreatedAt.Date,
                        ClosureTime = (t.ClosedAt!.Value - t.CreatedAt).TotalDays
                    })
                    .ToListAsync();

                if (groupBy == "day")
                {
                    result = closedTickets
                        .GroupBy(t => t.Date)
                        .Select(g => new TrendDataPointDto
                        {
                            Date = g.Key,
                            Value = (decimal)g.Average(t => t.ClosureTime),
                            Label = g.Key.ToString("yyyy-MM-dd")
                        })
                        .OrderBy(t => t.Date)
                        .ToList();
                }
                break;

            case "resolution_rate":
                // 计算解决率趋势
                if (groupBy == "day")
                {
                    var dailyResolution = await query
                        .GroupBy(t => t.CreatedAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Total = g.Count(),
                            Resolved = g.Count(t => t.Status == "Closed")
                        })
                        .OrderBy(s => s.Date)
                        .ToListAsync();

                    result = dailyResolution.Select(s => new TrendDataPointDto
                    {
                        Date = s.Date,
                        Value = s.Total > 0 ? (decimal)s.Resolved / s.Total * 100 : 0,
                        Label = s.Date.ToString("yyyy-MM-dd")
                    }).ToList();
                }
                break;
        }

        return result;
    }

    private string GetDomainName(char domain)
    {
        return domain switch
        {
            'A' => "机械问题",
            'B' => "电气问题",
            'C' => "软件问题",
            'D' => "参数问题",
            'E' => "其他问题",
            _ => $"问题域{domain}"
        };
    }

    private string GetStatusName(string status)
    {
        return status switch
        {
            "Draft" => "草稿",
            "Submitted" => "已提交",
            "Triage" => "分诊中",
            "SolutionIssued" => "已发布方案",
            "Verifying" => "验证中",
            "Closed" => "已关闭",
            "Reopened" => "已重开",
            _ => status
        };
    }

    private int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = culture.Calendar;
        return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }
}

