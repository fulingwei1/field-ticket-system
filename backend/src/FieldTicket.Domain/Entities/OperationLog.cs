namespace FieldTicket.Domain.Entities;

/// <summary>
/// 操作日志实体
/// 记录员工导入、更新、账户开通等操作
/// </summary>
public class OperationLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty; // EmployeeImport, EmployeeUpdate, AccountActivation, AccountBatchActivation

    /// <summary>
    /// 操作人ID
    /// </summary>
    public Guid OperatorId { get; set; }

    /// <summary>
    /// 操作人姓名
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperatedAt { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作结果（Success, Partial, Failed）
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 总处理数
    /// </summary>
    public int TotalCount { get; set; }

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
    /// 详细数据（JSON格式）
    /// 存储导入的员工列表、更新详情等
    /// </summary>
    public string? DetailsJson { get; set; }

    /// <summary>
    /// 错误消息（JSON数组）
    /// </summary>
    public string? ErrorMessagesJson { get; set; }

    /// <summary>
    /// 源文件名（如果是文件导入）
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 导航属性：操作人
    /// </summary>
    public User? Operator { get; set; }
}
