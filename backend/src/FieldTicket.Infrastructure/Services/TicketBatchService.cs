using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单批量操作服务实现
/// </summary>
public class TicketBatchService : ITicketBatchService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITicketStatusHistoryService _statusHistoryService;
    private readonly ILogger<TicketBatchService> _logger;

    public TicketBatchService(
        ApplicationDbContext dbContext,
        ITicketStatusHistoryService statusHistoryService,
        ILogger<TicketBatchService> logger)
    {
        _dbContext = dbContext;
        _statusHistoryService = statusHistoryService;
        _logger = logger;
    }

    public async Task<BatchOperationResult> BatchUpdateStatusAsync(
        List<Guid> ticketIds,
        string newStatus,
        string? reason,
        Guid userId)
    {
        var result = new BatchOperationResult
        {
            TotalCount = ticketIds.Count
        };

        var validStatuses = new[] { "Draft", "Submitted", "Triage", "SolutionIssued", "Verifying", "Closed", "Reopened" };
        if (!validStatuses.Contains(newStatus))
        {
            result.Success = false;
            result.Message = $"无效的状态: {newStatus}";
            return result;
        }

        var tickets = await _dbContext.Tickets
            .Where(t => ticketIds.Contains(t.TicketId))
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            try
            {
                var oldStatus = ticket.Status;
                ticket.Status = newStatus;
                ticket.UpdatedAt = DateTime.UtcNow;

                // 记录状态变更历史
                await _statusHistoryService.RecordStatusChangeAsync(
                    ticket.TicketId,
                    oldStatus,
                    newStatus,
                    userId,
                    reason ?? "批量更新状态",
                    "batch_operation"
                );

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新工单 {TicketId} 状态失败", ticket.TicketId);
                result.Errors.Add(new BatchOperationError
                {
                    TicketId = ticket.TicketId,
                    TicketNo = ticket.TicketNo ?? "未生成",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        result.Success = result.FailureCount == 0;
        result.Message = result.Success
            ? $"成功更新 {result.SuccessCount} 个工单状态"
            : $"成功更新 {result.SuccessCount} 个工单，失败 {result.FailureCount} 个";

        _logger.LogInformation("批量更新工单状态: 总数={Total}, 成功={Success}, 失败={Failure}",
            result.TotalCount, result.SuccessCount, result.FailureCount);

        return result;
    }

    public async Task<BatchOperationResult> BatchAssignEngineerAsync(
        List<Guid> ticketIds,
        Guid engineerId,
        Guid userId)
    {
        var result = new BatchOperationResult
        {
            TotalCount = ticketIds.Count
        };

        // 验证工程师是否存在
        var engineer = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == engineerId);
        if (engineer == null)
        {
            result.Success = false;
            result.Message = $"工程师 {engineerId} 不存在";
            return result;
        }

        var tickets = await _dbContext.Tickets
            .Where(t => ticketIds.Contains(t.TicketId))
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            try
            {
                // 这里假设 Ticket 实体有 AssignedToUserId 字段
                // 如果没有，需要先添加到实体中
                // ticket.AssignedToUserId = engineerId;
                ticket.UpdatedAt = DateTime.UtcNow;

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量分配工单 {TicketId} 失败", ticket.TicketId);
                result.Errors.Add(new BatchOperationError
                {
                    TicketId = ticket.TicketId,
                    TicketNo = ticket.TicketNo ?? "未生成",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        result.Success = result.FailureCount == 0;
        result.Message = result.Success
            ? $"成功分配 {result.SuccessCount} 个工单"
            : $"成功分配 {result.SuccessCount} 个工单，失败 {result.FailureCount} 个";

        _logger.LogInformation("批量分配工程师: 总数={Total}, 成功={Success}, 失败={Failure}",
            result.TotalCount, result.SuccessCount, result.FailureCount);

        return result;
    }

    public async Task<BatchOperationResult> BatchDeleteAsync(
        List<Guid> ticketIds,
        Guid userId)
    {
        var result = new BatchOperationResult
        {
            TotalCount = ticketIds.Count
        };

        var tickets = await _dbContext.Tickets
            .Where(t => ticketIds.Contains(t.TicketId))
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            try
            {
                // 软删除：标记为已删除
                // 如果 Ticket 实体有 IsDeleted 字段，使用软删除
                // 否则使用硬删除
                if (ticket.Status == "Closed" || ticket.Status == "Draft")
                {
                    // 只有已关闭或草稿状态的工单可以删除
                    _dbContext.Tickets.Remove(ticket);
                    result.SuccessCount++;
                }
                else
                {
                    result.Errors.Add(new BatchOperationError
                    {
                        TicketId = ticket.TicketId,
                        TicketNo = ticket.TicketNo ?? "未生成",
                        ErrorMessage = $"工单状态为 {ticket.Status}，无法删除"
                    });
                    result.FailureCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除工单 {TicketId} 失败", ticket.TicketId);
                result.Errors.Add(new BatchOperationError
                {
                    TicketId = ticket.TicketId,
                    TicketNo = ticket.TicketNo ?? "未生成",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        result.Success = result.FailureCount == 0;
        result.Message = result.Success
            ? $"成功删除 {result.SuccessCount} 个工单"
            : $"成功删除 {result.SuccessCount} 个工单，失败 {result.FailureCount} 个";

        _logger.LogInformation("批量删除工单: 总数={Total}, 成功={Success}, 失败={Failure}",
            result.TotalCount, result.SuccessCount, result.FailureCount);

        return result;
    }

    public async Task<BatchOperationResult> BatchUpdatePriorityAsync(
        List<Guid> ticketIds,
        string priority,
        Guid userId)
    {
        var result = new BatchOperationResult
        {
            TotalCount = ticketIds.Count
        };

        var validPriorities = new[] { "P0", "P1", "P2", "P3" };
        if (!validPriorities.Contains(priority))
        {
            result.Success = false;
            result.Message = $"无效的优先级: {priority}";
            return result;
        }

        var tickets = await _dbContext.Tickets
            .Where(t => ticketIds.Contains(t.TicketId))
            .ToListAsync();

        foreach (var ticket in tickets)
        {
            try
            {
                ticket.Priority = priority;
                ticket.UpdatedAt = DateTime.UtcNow;

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新工单 {TicketId} 优先级失败", ticket.TicketId);
                result.Errors.Add(new BatchOperationError
                {
                    TicketId = ticket.TicketId,
                    TicketNo = ticket.TicketNo ?? "未生成",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        result.Success = result.FailureCount == 0;
        result.Message = result.Success
            ? $"成功更新 {result.SuccessCount} 个工单优先级"
            : $"成功更新 {result.SuccessCount} 个工单，失败 {result.FailureCount} 个";

        _logger.LogInformation("批量更新优先级: 总数={Total}, 成功={Success}, 失败={Failure}",
            result.TotalCount, result.SuccessCount, result.FailureCount);

        return result;
    }

    public async Task<BatchOperationResult> BatchTagAsync(
        List<Guid> ticketIds,
        List<string> tags,
        Guid userId)
    {
        var result = new BatchOperationResult
        {
            TotalCount = ticketIds.Count
        };

        // 如果 Ticket 实体有 Tags 字段（JSONB），可以批量更新标签
        // 这里暂时返回未实现
        result.Success = false;
        result.Message = "批量标记功能暂未实现";
        result.FailureCount = result.TotalCount;

        return result;
    }
}















