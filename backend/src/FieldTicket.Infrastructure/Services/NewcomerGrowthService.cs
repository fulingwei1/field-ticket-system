using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 新人成长曲线服务实现
/// </summary>
public class NewcomerGrowthService : INewcomerGrowthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NewcomerGrowthService> _logger;
    private readonly IJudgementCardQualityService _qualityService;

    public NewcomerGrowthService(
        ApplicationDbContext dbContext,
        ILogger<NewcomerGrowthService> logger,
        IJudgementCardQualityService qualityService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _qualityService = qualityService;
    }

    public async Task<NewcomerGrowthCurveDto> GetPersonalGrowthCurveAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate)
    {
        _logger.LogInformation("获取工程师 {EngineerId} 的成长曲线数据，从 {StartDate} 到 {EndDate}",
            engineerId, startDate, endDate);

        var engineer = await _dbContext.Users.FindAsync(engineerId);
        var dataPoints = new List<GrowthDataPointDto>();

        // 按周或月分组计算数据点
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            var periodEnd = currentDate.AddDays(7); // 按周分组
            if (periodEnd > endDate)
            {
                periodEnd = endDate;
            }

            var dataPoint = await CalculateGrowthDataPointAsync(engineerId, currentDate, periodEnd);
            dataPoints.Add(dataPoint);

            currentDate = periodEnd.AddDays(1);
        }

        // 计算摘要
        var summary = CalculateSummary(dataPoints);

        return new NewcomerGrowthCurveDto
        {
            EngineerId = engineerId,
            EngineerName = engineer?.Name,
            StartDate = startDate,
            EndDate = endDate,
            DataPoints = dataPoints,
            Summary = summary
        };
    }

    public async Task<TeamAverageGrowthCurveDto> GetTeamAverageGrowthCurveAsync(
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

        var averageDataPoints = new List<GrowthDataPointDto>();

        // 按周或月分组计算平均数据点
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            var periodEnd = currentDate.AddDays(7); // 按周分组
            if (periodEnd > endDate)
            {
                periodEnd = endDate;
            }

            var periodDataPoints = new List<GrowthDataPointDto>();
            foreach (var engineerId in engineerIds)
            {
                var dataPoint = await CalculateGrowthDataPointAsync(engineerId, currentDate, periodEnd);
                periodDataPoints.Add(dataPoint);
            }

            if (periodDataPoints.Any())
            {
                var average = new GrowthDataPointDto
                {
                    Date = currentDate,
                    JudgementCardQualityScore = periodDataPoints.Average(d => d.JudgementCardQualityScore),
                    ConfidenceAccuracy = periodDataPoints.Average(d => d.ConfidenceAccuracy),
                    AiAdoptionRate = periodDataPoints.Average(d => d.AiAdoptionRate),
                    FirstTimeResolutionRate = periodDataPoints.Average(d => d.FirstTimeResolutionRate),
                    TicketsHandled = periodDataPoints.Sum(d => d.TicketsHandled),
                    JudgementCardsCreated = periodDataPoints.Sum(d => d.JudgementCardsCreated)
                };
                averageDataPoints.Add(average);
            }

            currentDate = periodEnd.AddDays(1);
        }

        return new TeamAverageGrowthCurveDto
        {
            TeamId = teamId,
            StartDate = startDate,
            EndDate = endDate,
            AverageDataPoints = averageDataPoints
        };
    }

    public async Task<List<GrowthMilestoneDto>> GetGrowthMilestonesAsync(Guid engineerId)
    {
        var milestones = new List<GrowthMilestoneDto>();

        // 第一个工单
        var firstTicket = await _dbContext.Tickets
            .Where(t => t.CreatedByUserId == engineerId)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (firstTicket != null)
        {
            milestones.Add(new GrowthMilestoneDto
            {
                MilestoneType = "first_ticket",
                Title = "第一个工单",
                Description = $"处理了第一个工单：{firstTicket.TicketNo}",
                AchievedDate = DateOnly.FromDateTime(firstTicket.CreatedAt)
            });
        }

        // 第一个判断卡
        var firstJudgementCard = await _dbContext.JudgementCards
            .Where(jc => jc.CreatedBy == engineerId)
            .OrderBy(jc => jc.CreatedAt)
            .FirstOrDefaultAsync();

        if (firstJudgementCard != null)
        {
            milestones.Add(new GrowthMilestoneDto
            {
                MilestoneType = "first_judgement_card",
                Title = "第一个判断卡",
                Description = $"创建了第一个判断卡：{firstJudgementCard.JcCode}",
                AchievedDate = DateOnly.FromDateTime(firstJudgementCard.CreatedAt)
            });
        }

        // 判断卡质量达到70分
        var qualityJudgementCards = await _dbContext.JudgementCards
            .Where(jc => jc.CreatedBy == engineerId)
            .ToListAsync();

        foreach (var jc in qualityJudgementCards)
        {
            try
            {
                var qualityScoreResult = await _qualityService.ScoreAsync(jc.JcCode);
                var qualityScore = qualityScoreResult.TotalScore;
                if (qualityScore >= 70 && !milestones.Any(m => m.MilestoneType == "quality_threshold_70"))
                {
                    milestones.Add(new GrowthMilestoneDto
                    {
                        MilestoneType = "quality_threshold_70",
                        Title = "判断卡质量达标",
                        Description = $"判断卡质量达到70分：{jc.JcCode}",
                        AchievedDate = DateOnly.FromDateTime(jc.CreatedAt),
                        Score = qualityScore
                    });
                    break;
                }
            }
            catch
            {
                // 忽略错误
            }
        }

        return milestones.OrderBy(m => m.AchievedDate).ToList();
    }

    public async Task<GrowthMetricsDto> CalculateGrowthMetricsAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var curve = await GetPersonalGrowthCurveAsync(engineerId, startDate, endDate);
        var engineer = await _dbContext.Users.FindAsync(engineerId);

        // 计算趋势（使用线性回归斜率）
        var judgementCardQualityTrend = CalculateTrend(curve.DataPoints.Select(d => (double)d.JudgementCardQualityScore).ToList());
        var confidenceAccuracyTrend = CalculateTrend(curve.DataPoints.Select(d => (double)d.ConfidenceAccuracy).ToList());
        var aiAdoptionTrend = CalculateTrend(curve.DataPoints.Select(d => (double)d.AiAdoptionRate).ToList());
        var firstTimeResolutionTrend = CalculateTrend(curve.DataPoints.Select(d => (double)d.FirstTimeResolutionRate).ToList());

        // 综合评估
        var overallTrend = (judgementCardQualityTrend + confidenceAccuracyTrend + aiAdoptionTrend + firstTimeResolutionTrend) / 4;
        var assessment = overallTrend > 0.1 ? "improving" : overallTrend < -0.1 ? "declining" : "stable";

        var recommendations = new List<string>();
        if (judgementCardQualityTrend < 0)
        {
            recommendations.Add("判断卡质量有所下降，建议加强质量检查");
        }
        if (confidenceAccuracyTrend < 0)
        {
            recommendations.Add("置信度准确性下降，建议提升分诊准确性");
        }
        if (aiAdoptionTrend < 0.1)
        {
            recommendations.Add("AI建议采纳率较低，建议更多利用AI辅助");
        }
        if (firstTimeResolutionTrend < 0)
        {
            recommendations.Add("一次解决率下降，建议加强问题分析能力");
        }

        return new GrowthMetricsDto
        {
            EngineerId = engineerId,
            EngineerName = engineer?.Name,
            StartDate = startDate,
            EndDate = endDate,
            JudgementCardQualityTrend = (decimal)judgementCardQualityTrend,
            ConfidenceAccuracyTrend = (decimal)confidenceAccuracyTrend,
            AiAdoptionTrend = (decimal)aiAdoptionTrend,
            FirstTimeResolutionTrend = (decimal)firstTimeResolutionTrend,
            OverallGrowthAssessment = assessment,
            Recommendations = recommendations
        };
    }

    private async Task<GrowthDataPointDto> CalculateGrowthDataPointAsync(
        Guid engineerId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        // 判断卡质量得分（平均）
        var judgementCards = await _dbContext.JudgementCards
            .Where(jc => jc.CreatedBy == engineerId &&
                        jc.CreatedAt >= startDateTime &&
                        jc.CreatedAt <= endDateTime)
            .ToListAsync();

        var qualityScores = new List<decimal>();
        foreach (var jc in judgementCards)
        {
            try
            {
                var scoreResult = await _qualityService.ScoreAsync(jc.JcCode);
                var score = scoreResult.TotalScore;
                qualityScores.Add(score);
            }
            catch
            {
                // 忽略错误
            }
        }

        var avgQualityScore = qualityScores.Any() ? qualityScores.Average() : 0;

        // 置信度准确性（confidence vs 实际正确率）
        var triageNotes = await _dbContext.TriageNotes
            .Where(tn => tn.CreatedBy == engineerId &&
                        tn.CreatedAt >= startDateTime &&
                        tn.CreatedAt <= endDateTime)
            .ToListAsync();

        var confidenceAccuracy = 0m;
        if (triageNotes.Any())
        {
            // 计算置信度与实际正确率的偏差
            var accuracyScores = new List<decimal>();
            foreach (var tn in triageNotes)
            {
                // 检查工单是否最终解决（通过验证结果判断）
                var verification = await _dbContext.Verifications
                    .Where(v => v.TicketId == tn.TicketId && v.Result == "PASS")
                    .FirstOrDefaultAsync();

                if (verification != null)
                {
                    // 如果验证通过，认为分诊正确
                    var expectedConfidence = 5; // 正确时应该是高置信度
                    var accuracy = 1.0m - Math.Abs(tn.Confidence - expectedConfidence) / 5.0m;
                    accuracyScores.Add(accuracy);
                }
            }

            confidenceAccuracy = accuracyScores.Any() ? accuracyScores.Average() * 100 : 0;
        }

        // AI建议采纳率（TODO: 需要记录AI建议采纳情况）
        var aiAdoptionRate = 0m; // 暂时返回0，需要实现AI建议采纳记录

        // 一次解决率（工单第一次分诊就解决）
        var tickets = await _dbContext.Tickets
            .Where(t => t.AssignedTo == engineerId &&
                       t.CreatedAt >= startDateTime &&
                       t.CreatedAt <= endDateTime)
            .ToListAsync();

        var firstTimeResolved = 0;
        foreach (var ticket in tickets)
        {
            var firstTriage = await _dbContext.TriageNotes
                .Where(tn => tn.TicketId == ticket.TicketId)
                .OrderBy(tn => tn.CreatedAt)
                .FirstOrDefaultAsync();

            if (firstTriage != null && firstTriage.CreatedBy == engineerId)
            {
                var verification = await _dbContext.Verifications
                    .Where(v => v.TicketId == ticket.TicketId && v.Result == "PASS")
                    .FirstOrDefaultAsync();

                if (verification != null)
                {
                    firstTimeResolved++;
                }
            }
        }

        var firstTimeResolutionRate = tickets.Any() ? (decimal)firstTimeResolved / tickets.Count * 100 : 0;

        return new GrowthDataPointDto
        {
            Date = startDate,
            JudgementCardQualityScore = Math.Round(avgQualityScore, 2),
            ConfidenceAccuracy = Math.Round(confidenceAccuracy, 2),
            AiAdoptionRate = Math.Round(aiAdoptionRate, 2),
            FirstTimeResolutionRate = Math.Round(firstTimeResolutionRate, 2),
            TicketsHandled = tickets.Count,
            JudgementCardsCreated = judgementCards.Count
        };
    }

    private GrowthSummaryDto CalculateSummary(List<GrowthDataPointDto> dataPoints)
    {
        if (!dataPoints.Any())
        {
            return new GrowthSummaryDto();
        }

        var trend = "stable";
        if (dataPoints.Count >= 2)
        {
            var first = dataPoints.First().JudgementCardQualityScore;
            var last = dataPoints.Last().JudgementCardQualityScore;
            if (last > first + 5)
            {
                trend = "improving";
            }
            else if (last < first - 5)
            {
                trend = "declining";
            }
        }

        return new GrowthSummaryDto
        {
            AverageJudgementCardQuality = Math.Round(dataPoints.Average(d => d.JudgementCardQualityScore), 2),
            AverageConfidenceAccuracy = Math.Round(dataPoints.Average(d => d.ConfidenceAccuracy), 2),
            AverageAiAdoptionRate = Math.Round(dataPoints.Average(d => d.AiAdoptionRate), 2),
            AverageFirstTimeResolutionRate = Math.Round(dataPoints.Average(d => d.FirstTimeResolutionRate), 2),
            TotalTicketsHandled = dataPoints.Sum(d => d.TicketsHandled),
            TotalJudgementCardsCreated = dataPoints.Sum(d => d.JudgementCardsCreated),
            GrowthTrend = trend
        };
    }

    private double CalculateTrend(List<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        // 简单线性回归计算斜率
        var n = values.Count;
        var x = Enumerable.Range(1, n).Select(i => (double)i).ToList();
        var sumX = x.Sum();
        var sumY = values.Sum();
        var sumXY = x.Zip(values, (a, b) => a * b).Sum();
        var sumX2 = x.Sum(xi => xi * xi);

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }
}

