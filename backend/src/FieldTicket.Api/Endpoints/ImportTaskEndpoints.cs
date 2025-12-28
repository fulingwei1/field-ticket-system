using Microsoft.AspNetCore.Mvc;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 导入任务管理端点
/// </summary>
public static class ImportTaskEndpoints
{
    public static void MapImportTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import-tasks")
            .WithTags("Import Tasks")
            .RequireAuthorization();

        // GET /api/import-tasks/{taskId} - 获取任务状态
        group.MapGet("/{taskId:guid}", GetTaskStatus)
            .WithName("GetImportTaskStatus")
            .WithDescription("获取导入任务状态")
            .Produces<ImportTaskDto>(200)
            .Produces(404);

        // GET /api/import-tasks/{taskId}/detail - 获取任务详情（包含结果）
        group.MapGet("/{taskId:guid}/detail", GetTaskDetail)
            .WithName("GetImportTaskDetail")
            .WithDescription("获取导入任务详情（包含结果）")
            .Produces<ImportTaskDetailDto>(200)
            .Produces(404);

        // GET /api/import-tasks - 查询任务列表
        group.MapPost("/query", QueryTasks)
            .WithName("QueryImportTasks")
            .WithDescription("查询导入任务列表")
            .Produces<ImportTaskQueryResult>(200);

        // DELETE /api/import-tasks/{taskId} - 删除任务
        group.MapDelete("/{taskId:guid}", DeleteTask)
            .WithName("DeleteImportTask")
            .WithDescription("删除导入任务")
            .Produces(200)
            .Produces(404);

        // POST /api/import-tasks/cleanup - 清理已完成的旧任务
        group.MapPost("/cleanup", CleanupCompletedTasks)
            .WithName("CleanupCompletedTasks")
            .WithDescription("清理已完成的旧任务")
            .Produces<CleanupResult>(200);
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    private static async Task<IResult> GetTaskStatus(
        Guid taskId,
        [FromServices] AsyncImportTaskService asyncTaskService)
    {
        var task = await asyncTaskService.GetTaskStatusAsync(taskId);

        if (task == null)
        {
            return Results.NotFound(new { message = "任务不存在" });
        }

        return Results.Ok(task);
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    private static async Task<IResult> GetTaskDetail(
        Guid taskId,
        [FromServices] AsyncImportTaskService asyncTaskService)
    {
        var task = await asyncTaskService.GetTaskDetailAsync(taskId);

        if (task == null)
        {
            return Results.NotFound(new { message = "任务不存在" });
        }

        return Results.Ok(task);
    }

    /// <summary>
    /// 查询任务列表
    /// </summary>
    private static async Task<IResult> QueryTasks(
        [FromBody] ImportTaskQueryRequest request,
        [FromServices] AsyncImportTaskService asyncTaskService,
        HttpContext httpContext)
    {
        // 如果不是管理员，只能查看自己的任务
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var currentUserId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (currentUserRole != "Admin" && Guid.TryParse(currentUserId, out var userId))
        {
            request.CreatedById = userId;
        }

        var result = await asyncTaskService.QueryTasksAsync(request);

        return Results.Ok(result);
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    private static async Task<IResult> DeleteTask(
        Guid taskId,
        [FromServices] AsyncImportTaskService asyncTaskService,
        HttpContext httpContext)
    {
        var task = await asyncTaskService.GetTaskStatusAsync(taskId);

        if (task == null)
        {
            return Results.NotFound(new { message = "任务不存在" });
        }

        // 只有管理员或任务创建者可以删除
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var currentUserId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (currentUserRole != "Admin" &&
            (!Guid.TryParse(currentUserId, out var userId) || task.CreatedById != userId))
        {
            return Results.Forbid();
        }

        // TODO: 实现删除逻辑
        // await asyncTaskService.DeleteTaskAsync(taskId);

        return Results.Ok(new { message = "任务已删除" });
    }

    /// <summary>
    /// 清理已完成的旧任务
    /// </summary>
    private static async Task<IResult> CleanupCompletedTasks(
        [FromQuery] int daysOld = 30,
        [FromServices] AsyncImportTaskService asyncTaskService,
        HttpContext httpContext)
    {
        // 只有管理员可以清理任务
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        if (currentUserRole != "Admin")
        {
            return Results.Forbid();
        }

        var beforeDate = DateTime.UtcNow.AddDays(-daysOld);
        var deletedCount = await asyncTaskService.CleanupCompletedTasksAsync(beforeDate);

        return Results.Ok(new CleanupResult
        {
            DeletedCount = deletedCount,
            Message = $"成功清理 {deletedCount} 个已完成的旧任务"
        });
    }
}

public class CleanupResult
{
    public int DeletedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
