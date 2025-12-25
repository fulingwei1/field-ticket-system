using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// AI分析服务接口
/// </summary>
public interface IAiAnalysisService
{
    /// <summary>
    /// 生成每日工作总结
    /// </summary>
    Task<AiAnalysisResultDto> GenerateDailySummaryAsync(
        Guid engineerId,
        DateOnly analysisDate,
        Guid? createdBy = null);

    /// <summary>
    /// 生成每周总结
    /// </summary>
    Task<AiAnalysisResultDto> GenerateWeeklySummaryAsync(
        Guid engineerId,
        DateOnly weekStart,
        Guid? createdBy = null);

    /// <summary>
    /// 生成团队分析
    /// </summary>
    Task<AiAnalysisResultDto> GenerateTeamAnalysisAsync(
        Guid? departmentId,
        DateOnly analysisDate,
        string periodType,
        Guid? createdBy = null);

    /// <summary>
    /// 生成人员安排建议
    /// </summary>
    Task<AiAnalysisResultDto> GenerateSchedulingSuggestionAsync(
        Guid? departmentId,
        DateOnly analysisDate,
        Guid? createdBy = null);

    /// <summary>
    /// 获取分析结果列表
    /// </summary>
    Task<(List<AiAnalysisResultDto> Items, int Total)> GetAnalysisResultsAsync(
        AiAnalysisQueryFilter filter,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 获取分析结果详情
    /// </summary>
    Task<AiAnalysisResultDto?> GetAnalysisResultAsync(Guid analysisId);
}

/// <summary>
/// AI分析查询过滤器
/// </summary>
public class AiAnalysisQueryFilter
{
    public string? AnalysisType { get; set; }
    public Guid? EngineerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateOnly? AnalysisDateFrom { get; set; }
    public DateOnly? AnalysisDateTo { get; set; }
}


