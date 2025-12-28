using System.Text;
using FieldTicket.Domain.Entities;
using FieldTicket.Shared.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 员工数据导出服务
/// </summary>
public class EmployeeExportService
{
    /// <summary>
    /// 导出为Excel格式
    /// </summary>
    public async Task<(byte[] FileContent, string FileName)> ExportToExcelAsync(
        List<EmployeeExportDto> employees,
        string? filterDescription = null)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("员工数据");

        // 设置标题行
        var headers = new[]
        {
            "用户名", "姓名", "角色", "部门", "上级",
            "手机号", "邮箱", "身份证后4位", "登录方式",
            "开通状态", "账户状态", "创建时间", "最后登录"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        // 填充数据
        int row = 2;
        foreach (var emp in employees)
        {
            worksheet.Cells[row, 1].Value = emp.Username;
            worksheet.Cells[row, 2].Value = emp.Name;
            worksheet.Cells[row, 3].Value = emp.Role;
            worksheet.Cells[row, 4].Value = emp.DeptName;
            worksheet.Cells[row, 5].Value = emp.SupervisorName;
            worksheet.Cells[row, 6].Value = emp.PhoneNumber;
            worksheet.Cells[row, 7].Value = emp.Email;
            worksheet.Cells[row, 8].Value = emp.IdCardLastFour;
            worksheet.Cells[row, 9].Value = emp.LoginType;
            worksheet.Cells[row, 10].Value = emp.IsActivated;
            worksheet.Cells[row, 11].Value = emp.IsActive;
            worksheet.Cells[row, 12].Value = emp.CreatedAt;
            worksheet.Cells[row, 13].Value = emp.LastLoginAt;
            row++;
        }

        // 自动调整列宽
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // 添加筛选器
        worksheet.Cells[1, 1, 1, headers.Length].AutoFilter = true;

        // 添加统计信息（在数据下方）
        if (employees.Any())
        {
            row += 2;
            worksheet.Cells[row, 1].Value = "导出统计";
            worksheet.Cells[row, 1].Style.Font.Bold = true;

            row++;
            worksheet.Cells[row, 1].Value = "总人数:";
            worksheet.Cells[row, 2].Value = employees.Count;

            row++;
            worksheet.Cells[row, 1].Value = "已开通:";
            worksheet.Cells[row, 2].Value = employees.Count(e => e.IsActivated == "已开通");

            row++;
            worksheet.Cells[row, 1].Value = "未开通:";
            worksheet.Cells[row, 2].Value = employees.Count(e => e.IsActivated == "未开通");

            // 按部门统计
            var deptStats = employees
                .Where(e => !string.IsNullOrEmpty(e.DeptName))
                .GroupBy(e => e.DeptName)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (deptStats.Any())
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "部门人数统计";
                worksheet.Cells[row, 1].Style.Font.Bold = true;

                foreach (var dept in deptStats)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = dept.Key;
                    worksheet.Cells[row, 2].Value = dept.Count();
                }
            }

            if (!string.IsNullOrEmpty(filterDescription))
            {
                row += 2;
                worksheet.Cells[row, 1].Value = "筛选条件:";
                worksheet.Cells[row, 2].Value = filterDescription;
            }
        }

        var fileContent = await package.GetAsByteArrayAsync();
        var fileName = $"员工数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return (fileContent, fileName);
    }

    /// <summary>
    /// 导出为CSV格式
    /// </summary>
    public async Task<(byte[] FileContent, string FileName)> ExportToCsvAsync(
        List<EmployeeExportDto> employees)
    {
        var csv = new StringBuilder();

        // 添加BOM以支持中文
        csv.Append('\ufeff');

        // 标题行
        csv.AppendLine("用户名,姓名,角色,部门,上级,手机号,邮箱,身份证后4位,登录方式,开通状态,账户状态,创建时间,最后登录");

        // 数据行
        foreach (var emp in employees)
        {
            csv.AppendLine($"{EscapeCsv(emp.Username)},{EscapeCsv(emp.Name)},{EscapeCsv(emp.Role)}," +
                          $"{EscapeCsv(emp.DeptName)},{EscapeCsv(emp.SupervisorName)},{EscapeCsv(emp.PhoneNumber)}," +
                          $"{EscapeCsv(emp.Email)},{EscapeCsv(emp.IdCardLastFour)},{EscapeCsv(emp.LoginType)}," +
                          $"{EscapeCsv(emp.IsActivated)},{EscapeCsv(emp.IsActive)},{EscapeCsv(emp.CreatedAt)}," +
                          $"{EscapeCsv(emp.LastLoginAt)}");
        }

        var fileContent = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"员工数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return await Task.FromResult((fileContent, fileName));
    }

    /// <summary>
    /// 转换用户实体为导出DTO
    /// </summary>
    public EmployeeExportDto MapToExportDto(User user)
    {
        return new EmployeeExportDto
        {
            Username = user.Username,
            Name = user.Name,
            Role = user.Role,
            DeptName = user.DeptName,
            SupervisorName = user.SupervisorName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            IdCardLastFour = user.IdCardLastFour,
            LoginType = user.LoginType,
            IsActivated = user.IsActivated ? "已开通" : "未开通",
            IsActive = user.IsActive ? "正常" : "已停用",
            CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// CSV字段转义（处理包含逗号、引号、换行的情况）
    /// </summary>
    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // 如果包含逗号、引号或换行符，需要用双引号包围，并且内部的双引号要转义
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// 生成筛选条件描述
    /// </summary>
    public string GenerateFilterDescription(EmployeeExportRequest request)
    {
        var filters = new List<string>();

        if (!string.IsNullOrEmpty(request.DeptName))
            filters.Add($"部门={request.DeptName}");

        if (!string.IsNullOrEmpty(request.Role))
            filters.Add($"角色={request.Role}");

        if (request.IsActivated.HasValue)
            filters.Add($"开通状态={( request.IsActivated.Value ? "已开通" : "未开通")}");

        if (!string.IsNullOrEmpty(request.LoginType))
            filters.Add($"登录方式={request.LoginType}");

        if (!request.IncludeInactive)
            filters.Add("仅正常账户");

        return filters.Any() ? string.Join(", ", filters) : "全部数据";
    }
}
