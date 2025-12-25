using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 客户沟通模板相关端点
/// </summary>
public static class CommunicationTemplateEndpoints
{
    public static void MapCommunicationTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/communication-templates")
            .WithTags("CommunicationTemplates")
            .RequireAuthorization();

        // 获取模板列表
        group.MapGet("", GetTemplates)
            .WithName("GetCommunicationTemplates")
            .WithSummary("获取沟通模板列表");

        // 获取模板详情
        group.MapGet("{templateId:guid}", GetTemplate)
            .WithName("GetCommunicationTemplate")
            .WithSummary("获取模板详情");

        // 根据编号获取模板
        group.MapGet("code/{code}", GetTemplateByCode)
            .WithName("GetCommunicationTemplateByCode")
            .WithSummary("根据编号获取模板");

        // 创建模板
        group.MapPost("", CreateTemplate)
            .WithName("CreateCommunicationTemplate")
            .WithSummary("创建沟通模板");

        // 更新模板
        group.MapPut("{templateId:guid}", UpdateTemplate)
            .WithName("UpdateCommunicationTemplate")
            .WithSummary("更新沟通模板");

        // 删除模板
        group.MapDelete("{templateId:guid}", DeleteTemplate)
            .WithName("DeleteCommunicationTemplate")
            .WithSummary("删除沟通模板");

        // 渲染模板
        group.MapPost("{templateId:guid}/render", RenderTemplate)
            .WithName("RenderCommunicationTemplate")
            .WithSummary("渲染模板（替换变量）");

        // 保存沟通记录
        group.MapPost("communications", SaveCommunication)
            .WithName("SaveCustomerCommunication")
            .WithSummary("保存沟通记录");

        // 获取工单的沟通记录
        group.MapGet("tickets/{ticketId:guid}/communications", GetTicketCommunications)
            .WithName("GetTicketCommunications")
            .WithSummary("获取工单的沟通记录");
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
    /// 获取模板列表
    /// </summary>
    private static async Task<IResult> GetTemplates(
        [FromQuery] string? scenario,
        ICommunicationTemplateService service)
    {
        try
        {
            var templates = await service.GetTemplatesAsync(scenario);
            return Results.Ok(templates);
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
        ICommunicationTemplateService service)
    {
        try
        {
            var template = await service.GetTemplateAsync(templateId);
            if (template == null)
            {
                return Results.NotFound(new { message = $"模板 {templateId} 不存在" });
            }
            return Results.Ok(template);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 根据编号获取模板
    /// </summary>
    private static async Task<IResult> GetTemplateByCode(
        string code,
        ICommunicationTemplateService service)
    {
        try
        {
            var template = await service.GetTemplateByCodeAsync(code);
            if (template == null)
            {
                return Results.NotFound(new { message = $"模板编号 {code} 不存在" });
            }
            return Results.Ok(template);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    private static async Task<IResult> CreateTemplate(
        [FromBody] CreateCommunicationTemplateRequest request,
        HttpContext context,
        ICommunicationTemplateService service)
    {
        try
        {
            var userId = GetUserId(context);
            var templateId = await service.CreateTemplateAsync(request, userId);
            return Results.Created($"/api/communication-templates/{templateId}", new { templateId });
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
        [FromBody] UpdateCommunicationTemplateRequest request,
        ICommunicationTemplateService service)
    {
        try
        {
            await service.UpdateTemplateAsync(templateId, request);
            return Results.Ok(new { message = "模板更新成功" });
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
    /// 删除模板
    /// </summary>
    private static async Task<IResult> DeleteTemplate(
        Guid templateId,
        ICommunicationTemplateService service)
    {
        try
        {
            await service.DeleteTemplateAsync(templateId);
            return Results.Ok(new { message = "模板删除成功" });
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
    /// 渲染模板
    /// </summary>
    private static async Task<IResult> RenderTemplate(
        Guid templateId,
        [FromBody] Dictionary<string, string> variables,
        ICommunicationTemplateService service)
    {
        try
        {
            var content = await service.RenderTemplateAsync(templateId, variables);
            return Results.Ok(new { content });
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
    /// 保存沟通记录
    /// </summary>
    private static async Task<IResult> SaveCommunication(
        [FromBody] CreateCommunicationRequest request,
        HttpContext context,
        ICommunicationTemplateService service)
    {
        try
        {
            var userId = GetUserId(context);
            var communicationId = await service.SaveCommunicationAsync(request, userId);
            return Results.Created($"/api/communication-templates/communications/{communicationId}", new { communicationId });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取工单的沟通记录
    /// </summary>
    private static async Task<IResult> GetTicketCommunications(
        Guid ticketId,
        ICommunicationTemplateService service)
    {
        try
        {
            var communications = await service.GetTicketCommunicationsAsync(ticketId);
            return Results.Ok(communications);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}







