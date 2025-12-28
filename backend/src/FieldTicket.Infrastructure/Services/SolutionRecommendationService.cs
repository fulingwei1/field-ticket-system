using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 解决方案推荐服务实现
/// </summary>
public class SolutionRecommendationService : ISolutionRecommendationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SolutionRecommendationService> _logger;

    // 推荐算法权重配置
    private const double DOMAIN_MATCH_WEIGHT = 0.30;         // Domain完全匹配权重
    private const double SYMPTOM_SIMILARITY_WEIGHT = 0.35;   // 症状相似度权重
    private const double SUCCESS_RATE_WEIGHT = 0.20;         // 成功率权重
    private const double VERSION_COMPATIBLE_WEIGHT = 0.10;   // 版本兼容性权重
    private const double RECENCY_WEIGHT = 0.05;              // 时间新鲜度权重

    public SolutionRecommendationService(
        ApplicationDbContext dbContext,
        ILogger<SolutionRecommendationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 为工单推荐解决方案
    /// </summary>
    public async Task<RecommendSolutionsResponse> RecommendSolutionsAsync(RecommendSolutionsRequest request)
    {
        _logger.LogInformation("Recommending solutions for ticket {TicketId}, TopK={TopK}",
            request.TicketId, request.TopK);

        // 获取目标工单
        var targetTicket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketId == request.TicketId);

        if (targetTicket == null)
        {
            _logger.LogWarning("Ticket not found: {TicketId}", request.TicketId);
            return new RecommendSolutionsResponse
            {
                TicketId = request.TicketId,
                Message = "工单不存在"
            };
        }

        // 获取所有已发布的解决方案（排除当前工单自己的方案）
        var candidateSolutions = await _dbContext.Solutions
            .AsNoTracking()
            .Include(s => s.Ticket)
            .Where(s => s.Status == "Published")
            .Where(s => s.TicketId != request.TicketId)
            .Where(s => !request.IncludeExpired || !s.IsExpired)
            .ToListAsync();

        if (!candidateSolutions.Any())
        {
            _logger.LogInformation("No candidate solutions found for recommendation");
            return new RecommendSolutionsResponse
            {
                TicketId = request.TicketId,
                TicketNo = targetTicket.TicketNo,
                Message = "暂无可推荐的历史解决方案"
            };
        }

        _logger.LogInformation("Found {Count} candidate solutions", candidateSolutions.Count);

        // 计算每个候选方案的推荐分数
        var recommendations = new List<SolutionRecommendationDto>();

        foreach (var solution in candidateSolutions)
        {
            var recommendation = await CalculateRecommendationScoreAsync(
                targetTicket,
                solution,
                solution.Ticket!);

            // 过滤低分方案
            if (recommendation.MatchScore >= request.MinSimilarityScore)
            {
                recommendations.Add(recommendation);
            }
        }

        // 按分数降序排序，取Top-K
        var topRecommendations = recommendations
            .OrderByDescending(r => r.MatchScore)
            .Take(request.TopK)
            .ToList();

        _logger.LogInformation("Generated {Count} recommendations (from {Total} candidates)",
            topRecommendations.Count, candidateSolutions.Count);

        return new RecommendSolutionsResponse
        {
            TicketId = request.TicketId,
            TicketNo = targetTicket.TicketNo,
            Recommendations = topRecommendations,
            TotalCandidates = candidateSolutions.Count,
            Message = topRecommendations.Any()
                ? $"找到 {topRecommendations.Count} 个推荐方案"
                : "未找到匹配度足够高的方案"
        };
    }

    /// <summary>
    /// 计算推荐分数
    /// </summary>
    private async Task<SolutionRecommendationDto> CalculateRecommendationScoreAsync(
        Ticket targetTicket,
        Solution solution,
        Ticket sourceTicket)
    {
        var matchReasons = new List<SolutionMatchReason>();
        var scoreBreakdown = new Dictionary<string, double>();

        // 1. Domain匹配度
        double domainScore = targetTicket.Domain == sourceTicket.Domain ? 1.0 : 0.0;
        scoreBreakdown["domain_match"] = domainScore * DOMAIN_MATCH_WEIGHT;
        if (domainScore > 0)
        {
            matchReasons.Add(new SolutionMatchReason
            {
                ReasonType = "domain_match",
                Description = $"问题域匹配 ({targetTicket.Domain})",
                Weight = DOMAIN_MATCH_WEIGHT
            });
        }

        // 2. 症状相似度（基于文本相似度）
        double symptomScore = CalculateSymptomSimilarity(targetTicket, sourceTicket);
        scoreBreakdown["symptom_similarity"] = symptomScore * SYMPTOM_SIMILARITY_WEIGHT;
        if (symptomScore > 0.5)
        {
            matchReasons.Add(new SolutionMatchReason
            {
                ReasonType = "symptom_similarity",
                Description = $"症状相似度 {symptomScore:P0}",
                Weight = SYMPTOM_SIMILARITY_WEIGHT
            });
        }

        // 3. 获取解决方案统计信息
        var statistics = await GetSolutionStatisticsAsync(solution.SolutionId);
        scoreBreakdown["success_rate"] = statistics.SuccessRate * SUCCESS_RATE_WEIGHT;
        if (statistics.SuccessRate > 0.7)
        {
            matchReasons.Add(new SolutionMatchReason
            {
                ReasonType = "high_success_rate",
                Description = $"历史成功率 {statistics.SuccessRate:P0} ({statistics.SuccessCount}/{statistics.VerificationCount})",
                Weight = SUCCESS_RATE_WEIGHT
            });
        }

        // 4. 版本兼容性
        var (isCompatible, compatibilityMessage) = CheckVersionCompatibilityDirect(solution, targetTicket);
        double versionScore = isCompatible ? 1.0 : 0.5; // 不兼容给0.5分，表示可以参考但需要调整
        scoreBreakdown["version_compatible"] = versionScore * VERSION_COMPATIBLE_WEIGHT;
        if (isCompatible)
        {
            matchReasons.Add(new SolutionMatchReason
            {
                ReasonType = "version_compatible",
                Description = "版本完全兼容",
                Weight = VERSION_COMPATIBLE_WEIGHT
            });
        }

        // 5. 时间新鲜度（最近使用的方案得分更高）
        var daysSinceLastUse = statistics.DaysSinceLastUse;
        double recencyScore = daysSinceLastUse <= 30 ? 1.0 :
                              daysSinceLastUse <= 90 ? 0.7 :
                              daysSinceLastUse <= 180 ? 0.4 : 0.2;
        scoreBreakdown["recency"] = recencyScore * RECENCY_WEIGHT;

        // 计算总分
        double totalScore = scoreBreakdown.Values.Sum();

        return new SolutionRecommendationDto
        {
            SolutionId = solution.SolutionId,
            SolutionCode = solution.SolutionCode,
            SourceTicketId = sourceTicket.TicketId,
            SourceTicketNo = sourceTicket.TicketNo,
            Title = solution.Title,
            Description = solution.Description,
            SolutionType = solution.SolutionType,
            ReleaseType = solution.ReleaseType,
            RequiredSwVersion = solution.RequiredSwVersion,
            RequiredPlcVersion = solution.RequiredPlcVersion,
            NewSwVersion = solution.NewSwVersion,
            NewPlcVersion = solution.NewPlcVersion,
            MatchScore = totalScore,
            MatchReasons = matchReasons,
            ScoreBreakdown = scoreBreakdown,
            Statistics = statistics,
            IsVersionCompatible = isCompatible,
            VersionCompatibilityMessage = compatibilityMessage,
            CreatedAt = solution.CreatedAt,
            PublishedAt = solution.PublishedAt,
            LastVerifiedAt = solution.LastVerifiedAt
        };
    }

    /// <summary>
    /// 计算症状相似度（基于文本相似度）
    /// </summary>
    private double CalculateSymptomSimilarity(Ticket ticket1, Ticket ticket2)
    {
        // 组合症状字符串
        string symptom1 = $"{ticket1.SymptomTitle ?? ""} {ticket1.SymptomDetail ?? ""}".ToLower();
        string symptom2 = $"{ticket2.SymptomTitle ?? ""} {ticket2.SymptomDetail ?? ""}".ToLower();

        if (string.IsNullOrWhiteSpace(symptom1) || string.IsNullOrWhiteSpace(symptom2))
        {
            return 0.0;
        }

        // 使用Jaccard相似度（基于词汇集合）
        var words1 = symptom1.Split(new[] { ' ', ',', '.', '，', '。', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        var words2 = symptom2.Split(new[] { ' ', ',', '.', '，', '。', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        if (words1.Count == 0 || words2.Count == 0)
        {
            return 0.0;
        }

        // Jaccard相似度 = 交集大小 / 并集大小
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        double jaccardSimilarity = (double)intersection / union;

        // 额外加分：StepCode相同
        if (!string.IsNullOrEmpty(ticket1.StepCode) &&
            ticket1.StepCode == ticket2.StepCode)
        {
            jaccardSimilarity = Math.Min(1.0, jaccardSimilarity + 0.2);
        }

        // 额外加分：AlarmCode相同
        if (!string.IsNullOrEmpty(ticket1.AlarmCode) &&
            ticket1.AlarmCode == ticket2.AlarmCode)
        {
            jaccardSimilarity = Math.Min(1.0, jaccardSimilarity + 0.3);
        }

        return jaccardSimilarity;
    }

    /// <summary>
    /// 计算两个工单的相似度
    /// </summary>
    public async Task<double> CalculateTicketSimilarityAsync(Guid ticketId1, Guid ticketId2)
    {
        var ticket1 = await _dbContext.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketId == ticketId1);
        var ticket2 = await _dbContext.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketId == ticketId2);

        if (ticket1 == null || ticket2 == null)
        {
            return 0.0;
        }

        return CalculateSymptomSimilarity(ticket1, ticket2);
    }

    /// <summary>
    /// 获取解决方案统计信息
    /// </summary>
    public async Task<SolutionStatistics> GetSolutionStatisticsAsync(Guid solutionId)
    {
        var solution = await _dbContext.Solutions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SolutionId == solutionId);

        if (solution == null)
        {
            return new SolutionStatistics();
        }

        // 查询该方案的所有验证记录
        var verifications = await _dbContext.Verifications
            .AsNoTracking()
            .Where(v => v.SolutionId == solutionId)
            .ToListAsync();

        int successCount = verifications.Count(v => v.FinalStatus == "PASS");
        int failureCount = verifications.Count(v => v.FinalStatus == "FAIL");
        int totalCount = successCount + failureCount;

        double successRate = totalCount > 0 ? (double)successCount / totalCount : 0.0;

        // 计算距离上次使用的天数
        int daysSinceLastUse = solution.LastVerifiedAt.HasValue
            ? (DateTime.UtcNow - solution.LastVerifiedAt.Value).Days
            : int.MaxValue;

        return new SolutionStatistics
        {
            TotalUsageCount = solution.VerificationCount,
            VerificationCount = totalCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            SuccessRate = successRate,
            DaysSinceLastUse = daysSinceLastUse
        };
    }

    /// <summary>
    /// 检查版本兼容性
    /// </summary>
    public async Task<(bool IsCompatible, string? Message)> CheckVersionCompatibilityAsync(
        Guid solutionId,
        Guid ticketId)
    {
        var solution = await _dbContext.Solutions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SolutionId == solutionId);
        var ticket = await _dbContext.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (solution == null || ticket == null)
        {
            return (false, "方案或工单不存在");
        }

        return CheckVersionCompatibilityDirect(solution, ticket);
    }

    /// <summary>
    /// 直接检查版本兼容性（不查询数据库）
    /// </summary>
    private (bool IsCompatible, string? Message) CheckVersionCompatibilityDirect(
        Solution solution,
        Ticket ticket)
    {
        var issues = new List<string>();

        // 检查软件版本
        if (!string.IsNullOrEmpty(solution.RequiredSwVersion) &&
            !string.IsNullOrEmpty(ticket.SwVersion) &&
            solution.RequiredSwVersion != ticket.SwVersion)
        {
            issues.Add($"软件版本不匹配：需要 {solution.RequiredSwVersion}，当前 {ticket.SwVersion}");
        }

        // 检查PLC版本
        if (!string.IsNullOrEmpty(solution.RequiredPlcVersion) &&
            !string.IsNullOrEmpty(ticket.PlcVersion) &&
            solution.RequiredPlcVersion != ticket.PlcVersion)
        {
            issues.Add($"PLC版本不匹配：需要 {solution.RequiredPlcVersion}，当前 {ticket.PlcVersion}");
        }

        // 检查参数版本
        if (!string.IsNullOrEmpty(solution.RequiredParamVersion) &&
            !string.IsNullOrEmpty(ticket.ParamVersion) &&
            solution.RequiredParamVersion != ticket.ParamVersion)
        {
            issues.Add($"参数版本不匹配：需要 {solution.RequiredParamVersion}，当前 {ticket.ParamVersion}");
        }

        // 检查适用版本列表
        if (solution.ApplicableSwVersions?.Any() == true &&
            !string.IsNullOrEmpty(ticket.SwVersion) &&
            !solution.ApplicableSwVersions.Contains(ticket.SwVersion))
        {
            issues.Add($"软件版本 {ticket.SwVersion} 不在适用列表中");
        }

        if (solution.ApplicableHwVersions?.Any() == true &&
            !string.IsNullOrEmpty(ticket.HwVersion) &&
            !solution.ApplicableHwVersions.Contains(ticket.HwVersion))
        {
            issues.Add($"硬件版本 {ticket.HwVersion} 不在适用列表中");
        }

        if (issues.Any())
        {
            return (false, string.Join("; ", issues));
        }

        return (true, "版本完全兼容");
    }
}
