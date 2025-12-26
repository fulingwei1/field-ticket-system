using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 附件相关 API 端点
/// </summary>
public static class AttachmentEndpoints
{
    public static void MapAttachmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/attachments")
            .WithTags("Attachments")
            .RequireAuthorization();

        // 上传附件
        group.MapPost("", UploadAttachment)
            .WithName("UploadAttachment")
            .WithSummary("上传附件")
            .DisableAntiforgery() // 文件上传需要禁用防伪验证
            .Produces<AttachmentDto>()
            .Produces(StatusCodes.Status400BadRequest);

        // 获取附件下载URL
        group.MapGet("{attachmentId:guid}/download-url", GetDownloadUrl)
            .WithName("GetDownloadUrl")
            .WithSummary("获取附件下载URL")
            .Produces<string>()
            .Produces(StatusCodes.Status404NotFound);

        // 删除附件
        group.MapDelete("{attachmentId:guid}", DeleteAttachment)
            .WithName("DeleteAttachment")
            .WithSummary("删除附件")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        // 获取工单的附件列表
        group.MapGet("tickets/{ticketId:guid}", GetTicketAttachments)
            .WithName("GetTicketAttachments")
            .WithSummary("获取工单的附件列表")
            .Produces<List<AttachmentDto>>();

        // 获取附件详情
        group.MapGet("{attachmentId:guid}", GetAttachment)
            .WithName("GetAttachment")
            .WithSummary("获取附件详情")
            .Produces<AttachmentDto>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UploadAttachment(
        HttpContext httpContext,
        IAttachmentService attachmentService,
        [FromForm] IFormFile file,
        [FromForm] Guid ticketId,
        [FromForm] string fileType)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest("文件不能为空");
        }

        if (!new[] { "photo", "video", "log", "file" }.Contains(fileType))
        {
            return Results.BadRequest("文件类型必须是：photo, video, log, file");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var attachment = await attachmentService.UploadAsync(
                ticketId,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                fileType,
                userId.Value
            );

            return Results.Ok(attachment);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetDownloadUrl(
        Guid attachmentId,
        IAttachmentService attachmentService = null!,
        [FromQuery] int expirySeconds = 900)
    {
        try
        {
            var url = await attachmentService.GetDownloadUrlAsync(attachmentId, expirySeconds);
            return Results.Ok(new { url });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> DeleteAttachment(
        Guid attachmentId,
        HttpContext httpContext,
        IAttachmentService attachmentService)
    {
        var userId = GetUserId(httpContext);
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            await attachmentService.DeleteAsync(attachmentId, userId.Value);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetTicketAttachments(
        Guid ticketId,
        IAttachmentService attachmentService)
    {
        try
        {
            var attachments = await attachmentService.GetTicketAttachmentsAsync(ticketId);
            return Results.Ok(attachments);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetAttachment(
        Guid attachmentId,
        IAttachmentService attachmentService)
    {
        try
        {
            var attachment = await attachmentService.GetAttachmentAsync(attachmentId);
            if (attachment == null)
            {
                return Results.NotFound();
            }

            // 获取下载URL
            var downloadUrl = await attachmentService.GetDownloadUrlAsync(attachmentId);
            attachment.DownloadUrl = downloadUrl;

            return Results.Ok(attachment);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}

