using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 判断卡推荐服务实现
/// </summary>
public class JudgementCardRecommendationService : IJudgementCardRecommendationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<JudgementCardRecommendationService> _logger;

    public JudgementCardRecommendationService(
        ApplicationDbContext dbContext,
        ILogger<JudgementCardRecommendationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<RecommendJudgementCardsResponse> RecommendJudgementCardsAsync(
        RecommendJudgementCardsRequest request)
    {
        _logger.LogInformation("Recommending judgement cards for domain: {Domain}, step: {StepCode}",
            request.Domain, request.StepCode);

        // 获取所有活跃的判断卡
        var query = _dbContext.JudgementCards
            .Where(jc => jc.Status == "Active" && jc.IsCurrent)
            .AsQueryable();

        // 如果指定了问题域，过滤
        if (request.Domain.HasValue)
        {
            query = query.Where(jc => jc.Domain == request.Domain.Value);
        }

        var allCards = await query.ToListAsync();
        var recommendations = new List<JudgementCardRecommendationDto>();

        foreach (var card in allCards)
        {
            var recommendation = CalculateMatchScore(card, request);
            if (recommendation.MatchScore > 0)
            {
                recommendations.Add(recommendation);
            }
        }

        // 按得分排序，取Top K
        var topRecommendations = recommendations
            .OrderByDescending(r => r.MatchScore)
            .Take(request.TopK)
            .ToList();

        _logger.LogInformation("Recommended {Count} judgement cards out of {Total} candidates",
            topRecommendations.Count, allCards.Count);

        return new RecommendJudgementCardsResponse
        {
            Recommendations = topRecommendations,
            TotalCandidates = allCards.Count
        };
    }

    public async Task<RecommendJudgementCardsResponse> RecommendJudgementCardsByTicketAsync(
        Guid ticketId,
        int topK = 5)
    {
        // 获取工单信息
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 构建推荐请求
        var request = new RecommendJudgementCardsRequest
        {
            Domain = ticket.Domain,
            StepCode = ticket.StepCode,
            SymptomTitle = ticket.SymptomTitle,
            TopK = topK
        };

        // 如果有事实表，也包含进去
        if (ticket.FactsJson != null)
        {
            try
            {
                var facts = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    ticket.FactsJson.RootElement.GetRawText());
                if (facts != null)
                {
                    request.FactsJson = facts;
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }

        return await RecommendJudgementCardsAsync(request);
    }

    private JudgementCardRecommendationDto CalculateMatchScore(
        JudgementCard card,
        RecommendJudgementCardsRequest request)
    {
        var recommendation = new JudgementCardRecommendationDto
        {
            JudgementCard = MapToDto(card),
            MatchScore = 0m,
            MatchReasons = new List<string>(),
            ScoreBreakdown = new Dictionary<string, decimal>()
        };

        // 1. 问题域匹配：+50分
        if (request.Domain.HasValue && card.Domain == request.Domain.Value)
        {
            recommendation.ScoreBreakdown["domain_match"] = 50m;
            recommendation.MatchScore += 50m;
            recommendation.MatchReasons.Add($"问题域匹配（{card.Domain}）");
        }

        // 2. 步骤匹配：+30分（完全匹配）或 +15分（部分匹配）
        if (!string.IsNullOrEmpty(request.StepCode))
        {
            var symptomStructure = card.SymptomStructure.RootElement;
            if (symptomStructure.TryGetProperty("applicable_steps", out var stepsElement))
            {
                var steps = stepsElement.EnumerateArray().Select(e => e.GetString()).ToList();
                if (steps.Contains(request.StepCode))
                {
                    recommendation.ScoreBreakdown["step_match"] = 30m;
                    recommendation.MatchScore += 30m;
                    recommendation.MatchReasons.Add($"步骤完全匹配（{request.StepCode}）");
                }
                else if (steps.Any(s => s != null && request.StepCode.Contains(s) || s != null && s.Contains(request.StepCode)))
                {
                    recommendation.ScoreBreakdown["step_partial_match"] = 15m;
                    recommendation.MatchScore += 15m;
                    recommendation.MatchReasons.Add($"步骤部分匹配（{request.StepCode}）");
                }
            }
        }

        // 3. 关键词匹配：+10分/词（最高20分）
        if (!string.IsNullOrEmpty(request.SymptomTitle))
        {
            var symptomTitleLower = request.SymptomTitle.ToLower();
            var keywords = ExtractKeywords(card);
            var matchedKeywords = keywords.Where(k => symptomTitleLower.Contains(k.ToLower())).ToList();

            if (matchedKeywords.Any())
            {
                var keywordScore = Math.Min(matchedKeywords.Count * 10m, 20m);
                recommendation.ScoreBreakdown["keyword_match"] = keywordScore;
                recommendation.MatchScore += keywordScore;
                recommendation.MatchReasons.Add($"关键词匹配：{string.Join("、", matchedKeywords.Take(3))}");
            }
        }

        // 4. 事实特征匹配：+5分/项（最高10分）
        if (request.FactsJson != null && request.FactsJson.Any())
        {
            var symptomStructure = card.SymptomStructure.RootElement;
            if (symptomStructure.TryGetProperty("key_checks", out var keyChecksElement))
            {
                var keyChecks = keyChecksElement.EnumerateArray().ToList();
                var matchedChecks = 0;

                foreach (var check in keyChecks)
                {
                    if (check.TryGetProperty("field", out var fieldElement))
                    {
                        var field = fieldElement.GetString();
                        if (!string.IsNullOrEmpty(field) && request.FactsJson.ContainsKey(field))
                        {
                            matchedChecks++;
                        }
                    }
                }

                if (matchedChecks > 0)
                {
                    var factScore = Math.Min(matchedChecks * 5m, 10m);
                    recommendation.ScoreBreakdown["fact_match"] = factScore;
                    recommendation.MatchScore += factScore;
                    recommendation.MatchReasons.Add($"事实特征匹配（{matchedChecks}项）");
                }
            }
        }

        // 5. 使用统计加分：+5分（如果使用次数>10）
        if (card.UsageCount > 10)
        {
            recommendation.ScoreBreakdown["usage_bonus"] = 5m;
            recommendation.MatchScore += 5m;
            recommendation.MatchReasons.Add($"常用判断卡（使用{card.UsageCount}次）");
        }

        return recommendation;
    }

    private List<string> ExtractKeywords(JudgementCard card)
    {
        var keywords = new List<string>();

        // 从标题提取关键词
        if (!string.IsNullOrEmpty(card.Title))
        {
            keywords.AddRange(card.Title.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // 从症状结构中提取关键词
        try
        {
            var symptomStructure = card.SymptomStructure.RootElement;
            if (symptomStructure.TryGetProperty("keywords", out var keywordsElement))
            {
                keywords.AddRange(keywordsElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty));
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return keywords.Distinct().Where(k => k.Length > 1).ToList();
    }

    private JudgementCardDto MapToDto(JudgementCard card)
    {
        return new JudgementCardDto
        {
            JudgementCardId = card.JudgementCardId,
            JcCode = card.JcCode,
            Title = card.Title,
            Description = card.Description,
            Domain = card.Domain,
            SymptomStructure = card.SymptomStructure,
            TroubleshootingPath = card.TroubleshootingPath,
            HypothesisTemplate = card.HypothesisTemplate,
            NextActionTemplate = card.NextActionTemplate,
            Status = card.Status,
            Version = card.Version,
            UsageCount = card.UsageCount,
            LastUsedAt = card.LastUsedAt,
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt
        };
    }
}

