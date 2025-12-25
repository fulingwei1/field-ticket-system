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

    public AiAnalysisService(
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<AiAnalysisService> logger)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
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

    #endregion
}

