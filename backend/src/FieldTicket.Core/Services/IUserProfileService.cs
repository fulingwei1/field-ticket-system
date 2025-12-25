using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 用户画像服务接口
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// 构建用户画像
    /// </summary>
    Task<UserProfileDto> BuildUserProfileAsync(Guid userId);

    /// <summary>
    /// 获取用户画像
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId);

    /// <summary>
    /// 更新用户画像
    /// </summary>
    Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);

    /// <summary>
    /// 智能预填充
    /// </summary>
    Task<PreFillData> GetPreFillDataAsync(Guid userId, Guid? deviceId);

    /// <summary>
    /// 个性化问题推荐
    /// </summary>
    Task<List<PersonalizedQuestion>> RecommendPersonalizedQuestionsAsync(
        Guid userId,
        Guid ticketId);
}

