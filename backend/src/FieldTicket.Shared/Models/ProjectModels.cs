namespace FieldTicket.Shared.Models;

/// <summary>
/// 项目查询过滤器
/// </summary>
public class ProjectQueryFilter
{
    public string? ProjectNo { get; set; }
    public string? ProjectName { get; set; }
    public string? CustomerName { get; set; }
    public string? DeviceType { get; set; }
    public string? ProjectStatus { get; set; }
    public DateTime? OrderDateFrom { get; set; }
    public DateTime? OrderDateTo { get; set; }
}

/// <summary>
/// 项目DTO
/// </summary>
public class ProjectDto
{
    public Guid ProjectId { get; set; }
    public string ProjectNo { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string? IndustryType { get; set; }
    public decimal? SalesAmount { get; set; }
    public int Quantity { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public int? DeliveryDelayDays { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public string? ProjectManagerName { get; set; }
    public int ProblemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 项目详情DTO
/// </summary>
public class ProjectDetailDto : ProjectDto
{
    public List<FieldProblemDto> Problems { get; set; } = new();
}

/// <summary>
/// 现场问题DTO
/// </summary>
public class FieldProblemDto
{
    public Guid ProblemId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectNo { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
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
    public string Status { get; set; } = string.Empty;
    public string? VerificationStatus { get; set; }
    public int? SatisfactionScore { get; set; }
    public bool IsRepeatProblem { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 问题详情DTO
/// </summary>
public class FieldProblemDetailDto : FieldProblemDto
{
    public string? Solution { get; set; }
    public string? SolutionDetails { get; set; }
    public string? CustomerFeedback { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public string? RelatedTicketNo { get; set; }
    public string? KnowledgeBaseId { get; set; }
    public string? Notes { get; set; }
    public RootCauseAnalysisDto? RootCauseAnalysis { get; set; }
}

/// <summary>
/// 问题统计过滤器
/// </summary>
public class ProblemStatisticsFilter
{
    public string? ProblemCategory { get; set; }
    public string? PrimaryDepartment { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? FoundDateFrom { get; set; }
    public DateTime? FoundDateTo { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// 问题统计DTO
/// </summary>
public class ProblemStatisticsDto
{
    public int TotalProblems { get; set; }
    public int ClosedProblems { get; set; }
    public int OpenProblems { get; set; }
    public decimal AverageProcessingDays { get; set; }
    public decimal AverageSatisfactionScore { get; set; }
    public List<CategoryStatistics> CategoryStatistics { get; set; } = new();
    public List<DepartmentStatistics> DepartmentStatistics { get; set; } = new();
    public List<CustomerStatistics> CustomerStatistics { get; set; } = new();
}

/// <summary>
/// 分类统计
/// </summary>
public class CategoryStatistics
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// 部门统计
/// </summary>
public class DepartmentStatistics
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal AverageProcessingDays { get; set; }
    public decimal AverageSatisfactionScore { get; set; }
}

/// <summary>
/// 客户统计
/// </summary>
public class CustomerStatistics
{
    public string CustomerName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal AverageSatisfactionScore { get; set; }
}




