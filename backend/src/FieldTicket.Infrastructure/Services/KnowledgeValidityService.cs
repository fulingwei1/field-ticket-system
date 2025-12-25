using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 知识有效期和版本绑定服务实现
/// </summary>
public class KnowledgeValidityService : IKnowledgeValidityService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KnowledgeValidityService> _logger;

    public KnowledgeValidityService(
        ApplicationDbContext dbContext,
        ILogger<KnowledgeValidityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> CheckAndUpdateExpiredKnowledgeAsync()
    {
        _logger.LogInformation("Checking and updating expired knowledge");

        var now = DateTime.UtcNow;
        var updatedCount = 0;

        // 检查判断卡
        var expiredJudgementCards = await _dbContext.JudgementCards
            .Where(jc => jc.ExpiryDate.HasValue && jc.ExpiryDate.Value < now && !jc.IsExpired)
            .ToListAsync();

        foreach (var jc in expiredJudgementCards)
        {
            jc.IsExpired = true;
            updatedCount++;
        }

        // 检查解决方案
        var expiredSolutions = await _dbContext.Solutions
            .Where(s => s.ExpiryDate.HasValue && s.ExpiryDate.Value < now && !s.IsExpired)
            .ToListAsync();

        foreach (var solution in expiredSolutions)
        {
            solution.IsExpired = true;
            updatedCount++;
        }

        // 同时更新未过期的知识（如果日期已更新）
        var unexpiredJudgementCards = await _dbContext.JudgementCards
            .Where(jc => jc.IsExpired && (!jc.ExpiryDate.HasValue || jc.ExpiryDate.Value >= now))
            .ToListAsync();

        foreach (var jc in unexpiredJudgementCards)
        {
            jc.IsExpired = false;
            updatedCount++;
        }

        var unexpiredSolutions = await _dbContext.Solutions
            .Where(s => s.IsExpired && (!s.ExpiryDate.HasValue || s.ExpiryDate.Value >= now))
            .ToListAsync();

        foreach (var solution in unexpiredSolutions)
        {
            solution.IsExpired = false;
            updatedCount++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated {Count} knowledge items", updatedCount);

        return updatedCount;
    }

    public async Task<bool> IsKnowledgeApplicableAsync(
        Guid knowledgeId,
        string knowledgeType,
        string? swVersion = null,
        string? hwVersion = null)
    {
        // 检查是否过期
        var isExpired = await IsKnowledgeExpiredAsync(knowledgeId, knowledgeType);
        if (isExpired)
        {
            return false;
        }

        // 检查版本匹配
        if (knowledgeType == "judgement_card")
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

            if (jc == null) return false;

            // 如果指定了软件版本，检查是否在适用版本列表中
            if (!string.IsNullOrEmpty(swVersion) && jc.ApplicableSwVersions.Any())
            {
                if (!jc.ApplicableSwVersions.Contains(swVersion))
                {
                    return false;
                }
            }

            // 如果指定了硬件版本，检查是否在适用版本列表中
            if (!string.IsNullOrEmpty(hwVersion) && jc.ApplicableHwVersions.Any())
            {
                if (!jc.ApplicableHwVersions.Contains(hwVersion))
                {
                    return false;
                }
            }

            return true;
        }
        else if (knowledgeType == "solution")
        {
            var solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == knowledgeId);

            if (solution == null) return false;

            // 如果指定了软件版本，检查是否在适用版本列表中
            if (!string.IsNullOrEmpty(swVersion) && solution.ApplicableSwVersions.Any())
            {
                if (!solution.ApplicableSwVersions.Contains(swVersion))
                {
                    return false;
                }
            }

            // 如果指定了硬件版本，检查是否在适用版本列表中
            if (!string.IsNullOrEmpty(hwVersion) && solution.ApplicableHwVersions.Any())
            {
                if (!solution.ApplicableHwVersions.Contains(hwVersion))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public async Task<bool> IsKnowledgeExpiredAsync(Guid knowledgeId, string knowledgeType)
    {
        if (knowledgeType == "judgement_card")
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

            if (jc == null) return true;

            // 先检查标记
            if (jc.IsExpired) return true;

            // 再检查日期
            if (jc.ExpiryDate.HasValue && jc.ExpiryDate.Value < DateTime.UtcNow)
            {
                // 自动更新标记
                jc.IsExpired = true;
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        else if (knowledgeType == "solution")
        {
            var solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == knowledgeId);

            if (solution == null) return true;

            // 先检查标记
            if (solution.IsExpired) return true;

            // 再检查日期
            if (solution.ExpiryDate.HasValue && solution.ExpiryDate.Value < DateTime.UtcNow)
            {
                // 自动更新标记
                solution.IsExpired = true;
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }

        return false;
    }

    public async Task<List<ExpiredKnowledgeInfoDto>> GetExpiredKnowledgeAsync(
        string? knowledgeType = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = new List<ExpiredKnowledgeInfoDto>();
        var now = DateTime.UtcNow;

        if (knowledgeType == null || knowledgeType == "judgement_card")
        {
            var expiredJcs = await _dbContext.JudgementCards
                .Where(jc => jc.IsExpired || (jc.ExpiryDate.HasValue && jc.ExpiryDate.Value < now))
                .OrderByDescending(jc => jc.ExpiryDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var jc in expiredJcs)
            {
                var expiryDate = jc.ExpiryDate ?? now;
                result.Add(new ExpiredKnowledgeInfoDto
                {
                    KnowledgeId = jc.JudgementCardId,
                    KnowledgeType = "judgement_card",
                    KnowledgeCode = jc.JcCode,
                    KnowledgeName = jc.Title,
                    ExpiryDate = jc.ExpiryDate,
                    DaysSinceExpiry = jc.ExpiryDate.HasValue
                        ? (int)(now - jc.ExpiryDate.Value).TotalDays
                        : 0,
                    ApplicableSwVersions = jc.ApplicableSwVersions,
                    ApplicableHwVersions = jc.ApplicableHwVersions
                });
            }
        }

        if (knowledgeType == null || knowledgeType == "solution")
        {
            var expiredSolutions = await _dbContext.Solutions
                .Where(s => s.IsExpired || (s.ExpiryDate.HasValue && s.ExpiryDate.Value < now))
                .OrderByDescending(s => s.ExpiryDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var solution in expiredSolutions)
            {
                result.Add(new ExpiredKnowledgeInfoDto
                {
                    KnowledgeId = solution.SolutionId,
                    KnowledgeType = "solution",
                    KnowledgeCode = solution.SolutionCode,
                    KnowledgeName = solution.Title,
                    ExpiryDate = solution.ExpiryDate,
                    DaysSinceExpiry = solution.ExpiryDate.HasValue
                        ? (int)(now - solution.ExpiryDate.Value).TotalDays
                        : 0,
                    ApplicableSwVersions = solution.ApplicableSwVersions,
                    ApplicableHwVersions = solution.ApplicableHwVersions
                });
            }
        }

        return result;
    }

    public async Task UpdateKnowledgeVersionBindingAsync(
        Guid knowledgeId,
        string knowledgeType,
        UpdateVersionBindingRequest request)
    {
        if (knowledgeType == "judgement_card")
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

            if (jc == null)
            {
                throw new KeyNotFoundException($"判断卡 {knowledgeId} 不存在");
            }

            if (request.ApplicableSwVersions != null)
            {
                jc.ApplicableSwVersions = request.ApplicableSwVersions;
            }

            if (request.ApplicableHwVersions != null)
            {
                jc.ApplicableHwVersions = request.ApplicableHwVersions;
            }

            if (request.ExpiryDate.HasValue)
            {
                jc.ExpiryDate = request.ExpiryDate.Value;
                // 更新过期标记
                jc.IsExpired = jc.ExpiryDate.Value < DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }
        else if (knowledgeType == "solution")
        {
            var solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == knowledgeId);

            if (solution == null)
            {
                throw new KeyNotFoundException($"解决方案 {knowledgeId} 不存在");
            }

            if (request.ApplicableSwVersions != null)
            {
                solution.ApplicableSwVersions = request.ApplicableSwVersions;
            }

            if (request.ApplicableHwVersions != null)
            {
                solution.ApplicableHwVersions = request.ApplicableHwVersions;
            }

            if (request.ExpiryDate.HasValue)
            {
                solution.ExpiryDate = request.ExpiryDate.Value;
                // 更新过期标记
                solution.IsExpired = solution.ExpiryDate.Value < DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }
        else
        {
            throw new ArgumentException($"不支持的知识类型: {knowledgeType}");
        }
    }

    public async Task<VersionMatchResultDto> CheckVersionMatchAsync(
        Guid ticketId,
        Guid knowledgeId,
        string knowledgeType)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        var result = new VersionMatchResultDto();

        // 检查是否过期
        result.IsExpired = await IsKnowledgeExpiredAsync(knowledgeId, knowledgeType);

        // 获取工单的版本信息
        var ticketSwVersion = ticket.SwVersion;
        var ticketHwVersion = ticket.HwVersion; // 假设工单有硬件版本字段，如果没有可以忽略

        // 检查版本匹配
        result.IsApplicable = await IsKnowledgeApplicableAsync(
            knowledgeId,
            knowledgeType,
            ticketSwVersion,
            ticketHwVersion);

        // 生成警告信息
        var warnings = new List<string>();

        if (result.IsExpired)
        {
            warnings.Add("该知识已过期，可能不再适用");
        }

        if (!result.IsApplicable)
        {
            if (knowledgeType == "judgement_card")
            {
                var jc = await _dbContext.JudgementCards
                    .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

                if (jc != null)
                {
                    if (!string.IsNullOrEmpty(ticketSwVersion) && jc.ApplicableSwVersions.Any() &&
                        !jc.ApplicableSwVersions.Contains(ticketSwVersion))
                    {
                        warnings.Add($"软件版本 {ticketSwVersion} 不在适用版本范围内");
                    }

                    if (!string.IsNullOrEmpty(ticketHwVersion) && jc.ApplicableHwVersions.Any() &&
                        !jc.ApplicableHwVersions.Contains(ticketHwVersion))
                    {
                        warnings.Add($"硬件版本 {ticketHwVersion} 不在适用版本范围内");
                    }
                }
            }
            else if (knowledgeType == "solution")
            {
                var solution = await _dbContext.Solutions
                    .FirstOrDefaultAsync(s => s.SolutionId == knowledgeId);

                if (solution != null)
                {
                    if (!string.IsNullOrEmpty(ticketSwVersion) && solution.ApplicableSwVersions.Any() &&
                        !solution.ApplicableSwVersions.Contains(ticketSwVersion))
                    {
                        warnings.Add($"软件版本 {ticketSwVersion} 不在适用版本范围内");
                    }

                    if (!string.IsNullOrEmpty(ticketHwVersion) && solution.ApplicableHwVersions.Any() &&
                        !solution.ApplicableHwVersions.Contains(ticketHwVersion))
                    {
                        warnings.Add($"硬件版本 {ticketHwVersion} 不在适用版本范围内");
                    }
                }
            }
        }

        result.WarningMessage = warnings.Any() ? string.Join("; ", warnings) : null;
        result.MatchDetails = new Dictionary<string, object>
        {
            { "ticket_sw_version", ticketSwVersion ?? "未指定" },
            { "ticket_hw_version", ticketHwVersion ?? "未指定" },
            { "is_expired", result.IsExpired },
            { "is_applicable", result.IsApplicable }
        };

        return result;
    }
}






