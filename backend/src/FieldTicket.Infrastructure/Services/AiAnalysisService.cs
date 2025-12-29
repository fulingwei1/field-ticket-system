using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// AI分析服务实现
/// </summary>
public class AiAnalysisService : IAiAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiAnalysisService> _logger;
    private readonly ILLMService _llmService;

    public AiAnalysisService(
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILLMService llmService,
        ILogger<AiAnalysisService> logger)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _llmService = llmService;
        _logger = logger;
    }

    private IPerformanceService GetPerformanceService()
    {
        return _serviceProvider.GetRequiredService<IPerformanceService>();
    }

    public async Task<AiAnalysisResultDto> GenerateDailySummaryAsync(
        Guid engineerId,
        DateOnly analysisDate,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Generating daily summary for engineer {EngineerId} on {AnalysisDate}",
            engineerId, analysisDate);

        // 获取工程师信息
        var engineer = await _dbContext.Users.FindAsync(engineerId);
        if (engineer == null)
        {
            throw new KeyNotFoundException($"Engineer {engineerId} not found");
        }

        // 获取当日工单数据
        var startDate = analysisDate.ToDateTime(TimeOnly.MinValue);
        var endDate = analysisDate.ToDateTime(TimeOnly.MaxValue);

        var tickets = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.CreatedAt >= startDate &&
                t.CreatedAt <= endDate)
            .ToListAsync();

        var resolvedTickets = tickets.Where(t => t.Status == "Closed").ToList();
        var pendingTickets = tickets.Where(t => t.Status != "Closed" && t.Status != "Draft").ToList();

        // 计算关键指标
        var totalTickets = tickets.Count;
        var resolvedCount = resolvedTickets.Count;
        var averageResolutionTime = resolvedTickets.Any()
            ? resolvedTickets.Average(t => t.ClosedAt.HasValue && t.CreatedAt != default
                ? (t.ClosedAt!.Value - t.CreatedAt).TotalHours
                : 0)
            : 0;

        // 生成工作摘要（这里使用规则引擎生成，实际应该调用 AI API）
        var summary = GenerateDailySummaryText(engineer.Name, totalTickets, resolvedCount, averageResolutionTime);

        // 生成关键洞察
        var keyInsights = new
        {
            total_tickets = totalTickets,
            resolved_tickets = resolvedCount,
            pending_tickets = pendingTickets.Count,
            average_resolution_hours = Math.Round(averageResolutionTime, 2),
            workload_assessment = GetWorkloadAssessment(totalTickets, averageResolutionTime),
            peak_periods = GetPeakPeriods(tickets),
        };

        // 生成建议
        var suggestions = new
        {
            workload_suggestions = GetWorkloadSuggestions(totalTickets, averageResolutionTime),
            improvement_suggestions = GetImprovementSuggestions(tickets, resolvedTickets),
        };

        // 保存分析结果
        var analysisResult = new AiAnalysisResult
        {
            AnalysisId = Guid.NewGuid(),
            AnalysisType = "daily_summary",
            AnalysisDate = analysisDate,
            EngineerId = engineerId,
            Summary = summary,
            KeyInsights = JsonDocument.Parse(JsonSerializer.Serialize(keyInsights)),
            Suggestions = JsonDocument.Parse(JsonSerializer.Serialize(suggestions)),
            AiModel = "rule_engine_v1", // 实际应该使用 AI 模型名称
            ConfidenceScore = 0.85m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

        _dbContext.AiAnalysisResults.Add(analysisResult);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Daily summary generated for engineer {EngineerId}, analysis ID: {AnalysisId}",
            engineerId, analysisResult.AnalysisId);

        return MapToDto(analysisResult, engineer);
    }

    public async Task<AiAnalysisResultDto> GenerateWeeklySummaryAsync(
        Guid engineerId,
        DateOnly weekStart,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Generating weekly summary for engineer {EngineerId} from {WeekStart}",
            engineerId, weekStart);

        var engineer = await _dbContext.Users.FindAsync(engineerId);
        if (engineer == null)
        {
            throw new KeyNotFoundException($"Engineer {engineerId} not found");
        }

        var weekEnd = weekStart.AddDays(6);
        var startDate = weekStart.ToDateTime(TimeOnly.MinValue);
        var endDate = weekEnd.ToDateTime(TimeOnly.MaxValue);

        var tickets = await _dbContext.Tickets
            .Where(t =>
                t.CreatedByUserId == engineerId &&
                t.CreatedAt >= startDate &&
                t.CreatedAt <= endDate)
            .ToListAsync();

        var resolvedTickets = tickets.Where(t => t.Status == "Closed").ToList();
        var totalTickets = tickets.Count;
        var averageResolutionTime = resolvedTickets.Any()
            ? resolvedTickets.Average(t => t.ClosedAt.HasValue && t.CreatedAt != default
                ? (t.ClosedAt!.Value - t.CreatedAt).TotalHours
                : 0)
            : 0;

        // 获取周度绩效数据
        var weeklyMetrics = await GetPerformanceService().GetEngineerMetricsAsync(
            engineerId,
            "weekly",
            weekStart);

        var summary = GenerateWeeklySummaryText(
            engineer.Name,
            weekStart,
            weekEnd,
            totalTickets,
            resolvedTickets.Count,
            weeklyMetrics?.OverallScore);

        var keyInsights = new
        {
            week_start = weekStart.ToString("yyyy-MM-dd"),
            week_end = weekEnd.ToString("yyyy-MM-dd"),
            total_tickets = totalTickets,
            resolved_tickets = resolvedTickets.Count,
            average_resolution_hours = Math.Round(averageResolutionTime, 2),
            overall_score = weeklyMetrics?.OverallScore,
            performance_trend = GetPerformanceTrend(weeklyMetrics),
        };

        var suggestions = new
        {
            weekly_highlights = GetWeeklyHighlights(tickets, resolvedTickets),
            improvement_areas = GetWeeklyImprovementAreas(weeklyMetrics),
        };

        var analysisResult = new AiAnalysisResult
        {
            AnalysisId = Guid.NewGuid(),
            AnalysisType = "weekly_summary",
            AnalysisDate = weekStart,
            EngineerId = engineerId,
            Summary = summary,
            KeyInsights = JsonDocument.Parse(JsonSerializer.Serialize(keyInsights)),
            Suggestions = JsonDocument.Parse(JsonSerializer.Serialize(suggestions)),
            AiModel = "rule_engine_v1",
            ConfidenceScore = 0.85m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

        _dbContext.AiAnalysisResults.Add(analysisResult);
        await _dbContext.SaveChangesAsync();

        return MapToDto(analysisResult, engineer);
    }

    public async Task<AiAnalysisResultDto> GenerateTeamAnalysisAsync(
        Guid? departmentId,
        DateOnly analysisDate,
        string periodType,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Generating team analysis for department {DepartmentId} on {AnalysisDate}",
            departmentId, analysisDate);

        // 获取团队绩效数据
        var teamMetrics = await GetPerformanceService().GetTeamMetricsAsync(
            departmentId,
            periodType,
            analysisDate);

        if (!teamMetrics.Any())
        {
            throw new InvalidOperationException("No team metrics found for the specified period");
        }

        // 分析团队整体状况
        var totalTickets = teamMetrics.Sum(m => m.TotalTickets);
        var totalResolved = teamMetrics.Sum(m => m.TicketsResolved);
        var averageScore = teamMetrics
            .Where(m => m.OverallScore.HasValue)
            .Select(m => m.OverallScore!.Value)
            .DefaultIfEmpty(0)
            .Average();

        var summary = GenerateTeamAnalysisText(
            teamMetrics.Count,
            totalTickets,
            totalResolved,
            (double)averageScore);

        // 识别工作负荷不均衡
        var workloadDistribution = teamMetrics
            .Select(m => new
            {
                engineer_id = m.EngineerId.ToString(),
                engineer_name = m.EngineerName,
                total_tickets = m.TotalTickets,
                workload_level = GetWorkloadLevel(m.TotalTickets),
            })
            .ToList();

        var keyInsights = new
        {
            team_size = teamMetrics.Count,
            total_tickets = totalTickets,
            total_resolved = totalResolved,
            average_score = Math.Round(averageScore, 2),
            workload_distribution = workloadDistribution,
            top_performers = teamMetrics
                .Where(m => m.OverallScore.HasValue)
                .OrderByDescending(m => m.OverallScore)
                .Take(3)
                .Select(m => new
                {
                    engineer_id = m.EngineerId.ToString(),
                    engineer_name = m.EngineerName,
                    overall_score = m.OverallScore,
                })
                .ToList(),
        };

        // 生成改进建议
        var suggestions = new
        {
            workload_balance_suggestions = GetWorkloadBalanceSuggestions(teamMetrics),
            performance_improvement_suggestions = GetPerformanceImprovementSuggestions(teamMetrics),
        };

        var analysisResult = new AiAnalysisResult
        {
            AnalysisId = Guid.NewGuid(),
            AnalysisType = "team_analysis",
            AnalysisDate = analysisDate,
            DepartmentId = departmentId,
            Summary = summary,
            KeyInsights = JsonDocument.Parse(JsonSerializer.Serialize(keyInsights)),
            Suggestions = JsonDocument.Parse(JsonSerializer.Serialize(suggestions)),
            PerformanceAnalysis = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                team_average_score = averageScore,
                performance_distribution = teamMetrics
                    .GroupBy(m => m.PerformanceLevel)
                    .Select(g => new { level = g.Key, count = g.Count() })
                    .ToList(),
            })),
            AiModel = "rule_engine_v1",
            ConfidenceScore = 0.80m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

        _dbContext.AiAnalysisResults.Add(analysisResult);
        await _dbContext.SaveChangesAsync();

        return MapToDto(analysisResult, null);
    }

    public async Task<AiAnalysisResultDto> GenerateSchedulingSuggestionAsync(
        Guid? departmentId,
        DateOnly analysisDate,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Generating scheduling suggestions for department {DepartmentId} on {AnalysisDate}",
            departmentId, analysisDate);

        // 获取团队绩效数据
        var teamMetrics = await GetPerformanceService().GetTeamMetricsAsync(
            departmentId,
            "monthly",
            analysisDate);

        if (!teamMetrics.Any())
        {
            throw new InvalidOperationException("No team metrics found");
        }

        // 分析工作负荷分布
        var workloadAnalysis = teamMetrics
            .Select(m => new
            {
                engineer_id = m.EngineerId.ToString(),
                engineer_name = m.EngineerName,
                current_tickets = m.TotalTickets,
                resolved_tickets = m.TicketsResolved,
                average_resolution_time = m.AverageResolutionTime?.ToString(),
                workload_level = GetWorkloadLevel(m.TotalTickets),
            })
            .ToList();

        // 生成人员分配建议
        var schedulingSuggestions = GenerateSchedulingSuggestions(teamMetrics);

        var summary = $"基于 {analysisDate:yyyy-MM-dd} 的团队绩效数据，生成了人员安排建议。";

        var keyInsights = new
        {
            workload_analysis = workloadAnalysis,
            workload_balance_score = CalculateWorkloadBalanceScore(teamMetrics),
        };

        var suggestions = new
        {
            scheduling_suggestions = schedulingSuggestions,
            workload_balance = GetWorkloadBalanceRecommendations(teamMetrics),
        };

        var analysisResult = new AiAnalysisResult
        {
            AnalysisId = Guid.NewGuid(),
            AnalysisType = "scheduling_suggestion",
            AnalysisDate = analysisDate,
            DepartmentId = departmentId,
            Summary = summary,
            KeyInsights = JsonDocument.Parse(JsonSerializer.Serialize(keyInsights)),
            Suggestions = JsonDocument.Parse(JsonSerializer.Serialize(suggestions)),
            AiModel = "rule_engine_v1",
            ConfidenceScore = 0.75m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

        _dbContext.AiAnalysisResults.Add(analysisResult);
        await _dbContext.SaveChangesAsync();

        return MapToDto(analysisResult, null);
    }

    public async Task<(List<AiAnalysisResultDto> Items, int Total)> GetAnalysisResultsAsync(
        AiAnalysisQueryFilter filter,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbContext.AiAnalysisResults
            .Include(a => a.Engineer)
            .Include(a => a.Creator)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.AnalysisType))
        {
            query = query.Where(a => a.AnalysisType == filter.AnalysisType);
        }

        if (filter.EngineerId.HasValue)
        {
            query = query.Where(a => a.EngineerId == filter.EngineerId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(a => a.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.AnalysisDateFrom.HasValue)
        {
            query = query.Where(a => a.AnalysisDate >= filter.AnalysisDateFrom.Value);
        }

        if (filter.AnalysisDateTo.HasValue)
        {
            query = query.Where(a => a.AnalysisDate <= filter.AnalysisDateTo.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.AnalysisDate)
            .ThenByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items.Select(a => MapToDto(a, a.Engineer)).ToList(), total);
    }

    public async Task<AiAnalysisResultDto?> GetAnalysisResultAsync(Guid analysisId)
    {
        var result = await _dbContext.AiAnalysisResults
            .Include(a => a.Engineer)
            .Include(a => a.Creator)
            .FirstOrDefaultAsync(a => a.AnalysisId == analysisId);

        return result == null ? null : MapToDto(result, result.Engineer);
    }

    #region 辅助方法

    private string GenerateDailySummaryText(string engineerName, int totalTickets, int resolvedCount, double avgHours)
    {
        return $"{engineerName} 在当日共处理 {totalTickets} 个工单，其中 {resolvedCount} 个已解决。" +
               $"平均解决时间为 {avgHours:F1} 小时。" +
               (totalTickets > 10
                   ? "工作负荷较高，建议合理安排时间。"
                   : totalTickets < 3
                       ? "工作负荷较轻，可以关注知识沉淀和技能提升。"
                       : "工作负荷正常。");
    }

    private string GenerateWeeklySummaryText(
        string engineerName,
        DateOnly weekStart,
        DateOnly weekEnd,
        int totalTickets,
        int resolvedCount,
        decimal? overallScore)
    {
        var scoreText = overallScore.HasValue ? $"综合评分为 {overallScore.Value:F1} 分。" : "";
        return $"{engineerName} 在 {weekStart:yyyy-MM-dd} 至 {weekEnd:yyyy-MM-dd} 期间，" +
               $"共处理 {totalTickets} 个工单，其中 {resolvedCount} 个已解决。{scoreText}";
    }

    private string GenerateTeamAnalysisText(int teamSize, int totalTickets, int totalResolved, double avgScore)
    {
        return $"团队共有 {teamSize} 名成员，本周期共处理 {totalTickets} 个工单，" +
               $"其中 {totalResolved} 个已解决。团队平均绩效评分为 {avgScore:F1} 分。";
    }

    private string GetWorkloadAssessment(int totalTickets, double avgHours)
    {
        if (totalTickets > 15 || avgHours > 8)
            return "超负荷";
        if (totalTickets > 8 || avgHours > 4)
            return "繁忙";
        return "正常";
    }

    private List<string> GetPeakPeriods(List<Ticket> tickets)
    {
        // 简化实现：返回工单创建时间分布
        var hourGroups = tickets
            .GroupBy(t => t.CreatedAt.Hour)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"{g.Key:00}:00-{g.Key + 1:00}:00")
            .ToList();

        return hourGroups.Any() ? hourGroups : new List<string> { "09:00-11:00", "14:00-16:00" };
    }

    private List<object> GetWorkloadSuggestions(int totalTickets, double avgHours)
    {
        var suggestions = new List<object>();

        if (totalTickets > 15)
        {
            suggestions.Add(new
            {
                type = "workload",
                suggestion = "工单数量较多，建议考虑工作负荷分配或优先级调整",
                priority = "high",
            });
        }

        if (avgHours > 8)
        {
            suggestions.Add(new
            {
                type = "efficiency",
                suggestion = "平均解决时间较长，建议优化工作流程或寻求技术支持",
                priority = "medium",
            });
        }

        return suggestions;
    }

    private List<object> GetImprovementSuggestions(List<Ticket> allTickets, List<Ticket> resolvedTickets)
    {
        var suggestions = new List<object>();

        if (resolvedTickets.Count < allTickets.Count * 0.8)
        {
            suggestions.Add(new
            {
                type = "resolution_rate",
                suggestion = "解决率有待提升，建议关注未解决工单的处理进度",
                priority = "medium",
            });
        }

        return suggestions;
    }

    private string GetPerformanceTrend(PerformanceMetricsDto? metrics)
    {
        if (metrics == null) return "无数据";
        if (metrics.OverallScore >= 90) return "优秀";
        if (metrics.OverallScore >= 80) return "良好";
        if (metrics.OverallScore >= 70) return "稳定";
        return "待改进";
    }

    private List<object> GetWeeklyHighlights(List<Ticket> tickets, List<Ticket> resolvedTickets)
    {
        return new List<object>
        {
            new { highlight = $"本周共处理 {tickets.Count} 个工单", type = "volume" },
            new { highlight = $"成功解决 {resolvedTickets.Count} 个工单", type = "resolution" },
        };
    }

    private List<object> GetWeeklyImprovementAreas(PerformanceMetricsDto? metrics)
    {
        var areas = new List<object>();

        if (metrics?.FirstTimeResolutionRate < 80)
        {
            areas.Add(new { area = "一次解决率", current = $"{metrics.FirstTimeResolutionRate:F1}%", target = "≥ 80%" });
        }

        if (metrics?.OnTimeResponseRate < 90)
        {
            areas.Add(new { area = "响应及时率", current = $"{metrics.OnTimeResponseRate:F1}%", target = "≥ 90%" });
        }

        return areas;
    }

    private string GetWorkloadLevel(int totalTickets)
    {
        if (totalTickets > 20) return "超负荷";
        if (totalTickets > 10) return "繁忙";
        if (totalTickets > 5) return "正常";
        return "轻松";
    }

    private List<object> GetWorkloadBalanceSuggestions(List<PerformanceMetricsDto> teamMetrics)
    {
        var suggestions = new List<object>();
        var avgTickets = teamMetrics.Average(m => m.TotalTickets);

        var overloaded = teamMetrics.Where(m => m.TotalTickets > avgTickets * 1.5).ToList();
        var underloaded = teamMetrics.Where(m => m.TotalTickets < avgTickets * 0.5).ToList();

        if (overloaded.Any() && underloaded.Any())
        {
            suggestions.Add(new
            {
                type = "workload_balance",
                suggestion = $"建议将部分工单从 {string.Join("、", overloaded.Select(m => m.EngineerName))} " +
                           $"转移到 {string.Join("、", underloaded.Select(m => m.EngineerName))}",
                priority = "high",
            });
        }

        return suggestions;
    }

    private List<object> GetPerformanceImprovementSuggestions(List<PerformanceMetricsDto> teamMetrics)
    {
        var suggestions = new List<object>();

        var lowPerformers = teamMetrics
            .Where(m => m.OverallScore.HasValue && m.OverallScore < 70)
            .ToList();

        if (lowPerformers.Any())
        {
            suggestions.Add(new
            {
                type = "performance_improvement",
                suggestion = $"以下工程师需要重点关注：{string.Join("、", lowPerformers.Select(m => m.EngineerName))}",
                priority = "high",
            });
        }

        return suggestions;
    }

    private List<object> GenerateSchedulingSuggestions(List<PerformanceMetricsDto> teamMetrics)
    {
        var suggestions = new List<object>();

        // 根据工作负荷和专业能力生成建议
        foreach (var engineer in teamMetrics.OrderByDescending(m => m.OverallScore))
        {
            if (engineer.TotalTickets < 5 && engineer.OverallScore >= 80)
            {
                suggestions.Add(new
                {
                    engineer_id = engineer.EngineerId.ToString(),
                    engineer_name = engineer.EngineerName,
                    suggestion = "可以承担更多工单，建议优先分配",
                    reason = "工作负荷较轻且绩效优秀",
                    priority = "high",
                });
            }
        }

        return suggestions;
    }

    private List<object> GetWorkloadBalanceRecommendations(List<PerformanceMetricsDto> teamMetrics)
    {
        var recommendations = new List<object>();
        var avgTickets = teamMetrics.Average(m => m.TotalTickets);

        foreach (var engineer in teamMetrics)
        {
            if (engineer.TotalTickets > avgTickets * 1.5)
            {
                recommendations.Add(new
                {
                    from_engineer = engineer.EngineerName,
                    suggestion = "建议减少工单分配，当前工作负荷过高",
                    expected_improvement = "预计可以提升工作质量和响应速度",
                });
            }
        }

        return recommendations;
    }

    private decimal CalculateWorkloadBalanceScore(List<PerformanceMetricsDto> teamMetrics)
    {
        if (teamMetrics.Count < 2) return 100;

        var tickets = teamMetrics.Select(m => (decimal)m.TotalTickets).ToList();
        var avg = tickets.Average();
        var variance = tickets.Sum(t => (t - avg) * (t - avg)) / tickets.Count;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        // 计算平衡分数（标准差越小，平衡度越高）
        var maxStdDev = avg * 0.5m; // 假设最大标准差为平均值的50%
        var balanceScore = Math.Max(0, 100 - (stdDev / maxStdDev) * 100);

        return Math.Round(balanceScore, 2);
    }

    private AiAnalysisResultDto MapToDto(AiAnalysisResult result, User? engineer)
    {
        return new AiAnalysisResultDto
        {
            AnalysisId = result.AnalysisId,
            AnalysisType = result.AnalysisType,
            AnalysisDate = result.AnalysisDate,
            EngineerId = result.EngineerId,
            EngineerName = engineer?.Name ?? result.Engineer?.Name,
            DepartmentId = result.DepartmentId,
            Summary = result.Summary,
            KeyInsights = result.KeyInsights,
            Suggestions = result.Suggestions,
            PerformanceAnalysis = result.PerformanceAnalysis,
            AiModel = result.AiModel,
            ConfidenceScore = result.ConfidenceScore,
            CreatedAt = result.CreatedAt,
            CreatedBy = result.CreatedBy,
            CreatedByName = result.Creator?.Name,
        };
    }

    public async Task<SkillLevelAnalysisDto> AnalyzeSkillLevelAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Analyzing skill level for engineer {EngineerId}, period {PeriodType} from {PeriodStart}",
            engineerId, periodType, periodStart);

        var engineer = await _dbContext.Users.FindAsync(engineerId);
        if (engineer == null)
        {
            throw new KeyNotFoundException($"Engineer {engineerId} not found");
        }

        // 计算周期结束日期
        var periodEnd = CalculatePeriodEnd(periodType, periodStart);
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        // 获取绩效数据
        var performanceMetrics = await GetPerformanceService().GetEngineerMetricsAsync(
            engineerId, periodType, periodStart);

        // 获取工单数据
        var tickets = await _dbContext.Tickets
            .Where(t => t.CreatedByUserId == engineerId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate &&
                       t.Status != "Draft")
            .OrderByDescending(t => t.CreatedAt)
            .Take(100) // 限制数量以避免prompt过长
            .ToListAsync();

        // 获取判断卡使用记录
        var judgementCardUsages = await _dbContext.JudgementCardUsageHistories
            .Where(jc => jc.UsedBy == engineerId &&
                        jc.UsedAt >= startDate &&
                        jc.UsedAt <= endDate)
            .ToListAsync();

        // 获取创建的判断卡和解决方案
        var createdJudgementCards = await _dbContext.JudgementCards
            .Where(jc => jc.CreatedBy == engineerId &&
                        jc.CreatedAt >= startDate &&
                        jc.CreatedAt <= endDate)
            .ToListAsync();

        var createdSolutions = await _dbContext.Solutions
            .Where(s => s.CreatedBy == engineerId &&
                       s.CreatedAt >= startDate &&
                       s.CreatedAt <= endDate)
            .ToListAsync();

        // 获取客户沟通记录
        var communications = await _dbContext.CustomerCommunications
            .Where(c => c.CommunicatedBy == engineerId &&
                       c.CommunicatedAt >= startDate &&
                       c.CommunicatedAt <= endDate)
            .ToListAsync();

        // 构建分析数据
        var analysisData = BuildSkillAnalysisData(
            engineer, performanceMetrics, tickets, judgementCardUsages,
            createdJudgementCards, createdSolutions, communications, periodStart, periodEnd);

        // 使用LLM分析技能水平
        var prompt = BuildSkillLevelAnalysisPrompt(analysisData);
        var llmResponse = await _llmService.GenerateStructuredAsync<SkillLevelAnalysisResponse>(
            prompt,
            null,
            new LLMRequestOptions { Model = "gemini-pro", Temperature = 0.3 });

        // 构建返回结果
        var result = new SkillLevelAnalysisDto
        {
            AnalysisId = Guid.NewGuid(),
            EngineerId = engineerId,
            EngineerName = engineer.Name,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            OverallSkillLevel = llmResponse.OverallSkillLevel,
            SkillScore = llmResponse.SkillScore,
            Summary = llmResponse.Summary,
            Strengths = llmResponse.Strengths ?? new List<string>(),
            ImprovementAreas = llmResponse.ImprovementAreas ?? new List<string>(),
            SkillDimensions = llmResponse.SkillDimensions?.ToDictionary(
                d => d.DimensionName,
                d => new SkillDimensionScore
                {
                    DimensionName = d.DimensionName,
                    Score = d.Score,
                    Level = d.Level,
                    Description = d.Description,
                    Evidence = d.Evidence ?? new List<string>()
                }) ?? new Dictionary<string, SkillDimensionScore>(),
            SkillAssessment = JsonDocument.Parse(JsonSerializer.Serialize(llmResponse)),
            AiModel = "gemini-pro",
            ConfidenceScore = 0.85m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _logger.LogInformation(
            "Skill level analysis completed for engineer {EngineerId}, overall level: {Level}, score: {Score}",
            engineerId, result.OverallSkillLevel, result.SkillScore);

        return result;
    }

    public async Task<DevelopmentSuggestionDto> GenerateDevelopmentSuggestionAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Generating development suggestions for engineer {EngineerId}, period {PeriodType} from {PeriodStart}",
            engineerId, periodType, periodStart);

        var engineer = await _dbContext.Users.FindAsync(engineerId);
        if (engineer == null)
        {
            throw new KeyNotFoundException($"Engineer {engineerId} not found");
        }

        // 先获取技能水平分析
        var skillAnalysis = await AnalyzeSkillLevelAsync(engineerId, periodType, periodStart, createdBy);

        // 获取绩效数据
        var performanceMetrics = await GetPerformanceService().GetEngineerMetricsAsync(
            engineerId, periodType, periodStart);

        var periodEnd = CalculatePeriodEnd(periodType, periodStart);
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        // 获取工单数据用于分析发展特点
        var tickets = await _dbContext.Tickets
            .Where(t => t.CreatedByUserId == engineerId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate &&
                       t.Status != "Draft")
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync();

        // 构建发展建议数据
        var developmentData = BuildDevelopmentSuggestionData(
            engineer, skillAnalysis, performanceMetrics, tickets, periodStart, periodEnd);

        // 使用LLM生成发展建议
        var prompt = BuildDevelopmentSuggestionPrompt(developmentData);
        var llmResponse = await _llmService.GenerateStructuredAsync<DevelopmentSuggestionResponse>(
            prompt,
            null,
            new LLMRequestOptions { Model = "gemini-pro", Temperature = 0.4 });

        // 构建返回结果
        var result = new DevelopmentSuggestionDto
        {
            AnalysisId = Guid.NewGuid(),
            EngineerId = engineerId,
            EngineerName = engineer.Name,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            DevelopmentCharacteristics = llmResponse.DevelopmentCharacteristics ?? string.Empty,
            Suggestions = llmResponse.Suggestions?.Select(s => new DevelopmentSuggestionItem
            {
                Category = s.Category,
                Title = s.Title,
                Description = s.Description,
                Priority = s.Priority,
                ActionItems = s.ActionItems ?? new List<string>(),
                ExpectedOutcome = s.ExpectedOutcome
            }).ToList() ?? new List<DevelopmentSuggestionItem>(),
            ShortTermGoals = llmResponse.ShortTermGoals ?? new List<string>(),
            MediumTermGoals = llmResponse.MediumTermGoals ?? new List<string>(),
            LongTermGoals = llmResponse.LongTermGoals ?? new List<string>(),
            RecommendedResources = llmResponse.RecommendedResources ?? new List<string>(),
            AiModel = "gemini-pro",
            ConfidenceScore = 0.85m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _logger.LogInformation(
            "Development suggestions generated for engineer {EngineerId}, {SuggestionCount} suggestions",
            engineerId, result.Suggestions.Count);

        return result;
    }

    public async Task<PerformanceEvaluationDto> GeneratePerformanceEvaluationAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid? createdBy = null)
    {
        _logger.LogInformation(
            "Generating performance evaluation for engineer {EngineerId}, period {PeriodType} from {PeriodStart}",
            engineerId, periodType, periodStart);

        var engineer = await _dbContext.Users.FindAsync(engineerId);
        if (engineer == null)
        {
            throw new KeyNotFoundException($"Engineer {engineerId} not found");
        }

        // 获取绩效数据
        var performanceMetrics = await GetPerformanceService().GetEngineerMetricsAsync(
            engineerId, periodType, periodStart);

        if (performanceMetrics == null)
        {
            throw new InvalidOperationException($"Performance metrics not found for engineer {engineerId}");
        }

        var periodEnd = CalculatePeriodEnd(periodType, periodStart);
        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MaxValue);

        // 获取工单数据
        var tickets = await _dbContext.Tickets
            .Where(t => t.CreatedByUserId == engineerId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate &&
                       t.Status != "Draft")
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();

        // 获取团队平均绩效用于对比
        var teamMetrics = await GetPerformanceService().GetTeamMetricsAsync(
            null, periodType, periodStart);
        var teamAverageScore = teamMetrics.Any() && teamMetrics.Any(m => m.OverallScore.HasValue)
            ? teamMetrics.Where(m => m.OverallScore.HasValue).Average(m => m.OverallScore!.Value)
            : (decimal?)null;

        // 构建绩效评价数据
        var evaluationData = BuildPerformanceEvaluationData(
            engineer, performanceMetrics, tickets, teamAverageScore, periodStart, periodEnd);

        // 使用LLM生成绩效评价
        var prompt = BuildPerformanceEvaluationPrompt(evaluationData);
        var llmResponse = await _llmService.GenerateStructuredAsync<PerformanceEvaluationResponse>(
            prompt,
            null,
            new LLMRequestOptions { Model = "gemini-pro", Temperature = 0.3 });

        // 构建返回结果
        var result = new PerformanceEvaluationDto
        {
            AnalysisId = Guid.NewGuid(),
            EngineerId = engineerId,
            EngineerName = engineer.Name,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            EvaluationSummary = llmResponse.EvaluationSummary ?? string.Empty,
            PerformanceLevel = llmResponse.PerformanceLevel ?? performanceMetrics.PerformanceLevel ?? "合格",
            OverallScore = llmResponse.OverallScore > 0 ? llmResponse.OverallScore : (performanceMetrics.OverallScore ?? 0),
            DimensionEvaluations = llmResponse.DimensionEvaluations?.ToDictionary(
                d => d.DimensionName,
                d => new DimensionEvaluation
                {
                    DimensionName = d.DimensionName,
                    Score = d.Score,
                    Level = d.Level,
                    Evaluation = d.Evaluation,
                    Strengths = d.Strengths ?? new List<string>(),
                    Weaknesses = d.Weaknesses ?? new List<string>()
                }) ?? new Dictionary<string, DimensionEvaluation>(),
            Highlights = llmResponse.Highlights ?? new List<string>(),
            AreasForImprovement = llmResponse.AreasForImprovement ?? new List<string>(),
            Evidence = llmResponse.Evidence?.Select(e => new PerformanceEvidence
            {
                Type = e.Type,
                Description = e.Description,
                ReferenceId = e.ReferenceId,
                OccurredAt = e.OccurredAt
            }).ToList() ?? new List<PerformanceEvidence>(),
            Comparison = teamAverageScore.HasValue ? new ComparisonWithAverage
            {
                TeamAverageScore = teamAverageScore.Value,
                DepartmentAverageScore = teamAverageScore.Value, // 简化处理
                ComparisonSummary = llmResponse.Comparison?.ComparisonSummary ?? string.Empty,
                Advantages = llmResponse.Comparison?.Advantages ?? new List<string>(),
                Gaps = llmResponse.Comparison?.Gaps ?? new List<string>()
            } : null,
            AiModel = "gemini-pro",
            ConfidenceScore = 0.85m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _logger.LogInformation(
            "Performance evaluation generated for engineer {EngineerId}, level: {Level}, score: {Score}",
            engineerId, result.PerformanceLevel, result.OverallScore);

        return result;
    }

    #region 辅助方法 - 技能水平分析

    private object BuildSkillAnalysisData(
        User engineer,
        PerformanceMetricsDto? performanceMetrics,
        List<Ticket> tickets,
        List<JudgementCardUsageHistory> judgementCardUsages,
        List<JudgementCard> createdJudgementCards,
        List<Solution> createdSolutions,
        List<CustomerCommunication> communications,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        return new
        {
            engineer = new
            {
                id = engineer.Id.ToString(),
                name = engineer.Name,
                role = engineer.Role
            },
            period = new
            {
                start = periodStart.ToString("yyyy-MM-dd"),
                end = periodEnd.ToString("yyyy-MM-dd")
            },
            performance_metrics = performanceMetrics != null ? new
            {
                overall_score = performanceMetrics.OverallScore,
                performance_level = performanceMetrics.PerformanceLevel,
                total_tickets = performanceMetrics.TotalTickets,
                tickets_resolved = performanceMetrics.TicketsResolved,
                first_time_resolution_rate = performanceMetrics.FirstTimeResolutionRate,
                average_resolution_time_hours = performanceMetrics.AverageResolutionTime?.TotalHours,
                ticket_creation_completeness = performanceMetrics.TicketCreationCompleteness,
                judgement_card_usage_accuracy = performanceMetrics.JudgementCardUsageAccuracy,
                judgement_card_hit_rate = performanceMetrics.JudgementCardHitRate,
                judgement_cards_created = performanceMetrics.JudgementCardsCreated,
                solutions_contributed = performanceMetrics.SolutionsContributed,
                customer_satisfaction_score = performanceMetrics.CustomerSatisfactionScore
            } : null,
            tickets_summary = new
            {
                total_count = tickets.Count,
                resolved_count = tickets.Count(t => t.Status == "Closed"),
                by_domain = tickets.GroupBy(t => t.Domain).Select(g => new
                {
                    domain = g.Key.ToString(),
                    count = g.Count()
                }).ToList(),
                by_priority = tickets.GroupBy(t => t.Priority).Select(g => new
                {
                    priority = g.Key,
                    count = g.Count()
                }).ToList()
            },
            judgement_card_usage = new
            {
                total_usages = judgementCardUsages.Count,
                valid_usages = judgementCardUsages.Count(jc => jc.IsValid),
                accuracy_rate = judgementCardUsages.Any() ?
                    (decimal)judgementCardUsages.Count(jc => jc.Result == "correct") / judgementCardUsages.Count * 100 : 0
            },
            knowledge_contribution = new
            {
                judgement_cards_created = createdJudgementCards.Count,
                solutions_created = createdSolutions.Count
            },
            customer_communication = new
            {
                total_communications = communications.Count,
                by_type = communications.GroupBy(c => c.CommunicationType).Select(g => new
                {
                    type = g.Key,
                    count = g.Count()
                }).ToList()
            }
        };
    }

    private string BuildSkillLevelAnalysisPrompt(object analysisData)
    {
        return $@"你是一位资深的客服工程师技能评估专家。请基于以下数据，分析该客服工程师的技能水平。

数据：
{JsonSerializer.Serialize(analysisData, new JsonSerializerOptions { WriteIndented = true })}

请从以下维度评估技能水平：
1. **技术诊断能力**：判断卡使用准确率、问题诊断准确性、技术问题解决能力
2. **问题解决能力**：一次解决率、平均解决时间、问题处理效率
3. **知识贡献能力**：判断卡创建数量和质量、解决方案贡献、知识分享
4. **客户服务能力**：客户沟通质量、客户满意度、响应及时性
5. **工作规范性**：工单创建完整度、信息记录规范性、流程遵循度
6. **学习成长能力**：技能提升速度、新知识掌握能力、问题处理能力改进

请返回JSON格式的分析结果，包含：
- overallSkillLevel: 总体技能水平（初级/中级/高级/专家）
- skillScore: 技能评分（0-100）
- summary: 技能水平总结（200-300字）
- strengths: 关键优势列表（3-5项）
- improvementAreas: 需要改进的领域（3-5项）
- skillDimensions: 各维度评分数组，每个维度包含：
  - dimensionName: 维度名称
  - score: 评分（0-100）
  - level: 水平等级（初级/中级/高级/专家）
  - description: 维度评价描述（100-150字）
  - evidence: 支撑证据列表（2-3项具体案例）

请确保分析客观、准确，基于数据说话。";
    }

    #endregion

    #region 辅助方法 - 发展建议

    private object BuildDevelopmentSuggestionData(
        User engineer,
        SkillLevelAnalysisDto skillAnalysis,
        PerformanceMetricsDto? performanceMetrics,
        List<Ticket> tickets,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        return new
        {
            engineer = new
            {
                id = engineer.Id.ToString(),
                name = engineer.Name,
                role = engineer.Role
            },
            period = new
            {
                start = periodStart.ToString("yyyy-MM-dd"),
                end = periodEnd.ToString("yyyy-MM-dd")
            },
            skill_analysis = new
            {
                overall_level = skillAnalysis.OverallSkillLevel,
                skill_score = skillAnalysis.SkillScore,
                strengths = skillAnalysis.Strengths,
                improvement_areas = skillAnalysis.ImprovementAreas,
                skill_dimensions = skillAnalysis.SkillDimensions.Values.Select(d => new
                {
                    name = d.DimensionName,
                    score = d.Score,
                    level = d.Level
                }).ToList()
            },
            performance_metrics = performanceMetrics != null ? new
            {
                overall_score = performanceMetrics.OverallScore,
                performance_level = performanceMetrics.PerformanceLevel,
                key_indicators = new
                {
                    first_time_resolution_rate = performanceMetrics.FirstTimeResolutionRate,
                    average_resolution_time_hours = performanceMetrics.AverageResolutionTime?.TotalHours,
                    ticket_creation_completeness = performanceMetrics.TicketCreationCompleteness,
                    judgement_card_usage_accuracy = performanceMetrics.JudgementCardUsageAccuracy
                }
            } : null,
            work_patterns = new
            {
                total_tickets = tickets.Count,
                ticket_distribution = tickets.GroupBy(t => t.Domain).Select(g => new
                {
                    domain = g.Key.ToString(),
                    count = g.Count()
                }).ToList()
            }
        };
    }

    private string BuildDevelopmentSuggestionPrompt(object developmentData)
    {
        return $@"你是一位资深的人力资源发展顾问。请基于以下数据，分析该客服工程师的个人发展特点，并给出个性化的发展建议。

数据：
{JsonSerializer.Serialize(developmentData, new JsonSerializerOptions { WriteIndented = true })}

请分析：
1. **个人发展特点**：该工程师的学习风格、工作特点、优势领域、成长轨迹
2. **发展建议**：针对不同维度（技术能力、沟通能力、问题解决、知识贡献等）给出具体建议
3. **目标设定**：
   - 短期目标（3个月）：可快速实现的具体目标
   - 中期目标（6-12个月）：需要一定时间积累的目标
   - 长期目标（1-2年）：职业发展方向性目标
4. **推荐资源**：推荐的学习资源、培训课程、实践机会

请返回JSON格式的结果，包含：
- developmentCharacteristics: 个人发展特点分析（300-400字）
- suggestions: 发展建议数组，每个建议包含：
  - category: 类别（技术能力/沟通能力/问题解决/知识贡献等）
  - title: 建议标题
  - description: 建议描述（100-150字）
  - priority: 优先级（high/medium/low）
  - actionItems: 具体行动项列表（3-5项）
  - expectedOutcome: 预期成果
- shortTermGoals: 短期目标列表（3-5项）
- mediumTermGoals: 中期目标列表（3-5项）
- longTermGoals: 长期目标列表（2-3项）
- recommendedResources: 推荐资源列表（5-8项）

请确保建议具体、可执行，符合该工程师的个人特点和发展阶段。";
    }

    #endregion

    #region 辅助方法 - 绩效评价

    private object BuildPerformanceEvaluationData(
        User engineer,
        PerformanceMetricsDto performanceMetrics,
        List<Ticket> tickets,
        decimal? teamAverageScore,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        return new
        {
            engineer = new
            {
                id = engineer.Id.ToString(),
                name = engineer.Name,
                role = engineer.Role
            },
            period = new
            {
                start = periodStart.ToString("yyyy-MM-dd"),
                end = periodEnd.ToString("yyyy-MM-dd")
            },
            performance_metrics = new
            {
                overall_score = performanceMetrics.OverallScore,
                performance_level = performanceMetrics.PerformanceLevel,
                rank_in_team = performanceMetrics.RankInTeam,
                rank_in_department = performanceMetrics.RankInDepartment,
                total_tickets = performanceMetrics.TotalTickets,
                tickets_resolved = performanceMetrics.TicketsResolved,
                first_time_resolution_rate = performanceMetrics.FirstTimeResolutionRate,
                average_resolution_time_hours = performanceMetrics.AverageResolutionTime?.TotalHours,
                ticket_creation_completeness = performanceMetrics.TicketCreationCompleteness,
                field_feedback_timeliness_rate = performanceMetrics.FieldFeedbackTimelinessRate,
                question_reply_timeliness_rate = performanceMetrics.QuestionReplyTimelinessRate,
                verification_pass_rate = performanceMetrics.VerificationPassRate,
                repeat_problem_rate = performanceMetrics.RepeatProblemRate,
                judgement_card_usage_accuracy = performanceMetrics.JudgementCardUsageAccuracy,
                judgement_card_hit_rate = performanceMetrics.JudgementCardHitRate,
                judgement_cards_created = performanceMetrics.JudgementCardsCreated,
                solutions_contributed = performanceMetrics.SolutionsContributed,
                customer_communication_timeliness = performanceMetrics.CustomerCommunicationTimeliness,
                customer_satisfaction_score = performanceMetrics.CustomerSatisfactionScore
            },
            team_comparison = teamAverageScore.HasValue ? new
            {
                team_average_score = teamAverageScore.Value,
                engineer_score = performanceMetrics.OverallScore ?? 0,
                difference = (performanceMetrics.OverallScore ?? 0) - teamAverageScore.Value
            } : null,
            work_summary = new
            {
                total_tickets = tickets.Count,
                resolved_tickets = tickets.Count(t => t.Status == "Closed"),
                ticket_examples = tickets.Take(5).Select(t => new
                {
                    ticket_no = t.TicketNo,
                    domain = t.Domain.ToString(),
                    priority = t.Priority,
                    status = t.Status,
                    created_at = t.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList()
            }
        };
    }

    private string BuildPerformanceEvaluationPrompt(object evaluationData)
    {
        return $@"你是一位资深的绩效管理专家。请基于以下数据，生成一份全面的绩效评价报告。

数据：
{JsonSerializer.Serialize(evaluationData, new JsonSerializerOptions { WriteIndented = true })}

请从以下维度进行评价：
1. **工单处理效率**：工单创建完整度、响应及时率、反馈及时性
2. **问题解决能力**：一次解决率、平均解决时间、验证通过率
3. **技术诊断能力**：判断卡使用准确率、判断卡命中率
4. **知识贡献**：判断卡创建、解决方案贡献
5. **客户服务能力**：客户沟通及时性、客户满意度
6. **工作规范性**：工单信息完整性、流程遵循度

请返回JSON格式的评价结果，包含：
- evaluationSummary: 综合绩效评价总结（400-500字）
- performanceLevel: 绩效等级（优秀/良好/合格/待改进/不合格）
- overallScore: 综合评分（0-100）
- dimensionEvaluations: 各维度评价数组，每个维度包含：
  - dimensionName: 维度名称
  - score: 评分（0-100）
  - level: 等级（优秀/良好/合格/待改进）
  - evaluation: 详细评价（150-200字）
  - strengths: 优势列表（2-3项）
  - weaknesses: 不足列表（1-2项）
- highlights: 工作亮点列表（3-5项）
- areasForImprovement: 需要改进的方面列表（3-5项）
- evidence: 具体案例和证据数组，每个证据包含：
  - type: 类型（ticket/judgement_card/solution/communication）
  - description: 描述
  - referenceId: 参考ID（如工单号）
  - occurredAt: 发生时间
- comparison: 与平均水平对比（如果提供了团队平均分），包含：
  - comparisonSummary: 对比总结（100-150字）
  - advantages: 相对优势列表（2-3项）
  - gaps: 与平均水平的差距列表（1-2项）

请确保评价客观、公正，既肯定成绩，也指出不足，并给出改进方向。";
    }

    private DateOnly CalculatePeriodEnd(string periodType, DateOnly periodStart)
    {
        return periodType.ToLower() switch
        {
            "daily" => periodStart,
            "weekly" => periodStart.AddDays(6),
            "monthly" => periodStart.AddMonths(1).AddDays(-1),
            "quarterly" => periodStart.AddMonths(3).AddDays(-1),
            "yearly" => periodStart.AddYears(1).AddDays(-1),
            _ => periodStart.AddDays(6) // 默认一周
        };
    }

    #endregion

    #endregion
}

