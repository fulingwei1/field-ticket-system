using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 工单导出相关端点
/// </summary>
public static class TicketExportEndpoints
{
    public static void MapTicketExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets/export")
            .WithTags("TicketExport")
            .RequireAuthorization();

        // 导出工单列表（Excel）
        group.MapGet("excel", ExportToExcel)
            .WithName("ExportToExcel")
            .WithSummary("导出工单列表（Excel格式）");

        // 导出工单列表（CSV）
        group.MapGet("csv", ExportToCsv)
            .WithName("ExportToCsv")
            .WithSummary("导出工单列表（CSV格式）");

        // 导出工单详情（Excel）
        group.MapGet("{ticketId:guid}/excel", ExportTicketDetailToExcel)
            .WithName("ExportTicketDetailToExcel")
            .WithSummary("导出工单详情（Excel格式）");
    }

    /// <summary>
    /// 导出工单列表（Excel）
    /// </summary>
    private static async Task<IResult> ExportToExcel(
        [FromQuery] string? statuses,
        [FromQuery] Guid? customerId,
        [FromQuery] string? deviceSn,
        [FromQuery] char? domain,
        [FromQuery] string? priority,
        [FromQuery] Guid? createdBy,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? fields,
        ITicketExportService service)
    {
        try
        {
            var filter = new TicketQueryFilter
            {
                Statuses = string.IsNullOrEmpty(statuses) ? null : statuses.Split(',').ToList(),
                CustomerId = customerId,
                DeviceSn = deviceSn,
                Domain = domain,
                Priority = priority,
                CreatedBy = createdBy,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var fieldList = string.IsNullOrEmpty(fields)
                ? null
                : fields.Split(',').ToList();

            var data = await service.ExportToExcelAsync(filter, fieldList);

            var fileName = $"工单列表_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(
                data,
                "text/csv; charset=utf-8",
                fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出工单列表（CSV）
    /// </summary>
    private static async Task<IResult> ExportToCsv(
        [FromQuery] string? statuses,
        [FromQuery] Guid? customerId,
        [FromQuery] string? deviceSn,
        [FromQuery] char? domain,
        [FromQuery] string? priority,
        [FromQuery] Guid? createdBy,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? fields,
        ITicketExportService service)
    {
        try
        {
            var filter = new TicketQueryFilter
            {
                Statuses = string.IsNullOrEmpty(statuses) ? null : statuses.Split(',').ToList(),
                CustomerId = customerId,
                DeviceSn = deviceSn,
                Domain = domain,
                Priority = priority,
                CreatedBy = createdBy,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var fieldList = string.IsNullOrEmpty(fields)
                ? null
                : fields.Split(',').ToList();

            var data = await service.ExportToCsvAsync(filter, fieldList);

            var fileName = $"工单列表_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(
                data,
                "text/csv; charset=utf-8",
                fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 导出工单详情（Excel）
    /// </summary>
    private static async Task<IResult> ExportTicketDetailToExcel(
        Guid ticketId,
        ITicketExportService service)
    {
        try
        {
            var data = await service.ExportTicketDetailToExcelAsync(ticketId);

            var fileName = $"工单详情_{ticketId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return Results.File(
                data,
                "text/csv; charset=utf-8",
                fileName);
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


















