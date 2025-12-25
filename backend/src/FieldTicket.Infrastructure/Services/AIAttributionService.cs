using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// AI辅助归因服务实现
/// </summary>
public class AIAttributionService : IAIAttributionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AIAttributionService> _logger;

    public AIAttributionService(
        ApplicationDbContext dbContext,
        ILogger<AIAttributionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AttributionSuggestionDto> SuggestAttributionAsync(Guid ticketId)
    {
        _logger.LogInformation("Generating attribution suggestion for ticket {TicketId}", ticketId);

        // 获取工单
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 提取归因特征
        var features = ExtractAttributionFeatures(ticket);

        // 查找相似工单
        var similarTickets = await FindSimilarTicketsAsync(ticket, features);

        // 分析归因模式
        var attributionPattern = AnalyzeAttributionPattern(similarTickets);

        // 生成归因建议
        var suggestion = GenerateSuggestion(ticket, similarTickets, attributionPattern);

        _logger.LogInformation("Generated attribution suggestion: {RootResponsibility}, confidence: {Confidence}",
            suggestion.RootResponsibility, suggestion.Confidence);

        return suggestion;
    }

    public async Task<ConsistencyCheckResultDto> CheckConsistencyAsync(
        Guid ticketId,
        string rootResponsibility,
        bool? isPreventable)
    {
        _logger.LogInformation("Checking attribution consistency for ticket {TicketId}", ticketId);

        // 获取工单
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 提取特征
        var features = ExtractAttributionFeatures(ticket);

        // 查找相似工单
        var similarTickets = await FindSimilarTicketsAsync(ticket, features);

        // 检查一致性
        var issues = new List<InconsistencyIssue>();
        var suggestions = new List<string>();

        // 检查根因分类一致性
        var responsibilityDistribution = similarTickets
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
            .GroupBy(t => t.RootResponsibility)
            .ToDictionary(g => g.Key, g => g.Count());

        if (responsibilityDistribution.Any())
        {
            var mostCommon = responsibilityDistribution.OrderByDescending(kvp => kvp.Value).First();
            if (mostCommon.Key != rootResponsibility)
            {
                issues.Add(new InconsistencyIssue
                {
                    Field = "root_responsibility",
                    CurrentValue = rootResponsibility,
                    ExpectedValue = mostCommon.Key,
                    Reason = $"相似工单中 {mostCommon.Value} 个工单的归因为 {mostCommon.Key}"
                });
                suggestions.Add($"建议将根因分类改为 {mostCommon.Key}，因为相似工单中大多数都是这个分类");
            }
        }

        // 检查可预防性一致性
        var preventabilityDistribution = similarTickets
            .Where(t => t.IsPreventable.HasValue)
            .GroupBy(t => t.IsPreventable!.Value)
            .ToDictionary(g => g.Key ? "preventable" : "non_preventable", g => g.Count());

        if (preventabilityDistribution.Any() && isPreventable.HasValue)
        {
            var mostCommonPreventable = preventabilityDistribution
                .OrderByDescending(kvp => kvp.Value)
                .First();

            var expectedPreventable = mostCommonPreventable.Key == "preventable";
            if (expectedPreventable != isPreventable.Value)
            {
                issues.Add(new InconsistencyIssue
                {
                    Field = "is_preventable",
                    CurrentValue = isPreventable.Value ? "true" : "false",
                    ExpectedValue = expectedPreventable ? "true" : "false",
                    Reason = $"相似工单中 {mostCommonPreventable.Value} 个工单的可预防性为 {expectedPreventable}"
                });
                suggestions.Add($"建议将可预防性改为 {expectedPreventable}，因为相似工单中大多数都是这个值");
            }
        }

        // 计算一致性分数
        var consistencyScore = issues.Count == 0 ? 1.0m : Math.Max(0m, 1.0m - (issues.Count * 0.3m));

        return new ConsistencyCheckResultDto
        {
            IsConsistent = issues.Count == 0,
            ConsistencyScore = consistencyScore,
            Issues = issues,
            Suggestions = suggestions
        };
    }

    public async Task<AttributionEvaluationDto> EvaluateAttributionAsync(
        DateTime? fromDate,
        DateTime? toDate)
    {
        _logger.LogInformation("Evaluating attribution from {FromDate} to {ToDate}", fromDate, toDate);

        var query = _dbContext.Tickets.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        var tickets = await query.ToListAsync();

        var attributedTickets = tickets.Where(t => !string.IsNullOrEmpty(t.RootResponsibility)).ToList();

        var evaluation = new AttributionEvaluationDto
        {
            FromDate = fromDate ?? tickets.Min(t => t.CreatedAt),
            ToDate = toDate ?? tickets.Max(t => t.CreatedAt),
            TotalTickets = tickets.Count,
            AttributedTickets = attributedTickets.Count,
            AttributionRate = tickets.Count > 0 ? (decimal)attributedTickets.Count / tickets.Count : 0m
        };

        // 责任分布
        evaluation.ResponsibilityDistribution = attributedTickets
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
            .GroupBy(t => t.RootResponsibility!)
            .ToDictionary(g => g.Key, g => g.Count());

        // 可预防性统计
        var preventableTickets = attributedTickets.Where(t => t.IsPreventable == true).ToList();
        var nonPreventableTickets = attributedTickets.Where(t => t.IsPreventable == false).ToList();

        evaluation.PreventabilityRate = new Dictionary<string, decimal>
        {
            ["preventable"] = attributedTickets.Count > 0
                ? (decimal)preventableTickets.Count / attributedTickets.Count
                : 0m,
            ["non_preventable"] = attributedTickets.Count > 0
                ? (decimal)nonPreventableTickets.Count / attributedTickets.Count
                : 0m
        };

        // 按日期分组统计趋势
        var trends = tickets
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new AttributionTrend
            {
                Date = g.Key,
                TicketCount = g.Count(),
                ResponsibilityCount = g
                    .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
                    .GroupBy(t => t.RootResponsibility!)
                    .ToDictionary(rg => rg.Key, rg => rg.Count())
            })
            .OrderBy(t => t.Date)
            .ToList();

        evaluation.Trends = trends;

        return evaluation;
    }

    public async Task<AttributionStatisticsDto> GetAttributionStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate)
    {
        _logger.LogInformation("Getting attribution statistics from {FromDate} to {ToDate}", fromDate, toDate);

        var query = _dbContext.Tickets.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        var tickets = await query.ToListAsync();
        var attributedTickets = tickets.Where(t => !string.IsNullOrEmpty(t.RootResponsibility)).ToList();

        var statistics = new AttributionStatisticsDto
        {
            TotalTickets = tickets.Count,
            AttributedTickets = attributedTickets.Count,
            AttributionRate = tickets.Count > 0 ? (decimal)attributedTickets.Count / tickets.Count : 0m
        };

        // 责任分布
        statistics.ResponsibilityDistribution = attributedTickets
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
            .GroupBy(t => t.RootResponsibility!)
            .ToDictionary(g => g.Key, g => g.Count());

        // 责任百分比
        if (attributedTickets.Count > 0)
        {
            statistics.ResponsibilityPercentage = statistics.ResponsibilityDistribution
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (decimal)kvp.Value / attributedTickets.Count * 100);
        }

        // 可预防性统计
        statistics.PreventableCount = attributedTickets.Count(t => t.IsPreventable == true);
        statistics.NonPreventableCount = attributedTickets.Count(t => t.IsPreventable == false);
        statistics.PreventabilityRate = attributedTickets.Count > 0
            ? (decimal)statistics.PreventableCount / attributedTickets.Count
            : 0m;

        return statistics;
    }

    // 私有辅助方法

    private AttributionFeaturesDto ExtractAttributionFeatures(Ticket ticket)
    {
        return new AttributionFeaturesDto
        {
            Domain = ticket.Domain.ToString(),
            SymptomTitle = ticket.SymptomTitle,
            RootCause = ticket.RootCause,
            DeviceModel = null, // 可以从关联的设备获取
            Tags = new List<string>()
        };
    }

    private async Task<List<Ticket>> FindSimilarTicketsAsync(Ticket ticket, AttributionFeaturesDto features)
    {
        // 查找相似工单：相同域、相似症状、相同步骤
        var similarTickets = await _dbContext.Tickets
            .Where(t => t.TicketId != ticket.TicketId &&
                       t.Domain == ticket.Domain &&
                       t.StepCode == ticket.StepCode &&
                       !string.IsNullOrEmpty(t.RootResponsibility))
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync();

        // 如果找到的工单太少，放宽条件
        if (similarTickets.Count < 5)
        {
            similarTickets = await _dbContext.Tickets
                .Where(t => t.TicketId != ticket.TicketId &&
                           t.Domain == ticket.Domain &&
                           !string.IsNullOrEmpty(t.RootResponsibility))
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        return similarTickets;
    }

    private Dictionary<string, decimal> AnalyzeAttributionPattern(List<Ticket> similarTickets)
    {
        var pattern = new Dictionary<string, decimal>();

        if (!similarTickets.Any())
        {
            return pattern;
        }

        // 分析根因分类分布
        var responsibilityCount = similarTickets
            .Where(t => !string.IsNullOrEmpty(t.RootResponsibility))
            .GroupBy(t => t.RootResponsibility!)
            .ToDictionary(g => g.Key, g => g.Count());

        var total = similarTickets.Count;
        foreach (var kvp in responsibilityCount)
        {
            pattern[$"responsibility_{kvp.Key}"] = (decimal)kvp.Value / total;
        }

        // 分析可预防性分布
        var preventableCount = similarTickets.Count(t => t.IsPreventable == true);
        pattern["preventable_rate"] = (decimal)preventableCount / total;

        return pattern;
    }

    private AttributionSuggestionDto GenerateSuggestion(
        Ticket ticket,
        List<Ticket> similarTickets,
        Dictionary<string, decimal> attributionPattern)
    {
        var suggestion = new AttributionSuggestionDto
        {
            Confidence = 0.5m,
            Reasons = new List<string>(),
            SimilarTickets = similarTickets
                .Take(5)
                .Select(t => new SimilarTicketInfo
                {
                    TicketId = t.TicketId,
                    TicketNo = t.TicketNo,
                    RootResponsibility = t.RootResponsibility ?? "unknown",
                    IsPreventable = t.IsPreventable,
                    SimilarityScore = 0.8m // 简化实现
                })
                .ToList(),
            AttributionPattern = attributionPattern
        };

        // 根据归因模式生成建议
        if (attributionPattern.Any())
        {
            var responsibilityPattern = attributionPattern
                .Where(kvp => kvp.Key.StartsWith("responsibility_"))
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            if (responsibilityPattern.Key != null)
            {
                var responsibility = responsibilityPattern.Key.Replace("responsibility_", "");
                suggestion.RootResponsibility = responsibility;
                suggestion.Confidence = responsibilityPattern.Value;
                suggestion.Reasons.Add($"相似工单中 {responsibilityPattern.Value:P0} 的归因为 {responsibility}");
            }

            // 可预防性建议
            if (attributionPattern.ContainsKey("preventable_rate"))
            {
                var preventableRate = attributionPattern["preventable_rate"];
                suggestion.IsPreventable = preventableRate > 0.5m;
                suggestion.Reasons.Add($"相似工单中 {preventableRate:P0} 是可预防的");
            }
        }
        else
        {
            // 如果没有相似工单，使用默认值
            suggestion.RootResponsibility = "unknown";
            suggestion.Confidence = 0.3m;
            suggestion.Reasons.Add("没有找到相似的历史工单，建议人工判断");
        }

        return suggestion;
    }
}

