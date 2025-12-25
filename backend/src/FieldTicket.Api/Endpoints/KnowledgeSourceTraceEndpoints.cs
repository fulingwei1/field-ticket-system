using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 知识来源追溯相关端点
/// </summary>
public static class KnowledgeSourceTraceEndpoints
{
    public static void MapKnowledgeSourceTraceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/knowledge-source-trace")
            .WithTags("KnowledgeSourceTrace")
            .RequireAuthorization();

        // 获取知识来源信息
        group.MapGet("{knowledgeId:guid}", GetSourceTrace)
            .WithName("GetKnowledgeSourceTrace")
            .WithSummary("获取知识来源信息");

        // 记录知识验证
        group.MapPost("{knowledgeId:guid}/verify", RecordVerification)
            .WithName("RecordKnowledgeVerification")
            .WithSummary("记录知识验证");

        // 更新知识来源
        group.MapPut("{knowledgeId:guid}/source", UpdateSource)
            .WithName("UpdateKnowledgeSource")
            .WithSummary("更新知识来源");

        // 获取验证历史
        group.MapGet("{knowledgeId:guid}/verification-history", GetVerificationHistory)
            .WithName("GetKnowledgeVerificationHistory")
            .WithSummary("获取知识的验证历史");

        // 获取可信度评估
        group.MapGet("{knowledgeId:guid}/credibility", GetCredibility)
            .WithName("GetKnowledgeCredibility")
            .WithSummary("获取知识可信度评估");
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
    /// 获取知识来源信息
    /// </summary>
    private static async Task<IResult> GetSourceTrace(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        IKnowledgeSourceTraceService service)
    {
        try
        {
            var trace = await service.GetSourceTraceAsync(knowledgeId, knowledgeType);
            if (trace == null)
            {
                return Results.NotFound(new { message = $"知识 {knowledgeId} 不存在" });
            }
            return Results.Ok(trace);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 记录知识验证
    /// </summary>
    private static async Task<IResult> RecordVerification(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        [FromBody] RecordVerificationRequest request,
        HttpContext context,
        IKnowledgeSourceTraceService service)
    {
        try
        {
            var userId = GetUserId(context);
            await service.RecordVerificationAsync(
                knowledgeId,
                knowledgeType,
                userId,
                request.VerificationNote);
            return Results.Ok(new { message = "验证记录成功" });
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
    /// 更新知识来源
    /// </summary>
    private static async Task<IResult> UpdateSource(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        [FromBody] UpdateKnowledgeSourceRequest request,
        IKnowledgeSourceTraceService service)
    {
        try
        {
            await service.UpdateSourceAsync(knowledgeId, knowledgeType, request);
            return Results.Ok(new { message = "来源更新成功" });
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
    /// 获取验证历史
    /// </summary>
    private static async Task<IResult> GetVerificationHistory(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        IKnowledgeSourceTraceService service)
    {
        try
        {
            var history = await service.GetVerificationHistoryAsync(knowledgeId, knowledgeType, page, pageSize);
            return Results.Ok(history);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取可信度评估
    /// </summary>
    private static async Task<IResult> GetCredibility(
        Guid knowledgeId,
        [FromQuery] string knowledgeType,
        IKnowledgeSourceTraceService service)
    {
        try
        {
            var credibility = await service.GetCredibilityAsync(knowledgeId, knowledgeType);
            return Results.Ok(credibility);
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

/// <summary>
/// 记录验证请求
/// </summary>
public class RecordVerificationRequest
{
    public string? VerificationNote { get; set; }
}






