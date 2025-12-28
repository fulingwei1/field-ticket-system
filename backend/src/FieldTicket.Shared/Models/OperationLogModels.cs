namespace FieldTicket.Shared.Models;

/// <summary>
/// 操作日志查询请求
/// </summary>
public class OperationLogQueryRequest
{
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 操作类型筛选
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// 操作人ID筛选
    /// </summary>
    public Guid? OperatorId { get; set; }

    /// <summary>
    /// 操作结果筛选
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 搜索关键词（操作人姓名或描述）
    /// </summary>
    public string? SearchKeyword { get; set; }
}

/// <summary>
/// 操作日志查询结果
/// </summary>
public class OperationLogQueryResult
{
    /// <summary>
    /// 总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 日志列表
    /// </summary>
    public List<OperationLogDto> Items { get; set; } = new();
}

/// <summary>
/// 操作日志DTO
/// </summary>
public class OperationLogDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型显示名称
    /// </summary>
    public string OperationTypeDisplay { get; set; } = string.Empty;

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
    /// 操作结果
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
    /// 源文件名
    /// </summary>
    public string? SourceFileName { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }
}

/// <summary>
/// 操作日志详情DTO
/// </summary>
public class OperationLogDetailDto : OperationLogDto
{
    /// <summary>
    /// 详细数据（已解析为对象）
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    /// 错误消息列表
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// 操作日志统计结果
/// </summary>
public class OperationLogStatistics
{
    /// <summary>
    /// 总操作数
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// 按操作类型统计
    /// </summary>
    public List<OperationTypeStats> ByType { get; set; } = new();

    /// <summary>
    /// 按操作结果统计
    /// </summary>
    public List<OperationResultStats> ByResult { get; set; } = new();

    /// <summary>
    /// 最近7天趋势
    /// </summary>
    public List<DailyStats> Last7Days { get; set; } = new();
}

/// <summary>
/// 操作类型统计
/// </summary>
public class OperationTypeStats
{
    public string OperationType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// 操作结果统计
/// </summary>
public class OperationResultStats
{
    public string Result { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// 每日统计
/// </summary>
public class DailyStats
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}
