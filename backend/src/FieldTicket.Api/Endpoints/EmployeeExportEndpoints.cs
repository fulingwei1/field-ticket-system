using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 员工数据导出API端点
/// </summary>
public static class EmployeeExportEndpoints
{
    public static void MapEmployeeExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees/export")
            .WithTags("员工导出")
            .RequireAuthorization();

        // 导出员工数据（Excel或CSV）
        group.MapPost("/", ExportEmployees)
            .WithName("ExportEmployees")
            .WithDescription("导出员工数据为Excel或CSV格式")
            .Produces<FileContentResult>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        // 获取导出统计信息（用于预览）
        group.MapPost("/preview", PreviewExportStats)
            .WithName("PreviewExportStats")
            .WithDescription("预览导出数据统计信息")
            .Produces<ExportPreviewResult>(200);
    }

    /// <summary>
    /// 导出员工数据
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> ExportEmployees(
        [FromBody] EmployeeExportRequest request,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] EmployeeExportService exportService,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Admin exporting employees with filters: Format={Format}, Dept={Dept}, Role={Role}",
                request.Format, request.DeptName, request.Role);

            // 构建查询
            var query = dbContext.Users.AsQueryable();

            // 应用筛选条件
            if (!string.IsNullOrEmpty(request.DeptName))
            {
                query = query.Where(u => u.DeptName == request.DeptName);
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                query = query.Where(u => u.Role == request.Role);
            }

            if (request.IsActivated.HasValue)
            {
                query = query.Where(u => u.IsActivated == request.IsActivated.Value);
            }

            if (!string.IsNullOrEmpty(request.LoginType))
            {
                query = query.Where(u => u.LoginType == request.LoginType);
            }

            if (!request.IncludeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            // 排序：按部门、姓名
            query = query.OrderBy(u => u.DeptName).ThenBy(u => u.Name);

            // 获取数据
            var users = await query.ToListAsync();

            if (!users.Any())
            {
                logger.LogWarning("No employees found matching the export criteria");
                return Results.BadRequest(new { message = "没有符合条件的员工数据" });
            }

            // 转换为导出DTO
            var exportData = users.Select(u => exportService.MapToExportDto(u)).ToList();

            // 根据格式生成文件
            byte[] fileContent;
            string fileName;
            string contentType;

            if (request.Format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            {
                (fileContent, fileName) = await exportService.ExportToCsvAsync(exportData);
                contentType = "text/csv";
            }
            else // 默认Excel
            {
                var filterDesc = exportService.GenerateFilterDescription(request);
                (fileContent, fileName) = await exportService.ExportToExcelAsync(exportData, filterDesc);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            logger.LogInformation("Successfully exported {Count} employees to {Format}", users.Count, request.Format);

            // 返回文件
            return Results.File(
                fileContent,
                contentType,
                fileName,
                enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting employees");
            return Results.Problem(
                title: "导出失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// 预览导出统计信息
    /// </summary>
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> PreviewExportStats(
        [FromBody] EmployeeExportRequest request,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            // 构建查询（与导出相同的逻辑）
            var query = dbContext.Users.AsQueryable();

            if (!string.IsNullOrEmpty(request.DeptName))
                query = query.Where(u => u.DeptName == request.DeptName);

            if (!string.IsNullOrEmpty(request.Role))
                query = query.Where(u => u.Role == request.Role);

            if (request.IsActivated.HasValue)
                query = query.Where(u => u.IsActivated == request.IsActivated.Value);

            if (!string.IsNullOrEmpty(request.LoginType))
                query = query.Where(u => u.LoginType == request.LoginType);

            if (!request.IncludeInactive)
                query = query.Where(u => u.IsActive);

            // 获取统计数据
            var totalCount = await query.CountAsync();
            var activatedCount = await query.CountAsync(u => u.IsActivated);
            var inactivatedCount = await query.CountAsync(u => !u.IsActivated);

            // 按部门统计
            var deptStats = await query
                .Where(u => u.DeptName != null)
                .GroupBy(u => u.DeptName)
                .Select(g => new DepartmentStat
                {
                    DeptName = g.Key!,
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToListAsync();

            // 按角色统计
            var roleStats = await query
                .GroupBy(u => u.Role)
                .Select(g => new RoleStat
                {
                    Role = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(r => r.Count)
                .ToListAsync();

            var result = new ExportPreviewResult
            {
                TotalCount = totalCount,
                ActivatedCount = activatedCount,
                InactivatedCount = inactivatedCount,
                DepartmentStats = deptStats,
                RoleStats = roleStats
            };

            logger.LogInformation("Export preview: {TotalCount} employees", totalCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error previewing export stats");
            return Results.Problem(
                title: "预览失败",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

/// <summary>
/// 导出预览结果
/// </summary>
public class ExportPreviewResult
{
    public int TotalCount { get; set; }
    public int ActivatedCount { get; set; }
    public int InactivatedCount { get; set; }
    public List<DepartmentStat> DepartmentStats { get; set; } = new();
    public List<RoleStat> RoleStats { get; set; } = new();
}

public class DepartmentStat
{
    public string DeptName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RoleStat
{
    public string Role { get; set; } = string.Empty;
    public int Count { get; set; }
}
