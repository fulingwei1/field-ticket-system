using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 知识版本管理相关端点
/// </summary>
public static class KnowledgeVersionEndpoints
{
    public static void MapKnowledgeVersionEndpoints(this WebApplication app)
    {
        // 创建版本
        app.MapPost("/api/knowledge/{knowledgeId}/versions", CreateVersion)
            .WithName("CreateKnowledgeVersion")
            .WithSummary("创建知识版本")
            .WithTags("KnowledgeVersion")
            .RequireAuthorization();

        // 获取版本历史
        app.MapGet("/api/knowledge/{knowledgeId}/versions", GetVersionHistory)
            .WithName("GetVersionHistory")
            .WithSummary("获取版本历史")
            .WithTags("KnowledgeVersion")
            .RequireAuthorization();

        // 版本对比
        app.MapGet("/api/knowledge/versions/{versionId1}/compare/{versionId2}", CompareVersions)
            .WithName("CompareVersions")
            .WithSummary("版本对比")
            .WithTags("KnowledgeVersion")
            .RequireAuthorization();

        // 版本回滚
        app.MapPost("/api/knowledge/{knowledgeId}/versions/{versionId}/rollback", RollbackVersion)
            .WithName("RollbackVersion")
            .WithSummary("版本回滚")
            .WithTags("KnowledgeVersion")
            .RequireAuthorization("Admin");

        // 检查过期知识
        app.MapGet("/api/knowledge/expired", CheckExpiredKnowledge)
            .WithName("CheckExpiredKnowledge")
            .WithSummary("检查过期知识")
            .WithTags("KnowledgeVersion")
            .RequireAuthorization();
    }

    /// <summary>
    /// 创建版本
    /// </summary>
    private static async Task<IResult> CreateVersion(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        [FromBody] CreateVersionRequest request,
        IKnowledgeVersionService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            request.KnowledgeId = knowledgeId;
            request.KnowledgeType = knowledgeType;
            var version = await service.CreateVersionAsync(request, userId);
            return Results.Ok(version);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取版本历史
    /// </summary>
    private static async Task<IResult> GetVersionHistory(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        IKnowledgeVersionService service)
    {
        try
        {
            var versions = await service.GetVersionHistoryAsync(knowledgeId, knowledgeType);
            return Results.Ok(versions);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 版本对比
    /// </summary>
    private static async Task<IResult> CompareVersions(
        Guid versionId1,
        Guid versionId2,
        IKnowledgeVersionService service)
    {
        try
        {
            var comparison = await service.CompareVersionsAsync(versionId1, versionId2);
            return Results.Ok(comparison);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 版本回滚
    /// </summary>
    private static async Task<IResult> RollbackVersion(
        Guid knowledgeId,
        Guid versionId,
        [FromQuery] string knowledgeType,
        [FromBody] RollbackVersionRequest request,
        IKnowledgeVersionService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            var version = await service.RollbackVersionAsync(knowledgeId, knowledgeType, request, userId);
            return Results.Ok(version);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 检查过期知识
    /// </summary>
    private static async Task<IResult> CheckExpiredKnowledge(
        IKnowledgeVersionService service)
    {
        try
        {
            var expired = await service.CheckExpiredKnowledgeAsync();
            return Results.Ok(expired);
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

