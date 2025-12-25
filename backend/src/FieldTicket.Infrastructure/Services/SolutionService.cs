using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 解决方案服务实现
/// </summary>
public class SolutionService : ISolutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SolutionNumberService _solutionNumberService;
    private readonly ILogger<SolutionService> _logger;
    private readonly IWeComNotificationService? _notificationService;
    private readonly INotificationRuleService? _notificationRuleService;

    public SolutionService(
        ApplicationDbContext dbContext,
        SolutionNumberService solutionNumberService,
        ILogger<SolutionService> logger,
        IWeComNotificationService? notificationService = null,
        INotificationRuleService? notificationRuleService = null)
    {
        _dbContext = dbContext;
        _solutionNumberService = solutionNumberService;
        _logger = logger;
        _notificationService = notificationService;
        _notificationRuleService = notificationRuleService;
    }

    public async Task<SolutionDto> CreateSolutionAsync(Guid ticketId, CreateSolutionRequest request, Guid userId)
    {
        // 验证工单存在
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 验证工单状态（必须是 Triage 或 SolutionIssued）
        if (ticket.Status != "Triage" && ticket.Status != "SolutionIssued")
        {
            throw new InvalidOperationException($"工单状态必须是 Triage 或 SolutionIssued，当前状态：{ticket.Status}");
        }

        // 创建解决方案草稿
        var solution = new Solution
        {
            SolutionId = Guid.NewGuid(),
            SolutionCode = string.Empty, // 发布时生成
            TicketId = ticketId,
            Title = request.Title,
            Description = request.Description,
            SolutionType = request.SolutionType,
            ReleaseType = request.ReleaseType,
            RequiredSwVersion = request.RequiredSwVersion,
            RequiredPlcVersion = request.RequiredPlcVersion,
            RequiredParamVersion = request.RequiredParamVersion,
            NewSwVersion = request.NewSwVersion,
            NewPlcVersion = request.NewPlcVersion,
            NewParamVersion = request.NewParamVersion,
            ChangeDetailJson = request.ChangeDetailJson,
            VerificationChecklistJson = request.VerificationChecklistJson,
            ImplementationSteps = request.ImplementationSteps,
            EstimatedImplementationTime = request.EstimatedImplementationTime,
            RiskLevel = request.RiskLevel,
            RiskDescription = request.RiskDescription,
            RollbackPossible = request.RollbackPossible,
            RollbackProcedure = request.RollbackProcedure,
            Status = "Draft",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Solutions.Add(solution);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("为工单 {TicketId} 创建解决方案草稿 {SolutionId}", ticketId, solution.SolutionId);

        return await MapToDtoAsync(solution);
    }

    public async Task<SolutionDto> UpdateSolutionAsync(Guid solutionId, UpdateSolutionRequest request, Guid userId)
    {
        var solution = await _dbContext.Solutions
            .FirstOrDefaultAsync(s => s.SolutionId == solutionId);

        if (solution == null)
        {
            throw new KeyNotFoundException($"解决方案 {solutionId} 不存在");
        }

        // 只能更新草稿状态的解决方案
        if (solution.Status != "Draft")
        {
            throw new InvalidOperationException($"只能更新草稿状态的解决方案，当前状态：{solution.Status}");
        }

        // 更新字段
        if (!string.IsNullOrEmpty(request.Title))
            solution.Title = request.Title;
        
        if (request.Description != null)
            solution.Description = request.Description;
        
        if (!string.IsNullOrEmpty(request.SolutionType))
            solution.SolutionType = request.SolutionType;
        
        if (!string.IsNullOrEmpty(request.ReleaseType))
            solution.ReleaseType = request.ReleaseType;

        if (request.RequiredSwVersion != null)
            solution.RequiredSwVersion = request.RequiredSwVersion;
        
        if (request.RequiredPlcVersion != null)
            solution.RequiredPlcVersion = request.RequiredPlcVersion;
        
        if (request.RequiredParamVersion != null)
            solution.RequiredParamVersion = request.RequiredParamVersion;

        if (request.NewSwVersion != null)
            solution.NewSwVersion = request.NewSwVersion;
        
        if (request.NewPlcVersion != null)
            solution.NewPlcVersion = request.NewPlcVersion;
        
        if (request.NewParamVersion != null)
            solution.NewParamVersion = request.NewParamVersion;

        if (request.ChangeDetailJson != null)
            solution.ChangeDetailJson = request.ChangeDetailJson;
        
        if (request.VerificationChecklistJson != null)
            solution.VerificationChecklistJson = request.VerificationChecklistJson;

        if (request.ImplementationSteps != null)
            solution.ImplementationSteps = request.ImplementationSteps;
        
        if (request.EstimatedImplementationTime.HasValue)
            solution.EstimatedImplementationTime = request.EstimatedImplementationTime;

        if (!string.IsNullOrEmpty(request.RiskLevel))
            solution.RiskLevel = request.RiskLevel;
        
        if (request.RiskDescription != null)
            solution.RiskDescription = request.RiskDescription;

        if (request.RollbackPossible.HasValue)
            solution.RollbackPossible = request.RollbackPossible.Value;
        
        if (request.RollbackProcedure != null)
            solution.RollbackProcedure = request.RollbackProcedure;

        solution.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("更新解决方案 {SolutionId}", solutionId);

        return await MapToDtoAsync(solution);
    }

    public async Task<SolutionDto> PublishSolutionAsync(Guid solutionId, Guid userId)
    {
        var solution = await _dbContext.Solutions
            .FirstOrDefaultAsync(s => s.SolutionId == solutionId);

        if (solution == null)
        {
            throw new KeyNotFoundException($"解决方案 {solutionId} 不存在");
        }

        // 只能发布草稿状态的解决方案
        if (solution.Status != "Draft")
        {
            throw new InvalidOperationException($"只能发布草稿状态的解决方案，当前状态：{solution.Status}");
        }

        // 硬规则HR-001：必须关联判断卡才能发布解决方案
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == solution.TicketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {solution.TicketId} 不存在");
        }

        if (string.IsNullOrEmpty(ticket.CurrentJcCode))
        {
            throw new InvalidOperationException("工单必须关联判断卡才能发布解决方案（硬规则HR-001）");
        }

        // 生成解决方案编号
        solution.SolutionCode = await _solutionNumberService.GenerateSolutionNumberAsync();

        // 更新解决方案状态
        solution.Status = "Published";
        solution.PublishedAt = DateTime.UtcNow;
        solution.PublishedBy = userId;
        solution.UpdatedAt = DateTime.UtcNow;

        // 更新工单状态
        ticket.Status = "SolutionIssued";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("发布解决方案 {SolutionId}，编号 {SolutionCode}，工单 {TicketId} 状态更新为 SolutionIssued", 
            solutionId, solution.SolutionCode, solution.TicketId);

        // 发送企业微信通知（异步，不阻塞主流程）
        // 优先使用通知规则系统，如果没有规则则使用默认通知
        _ = Task.Run(async () =>
        {
            try
            {
                // 优先使用通知规则系统
                if (_notificationRuleService != null)
                {
                    var notificationResult = await _notificationRuleService.ExecuteNotificationAsync(
                        solution.TicketId,
                        "solution_published",
                        new Dictionary<string, object>
                        {
                            ["solution"] = solution,
                            ["ticket"] = ticket
                        });

                    if (notificationResult.SentCount > 0)
                    {
                        _logger.LogInformation("通过通知规则发送通知成功，解决方案 {SolutionId}，发送数：{SentCount}",
                            solutionId, notificationResult.SentCount);
                        return;
                    }
                }

                // 回退到默认通知（如果没有配置通知规则）
                if (_notificationService != null)
                {
                    await _notificationService.NotifySolutionPublishedAsync(solutionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送解决方案发布通知失败，解决方案ID：{SolutionId}", solutionId);
            }
        });

        return await MapToDtoAsync(solution);
    }

    public async Task<SolutionDto?> GetSolutionAsync(Guid solutionId)
    {
        var solution = await _dbContext.Solutions
            .FirstOrDefaultAsync(s => s.SolutionId == solutionId);

        if (solution == null)
        {
            return null;
        }

        return await MapToDtoAsync(solution);
    }

    public async Task<List<SolutionDto>> GetTicketSolutionsAsync(Guid ticketId)
    {
        var solutions = await _dbContext.Solutions
            .Where(s => s.TicketId == ticketId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var result = new List<SolutionDto>();
        foreach (var solution in solutions)
        {
            result.Add(await MapToDtoAsync(solution));
        }

        return result;
    }

    private async Task<SolutionDto> MapToDtoAsync(Solution solution)
    {
        return new SolutionDto
        {
            SolutionId = solution.SolutionId,
            SolutionCode = solution.SolutionCode,
            TicketId = solution.TicketId,
            Title = solution.Title,
            Description = solution.Description,
            SolutionType = solution.SolutionType,
            ReleaseType = solution.ReleaseType,
            RequiredSwVersion = solution.RequiredSwVersion,
            RequiredPlcVersion = solution.RequiredPlcVersion,
            RequiredParamVersion = solution.RequiredParamVersion,
            NewSwVersion = solution.NewSwVersion,
            NewPlcVersion = solution.NewPlcVersion,
            NewParamVersion = solution.NewParamVersion,
            ChangeDetailJson = solution.ChangeDetailJson,
            VerificationChecklistJson = solution.VerificationChecklistJson,
            ImplementationSteps = solution.ImplementationSteps,
            EstimatedImplementationTime = solution.EstimatedImplementationTime,
            RiskLevel = solution.RiskLevel,
            RiskDescription = solution.RiskDescription,
            RollbackPossible = solution.RollbackPossible,
            RollbackProcedure = solution.RollbackProcedure,
            Status = solution.Status,
            CreatedBy = solution.CreatedBy,
            CreatedAt = solution.CreatedAt,
            UpdatedAt = solution.UpdatedAt,
            PublishedAt = solution.PublishedAt,
            PublishedBy = solution.PublishedBy
        };
    }
}

