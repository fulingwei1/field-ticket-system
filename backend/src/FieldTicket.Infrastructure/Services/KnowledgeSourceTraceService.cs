using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 知识来源追溯服务实现
/// </summary>
public class KnowledgeSourceTraceService : IKnowledgeSourceTraceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KnowledgeSourceTraceService> _logger;

    public KnowledgeSourceTraceService(
        ApplicationDbContext dbContext,
        ILogger<KnowledgeSourceTraceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<KnowledgeSourceTraceDto?> GetSourceTraceAsync(Guid knowledgeId, string knowledgeType)
    {
        if (knowledgeType == "judgement_card")
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

            if (jc == null) return null;

            var dto = new KnowledgeSourceTraceDto
            {
                KnowledgeId = jc.JudgementCardId,
                KnowledgeType = "judgement_card",
                KnowledgeCode = jc.JcCode,
                KnowledgeName = jc.Title,
                SourceTicketId = jc.SourceTicketId,
                CreatedBy = jc.CreatedBy,
                CreatedAt = jc.CreatedAt,
                VerifiedBy = jc.VerifiedBy,
                LastVerifiedAt = jc.LastVerifiedAt,
                VerificationCount = jc.VerificationCount
            };

            // 获取来源工单编号
            if (jc.SourceTicketId.HasValue)
            {
                var ticket = await _dbContext.Tickets
                    .Where(t => t.TicketId == jc.SourceTicketId.Value)
                    .Select(t => t.TicketNo)
                    .FirstOrDefaultAsync();
                dto.SourceTicketNo = ticket;
            }

            // 获取创建人名称
            var creator = await _dbContext.Users
                .Where(u => u.Id == jc.CreatedBy)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            dto.CreatedByName = creator;

            // 获取验证人名称
            if (jc.VerifiedBy.HasValue)
            {
                var verifier = await _dbContext.Users
                    .Where(u => u.Id == jc.VerifiedBy.Value)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync();
                dto.VerifiedByName = verifier;
            }

            return dto;
        }
        else if (knowledgeType == "solution")
        {
            var solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == knowledgeId);

            if (solution == null) return null;

            var dto = new KnowledgeSourceTraceDto
            {
                KnowledgeId = solution.SolutionId,
                KnowledgeType = "solution",
                KnowledgeCode = solution.SolutionCode,
                KnowledgeName = solution.Title,
                SourceTicketId = solution.TicketId, // Solution 的 TicketId 就是来源工单
                CreatedBy = solution.CreatedBy,
                CreatedAt = solution.CreatedAt,
                VerifiedBy = solution.VerifiedBy,
                LastVerifiedAt = solution.LastVerifiedAt,
                VerificationCount = solution.VerificationCount
            };

            // 获取来源工单编号
            var ticket = await _dbContext.Tickets
                .Where(t => t.TicketId == solution.TicketId)
                .Select(t => t.TicketNo)
                .FirstOrDefaultAsync();
            dto.SourceTicketNo = ticket;

            // 获取创建人名称
            var creator = await _dbContext.Users
                .Where(u => u.Id == solution.CreatedBy)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            dto.CreatedByName = creator;

            // 获取验证人名称
            if (solution.VerifiedBy.HasValue)
            {
                var verifier = await _dbContext.Users
                    .Where(u => u.Id == solution.VerifiedBy.Value)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync();
                dto.VerifiedByName = verifier;
            }

            return dto;
        }

        return null;
    }

    public async Task RecordVerificationAsync(
        Guid knowledgeId,
        string knowledgeType,
        Guid verifiedBy,
        string? verificationNote = null)
    {
        _logger.LogInformation("Recording verification for knowledge {KnowledgeId} of type {KnowledgeType}", knowledgeId, knowledgeType);

        // 更新知识实体验证信息
        if (knowledgeType == "judgement_card")
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

            if (jc == null)
            {
                throw new KeyNotFoundException($"判断卡 {knowledgeId} 不存在");
            }

            jc.VerifiedBy = verifiedBy;
            jc.LastVerifiedAt = DateTime.UtcNow;
            jc.VerificationCount++;
        }
        else if (knowledgeType == "solution")
        {
            var solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == knowledgeId);

            if (solution == null)
            {
                throw new KeyNotFoundException($"解决方案 {knowledgeId} 不存在");
            }

            solution.VerifiedBy = verifiedBy;
            solution.LastVerifiedAt = DateTime.UtcNow;
            solution.VerificationCount++;
        }
        else
        {
            throw new ArgumentException($"不支持的知识类型: {knowledgeType}");
        }

        // 记录验证历史
        var history = new KnowledgeVerificationHistory
        {
            VerificationId = Guid.NewGuid(),
            KnowledgeId = knowledgeId,
            KnowledgeType = knowledgeType,
            VerifiedBy = verifiedBy,
            VerifiedAt = DateTime.UtcNow,
            VerificationNote = verificationNote
        };

        _dbContext.KnowledgeVerificationHistories.Add(history);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Recorded verification for knowledge {KnowledgeId}", knowledgeId);
    }

    public async Task UpdateSourceAsync(
        Guid knowledgeId,
        string knowledgeType,
        UpdateKnowledgeSourceRequest request)
    {
        if (knowledgeType == "judgement_card")
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JudgementCardId == knowledgeId);

            if (jc == null)
            {
                throw new KeyNotFoundException($"判断卡 {knowledgeId} 不存在");
            }

            if (request.SourceTicketId.HasValue)
            {
                jc.SourceTicketId = request.SourceTicketId.Value;
            }

            await _dbContext.SaveChangesAsync();
        }
        else if (knowledgeType == "solution")
        {
            // Solution 的 TicketId 就是来源工单，通常不需要更新
            // 如果需要更新，可以在这里实现
            throw new InvalidOperationException("解决方案的来源工单不能更改");
        }
        else
        {
            throw new ArgumentException($"不支持的知识类型: {knowledgeType}");
        }
    }

    public async Task<List<KnowledgeVerificationHistoryDto>> GetVerificationHistoryAsync(
        Guid knowledgeId,
        string knowledgeType,
        int page = 1,
        int pageSize = 20)
    {
        var histories = await _dbContext.KnowledgeVerificationHistories
            .Where(h => h.KnowledgeId == knowledgeId && h.KnowledgeType == knowledgeType)
            .OrderByDescending(h => h.VerifiedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = histories.Select(h => h.VerifiedBy).Distinct().ToList();
        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return histories.Select(h => new KnowledgeVerificationHistoryDto
        {
            VerificationId = h.VerificationId,
            KnowledgeId = h.KnowledgeId,
            KnowledgeType = h.KnowledgeType,
            VerifiedBy = h.VerifiedBy,
            VerifiedByName = users.GetValueOrDefault(h.VerifiedBy),
            VerifiedAt = h.VerifiedAt,
            VerificationNote = h.VerificationNote
        }).ToList();
    }

    public async Task<KnowledgeCredibilityDto> GetCredibilityAsync(Guid knowledgeId, string knowledgeType)
    {
        var trace = await GetSourceTraceAsync(knowledgeId, knowledgeType);
        if (trace == null)
        {
            throw new KeyNotFoundException($"知识 {knowledgeId} 不存在");
        }

        var score = 0m;
        var factors = new Dictionary<string, object>();

        // 因子1：验证次数（0-40分）
        var verificationScore = Math.Min(trace.VerificationCount * 5, 40);
        score += verificationScore;
        factors["verification_count"] = trace.VerificationCount;
        factors["verification_score"] = verificationScore;

        // 因子2：是否有验证人（0-20分）
        if (trace.VerifiedBy.HasValue)
        {
            score += 20;
            factors["has_verifier"] = true;
        }
        else
        {
            factors["has_verifier"] = false;
        }

        // 因子3：是否有来源工单（0-20分）
        if (trace.SourceTicketId.HasValue)
        {
            score += 20;
            factors["has_source_ticket"] = true;
        }
        else
        {
            factors["has_source_ticket"] = false;
        }

        // 因子4：最后验证时间（0-20分）
        if (trace.LastVerifiedAt.HasValue)
        {
            var daysSinceVerification = (DateTime.UtcNow - trace.LastVerifiedAt.Value).Days;
            if (daysSinceVerification <= 30)
            {
                score += 20;
            }
            else if (daysSinceVerification <= 90)
            {
                score += 10;
            }
            else if (daysSinceVerification <= 180)
            {
                score += 5;
            }
            factors["days_since_verification"] = daysSinceVerification;
        }

        // 确定可信度等级
        string credibilityLevel;
        string? assessment = null;

        if (score >= 80)
        {
            credibilityLevel = "high";
            assessment = "高可信度：已验证多次，来源清晰，最近已验证";
        }
        else if (score >= 60)
        {
            credibilityLevel = "medium";
            assessment = "中等可信度：已验证，但可能需要更新";
        }
        else
        {
            credibilityLevel = "low";
            assessment = "低可信度：验证不足或来源不清晰，建议验证";
        }

        return new KnowledgeCredibilityDto
        {
            KnowledgeId = knowledgeId,
            KnowledgeType = knowledgeType,
            CredibilityScore = score,
            CredibilityLevel = credibilityLevel,
            ScoreFactors = factors,
            Assessment = assessment
        };
    }
}



















