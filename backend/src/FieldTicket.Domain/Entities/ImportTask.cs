namespace FieldTicket.Domain.Entities;

/// <summary>
/// 导入任务实体
/// 用于异步导入处理和进度跟踪
/// </summary>
public class ImportTask
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    public string TaskType { get; set; } = string.Empty; // EmployeeImport, EmployeeUpdate

    /// <summary>
    /// 任务状态
    /// </summary>
    public string Status { get; set; } = string.Empty; // Pending, Processing, Completed, Failed

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 开始处理时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// 创建人姓名
    /// </summary>
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径（临时存储）
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已处理记录数
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// 成功数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 跳过数
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// 当前处理消息
    /// </summary>
    public string? CurrentMessage { get; set; }

    /// <summary>
    /// 结果JSON（任务完成后）
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 导航属性：创建人
    /// </summary>
    public User? CreatedBy { get; set; }
}
