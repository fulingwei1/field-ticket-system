namespace FieldTicket.Shared.Models;

/// <summary>
/// 员工批量更新请求
/// </summary>
public class EmployeeBatchUpdateRequest
{
    /// <summary>
    /// 更新项列表
    /// </summary>
    public List<EmployeeUpdateItem> Updates { get; set; } = new();

    /// <summary>
    /// 是否覆盖现有数据（如果用户已存在）
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;
}

/// <summary>
/// 单个员工更新项
/// </summary>
public class EmployeeUpdateItem
{
    /// <summary>
    /// 匹配标识（用户名或姓名）
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 匹配方式：Username 或 Name
    /// </summary>
    public string MatchBy { get; set; } = "Username"; // Username, Name

    /// <summary>
    /// 新的部门名称（null表示不更新）
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// 新的上级姓名（null表示不更新）
    /// </summary>
    public string? SupervisorName { get; set; }

    /// <summary>
    /// 新的角色（null表示不更新）
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// 新的手机号（null表示不更新）
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 新的邮箱（null表示不更新）
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// 员工批量更新结果
/// </summary>
public class EmployeeBatchUpdateResult
{
    /// <summary>
    /// 总处理数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功更新数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 跳过数（未找到匹配用户）
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 详细结果
    /// </summary>
    public List<EmployeeUpdateItemResult> Details { get; set; } = new();

    /// <summary>
    /// 错误消息汇总
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// 单个员工更新结果
/// </summary>
public class EmployeeUpdateItemResult
{
    /// <summary>
    /// 标识符
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 更新前的值
    /// </summary>
    public Dictionary<string, string?> BeforeValues { get; set; } = new();

    /// <summary>
    /// 更新后的值
    /// </summary>
    public Dictionary<string, string?> AfterValues { get; set; } = new();
}

/// <summary>
/// 导出包含现有数据的更新模板请求
/// </summary>
public class EmployeeUpdateTemplateRequest
{
    /// <summary>
    /// 筛选条件：部门
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// 筛选条件：角色
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// 是否只导出未开通账户
    /// </summary>
    public bool OnlyInactivated { get; set; } = false;
}

/// <summary>
/// 员工更新模板DTO（用于Excel导出）
/// </summary>
public class EmployeeUpdateTemplateDto
{
    /// <summary>
    /// 用户名（匹配标识，不可修改）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 姓名（当前值）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 部门（可修改）
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// 上级姓名（可修改）
    /// </summary>
    public string? SupervisorName { get; set; }

    /// <summary>
    /// 角色（可修改）
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 手机号（可修改）
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 邮箱（可修改）
    /// </summary>
    public string? Email { get; set; }
}
