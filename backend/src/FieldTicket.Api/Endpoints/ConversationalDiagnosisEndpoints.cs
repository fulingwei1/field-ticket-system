using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 对话式诊断相关端点
/// </summary>
public static class ConversationalDiagnosisEndpoints
{
    public static void MapConversationalDiagnosisEndpoints(this WebApplication app)
    {
        // 开始诊断对话
        app.MapPost("/api/tickets/{ticketId}/diagnosis/start", StartConversation)
            .WithName("StartDiagnosisConversation")
            .WithSummary("开始诊断对话")
            .WithTags("Diagnosis")
            .RequireAuthorization();

        // 生成初始假设
        app.MapPost("/api/tickets/{ticketId}/diagnosis/hypotheses", GenerateInitialHypotheses)
            .WithName("GenerateInitialHypotheses")
            .WithSummary("生成初始假设")
            .WithTags("Diagnosis")
            .RequireAuthorization();

        // 生成验证步骤
        app.MapPost("/api/conversations/{conversationId}/verification-steps", GenerateVerificationSteps)
            .WithName("GenerateVerificationSteps")
            .WithSummary("生成验证步骤")
            .WithTags("Diagnosis")
            .RequireAuthorization();

        // 提交验证结果
        app.MapPost("/api/conversations/{conversationId}/verification-results", SubmitVerificationResult)
            .WithName("SubmitVerificationResult")
            .WithSummary("提交验证结果")
            .WithTags("Diagnosis")
            .RequireAuthorization();

        // 调整假设
        app.MapPost("/api/conversations/{conversationId}/adjust-hypothesis", AdjustHypothesis)
            .WithName("AdjustHypothesis")
            .WithSummary("调整假设")
            .WithTags("Diagnosis")
            .RequireAuthorization();

        // 完成诊断
        app.MapPost("/api/conversations/{conversationId}/complete", CompleteDiagnosis)
            .WithName("CompleteDiagnosis")
            .WithSummary("完成诊断")
            .WithTags("Diagnosis")
            .RequireAuthorization();

        // 获取诊断路径
        app.MapGet("/api/conversations/{conversationId}/diagnosis-path", GetDiagnosisPath)
            .WithName("GetDiagnosisPath")
            .WithSummary("获取诊断路径")
            .WithTags("Diagnosis")
            .RequireAuthorization();
    }

    /// <summary>
    /// 开始诊断对话
    /// </summary>
    private static async Task<IResult> StartConversation(
        Guid ticketId,
        [FromBody] StartConversationRequest? request,
        IConversationalDiagnosisService service)
    {
        try
        {
            var result = await service.StartConversationAsync(ticketId);
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

    /// <summary>
    /// 生成初始假设
    /// </summary>
    private static async Task<IResult> GenerateInitialHypotheses(
        Guid ticketId,
        IConversationalDiagnosisService service)
    {
        try
        {
            var hypotheses = await service.GenerateInitialHypothesesAsync(ticketId);
            return Results.Ok(hypotheses);
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
    /// 生成验证步骤
    /// </summary>
    private static async Task<IResult> GenerateVerificationSteps(
        Guid conversationId,
        [FromBody] GenerateVerificationStepsRequest request,
        IConversationalDiagnosisService service)
    {
        try
        {
            var steps = await service.GenerateVerificationStepsAsync(conversationId, request.HypothesisId);
            return Results.Ok(steps);
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
    /// 提交验证结果
    /// </summary>
    private static async Task<IResult> SubmitVerificationResult(
        Guid conversationId,
        [FromBody] SubmitVerificationResultRequest request,
        IConversationalDiagnosisService service)
    {
        try
        {
            var result = await service.SubmitVerificationResultAsync(conversationId, request);
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

    /// <summary>
    /// 调整假设
    /// </summary>
    private static async Task<IResult> AdjustHypothesis(
        Guid conversationId,
        [FromBody] AdjustHypothesisRequest request,
        IConversationalDiagnosisService service)
    {
        try
        {
            var hypothesis = await service.AdjustHypothesisAsync(conversationId, request);
            return Results.Ok(hypothesis);
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
    /// 完成诊断
    /// </summary>
    private static async Task<IResult> CompleteDiagnosis(
        Guid conversationId,
        IConversationalDiagnosisService service)
    {
        try
        {
            var result = await service.CompleteDiagnosisAsync(conversationId);
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

    /// <summary>
    /// 获取诊断路径
    /// </summary>
    private static async Task<IResult> GetDiagnosisPath(
        Guid conversationId,
        IConversationalDiagnosisService service)
    {
        try
        {
            var path = await service.GetDiagnosisPathAsync(conversationId);
            return Results.Ok(path);
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


