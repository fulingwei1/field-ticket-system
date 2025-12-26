using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// Excel导入端点
/// </summary>
public static class ExcelImportEndpoints
{
    public static void MapExcelImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/excel-import")
            .WithTags("Excel导入")
            .RequireAuthorization();

        // 解析Excel文件（预览）
        group.MapPost("parse", async (
            [FromForm] IFormFile file,
            IExcelImportService excelImportService) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { message = "请选择要上传的Excel文件" });
                }

                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return Results.BadRequest(new { message = "只支持Excel文件格式（.xlsx, .xls）" });
                }

                using var stream = file.OpenReadStream();
                var result = await excelImportService.ParseExcelAsync(stream, file.FileName);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("ParseExcel")
        .WithSummary("解析Excel文件（预览）")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data");

        // 验证导入数据
        group.MapPost("validate", async (
            [FromBody] ExcelImportResult importResult,
            IExcelImportService excelImportService) =>
        {
            try
            {
                var validationResult = await excelImportService.ValidateImportDataAsync(importResult);
                return Results.Ok(validationResult);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("ValidateImportData")
        .WithSummary("验证导入数据");

        // 执行导入
        group.MapPost("execute", async (
            [FromBody] ExcelImportRequest request,
            HttpContext context,
            IExcelImportService excelImportService) =>
        {
            try
            {
                var userId = GetUserId(context);
                if (userId == null)
                {
                    return Results.Unauthorized();
                }

                var executionResult = await excelImportService.ExecuteImportAsync(request.ImportResult, userId.Value);
                return Results.Ok(executionResult);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("ExecuteImport")
        .WithSummary("执行导入");

        // 下载Excel模板
        group.MapGet("template", async (IExcelImportService excelImportService) =>
        {
            try
            {
                var templateBytes = await excelImportService.GenerateTemplateAsync();
                return Results.File(
                    templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "现场问题导入模板.xlsx");
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("DownloadTemplate")
        .WithSummary("下载Excel模板");
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("user_id")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Excel导入请求
/// </summary>
public class ExcelImportRequest
{
    public ExcelImportResult ImportResult { get; set; } = null!;
}







