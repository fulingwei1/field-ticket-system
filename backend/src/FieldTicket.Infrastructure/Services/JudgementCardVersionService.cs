using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VersionComparisonDto = FieldTicket.Core.Services.VersionComparisonDto;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 判断卡版本管理服务实现
/// </summary>
public class JudgementCardVersionService : IJudgementCardVersionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<JudgementCardVersionService> _logger;

    public JudgementCardVersionService(
        ApplicationDbContext dbContext,
        ILogger<JudgementCardVersionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<JudgementCardDto> CreateNewVersionAsync(
        string jcCode,
        UpdateJudgementCardRequest request,
        string changeReason,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(changeReason))
        {
            throw new ArgumentException("变更原因不能为空，必须填写推翻原因");
        }

        // 获取当前版本
        var currentVersion = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode && jc.IsCurrent);

        if (currentVersion == null)
        {
            throw new KeyNotFoundException($"判断卡 {jcCode} 不存在");
        }

        // 获取下一个版本号
        var maxVersion = await _dbContext.JudgementCards
            .Where(jc => jc.JcCode == jcCode)
            .MaxAsync(jc => (int?)jc.Version) ?? 0;
        var newVersion = maxVersion + 1;

        // 将当前版本标记为非当前版本
        currentVersion.IsCurrent = false;
        currentVersion.UpdatedAt = DateTime.UtcNow;

        // 创建新版本
        var newCard = new JudgementCard
        {
            JudgementCardId = Guid.NewGuid(),
            JcCode = jcCode,
            Title = request.Title ?? currentVersion.Title,
            Description = request.Description ?? currentVersion.Description,
            Domain = request.Domain ?? currentVersion.Domain,
            SymptomStructure = request.SymptomStructure ?? currentVersion.SymptomStructure,
            TroubleshootingPath = request.TroubleshootingPath ?? currentVersion.TroubleshootingPath,
            HypothesisTemplate = request.HypothesisTemplate ?? currentVersion.HypothesisTemplate,
            NextActionTemplate = request.NextActionTemplate ?? currentVersion.NextActionTemplate,
            EscalationConditions = request.EscalationConditions ?? currentVersion.EscalationConditions,
            Status = request.Status ?? currentVersion.Status,
            Version = newVersion,
            ParentJcId = currentVersion.JudgementCardId,
            IsCurrent = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UsageCount = 0
        };

        _dbContext.JudgementCards.Add(newCard);

        // 创建变更记录
        var changeLog = new JudgementCardChangeLog
        {
            ChangeLogId = Guid.NewGuid(),
            JcCode = jcCode,
            Version = newVersion,
            PreviousVersionId = currentVersion.JudgementCardId,
            ChangeReason = changeReason,
            ChangeSummary = GenerateChangeSummary(currentVersion, newCard),
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            IsOverturned = true // 创建新版本意味着推翻旧版本
        };

        _dbContext.JudgementCardChangeLogs.Add(changeLog);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("判断卡 {JcCode} 创建新版本 v{Version}，变更原因：{ChangeReason}",
            jcCode, newVersion, changeReason);

        return MapToDto(newCard);
    }

    public async Task<List<JudgementCardVersionDto>> GetVersionsAsync(string jcCode)
    {
        var cards = await _dbContext.JudgementCards
            .Where(jc => jc.JcCode == jcCode)
            .OrderByDescending(jc => jc.Version)
            .ToListAsync();

        var changeLogs = await _dbContext.JudgementCardChangeLogs
            .Where(cl => cl.JcCode == jcCode)
            .ToListAsync();

        var versions = new List<JudgementCardVersionDto>();

        foreach (var card in cards)
        {
            var changeLog = changeLogs.FirstOrDefault(cl => cl.Version == card.Version);
            versions.Add(new JudgementCardVersionDto
            {
                JudgementCardId = card.JudgementCardId,
                JcCode = card.JcCode,
                Version = card.Version,
                Title = card.Title,
                IsCurrent = card.IsCurrent,
                ParentJcId = card.ParentJcId,
                ChangeReason = changeLog?.ChangeReason,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt,
                UsageCount = card.UsageCount
            });
        }

        return versions;
    }

    public async Task<JudgementCardDto?> GetVersionAsync(string jcCode, int version)
    {
        var card = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode && jc.Version == version);

        if (card == null)
        {
            return null;
        }

        return MapToDto(card);
    }

    public async Task<VersionComparisonDto> CompareVersionsAsync(string jcCode, int version1, int version2)
    {
        var card1 = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode && jc.Version == version1);
        var card2 = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode && jc.Version == version2);

        if (card1 == null || card2 == null)
        {
            throw new KeyNotFoundException($"判断卡 {jcCode} 的版本 {version1} 或 {version2} 不存在");
        }

        var changes = CompareCards(card1, card2);

        var changeLog1 = await _dbContext.JudgementCardChangeLogs
            .FirstOrDefaultAsync(cl => cl.JcCode == jcCode && cl.Version == version1);
        var changeLog2 = await _dbContext.JudgementCardChangeLogs
            .FirstOrDefaultAsync(cl => cl.JcCode == jcCode && cl.Version == version2);

        return new VersionComparisonDto
        {
            JcCode = jcCode,
            Version1 = new JudgementCardVersionDto
            {
                JudgementCardId = card1.JudgementCardId,
                JcCode = card1.JcCode,
                Version = card1.Version,
                Title = card1.Title,
                IsCurrent = card1.IsCurrent,
                ParentJcId = card1.ParentJcId,
                ChangeReason = changeLog1?.ChangeReason,
                CreatedAt = card1.CreatedAt,
                UpdatedAt = card1.UpdatedAt,
                UsageCount = card1.UsageCount
            },
            Version2 = new JudgementCardVersionDto
            {
                JudgementCardId = card2.JudgementCardId,
                JcCode = card2.JcCode,
                Version = card2.Version,
                Title = card2.Title,
                IsCurrent = card2.IsCurrent,
                ParentJcId = card2.ParentJcId,
                ChangeReason = changeLog2?.ChangeReason,
                CreatedAt = card2.CreatedAt,
                UpdatedAt = card2.UpdatedAt,
                UsageCount = card2.UsageCount
            },
            Changes = changes
        };
    }

    public async Task<List<JudgementCardUsageHistoryDto>> GetUsageHistoryAsync(string jcCode, int? version = null)
    {
        var query = _dbContext.JudgementCardUsageHistories
            .Where(uh => uh.JcCode == jcCode);

        if (version.HasValue)
        {
            query = query.Where(uh => uh.JcVersion == version.Value);
        }

        var histories = await query
            .OrderByDescending(uh => uh.UsedAt)
            .ToListAsync();

        var ticketIds = histories.Select(h => h.TicketId).Distinct().ToList();
        var tickets = await _dbContext.Tickets
            .Where(t => ticketIds.Contains(t.TicketId))
            .ToDictionaryAsync(t => t.TicketId, t => t);

        var userIds = histories.Select(h => h.UsedBy).Distinct().ToList();
        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        return histories.Select(h => new JudgementCardUsageHistoryDto
        {
            UsageHistoryId = h.UsageHistoryId,
            JcCode = h.JcCode,
            JcVersion = h.JcVersion,
            TicketId = h.TicketId,
            TicketNo = tickets.GetValueOrDefault(h.TicketId)?.TicketNo,
            UsedBy = h.UsedBy,
            UsedByName = users.GetValueOrDefault(h.UsedBy)?.Name,
            UsedAt = h.UsedAt,
            Result = h.Result,
            Feedback = h.Feedback
        }).ToList();
    }

    public async Task RecordUsageAsync(
        string jcCode,
        int version,
        Guid ticketId,
        Guid userId,
        string? result = null,
        string? feedback = null)
    {
        var usageHistory = new JudgementCardUsageHistory
        {
            UsageHistoryId = Guid.NewGuid(),
            JcCode = jcCode,
            JcVersion = version,
            TicketId = ticketId,
            UsedBy = userId,
            UsedAt = DateTime.UtcNow,
            Result = result,
            Feedback = feedback,
            IsValid = true
        };

        _dbContext.JudgementCardUsageHistories.Add(usageHistory);

        // 更新判断卡使用统计
        var card = await _dbContext.JudgementCards
            .FirstOrDefaultAsync(jc => jc.JcCode == jcCode && jc.Version == version);

        if (card != null)
        {
            card.UsageCount++;
            card.LastUsedAt = DateTime.UtcNow;
            card.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("记录判断卡 {JcCode} v{Version} 使用历史，工单 {TicketId}，结果：{Result}",
            jcCode, version, ticketId, result ?? "未记录");
    }

    /// <summary>
    /// 生成变更摘要
    /// </summary>
    private string GenerateChangeSummary(JudgementCard oldCard, JudgementCard newCard)
    {
        var changes = new List<string>();

        if (oldCard.Title != newCard.Title)
            changes.Add($"标题：{oldCard.Title} → {newCard.Title}");

        if (oldCard.Description != newCard.Description)
            changes.Add("描述已更新");

        if (oldCard.Domain != newCard.Domain)
            changes.Add($"问题域：{oldCard.Domain} → {newCard.Domain}");

        if (oldCard.HypothesisTemplate != newCard.HypothesisTemplate)
            changes.Add("假设模板已更新");

        if (oldCard.NextActionTemplate != newCard.NextActionTemplate)
            changes.Add("下一步动作模板已更新");

        // 比较 JSON 字段（简化处理）
        if (oldCard.SymptomStructure.ToString() != newCard.SymptomStructure.ToString())
            changes.Add("症状结构已更新");

        if (oldCard.TroubleshootingPath.ToString() != newCard.TroubleshootingPath.ToString())
            changes.Add("排查路径已更新");

        return changes.Count > 0 ? string.Join("；", changes) : "无变更";
    }

    /// <summary>
    /// 对比两个判断卡
    /// </summary>
    private List<FieldChange> CompareCards(JudgementCard card1, JudgementCard card2)
    {
        var changes = new List<FieldChange>();

        if (card1.Title != card2.Title)
        {
            changes.Add(new FieldChange
            {
                FieldName = "Title",
                FieldLabel = "标题",
                OldValue = card1.Title,
                NewValue = card2.Title,
                ChangeType = "Modified"
            });
        }

        if (card1.Description != card2.Description)
        {
            changes.Add(new FieldChange
            {
                FieldName = "Description",
                FieldLabel = "描述",
                OldValue = card1.Description,
                NewValue = card2.Description,
                ChangeType = "Modified"
            });
        }

        if (card1.Domain != card2.Domain)
        {
            changes.Add(new FieldChange
            {
                FieldName = "Domain",
                FieldLabel = "问题域",
                OldValue = card1.Domain.ToString(),
                NewValue = card2.Domain.ToString(),
                ChangeType = "Modified"
            });
        }

        if (card1.HypothesisTemplate != card2.HypothesisTemplate)
        {
            changes.Add(new FieldChange
            {
                FieldName = "HypothesisTemplate",
                FieldLabel = "假设模板",
                OldValue = card1.HypothesisTemplate,
                NewValue = card2.HypothesisTemplate,
                ChangeType = "Modified"
            });
        }

        if (card1.NextActionTemplate != card2.NextActionTemplate)
        {
            changes.Add(new FieldChange
            {
                FieldName = "NextActionTemplate",
                FieldLabel = "下一步动作模板",
                OldValue = card1.NextActionTemplate,
                NewValue = card2.NextActionTemplate,
                ChangeType = "Modified"
            });
        }

        // JSON 字段对比（简化处理）
        if (card1.SymptomStructure.ToString() != card2.SymptomStructure.ToString())
        {
            changes.Add(new FieldChange
            {
                FieldName = "SymptomStructure",
                FieldLabel = "症状结构",
                OldValue = "已修改",
                NewValue = "已修改",
                ChangeType = "Modified"
            });
        }

        if (card1.TroubleshootingPath.ToString() != card2.TroubleshootingPath.ToString())
        {
            changes.Add(new FieldChange
            {
                FieldName = "TroubleshootingPath",
                FieldLabel = "排查路径",
                OldValue = "已修改",
                NewValue = "已修改",
                ChangeType = "Modified"
            });
        }

        return changes;
    }

    /// <summary>
    /// 映射到 DTO
    /// </summary>
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

