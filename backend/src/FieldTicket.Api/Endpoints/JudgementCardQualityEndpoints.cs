using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 判断卡质量评分相关端点
/// </summary>
public static class JudgementCardQualityEndpoints
{
    public static void MapJudgementCardQualityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/judgement-cards/quality")
            .WithTags("JudgementCardQuality")
            .RequireAuthorization();

        // 评分判断卡
        group.MapGet("/{jcCode}/score", GetQualityScore)
            .WithName("GetQualityScore")
            .WithSummary("获取判断卡质量评分");

        // 批量评分
        group.MapPost("/batch-score", BatchScore)
            .WithName("BatchScore")
            .WithSummary("批量评分判断卡");

        // 获取质量问题列表
        group.MapGet("/{jcCode}/issues", GetQualityIssues)
            .WithName("GetQualityIssues")
            .WithSummary("获取判断卡的质量问题列表");
    }

    /// <summary>
    /// 获取判断卡质量评分
    /// </summary>
    private static async Task<IResult> GetQualityScore(
        string jcCode,
        IJudgementCardQualityService service)
    {
        try
        {
            var score = await service.ScoreAsync(jcCode);
            var dto = MapToDto(score);
            return Results.Ok(dto);
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
    /// 批量评分判断卡
    /// </summary>
    private static async Task<IResult> BatchScore(
        [FromBody] BatchScoreRequest request,
        IJudgementCardQualityService service)
    {
        try
        {
            if (request.JcCodes == null || request.JcCodes.Count == 0)
            {
                return Results.BadRequest(new { message = "判断卡编号列表不能为空" });
            }

            var scores = await service.BatchScoreAsync(request.JcCodes);
            var dtos = scores.Select(MapToDto).ToList();
            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取质量问题列表
    /// </summary>
    private static async Task<IResult> GetQualityIssues(
        string jcCode,
        IJudgementCardQualityService service)
    {
        try
        {
            var issues = await service.GetQualityIssuesAsync(jcCode);
            var dtos = issues.Select(MapIssueToDto).ToList();
            return Results.Ok(dtos);
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
    /// 映射评分结果到DTO
    /// </summary>
    private static JudgementCardQualityScoreDto MapToDto(JudgementCardQualityScore score)
    {
        return new JudgementCardQualityScoreDto
        {
            JcCode = score.JcCode,
            Title = score.Title,
            CompletenessScore = score.CompletenessScore,
            LogicConsistencyScore = score.LogicConsistencyScore,
            VerifiabilityScore = score.VerifiabilityScore,
            EvidenceScore = score.EvidenceScore,
            TotalScore = score.TotalScore,
            Level = score.Level.ToString(),
            Issues = score.Issues.Select(MapIssueToDto).ToList(),
            ScoredAt = score.ScoredAt
        };
    }

    /// <summary>
    /// 映射质量问题到DTO
    /// </summary>
    private static QualityIssueDto MapIssueToDto(QualityIssue issue)
    {
        return new QualityIssueDto
        {
            Type = issue.Type.ToString(),
            Description = issue.Description,
            Severity = issue.Severity.ToString(),
            Suggestion = issue.Suggestion
        };
    }
}

