using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 用户画像服务实现
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        ApplicationDbContext dbContext,
        ILogger<UserProfileService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserProfileDto> BuildUserProfileAsync(Guid userId)
    {
        // 获取用户历史工单
        var tickets = await _dbContext.Tickets
            .Where(t => t.CreatedByUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();

        // 获取用户填写历史
        var fillingHistory = await _dbContext.UserFillingHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(50)
            .ToListAsync();

        // 分析常用字段
        var commonFields = AnalyzeCommonFields(tickets, fillingHistory);

        // 计算专业度
        var expertiseScore = await CalculateExpertiseScoreAsync(userId, tickets, fillingHistory);
        var expertiseLevel = expertiseScore.HasValue ? DetermineExpertiseLevel(expertiseScore.Value) : null;

        // 计算平均完成时间
        var avgCompletionTime = fillingHistory
            .Where(h => h.FillingTime.HasValue)
            .Select(h => h.FillingTime.Value)
            .DefaultIfEmpty(0)
            .Average();

        // 分析常见错误
        var commonMistakes = await AnalyzeCommonMistakesAsync(userId, tickets, fillingHistory);

        // 获取或创建用户画像
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            profile = new UserProfile
            {
                ProfileId = Guid.NewGuid(),
                UserId = userId,
                CommonFields = JsonDocument.Parse(JsonSerializer.Serialize(commonFields)),
                ExpertiseLevel = expertiseLevel,
                ExpertiseScore = expertiseScore,
                TotalTickets = tickets.Count,
                AverageCompletionTime = avgCompletionTime > 0 ? (int)avgCompletionTime : null,
                CommonMistakes = commonMistakes != null ? JsonDocument.Parse(JsonSerializer.Serialize(commonMistakes)) : null,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.UserProfiles.Add(profile);
        }
        else
        {
            profile.CommonFields = JsonDocument.Parse(JsonSerializer.Serialize(commonFields));
            profile.ExpertiseLevel = expertiseLevel;
            profile.ExpertiseScore = expertiseScore;
            profile.TotalTickets = tickets.Count;
            profile.AverageCompletionTime = avgCompletionTime > 0 ? (int)avgCompletionTime : null;
            profile.CommonMistakes = commonMistakes != null ? JsonDocument.Parse(JsonSerializer.Serialize(commonMistakes)) : null;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return MapToDto(profile);
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        var profile = await _dbContext.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return profile != null ? MapToDto(profile) : null;
    }

    public async Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
    {
        // 记录填写历史
        var history = new UserFillingHistory
        {
            HistoryId = Guid.NewGuid(),
            UserId = userId,
            TicketId = request.TicketId,
            FilledFields = request.FilledFields ?? JsonDocument.Parse("{}"),
            FillingTime = request.FillingTime,
            SkippedFields = request.SkippedFields,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserFillingHistories.Add(history);

        // 异步更新用户画像（不阻塞）
        _ = Task.Run(async () =>
        {
            try
            {
                await BuildUserProfileAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user profile for user {UserId}", userId);
            }
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task<PreFillData> GetPreFillDataAsync(Guid userId, Guid? deviceId)
    {
        var profile = await GetUserProfileAsync(userId);
        var preFillData = new PreFillData();

        if (profile != null)
        {
            // 从用户画像中提取常用字段
            var commonFields = profile.CommonFields;
            if (commonFields.RootElement.TryGetProperty("frequently_used", out var frequentlyUsed))
            {
                var fields = new Dictionary<string, object>();
                foreach (var prop in frequentlyUsed.EnumerateObject())
                {
                    fields[prop.Name] = prop.Value.GetRawText();
                }
                preFillData.Fields = fields;
            }

            // 基于设备信息预填充
            if (deviceId.HasValue)
            {
                var device = await _dbContext.Tickets
                    .Where(t => t.DeviceId == deviceId.Value && t.CreatedByUserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (device != null)
                {
                    preFillData.Fields["sw_version"] = device.SwVersion;
                    preFillData.Fields["plc_version"] = device.PlcVersion;
                    preFillData.Fields["param_version"] = device.ParamVersion;
                }
            }

            preFillData.Confidence = profile.ExpertiseScore ?? 0.5m;
        }

        return preFillData;
    }

    public async Task<List<PersonalizedQuestion>> RecommendPersonalizedQuestionsAsync(
        Guid userId,
        Guid ticketId)
    {
        var profile = await GetUserProfileAsync(userId);
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        var questions = new List<PersonalizedQuestion>();

        if (profile == null || ticket == null)
        {
            return questions;
        }

        // 基于用户历史推荐问题
        var commonFields = profile.CommonFields;
        if (commonFields.RootElement.TryGetProperty("frequently_used", out var frequentlyUsed))
        {
            if (frequentlyUsed.TryGetProperty("common_symptoms", out var symptoms))
            {
                foreach (var symptom in symptoms.EnumerateArray())
                {
                    questions.Add(new PersonalizedQuestion
                    {
                        QuestionId = Guid.NewGuid().ToString(),
                        Question = $"是否出现{symptom.GetString()}？",
                        Type = "yes_no",
                        RelevanceScore = 0.8m,
                        Reason = "基于您的历史填写习惯"
                    });
                }
            }
        }

        // 基于专业度推荐问题
        if (profile.ExpertiseLevel == "beginner")
        {
            questions.Add(new PersonalizedQuestion
            {
                QuestionId = Guid.NewGuid().ToString(),
                Question = "请详细描述问题的复现步骤",
                Type = "text",
                RelevanceScore = 0.9m,
                Reason = "帮助您更好地描述问题"
            });
        }

        return questions.OrderByDescending(q => q.RelevanceScore).Take(5).ToList();
    }

    private Dictionary<string, object> AnalyzeCommonFields(
        List<Ticket> tickets,
        List<UserFillingHistory> fillingHistory)
    {
        var commonFields = new Dictionary<string, object>();
        var frequentlyUsed = new Dictionary<string, object>();

        if (tickets.Any())
        {
            // 统计常用设备型号
            var deviceModels = tickets
                .GroupBy(t => t.DeviceId)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.First().DeviceId.ToString())
                .ToList();

            // 统计常用问题域
            var domains = tickets
                .GroupBy(t => t.Domain)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key.ToString())
                .FirstOrDefault();

            // 统计常见症状
            var symptoms = tickets
                .Where(t => !string.IsNullOrEmpty(t.SymptomTitle))
                .GroupBy(t => t.SymptomTitle)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            frequentlyUsed["device_model"] = deviceModels.FirstOrDefault() ?? "";
            frequentlyUsed["problem_domain"] = domains ?? "";
            frequentlyUsed["common_symptoms"] = symptoms;
        }

        commonFields["frequently_used"] = frequentlyUsed;
        commonFields["preferences"] = new Dictionary<string, object>
        {
            ["question_style"] = "detailed",
            ["detail_level"] = "high"
        };

        return commonFields;
    }

    private async Task<decimal?> CalculateExpertiseScoreAsync(
        Guid userId,
        List<Ticket> tickets,
        List<UserFillingHistory> fillingHistory)
    {
        var score = 0.0m;
        var hasData = false;
        var totalWeight = 0.0m;

        // 1. 工单数量（权重30%）
        var ticketCountScore = Math.Min(tickets.Count / 100.0m, 1.0m);
        score += ticketCountScore * 0.3m;
        totalWeight += 0.3m;
        hasData = true; // 至少有一个工单就算有数据

        // 2. 平均完成时间（权重20%）
        if (fillingHistory.Any(h => h.FillingTime.HasValue))
        {
            var avgTime = fillingHistory
                .Where(h => h.FillingTime.HasValue)
                .Average(h => h.FillingTime!.Value);
            var timeScore = avgTime < 300 ? 1.0m : Math.Max(0.0m, 1.0m - ((decimal)avgTime - 300) / 600.0m);
            score += timeScore * 0.2m;
            totalWeight += 0.2m;
        }

        // 3. 错误率（权重30%）
        var errorRate = await CalculateErrorRateAsync(userId, tickets);
        if (errorRate.HasValue)
        {
            score += (1.0m - errorRate.Value) * 0.3m;
            totalWeight += 0.3m;
        }

        // 4. 知识掌握度（权重20%）
        var knowledgeScore = await CalculateKnowledgeScoreAsync(userId);
        if (knowledgeScore.HasValue)
        {
            score += knowledgeScore.Value * 0.2m;
            totalWeight += 0.2m;
        }

        // 如果没有任何有效数据，返回 null
        if (!hasData && totalWeight == 0)
        {
            return null;
        }

        // 如果数据不足（权重总和小于0.5），返回 null 表示数据不足
        if (totalWeight < 0.5m)
        {
            return null;
        }

        // 按实际权重归一化分数
        var normalizedScore = totalWeight > 0 ? score / totalWeight : 0m;
        return Math.Min(1.0m, normalizedScore);
    }

    /// <summary>
    /// 计算用户错误率（基于验证结果）
    /// 如果数据不足，返回 null 表示无法计算
    /// </summary>
    private async Task<decimal?> CalculateErrorRateAsync(Guid userId, List<Ticket> tickets)
    {
        if (!tickets.Any())
        {
            return null; // 没有工单数据，无法计算错误率
        }

        var ticketIds = tickets.Select(t => t.TicketId).ToList();
        var verifications = await _dbContext.Verifications
            .Where(v => ticketIds.Contains(v.TicketId) && v.ExecutedBy == userId)
            .ToListAsync();

        if (!verifications.Any())
        {
            return null; // 没有验证记录，无法计算错误率
        }

        // 计算失败率：FAIL 和 PARTIAL 都算作错误
        var totalRuns = verifications.Sum(v => v.RunCount);
        var totalFails = verifications.Sum(v => v.FailCount);
        var partialCount = verifications.Count(v => v.Result == "PARTIAL");

        // 错误率 = (失败次数 + 部分通过次数 * 0.5) / 总验证次数
        if (totalRuns == 0)
        {
            return null; // 没有验证运行记录，无法计算错误率
        }

        var errorRate = (decimal)(totalFails + partialCount * 0.5) / totalRuns;
        return Math.Min(1.0m, Math.Max(0.0m, errorRate));
    }

    /// <summary>
    /// 计算知识掌握度（基于判断卡使用情况）
    /// 如果数据不足，返回 null 表示无法计算
    /// </summary>
    private async Task<decimal?> CalculateKnowledgeScoreAsync(Guid userId)
    {
        var usageHistories = await _dbContext.JudgementCardUsageHistories
            .Where(h => h.UsedBy == userId && h.IsValid)
            .OrderByDescending(h => h.UsedAt)
            .Take(50) // 最近50次使用
            .ToListAsync();

        if (!usageHistories.Any())
        {
            return null; // 没有使用记录，无法计算知识掌握度
        }

        // 计算正确率：correct 结果占比
        var correctCount = usageHistories.Count(h => h.Result == "correct");
        var totalCount = usageHistories.Count(h => !string.IsNullOrEmpty(h.Result));

        if (totalCount == 0)
        {
            return null; // 没有结果记录，无法计算知识掌握度
        }

        var knowledgeScore = (decimal)correctCount / totalCount;
        return Math.Min(1.0m, Math.Max(0.0m, knowledgeScore));
    }

    private string DetermineExpertiseLevel(decimal score)
    {
        if (score >= 0.8m) return "expert";
        if (score >= 0.5m) return "intermediate";
        return "beginner";
    }

    private async Task<Dictionary<string, object>?> AnalyzeCommonMistakesAsync(
        Guid userId,
        List<Ticket> tickets,
        List<UserFillingHistory> fillingHistory)
    {
        var mistakes = new Dictionary<string, object>();
        var mistakeTypes = new List<string>();

        // 1. 分析验证失败的原因
        var ticketIds = tickets.Select(t => t.TicketId).ToList();
        var failedVerifications = await _dbContext.Verifications
            .Where(v => ticketIds.Contains(v.TicketId) && 
                       v.ExecutedBy == userId && 
                       (v.Result == "FAIL" || v.Result == "PARTIAL"))
            .ToListAsync();

        if (failedVerifications.Any())
        {
            var failRate = (decimal)failedVerifications.Count / tickets.Count;
            if (failRate > 0.2m) // 失败率超过20%
            {
                mistakeTypes.Add("验证失败率较高");
            }
        }

        // 2. 分析工单填写完整性
        var incompleteTickets = tickets.Count(t => 
            string.IsNullOrEmpty(t.SymptomTitle) || 
            string.IsNullOrEmpty(t.SymptomDetail));
        
        if (incompleteTickets > tickets.Count * 0.3m) // 超过30%的工单不完整
        {
            mistakeTypes.Add("工单信息填写不完整");
        }

        // 3. 分析填写时间异常
        var slowFilling = fillingHistory
            .Where(h => h.FillingTime.HasValue && h.FillingTime.Value > 600) // 超过10分钟
            .Count();
        
        if (slowFilling > fillingHistory.Count * 0.3m) // 超过30%的填写时间过长
        {
            mistakeTypes.Add("填写时间过长，可能存在理解困难");
        }

        // 4. 分析跳过的字段
        var skippedFields = new Dictionary<string, int>();
        foreach (var history in fillingHistory.Where(h => h.SkippedFields != null))
        {
            if (history.SkippedFields != null)
            {
                var root = history.SkippedFields.RootElement;
                foreach (var prop in root.EnumerateObject())
                {
                    if (!skippedFields.ContainsKey(prop.Name))
                    {
                        skippedFields[prop.Name] = 0;
                    }
                    skippedFields[prop.Name]++;
                }
            }
        }

        if (skippedFields.Any())
        {
            var mostSkipped = skippedFields.OrderByDescending(kv => kv.Value).First();
            if (mostSkipped.Value > fillingHistory.Count * 0.5m) // 超过50%的情况跳过
            {
                mistakeTypes.Add($"经常跳过字段：{mostSkipped.Key}");
            }
        }

        if (mistakeTypes.Any())
        {
            mistakes["mistake_types"] = mistakeTypes;
            mistakes["total_mistakes"] = mistakeTypes.Count;
            mistakes["improvement_suggestions"] = GenerateImprovementSuggestions(mistakeTypes);
        }

        return mistakes.Any() ? mistakes : null;
    }

    /// <summary>
    /// 生成改进建议
    /// </summary>
    private List<string> GenerateImprovementSuggestions(List<string> mistakeTypes)
    {
        var suggestions = new List<string>();

        if (mistakeTypes.Contains("验证失败率较高"))
        {
            suggestions.Add("建议在验证前仔细阅读解决方案，确保理解操作步骤");
        }

        if (mistakeTypes.Contains("工单信息填写不完整"))
        {
            suggestions.Add("建议填写完整的症状描述和问题详情，有助于快速定位问题");
        }

        if (mistakeTypes.Contains("填写时间过长，可能存在理解困难"))
        {
            suggestions.Add("建议参考历史工单模板，或使用智能预填充功能提高填写效率");
        }

        if (mistakeTypes.Any(m => m.Contains("经常跳过字段")))
        {
            suggestions.Add("建议填写所有必填字段，完整的信息有助于问题快速解决");
        }

        return suggestions;
    }

    private UserProfileDto MapToDto(UserProfile profile)
    {
        return new UserProfileDto
        {
            ProfileId = profile.ProfileId,
            UserId = profile.UserId,
            UserName = profile.User?.Name,
            CommonFields = profile.CommonFields,
            ExpertiseLevel = profile.ExpertiseLevel,
            ExpertiseScore = profile.ExpertiseScore,
            TotalTickets = profile.TotalTickets,
            AverageCompletionTime = profile.AverageCompletionTime,
            CommonMistakes = profile.CommonMistakes,
            UpdatedAt = profile.UpdatedAt
        };
    }
}

