using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单去重检测服务实现
/// </summary>
public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DuplicateDetectionService> _logger;
    private const int TIME_WINDOW_DAYS = 7; // 7天内的时间窗口

    public DuplicateDetectionService(
        ApplicationDbContext dbContext,
        ILogger<DuplicateDetectionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<DuplicateCandidate>> DetectDuplicatesAsync(
        Guid ticketId,
        int maxResults = 10)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 排除已合并的工单和自己
        var candidateTickets = await _dbContext.Tickets
            .Where(t => t.TicketId != ticketId &&
                       t.MergedInto == null &&
                       t.DuplicateOf == null &&
                       t.Status != "Draft")
            .ToListAsync();

        var candidates = new List<DuplicateCandidate>();

        foreach (var candidate in candidateTickets)
        {
            var similarity = await CalculateSimilarityAsync(ticketId, candidate.TicketId);
            
            // 只返回相似度 >= 0.5 的候选
            if (similarity.Overall >= 0.5m)
            {
                candidates.Add(new DuplicateCandidate
                {
                    TicketId = candidate.TicketId,
                    TicketNo = candidate.TicketNo,
                    SymptomTitle = candidate.SymptomTitle,
                    Domain = candidate.Domain,
                    StepCode = candidate.StepCode,
                    CreatedAt = candidate.CreatedAt,
                    Status = candidate.Status,
                    Similarity = similarity
                });
            }
        }

        // 按相似度排序，取Top K
        var topCandidates = candidates
            .OrderByDescending(c => c.Similarity.Overall)
            .Take(maxResults)
            .ToList();

        // 记录检测日志
        if (topCandidates.Any())
        {
            await LogDetectionAsync(ticketId, topCandidates.Select(c => c.TicketId).ToList());
        }

        _logger.LogInformation("检测到工单 {TicketId} 的 {Count} 个潜在重复工单",
            ticketId, topCandidates.Count);

        return topCandidates;
    }

    public async Task<MergeResult> MergeTicketsAsync(
        Guid sourceTicketId,
        Guid targetTicketId,
        string reason,
        Guid userId)
    {
        var sourceTicket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == sourceTicketId);
        var targetTicket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == targetTicketId);

        if (sourceTicket == null || targetTicket == null)
        {
            return new MergeResult
            {
                Success = false,
                Message = "工单不存在"
            };
        }

        if (sourceTicket.MergedInto != null || targetTicket.MergedInto != null)
        {
            return new MergeResult
            {
                Success = false,
                Message = "工单已被合并，无法再次合并"
            };
        }

        // 合并操作
        sourceTicket.MergedInto = targetTicketId;
        sourceTicket.MergeReason = reason;
        sourceTicket.MergedBy = userId;
        sourceTicket.MergedAt = DateTime.UtcNow;
        sourceTicket.Status = "Merged"; // 新增状态，或使用现有状态
        sourceTicket.UpdatedAt = DateTime.UtcNow;

        // 合并附件（如果有）
        await MergeAttachmentsAsync(sourceTicketId, targetTicketId);

        // 合并沟通记录（如果有）
        await MergeCommunicationsAsync(sourceTicketId, targetTicketId);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("工单 {SourceTicketId} 已合并到 {TargetTicketId}，原因：{Reason}",
            sourceTicketId, targetTicketId, reason);

        return new MergeResult
        {
            Success = true,
            Message = "合并成功",
            SourceTicketId = sourceTicketId,
            TargetTicketId = targetTicketId
        };
    }

    public async Task<SimilarityScore> CalculateSimilarityAsync(
        Guid ticketId1,
        Guid ticketId2)
    {
        var ticket1 = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId1);
        var ticket2 = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId2);

        if (ticket1 == null || ticket2 == null)
        {
            throw new KeyNotFoundException("工单不存在");
        }

        var score = new SimilarityScore
        {
            Breakdown = new Dictionary<string, decimal>()
        };

        // 1. 设备相似度（40%）
        score.Device = CalculateDeviceSimilarity(ticket1, ticket2);
        score.Breakdown["device"] = score.Device;

        // 2. 症状相似度（30%）
        score.Symptom = CalculateSymptomSimilarity(ticket1, ticket2);
        score.Breakdown["symptom"] = score.Symptom;

        // 3. 问题域相似度（20%）
        score.Domain = CalculateDomainSimilarity(ticket1, ticket2);
        score.Breakdown["domain"] = score.Domain;

        // 4. 时间相似度（10%）
        score.Time = CalculateTimeSimilarity(ticket1, ticket2);
        score.Breakdown["time"] = score.Time;

        // 综合评分
        score.Overall = score.Device * 0.4m + score.Symptom * 0.3m + score.Domain * 0.2m + score.Time * 0.1m;

        return score;
    }

    public async Task<List<MergeHistoryDto>> GetMergeHistoryAsync(Guid ticketId)
    {
        var history = new List<MergeHistoryDto>();

        // 查找被合并的工单（source）
        var mergedTickets = await _dbContext.Tickets
            .Where(t => t.MergedInto == ticketId)
            .ToListAsync();

        foreach (var merged in mergedTickets)
        {
            history.Add(new MergeHistoryDto
            {
                TicketId = merged.TicketId,
                TicketNo = merged.TicketNo,
                MergeReason = merged.MergeReason ?? "",
                MergedBy = merged.MergedBy,
                MergedAt = merged.MergedAt ?? DateTime.UtcNow,
                IsSource = true
            });
        }

        // 查找合并到的工单（target）
        var currentTicket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (currentTicket != null && currentTicket.MergedInto != null)
        {
            var targetTicket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.TicketId == currentTicket.MergedInto);

            if (targetTicket != null)
            {
                history.Add(new MergeHistoryDto
                {
                    TicketId = targetTicket.TicketId,
                    TicketNo = targetTicket.TicketNo,
                    MergeReason = currentTicket.MergeReason ?? "",
                    MergedBy = currentTicket.MergedBy,
                    MergedAt = currentTicket.MergedAt ?? DateTime.UtcNow,
                    IsSource = false
                });
            }
        }

        return history.OrderByDescending(h => h.MergedAt).ToList();
    }

    /// <summary>
    /// 计算设备相似度
    /// </summary>
    private decimal CalculateDeviceSimilarity(Ticket ticket1, Ticket ticket2)
    {
        if (ticket1.DeviceId == ticket2.DeviceId)
        {
            return 1.0m;
        }
        return 0.0m;
    }

    /// <summary>
    /// 计算症状相似度（使用简单的文本相似度）
    /// </summary>
    private decimal CalculateSymptomSimilarity(Ticket ticket1, Ticket ticket2)
    {
        var text1 = $"{ticket1.SymptomTitle} {ticket1.SymptomDetail ?? ""}";
        var text2 = $"{ticket2.SymptomTitle} {ticket2.SymptomDetail ?? ""}";

        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
        {
            return 0.0m;
        }

        // 使用简单的词汇重叠度
        var words1 = ExtractWords(text1);
        var words2 = ExtractWords(text2);

        if (!words1.Any() || !words2.Any())
        {
            return 0.0m;
        }

        var intersection = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
        var union = words1.Union(words2, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (decimal)intersection / union : 0.0m;
    }

    /// <summary>
    /// 计算问题域相似度
    /// </summary>
    private decimal CalculateDomainSimilarity(Ticket ticket1, Ticket ticket2)
    {
        var score = 0.0m;

        // 问题域匹配
        if (ticket1.Domain == ticket2.Domain)
        {
            score += 0.6m;
        }

        // 步骤匹配
        if (ticket1.StepCode == ticket2.StepCode)
        {
            score += 0.4m;
        }
        else if (!string.IsNullOrEmpty(ticket1.StepCode) && 
                 !string.IsNullOrEmpty(ticket2.StepCode) &&
                 (ticket1.StepCode.Contains(ticket2.StepCode) || ticket2.StepCode.Contains(ticket1.StepCode)))
        {
            score += 0.2m;
        }

        return Math.Min(1.0m, score);
    }

    /// <summary>
    /// 计算时间相似度
    /// </summary>
    private decimal CalculateTimeSimilarity(Ticket ticket1, Ticket ticket2)
    {
        var timeDiff = Math.Abs((ticket1.CreatedAt - ticket2.CreatedAt).TotalDays);

        if (timeDiff <= 1)
        {
            return 1.0m;
        }
        else if (timeDiff <= 3)
        {
            return 0.8m;
        }
        else if (timeDiff <= 7)
        {
            return 0.5m;
        }
        else if (timeDiff <= 30)
        {
            return 0.2m;
        }

        return 0.0m;
    }

    /// <summary>
    /// 提取文本中的词汇
    /// </summary>
    private List<string> ExtractWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        // 移除标点符号，转换为小写，分割
        var words = Regex.Replace(text, @"[^\w\s]", " ")
            .ToLower()
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1) // 过滤单字符
            .Distinct()
            .ToList();

        return words;
    }

    /// <summary>
    /// 记录检测日志
    /// </summary>
    private async Task LogDetectionAsync(Guid ticketId, List<Guid> potentialDuplicates)
    {
        try
        {
            var log = new DuplicateDetectionLog
            {
                LogId = Guid.NewGuid(),
                TicketId = ticketId,
                PotentialDuplicates = potentialDuplicates,
                SimilarityScores = JsonDocument.Parse("{}"), // TODO: 保存详细相似度评分
                DetectedAt = DateTime.UtcNow,
                DetectionType = "auto"
            };

            _dbContext.DuplicateDetectionLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录去重检测日志失败，工单：{TicketId}", ticketId);
        }
    }

    /// <summary>
    /// 合并附件：将源工单的附件关联到目标工单
    /// </summary>
    private async Task MergeAttachmentsAsync(Guid sourceTicketId, Guid targetTicketId)
    {
        try
        {
            var attachments = await _dbContext.Attachments
                .Where(a => a.TicketId == sourceTicketId)
                .ToListAsync();

            if (attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    attachment.TicketId = targetTicketId;
                }

                _logger.LogInformation("合并附件：将工单 {SourceTicketId} 的 {Count} 个附件合并到工单 {TargetTicketId}",
                    sourceTicketId, attachments.Count, targetTicketId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并附件失败，源工单：{SourceTicketId}，目标工单：{TargetTicketId}",
                sourceTicketId, targetTicketId);
            // 不抛出异常，允许合并流程继续
        }
    }

    /// <summary>
    /// 合并沟通记录：将源工单的沟通记录关联到目标工单
    /// </summary>
    private async Task MergeCommunicationsAsync(Guid sourceTicketId, Guid targetTicketId)
    {
        try
        {
            var communications = await _dbContext.CustomerCommunications
                .Where(c => c.TicketId == sourceTicketId)
                .ToListAsync();

            if (communications.Any())
            {
                foreach (var communication in communications)
                {
                    communication.TicketId = targetTicketId;
                }

                _logger.LogInformation("合并沟通记录：将工单 {SourceTicketId} 的 {Count} 条沟通记录合并到工单 {TargetTicketId}",
                    sourceTicketId, communications.Count, targetTicketId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并沟通记录失败，源工单：{SourceTicketId}，目标工单：{TargetTicketId}",
                sourceTicketId, targetTicketId);
            // 不抛出异常，允许合并流程继续
        }
    }
}

