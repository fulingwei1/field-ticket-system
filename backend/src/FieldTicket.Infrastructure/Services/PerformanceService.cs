using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 绩效服务实现
/// </summary>
public class PerformanceService : IPerformanceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<PerformanceService> _logger;

    public PerformanceService(
        ApplicationDbContext dbContext,
        ILogger<PerformanceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PerformanceMetricsDto?> GetEngineerMetricsAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid? currentUserId = null)
    {
        // 权限检查：工程师只能查看自己的绩效
        if (currentUserId.HasValue && currentUserId.Value == engineerId)
        {
            // 允许查看自己的绩效
        }
        else if (currentUserId.HasValue)
        {
            // 检查是否是部门经理或管理员
            var currentUser = await _dbContext.Users.FindAsync(currentUserId.Value);
            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "Admin"))
            {
                throw new UnauthorizedAccessException("无权查看其他工程师的绩效");
            }
        }

        var metrics = await _dbContext.PerformanceMetrics
            .Include(m => m.Engineer)
            .FirstOrDefaultAsync(m =>
                m.EngineerId == engineerId &&
                m.PeriodType == periodType &&
                m.PeriodStart == periodStart);

        if (metrics == null)
        {
            return null;
        }

        return MapToDto(metrics);
    }

    public async Task<(List<PerformanceMetricsDto> Items, int Total)> GetMetricsAsync(
        PerformanceQueryFilter filter,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbContext.PerformanceMetrics
            .Include(m => m.Engineer)
            .AsQueryable();

        // 应用过滤器
        if (filter.EngineerId.HasValue)
        {
            query = query.Where(m => m.EngineerId == filter.EngineerId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            // TODO: 需要关联部门信息
            // query = query.Where(m => m.Engineer.DeptId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrEmpty(filter.PeriodType))
        {
            query = query.Where(m => m.PeriodType == filter.PeriodType);
        }

        if (filter.PeriodStartFrom.HasValue)
        {
            query = query.Where(m => m.PeriodStart >= filter.PeriodStartFrom.Value);
        }

        if (filter.PeriodStartTo.HasValue)
        {
            query = query.Where(m => m.PeriodStart <= filter.PeriodStartTo.Value);
        }

        if (filter.MinOverallScore.HasValue)
        {
            query = query.Where(m => m.OverallScore >= filter.MinOverallScore.Value);
        }

        if (!string.IsNullOrEmpty(filter.PerformanceLevel))
        {
            query = query.Where(m => m.PerformanceLevel == filter.PerformanceLevel);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.PeriodStart)
            .ThenByDescending(m => m.OverallScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items.Select(MapToDto).ToList(), total);
    }

    public async Task<List<PerformanceMetricsDto>> GetTeamMetricsAsync(
        Guid? departmentId,
        string periodType,
        DateOnly periodStart,
        Guid? currentUserId = null)
    {
        var query = _dbContext.PerformanceMetrics
            .Include(m => m.Engineer)
            .Where(m => m.PeriodType == periodType && m.PeriodStart == periodStart)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            // TODO: 需要关联部门信息
            // query = query.Where(m => m.Engineer.DeptId == departmentId.Value);
        }

        var metrics = await query
            .OrderByDescending(m => m.OverallScore)
            .ToListAsync();

        return metrics.Select(MapToDto).ToList();
    }

    public async Task<List<PerformanceRankingDto>> GetRankingAsync(
        string periodType,
        DateOnly periodStart,
        Guid? departmentId = null,
        Guid? currentUserId = null)
    {
        var query = _dbContext.PerformanceMetrics
            .Include(m => m.Engineer)
            .Where(m => m.PeriodType == periodType && m.PeriodStart == periodStart)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            // TODO: 需要关联部门信息
            // query = query.Where(m => m.Engineer.DeptId == departmentId.Value);
        }

        var metrics = await query
            .OrderByDescending(m => m.OverallScore)
            .ToListAsync();

        var ranking = new List<PerformanceRankingDto>();
        int rank = 1;

        foreach (var metric in metrics)
        {
            ranking.Add(new PerformanceRankingDto
            {
                EngineerId = metric.EngineerId,
                EngineerName = metric.Engineer?.Name ?? "未知",
                OverallScore = metric.OverallScore,
                PerformanceLevel = metric.PerformanceLevel,
                Rank = rank++,
                TotalTickets = metric.TotalTickets,
                TicketsResolved = metric.TicketsResolved,
                FirstTimeResolutionRate = metric.FirstTimeResolutionRate,
                AverageResolutionTime = metric.AverageResolutionTime
            });
        }

        return ranking;
    }

    public async Task<PerformanceMetricsDto> CalculateMetricsAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        _logger.LogInformation(
            "Calculating performance metrics for engineer {EngineerId}, period {PeriodType} from {PeriodStart} to {PeriodEnd}",
            engineerId, periodType, periodStart, periodEnd);

        // 检查是否已存在
        var existing = await _dbContext.PerformanceMetrics
            .FirstOrDefaultAsync(m =>
                m.EngineerId == engineerId &&
                m.PeriodType == periodType &&
                m.PeriodStart == periodStart);

        var metrics = existing ?? new PerformanceMetrics
        {
            MetricId = Guid.NewGuid(),
            EngineerId = engineerId,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTime.UtcNow
        };

        // 计算各项指标
        await CalculateTicketMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateResponseMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateDeviceMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateWorkActivityMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateQualityMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateKnowledgeMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateCustomerMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateCollaborationMetricsAsync(metrics, engineerId, periodStart, periodEnd);
        await CalculateComplianceMetricsAsync(metrics, engineerId, periodStart, periodEnd);

        // 计算综合评分
        CalculateOverallScore(metrics);

        // 计算排名
        await CalculateRankingAsync(metrics, periodType, periodStart);

        metrics.UpdatedAt = DateTime.UtcNow;
        metrics.CalculatedAt = DateTime.UtcNow;

        if (existing == null)
        {
            _dbContext.PerformanceMetrics.Add(metrics);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Performance metrics calculated for engineer {EngineerId}, overall score: {OverallScore}",
            engineerId, metrics.OverallScore);

        return MapToDto(metrics);
    }

    public async Task<List<PerformanceTrendDto>> GetTrendsAsync(
        Guid engineerId,
        string periodType,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var metrics = await _dbContext.PerformanceMetrics
            .Where(m =>
                m.EngineerId == engineerId &&
                m.PeriodType == periodType &&
                m.PeriodStart >= fromDate &&
                m.PeriodStart <= toDate)
            .OrderBy(m => m.PeriodStart)
            .ToListAsync();

        return metrics.Select(m => new PerformanceTrendDto
        {
            PeriodStart = m.PeriodStart,
            PeriodEnd = m.PeriodEnd,
            OverallScore = m.OverallScore,
            TotalTickets = m.TotalTickets,
            TicketsResolved = m.TicketsResolved,
            FirstTimeResolutionRate = m.FirstTimeResolutionRate,
            AverageResolutionTime = m.AverageResolutionTime,
            OnTimeResponseRate = m.OnTimeResponseRate,
            RankInTeam = m.RankInTeam,
            RankInDepartment = m.RankInDepartment
        }).ToList();
    }

    #region 指标计算

    private async Task CalculateTicketMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        var tickets = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.CreatedAt >= startDate &&
                t.CreatedAt <= endDate)
            .ToListAsync();

        metrics.TotalTickets = tickets.Count;
        metrics.TicketsResolved = tickets.Count(t => t.Status == "Closed");
        metrics.TicketsPending = tickets.Count(t => t.Status != "Closed" && t.Status != "Draft");

        // 计算平均解决时间
        var resolvedTickets = tickets.Where(t => t.ClosedAt.HasValue && t.CreatedAt != default).ToList();
        if (resolvedTickets.Any())
        {
            var totalTime = resolvedTickets.Sum(t => (t.ClosedAt!.Value - t.CreatedAt).TotalHours);
            metrics.AverageResolutionTime = TimeSpan.FromHours(totalTime / resolvedTickets.Count);
        }

        // 计算一次解决率（需要根据业务逻辑判断）
        // TODO: 需要根据工单历史判断是否为一次解决
        var firstTimeResolved = resolvedTickets.Count; // 简化处理
        if (metrics.TotalTickets > 0)
        {
            metrics.FirstTimeResolutionRate = (decimal)firstTimeResolved / metrics.TotalTickets * 100;
        }
    }

    private async Task CalculateResponseMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        var tickets = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.SubmittedAt.HasValue &&
                t.SubmittedAt.Value >= startDate &&
                t.SubmittedAt.Value <= endDate)
            .ToListAsync();

        if (tickets.Any())
        {
            // 计算平均响应时间（从提交到首次响应）
            // TODO: 需要工单响应记录表来计算准确的响应时间
            var responseTimes = tickets
                .Where(t => t.SubmittedAt.HasValue && t.UpdatedAt > t.SubmittedAt.Value)
                .Select(t => (t.UpdatedAt - t.SubmittedAt!.Value).TotalHours)
                .ToList();

            if (responseTimes.Any())
            {
                metrics.AverageResponseTime = TimeSpan.FromHours(responseTimes.Average());
                
                // 计算95分位响应时间
                responseTimes.Sort();
                var p95Index = (int)(responseTimes.Count * 0.95);
                if (p95Index < responseTimes.Count)
                {
                    metrics.ResponseTimeP95 = TimeSpan.FromHours(responseTimes[p95Index]);
                }

                // 计算及时响应率（30分钟内）
                var timelyCount = responseTimes.Count(t => t <= 0.5); // 30分钟 = 0.5小时
                metrics.OnTimeResponseRate = (decimal)timelyCount / responseTimes.Count * 100;
            }
        }
    }

    private async Task CalculateDeviceMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        // 获取服务的设备数
        var devicesServiced = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.CreatedAt >= startDate &&
                t.CreatedAt <= endDate)
            .Select(t => t.DeviceId)
            .Distinct()
            .CountAsync();

        metrics.DevicesServiced = devicesServiced;

        // TODO: 计算设备故障率和重复故障率
        // 需要根据业务逻辑判断故障设备数和重复故障
    }

    private async Task CalculateWorkActivityMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        // 计算有工单处理记录的天数
        var activityDays = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.CreatedAt >= startDate &&
                t.CreatedAt <= endDate)
            .Select(t => DateOnly.FromDateTime(t.CreatedAt.Date))
            .Distinct()
            .CountAsync();

        metrics.WorkActivityDays = activityDays;

        // 计算工作活动完整度
        var totalDays = (periodEnd.ToDateTime(TimeOnly.MinValue) - periodStart.ToDateTime(TimeOnly.MinValue)).Days + 1;
        if (totalDays > 0)
        {
            metrics.WorkActivityCompleteness = (decimal)activityDays / totalDays * 100;
        }
    }

    private async Task CalculateQualityMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        var tickets = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.CreatedAt >= startDate &&
                t.CreatedAt <= endDate &&
                t.Status != "Draft")
            .ToListAsync();

        if (tickets.Any())
        {
            // 计算工单创建完整度
            var completeTickets = tickets.Count(t =>
                !string.IsNullOrEmpty(t.StepCode) &&
                !string.IsNullOrEmpty(t.SymptomTitle) &&
                !string.IsNullOrEmpty(t.SwVersion));

            metrics.TicketCreationCompleteness = (decimal)completeTickets / tickets.Count * 100;

            // 计算工单信息完整性
            var infoCompleteTickets = tickets.Count(t =>
                !string.IsNullOrEmpty(t.StepCode) &&
                !string.IsNullOrEmpty(t.SymptomTitle) &&
                t.FactsJson != null);

            metrics.TicketInformationCompleteness = (decimal)infoCompleteTickets / tickets.Count * 100;

            // TODO: 计算现场问题反馈及时率（需要问题发生时间字段）
            // TODO: 计算追问回复及时性（需要工单评论/追问记录）
        }
    }

    private async Task CalculateKnowledgeMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        // TODO: 需要判断卡和解决方案表
        // 计算判断卡创建数量、质量评分、复用贡献等
        metrics.JudgementCardsCreated = 0;
        metrics.SolutionsContributed = 0;
    }

    private async Task CalculateCustomerMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        // TODO: 需要客户反馈和满意度表
        // 计算客户沟通及时性、质量、满意度等
        metrics.CustomerFeedbackCount = 0;
    }

    private async Task CalculateCollaborationMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        // TODO: 需要工单评论、@提醒等协作记录
        // 计算团队协作活跃度、知识分享贡献等
    }

    private async Task CalculateComplianceMetricsAsync(
        PerformanceMetrics metrics,
        Guid engineerId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        var closedTickets = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.Status == "Closed" &&
                t.ClosedAt >= startDate &&
                t.ClosedAt <= endDate)
            .ToListAsync();

        if (closedTickets.Any())
        {
            // TODO: 计算责任归因完成度（需要责任归因字段）
            // 简化处理：假设所有已结案工单都已完成归因
            metrics.RootCauseAttributionCompleteness = 100;
        }
    }

    private void CalculateOverallScore(PerformanceMetrics metrics)
    {
        // 根据绩效指标体系设计文档计算综合评分
        // 这里使用简化的权重计算，实际应该根据完整的23个指标计算

        decimal score = 0;
        decimal totalWeight = 0;

        // 工单处理效率维度（权重：50%）
        if (metrics.TicketCreationCompleteness.HasValue)
        {
            score += metrics.TicketCreationCompleteness.Value * 0.15m;
            totalWeight += 0.15m;
        }
        if (metrics.OnTimeResponseRate.HasValue)
        {
            score += metrics.OnTimeResponseRate.Value * 0.15m;
            totalWeight += 0.15m;
        }
        if (metrics.FieldFeedbackTimelinessRate.HasValue)
        {
            score += metrics.FieldFeedbackTimelinessRate.Value * 0.10m;
            totalWeight += 0.10m;
        }
        if (metrics.QuestionReplyTimelinessRate.HasValue)
        {
            score += metrics.QuestionReplyTimelinessRate.Value * 0.10m;
            totalWeight += 0.10m;
        }

        // 问题解决能力维度（权重：50%）
        if (metrics.FirstTimeResolutionRate.HasValue)
        {
            score += metrics.FirstTimeResolutionRate.Value * 0.20m;
            totalWeight += 0.20m;
        }
        if (metrics.AverageResolutionTime.HasValue)
        {
            // 标准化解决时间（假设目标为4小时，越短越好）
            var hours = metrics.AverageResolutionTime.Value.TotalHours;
            var normalizedScore = hours <= 4 ? 100 : Math.Max(0, 100 - (hours - 4) * 10);
            score += (decimal)normalizedScore * 0.15m;
            totalWeight += 0.15m;
        }
        if (metrics.VerificationPassRate.HasValue)
        {
            score += metrics.VerificationPassRate.Value * 0.10m;
            totalWeight += 0.10m;
        }
        if (metrics.RepeatProblemRate.HasValue)
        {
            // 重复问题率越低越好
            var normalizedScore = Math.Max(0, 100 - metrics.RepeatProblemRate.Value);
            score += normalizedScore * 0.05m;
            totalWeight += 0.05m;
        }

        // 工作规范性维度（权重：15%）
        if (metrics.TicketInformationCompleteness.HasValue)
        {
            score += metrics.TicketInformationCompleteness.Value * 0.10m;
            totalWeight += 0.10m;
        }
        if (metrics.RootCauseAttributionCompleteness.HasValue)
        {
            score += metrics.RootCauseAttributionCompleteness.Value * 0.05m;
            totalWeight += 0.05m;
        }

        // 归一化评分
        if (totalWeight > 0)
        {
            metrics.OverallScore = score / totalWeight;
        }
        else
        {
            metrics.OverallScore = 0;
        }

        // 确定绩效等级
        metrics.PerformanceLevel = metrics.OverallScore switch
        {
            >= 90 => "excellent",
            >= 80 => "good",
            >= 70 => "average",
            >= 60 => "below_average",
            _ => "poor"
        };
    }

    private async Task CalculateRankingAsync(
        PerformanceMetrics metrics,
        string periodType,
        DateOnly periodStart)
    {
        // 计算团队排名
        var teamMetrics = await _dbContext.PerformanceMetrics
            .Where(m =>
                m.PeriodType == periodType &&
                m.PeriodStart == periodStart &&
                m.OverallScore.HasValue)
            .OrderByDescending(m => m.OverallScore)
            .ToListAsync();

        var rank = teamMetrics.FindIndex(m => m.MetricId == metrics.MetricId) + 1;
        if (rank > 0)
        {
            metrics.RankInTeam = rank;
        }

        // TODO: 计算部门排名（需要部门信息）
        metrics.RankInDepartment = rank;
    }

    #endregion

    #region 映射方法

    private PerformanceMetricsDto MapToDto(PerformanceMetrics metrics)
    {
        return new PerformanceMetricsDto
        {
            MetricId = metrics.MetricId,
            EngineerId = metrics.EngineerId,
            EngineerName = metrics.Engineer?.Name,
            PeriodType = metrics.PeriodType,
            PeriodStart = metrics.PeriodStart,
            PeriodEnd = metrics.PeriodEnd,
            TotalTickets = metrics.TotalTickets,
            TicketsResolved = metrics.TicketsResolved,
            TicketsPending = metrics.TicketsPending,
            AverageResolutionTime = metrics.AverageResolutionTime,
            FirstTimeResolutionRate = metrics.FirstTimeResolutionRate,
            AverageResponseTime = metrics.AverageResponseTime,
            ResponseTimeP95 = metrics.ResponseTimeP95,
            OnTimeResponseRate = metrics.OnTimeResponseRate,
            DevicesServiced = metrics.DevicesServiced,
            DeviceFailureRate = metrics.DeviceFailureRate,
            RepeatFailureRate = metrics.RepeatFailureRate,
            WorkActivityDays = metrics.WorkActivityDays,
            WorkActivityCompleteness = metrics.WorkActivityCompleteness,
            TicketCreationCompleteness = metrics.TicketCreationCompleteness,
            FieldFeedbackTimelinessRate = metrics.FieldFeedbackTimelinessRate,
            QuestionReplyTimelinessRate = metrics.QuestionReplyTimelinessRate,
            VerificationPassRate = metrics.VerificationPassRate,
            RepeatProblemRate = metrics.RepeatProblemRate,
            JudgementCardUsageAccuracy = metrics.JudgementCardUsageAccuracy,
            JudgementCardHitRate = metrics.JudgementCardHitRate,
            AiSuggestionAdoptionRate = metrics.AiSuggestionAdoptionRate,
            LowConfidenceUpgradeTimeliness = metrics.LowConfidenceUpgradeTimeliness,
            JudgementCardsCreated = metrics.JudgementCardsCreated,
            JudgementCardQualityScore = metrics.JudgementCardQualityScore,
            JudgementCardReuseContribution = metrics.JudgementCardReuseContribution,
            SolutionsContributed = metrics.SolutionsContributed,
            CustomerCommunicationTimeliness = metrics.CustomerCommunicationTimeliness,
            CustomerCommunicationQuality = metrics.CustomerCommunicationQuality,
            CustomerSatisfactionScore = metrics.CustomerSatisfactionScore,
            CustomerFeedbackCount = metrics.CustomerFeedbackCount,
            TeamCollaborationActivity = metrics.TeamCollaborationActivity,
            KnowledgeSharingContribution = metrics.KnowledgeSharingContribution,
            TicketInformationCompleteness = metrics.TicketInformationCompleteness,
            RootCauseAttributionCompleteness = metrics.RootCauseAttributionCompleteness,
            OverallScore = metrics.OverallScore,
            PerformanceLevel = metrics.PerformanceLevel,
            RankInTeam = metrics.RankInTeam,
            RankInDepartment = metrics.RankInDepartment,
            CalculatedAt = metrics.CalculatedAt,
            CreatedAt = metrics.CreatedAt,
            UpdatedAt = metrics.UpdatedAt
        };
    }

    #endregion

    #region Phase 3: 高级功能

    public async Task<PerformanceReportDto> GeneratePerformanceReportAsync(
        GeneratePerformanceReportRequest request,
        Guid currentUserId)
    {
        _logger.LogInformation("Generating performance report for engineer {EngineerId}, period {PeriodType} {PeriodStart}",
            request.EngineerId, request.PeriodType, request.PeriodStart);

        var report = new PerformanceReportDto
        {
            ReportId = Guid.NewGuid().ToString(),
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            PeriodType = request.PeriodType,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = currentUserId
        };

        // 生成报告标题
        if (request.EngineerId.HasValue)
        {
            var engineer = await _dbContext.Users.FindAsync(request.EngineerId.Value);
            report.ReportTitle = $"{engineer?.Name ?? "工程师"}绩效报告 - {request.PeriodType} ({request.PeriodStart:yyyy-MM-dd} 至 {request.PeriodEnd:yyyy-MM-dd})";
        }
        else if (request.DepartmentId.HasValue)
        {
            report.ReportTitle = $"团队绩效报告 - {request.PeriodType} ({request.PeriodStart:yyyy-MM-dd} 至 {request.PeriodEnd:yyyy-MM-dd})";
        }
        else
        {
            report.ReportTitle = $"绩效报告 - {request.PeriodType} ({request.PeriodStart:yyyy-MM-dd} 至 {request.PeriodEnd:yyyy-MM-dd})";
        }

        // 获取工程师绩效
        if (request.EngineerId.HasValue)
        {
            var metrics = await GetEngineerMetricsAsync(
                request.EngineerId.Value,
                request.PeriodType,
                request.PeriodStart,
                currentUserId);
            report.EngineerMetrics = metrics;

            // 获取趋势数据
            var trends = await GetTrendsAsync(
                request.EngineerId.Value,
                request.PeriodType,
                request.PeriodStart,
                request.PeriodEnd);
            report.Trends = trends;

            // 生成改进建议
            var suggestions = await GenerateImprovementSuggestionsAsync(
                request.EngineerId.Value,
                request.PeriodType,
                request.PeriodStart,
                currentUserId);
            report.ImprovementSuggestions = suggestions;
        }

        // 获取团队绩效
        if (request.DepartmentId.HasValue || !request.EngineerId.HasValue)
        {
            var teamMetrics = await GetTeamMetricsAsync(
                request.DepartmentId,
                request.PeriodType,
                request.PeriodStart,
                currentUserId);
            report.TeamMetrics = teamMetrics;

            // 获取排名
            var ranking = await GetRankingAsync(
                request.PeriodType,
                request.PeriodStart,
                request.DepartmentId,
                currentUserId);
            report.Ranking = ranking;
        }

        // 生成摘要
        report.Summary = GenerateReportSummary(report);

        return report;
    }

    public async Task<byte[]> ExportPerformanceDataAsync(
        ExportPerformanceDataRequest request,
        Guid currentUserId)
    {
        _logger.LogInformation("Exporting performance data, format: {Format}", request.ExportFormat);

        var filter = new PerformanceQueryFilter
        {
            EngineerId = request.EngineerId,
            DepartmentId = request.DepartmentId,
            PeriodType = request.PeriodType,
            PeriodStartFrom = request.PeriodStartFrom,
            PeriodStartTo = request.PeriodStartTo
        };

        var (items, _) = await GetMetricsAsync(filter, 1, int.MaxValue);

        // 根据导出格式生成数据
        if (request.ExportFormat.ToLower() == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(items, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        else if (request.ExportFormat.ToLower() == "csv")
        {
            return GenerateCsvData(items);
        }
        else // excel
        {
            // TODO: 使用 EPPlus 或 ClosedXML 生成 Excel
            // 暂时返回 CSV 格式
            return GenerateCsvData(items);
        }
    }

    public async Task<List<ImprovementSuggestionDto>> GenerateImprovementSuggestionsAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid currentUserId)
    {
        _logger.LogInformation("Generating improvement suggestions for engineer {EngineerId}", engineerId);

        var suggestions = new List<ImprovementSuggestionDto>();
        var metrics = await GetEngineerMetricsAsync(engineerId, periodType, periodStart, currentUserId);

        if (metrics == null)
        {
            return suggestions;
        }

        // 分析各项指标，生成改进建议
        // 1. 工单处理效率维度
        if (metrics.OnTimeResponseRate.HasValue && metrics.OnTimeResponseRate < 80)
        {
            suggestions.Add(new ImprovementSuggestionDto
            {
                Category = "efficiency",
                Title = "提升响应及时率",
                Description = $"当前响应及时率为 {metrics.OnTimeResponseRate:F1}%，低于目标值80%。建议：",
                Priority = "high",
                ActionItems = new List<string>
                {
                    "设置工单提醒，确保30分钟内响应",
                    "优化工作流程，减少响应时间",
                    "使用移动端及时接收工单通知"
                },
                ExpectedImprovement = 15m
            });
        }

        // 2. 问题解决能力维度
        if (metrics.FirstTimeResolutionRate.HasValue && metrics.FirstTimeResolutionRate < 70)
        {
            suggestions.Add(new ImprovementSuggestionDto
            {
                Category = "quality",
                Title = "提升一次解决率",
                Description = $"当前一次解决率为 {metrics.FirstTimeResolutionRate:F1}%，低于目标值70%。建议：",
                Priority = "high",
                ActionItems = new List<string>
                {
                    "在分诊前充分了解问题背景",
                    "使用判断卡提高诊断准确性",
                    "加强与现场工程师的沟通"
                },
                ExpectedImprovement = 20m
            });
        }

        // 3. 知识贡献维度
        if (metrics.JudgementCardsCreated == 0)
        {
            suggestions.Add(new ImprovementSuggestionDto
            {
                Category = "knowledge",
                Title = "增加知识贡献",
                Description = "当前未创建任何判断卡。建议：",
                Priority = "medium",
                ActionItems = new List<string>
                {
                    "总结常见问题的处理经验",
                    "创建判断卡帮助团队提高效率",
                    "分享解决方案和最佳实践"
                },
                ExpectedImprovement = 10m
            });
        }

        // 4. 工作规范性维度
        if (metrics.TicketInformationCompleteness.HasValue && metrics.TicketInformationCompleteness < 90)
        {
            suggestions.Add(new ImprovementSuggestionDto
            {
                Category = "quality",
                Title = "提升工单信息完整性",
                Description = $"当前工单信息完整度为 {metrics.TicketInformationCompleteness:F1}%，低于目标值90%。建议：",
                Priority = "medium",
                ActionItems = new List<string>
                {
                    "使用问诊式补全功能确保信息完整",
                    "检查工单必填项是否都已填写",
                    "补充关键信息后再提交工单"
                },
                ExpectedImprovement = 10m
            });
        }

        // 5. 客户服务能力维度
        if (metrics.CustomerSatisfactionScore.HasValue && metrics.CustomerSatisfactionScore < 4.0m)
        {
            suggestions.Add(new ImprovementSuggestionDto
            {
                Category = "collaboration",
                Title = "提升客户满意度",
                Description = $"当前客户满意度为 {metrics.CustomerSatisfactionScore:F1}/5.0，低于目标值4.0。建议：",
                Priority = "high",
                ActionItems = new List<string>
                {
                    "及时响应客户沟通",
                    "使用沟通模板确保专业表达",
                    "主动跟进问题解决进度"
                },
                ExpectedImprovement = 15m
            });
        }

        return suggestions;
    }

    private Dictionary<string, object> GenerateReportSummary(PerformanceReportDto report)
    {
        var summary = new Dictionary<string, object>();

        if (report.EngineerMetrics != null)
        {
            summary["engineerName"] = report.EngineerMetrics.EngineerName ?? "工程师";
            summary["overallScore"] = report.EngineerMetrics.OverallScore ?? 0;
            summary["performanceLevel"] = report.EngineerMetrics.PerformanceLevel ?? "unknown";
            summary["totalTickets"] = report.EngineerMetrics.TotalTickets;
            summary["ticketsResolved"] = report.EngineerMetrics.TicketsResolved;
            summary["rankInTeam"] = report.EngineerMetrics.RankInTeam;
        }

        if (report.TeamMetrics != null && report.TeamMetrics.Any())
        {
            summary["teamSize"] = report.TeamMetrics.Count;
            summary["averageScore"] = report.TeamMetrics.Average(m => m.OverallScore ?? 0);
            summary["topPerformer"] = report.TeamMetrics.OrderByDescending(m => m.OverallScore).FirstOrDefault()?.EngineerName;
        }

        return summary;
    }

    private byte[] GenerateCsvData(List<PerformanceMetricsDto> items)
    {
        var csv = new System.Text.StringBuilder();
        
        // CSV 头部
        csv.AppendLine("工程师,周期类型,周期开始,总工单数,已解决工单数,平均解决时间,一次解决率,响应及时率,综合评分,绩效等级,团队排名");

        // CSV 数据行
        foreach (var item in items)
        {
            csv.AppendLine($"{item.EngineerName ?? "未知"}," +
                          $"{item.PeriodType}," +
                          $"{item.PeriodStart}," +
                          $"{item.TotalTickets}," +
                          $"{item.TicketsResolved}," +
                          $"{item.AverageResolutionTime?.ToString() ?? "N/A"}," +
                          $"{item.FirstTimeResolutionRate?.ToString("F2") ?? "N/A"}," +
                          $"{item.OnTimeResponseRate?.ToString("F2") ?? "N/A"}," +
                          $"{item.OverallScore?.ToString("F2") ?? "N/A"}," +
                          $"{item.PerformanceLevel ?? "N/A"}," +
                          $"{item.RankInTeam ?? 0}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    #endregion
}


