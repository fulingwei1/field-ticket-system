using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 验证相关端点
/// </summary>
public static class VerificationEndpoints
{
    public static void MapVerificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/verifications")
            .WithTags("Verifications")
            .RequireAuthorization();

        // 提交验证结果
        group.MapPost("/tickets/{ticketId}", SubmitVerification)
            .WithName("SubmitVerification")
            .WithSummary("提交验证结果");

        // 获取工单的验证历史
        group.MapGet("/tickets/{ticketId}", GetVerificationHistory)
            .WithName("GetVerificationHistory")
            .WithSummary("获取工单的验证历史");

        // 获取验证详情
        group.MapGet("/{verificationId}", GetVerification)
            .WithName("GetVerification")
            .WithSummary("获取验证详情");
    }

    /// <summary>
    /// 提交验证结果
    /// </summary>
    private static async Task<IResult> SubmitVerification(
        Guid ticketId,
        [FromBody] SubmitVerificationRequest request,
        IVerificationService verificationService,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            var verification = await verificationService.SubmitVerificationAsync(ticketId, request, userId);
            return Results.Created($"/api/verifications/{verification.VerificationId}", verification);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
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
    /// 获取工单的验证历史
    /// </summary>
    private static async Task<IResult> GetVerificationHistory(
        Guid ticketId,
        IVerificationService verificationService)
    {
        try
        {
            var verifications = await verificationService.GetVerificationHistoryAsync(ticketId);
            return Results.Ok(verifications);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取验证详情
    /// </summary>
    private static async Task<IResult> GetVerification(
        Guid verificationId,
        IVerificationService verificationService)
    {
        try
        {
            var verification = await verificationService.GetVerificationAsync(verificationId);
            if (verification == null)
            {
                return Results.NotFound(new { message = $"验证记录 {verificationId} 不存在" });
            }
            return Results.Ok(verification);
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
