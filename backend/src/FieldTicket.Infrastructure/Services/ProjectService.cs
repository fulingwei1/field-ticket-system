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

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(p => p.CustomerId == filter.CustomerId.Value);
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

        var relatedPersons = await GetProjectRelatedPersonsAsync(projectId);

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
            Problems = problems,
            RelatedPersons = relatedPersons
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

    public async Task<List<ProjectRelatedPersonDto>> GetProjectRelatedPersonsAsync(Guid projectId)
    {
        var persons = new Dictionary<Guid, ProjectRelatedPersonDto>();

        // 1. 项目经理
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
        
        if (project != null && project.ProjectManagerId.HasValue)
        {
            var pm = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == project.ProjectManagerId.Value);
            
            if (pm != null)
            {
                persons[pm.Id] = new ProjectRelatedPersonDto
                {
                    UserId = pm.Id,
                    UserName = pm.Name,
                    Role = pm.Role,
                    Department = pm.DeptId,
                    Mobile = pm.Mobile,
                    RolesInProject = new List<string> { "项目经理" },
                    TicketCount = 0,
                    LastActivityAt = null
                };
            }
        }

        // 2. 创建工单的工程师
        var ticketCreators = await _dbContext.Tickets
            .Where(t => t.ProjectId == projectId)
            .GroupBy(t => t.CreatedByUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(t => t.CreatedAt) })
            .ToListAsync();

        foreach (var creator in ticketCreators)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == creator.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    persons[user.Id].RolesInProject.Add("创建工单");
                    persons[user.Id].TicketCount += creator.Count;
                    if (creator.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = creator.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "创建工单" },
                        TicketCount = creator.Count,
                        LastActivityAt = creator.LastActivity
                    };
                }
            }
        }

        // 3. 分诊人员
        var triagePersons = await _dbContext.TriageNotes
            .Where(tn => _dbContext.Tickets.Any(t => t.TicketId == tn.TicketId && t.ProjectId == projectId))
            .GroupBy(tn => tn.CreatedBy)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(tn => tn.CreatedAt) })
            .ToListAsync();

        foreach (var triage in triagePersons)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == triage.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    if (!persons[user.Id].RolesInProject.Contains("分诊"))
                    {
                        persons[user.Id].RolesInProject.Add("分诊");
                    }
                    persons[user.Id].TicketCount += triage.Count;
                    if (triage.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = triage.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "分诊" },
                        TicketCount = triage.Count,
                        LastActivityAt = triage.LastActivity
                    };
                }
            }
        }

        // 4. 分配处理工单的人员
        var assignedPersons = await _dbContext.Tickets
            .Where(t => t.ProjectId == projectId && t.AssignedTo.HasValue)
            .GroupBy(t => t.AssignedTo!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(t => t.UpdatedAt) })
            .ToListAsync();

        foreach (var assigned in assignedPersons)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == assigned.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    if (!persons[user.Id].RolesInProject.Contains("处理工单"))
                    {
                        persons[user.Id].RolesInProject.Add("处理工单");
                    }
                    if (assigned.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = assigned.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "处理工单" },
                        TicketCount = assigned.Count,
                        LastActivityAt = assigned.LastActivity
                    };
                }
            }
        }

        // 5. 提供解决方案的人员
        var solutionCreators = await _dbContext.Solutions
            .Where(s => _dbContext.Tickets.Any(t => t.TicketId == s.TicketId && t.ProjectId == projectId))
            .GroupBy(s => s.CreatedBy)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(s => s.CreatedAt) })
            .ToListAsync();

        foreach (var solution in solutionCreators)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == solution.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    if (!persons[user.Id].RolesInProject.Contains("解决方案"))
                    {
                        persons[user.Id].RolesInProject.Add("解决方案");
                    }
                    if (solution.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = solution.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "解决方案" },
                        TicketCount = solution.Count,
                        LastActivityAt = solution.LastActivity
                    };
                }
            }
        }

        // 6. 验证人员
        var verifiers = await _dbContext.Verifications
            .Where(v => _dbContext.Tickets.Any(t => t.TicketId == v.TicketId && t.ProjectId == projectId))
            .GroupBy(v => v.ExecutedBy)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(v => v.VerifiedAt) })
            .ToListAsync();

        foreach (var verifier in verifiers)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == verifier.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    if (!persons[user.Id].RolesInProject.Contains("验证"))
                    {
                        persons[user.Id].RolesInProject.Add("验证");
                    }
                    if (verifier.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = verifier.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "验证" },
                        TicketCount = verifier.Count,
                        LastActivityAt = verifier.LastActivity
                    };
                }
            }
        }

        // 7. 责任归属人员
        var attributedPersons = await _dbContext.Tickets
            .Where(t => t.ProjectId == projectId && t.AttributedBy.HasValue)
            .GroupBy(t => t.AttributedBy!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(t => t.AttributedAt!.Value) })
            .ToListAsync();

        foreach (var attributed in attributedPersons)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == attributed.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    if (!persons[user.Id].RolesInProject.Contains("责任归属"))
                    {
                        persons[user.Id].RolesInProject.Add("责任归属");
                    }
                    if (attributed.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = attributed.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "责任归属" },
                        TicketCount = attributed.Count,
                        LastActivityAt = attributed.LastActivity
                    };
                }
            }
        }

        // 8. 合并工单的人员
        var mergedPersons = await _dbContext.Tickets
            .Where(t => t.ProjectId == projectId && t.MergedBy.HasValue)
            .GroupBy(t => t.MergedBy!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastActivity = g.Max(t => t.MergedAt!.Value) })
            .ToListAsync();

        foreach (var merged in mergedPersons)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == merged.UserId);
            if (user != null)
            {
                if (persons.ContainsKey(user.Id))
                {
                    if (!persons[user.Id].RolesInProject.Contains("合并工单"))
                    {
                        persons[user.Id].RolesInProject.Add("合并工单");
                    }
                    if (merged.LastActivity > (persons[user.Id].LastActivityAt ?? DateTime.MinValue))
                    {
                        persons[user.Id].LastActivityAt = merged.LastActivity;
                    }
                }
                else
                {
                    persons[user.Id] = new ProjectRelatedPersonDto
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        Role = user.Role,
                        Department = user.DeptId,
                        Mobile = user.Mobile,
                        RolesInProject = new List<string> { "合并工单" },
                        TicketCount = merged.Count,
                        LastActivityAt = merged.LastActivity
                    };
                }
            }
        }

        return persons.Values
            .OrderByDescending(p => p.LastActivityAt ?? DateTime.MinValue)
            .ThenBy(p => p.UserName)
            .ToList();
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






