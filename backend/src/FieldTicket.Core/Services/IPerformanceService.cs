using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 绩效服务接口
/// </summary>
public interface IPerformanceService
{
    /// <summary>
    /// 获取工程师绩效指标
    /// </summary>
    Task<PerformanceMetricsDto?> GetEngineerMetricsAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid? currentUserId = null);

    /// <summary>
    /// 获取绩效指标列表
    /// </summary>
    Task<(List<PerformanceMetricsDto> Items, int Total)> GetMetricsAsync(
        PerformanceQueryFilter filter,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 获取团队绩效
    /// </summary>
    Task<List<PerformanceMetricsDto>> GetTeamMetricsAsync(
        Guid? departmentId,
        string periodType,
        DateOnly periodStart,
        Guid? currentUserId = null);

    /// <summary>
    /// 获取绩效排名
    /// </summary>
    Task<List<PerformanceRankingDto>> GetRankingAsync(
        string periodType,
        DateOnly periodStart,
        Guid? departmentId = null,
        Guid? currentUserId = null);

    /// <summary>
    /// 手动触发绩效计算
    /// </summary>
    Task<PerformanceMetricsDto> CalculateMetricsAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        DateOnly periodEnd);

    /// <summary>
    /// 获取绩效趋势
    /// </summary>
    Task<List<PerformanceTrendDto>> GetTrendsAsync(
        Guid engineerId,
        string periodType,
        DateOnly fromDate,
        DateOnly toDate);

    /// <summary>
    /// 生成绩效报告
    /// </summary>
    Task<PerformanceReportDto> GeneratePerformanceReportAsync(
        GeneratePerformanceReportRequest request,
        Guid currentUserId);

    /// <summary>
    /// 导出绩效数据
    /// </summary>
    Task<byte[]> ExportPerformanceDataAsync(
        ExportPerformanceDataRequest request,
        Guid currentUserId);

    /// <summary>
    /// 生成个性化改进建议
    /// </summary>
    Task<List<ImprovementSuggestionDto>> GenerateImprovementSuggestionsAsync(
        Guid engineerId,
        string periodType,
        DateOnly periodStart,
        Guid currentUserId);
}

/// <summary>
/// 绩效查询过滤器
/// </summary>
public class PerformanceQueryFilter
{
    public Guid? EngineerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? PeriodType { get; set; }
    public DateOnly? PeriodStartFrom { get; set; }
    public DateOnly? PeriodStartTo { get; set; }
    public decimal? MinOverallScore { get; set; }
    public string? PerformanceLevel { get; set; }
}


