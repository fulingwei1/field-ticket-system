using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 知识版本服务接口
/// </summary>
public interface IKnowledgeVersionService
{
    /// <summary>
    /// 创建新版本
    /// </summary>
    Task<KnowledgeVersionDto> CreateVersionAsync(CreateVersionRequest request, Guid userId);

    /// <summary>
    /// 获取版本历史
    /// </summary>
    Task<List<KnowledgeVersionDto>> GetVersionHistoryAsync(Guid knowledgeId, string knowledgeType);

    /// <summary>
    /// 版本对比
    /// </summary>
    Task<FieldTicket.Shared.Models.VersionComparisonDto> CompareVersionsAsync(Guid versionId1, Guid versionId2);

    /// <summary>
    /// 版本回滚
    /// </summary>
    Task<KnowledgeVersionDto> RollbackVersionAsync(
        Guid knowledgeId,
        string knowledgeType,
        RollbackVersionRequest request,
        Guid userId);

    /// <summary>
    /// 检查知识过期
    /// </summary>
    Task<List<ExpiredKnowledgeDto>> CheckExpiredKnowledgeAsync();
}

