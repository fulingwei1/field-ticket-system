using System.Text.Json;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 操作日志服务
/// </summary>
public class OperationLogService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OperationLogService> _logger;

    public OperationLogService(
        ApplicationDbContext dbContext,
        ILogger<OperationLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 记录员工导入操作
    /// </summary>
    public async Task<Guid> LogEmployeeImportAsync(
        Guid operatorId,
        string operatorName,
        EmployeeBatchImportResult importResult,
        string fileName,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var log = new OperationLog
        {
            Id = Guid.NewGuid(),
            OperationType = "EmployeeImport",
            OperatorId = operatorId,
            OperatorName = operatorName,
            OperatedAt = DateTime.UtcNow,
            Description = $"批量导入员工，文件名：{fileName}",
            Result = DetermineResult(importResult.SuccessCount, importResult.FailedCount, importResult.TotalCount),
            TotalCount = importResult.TotalCount,
            SuccessCount = importResult.SuccessCount,
            FailedCount = importResult.FailedCount,
            SkippedCount = importResult.SkippedCount,
            DetailsJson = JsonSerializer.Serialize(importResult.ImportedEmployees),
            ErrorMessagesJson = importResult.ErrorMessages.Any()
                ? JsonSerializer.Serialize(importResult.ErrorMessages)
                : null,
            SourceFileName = fileName,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _dbContext.OperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Logged employee import operation: {LogId}, Result: {Result}",
            log.Id, log.Result);

        return log.Id;
    }

    /// <summary>
    /// 记录员工批量更新操作
    /// </summary>
    public async Task<Guid> LogEmployeeUpdateAsync(
        Guid operatorId,
        string operatorName,
        EmployeeBatchUpdateResult updateResult,
        string? fileName = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var log = new OperationLog
        {
            Id = Guid.NewGuid(),
            OperationType = "EmployeeUpdate",
            OperatorId = operatorId,
            OperatorName = operatorName,
            OperatedAt = DateTime.UtcNow,
            Description = fileName != null
                ? $"批量更新员工，文件名：{fileName}"
                : "批量更新员工",
            Result = DetermineResult(updateResult.SuccessCount, updateResult.FailedCount, updateResult.TotalCount),
            TotalCount = updateResult.TotalCount,
            SuccessCount = updateResult.SuccessCount,
            FailedCount = updateResult.FailedCount,
            SkippedCount = updateResult.SkippedCount,
            DetailsJson = JsonSerializer.Serialize(updateResult.Details),
            ErrorMessagesJson = updateResult.ErrorMessages.Any()
                ? JsonSerializer.Serialize(updateResult.ErrorMessages)
                : null,
            SourceFileName = fileName,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _dbContext.OperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Logged employee update operation: {LogId}, Result: {Result}",
            log.Id, log.Result);

        return log.Id;
    }

    /// <summary>
    /// 记录账户开通操作（单个）
    /// </summary>
    public async Task<Guid> LogAccountActivationAsync(
        Guid operatorId,
        string operatorName,
        string employeeName,
        bool success,
        string? errorMessage = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var log = new OperationLog
        {
            Id = Guid.NewGuid(),
            OperationType = "AccountActivation",
            OperatorId = operatorId,
            OperatorName = operatorName,
            OperatedAt = DateTime.UtcNow,
            Description = $"开通账户：{employeeName}",
            Result = success ? "Success" : "Failed",
            TotalCount = 1,
            SuccessCount = success ? 1 : 0,
            FailedCount = success ? 0 : 1,
            SkippedCount = 0,
            DetailsJson = JsonSerializer.Serialize(new { EmployeeName = employeeName }),
            ErrorMessagesJson = errorMessage != null
                ? JsonSerializer.Serialize(new[] { errorMessage })
                : null,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _dbContext.OperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Logged account activation operation: {LogId}, Employee: {EmployeeName}, Success: {Success}",
            log.Id, employeeName, success);

        return log.Id;
    }

    /// <summary>
    /// 记录批量账户开通操作
    /// </summary>
    public async Task<Guid> LogBatchAccountActivationAsync(
        Guid operatorId,
        string operatorName,
        List<Guid> employeeIds,
        int successCount,
        int failedCount,
        List<string>? errorMessages = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var log = new OperationLog
        {
            Id = Guid.NewGuid(),
            OperationType = "AccountBatchActivation",
            OperatorId = operatorId,
            OperatorName = operatorName,
            OperatedAt = DateTime.UtcNow,
            Description = $"批量开通账户，共{employeeIds.Count}个",
            Result = DetermineResult(successCount, failedCount, employeeIds.Count),
            TotalCount = employeeIds.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = 0,
            DetailsJson = JsonSerializer.Serialize(new { EmployeeIds = employeeIds }),
            ErrorMessagesJson = errorMessages?.Any() == true
                ? JsonSerializer.Serialize(errorMessages)
                : null,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _dbContext.OperationLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Logged batch account activation operation: {LogId}, Total: {Total}, Success: {Success}",
            log.Id, employeeIds.Count, successCount);

        return log.Id;
    }

    /// <summary>
    /// 查询操作日志
    /// </summary>
    public async Task<OperationLogQueryResult> QueryLogsAsync(OperationLogQueryRequest request)
    {
        var query = _dbContext.OperationLogs.AsQueryable();

        // 应用筛选条件
        if (!string.IsNullOrEmpty(request.OperationType))
        {
            query = query.Where(l => l.OperationType == request.OperationType);
        }

        if (request.OperatorId.HasValue)
        {
            query = query.Where(l => l.OperatorId == request.OperatorId.Value);
        }

        if (!string.IsNullOrEmpty(request.Result))
        {
            query = query.Where(l => l.Result == request.Result);
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(l => l.OperatedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(l => l.OperatedAt <= request.EndTime.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchKeyword))
        {
            var keyword = request.SearchKeyword.ToLower();
            query = query.Where(l =>
                l.OperatorName.ToLower().Contains(keyword) ||
                l.Description.ToLower().Contains(keyword));
        }

        // 获取总数
        var total = await query.CountAsync();

        // 分页和排序
        var items = await query
            .OrderByDescending(l => l.OperatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new OperationLogDto
            {
                Id = l.Id,
                OperationType = l.OperationType,
                OperationTypeDisplay = GetOperationTypeDisplay(l.OperationType),
                OperatorId = l.OperatorId,
                OperatorName = l.OperatorName,
                OperatedAt = l.OperatedAt,
                Description = l.Description,
                Result = l.Result,
                TotalCount = l.TotalCount,
                SuccessCount = l.SuccessCount,
                FailedCount = l.FailedCount,
                SkippedCount = l.SkippedCount,
                SourceFileName = l.SourceFileName,
                IpAddress = l.IpAddress
            })
            .ToListAsync();

        return new OperationLogQueryResult
        {
            Total = total,
            Items = items
        };
    }

    /// <summary>
    /// 获取操作日志详情
    /// </summary>
    public async Task<OperationLogDetailDto?> GetLogDetailAsync(Guid logId)
    {
        var log = await _dbContext.OperationLogs
            .FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null)
        {
            return null;
        }

        object? details = null;
        if (!string.IsNullOrEmpty(log.DetailsJson))
        {
            try
            {
                details = JsonSerializer.Deserialize<object>(log.DetailsJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize details JSON for log {LogId}", logId);
            }
        }

        List<string> errorMessages = new();
        if (!string.IsNullOrEmpty(log.ErrorMessagesJson))
        {
            try
            {
                errorMessages = JsonSerializer.Deserialize<List<string>>(log.ErrorMessagesJson) ?? new();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize error messages JSON for log {LogId}", logId);
            }
        }

        return new OperationLogDetailDto
        {
            Id = log.Id,
            OperationType = log.OperationType,
            OperationTypeDisplay = GetOperationTypeDisplay(log.OperationType),
            OperatorId = log.OperatorId,
            OperatorName = log.OperatorName,
            OperatedAt = log.OperatedAt,
            Description = log.Description,
            Result = log.Result,
            TotalCount = log.TotalCount,
            SuccessCount = log.SuccessCount,
            FailedCount = log.FailedCount,
            SkippedCount = log.SkippedCount,
            SourceFileName = log.SourceFileName,
            IpAddress = log.IpAddress,
            Details = details,
            ErrorMessages = errorMessages,
            UserAgent = log.UserAgent
        };
    }

    /// <summary>
    /// 获取操作日志统计
    /// </summary>
    public async Task<OperationLogStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbContext.OperationLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(l => l.OperatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.OperatedAt <= endDate.Value);
        }

        var totalOperations = await query.CountAsync();

        var byType = await query
            .GroupBy(l => l.OperationType)
            .Select(g => new OperationTypeStats
            {
                OperationType = g.Key,
                DisplayName = GetOperationTypeDisplay(g.Key),
                Count = g.Count()
            })
            .ToListAsync();

        var byResult = await query
            .GroupBy(l => l.Result)
            .Select(g => new OperationResultStats
            {
                Result = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // 最近7天趋势
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;
        var last7Days = await query
            .Where(l => l.OperatedAt >= sevenDaysAgo)
            .GroupBy(l => l.OperatedAt.Date)
            .Select(g => new DailyStats
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count()
            })
            .OrderBy(s => s.Date)
            .ToListAsync();

        return new OperationLogStatistics
        {
            TotalOperations = totalOperations,
            ByType = byType,
            ByResult = byResult,
            Last7Days = last7Days
        };
    }

    /// <summary>
    /// 删除指定日期之前的日志
    /// </summary>
    public async Task<int> DeleteLogsBeforeAsync(DateTime date)
    {
        var logsToDelete = await _dbContext.OperationLogs
            .Where(l => l.OperatedAt < date)
            .ToListAsync();

        if (!logsToDelete.Any())
        {
            return 0;
        }

        _dbContext.OperationLogs.RemoveRange(logsToDelete);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} operation logs before {Date}", logsToDelete.Count, date);

        return logsToDelete.Count;
    }

    /// <summary>
    /// 确定操作结果
    /// </summary>
    private string DetermineResult(int successCount, int failedCount, int totalCount)
    {
        if (failedCount == 0)
        {
            return "Success";
        }
        else if (successCount == 0)
        {
            return "Failed";
        }
        else
        {
            return "Partial";
        }
    }

    /// <summary>
    /// 获取操作类型显示名称
    /// </summary>
    private string GetOperationTypeDisplay(string operationType)
    {
        return operationType switch
        {
            "EmployeeImport" => "员工批量导入",
            "EmployeeUpdate" => "员工批量更新",
            "AccountActivation" => "账户开通",
            "AccountBatchActivation" => "批量账户开通",
            _ => operationType
        };
    }
}
