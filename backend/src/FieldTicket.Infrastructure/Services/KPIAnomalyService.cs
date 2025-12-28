using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// KPI反作弊预警服务实现
/// </summary>
public class KPIAnomalyService : IKPIAnomalyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KPIAnomalyService> _logger;
    private readonly KPIAnomalyOptions _options;

    public KPIAnomalyService(
        ApplicationDbContext dbContext,
        ILogger<KPIAnomalyService> logger,
        IOptions<KPIAnomalyOptions> options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<List<AnomalyDto>> DetectAnomaliesAsync(Guid engineerId, int days = 30)
    {
        var anomalies = new List<AnomalyDto>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        // 获取工程师统计数据
        var stats = await GetEngineerStatsAsync(engineerId, startDate, endDate);

        // 检测1: confidence长期偏高
        if (stats.AverageConfidence > _options.HighConfidenceThreshold)
        {
            anomalies.Add(new AnomalyDto
            {
                Type = "high_confidence",
                Description = $"置信度长期偏高（平均 {stats.AverageConfidence:F2}），可能存在过度自信",
                Severity = "medium",
                Details = new Dictionary<string, object>
                {
                    { "averageConfidence", stats.AverageConfidence },
                    { "threshold", _options.HighConfidenceThreshold },
                    { "sampleSize", stats.TriageCount }
                },
                DetectedAt = DateTime.UtcNow
            });
        }

        // 检测2: 一次解决率高但重复问题率高
        if (stats.FirstTimeResolutionRate > _options.SuspiciousResolutionRateThreshold &&
            stats.RepeatProblemRate > _options.HighRepeatProblemRateThreshold)
        {
            anomalies.Add(new AnomalyDto
            {
                Type = "suspicious_resolution",
                Description = $"一次解决率异常高（{stats.FirstTimeResolutionRate:P2}）但重复问题率高（{stats.RepeatProblemRate:P2}），可能存在数据异常",
                Severity = "high",
                Details = new Dictionary<string, object>
                {
                    { "firstTimeResolutionRate", stats.FirstTimeResolutionRate },
                    { "repeatProblemRate", stats.RepeatProblemRate },
                    { "ticketsResolved", stats.TicketsResolved },
                    { "repeatProblems", stats.RepeatProblems }
                },
                DetectedAt = DateTime.UtcNow
            });
        }

        // 检测3: 结案速度快但返工率高
        if (stats.AverageResolutionTimeHours < _options.FastClosureTimeHours &&
            stats.RepeatProblemRate > _options.MediumRepeatProblemRateThreshold)
        {
            anomalies.Add(new AnomalyDto
            {
                Type = "fast_closure_high_rework",
                Description = $"平均结案时间过短（{stats.AverageResolutionTimeHours:F1}小时）但返工率高（{stats.RepeatProblemRate:P2}），可能存在草率结案",
                Severity = "high",
                Details = new Dictionary<string, object>
                {
                    { "averageResolutionTimeHours", stats.AverageResolutionTimeHours },
                    { "repeatProblemRate", stats.RepeatProblemRate },
                    { "ticketsResolved", stats.TicketsResolved }
                },
                DetectedAt = DateTime.UtcNow
            });
        }

        // 检测4: 置信度分布异常（全部都是高置信度）
        if (stats.TriageCount > _options.MinTriageCountForDistributionCheck &&
            stats.HighConfidenceRate > _options.HighConfidenceRateAnomalyThreshold)
        {
            anomalies.Add(new AnomalyDto
            {
                Type = "confidence_distribution_anomaly",
                Description = $"置信度分布异常，高置信度比例过高（{stats.HighConfidenceRate:P2}），可能存在过度自信或数据异常",
                Severity = "medium",
                Details = new Dictionary<string, object>
                {
                    { "highConfidenceRate", stats.HighConfidenceRate },
                    { "triageCount", stats.TriageCount },
                    { "averageConfidence", stats.AverageConfidence }
                },
                DetectedAt = DateTime.UtcNow
            });
        }

        // 检测5: 验证通过率异常高（可能未真实验证）
        if (stats.VerificationCount > 5 && stats.VerificationPassRate > 0.98m)
        {
            anomalies.Add(new AnomalyDto
            {
                Type = "suspicious_verification_rate",
                Description = $"验证通过率异常高（{stats.VerificationPassRate:P2}），可能存在验证不充分的情况",
                Severity = "medium",
                Details = new Dictionary<string, object>
                {
                    { "verificationPassRate", stats.VerificationPassRate },
                    { "verificationCount", stats.VerificationCount },
                    { "passCount", stats.VerificationPassCount }
                },
                DetectedAt = DateTime.UtcNow
            });
        }

        return anomalies;
    }

    public async Task<List<EngineerAnomalyReportDto>> DetectTeamAnomaliesAsync(Guid? teamId = null, int days = 30)
    {
        var reports = new List<EngineerAnomalyReportDto>();

        // 获取团队工程师列表（这里简化处理，实际应该从用户表获取）
        var engineers = await _dbContext.Tickets
            .Where(t => t.AssignedTo.HasValue)
            .Select(t => t.AssignedTo!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var engineerId in engineers)
        {
            var anomalies = await DetectAnomaliesAsync(engineerId, days);
            if (anomalies.Any())
            {
                var engineer = await _dbContext.UserProfiles
                    .Where(u => u.UserId == engineerId)
                    .Select(u => new { u.UserId, Name = u.User!.Name })
                    .FirstOrDefaultAsync();

                reports.Add(new EngineerAnomalyReportDto
                {
                    EngineerId = engineerId,
                    EngineerName = engineer?.Name,
                    Anomalies = anomalies,
                    TotalAnomalies = anomalies.Count,
                    CriticalAnomalies = anomalies.Count(a => a.Severity == "critical"),
                    HighAnomalies = anomalies.Count(a => a.Severity == "high")
                });
            }
        }

        return reports.OrderByDescending(r => r.CriticalAnomalies)
            .ThenByDescending(r => r.HighAnomalies)
            .ToList();
    }

    public async Task<AnomalyReportDto> GenerateAnomalyReportAsync(Guid? engineerId = null, Guid? teamId = null, int days = 30)
    {
        var report = new AnomalyReportDto
        {
            ReportDate = DateTime.UtcNow,
            Days = days,
            EngineerId = engineerId,
            TeamId = teamId
        };

        if (engineerId.HasValue)
        {
            var engineer = await _dbContext.UserProfiles
                .Where(u => u.UserId == engineerId.Value)
                .Select(u => u.User!.Name)
                .FirstOrDefaultAsync();
            report.EngineerName = engineer;

            var anomalies = await DetectAnomaliesAsync(engineerId.Value, days);
            report.EngineerReports.Add(new EngineerAnomalyReportDto
            {
                EngineerId = engineerId.Value,
                EngineerName = engineer,
                Anomalies = anomalies,
                TotalAnomalies = anomalies.Count,
                CriticalAnomalies = anomalies.Count(a => a.Severity == "critical"),
                HighAnomalies = anomalies.Count(a => a.Severity == "high")
            });
        }
        else
        {
            report.EngineerReports = await DetectTeamAnomaliesAsync(teamId, days);
        }

        report.TotalEngineers = report.EngineerReports.Count;
        report.EngineersWithAnomalies = report.EngineerReports.Count(r => r.TotalAnomalies > 0);

        // 统计异常类型
        foreach (var engineerReport in report.EngineerReports)
        {
            foreach (var anomaly in engineerReport.Anomalies)
            {
                if (!report.AnomalyTypeCounts.ContainsKey(anomaly.Type))
                {
                    report.AnomalyTypeCounts[anomaly.Type] = 0;
                }
                report.AnomalyTypeCounts[anomaly.Type]++;
            }
        }

        return report;
    }

    /// <summary>
    /// 获取工程师统计数据
    /// </summary>
    private async Task<EngineerStats> GetEngineerStatsAsync(Guid engineerId, DateTime startDate, DateTime endDate)
    {
        var stats = new EngineerStats();

        // 获取分诊记录
        var triageNotes = await _dbContext.TriageNotes
            .Where(tn => tn.CreatedBy == engineerId &&
                        tn.CreatedAt >= startDate &&
                        tn.CreatedAt <= endDate)
            .ToListAsync();

        stats.TriageCount = triageNotes.Count();
        if (triageNotes.Any())
        {
            stats.AverageConfidence = (decimal)triageNotes.Average(tn => tn.Confidence);
            stats.HighConfidenceRate = (decimal)triageNotes.Count(tn => tn.Confidence >= 4) / triageNotes.Count;
        }

        // 获取工单统计
        var tickets = await _dbContext.Tickets
            .Where(t => t.AssignedTo == engineerId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
            .ToListAsync();

        stats.TicketsResolved = tickets.Count(t => t.Status == "Closed");
        stats.TotalTickets = tickets.Count;

        // 计算一次解决率（第一次分诊就解决的工单比例）
        var firstTimeResolved = 0;
        foreach (var ticket in tickets.Where(t => t.Status == "Closed"))
        {
            var firstTriage = triageNotes
                .Where(tn => tn.TicketId == ticket.TicketId)
                .OrderBy(tn => tn.CreatedAt)
                .FirstOrDefault();

            if (firstTriage != null && firstTriage.Confidence >= 3)
            {
                // 检查是否有验证通过记录
                var verification = await _dbContext.Verifications
                    .Where(v => v.TicketId == ticket.TicketId && v.Result == "PASS")
                    .FirstOrDefaultAsync();

                if (verification != null)
                {
                    firstTimeResolved++;
                }
            }
        }

        stats.FirstTimeResolutionRate = stats.TicketsResolved > 0
            ? (decimal)firstTimeResolved / stats.TicketsResolved
            : 0;

        // 计算重复问题率（同一设备30天内出现相同问题域的问题）
        var repeatProblems = 0;
        var deviceDomainGroups = tickets
            .GroupBy(t => new { t.DeviceId, t.Domain })
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in deviceDomainGroups)
        {
            var groupTickets = group.OrderBy(t => t.CreatedAt).ToList();
            for (int i = 1; i < groupTickets.Count; i++)
            {
                var daysBetween = (groupTickets[i].CreatedAt - groupTickets[i - 1].CreatedAt).TotalDays;
                if (daysBetween <= 30)
                {
                    repeatProblems++;
                    break; // 每个设备-问题域组合只计算一次
                }
            }
        }

        stats.RepeatProblems = repeatProblems;
        stats.RepeatProblemRate = stats.TotalTickets > 0
            ? (decimal)repeatProblems / stats.TotalTickets
            : 0;

        // 计算平均解决时间
        var resolvedTickets = tickets
            .Where(t => t.Status == "Closed" && t.ClosedAt.HasValue)
            .ToList();

        if (resolvedTickets.Any())
        {
            stats.AverageResolutionTimeHours = (decimal)resolvedTickets
                .Average(t => (t.ClosedAt!.Value - t.CreatedAt).TotalHours);
        }

        // 获取验证统计
        var ticketIds = tickets.Select(t => t.TicketId).ToList();
        var verifications = await _dbContext.Verifications
            .Where(v => ticketIds.Contains(v.TicketId))
            .ToListAsync();

        stats.VerificationCount = verifications.Count();
        stats.VerificationPassCount = verifications.Count(v => v.Result == "PASS");
        stats.VerificationPassRate = stats.VerificationCount > 0
            ? (decimal)stats.VerificationPassCount / stats.VerificationCount
            : 0;

        return stats;
    }

    /// <summary>
    /// 工程师统计数据
    /// </summary>
    private class EngineerStats
    {
        public int TriageCount { get; set; }
        public decimal AverageConfidence { get; set; }
        public decimal HighConfidenceRate { get; set; }
        public int TicketsResolved { get; set; }
        public int TotalTickets { get; set; }
        public decimal FirstTimeResolutionRate { get; set; }
        public int RepeatProblems { get; set; }
        public decimal RepeatProblemRate { get; set; }
        public decimal AverageResolutionTimeHours { get; set; }
        public int VerificationCount { get; set; }
        public int VerificationPassCount { get; set; }
        public decimal VerificationPassRate { get; set; }
    }
}














