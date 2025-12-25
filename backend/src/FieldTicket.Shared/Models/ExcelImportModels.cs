namespace FieldTicket.Shared.Models;

/// <summary>
/// Excel导入结果
/// </summary>
public class ExcelImportResult
{
    public List<ProjectImportData> Projects { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
}

/// <summary>
/// 项目导入数据
/// </summary>
public class ProjectImportData
{
    public string ProjectNo { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? DeviceType { get; set; }
    public string? IndustryType { get; set; }
    public decimal? SalesAmount { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime? OrderDate { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public int? DeliveryDelayDays { get; set; }
    public string ProjectStatus { get; set; } = "进行中";
    public string? ProjectManagerName { get; set; }
    
    public List<ProblemImportData> Problems { get; set; } = new();
    public int ExcelRowNumber { get; set; } // Excel中的行号
}

/// <summary>
/// 问题导入数据
/// </summary>
public class ProblemImportData
{
    public int ProblemSequence { get; set; }
    public string ProblemCategory { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public DateTime FoundDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int? ProcessingDays { get; set; }
    public string PrimaryDepartment { get; set; } = string.Empty;
    public string PrimaryResponsible { get; set; } = string.Empty;
    public string? CollaboratingDepartment { get; set; }
    public string? CollaboratingPerson { get; set; }
    public string Status { get; set; } = "待分配";
    public string? Solution { get; set; }
    public string? SolutionDetails { get; set; }
    public string? VerificationStatus { get; set; }
    public string? CustomerFeedback { get; set; }
    public int? SatisfactionScore { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public string? RelatedTicketNo { get; set; }
    public string? KnowledgeBaseId { get; set; }
    public bool IsRepeatProblem { get; set; }
    public string? Notes { get; set; }
    public int ExcelRowNumber { get; set; } // Excel中的行号
}

/// <summary>
/// 导入错误
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty; // Format/Required/Business/Relation
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
}

/// <summary>
/// 导入执行结果
/// </summary>
public class ImportExecutionResult
{
    public bool Success { get; set; }
    public int ProjectsCreated { get; set; }
    public int ProjectsUpdated { get; set; }
    public int ProblemsCreated { get; set; }
    public int ProblemsUpdated { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 字段映射配置
/// </summary>
public class ExcelFieldMapping
{
    public string ExcelColumn { get; set; } = string.Empty; // Excel列名（如"项目号"、"项目号(PJ)"）
    public string SystemField { get; set; } = string.Empty; // 系统字段（如"ProjectNo"）
    public string FieldType { get; set; } = "String"; // String/Date/Decimal/Int/Bool
    public bool Required { get; set; }
    public string? SheetName { get; set; } // 所属Sheet名称
}






