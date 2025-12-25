using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 知识有效期和版本绑定相关端点
/// </summary>
public static class KnowledgeValidityEndpoints
{
    public static void MapKnowledgeValidityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/knowledge-validity")
            .WithTags("KnowledgeValidity")
            .RequireAuthorization();

        // 检查并更新过期知识
        group.MapPost("check-expired", CheckAndUpdateExpired)
            .WithName("CheckAndUpdateExpiredKnowledge")
            .WithSummary("检查并更新过期知识标记");

        // 检查知识是否适用于指定版本
        group.MapGet("{knowledgeId:guid}/applicable", CheckApplicable)
            .WithName("CheckKnowledgeApplicable")
            .WithSummary("检查知识是否适用于指定版本");

        // 检查知识是否过期
        group.MapGet("{knowledgeId:guid}/expired", CheckExpired)
            .WithName("CheckKnowledgeExpired")
            .WithSummary("检查知识是否过期");

        // 获取过期知识列表
        group.MapGet("expired", GetExpiredKnowledge)
            .WithName("GetExpiredKnowledge")
            .WithSummary("获取过期知识列表");

        // 更新知识的版本绑定
        group.MapPut("{knowledgeId:guid}/version-binding", UpdateVersionBinding)
            .WithName("UpdateKnowledgeVersionBinding")
            .WithSummary("更新知识的版本绑定");

        // 检查工单版本与知识版本的匹配情况
        group.MapGet("tickets/{ticketId:guid}/knowledge/{knowledgeId:guid}/version-match", CheckVersionMatch)
            .WithName("CheckVersionMatch")
            .WithSummary("检查工单版本与知识版本的匹配情况");
    }

    private static Guid GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("用户未认证");
        }
        return userId;
    }

    /// <summary>
    /// 检查并更新过期知识
    /// </summary>
    private static async Task<IResult> CheckAndUpdateExpired(
        IKnowledgeValidityService service)
    {
        try
        {
            var count = await service.CheckAndUpdateExpiredKnowledgeAsync();
            return Results.Ok(new { updatedCount = count, message = $"已更新 {count} 条知识" });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 检查知识是否适用于指定版本
    /// </summary>
    private static async Task<IResult> CheckApplicable(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        [FromQuery] string? swVersion,
        [FromQuery] string? hwVersion,
        IKnowledgeValidityService service)
    {
        try
        {
            var isApplicable = await service.IsKnowledgeApplicableAsync(
                knowledgeId,
                knowledgeType,
                swVersion,
                hwVersion);
            return Results.Ok(new { isApplicable });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 检查知识是否过期
    /// </summary>
    private static async Task<IResult> CheckExpired(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        IKnowledgeValidityService service)
    {
        try
        {
            var isExpired = await service.IsKnowledgeExpiredAsync(knowledgeId, knowledgeType);
            return Results.Ok(new { isExpired });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取过期知识列表
    /// </summary>
    private static async Task<IResult> GetExpiredKnowledge(
        [FromQuery] string? knowledgeType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        IKnowledgeValidityService service)
    {
        try
        {
            var expired = await service.GetExpiredKnowledgeAsync(knowledgeType, page, pageSize);
            return Results.Ok(expired);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 更新知识的版本绑定
    /// </summary>
    private static async Task<IResult> UpdateVersionBinding(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        [FromBody] UpdateVersionBindingRequest request,
        IKnowledgeValidityService service)
    {
        try
        {
            await service.UpdateKnowledgeVersionBindingAsync(knowledgeId, knowledgeType, request);
            return Results.Ok(new { message = "版本绑定更新成功" });
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
    /// 检查工单版本与知识版本的匹配情况
    /// </summary>
    private static async Task<IResult> CheckVersionMatch(
        Guid ticketId,
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        IKnowledgeValidityService service)
    {
        try
        {
            var result = await service.CheckVersionMatchAsync(ticketId, knowledgeId, knowledgeType);
            return Results.Ok(result);
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
}






