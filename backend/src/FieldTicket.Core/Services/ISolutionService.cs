using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 解决方案服务接口
/// </summary>
public interface ISolutionService
{
    /// <summary>
    /// 创建解决方案草稿
    /// </summary>
    Task<SolutionDto> CreateSolutionAsync(Guid ticketId, CreateSolutionRequest request, Guid userId);
    
    /// <summary>
    /// 更新解决方案
    /// </summary>
    Task<SolutionDto> UpdateSolutionAsync(Guid solutionId, UpdateSolutionRequest request, Guid userId);
    
    /// <summary>
    /// 发布解决方案
    /// </summary>
    Task<SolutionDto> PublishSolutionAsync(Guid solutionId, Guid userId);
    
    /// <summary>
    /// 获取解决方案详情
    /// </summary>
    Task<SolutionDto?> GetSolutionAsync(Guid solutionId);
    
    /// <summary>
    /// 获取工单的解决方案列表
    /// </summary>
    Task<List<SolutionDto>> GetTicketSolutionsAsync(Guid ticketId);
}


