namespace FieldTicket.Core.Services;

/// <summary>
/// KPI反作弊预警服务接口
/// </summary>
public interface IKPIAnomalyService
{
    /// <summary>
    /// 检测工程师的KPI异常
    /// </summary>
    Task<List<AnomalyDto>> DetectAnomaliesAsync(Guid engineerId, int days = 30);

    /// <summary>
    /// 检测团队的KPI异常
    /// </summary>
    Task<List<EngineerAnomalyReportDto>> DetectTeamAnomaliesAsync(Guid? teamId = null, int days = 30);

    /// <summary>
    /// 生成异常报告
    /// </summary>
    Task<AnomalyReportDto> GenerateAnomalyReportAsync(Guid? engineerId = null, Guid? teamId = null, int days = 30);
}

/// <summary>
/// 异常DTO
/// </summary>
public class AnomalyDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // low, medium, high, critical
    public Dictionary<string, object>? Details { get; set; }
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// 工程师异常报告DTO
/// </summary>
public class EngineerAnomalyReportDto
{
    public Guid EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public List<AnomalyDto> Anomalies { get; set; } = new();
    public int TotalAnomalies { get; set; }
    public int CriticalAnomalies { get; set; }
    public int HighAnomalies { get; set; }
}

/// <summary>
/// 异常报告DTO
/// </summary>
public class AnomalyReportDto
{
    public DateTime ReportDate { get; set; }
    public int Days { get; set; }
    public Guid? EngineerId { get; set; }
    public string? EngineerName { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public List<EngineerAnomalyReportDto> EngineerReports { get; set; } = new();
    public int TotalEngineers { get; set; }
    public int EngineersWithAnomalies { get; set; }
    public Dictionary<string, int> AnomalyTypeCounts { get; set; } = new();
}

















