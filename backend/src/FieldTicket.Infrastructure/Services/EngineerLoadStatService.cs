using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工程师负载统计服务实现
/// </summary>
public class EngineerLoadStatService : IEngineerLoadStatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EngineerLoadStatService> _logger;

    public EngineerLoadStatService(
        ApplicationDbContext dbContext,
        ILogger<EngineerLoadStatService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EngineerLoadStatDto> CalculateAndUpdateStatsAsync(
        Guid engineerId,
        DateOnly statDate)
    {
        _logger.LogInformation("计算工程师 {EngineerId} 在 {StatDate} 的负载统计", engineerId, statDate);

        // 获取或创建统计记录
        var stat = await _dbContext.EngineerLoadStats
            .FirstOrDefaultAsync(s => s.EngineerId == engineerId && s.StatDate == statDate);

        if (stat == null)
        {
            stat = new EngineerLoadStat
            {
                StatId = Guid.NewGuid(),
                EngineerId = engineerId,
                StatDate = statDate,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.EngineerLoadStats.Add(stat);
        }

        var startDate = statDate.ToDateTime(TimeOnly.MinValue);
        var endDate = statDate.ToDateTime(TimeOnly.MaxValue);

        // 统计被@次数（从工单评论中统计，目前系统可能没有评论表，暂时返回0）
        stat.MentionedCount = 0; // TODO: 实现评论表的@统计

        // 统计升级接手次数（从分诊记录中统计）
        stat.EscalationTakenCount = await _dbContext.TriageNotes
            .Where(tn => tn.EscalatedTo == engineerId &&
                         tn.CreatedAt >= startDate &&
                         tn.CreatedAt <= endDate)
            .CountAsync();

        // 统计判断被复用次数（从判断卡使用历史中统计）
        stat.JudgementReusedCount = await _dbContext.JudgementCardUsageHistories
            .Where(jcuh => jcuh.UsedBy == engineerId &&
                          jcuh.UsedAt >= startDate &&
                          jcuh.UsedAt <= endDate)
            .CountAsync();

        // 统计低置信度工单接手次数（从分诊记录中统计，置信度<3）
        stat.LowConfidenceTakenCount = await _dbContext.TriageNotes
            .Where(tn => tn.CreatedBy == engineerId &&
                         tn.Confidence < 3 &&
                         tn.CreatedAt >= startDate &&
                         tn.CreatedAt <= endDate)
            .CountAsync();

        // 统计分配的工单数
        stat.TicketsAssigned = await _dbContext.Tickets
            .Where(t => t.AssignedTo == engineerId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
            .CountAsync();

        // 统计关闭的工单数
        stat.TicketsClosed = await _dbContext.Tickets
            .Where(t => t.AssignedTo == engineerId &&
                       t.Status == "Closed" &&
                       t.ClosedAt.HasValue &&
                       t.ClosedAt.Value >= startDate &&
                       t.ClosedAt.Value <= endDate)
            .CountAsync();

        await _dbContext.SaveChangesAsync();

        // 转换为DTO
        var engineer = await _dbContext.Users.FindAsync(engineerId);
        var dto = new EngineerLoadStatDto
        {
            StatId = stat.StatId,
            EngineerId = stat.EngineerId,
            EngineerName = engineer?.Name,
            StatDate = stat.StatDate,
            MentionedCount = stat.MentionedCount,
            EscalationTakenCount = stat.EscalationTakenCount,
            JudgementReusedCount = stat.JudgementReusedCount,
            LowConfidenceTakenCount = stat.LowConfidenceTakenCount,
            TicketsAssigned = stat.TicketsAssigned,
            TicketsClosed = stat.TicketsClosed,
            TotalLoadScore = CalculateTotalLoadScore(stat)
        };

        return dto;
    }

    public async Task<EngineerLoadReportDto> GetPersonalLoadReportAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var stats = await _dbContext.EngineerLoadStats
            .Where(s => s.EngineerId == engineerId &&
                       s.StatDate >= startDate &&
                       s.StatDate <= endDate)
            .OrderBy(s => s.StatDate)
            .ToListAsync();

        var engineer = await _dbContext.Users.FindAsync(engineerId);

        // 计算总计
        var totalStat = new EngineerLoadStat
        {
            MentionedCount = stats.Sum(s => s.MentionedCount),
            EscalationTakenCount = stats.Sum(s => s.EscalationTakenCount),
            JudgementReusedCount = stats.Sum(s => s.JudgementReusedCount),
            LowConfidenceTakenCount = stats.Sum(s => s.LowConfidenceTakenCount),
            TicketsAssigned = stats.Sum(s => s.TicketsAssigned),
            TicketsClosed = stats.Sum(s => s.TicketsClosed)
        };

        var totalDto = new EngineerLoadStatDto
        {
            EngineerId = engineerId,
            EngineerName = engineer?.Name,
            StatDate = startDate,
            MentionedCount = totalStat.MentionedCount,
            EscalationTakenCount = totalStat.EscalationTakenCount,
            JudgementReusedCount = totalStat.JudgementReusedCount,
            LowConfidenceTakenCount = totalStat.LowConfidenceTakenCount,
            TicketsAssigned = totalStat.TicketsAssigned,
            TicketsClosed = totalStat.TicketsClosed,
            TotalLoadScore = CalculateTotalLoadScore(totalStat)
        };

        var dailyStats = stats.Select(s => new EngineerLoadStatDto
        {
            StatId = s.StatId,
            EngineerId = s.EngineerId,
            EngineerName = engineer?.Name,
            StatDate = s.StatDate,
            MentionedCount = s.MentionedCount,
            EscalationTakenCount = s.EscalationTakenCount,
            JudgementReusedCount = s.JudgementReusedCount,
            LowConfidenceTakenCount = s.LowConfidenceTakenCount,
            TicketsAssigned = s.TicketsAssigned,
            TicketsClosed = s.TicketsClosed,
            TotalLoadScore = CalculateTotalLoadScore(s)
        }).ToList();

        var analysis = AnalyzeLoad(totalDto);

        return new EngineerLoadReportDto
        {
            EngineerId = engineerId,
            EngineerName = engineer?.Name,
            StartDate = startDate,
            EndDate = endDate,
            TotalStats = totalDto,
            DailyStats = dailyStats,
            Analysis = analysis
        };
    }

    public async Task<TeamLoadDistributionDto> GetTeamLoadDistributionAsync(
        Guid? teamId,
        DateOnly startDate,
        DateOnly endDate)
    {
        // 获取团队工程师（如果指定了团队ID）
        IQueryable<User> engineersQuery = _dbContext.Users;
        if (teamId.HasValue)
        {
            // TODO: 根据团队ID过滤工程师（需要团队表）
            engineersQuery = engineersQuery.Where(u => u.Id == teamId.Value); // 临时实现
        }

        var engineers = await engineersQuery.ToListAsync();
        var engineerIds = engineers.Select(e => e.Id).ToList();

        var stats = await _dbContext.EngineerLoadStats
            .Where(s => engineerIds.Contains(s.EngineerId) &&
                       s.StatDate >= startDate &&
                       s.StatDate <= endDate)
            .GroupBy(s => s.EngineerId)
            .Select(g => new
            {
                EngineerId = g.Key,
                MentionedCount = g.Sum(s => s.MentionedCount),
                EscalationTakenCount = g.Sum(s => s.EscalationTakenCount),
                JudgementReusedCount = g.Sum(s => s.JudgementReusedCount),
                LowConfidenceTakenCount = g.Sum(s => s.LowConfidenceTakenCount),
                TicketsAssigned = g.Sum(s => s.TicketsAssigned),
                TicketsClosed = g.Sum(s => s.TicketsClosed)
            })
            .ToListAsync();

        var engineerStats = stats.Select(s =>
        {
            var engineer = engineers.FirstOrDefault(e => e.Id == s.EngineerId);
            var stat = new EngineerLoadStat
            {
                MentionedCount = s.MentionedCount,
                EscalationTakenCount = s.EscalationTakenCount,
                JudgementReusedCount = s.JudgementReusedCount,
                LowConfidenceTakenCount = s.LowConfidenceTakenCount,
                TicketsAssigned = s.TicketsAssigned,
                TicketsClosed = s.TicketsClosed
            };
            return new EngineerLoadStatDto
            {
                EngineerId = s.EngineerId,
                EngineerName = engineer?.Name,
                StatDate = startDate,
                MentionedCount = s.MentionedCount,
                EscalationTakenCount = s.EscalationTakenCount,
                JudgementReusedCount = s.JudgementReusedCount,
                LowConfidenceTakenCount = s.LowConfidenceTakenCount,
                TicketsAssigned = s.TicketsAssigned,
                TicketsClosed = s.TicketsClosed,
                TotalLoadScore = CalculateTotalLoadScore(stat)
            };
        }).ToList();

        var distributionAnalysis = AnalyzeDistribution(engineerStats);

        return new TeamLoadDistributionDto
        {
            TeamId = teamId,
            StartDate = startDate,
            EndDate = endDate,
            EngineerStats = engineerStats,
            DistributionAnalysis = distributionAnalysis
        };
    }

    public async Task<List<LoadTrendDto>> GetLoadTrendAsync(
        Guid engineerId,
        string periodType,
        int periods)
    {
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = periodType switch
        {
            "daily" => endDate.AddDays(-periods),
            "weekly" => endDate.AddDays(-periods * 7),
            "monthly" => endDate.AddMonths(-periods),
            _ => endDate.AddDays(-periods)
        };

        var stats = await _dbContext.EngineerLoadStats
            .Where(s => s.EngineerId == engineerId &&
                       s.StatDate >= startDate &&
                       s.StatDate <= endDate)
            .OrderBy(s => s.StatDate)
            .ToListAsync();

        var trend = new List<LoadTrendDto>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            var periodEnd = periodType switch
            {
                "daily" => currentDate.AddDays(1),
                "weekly" => currentDate.AddDays(7),
                "monthly" => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };

            var periodStats = stats
                .Where(s => s.StatDate >= currentDate && s.StatDate < periodEnd)
                .ToList();

            var stat = new EngineerLoadStat
            {
                MentionedCount = periodStats.Sum(s => s.MentionedCount),
                EscalationTakenCount = periodStats.Sum(s => s.EscalationTakenCount),
                JudgementReusedCount = periodStats.Sum(s => s.JudgementReusedCount),
                LowConfidenceTakenCount = periodStats.Sum(s => s.LowConfidenceTakenCount),
                TicketsAssigned = periodStats.Sum(s => s.TicketsAssigned),
                TicketsClosed = periodStats.Sum(s => s.TicketsClosed)
            };

            trend.Add(new LoadTrendDto
            {
                Period = periodType switch
                {
                    "daily" => currentDate.ToString("yyyy-MM-dd"),
                    "weekly" => $"Week {currentDate:yyyy-MM-dd}",
                    "monthly" => currentDate.ToString("yyyy-MM"),
                    _ => currentDate.ToString("yyyy-MM-dd")
                },
                PeriodStart = currentDate,
                PeriodEnd = periodEnd.AddDays(-1),
                TotalLoadScore = CalculateTotalLoadScore(stat),
                MentionedCount = stat.MentionedCount,
                EscalationTakenCount = stat.EscalationTakenCount,
                JudgementReusedCount = stat.JudgementReusedCount,
                TicketsAssigned = stat.TicketsAssigned,
                TicketsClosed = stat.TicketsClosed
            });

            currentDate = periodEnd;
        }

        return trend;
    }

    public async Task<int> CalculateAllEngineersStatsAsync(DateOnly statDate)
    {
        var engineers = await _dbContext.Users
            .Where(u => u.Role == "Engineer" || u.Role == "SeniorEngineer") // 假设角色字段
            .ToListAsync();

        var count = 0;
        foreach (var engineer in engineers)
        {
            try
            {
                await CalculateAndUpdateStatsAsync(engineer.Id, statDate);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算工程师 {EngineerId} 的负载统计失败", engineer.Id);
            }
        }

        return count;
    }

    private decimal CalculateTotalLoadScore(EngineerLoadStat stat)
    {
        // 综合负载分数计算（权重可调整）
        var score = stat.MentionedCount * 0.1m +
                   stat.EscalationTakenCount * 2.0m +
                   stat.JudgementReusedCount * 1.0m +
                   stat.LowConfidenceTakenCount * 1.5m +
                   stat.TicketsAssigned * 1.0m +
                   stat.TicketsClosed * 0.5m;
        return Math.Round(score, 2);
    }

    private LoadAnalysisDto AnalyzeLoad(EngineerLoadStatDto stat)
    {
        var loadLevel = stat.TotalLoadScore switch
        {
            < 10 => "low",
            < 30 => "normal",
            < 50 => "high",
            _ => "very_high"
        };

        var suggestions = new List<string>();
        if (stat.TotalLoadScore >= 50)
        {
            suggestions.Add("负载过高，建议减少分配或寻求协助");
        }
        if (stat.EscalationTakenCount > 5)
        {
            suggestions.Add("升级接手次数较多，建议加强团队协作");
        }
        if (stat.LowConfidenceTakenCount > 3)
        {
            suggestions.Add("低置信度工单较多，建议提升分诊准确性");
        }

        return new LoadAnalysisDto
        {
            LoadLevel = loadLevel,
            LoadScore = stat.TotalLoadScore,
            Assessment = $"当前负载水平：{loadLevel}",
            Suggestions = suggestions
        };
    }

    private LoadDistributionAnalysisDto AnalyzeDistribution(List<EngineerLoadStatDto> stats)
    {
        if (stats.Count == 0)
        {
            return new LoadDistributionAnalysisDto
            {
                AverageLoadScore = 0,
                LoadBalanceScore = 100,
                Recommendations = new List<string>()
            };
        }

        var averageLoad = stats.Average(s => s.TotalLoadScore);
        var maxLoad = stats.Max(s => s.TotalLoadScore);
        var minLoad = stats.Min(s => s.TotalLoadScore);
        var variance = stats.Average(s => Math.Pow((double)(s.TotalLoadScore - averageLoad), 2));
        var stdDev = Math.Sqrt(variance);

        // 负载均衡分数（标准差越小，分数越高）
        var balanceScore = maxLoad > 0
            ? Math.Max(0, 100 - (decimal)(stdDev / (double)maxLoad * 100))
            : 100;

        var recommendations = new List<string>();
        if (balanceScore < 70)
        {
            recommendations.Add("团队负载分布不均衡，建议重新分配工单");
        }
        var highLoadEngineers = stats.Where(s => s.TotalLoadScore > averageLoad * 1.5m).ToList();
        if (highLoadEngineers.Any())
        {
            recommendations.Add($"以下工程师负载较高：{string.Join(", ", highLoadEngineers.Select(e => e.EngineerName))}");
        }

        return new LoadDistributionAnalysisDto
        {
            AverageLoadScore = Math.Round(averageLoad, 2),
            LoadBalanceScore = Math.Round(balanceScore, 2),
            Recommendations = recommendations
        };
    }
}









