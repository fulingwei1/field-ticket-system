using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 验证服务接口
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// 提交验证结果
    /// </summary>
    Task<VerificationDto> SubmitVerificationAsync(Guid ticketId, SubmitVerificationRequest request, Guid userId);
    
    /// <summary>
    /// 获取工单的验证历史
    /// </summary>
    Task<List<VerificationDto>> GetVerificationHistoryAsync(Guid ticketId);
    
    /// <summary>
    /// 获取验证详情
    /// </summary>
    Task<VerificationDto?> GetVerificationAsync(Guid verificationId);
}


