using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 对话式诊断服务接口
/// </summary>
public interface IConversationalDiagnosisService
{
    /// <summary>
    /// 开始诊断对话
    /// </summary>
    Task<DiagnosisConversationDto> StartConversationAsync(Guid ticketId);

    /// <summary>
    /// 生成初始假设
    /// </summary>
    Task<List<Shared.Models.HypothesisDto>> GenerateInitialHypothesesAsync(Guid ticketId);

    /// <summary>
    /// 生成验证步骤
    /// </summary>
    Task<List<VerificationStepDto>> GenerateVerificationStepsAsync(
        Guid conversationId,
        string hypothesisId);

    /// <summary>
    /// 提交验证结果
    /// </summary>
    Task<ConversationResultDto> SubmitVerificationResultAsync(
        Guid conversationId,
        SubmitVerificationResultRequest request);

    /// <summary>
    /// 调整假设
    /// </summary>
    Task<Shared.Models.HypothesisDto> AdjustHypothesisAsync(
        Guid conversationId,
        AdjustHypothesisRequest request);

    /// <summary>
    /// 完成诊断
    /// </summary>
    Task<DiagnosisResultDto> CompleteDiagnosisAsync(Guid conversationId);

    /// <summary>
    /// 获取诊断路径
    /// </summary>
    Task<DiagnosisPathDto> GetDiagnosisPathAsync(Guid conversationId);
}


