using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Domain.Entities;
using FieldTicket.Shared.Models;
using FieldTicket.Infrastructure.Utils;
using OfficeOpenXml;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 员工批量导入端点
/// </summary>
public static class EmployeeImportEndpoints
{
    public static void MapEmployeeImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .WithTags("Employee Import")
            .RequireAuthorization();

        // POST /api/employees/import/excel - 上传Excel文件导入员工
        group.MapPost("/import/excel", ImportFromExcel)
            .WithName("ImportEmployeesFromExcel")
            .WithOpenApi()
            .DisableAntiforgery(); // 允许文件上传

        // POST /api/employees/import/json - 从JSON数据导入员工
        group.MapPost("/import/json", ImportFromJson)
            .WithName("ImportEmployeesFromJson")
            .WithOpenApi();

        // GET /api/employees/import/template - 下载Excel模板
        group.MapGet("/import/template", DownloadTemplate)
            .WithName("DownloadEmployeeImportTemplate")
            .WithOpenApi();

        // POST /api/employees/{id}/activate - 开通账户
        group.MapPost("/{id}/activate", ActivateAccount)
            .WithName("ActivateEmployeeAccount")
            .WithOpenApi();

        // POST /api/employees/batch/activate - 批量开通账户
        group.MapPost("/batch/activate", BatchActivateAccounts)
            .WithName("BatchActivateEmployeeAccounts")
            .WithOpenApi();
    }

    /// <summary>
    /// 从Excel文件导入员工
    /// </summary>
    private static async Task<IResult> ImportFromExcel(
        IFormFile file,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        [FromQuery] string defaultRole = "FieldEngineer",
        [FromQuery] bool overwriteExisting = false)
    {
        // 验证管理员权限
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        if (currentUserRole != "Admin")
        {
            return Results.Forbid();
        }

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "请上传Excel文件" });
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            return Results.BadRequest(new { message = "文件格式错误，请上传.xlsx或.xls文件" });
        }

        try
        {
            // 设置EPPlus许可证
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var employees = new List<EmployeeImportRow>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                // 读取数据（从第2行开始，第1行是标题）
                int rowCount = worksheet.Dimension?.Rows ?? 0;
                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                    var idCard = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                    var deptName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                    var deptId = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                    var supervisorName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                    var mobile = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                    var email = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                    var role = worksheet.Cells[row, 8].Value?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(idCard))
                    {
                        continue; // 跳过空行
                    }

                    employees.Add(new EmployeeImportRow
                    {
                        RowNumber = row,
                        Name = name ?? "",
                        IdCard = idCard ?? "",
                        DeptName = deptName ?? "",
                        DeptId = deptId,
                        SupervisorName = supervisorName,
                        Mobile = mobile,
                        Email = email,
                        Role = role
                    });
                }
            }

            // 执行导入
            var request = new EmployeeImportRequest
            {
                Employees = employees,
                DefaultRole = defaultRole,
                OverwriteExisting = overwriteExisting
            };

            var result = await ProcessImport(request, dbContext);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: $"导入失败: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// 从JSON数据导入员工
    /// </summary>
    private static async Task<IResult> ImportFromJson(
        [FromBody] EmployeeImportRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext)
    {
        // 验证管理员权限
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        if (currentUserRole != "Admin")
        {
            return Results.Forbid();
        }

        try
        {
            var result = await ProcessImport(request, dbContext);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: $"导入失败: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// 处理导入逻辑
    /// </summary>
    private static async Task<EmployeeImportResult> ProcessImport(
        EmployeeImportRequest request,
        ApplicationDbContext dbContext)
    {
        var result = new EmployeeImportResult
        {
            TotalCount = request.Employees.Count
        };

        // 第一遍：创建所有用户（不设置上级关系）
        var userMap = new Dictionary<string, User>(); // 姓名 -> User

        foreach (var emp in request.Employees)
        {
            try
            {
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(emp.Name))
                {
                    result.ErrorList.Add(new EmployeeImportError
                    {
                        RowNumber = emp.RowNumber,
                        Name = emp.Name,
                        Error = "姓名不能为空"
                    });
                    result.FailureCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(emp.IdCard) || emp.IdCard.Length < 4)
                {
                    result.ErrorList.Add(new EmployeeImportError
                    {
                        RowNumber = emp.RowNumber,
                        Name = emp.Name,
                        Error = "身份证号不能为空且至少4位"
                    });
                    result.FailureCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(emp.DeptName))
                {
                    result.ErrorList.Add(new EmployeeImportError
                    {
                        RowNumber = emp.RowNumber,
                        Name = emp.Name,
                        Error = "部门名称不能为空"
                    });
                    result.FailureCount++;
                    continue;
                }

                // 检查用户是否已存在
                var existingUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == emp.Name);

                if (existingUser != null && !request.OverwriteExisting)
                {
                    result.SkippedCount++;
                    continue;
                }

                // 生成密码：姓名拼音 + 身份证后4位
                var idCardLast4 = emp.IdCard.Substring(emp.IdCard.Length - 4);
                var password = PinyinHelper.GeneratePassword(emp.Name, idCardLast4);

                User user;
                if (existingUser != null)
                {
                    // 更新已存在的用户
                    user = existingUser;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    user.IdCardLastFour = idCardLast4;
                    user.DeptId = emp.DeptId;
                    user.DeptName = emp.DeptName;
                    user.Mobile = emp.Mobile;
                    user.Email = emp.Email;
                    user.Role = emp.Role ?? request.DefaultRole;
                    user.UpdatedAt = DateTime.UtcNow;
                    user.MustChangePassword = true; // 强制首次登录修改密码
                    user.IsActivated = false; // 导入后需要管理员开通
                }
                else
                {
                    // 创建新用户
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = emp.Name, // 登录名 = 姓名
                        Name = emp.Name,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        IdCardLastFour = idCardLast4,
                        LoginType = "Password",
                        DeptId = emp.DeptId,
                        DeptName = emp.DeptName,
                        Mobile = emp.Mobile,
                        Email = emp.Email,
                        Role = emp.Role ?? request.DefaultRole,
                        IsActive = true,
                        IsActivated = false, // 导入后需要管理员开通
                        MustChangePassword = true, // 强制首次登录修改密码
                        LastPasswordChangeAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    dbContext.Users.Add(user);
                }

                userMap[emp.Name] = user;

                result.SuccessList.Add(new EmployeeImportSuccess
                {
                    RowNumber = emp.RowNumber,
                    Name = emp.Name,
                    Username = emp.Name,
                    Password = password,
                    DeptName = emp.DeptName
                });

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.ErrorList.Add(new EmployeeImportError
                {
                    RowNumber = emp.RowNumber,
                    Name = emp.Name,
                    Error = ex.Message
                });
                result.FailureCount++;
            }
        }

        // 保存用户
        await dbContext.SaveChangesAsync();

        // 第二遍：设置上级关系
        foreach (var emp in request.Employees)
        {
            if (!string.IsNullOrWhiteSpace(emp.SupervisorName) && userMap.ContainsKey(emp.Name))
            {
                var user = userMap[emp.Name];

                // 查找上级
                var supervisor = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Name == emp.SupervisorName);

                if (supervisor != null)
                {
                    user.SupervisorId = supervisor.Id;
                    user.SupervisorName = supervisor.Name;
                }
            }
        }

        // 保存上级关系
        await dbContext.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// 下载Excel模板
    /// </summary>
    private static IResult DownloadTemplate()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("员工信息");

            // 设置标题行
            worksheet.Cells[1, 1].Value = "姓名（必填）";
            worksheet.Cells[1, 2].Value = "身份证号（必填）";
            worksheet.Cells[1, 3].Value = "部门名称（必填）";
            worksheet.Cells[1, 4].Value = "部门号（可选）";
            worksheet.Cells[1, 5].Value = "上级姓名（可选）";
            worksheet.Cells[1, 6].Value = "手机号（可选）";
            worksheet.Cells[1, 7].Value = "邮箱（可选）";
            worksheet.Cells[1, 8].Value = "角色（可选）";

            // 设置标题样式
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 添加示例数据
            worksheet.Cells[2, 1].Value = "张三";
            worksheet.Cells[2, 2].Value = "110101199001011234";
            worksheet.Cells[2, 3].Value = "技术部";
            worksheet.Cells[2, 4].Value = "DEPT001";
            worksheet.Cells[2, 5].Value = "李四";
            worksheet.Cells[2, 6].Value = "13800138000";
            worksheet.Cells[2, 7].Value = "zhangsan@example.com";
            worksheet.Cells[2, 8].Value = "FieldEngineer";

            // 添加说明
            worksheet.Cells[4, 1].Value = "说明：";
            worksheet.Cells[5, 1].Value = "1. 姓名将作为登录账号";
            worksheet.Cells[6, 1].Value = "2. 默认密码为：姓名拼音 + 身份证后4位（例如：zhangsan1234）";
            worksheet.Cells[7, 1].Value = "3. 导入后账户默认未开通，需要管理员手动开通";
            worksheet.Cells[8, 1].Value = "4. 用户首次登录后必须修改密码";
            worksheet.Cells[9, 1].Value = "5. 角色可选值：FieldEngineer（现场工程师）、CS（客服）、SeniorEngineer（高级工程师）、Admin（管理员）";

            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();

            var fileBytes = package.GetAsByteArray();

            return Results.File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "员工导入模板.xlsx"
            );
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: $"生成模板失败: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// 开通单个账户
    /// </summary>
    private static async Task<IResult> ActivateAccount(
        Guid id,
        ApplicationDbContext dbContext,
        HttpContext httpContext)
    {
        // 验证管理员权限
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        if (currentUserRole != "Admin")
        {
            return Results.Forbid();
        }

        var user = await dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        if (user.IsActivated)
        {
            return Results.BadRequest(new { message = "账户已开通" });
        }

        user.IsActivated = true;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            message = "账户已开通",
            user = new
            {
                user.Id,
                user.Username,
                user.Name,
                user.DeptName,
                user.IsActivated
            }
        });
    }

    /// <summary>
    /// 批量开通账户
    /// </summary>
    private static async Task<IResult> BatchActivateAccounts(
        [FromBody] List<Guid> userIds,
        ApplicationDbContext dbContext,
        HttpContext httpContext)
    {
        // 验证管理员权限
        var currentUserRole = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        if (currentUserRole != "Admin")
        {
            return Results.Forbid();
        }

        if (userIds == null || userIds.Count == 0)
        {
            return Results.BadRequest(new { message = "请选择要开通的账户" });
        }

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && !u.IsActivated)
            .ToListAsync();

        if (users.Count == 0)
        {
            return Results.BadRequest(new { message = "没有找到待开通的账户" });
        }

        foreach (var user in users)
        {
            user.IsActivated = true;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            message = $"成功开通 {users.Count} 个账户",
            activatedCount = users.Count,
            users = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Name,
                u.DeptName
            }).ToList()
        });
    }
}
