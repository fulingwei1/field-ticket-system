using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 项目相关 API 端点
/// </summary>
public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        // 获取项目列表
        group.MapGet("", async (
            [FromQuery] string? projectNo,
            [FromQuery] string? projectName,
            [FromQuery] string? customerName,
            [FromQuery] string? deviceType,
            [FromQuery] string? projectStatus,
            [FromQuery] DateTime? orderDateFrom,
            [FromQuery] DateTime? orderDateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            IProjectService projectService) =>
        {
            try
            {
                var filter = new ProjectQueryFilter
                {
                    ProjectNo = projectNo,
                    ProjectName = projectName,
                    CustomerName = customerName,
                    DeviceType = deviceType,
                    ProjectStatus = projectStatus,
                    OrderDateFrom = orderDateFrom,
                    OrderDateTo = orderDateTo
                };

                var (items, total) = await projectService.GetProjectsAsync(filter, page, pageSize);
                return Results.Ok(new { items, total, page, pageSize });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetProjects")
        .WithSummary("获取项目列表");

        // 获取项目详情
        group.MapGet("/{projectId:guid}", async (
            Guid projectId,
            IProjectService projectService) =>
        {
            try
            {
                var project = await projectService.GetProjectAsync(projectId);
                if (project == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(project);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetProject")
        .WithSummary("获取项目详情");

        // 获取项目的问题列表
        group.MapGet("/{projectId:guid}/problems", async (
            Guid projectId,
            IProjectService projectService) =>
        {
            try
            {
                var problems = await projectService.GetProjectProblemsAsync(projectId);
                return Results.Ok(problems);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetProjectProblems")
        .WithSummary("获取项目的问题列表");

        // 获取问题详情
        group.MapGet("/problems/{problemId:guid}", async (
            Guid problemId,
            IProjectService projectService) =>
        {
            try
            {
                var problem = await projectService.GetProblemAsync(problemId);
                if (problem == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(problem);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetProblem")
        .WithSummary("获取问题详情");

        // 获取问题统计
        group.MapGet("/statistics/problems", async (
            [FromQuery] string? problemCategory,
            [FromQuery] string? primaryDepartment,
            [FromQuery] string? customerName,
            [FromQuery] DateTime? foundDateFrom,
            [FromQuery] DateTime? foundDateTo,
            [FromQuery] string? status,
            IProjectService projectService) =>
        {
            try
            {
                var filter = new ProblemStatisticsFilter
                {
                    ProblemCategory = problemCategory,
                    PrimaryDepartment = primaryDepartment,
                    CustomerName = customerName,
                    FoundDateFrom = foundDateFrom,
                    FoundDateTo = foundDateTo,
                    Status = status
                };

                var statistics = await projectService.GetProblemStatisticsAsync(filter);
                return Results.Ok(statistics);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetProblemStatistics")
        .WithSummary("获取问题统计");
    }
}



