using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// AI深度分析缺失信息服务接口
/// </summary>
public interface IAIDeepAnalysisService
{
    /// <summary>
    /// 深度分析工单内容，识别隐含信息需求
    /// </summary>
    Task<DeepAnalysisResult> AnalyzeTicketAsync(Guid ticketId, string? jcCode = null);

    /// <summary>
    /// 识别隐含信息需求
    /// </summary>
    Task<List<ImplicitInfoRequirement>> IdentifyImplicitRequirementsAsync(
        TicketDto ticket,
        JudgementCardDto? jc);

    /// <summary>
    /// 生成个性化问题
    /// </summary>
    Task<List<PersonalizedQuestion>> GeneratePersonalizedQuestionsAsync(
        Guid ticketId,
        List<ImplicitInfoRequirement> requirements);

    /// <summary>
    /// 多轮对话补全
    /// </summary>
    Task<ConversationResult> ContinueConversationAsync(
        Guid ticketId,
        string userAnswer,
        string questionId);
}

