using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 根本原因分析服务接口
/// </summary>
public interface IRootCauseAnalysisService
{
    /// <summary>
    /// 创建或更新根本原因分析
    /// </summary>
    Task<RootCauseAnalysisDto> CreateOrUpdateAnalysisAsync(
        Guid problemId,
        CreateRootCauseAnalysisRequest request);

    /// <summary>
    /// 获取问题的根本原因分析
    /// </summary>
    Task<RootCauseAnalysisDto?> GetAnalysisByProblemIdAsync(Guid problemId);

    /// <summary>
    /// 获取根本原因分析详情
    /// </summary>
    Task<RootCauseAnalysisDto?> GetAnalysisAsync(Guid analysisId);

    /// <summary>
    /// 删除根本原因分析
    /// </summary>
    Task DeleteAnalysisAsync(Guid analysisId);

    /// <summary>
    /// 获取5Why分析模板
    /// </summary>
    Task<FiveWhyTemplate> GetFiveWhyTemplateAsync(string problemCategory);

    /// <summary>
    /// 获取预防措施建议
    /// </summary>
    Task<List<PreventiveMeasureSuggestion>> GetPreventiveMeasureSuggestionsAsync(
        string rootCauseCategory);
}











