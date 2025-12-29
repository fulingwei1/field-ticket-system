using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Helpers;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// AI引导式工单创建端点
/// </summary>
public static class GuidedTicketCreationEndpoints
{
    public static void MapGuidedTicketCreationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/guided-ticket-creation")
            .WithTags("Guided Ticket Creation")
            .RequireAuthorization();

        // 创建会话
        group.MapPost("/sessions", CreateSession)
            .WithName("CreateGuidedTicketSession")
            .WithSummary("创建新的引导式工单创建会话");

        // 提交初始信息
        group.MapPost("/sessions/{sessionId}/initial-info", SubmitInitialInfo)
            .WithName("SubmitInitialInfo")
            .WithSummary("提交初始信息（文字+图片）")
            .Accepts<SubmitInitialInfoRequest>("multipart/form-data");

        // 回答引导性问题
        group.MapPost("/sessions/{sessionId}/answer", AnswerQuestion)
            .WithName("AnswerQuestion")
            .WithSummary("回答引导性问题")
            .Accepts<AnswerQuestionRequest>("multipart/form-data");

        // 生成工单内容
        group.MapPost("/sessions/{sessionId}/generate-content", GenerateTicketContent)
            .WithName("GenerateTicketContent")
            .WithSummary("生成工单内容（AI生成的专业描述）");

        // 使用会话创建工单
        group.MapPost("/sessions/{sessionId}/create-ticket", CreateTicketFromSession)
            .WithName("CreateTicketFromSession")
            .WithSummary("使用AI生成的内容创建工单草稿");

        // 获取会话状态
        group.MapGet("/sessions/{sessionId}", GetSession)
            .WithName("GetGuidedTicketSession")
            .WithSummary("获取会话状态");

        // 清理过期会话（管理员功能）
        group.MapPost("/sessions/cleanup", CleanupExpiredSessions)
            .WithName("CleanupExpiredSessions")
            .WithSummary("清理过期的会话");
    }

    private static async Task<IResult> CreateSession(
        HttpContext httpContext,
        IGuidedTicketCreationService service)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var session = await service.CreateSessionAsync(userId.Value);
            return Results.Ok(session);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> SubmitInitialInfo(
        HttpContext httpContext,
        IGuidedTicketCreationService service,
        Guid sessionId,
        [FromForm] SubmitInitialInfoRequest request)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.TextDescription))
        {
            return Results.BadRequest("Text description is required");
        }

        List<Stream>? imageStreams = null;

        try
        {
            // 处理图片上传（如果有）
            if (request.Images != null && request.Images.Any())
            {
                imageStreams = new List<Stream>();
                foreach (var image in request.Images)
                {
                    if (image.Length > 0)
                    {
                        // 验证文件大小（10MB限制）
                        const long maxImageSize = 10 * 1024 * 1024; // 10MB
                        if (image.Length > maxImageSize)
                        {
                            return Results.BadRequest($"文件 {image.FileName} 大小超过限制（最大10MB）");
                        }

                        // 验证文件类型
                        if (!MimeTypeHelper.IsImageFile(image.FileName))
                        {
                            return Results.BadRequest($"文件 {image.FileName} 不是支持的图片格式。支持的格式：jpg, jpeg, png, gif, bmp, webp");
                        }

                        imageStreams.Add(image.OpenReadStream());
                    }
                }
            }

            var response = await service.SubmitInitialInfoAsync(
                sessionId,
                request.TextDescription,
                imageStreams);

            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        finally
        {
            if (imageStreams != null)
            {
                foreach (var stream in imageStreams)
                {
                    await stream.DisposeAsync();
                }
            }
        }
    }

    private static async Task<IResult> AnswerQuestion(
        HttpContext httpContext,
        IGuidedTicketCreationService service,
        Guid sessionId,
        [FromForm] AnswerQuestionRequest request)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.QuestionId) || string.IsNullOrWhiteSpace(request.Answer))
        {
            return Results.BadRequest("QuestionId and Answer are required");
        }

        List<Stream>? additionalImages = null;

        try
        {
            // 处理额外图片（如果有）
            if (request.AdditionalImages != null && request.AdditionalImages.Any())
            {
                additionalImages = new List<Stream>();
                foreach (var image in request.AdditionalImages)
                {
                    if (image.Length > 0)
                    {
                        additionalImages.Add(image.OpenReadStream());
                    }
                }
            }

            var response = await service.AnswerQuestionAsync(
                sessionId,
                request.QuestionId,
                request.Answer,
                additionalImages);

            return Results.Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        finally
        {
            if (additionalImages != null)
            {
                foreach (var stream in additionalImages)
                {
                    await stream.DisposeAsync();
                }
            }
        }
    }

    private static async Task<IResult> GenerateTicketContent(
        HttpContext httpContext,
        IGuidedTicketCreationService service,
        Guid sessionId)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var content = await service.GenerateTicketContentAsync(sessionId);
            return Results.Ok(content);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> CreateTicketFromSession(
        HttpContext httpContext,
        IGuidedTicketCreationService service,
        Guid sessionId,
        [FromBody] CreateTicketFromSessionRequest request)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        if (request.DeviceId == Guid.Empty)
        {
            return Results.BadRequest("DeviceId is required");
        }

        try
        {
            var ticket = await service.CreateTicketFromSessionAsync(sessionId, request.DeviceId);
            return Results.Ok(ticket);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetSession(
        HttpContext httpContext,
        IGuidedTicketCreationService service,
        Guid sessionId)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var session = await service.GetSessionAsync(sessionId);
            
            // 验证用户权限（只能查看自己的会话）
            if (session.UserId != userId.Value)
            {
                return Results.Forbid();
            }

            return Results.Ok(session);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst("userId")?.Value 
            ?? httpContext.User.FindFirst("sub")?.Value;
        
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static async Task<IResult> CleanupExpiredSessions(
        HttpContext httpContext,
        IGuidedTicketCreationService service,
        [FromQuery] int? hours = 24)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var expirationTime = TimeSpan.FromHours(hours ?? 24);
            var count = await service.CleanupExpiredSessionsAsync(expirationTime);
            
            return Results.Ok(new { cleanedCount = count, expirationHours = hours ?? 24 });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

/// <summary>
/// 提交初始信息请求
/// </summary>
public class SubmitInitialInfoRequest
{
    public string TextDescription { get; set; } = string.Empty;
    public List<IFormFile>? Images { get; set; }
}

/// <summary>
/// 回答问题请求
/// </summary>
public class AnswerQuestionRequest
{
    public string QuestionId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<IFormFile>? AdditionalImages { get; set; }
}

/// <summary>
/// 从会话创建工单请求
/// </summary>
public class CreateTicketFromSessionRequest
{
    public Guid DeviceId { get; set; }
}
