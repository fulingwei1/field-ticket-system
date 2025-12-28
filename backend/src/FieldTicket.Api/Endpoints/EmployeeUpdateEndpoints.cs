using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 员工批量更新API端点
/// </summary>
public static class EmployeeUpdateEndpoints
{
    public static void MapEmployeeUpdateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees/update")
            .WithTags("员工更新")
            .RequireAuthorization();

        // 批量更新员工信息
        group.MapPost("/batch", BatchUpdateEmployees)
            .WithName("BatchUpdateEmployees")
            .WithDescription("批量更新员工信息")
            .Produces<EmployeeBatchUpdateResult>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        // 下载更新模板（包含现有数据）
        group.MapPost("/template", GenerateUpdateTemplate)
            .WithName("GenerateUpdateTemplate")
            .WithDescription("生成包含现有员工数据的更新模板")
            .Produces<FileContentResult>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        // 从Excel解析更新数据
        group.MapPost("/parse", ParseUpdateFromExcel)
            .WithName("ParseUpdateFromExcel")
            .WithDescription("从Excel文件解析更新数据")
            .Produces<List<EmployeeUpdateItem>>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .DisableAntiforgery();
    }

    /// <summary>
    /// 批量更新员工信息
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> BatchUpdateEmployees(
        [FromBody] EmployeeBatchUpdateRequest request,
        [FromServices] EmployeeUpdateService updateService,
        [FromServices] ILogger<Program> logger,
        ClaimsPrincipal user)
    {
        try
        {
            var operatorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

            logger.LogInformation("Admin {OperatorId} batch updating {Count} employees",
                operatorId, request.Updates.Count);

            var result = await updateService.BatchUpdateAsync(request, operatorId);

            logger.LogInformation("Batch update completed: {SuccessCount} success, {FailedCount} failed, {SkippedCount} skipped",
                result.SuccessCount, result.FailedCount, result.SkippedCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error batch updating employees");
            return Results.Problem(
                title: "批量更新失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 生成更新模板
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GenerateUpdateTemplate(
        [FromBody] EmployeeUpdateTemplateRequest request,
        [FromServices] EmployeeUpdateService updateService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Generating employee update template with filters: Dept={Dept}, Role={Role}",
                request.DeptName, request.Role);

            var (fileContent, fileName) = await updateService.GenerateUpdateTemplateAsync(request);

            logger.LogInformation("Generated update template: {FileName}", fileName);

            return Results.File(
                fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName,
                enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating update template");
            return Results.Problem(
                title: "生成模板失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 从Excel解析更新数据
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> ParseUpdateFromExcel(
        IFormFile file,
        [FromServices] EmployeeUpdateService updateService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            // 验证文件
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { message = "请上传文件" });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return Results.BadRequest(new { message = "只支持Excel文件格式（.xlsx, .xls）" });
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB
            {
                return Results.BadRequest(new { message = "文件大小不能超过10MB" });
            }

            logger.LogInformation("Parsing employee update from file: {FileName}", file.FileName);

            using var stream = file.OpenReadStream();
            var updates = await updateService.ParseUpdateFromExcelAsync(stream);

            logger.LogInformation("Parsed {Count} employee updates from Excel", updates.Count);

            return Results.Ok(updates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing update from Excel");
            return Results.Problem(
                title: "解析文件失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
