using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单相关 API 端点
/// </summary>
public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets").WithTags("Tickets").RequireAuthorization();

        // 创建工单草稿
        group.MapPost("", async (
            [FromBody] CreateTicketRequest request,
            HttpContext context,
            ITicketService ticketService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // 获取幂等键
            var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

            try
            {
                var ticket = await ticketService.CreateDraftAsync(request, userId.Value, idempotencyKey);
                return Results.Ok(ticket);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("CreateTicket")
        .WithSummary("创建工单草稿")
        .Produces<TicketDto>()
        .Produces(StatusCodes.Status500InternalServerError);

        // 更新工单草稿
        group.MapPut("{id:guid}", async (
            Guid id,
            [FromBody] UpdateTicketRequest request,
            HttpContext context,
            ITicketService ticketService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var ticket = await ticketService.UpdateDraftAsync(id, request, userId.Value);
                return Results.Ok(ticket);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
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
        })
        .WithName("UpdateTicket")
        .WithSummary("更新工单草稿")
        .Produces<TicketDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // 提交工单
        group.MapPost("{id:guid}/submit", async (
            Guid id,
            HttpContext context,
            ITicketService ticketService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var ticket = await ticketService.SubmitTicketAsync(id, userId.Value);
                return Results.Ok(new { success = true, ticketNo = ticket.TicketNo, status = ticket.Status });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (FieldTicket.Infrastructure.Services.ValidationException ex)
            {
                return Results.Json(
                    new
                    {
                        code = "VALIDATION_FAILED",
                        message = "提交校验失败",
                        errors = ex.Errors
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity
                );
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("SubmitTicket")
        .WithSummary("提交工单")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status404NotFound);

        // 获取工单详情
        group.MapGet("{id:guid}", async (
            Guid id,
            HttpContext context,
            ITicketService ticketService,
            IRecentChangeAssociationService? recentChangeService) =>
        {
            var userId = GetUserId(context);
            var ticket = await ticketService.GetTicketAsync(id, userId);

            if (ticket == null)
            {
                return Results.NotFound();
            }

            // 如果工单已提交，自动查找相关变更
            List<RelatedChangeDto>? relatedChanges = null;
            if (ticket.Status != "Draft" && recentChangeService != null)
            {
                try
                {
                    relatedChanges = await recentChangeService.GetRelatedChangesAsync(id);
                }
                catch (Exception ex)
                {
                    // 记录错误但不影响工单详情返回
                    // 可以在这里添加日志
                }
            }

            return Results.Ok(new
            {
                ticket,
                relatedChanges
            });
        })
        .WithName("GetTicket")
        .WithSummary("获取工单详情（包含相关变更）")
        .Produces<TicketDto>()
        .Produces(StatusCodes.Status404NotFound);

        // 获取工单列表
        group.MapGet("", async (
            [FromQuery] List<string>? status,
            [FromQuery] Guid? customerId,
            [FromQuery] string? deviceSn,
            [FromQuery] char? domain,
            [FromQuery] string? priority,
            [FromQuery] Guid? createdBy,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            HttpContext context,
            ITicketService ticketService) =>
        {
            var userId = GetUserId(context);
            
            // 如果是 FieldEngineer，只能看自己创建的工单
            var filter = new TicketQueryFilter
            {
                Statuses = status,
                CustomerId = customerId,
                DeviceSn = deviceSn,
                Domain = domain,
                Priority = priority,
                CreatedBy = createdBy ?? userId, // 默认只看自己的
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var (items, total) = await ticketService.GetTicketsAsync(filter, page, pageSize);

            return Results.Ok(new { items, total, page, pageSize });
        })
        .WithName("GetTickets")
        .WithSummary("获取工单列表")
        .Produces<object>();

        // 获取工单缺失信息分析
        group.MapGet("{ticketId:guid}/missing-info", async (
            Guid ticketId,
            [FromQuery] string? jcCode,
            HttpContext context,
            IMissingInfoAnalysisService missingInfoService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var missingInfo = await missingInfoService.AnalyzeMissingInfoAsync(ticketId, jcCode);
                var questions = await missingInfoService.GenerateQuestionnaireAsync(missingInfo);

                var result = new MissingInfoAnalysisResult
                {
                    MissingInfo = missingInfo,
                    Questions = questions,
                    HasCriticalMissing = missingInfo.Any(m => m.Required)
                };

                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("GetMissingInfo")
        .WithSummary("获取工单缺失信息分析")
        .Produces<MissingInfoAnalysisResult>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // 补全缺失信息
        group.MapPost("{ticketId:guid}/complete-missing-info", async (
            Guid ticketId,
            [FromBody] CompleteMissingInfoRequest request,
            HttpContext context,
            ITicketService ticketService,
            IMissingInfoAnalysisService missingInfoService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                // 获取工单详情
                var ticket = await ticketService.GetTicketAsync(ticketId, userId);
                if (ticket == null)
                {
                    return Results.NotFound();
                }

                // 获取缺失信息分析，以确定字段映射
                var missingInfo = await missingInfoService.AnalyzeMissingInfoAsync(ticketId, ticket.CurrentJcCode);
                var questions = await missingInfoService.GenerateQuestionnaireAsync(missingInfo);

                // 构建字段映射（QuestionId -> Field）
                var fieldMapping = questions.ToDictionary(q => q.QuestionId, q => q.Field ?? string.Empty);

                // 合并答案到现有的 FactsJson
                var factsDict = new Dictionary<string, object>();

                // 将现有 FactsJson 转换为字典
                if (ticket.FactsJson.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in ticket.FactsJson.RootElement.EnumerateObject())
                    {
                        factsDict[prop.Name] = System.Text.Json.JsonSerializer.Deserialize<object>(prop.Value.GetRawText()) ?? string.Empty;
                    }
                }

                // 添加新答案
                foreach (var answer in request.Answers)
                {
                    if (fieldMapping.TryGetValue(answer.Key, out var field) && !string.IsNullOrEmpty(field))
                    {
                        // 支持嵌套字段（如 "domain.mechanical.action_completed"）
                        var parts = field.Split('.');
                        if (parts.Length == 1)
                        {
                            factsDict[field] = answer.Value;
                        }
                        else
                        {
                            // 处理嵌套字段：创建嵌套字典结构
                            var current = factsDict;
                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if (!current.ContainsKey(parts[i]) || current[parts[i]] is not Dictionary<string, object>)
                                {
                                    current[parts[i]] = new Dictionary<string, object>();
                                }
                                current = (Dictionary<string, object>)current[parts[i]]!;
                            }
                            current[parts[parts.Length - 1]] = answer.Value;
                        }
                    }
                }

                // 将字典转换回 JsonDocument
                var mergedFactsJson = System.Text.Json.JsonSerializer.SerializeToDocument(factsDict);

                // 更新工单的事实表数据
                var updateRequest = new UpdateTicketRequest
                {
                    FactsJson = mergedFactsJson
                };

                var updatedTicket = await ticketService.UpdateDraftAsync(ticketId, updateRequest, userId.Value);
                return Results.Ok(updatedTicket);
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
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("CompleteMissingInfo")
        .WithSummary("补全缺失信息")
        .Produces<TicketDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        // AI深度分析缺失信息
        group.MapPost("{ticketId:guid}/ai-analysis/deep", async (
            Guid ticketId,
            [FromQuery] string? jcCode,
            HttpContext context,
            IAIDeepAnalysisService aiDeepAnalysisService) =>
        {
            var userId = GetUserId(context);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var result = await aiDeepAnalysisService.AnalyzeTicketAsync(ticketId, jcCode);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("AIDeepAnalysis")
        .WithSummary("AI深度分析缺失信息")
        .Produces<DeepAnalysisResult>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // 获取工单的沟通记录（统一端点，支持小程序）
        group.MapGet("{ticketId:guid}/communications", async (
            Guid ticketId,
            ICommunicationTemplateService communicationService) =>
        {
            try
            {
                var communications = await communicationService.GetTicketCommunicationsAsync(ticketId);
                return Results.Ok(communications);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetTicketCommunications")
        .WithSummary("获取工单的沟通记录")
        .Produces<List<CustomerCommunicationDto>>()
        .Produces(StatusCodes.Status500InternalServerError);

        // 创建沟通记录（统一端点，支持小程序）
        group.MapPost("{ticketId:guid}/communications", async (
            Guid ticketId,
            [FromBody] CreateCommunicationRequest request,
            HttpContext context,
            ICommunicationTemplateService communicationService) =>
        {
            try
            {
                var userId = GetUserId(context);
                if (userId == null)
                {
                    return Results.Unauthorized();
                }

                // 设置工单ID
                request.TicketId = ticketId;
                
                var communicationId = await communicationService.SaveCommunicationAsync(request, userId.Value);
                return Results.Created($"/api/tickets/{ticketId}/communications/{communicationId}", new { communicationId });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("CreateTicketCommunication")
        .WithSummary("创建沟通记录")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);
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

