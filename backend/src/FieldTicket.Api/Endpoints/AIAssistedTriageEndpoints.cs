using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// AI辅助分诊相关端点
/// </summary>
public static class AIAssistedTriageEndpoints
{
    public static void MapAIAssistedTriageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets/{ticketId}/ai-assist")
            .WithTags("AIAssistedTriage")
            .RequireAuthorization();

        // AI辅助填卡
        group.MapPost("/triage", AssistTriage)
            .WithName("AssistTriage")
            .WithSummary("AI辅助填写判断卡（结构化triage）");

        // 生成假设
        group.MapPost("/hypotheses", GenerateHypotheses)
            .WithName("GenerateHypotheses")
            .WithSummary("生成Top-3假设（基于RAG）");

        // 生成动作建议
        group.MapPost("/actions", GenerateActionSuggestions)
            .WithName("GenerateActionSuggestions")
            .WithSummary("生成下一步动作建议");

        // 生成缺失信息清单
        group.MapPost("/missing-info", GenerateMissingInfo)
            .WithName("GenerateMissingInfo")
            .WithSummary("生成缺失信息问题清单");
    }

    /// <summary>
    /// AI辅助填卡
    /// </summary>
    private static async Task<IResult> AssistTriage(
        Guid ticketId,
        IAIAssistedTriageService service)
    {
        try
        {
            var result = await service.AssistTriageAsync(ticketId);
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
    /// 生成假设
    /// </summary>
    private static async Task<IResult> GenerateHypotheses(
        Guid ticketId,
        [FromQuery] string? jcCode,
        IAIAssistedTriageService service)
    {
        try
        {
            var hypotheses = await service.GenerateHypothesesAsync(ticketId, jcCode);
            return Results.Ok(new { hypotheses });
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
    /// 生成动作建议
    /// </summary>
    private static async Task<IResult> GenerateActionSuggestions(
        Guid ticketId,
        [FromBody] GenerateActionSuggestionsRequest? request,
        IAIAssistedTriageService service)
    {
        try
        {
            var suggestions = await service.GenerateActionSuggestionsAsync(
                ticketId,
                request?.JcCode,
                request?.HypothesisIds);
            return Results.Ok(new { suggestions });
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
    /// 生成缺失信息清单
    /// </summary>
    private static async Task<IResult> GenerateMissingInfo(
        Guid ticketId,
        [FromQuery] string? jcCode,
        IAIAssistedTriageService service)
    {
        try
        {
            var questions = await service.GenerateMissingInfoQuestionsAsync(ticketId, jcCode);
            return Results.Ok(new { questions });
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
/// 生成动作建议请求
/// </summary>
public class GenerateActionSuggestionsRequest
{
    public string? JcCode { get; set; }
    public List<string>? HypothesisIds { get; set; }
}
















