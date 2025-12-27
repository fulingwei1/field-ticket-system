namespace FieldTicket.Shared.Models;

/// <summary>
/// Excel导入的员工数据行
/// </summary>
public class EmployeeImportRow
{
    /// <summary>
    /// 姓名（必填）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 身份证号（必填，用于生成密码）
    /// </summary>
    public string IdCard { get; set; } = string.Empty;

    /// <summary>
    /// 部门号（可选）
    /// </summary>
    public string? DeptId { get; set; }

    /// <summary>
    /// 部门名称（必填）
    /// </summary>
    public string DeptName { get; set; } = string.Empty;

    /// <summary>
    /// 上级姓名（可选）
    /// </summary>
    public string? SupervisorName { get; set; }

    /// <summary>
    /// 手机号（可选）
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 邮箱（可选）
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 角色（可选，默认为FieldEngineer）
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Excel行号（用于错误提示）
    /// </summary>
    public int RowNumber { get; set; }
}

/// <summary>
/// Excel导入请求
/// </summary>
public class EmployeeImportRequest
{
    /// <summary>
    /// 员工数据列表
    /// </summary>
    public List<EmployeeImportRow> Employees { get; set; } = new();

    /// <summary>
    /// 默认角色
    /// </summary>
    public string DefaultRole { get; set; } = "FieldEngineer";

    /// <summary>
    /// 是否覆盖已存在的用户
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// 导入结果
/// </summary>
public class EmployeeImportResult
{
    /// <summary>
    /// 成功导入的数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败的数量
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 跳过的数量（已存在）
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功导入的用户列表
    /// </summary>
    public List<EmployeeImportSuccess> SuccessList { get; set; } = new();

    /// <summary>
    /// 失败的记录列表
    /// </summary>
    public List<EmployeeImportError> ErrorList { get; set; } = new();
}

/// <summary>
/// 导入成功记录
/// </summary>
public class EmployeeImportSuccess
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeptName { get; set; } = string.Empty;
}

/// <summary>
/// 导入错误记录
/// </summary>
public class EmployeeImportError
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
