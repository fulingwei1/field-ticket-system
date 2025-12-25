using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 缺失信息分析服务接口
/// </summary>
public interface IMissingInfoAnalysisService
{
    /// <summary>
    /// 分析缺失信息
    /// </summary>
    Task<List<MissingInfoItem>> AnalyzeMissingInfoAsync(Guid ticketId, string? jcCode = null);
    
    /// <summary>
    /// 生成问诊式问题清单
    /// </summary>
    Task<List<QuestionItem>> GenerateQuestionnaireAsync(List<MissingInfoItem> missingInfo);
}


