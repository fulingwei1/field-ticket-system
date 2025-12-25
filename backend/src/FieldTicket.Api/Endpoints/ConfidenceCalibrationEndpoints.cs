using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 置信度校准相关端点
/// </summary>
public static class ConfidenceCalibrationEndpoints
{
    public static void MapConfidenceCalibrationEndpoints(this WebApplication app)
    {
        // 校准置信度
        app.MapPost("/api/confidence/calibrate", CalibrateConfidence)
            .WithName("CalibrateConfidence")
            .WithSummary("校准置信度")
            .WithTags("Confidence")
            .RequireAuthorization();

        // 训练校准模型
        app.MapPost("/api/confidence/train-model", TrainCalibrationModel)
            .WithName("TrainCalibrationModel")
            .WithSummary("训练校准模型")
            .WithTags("Confidence")
            .RequireAuthorization("Admin");

        // 评估校准效果
        app.MapGet("/api/confidence/evaluate", EvaluateCalibration)
            .WithName("EvaluateCalibration")
            .WithSummary("评估校准效果")
            .WithTags("Confidence")
            .RequireAuthorization("Admin");

        // 获取置信度分布
        app.MapGet("/api/confidence/distribution", GetConfidenceDistribution)
            .WithName("GetConfidenceDistribution")
            .WithSummary("获取置信度分布")
            .WithTags("Confidence")
            .RequireAuthorization();

        // 提交校准反馈
        app.MapPost("/api/confidence/feedback", SubmitCalibrationFeedback)
            .WithName("SubmitCalibrationFeedback")
            .WithSummary("提交校准反馈")
            .WithTags("Confidence")
            .RequireAuthorization();

        // 获取活跃的校准模型
        app.MapGet("/api/confidence/active-model", GetActiveModel)
            .WithName("GetActiveModel")
            .WithSummary("获取活跃的校准模型")
            .WithTags("Confidence")
            .RequireAuthorization();
    }

    /// <summary>
    /// 校准置信度
    /// </summary>
    private static async Task<IResult> CalibrateConfidence(
        [FromBody] CalibrateConfidenceRequest request,
        IConfidenceCalibrationService service)
    {
        try
        {
            var result = await service.CalibrateConfidenceAsync(request);
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
    /// 训练校准模型
    /// </summary>
    private static async Task<IResult> TrainCalibrationModel(
        [FromBody] TrainCalibrationModelRequest request,
        IConfidenceCalibrationService service)
    {
        try
        {
            var model = await service.TrainCalibrationModelAsync(request);
            return Results.Ok(model);
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
    /// 评估校准效果
    /// </summary>
    private static async Task<IResult> EvaluateCalibration(
        [FromQuery] Guid modelId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IConfidenceCalibrationService service)
    {
        try
        {
            var request = new EvaluateCalibrationRequest
            {
                ModelId = modelId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var evaluation = await service.EvaluateCalibrationAsync(request);
            return Results.Ok(evaluation);
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
    /// 获取置信度分布
    /// </summary>
    private static async Task<IResult> GetConfidenceDistribution(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        IConfidenceCalibrationService service)
    {
        try
        {
            var distribution = await service.GetConfidenceDistributionAsync(fromDate, toDate);
            return Results.Ok(distribution);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 提交校准反馈
    /// </summary>
    private static async Task<IResult> SubmitCalibrationFeedback(
        [FromBody] SubmitCalibrationFeedbackRequest request,
        IConfidenceCalibrationService service)
    {
        try
        {
            await service.RecordCalibrationResultAsync(request);
            return Results.Ok(new { message = "校准反馈已记录" });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取活跃的校准模型
    /// </summary>
    private static async Task<IResult> GetActiveModel(
        IConfidenceCalibrationService service)
    {
        try
        {
            var model = await service.GetActiveModelAsync();
            if (model == null)
            {
                return Results.NotFound(new { message = "没有活跃的校准模型" });
            }
            return Results.Ok(model);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

