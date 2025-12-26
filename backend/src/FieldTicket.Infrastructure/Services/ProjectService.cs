using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 项目服务实现
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ApplicationDbContext dbContext,
        ILogger<ProjectService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(List<ProjectDto> Items, int Total)> GetProjectsAsync(
        ProjectQueryFilter filter,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbContext.Projects.AsQueryable();

        // 应用过滤器
        if (!string.IsNullOrEmpty(filter.ProjectNo))
        {
            query = query.Where(p => p.ProjectNo.Contains(filter.ProjectNo));
        }

        if (!string.IsNullOrEmpty(filter.ProjectName))
        {
            query = query.Where(p => p.ProjectName.Contains(filter.ProjectName));
        }

        if (!string.IsNullOrEmpty(filter.CustomerName))
        {
            query = query.Where(p => p.CustomerName != null && p.CustomerName.Contains(filter.CustomerName));
        }

        if (!string.IsNullOrEmpty(filter.DeviceType))
        {
            query = query.Where(p => p.DeviceType == filter.DeviceType);
        }

        if (!string.IsNullOrEmpty(filter.ProjectStatus))
        {
            query = query.Where(p => p.ProjectStatus == filter.ProjectStatus);
        }

        if (filter.OrderDateFrom.HasValue)
        {
            query = query.Where(p => p.OrderDate >= filter.OrderDateFrom.Value);
        }

        if (filter.OrderDateTo.HasValue)
        {
            query = query.Where(p => p.OrderDate <= filter.OrderDateTo.Value);
        }

        var total = await query.CountAsync();

        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 获取每个项目的问题数量
        var projectIds = projects.Select(p => p.ProjectId).ToList();
        var problemCounts = await _dbContext.FieldProblems
            .Where(p => projectIds.Contains(p.ProjectId))
            .GroupBy(p => p.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        var items = projects.Select(p => new ProjectDto
        {
            ProjectId = p.ProjectId,
            ProjectNo = p.ProjectNo,
            ProjectName = p.ProjectName,
            CustomerId = p.CustomerId,
            CustomerName = p.CustomerName,
            DeviceType = p.DeviceType,
            IndustryType = p.IndustryType,
            SalesAmount = p.SalesAmount,
            Quantity = p.Quantity,
            OrderDate = p.OrderDate,
            RequiredDeliveryDate = p.RequiredDeliveryDate,
            ActualDeliveryDate = p.ActualDeliveryDate,
            DeliveryDelayDays = p.DeliveryDelayDays,
            ProjectStatus = p.ProjectStatus,
            ProjectManagerName = p.ProjectManagerName,
            ProblemCount = problemCounts.GetValueOrDefault(p.ProjectId, 0),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return (items, total);
    }

    public async Task<ProjectDetailDto?> GetProjectAsync(Guid projectId)
    {
        var project = await _dbContext.Projects
            .Include(p => p.Problems)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
        {
            return null;
        }

        var problems = project.Problems
            .OrderBy(p => p.ProblemSequence)
            .Select(p => MapToProblemDto(p, project))
            .ToList();

        return new ProjectDetailDto
        {
            ProjectId = project.ProjectId,
            ProjectNo = project.ProjectNo,
            ProjectName = project.ProjectName,
            CustomerId = project.CustomerId,
            CustomerName = project.CustomerName,
            DeviceType = project.DeviceType,
            IndustryType = project.IndustryType,
            SalesAmount = project.SalesAmount,
            Quantity = project.Quantity,
            OrderDate = project.OrderDate,
            RequiredDeliveryDate = project.RequiredDeliveryDate,
            ActualDeliveryDate = project.ActualDeliveryDate,
            DeliveryDelayDays = project.DeliveryDelayDays,
            ProjectStatus = project.ProjectStatus,
            ProjectManagerName = project.ProjectManagerName,
            ProblemCount = project.Problems.Count,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Problems = problems
        };
    }

    public async Task<List<FieldProblemDto>> GetProjectProblemsAsync(Guid projectId)
    {
        var problems = await _dbContext.FieldProblems
            .Include(p => p.Project)
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.ProblemSequence)
            .ToListAsync();

        return problems.Select(p => MapToProblemDto(p, p.Project)).ToList();
    }

    public async Task<FieldProblemDetailDto?> GetProblemAsync(Guid problemId)
    {
        var problem = await _dbContext.FieldProblems
            .Include(p => p.Project)
            .Include(p => p.RootCauseAnalysis)
            .FirstOrDefaultAsync(p => p.ProblemId == problemId);

        if (problem == null)
        {
            return null;
        }

        var dto = new FieldProblemDetailDto
        {
            ProblemId = problem.ProblemId,
            ProjectId = problem.ProjectId,
            ProjectNo = problem.Project.ProjectNo,
            ProjectName = problem.Project.ProjectName,
            ProblemSequence = problem.ProblemSequence,
            ProblemCategory = problem.ProblemCategory,
            ProblemDescription = problem.ProblemDescription,
            Priority = problem.Priority,
            FoundDate = problem.FoundDate,
            CompletedDate = problem.CompletedDate,
            ProcessingDays = problem.ProcessingDays,
            PrimaryDepartment = problem.PrimaryDepartment,
            PrimaryResponsible = problem.PrimaryResponsible,
            CollaboratingDepartment = problem.CollaboratingDepartment,
            CollaboratingPerson = problem.CollaboratingPerson,
            Status = problem.Status,
            VerificationStatus = problem.VerificationStatus,
            SatisfactionScore = problem.SatisfactionScore,
            IsRepeatProblem = problem.IsRepeatProblem,
            CreatedAt = problem.CreatedAt,
            Solution = problem.Solution,
            SolutionDetails = problem.SolutionDetails,
            CustomerFeedback = problem.CustomerFeedback,
            VerifiedAt = problem.VerifiedAt,
            VerifiedBy = problem.VerifiedBy,
            RelatedTicketNo = problem.RelatedTicketNo,
            KnowledgeBaseId = problem.KnowledgeBaseId,
            Notes = problem.Notes
        };

        if (problem.RootCauseAnalysis != null)
        {
            dto.RootCauseAnalysis = new RootCauseAnalysisDto
            {
                AnalysisId = problem.RootCauseAnalysis.AnalysisId,
                ProblemId = problem.RootCauseAnalysis.ProblemId,
                Why1 = problem.RootCauseAnalysis.Why1,
                Why2 = problem.RootCauseAnalysis.Why2,
                Why3 = problem.RootCauseAnalysis.Why3,
                Why4 = problem.RootCauseAnalysis.Why4,
                Why5 = problem.RootCauseAnalysis.Why5,
                RootCause = problem.RootCauseAnalysis.RootCause,
                RootCauseCategory = problem.RootCauseAnalysis.RootCauseCategory,
                PreventiveMeasures = problem.RootCauseAnalysis.PreventiveMeasures,
                VerificationMethod = problem.RootCauseAnalysis.VerificationMethod,
                AnalyzedAt = problem.RootCauseAnalysis.AnalyzedAt,
                CreatedAt = problem.RootCauseAnalysis.CreatedAt,
                UpdatedAt = problem.RootCauseAnalysis.UpdatedAt
            };
        }

        return dto;
    }

    public async Task<ProblemStatisticsDto> GetProblemStatisticsAsync(ProblemStatisticsFilter filter)
    {
        var query = _dbContext.FieldProblems
            .Include(p => p.Project)
            .AsQueryable();

        // 应用过滤器
        if (!string.IsNullOrEmpty(filter.ProblemCategory))
        {
            query = query.Where(p => p.ProblemCategory == filter.ProblemCategory);
        }

        if (!string.IsNullOrEmpty(filter.PrimaryDepartment))
        {
            query = query.Where(p => p.PrimaryDepartment == filter.PrimaryDepartment);
        }

        if (!string.IsNullOrEmpty(filter.CustomerName))
        {
            query = query.Where(p => p.Project.CustomerName != null && p.Project.CustomerName.Contains(filter.CustomerName));
        }

        if (filter.FoundDateFrom.HasValue)
        {
            query = query.Where(p => p.FoundDate >= filter.FoundDateFrom.Value);
        }

        if (filter.FoundDateTo.HasValue)
        {
            query = query.Where(p => p.FoundDate <= filter.FoundDateTo.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(p => p.Status == filter.Status);
        }

        var problems = await query.ToListAsync();

        var statistics = new ProblemStatisticsDto
        {
            TotalProblems = problems.Count,
            ClosedProblems = problems.Count(p => p.Status == "已关闭"),
            OpenProblems = problems.Count(p => p.Status != "已关闭"),
            AverageProcessingDays = problems
                .Where(p => p.ProcessingDays.HasValue)
                .Select(p => (decimal)p.ProcessingDays!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            AverageSatisfactionScore = problems
                .Where(p => p.SatisfactionScore.HasValue)
                .Select(p => (decimal)p.SatisfactionScore!.Value)
                .DefaultIfEmpty(0)
                .Average()
        };

        // 按分类统计
        var categoryGroups = problems
            .GroupBy(p => p.ProblemCategory)
            .Select(g => new CategoryStatistics
            {
                Category = g.Key,
                Count = g.Count(),
                Percentage = problems.Count > 0 ? (decimal)g.Count() / problems.Count * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();
        statistics.CategoryStatistics = categoryGroups;

        // 按部门统计
        var departmentGroups = problems
            .GroupBy(p => p.PrimaryDepartment)
            .Select(g => new DepartmentStatistics
            {
                Department = g.Key,
                Count = g.Count(),
                AverageProcessingDays = g
                    .Where(p => p.ProcessingDays.HasValue)
                    .Select(p => (decimal)p.ProcessingDays!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                AverageSatisfactionScore = g
                    .Where(p => p.SatisfactionScore.HasValue)
                    .Select(p => (decimal)p.SatisfactionScore!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderByDescending(s => s.Count)
            .ToList();
        statistics.DepartmentStatistics = departmentGroups;

        // 按客户统计
        var customerGroups = problems
            .Where(p => !string.IsNullOrEmpty(p.Project.CustomerName))
            .GroupBy(p => p.Project.CustomerName!)
            .Select(g => new CustomerStatistics
            {
                CustomerName = g.Key,
                Count = g.Count(),
                AverageSatisfactionScore = g
                    .Where(p => p.SatisfactionScore.HasValue)
                    .Select(p => (decimal)p.SatisfactionScore!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderByDescending(s => s.Count)
            .ToList();
        statistics.CustomerStatistics = customerGroups;

        return statistics;
    }

    private FieldProblemDto MapToProblemDto(FieldProblem problem, Project project)
    {
        return new FieldProblemDto
        {
            ProblemId = problem.ProblemId,
            ProjectId = problem.ProjectId,
            ProjectNo = project.ProjectNo,
            ProjectName = project.ProjectName,
            ProblemSequence = problem.ProblemSequence,
            ProblemCategory = problem.ProblemCategory,
            ProblemDescription = problem.ProblemDescription,
            Priority = problem.Priority,
            FoundDate = problem.FoundDate,
            CompletedDate = problem.CompletedDate,
            ProcessingDays = problem.ProcessingDays,
            PrimaryDepartment = problem.PrimaryDepartment,
            PrimaryResponsible = problem.PrimaryResponsible,
            CollaboratingDepartment = problem.CollaboratingDepartment,
            CollaboratingPerson = problem.CollaboratingPerson,
            Status = problem.Status,
            VerificationStatus = problem.VerificationStatus,
            SatisfactionScore = problem.SatisfactionScore,
            IsRepeatProblem = problem.IsRepeatProblem,
            CreatedAt = problem.CreatedAt
        };
    }
}



