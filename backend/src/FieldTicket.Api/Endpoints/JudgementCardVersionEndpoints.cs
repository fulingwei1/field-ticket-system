using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 判断卡版本管理相关端点
/// </summary>
public static class JudgementCardVersionEndpoints
{
    public static void MapJudgementCardVersionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/judgement-cards/{jcCode}/versions")
            .WithTags("JudgementCardVersion")
            .RequireAuthorization();

        // 创建新版本
        group.MapPost("", CreateNewVersion)
            .WithName("CreateNewVersion")
            .WithSummary("创建判断卡新版本（必须填写变更原因）");

        // 获取所有版本
        group.MapGet("", GetVersions)
            .WithName("GetVersions")
            .WithSummary("获取判断卡的所有版本");

        // 获取指定版本
        group.MapGet("/{version}", GetVersion)
            .WithName("GetVersion")
            .WithSummary("获取指定版本的判断卡");

        // 版本对比
        group.MapGet("/compare", CompareVersions)
            .WithName("CompareVersions")
            .WithSummary("对比两个版本");

        // 获取使用历史
        group.MapGet("/{version}/usage-history", GetUsageHistory)
            .WithName("GetUsageHistory")
            .WithSummary("获取判断卡的使用历史");
    }

    /// <summary>
    /// 创建新版本
    /// </summary>
    private static async Task<IResult> CreateNewVersion(
        string jcCode,
        [FromBody] CreateNewVersionRequest request,
        IJudgementCardVersionService service,
        HttpContext httpContext)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ChangeReason))
            {
                return Results.BadRequest(new { message = "变更原因不能为空，必须填写推翻原因" });
            }

            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var card = await service.CreateNewVersionAsync(
                jcCode,
                request.UpdateRequest,
                request.ChangeReason,
                userId.Value);

            return Results.Created($"/api/judgement-cards/{jcCode}/versions/{card.Version}", card);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取所有版本
    /// </summary>
    private static async Task<IResult> GetVersions(
        string jcCode,
        IJudgementCardVersionService service)
    {
        try
        {
            var versions = await service.GetVersionsAsync(jcCode);
            return Results.Ok(versions);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取指定版本
    /// </summary>
    private static async Task<IResult> GetVersion(
        string jcCode,
        int version,
        IJudgementCardVersionService service)
    {
        try
        {
            var card = await service.GetVersionAsync(jcCode, version);
            if (card == null)
            {
                return Results.NotFound(new { message = $"判断卡 {jcCode} 的版本 {version} 不存在" });
            }
            return Results.Ok(card);
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
        string jcCode,
        [FromQuery] int version1,
        [FromQuery] int version2,
        IJudgementCardVersionService service)
    {
        try
        {
            var comparison = await service.CompareVersionsAsync(jcCode, version1, version2);
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
    /// 获取使用历史
    /// </summary>
    private static async Task<IResult> GetUsageHistory(
        string jcCode,
        int version,
        [FromQuery] int? limit,
        IJudgementCardVersionService service)
    {
        try
        {
            var history = await service.GetUsageHistoryAsync(jcCode, version);
            if (limit.HasValue && limit.Value > 0)
            {
                history = history.Take(limit.Value).ToList();
            }
            return Results.Ok(history);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}

/// <summary>
/// 创建新版本请求
/// </summary>
public class CreateNewVersionRequest
{
    public UpdateJudgementCardRequest UpdateRequest { get; set; } = null!;
    public string ChangeReason { get; set; } = string.Empty;
}

