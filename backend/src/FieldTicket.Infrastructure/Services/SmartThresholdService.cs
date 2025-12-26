using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 智能阈值服务实现
/// </summary>
public class SmartThresholdService : ISmartThresholdService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SmartThresholdService> _logger;

    public SmartThresholdService(
        ApplicationDbContext dbContext,
        ILogger<SmartThresholdService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ThresholdConfigDto> LearnOptimalThresholdAsync(LearnOptimalThresholdRequest request)
    {
        // 获取历史工单数据
        var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-6);
        var toDate = request.ToDate ?? DateTime.UtcNow;

        var tickets = await _dbContext.Tickets
            .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate && t.Status == "Closed")
            .ToListAsync();

        // 根据场景类型过滤
        if (request.ScenarioType == "device_type" && !string.IsNullOrEmpty(request.ScenarioValue))
        {
            // 通过 Project 获取设备类型
            var projectIds = await _dbContext.Projects
                .Where(p => p.DeviceType == request.ScenarioValue)
                .Select(p => p.ProjectId)
                .ToListAsync();
            
            tickets = tickets.Where(t => projectIds.Contains(t.ProjectId)).ToList();
        }
        else if (request.ScenarioType == "problem_type" && !string.IsNullOrEmpty(request.ScenarioValue))
        {
            tickets = tickets.Where(t => t.Domain.ToString() == request.ScenarioValue).ToList();
        }

        // 分析历史触发情况
        var historicalTriggers = AnalyzeHistoricalTriggers(tickets);

        // 学习最优阈值
        var bestThreshold = LearnOptimalThreshold(historicalTriggers);

        // 创建或更新阈值配置
        var config = await _dbContext.ThresholdConfigs
            .FirstOrDefaultAsync(c => c.ScenarioType == request.ScenarioType &&
                                     c.ScenarioValue == request.ScenarioValue);

        if (config == null)
        {
            config = new ThresholdConfig
            {
                ConfigId = Guid.NewGuid(),
                ConfigName = $"{request.ScenarioType}_{request.ScenarioValue ?? "default"}",
                ScenarioType = request.ScenarioType,
                ScenarioValue = request.ScenarioValue,
                TimeWindowDays = bestThreshold.TimeWindowDays,
                TriggerCount = bestThreshold.TriggerCount,
                MatchCriteria = bestThreshold.MatchCriteria,
                IsActive = true,
                IsAutoOptimized = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ThresholdConfigs.Add(config);
        }
        else
        {
            config.TimeWindowDays = bestThreshold.TimeWindowDays;
            config.TriggerCount = bestThreshold.TriggerCount;
            config.MatchCriteria = bestThreshold.MatchCriteria;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return MapToDto(config);
    }

    public async Task<ThresholdConfigDto?> GetScenarioThresholdAsync(Guid ticketId)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            return null;
        }

        // 尝试按问题类型匹配
        var config = await _dbContext.ThresholdConfigs
            .Where(c => c.IsActive &&
                        c.ScenarioType == "problem_type" &&
                        c.ScenarioValue == ticket.Domain.ToString())
            .FirstOrDefaultAsync();

        if (config == null)
        {
            // 使用默认配置
            config = await _dbContext.ThresholdConfigs
                .Where(c => c.IsActive && c.ScenarioType == "default")
                .FirstOrDefaultAsync();
        }

        return config != null ? MapToDto(config) : null;
    }

    public async Task<ThresholdEvaluation> EvaluateThresholdAsync(
        Guid configId,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var config = await _dbContext.ThresholdConfigs.FindAsync(configId);
        if (config == null)
        {
            throw new ArgumentException($"Threshold config {configId} not found");
        }

        fromDate ??= DateTime.UtcNow.AddMonths(-3);
        toDate ??= DateTime.UtcNow;

        // 获取触发历史
        var triggerHistory = await _dbContext.ThresholdTriggerHistories
            .Where(h => h.ConfigId == configId &&
                        h.TriggerTime >= fromDate.Value &&
                        h.TriggerTime <= toDate.Value)
            .ToListAsync();

        var totalTriggers = triggerHistory.Count;
        var correctTriggers = triggerHistory.Count(h => h.ActualResult == "correct");
        var falsePositives = triggerHistory.Count(h => h.ActualResult == "false_positive");
        var falseNegatives = triggerHistory.Count(h => h.ActualResult == "false_negative");

        var accuracyRate = totalTriggers > 0
            ? (decimal)correctTriggers / totalTriggers
            : 0m;

        var falsePositiveRate = totalTriggers > 0
            ? (decimal)falsePositives / totalTriggers
            : 0m;

        var falseNegativeRate = totalTriggers > 0
            ? (decimal)falseNegatives / totalTriggers
            : 0m;

        // 计算综合评分
        var score = CalculateThresholdScore(accuracyRate, falsePositiveRate, falseNegativeRate);

        return new ThresholdEvaluation
        {
            ConfigId = configId,
            TotalTriggers = totalTriggers,
            CorrectTriggers = correctTriggers,
            FalsePositives = falsePositives,
            FalseNegatives = falseNegatives,
            AccuracyRate = accuracyRate,
            FalsePositiveRate = falsePositiveRate,
            FalseNegativeRate = falseNegativeRate,
            Score = score
        };
    }

    public async Task<ThresholdConfigDto> AutoOptimizeThresholdAsync(AutoOptimizeThresholdRequest request)
    {
        var config = await _dbContext.ThresholdConfigs.FindAsync(request.ConfigId);
        if (config == null)
        {
            throw new ArgumentException($"Threshold config {request.ConfigId} not found");
        }

        // 评估当前阈值效果
        var currentEvaluation = await EvaluateThresholdAsync(request.ConfigId);

        // 如果效果不佳，尝试优化
        if (currentEvaluation.Score < 0.7m)
        {
            // 重新学习最优阈值
            var learnRequest = new LearnOptimalThresholdRequest
            {
                ScenarioType = config.ScenarioType,
                ScenarioValue = config.ScenarioValue
            };

            var optimizedConfig = await LearnOptimalThresholdAsync(learnRequest);

            if (request.ApplyOptimization)
            {
                config.TimeWindowDays = optimizedConfig.TimeWindowDays;
                config.TriggerCount = optimizedConfig.TriggerCount;
                config.MatchCriteria = JsonDocument.Parse(JsonSerializer.Serialize(optimizedConfig.MatchCriteria));
                config.IsAutoOptimized = true;
                config.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
            }

            return optimizedConfig;
        }

        return MapToDto(config);
    }

    public async Task<List<ThresholdConfigDto>> GetThresholdConfigsAsync(
        string? scenarioType = null,
        bool? isActive = null)
    {
        var query = _dbContext.ThresholdConfigs.AsQueryable();

        if (!string.IsNullOrEmpty(scenarioType))
        {
            query = query.Where(c => c.ScenarioType == scenarioType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var configs = await query
            .OrderBy(c => c.ScenarioType)
            .ThenBy(c => c.ScenarioValue)
            .ToListAsync();

        return configs.Select(MapToDto).ToList();
    }

    private List<HistoricalTrigger> AnalyzeHistoricalTriggers(List<Ticket> tickets)
    {
        var triggers = new List<HistoricalTrigger>();

        // 按设备型号和根因分组
        var groupedTickets = tickets
            .GroupBy(t => new { t.DeviceId, Domain = t.Domain })
            .ToList();

        foreach (var group in groupedTickets)
        {
            var ticketList = group.OrderBy(t => t.CreatedAt).ToList();
            
            for (int i = 0; i < ticketList.Count; i++)
            {
                var currentTicket = ticketList[i];
                var similarTickets = ticketList
                    .Where(t => t.TicketId != currentTicket.TicketId &&
                                Math.Abs((t.CreatedAt - currentTicket.CreatedAt).TotalDays) <= 30)
                    .ToList();

                triggers.Add(new HistoricalTrigger
                {
                    TicketId = currentTicket.TicketId,
                    TriggerTime = currentTicket.CreatedAt,
                    SimilarTicketCount = similarTickets.Count,
                    ShouldTrigger = similarTickets.Count >= 3 // 默认阈值为3
                });
            }
        }

        return triggers;
    }

    private ThresholdConfig LearnOptimalThreshold(List<HistoricalTrigger> historicalTriggers)
    {
        var bestConfig = new ThresholdConfig
        {
            TimeWindowDays = 30,
            TriggerCount = 3,
            MatchCriteria = JsonDocument.Parse("{\"same_device\": true, \"same_symptom\": true}")
        };
        var bestScore = 0.0m;

        // 尝试不同的阈值组合
        for (int days = 7; days <= 90; days += 7)
        {
            for (int count = 2; count <= 10; count++)
            {
                var config = new ThresholdConfig
                {
                    TimeWindowDays = days,
                    TriggerCount = count,
                    MatchCriteria = JsonDocument.Parse("{\"same_device\": true, \"same_symptom\": true}")
                };

                var evaluation = EvaluateThresholdConfig(config, historicalTriggers);
                var score = CalculateThresholdScore(
                    evaluation.AccuracyRate,
                    evaluation.FalsePositiveRate,
                    evaluation.FalseNegativeRate);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestConfig = config;
                }
            }
        }

        return bestConfig;
    }

    private ThresholdEvaluation EvaluateThresholdConfig(
        ThresholdConfig config,
        List<HistoricalTrigger> historicalTriggers)
    {
        var totalTriggers = 0;
        var correctTriggers = 0;
        var falsePositives = 0;
        var falseNegatives = 0;

        foreach (var trigger in historicalTriggers)
        {
            var nearbyTriggersCount = historicalTriggers
                .Where(t => t.TicketId != trigger.TicketId)
                .Select(t => t.TriggerTime)
                .Count(time => Math.Abs((time - trigger.TriggerTime).TotalDays) <= config.TimeWindowDays);

            var shouldTrigger = trigger.SimilarTicketCount >= config.TriggerCount &&
                                nearbyTriggersCount >= config.TriggerCount;

            if (shouldTrigger)
            {
                totalTriggers++;
                if (trigger.ShouldTrigger)
                {
                    correctTriggers++;
                }
                else
                {
                    falsePositives++;
                }
            }
            else if (trigger.ShouldTrigger)
            {
                falseNegatives++;
            }
        }

        var accuracyRate = totalTriggers > 0
            ? (decimal)correctTriggers / totalTriggers
            : 0m;

        var falsePositiveRate = totalTriggers > 0
            ? (decimal)falsePositives / totalTriggers
            : 0m;

        var falseNegativeRate = historicalTriggers.Count > 0
            ? (decimal)falseNegatives / historicalTriggers.Count
            : 0m;

        return new ThresholdEvaluation
        {
            ConfigId = config.ConfigId,
            TotalTriggers = totalTriggers,
            CorrectTriggers = correctTriggers,
            FalsePositives = falsePositives,
            FalseNegatives = falseNegatives,
            AccuracyRate = accuracyRate,
            FalsePositiveRate = falsePositiveRate,
            FalseNegativeRate = falseNegativeRate,
            Score = CalculateThresholdScore(accuracyRate, falsePositiveRate, falseNegativeRate)
        };
    }

    private decimal CalculateThresholdScore(
        decimal accuracyRate,
        decimal falsePositiveRate,
        decimal falseNegativeRate)
    {
        // 综合评分 = 准确率 * 0.5 + (1 - 误触发率) * 0.3 + (1 - 漏触发率) * 0.2
        return accuracyRate * 0.5m +
               (1.0m - falsePositiveRate) * 0.3m +
               (1.0m - falseNegativeRate) * 0.2m;
    }

    private ThresholdConfigDto MapToDto(ThresholdConfig config)
    {
        return new ThresholdConfigDto
        {
            ConfigId = config.ConfigId,
            ConfigName = config.ConfigName,
            ScenarioType = config.ScenarioType,
            ScenarioValue = config.ScenarioValue,
            TimeWindowDays = config.TimeWindowDays,
            TriggerCount = config.TriggerCount,
            MatchCriteria = config.MatchCriteria,
            TriggerRate = config.TriggerRate,
            AccuracyRate = config.AccuracyRate,
            FalsePositiveRate = config.FalsePositiveRate,
            IsActive = config.IsActive,
            IsAutoOptimized = config.IsAutoOptimized,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    private class HistoricalTrigger
    {
        public Guid TicketId { get; set; }
        public DateTime TriggerTime { get; set; }
        public int SimilarTicketCount { get; set; }
        public bool ShouldTrigger { get; set; }
    }
}

