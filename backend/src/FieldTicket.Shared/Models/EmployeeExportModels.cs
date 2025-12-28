namespace FieldTicket.Shared.Models;

/// <summary>
/// 员工导出请求
/// </summary>
public class EmployeeExportRequest
{
    /// <summary>
    /// 导出格式：Excel 或 CSV
    /// </summary>
    public string Format { get; set; } = "Excel"; // Excel, CSV

    /// <summary>
    /// 筛选条件：部门名称
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// 筛选条件：角色
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// 筛选条件：开通状态（null=全部, true=已开通, false=未开通）
    /// </summary>
    public bool? IsActivated { get; set; }

    /// <summary>
    /// 筛选条件：登录类型
    /// </summary>
    public string? LoginType { get; set; }

    /// <summary>
    /// 是否包含已停用账户
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// 导出字段选择（为空表示导出所有字段）
    /// </summary>
    public List<string>? Fields { get; set; }
}

/// <summary>
/// 员工导出DTO（用于Excel/CSV）
/// </summary>
public class EmployeeExportDto
{
    /// <summary>
    /// 用户名（登录账号）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 部门
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// 上级姓名
    /// </summary>
    public string? SupervisorName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 身份证后4位
    /// </summary>
    public string? IdCardLastFour { get; set; }

    /// <summary>
    /// 登录类型
    /// </summary>
    public string LoginType { get; set; } = string.Empty;

    /// <summary>
    /// 开通状态
    /// </summary>
    public string IsActivated { get; set; } = string.Empty;

    /// <summary>
    /// 账户状态
    /// </summary>
    public string IsActive { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public string? LastLoginAt { get; set; }
}

/// <summary>
/// 导出结果
/// </summary>
public class EmployeeExportResult
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件内容类型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 导出记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 导出时间
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
