using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 解决方案相关端点
/// </summary>
public static class SolutionEndpoints
{
    public static void MapSolutionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/solutions")
            .WithTags("Solutions")
            .RequireAuthorization();

        // 创建解决方案
        group.MapPost("/tickets/{ticketId}", CreateSolution)
            .WithName("CreateSolution")
            .WithSummary("创建解决方案草稿")
            .RequireAuthorization("SeniorEngineer");

        // 更新解决方案
        group.MapPut("/{solutionId}", UpdateSolution)
            .WithName("UpdateSolution")
            .WithSummary("更新解决方案")
            .RequireAuthorization("SeniorEngineer");

        // 发布解决方案
        group.MapPost("/{solutionId}/publish", PublishSolution)
            .WithName("PublishSolution")
            .WithSummary("发布解决方案")
            .RequireAuthorization("SeniorEngineer");

        // 获取解决方案详情
        group.MapGet("/{solutionId}", GetSolution)
            .WithName("GetSolution")
            .WithSummary("获取解决方案详情");

        // 获取工单的解决方案列表
        group.MapGet("/tickets/{ticketId}", GetTicketSolutions)
            .WithName("GetTicketSolutions")
            .WithSummary("获取工单的解决方案列表");
    }

    /// <summary>
    /// 创建解决方案
    /// </summary>
    private static async Task<IResult> CreateSolution(
        Guid ticketId,
        [FromBody] CreateSolutionRequest request,
        ISolutionService solutionService,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            var solution = await solutionService.CreateSolutionAsync(ticketId, request, userId);
            return Results.Created($"/api/solutions/{solution.SolutionId}", solution);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 更新解决方案
    /// </summary>
    private static async Task<IResult> UpdateSolution(
        Guid solutionId,
        [FromBody] UpdateSolutionRequest request,
        ISolutionService solutionService,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            var solution = await solutionService.UpdateSolutionAsync(solutionId, request, userId);
            return Results.Ok(solution);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 发布解决方案
    /// </summary>
    private static async Task<IResult> PublishSolution(
        Guid solutionId,
        ISolutionService solutionService,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            var solution = await solutionService.PublishSolutionAsync(solutionId, userId);
            return Results.Ok(solution);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取解决方案详情
    /// </summary>
    private static async Task<IResult> GetSolution(
        Guid solutionId,
        ISolutionService solutionService)
    {
        try
        {
            var solution = await solutionService.GetSolutionAsync(solutionId);
            if (solution == null)
            {
                return Results.NotFound(new { message = $"解决方案 {solutionId} 不存在" });
            }
            return Results.Ok(solution);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取工单的解决方案列表
    /// </summary>
    private static async Task<IResult> GetTicketSolutions(
        Guid ticketId,
        ISolutionService solutionService)
    {
        try
        {
            var solutions = await solutionService.GetTicketSolutionsAsync(ticketId);
            return Results.Ok(solutions);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value 
            ?? httpContext.User.FindFirst("user_id")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("无法获取用户ID");
        }

        return userId;
    }
}


