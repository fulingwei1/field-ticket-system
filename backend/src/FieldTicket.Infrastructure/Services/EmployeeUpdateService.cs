using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 员工批量更新服务
/// </summary>
public class EmployeeUpdateService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EmployeeUpdateService> _logger;

    public EmployeeUpdateService(
        ApplicationDbContext dbContext,
        ILogger<EmployeeUpdateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 批量更新员工信息
    /// </summary>
    public async Task<EmployeeBatchUpdateResult> BatchUpdateAsync(
        EmployeeBatchUpdateRequest request,
        string operatorId)
    {
        var result = new EmployeeBatchUpdateResult
        {
            TotalCount = request.Updates.Count
        };

        foreach (var update in request.Updates)
        {
            try
            {
                var itemResult = await UpdateSingleEmployeeAsync(update, operatorId);
                result.Details.Add(itemResult);

                if (itemResult.Success)
                {
                    result.SuccessCount++;
                }
                else if (itemResult.Message.Contains("未找到"))
                {
                    result.SkippedCount++;
                }
                else
                {
                    result.FailedCount++;
                    result.ErrorMessages.Add($"{update.Identifier}: {itemResult.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {Identifier}", update.Identifier);
                result.FailedCount++;
                result.Details.Add(new EmployeeUpdateItemResult
                {
                    Identifier = update.Identifier,
                    Success = false,
                    Message = $"更新失败: {ex.Message}"
                });
                result.ErrorMessages.Add($"{update.Identifier}: {ex.Message}");
            }
        }

        // 保存所有更改
        if (result.SuccessCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Batch update completed: {SuccessCount} success, {FailedCount} failed, {SkippedCount} skipped",
                result.SuccessCount, result.FailedCount, result.SkippedCount);
        }

        return result;
    }

    /// <summary>
    /// 更新单个员工
    /// </summary>
    private async Task<EmployeeUpdateItemResult> UpdateSingleEmployeeAsync(
        EmployeeUpdateItem update,
        string operatorId)
    {
        // 查找用户
        User? user = null;

        if (update.MatchBy == "Username")
        {
            user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == update.Identifier);
        }
        else if (update.MatchBy == "Name")
        {
            user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Name == update.Identifier);
        }

        if (user == null)
        {
            return new EmployeeUpdateItemResult
            {
                Identifier = update.Identifier,
                Success = false,
                Message = $"未找到匹配的用户 (匹配方式: {update.MatchBy})"
            };
        }

        // 记录更新前的值
        var beforeValues = new Dictionary<string, string?>
        {
            ["部门"] = user.DeptName,
            ["上级"] = user.SupervisorName,
            ["角色"] = user.Role,
            ["手机号"] = user.PhoneNumber,
            ["邮箱"] = user.Email
        };

        var hasChanges = false;

        // 更新部门
        if (update.DeptName != null && user.DeptName != update.DeptName)
        {
            user.DeptName = update.DeptName;
            hasChanges = true;
        }

        // 更新上级
        if (update.SupervisorName != null)
        {
            if (string.IsNullOrWhiteSpace(update.SupervisorName))
            {
                // 清空上级
                user.SupervisorId = null;
                user.SupervisorName = null;
                hasChanges = true;
            }
            else if (user.SupervisorName != update.SupervisorName)
            {
                // 查找新上级
                var supervisor = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Name == update.SupervisorName);

                if (supervisor != null)
                {
                    // 检查不能设置自己为上级
                    if (supervisor.Id == user.Id)
                    {
                        return new EmployeeUpdateItemResult
                        {
                            Identifier = update.Identifier,
                            Name = user.Name,
                            Success = false,
                            Message = "不能设置自己为上级"
                        };
                    }

                    // 检查循环依赖（简单检查：上级的上级不能是自己）
                    if (supervisor.SupervisorId == user.Id)
                    {
                        return new EmployeeUpdateItemResult
                        {
                            Identifier = update.Identifier,
                            Name = user.Name,
                            Success = false,
                            Message = "上下级关系存在循环依赖"
                        };
                    }

                    user.SupervisorId = supervisor.Id;
                    user.SupervisorName = supervisor.Name;
                    hasChanges = true;
                }
                else
                {
                    return new EmployeeUpdateItemResult
                    {
                        Identifier = update.Identifier,
                        Name = user.Name,
                        Success = false,
                        Message = $"未找到上级: {update.SupervisorName}"
                    };
                }
            }
        }

        // 更新角色
        if (update.Role != null && user.Role != update.Role)
        {
            // 验证角色有效性
            var validRoles = new[] { "Admin", "FieldEngineer", "CS", "SeniorEngineer" };
            if (!validRoles.Contains(update.Role))
            {
                return new EmployeeUpdateItemResult
                {
                    Identifier = update.Identifier,
                    Name = user.Name,
                    Success = false,
                    Message = $"无效的角色: {update.Role}"
                };
            }

            user.Role = update.Role;
            hasChanges = true;
        }

        // 更新手机号
        if (update.PhoneNumber != null && user.PhoneNumber != update.PhoneNumber)
        {
            user.PhoneNumber = update.PhoneNumber;
            hasChanges = true;
        }

        // 更新邮箱
        if (update.Email != null && user.Email != update.Email)
        {
            user.Email = update.Email;
            hasChanges = true;
        }

        if (!hasChanges)
        {
            return new EmployeeUpdateItemResult
            {
                Identifier = update.Identifier,
                Name = user.Name,
                Success = true,
                Message = "无需更新（数据相同）",
                BeforeValues = beforeValues,
                AfterValues = beforeValues
            };
        }

        // 记录更新后的值
        var afterValues = new Dictionary<string, string?>
        {
            ["部门"] = user.DeptName,
            ["上级"] = user.SupervisorName,
            ["角色"] = user.Role,
            ["手机号"] = user.PhoneNumber,
            ["邮箱"] = user.Email
        };

        _logger.LogInformation("Updated employee {UserId}: {Changes}",
            user.Id, string.Join(", ", afterValues.Where(kv => beforeValues[kv.Key] != kv.Value).Select(kv => kv.Key)));

        return new EmployeeUpdateItemResult
        {
            Identifier = update.Identifier,
            Name = user.Name,
            Success = true,
            Message = "更新成功",
            BeforeValues = beforeValues,
            AfterValues = afterValues
        };
    }

    /// <summary>
    /// 生成包含现有数据的更新模板（Excel）
    /// </summary>
    public async Task<(byte[] FileContent, string FileName)> GenerateUpdateTemplateAsync(
        EmployeeUpdateTemplateRequest request)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // 查询用户
        var query = _dbContext.Users
            .Where(u => u.LoginType == "Password") // 只导出密码登录用户
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.DeptName))
        {
            query = query.Where(u => u.DeptName == request.DeptName);
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            query = query.Where(u => u.Role == request.Role);
        }

        if (request.OnlyInactivated)
        {
            query = query.Where(u => !u.IsActivated);
        }

        var users = await query.OrderBy(u => u.DeptName).ThenBy(u => u.Name).ToListAsync();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("员工更新模板");

        // 设置说明行
        worksheet.Cells[1, 1].Value = "员工批量更新模板";
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;

        worksheet.Cells[2, 1].Value = "说明：请修改需要更新的字段，保存后重新导入。用户名列不可修改，用于匹配员工。";
        worksheet.Cells[2, 1].Style.Font.Color.SetColor(System.Drawing.Color.Red);
        worksheet.Cells[2, 1].Style.Font.Size = 10;

        // 设置标题行（第4行）
        var headers = new[] { "用户名（匹配标识）", "姓名", "部门", "上级姓名", "角色", "手机号", "邮箱" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[4, i + 1].Value = headers[i];
            worksheet.Cells[4, i + 1].Style.Font.Bold = true;
            worksheet.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
        }

        // 填充数据
        int row = 5;
        foreach (var user in users)
        {
            worksheet.Cells[row, 1].Value = user.Username;
            worksheet.Cells[row, 2].Value = user.Name;
            worksheet.Cells[row, 3].Value = user.DeptName;
            worksheet.Cells[row, 4].Value = user.SupervisorName;
            worksheet.Cells[row, 5].Value = user.Role;
            worksheet.Cells[row, 6].Value = user.PhoneNumber;
            worksheet.Cells[row, 7].Value = user.Email;

            // 用户名列设为只读样式（灰色背景）
            worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            worksheet.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkGray);

            row++;
        }

        // 自动调整列宽
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // 冻结前4行
        worksheet.View.FreezePanes(5, 1);

        var fileContent = await package.GetAsByteArrayAsync();
        var fileName = $"员工更新模板_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return (fileContent, fileName);
    }

    /// <summary>
    /// 从Excel文件解析更新数据
    /// </summary>
    public async Task<List<EmployeeUpdateItem>> ParseUpdateFromExcelAsync(Stream fileStream)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var updates = new List<EmployeeUpdateItem>();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets[0];

        if (worksheet == null)
        {
            throw new InvalidOperationException("Excel文件为空或格式不正确");
        }

        // 从第5行开始读取数据（跳过说明和标题）
        int rowCount = worksheet.Dimension?.Rows ?? 0;

        for (int row = 5; row <= rowCount; row++)
        {
            var username = worksheet.Cells[row, 1].Value?.ToString()?.Trim();

            if (string.IsNullOrEmpty(username))
            {
                continue; // 跳过空行
            }

            var update = new EmployeeUpdateItem
            {
                Identifier = username,
                MatchBy = "Username",
                DeptName = worksheet.Cells[row, 3].Value?.ToString()?.Trim(),
                SupervisorName = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                Role = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                PhoneNumber = worksheet.Cells[row, 6].Value?.ToString()?.Trim(),
                Email = worksheet.Cells[row, 7].Value?.ToString()?.Trim()
            };

            updates.Add(update);
        }

        return await Task.FromResult(updates);
    }
}
