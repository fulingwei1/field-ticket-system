using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 分诊服务接口
/// </summary>
public interface ITriageService
{
    /// <summary>
    /// 分诊工单
    /// </summary>
    Task<TriageResult> TriageTicketAsync(Guid ticketId, TriageTicketRequest request, Guid userId);
    
    /// <summary>
    /// 获取判断卡列表
    /// </summary>
    Task<List<JudgementCardDto>> GetJudgementCardsAsync(char? domain = null, string? status = null);
    
    /// <summary>
    /// 获取判断卡详情
    /// </summary>
    Task<JudgementCardDto?> GetJudgementCardAsync(string jcCode);
}


