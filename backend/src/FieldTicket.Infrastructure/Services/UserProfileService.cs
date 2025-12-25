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
        var expertiseScore = CalculateExpertiseScore(tickets, fillingHistory);
        var expertiseLevel = DetermineExpertiseLevel(expertiseScore);

        // 计算平均完成时间
        var avgCompletionTime = fillingHistory
            .Where(h => h.FillingTime.HasValue)
            .Select(h => h.FillingTime.Value)
            .DefaultIfEmpty(0)
            .Average();

        // 分析常见错误
        var commonMistakes = AnalyzeCommonMistakes(tickets, fillingHistory);

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

    private decimal CalculateExpertiseScore(
        List<Ticket> tickets,
        List<UserFillingHistory> fillingHistory)
    {
        var score = 0.0m;

        // 1. 工单数量（权重30%）
        var ticketCountScore = Math.Min(tickets.Count / 100.0m, 1.0m);
        score += ticketCountScore * 0.3m;

        // 2. 平均完成时间（权重20%）
        if (fillingHistory.Any(h => h.FillingTime.HasValue))
        {
            var avgTime = fillingHistory
                .Where(h => h.FillingTime.HasValue)
                .Average(h => h.FillingTime!.Value);
            var timeScore = avgTime < 300 ? 1.0m : Math.Max(0.0m, 1.0m - ((decimal)avgTime - 300) / 600.0m);
            score += timeScore * 0.2m;
        }

        // 3. 错误率（权重30%）
        // TODO: 需要从验证结果中计算错误率
        var errorRate = 0.1m; // 默认值
        score += (1.0m - errorRate) * 0.3m;

        // 4. 知识掌握度（权重20%）
        // TODO: 需要从判断卡使用情况计算
        var knowledgeScore = 0.7m; // 默认值
        score += knowledgeScore * 0.2m;

        return Math.Min(1.0m, score);
    }

    private string DetermineExpertiseLevel(decimal score)
    {
        if (score >= 0.8m) return "expert";
        if (score >= 0.5m) return "intermediate";
        return "beginner";
    }

    private Dictionary<string, object>? AnalyzeCommonMistakes(
        List<Ticket> tickets,
        List<UserFillingHistory> fillingHistory)
    {
        // TODO: 实现常见错误分析
        return null;
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

