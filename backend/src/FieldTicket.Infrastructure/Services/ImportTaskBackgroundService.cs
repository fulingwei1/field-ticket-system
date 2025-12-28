using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 后台服务：处理异步导入任务
/// </summary>
public class ImportTaskBackgroundService : BackgroundService
{
    private readonly ILogger<ImportTaskBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public ImportTaskBackgroundService(
        ILogger<ImportTaskBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Import Task Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing import tasks");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Import Task Background Service stopped");
    }

    private async Task ProcessPendingTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var asyncTaskService = scope.ServiceProvider.GetRequiredService<AsyncImportTaskService>();

        // 查询所有待处理的任务
        var pendingTasks = await dbContext.ImportTasks
            .Where(t => t.Status == "Pending")
            .OrderBy(t => t.CreatedAt)
            .Take(5) // 一次最多处理5个任务
            .ToListAsync(cancellationToken);

        if (!pendingTasks.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} pending import tasks", pendingTasks.Count);

        foreach (var task in pendingTasks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessTaskAsync(task, scope.ServiceProvider, asyncTaskService, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process task {TaskId}", task.Id);
                await asyncTaskService.FailTaskAsync(task.Id, $"处理失败: {ex.Message}");
            }
        }
    }

    private async Task ProcessTaskAsync(
        ImportTask task,
        IServiceProvider serviceProvider,
        AsyncImportTaskService asyncTaskService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing task {TaskId}, Type: {Type}", task.Id, task.TaskType);

        await asyncTaskService.StartTaskAsync(task.Id);

        try
        {
            switch (task.TaskType)
            {
                case "EmployeeImport":
                    await ProcessEmployeeImportAsync(task, serviceProvider, asyncTaskService, cancellationToken);
                    break;

                case "DeviceImport":
                    await ProcessDeviceImportAsync(task, serviceProvider, asyncTaskService, cancellationToken);
                    break;

                case "CustomerImport":
                    await ProcessCustomerImportAsync(task, serviceProvider, asyncTaskService, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported task type: {task.TaskType}");
            }

            await asyncTaskService.CompleteTaskAsync(task.Id);
            _logger.LogInformation("Task {TaskId} completed successfully", task.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId} failed", task.Id);
            await asyncTaskService.FailTaskAsync(task.Id, ex.Message);
        }
    }

    private async Task ProcessEmployeeImportAsync(
        ImportTask task,
        IServiceProvider serviceProvider,
        AsyncImportTaskService asyncTaskService,
        CancellationToken cancellationToken)
    {
        // 这里可以调用实际的员工导入逻辑
        // 暂时模拟处理
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Processing employee import for task {TaskId}", task.Id);

        // 模拟处理进度更新
        for (int i = 0; i < task.TotalCount; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            // 这里应该调用实际的员工导入逻辑
            // 示例：await ImportSingleEmployeeAsync(...)

            await asyncTaskService.UpdateProgressAsync(
                task.Id,
                processedCount: i + 1,
                successCount: i + 1,
                failedCount: 0,
                skippedCount: 0,
                currentMessage: $"正在导入第 {i + 1}/{task.TotalCount} 条记录..."
            );

            // 模拟处理时间
            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task ProcessDeviceImportAsync(
        ImportTask task,
        IServiceProvider serviceProvider,
        AsyncImportTaskService asyncTaskService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing device import for task {TaskId}", task.Id);

        // TODO: 实现设备导入逻辑
        await Task.CompletedTask;
    }

    private async Task ProcessCustomerImportAsync(
        ImportTask task,
        IServiceProvider serviceProvider,
        AsyncImportTaskService asyncTaskService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing customer import for task {TaskId}", task.Id);

        // TODO: 实现客户导入逻辑
        await Task.CompletedTask;
    }
}
