using System.Text.Json;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 异步导入任务服务
/// </summary>
public class AsyncImportTaskService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AsyncImportTaskService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AsyncImportTaskService(
        ApplicationDbContext dbContext,
        ILogger<AsyncImportTaskService> logger,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 创建异步导入任务
    /// </summary>
    public async Task<Guid> CreateTaskAsync(
        string taskType,
        string fileName,
        string filePath,
        int totalCount,
        Guid createdById,
        string createdByName)
    {
        var task = new ImportTask
        {
            Id = Guid.NewGuid(),
            TaskType = taskType,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById,
            CreatedByName = createdByName,
            FileName = fileName,
            FilePath = filePath,
            TotalCount = totalCount,
            ProcessedCount = 0,
            SuccessCount = 0,
            FailedCount = 0,
            SkippedCount = 0,
            ProgressPercentage = 0,
            CurrentMessage = "任务已创建，等待处理..."
        };

        _dbContext.ImportTasks.Add(task);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created async import task: {TaskId}, Type: {TaskType}, Total: {Total}",
            task.Id, taskType, totalCount);

        return task.Id;
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public async Task UpdateProgressAsync(
        Guid taskId,
        int processedCount,
        int successCount,
        int failedCount,
        int skippedCount,
        string? currentMessage = null)
    {
        var task = await _dbContext.ImportTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        task.ProcessedCount = processedCount;
        task.SuccessCount = successCount;
        task.FailedCount = failedCount;
        task.SkippedCount = skippedCount;
        task.ProgressPercentage = task.TotalCount > 0
            ? (int)((double)processedCount / task.TotalCount * 100)
            : 0;

        if (currentMessage != null)
        {
            task.CurrentMessage = currentMessage;
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 开始处理任务
    /// </summary>
    public async Task StartTaskAsync(Guid taskId)
    {
        var task = await _dbContext.ImportTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        task.Status = "Processing";
        task.StartedAt = DateTime.UtcNow;
        task.CurrentMessage = "正在处理...";

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Started processing task: {TaskId}", taskId);
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    public async Task CompleteTaskAsync(Guid taskId, object? result = null)
    {
        var task = await _dbContext.ImportTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        task.Status = "Completed";
        task.CompletedAt = DateTime.UtcNow;
        task.ProgressPercentage = 100;
        task.CurrentMessage = "处理完成";

        if (result != null)
        {
            task.ResultJson = JsonSerializer.Serialize(result);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Completed task: {TaskId}, Success: {Success}, Failed: {Failed}",
            taskId, task.SuccessCount, task.FailedCount);
    }

    /// <summary>
    /// 标记任务失败
    /// </summary>
    public async Task FailTaskAsync(Guid taskId, string errorMessage)
    {
        var task = await _dbContext.ImportTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("Task not found: {TaskId}", taskId);
            return;
        }

        task.Status = "Failed";
        task.CompletedAt = DateTime.UtcNow;
        task.ErrorMessage = errorMessage;
        task.CurrentMessage = "处理失败";

        await _dbContext.SaveChangesAsync();

        _logger.LogError("Task failed: {TaskId}, Error: {Error}", taskId, errorMessage);
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    public async Task<ImportTaskDto?> GetTaskStatusAsync(Guid taskId)
    {
        var task = await _dbContext.ImportTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            return null;
        }

        return MapToDto(task);
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    public async Task<ImportTaskDetailDto?> GetTaskDetailAsync(Guid taskId)
    {
        var task = await _dbContext.ImportTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            return null;
        }

        object? result = null;
        if (!string.IsNullOrEmpty(task.ResultJson))
        {
            try
            {
                result = JsonSerializer.Deserialize<object>(task.ResultJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize result JSON for task {TaskId}", taskId);
            }
        }

        return new ImportTaskDetailDto
        {
            Id = task.Id,
            TaskType = task.TaskType,
            TaskTypeDisplay = GetTaskTypeDisplay(task.TaskType),
            Status = task.Status,
            StatusDisplay = GetStatusDisplay(task.Status),
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            CreatedById = task.CreatedById,
            CreatedByName = task.CreatedByName,
            FileName = task.FileName,
            TotalCount = task.TotalCount,
            ProcessedCount = task.ProcessedCount,
            SuccessCount = task.SuccessCount,
            FailedCount = task.FailedCount,
            SkippedCount = task.SkippedCount,
            ProgressPercentage = task.ProgressPercentage,
            CurrentMessage = task.CurrentMessage,
            ErrorMessage = task.ErrorMessage,
            Result = result
        };
    }

    /// <summary>
    /// 查询任务列表
    /// </summary>
    public async Task<ImportTaskQueryResult> QueryTasksAsync(ImportTaskQueryRequest request)
    {
        var query = _dbContext.ImportTasks.AsQueryable();

        // 应用筛选条件
        if (!string.IsNullOrEmpty(request.TaskType))
        {
            query = query.Where(t => t.TaskType == request.TaskType);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(t => t.Status == request.Status);
        }

        if (request.CreatedById.HasValue)
        {
            query = query.Where(t => t.CreatedById == request.CreatedById.Value);
        }

        // 获取总数
        var total = await query.CountAsync();

        // 分页和排序
        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new ImportTaskQueryResult
        {
            Total = total,
            Items = tasks.Select(MapToDto).ToList()
        };
    }

    /// <summary>
    /// 清理已完成的旧任务
    /// </summary>
    public async Task<int> CleanupCompletedTasksAsync(DateTime beforeDate)
    {
        var tasksToDelete = await _dbContext.ImportTasks
            .Where(t => t.Status == "Completed" || t.Status == "Failed")
            .Where(t => t.CompletedAt < beforeDate)
            .ToListAsync();

        if (!tasksToDelete.Any())
        {
            return 0;
        }

        // 删除关联的临时文件
        foreach (var task in tasksToDelete)
        {
            if (!string.IsNullOrEmpty(task.FilePath) && File.Exists(task.FilePath))
            {
                try
                {
                    File.Delete(task.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {FilePath}", task.FilePath);
                }
            }
        }

        _dbContext.ImportTasks.RemoveRange(tasksToDelete);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} completed tasks before {Date}",
            tasksToDelete.Count, beforeDate);

        return tasksToDelete.Count;
    }

    /// <summary>
    /// 映射为DTO
    /// </summary>
    private ImportTaskDto MapToDto(ImportTask task)
    {
        return new ImportTaskDto
        {
            Id = task.Id,
            TaskType = task.TaskType,
            TaskTypeDisplay = GetTaskTypeDisplay(task.TaskType),
            Status = task.Status,
            StatusDisplay = GetStatusDisplay(task.Status),
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            CreatedById = task.CreatedById,
            CreatedByName = task.CreatedByName,
            FileName = task.FileName,
            TotalCount = task.TotalCount,
            ProcessedCount = task.ProcessedCount,
            SuccessCount = task.SuccessCount,
            FailedCount = task.FailedCount,
            SkippedCount = task.SkippedCount,
            ProgressPercentage = task.ProgressPercentage,
            CurrentMessage = task.CurrentMessage,
            ErrorMessage = task.ErrorMessage
        };
    }

    private string GetTaskTypeDisplay(string taskType)
    {
        return taskType switch
        {
            "EmployeeImport" => "员工导入",
            "EmployeeUpdate" => "员工更新",
            _ => taskType
        };
    }

    private string GetStatusDisplay(string status)
    {
        return status switch
        {
            "Pending" => "等待处理",
            "Processing" => "处理中",
            "Completed" => "已完成",
            "Failed" => "失败",
            _ => status
        };
    }
}
