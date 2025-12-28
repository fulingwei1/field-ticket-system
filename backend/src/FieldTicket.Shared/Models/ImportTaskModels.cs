namespace FieldTicket.Shared.Models;

/// <summary>
/// 导入任务DTO
/// </summary>
public class ImportTaskDto
{
    public Guid Id { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string TaskTypeDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ProgressPercentage { get; set; }
    public string? CurrentMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 导入任务详情DTO
/// </summary>
public class ImportTaskDetailDto : ImportTaskDto
{
    public object? Result { get; set; }
}

/// <summary>
/// 创建异步导入任务请求
/// </summary>
public class CreateAsyncImportRequest
{
    public string TaskType { get; set; } = string.Empty; // EmployeeImport, EmployeeUpdate
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// 创建异步导入任务响应
/// </summary>
public class CreateAsyncImportResponse
{
    public Guid TaskId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 任务列表查询请求
/// </summary>
public class ImportTaskQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? TaskType { get; set; }
    public string? Status { get; set; }
    public Guid? CreatedById { get; set; }
}

/// <summary>
/// 任务列表查询结果
/// </summary>
public class ImportTaskQueryResult
{
    public int Total { get; set; }
    public List<ImportTaskDto> Items { get; set; } = new();
}
