using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单模板相关端点
/// </summary>
public static class TicketTemplateEndpoints
{
    public static void MapTicketTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ticket-templates")
            .WithTags("TicketTemplates")
            .RequireAuthorization();

        // 创建模板
        group.MapPost("", CreateTemplate)
            .WithName("CreateTemplate")
            .WithSummary("创建工单模板");

        // 从工单创建模板
        group.MapPost("from-ticket/{ticketId:guid}", CreateTemplateFromTicket)
            .WithName("CreateTemplateFromTicket")
            .WithSummary("从工单创建模板");

        // 更新模板
        group.MapPut("{templateId:guid}", UpdateTemplate)
            .WithName("UpdateTemplate")
            .WithSummary("更新模板");

        // 删除模板
        group.MapDelete("{templateId:guid}", DeleteTemplate)
            .WithName("DeleteTemplate")
            .WithSummary("删除模板");

        // 获取模板详情
        group.MapGet("{templateId:guid}", GetTemplate)
            .WithName("GetTemplate")
            .WithSummary("获取模板详情");

        // 获取模板列表
        group.MapGet("", GetTemplates)
            .WithName("GetTemplates")
            .WithSummary("获取模板列表");

        // 应用模板创建工单草稿
        group.MapPost("{templateId:guid}/apply", ApplyTemplate)
            .WithName("ApplyTemplate")
            .WithSummary("应用模板创建工单草稿");
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    private static async Task<IResult> CreateTemplate(
        CreateTicketTemplateRequest request,
        ITicketTemplateService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var template = await service.CreateTemplateAsync(request, userId.Value);
            return Results.Ok(template);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 从工单创建模板
    /// </summary>
    private static async Task<IResult> CreateTemplateFromTicket(
        Guid ticketId,
        [FromBody] CreateTemplateFromTicketRequest request,
        ITicketTemplateService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var template = await service.CreateTemplateFromTicketAsync(
                ticketId, 
                request.Name, 
                request.Description, 
                userId.Value);
            return Results.Ok(template);
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
    /// 更新模板
    /// </summary>
    private static async Task<IResult> UpdateTemplate(
        Guid templateId,
        [FromBody] UpdateTicketTemplateRequest request,
        ITicketTemplateService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var template = await service.UpdateTemplateAsync(templateId, request, userId.Value);
            return Results.Ok(template);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    private static async Task<IResult> DeleteTemplate(
        Guid templateId,
        ITicketTemplateService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            await service.DeleteTemplateAsync(templateId, userId.Value);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    private static async Task<IResult> GetTemplate(
        Guid templateId,
        ITicketTemplateService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var template = await service.GetTemplateAsync(templateId, userId.Value);
            return Results.Ok(template);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取模板列表
    /// </summary>
    private static async Task<IResult> GetTemplates(
        [FromQuery] string? category,
        ITicketTemplateService service = null!,
        HttpContext httpContext = null!,
        [FromQuery] bool includePublic = true)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var templates = await service.GetTemplatesAsync(userId.Value, category, includePublic);
            return Results.Ok(templates);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 应用模板创建工单草稿
    /// </summary>
    private static async Task<IResult> ApplyTemplate(
        Guid templateId,
        [FromBody] ApplyTemplateRequest request,
        ITicketTemplateService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var createRequest = await service.ApplyTemplateAsync(templateId, request.DeviceId, userId.Value);
            return Results.Ok(createRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Forbid();
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
/// 从工单创建模板请求
/// </summary>
public class CreateTemplateFromTicketRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 应用模板请求
/// </summary>
public class ApplyTemplateRequest
{
    public Guid? DeviceId { get; set; }
}








