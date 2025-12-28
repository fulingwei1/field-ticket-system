using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 现场问题自动生成服务实现
/// </summary>
public class FieldProblemAutoGenerationService : IFieldProblemAutoGenerationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<FieldProblemAutoGenerationService> _logger;
    private readonly ISolutionRecommendationService _recommendationService;

    public FieldProblemAutoGenerationService(
        ApplicationDbContext dbContext,
        ILogger<FieldProblemAutoGenerationService> logger,
        ISolutionRecommendationService recommendationService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _recommendationService = recommendationService;
    }

    /// <summary>
    /// 从工单自动生成FieldProblem记录
    /// </summary>
    public async Task<AutoGenerateProblemResult> GenerateFromTicketAsync(Guid ticketId)
    {
        _logger.LogInformation("Auto-generating FieldProblem from ticket {TicketId}", ticketId);

        try
        {
            // 获取工单详情
            var ticket = await _dbContext.Tickets
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                return new AutoGenerateProblemResult
                {
                    Success = false,
                    Message = "工单不存在"
                };
            }

            // 检查是否已经生成过
            var existingProblem = await _dbContext.Set<FieldProblem>()
                .FirstOrDefaultAsync(p => p.RelatedTicketId == ticketId);

            if (existingProblem != null)
            {
                _logger.LogWarning("FieldProblem already exists for ticket {TicketId}", ticketId);
                return new AutoGenerateProblemResult
                {
                    Success = true,
                    ProblemId = existingProblem.ProblemId,
                    Message = "问题记录已存在",
                    IsRepeatProblem = existingProblem.IsRepeatProblem,
                    RelatedHistoryProblemId = existingProblem.RelatedHistoryProblemId
                };
            }

            // 检测是否为重复问题
            var (isRepeat, relatedProblemId, similarityScore) = await DetectRepeatProblemAsync(ticketId);

            // 获取解决方案
            var solution = await _dbContext.Solutions
                .Where(s => s.TicketId == ticketId && s.Status == "Published")
                .OrderByDescending(s => s.PublishedAt)
                .FirstOrDefaultAsync();

            // 获取根本原因分析
            var rootCauseAnalysis = await _dbContext.Set<RootCauseAnalysis>()
                .FirstOrDefaultAsync(r => r.TicketId == ticketId);

            // 生成问题序号
            var problemSequence = await GenerateProblemSequenceAsync(ticket.ProjectId);

            // 创建FieldProblem记录
            var fieldProblem = new FieldProblem
            {
                ProblemId = Guid.NewGuid(),
                ProjectId = ticket.ProjectId,
                ProblemSequence = problemSequence,

                // 问题基本信息（从工单提取）
                ProblemCategory = MapDomainToCategory(ticket.Domain),
                ProblemDescription = $"{ticket.SymptomTitle}\n{ticket.SymptomDetail}".Trim(),
                Priority = ticket.Priority,

                // 时间信息
                FoundDate = ticket.CreatedAt,
                CompletedDate = ticket.ClosedAt,
                ProcessingDays = ticket.ClosedAt.HasValue
                    ? (int)(ticket.ClosedAt.Value - ticket.CreatedAt).TotalDays
                    : null,

                // 责任信息（从归因信息提取）
                PrimaryDepartment = ticket.ResponsibilityTeam ?? "未归因",
                PrimaryResponsible = ticket.AttributedBy != null
                    ? await GetUserNameAsync(ticket.AttributedBy.Value)
                    : "未指定",
                PrimaryResponsibleId = ticket.AttributedBy,

                // 处理信息
                Status = "已完成",
                Solution = solution?.Title,
                SolutionDetails = solution?.Description,

                // 验证信息
                VerificationStatus = solution != null ? "验证通过" : "未验证",

                // 关联信息
                RelatedTicketId = ticketId,
                RelatedTicketNo = ticket.TicketNo,
                IsRepeatProblem = isRepeat,
                RelatedHistoryProblemId = relatedProblemId,

                // 审计字段
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = ticket.ClosedBy
            };

            _dbContext.Set<FieldProblem>().Add(fieldProblem);

            // 如果有根本原因分析，关联它
            if (rootCauseAnalysis != null)
            {
                rootCauseAnalysis.ProblemId = fieldProblem.ProblemId;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully generated FieldProblem {ProblemId} from ticket {TicketId}, IsRepeat={IsRepeat}",
                fieldProblem.ProblemId, ticketId, isRepeat);

            return new AutoGenerateProblemResult
            {
                Success = true,
                ProblemId = fieldProblem.ProblemId,
                Message = isRepeat ? $"检测到重复问题（相似度 {similarityScore:P0}）" : "成功生成问题记录",
                IsRepeatProblem = isRepeat,
                RelatedHistoryProblemId = relatedProblemId,
                SimilarityScore = similarityScore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate FieldProblem from ticket {TicketId}", ticketId);
            return new AutoGenerateProblemResult
            {
                Success = false,
                Message = $"生成失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 检测是否为重复问题
    /// </summary>
    public async Task<(bool IsRepeat, Guid? RelatedProblemId, double SimilarityScore)> DetectRepeatProblemAsync(
        Guid ticketId,
        double similarityThreshold = 0.7)
    {
        _logger.LogInformation("Detecting repeat problem for ticket {TicketId}", ticketId);

        var ticket = await _dbContext.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            return (false, null, 0);
        }

        // 查询同项目的历史问题（最近90天内）
        var historicalProblems = await _dbContext.Set<FieldProblem>()
            .AsNoTracking()
            .Include(p => p.Project)
            .Where(p => p.ProjectId == ticket.ProjectId)
            .Where(p => p.RelatedTicketId != ticketId) // 排除自己
            .Where(p => p.FoundDate >= DateTime.UtcNow.AddDays(-90))
            .ToListAsync();

        if (!historicalProblems.Any())
        {
            return (false, null, 0);
        }

        // 计算与每个历史问题的相似度
        var similarities = new List<(Guid ProblemId, double Score)>();

        foreach (var problem in historicalProblems)
        {
            if (problem.RelatedTicketId.HasValue)
            {
                var similarity = await _recommendationService.CalculateTicketSimilarityAsync(
                    ticketId,
                    problem.RelatedTicketId.Value);

                similarities.Add((problem.ProblemId, similarity));
            }
        }

        // 找到最相似的问题
        var mostSimilar = similarities
            .OrderByDescending(s => s.Score)
            .FirstOrDefault();

        if (mostSimilar.Score >= similarityThreshold)
        {
            _logger.LogInformation(
                "Detected repeat problem: {ProblemId} with similarity {Score:P0}",
                mostSimilar.ProblemId, mostSimilar.Score);

            return (true, mostSimilar.ProblemId, mostSimilar.Score);
        }

        return (false, null, mostSimilar.Score);
    }

    /// <summary>
    /// 获取问题统计信息
    /// </summary>
    public async Task<ProblemStatisticsResponse> GetStatisticsAsync(ProblemStatisticsRequest request)
    {
        var query = _dbContext.Set<FieldProblem>().AsNoTracking();

        // 应用过滤条件
        if (request.StartDate.HasValue)
        {
            query = query.Where(p => p.FoundDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(p => p.FoundDate <= request.EndDate.Value);
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(p => p.ProjectId == request.ProjectId.Value);
        }

        if (!string.IsNullOrEmpty(request.ProblemCategory))
        {
            query = query.Where(p => p.ProblemCategory == request.ProblemCategory);
        }

        var problems = await query.ToListAsync();

        var totalCount = problems.Count;
        var resolvedCount = problems.Count(p => p.Status == "已完成");
        var repeatCount = problems.Count(p => p.IsRepeatProblem);
        var repeatRate = totalCount > 0 ? (double)repeatCount / totalCount : 0;

        var avgProcessingDays = problems
            .Where(p => p.ProcessingDays.HasValue)
            .Select(p => p.ProcessingDays!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // 热点分析
        var hotspots = problems
            .GroupBy(p => p.ProblemCategory)
            .Select(g => new ProblemHotspotDto
            {
                ProblemCategory = g.Key,
                Count = g.Count(),
                Percentage = totalCount > 0 ? (double)g.Count() / totalCount : 0,
                TopSymptoms = g.Select(p => p.ProblemDescription)
                    .Take(3)
                    .ToList(),
                AverageProcessingDays = g.Where(p => p.ProcessingDays.HasValue)
                    .Select(p => p.ProcessingDays!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                RepeatCount = g.Count(p => p.IsRepeatProblem)
            })
            .OrderByDescending(h => h.Count)
            .Take(request.TopN)
            .ToList();

        // 趋势分析（按天）
        var trends = problems
            .GroupBy(p => p.FoundDate.Date)
            .Select(g => new ProblemTrendDto
            {
                Date = g.Key,
                TotalCount = g.Count(),
                NewCount = g.Count(p => !p.IsRepeatProblem),
                ResolvedCount = g.Count(p => p.Status == "已完成"),
                RepeatCount = g.Count(p => p.IsRepeatProblem),
                AverageProcessingDays = g.Where(p => p.ProcessingDays.HasValue)
                    .Select(p => p.ProcessingDays!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderBy(t => t.Date)
            .ToList();

        return new ProblemStatisticsResponse
        {
            TotalCount = totalCount,
            ResolvedCount = resolvedCount,
            RepeatProblemCount = repeatCount,
            RepeatRate = repeatRate,
            AverageProcessingDays = avgProcessingDays,
            Hotspots = hotspots,
            Trends = trends
        };
    }

    /// <summary>
    /// 获取问题热点
    /// </summary>
    public async Task<List<ProblemHotspotDto>> GetHotspotsAsync(
        Guid? projectId = null,
        int topN = 10,
        int days = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var query = _dbContext.Set<FieldProblem>()
            .AsNoTracking()
            .Where(p => p.FoundDate >= cutoffDate);

        if (projectId.HasValue)
        {
            query = query.Where(p => p.ProjectId == projectId.Value);
        }

        var problems = await query.ToListAsync();
        var totalCount = problems.Count;

        var hotspots = problems
            .GroupBy(p => p.ProblemCategory)
            .Select(g => new ProblemHotspotDto
            {
                ProblemCategory = g.Key,
                Count = g.Count(),
                Percentage = totalCount > 0 ? (double)g.Count() / totalCount : 0,
                TopSymptoms = g.Select(p => p.ProblemDescription)
                    .Distinct()
                    .Take(5)
                    .ToList(),
                AverageProcessingDays = g.Where(p => p.ProcessingDays.HasValue)
                    .Select(p => p.ProcessingDays!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                RepeatCount = g.Count(p => p.IsRepeatProblem)
            })
            .OrderByDescending(h => h.Count)
            .Take(topN)
            .ToList();

        return hotspots;
    }

    #region 辅助方法

    /// <summary>
    /// 生成问题序号
    /// </summary>
    private async Task<int> GenerateProblemSequenceAsync(Guid projectId)
    {
        var maxSequence = await _dbContext.Set<FieldProblem>()
            .Where(p => p.ProjectId == projectId)
            .MaxAsync(p => (int?)p.ProblemSequence) ?? 0;

        return maxSequence + 1;
    }

    /// <summary>
    /// 将Domain映射到问题分类
    /// </summary>
    private string MapDomainToCategory(char domain)
    {
        return domain switch
        {
            'A' => "机械/动作",
            'B' => "电气/IO",
            'C' => "PLC/程序",
            'D' => "测试/判定",
            'E' => "系统/环境",
            _ => "其他"
        };
    }

    /// <summary>
    /// 获取用户名
    /// </summary>
    private async Task<string> GetUserNameAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Name ?? "未知";
    }

    #endregion
}
