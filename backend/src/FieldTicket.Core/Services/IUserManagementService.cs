using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 用户管理服务接口
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// 获取用户列表
    /// </summary>
    Task<UserListResponse> GetUsersAsync(UserQueryFilter filter, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取用户详情
    /// </summary>
    Task<UserDetailDto?> GetUserAsync(Guid userId);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<UserDetailDto> CreateUserAsync(CreateUserRequest request);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<UserDetailDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);

    /// <summary>
    /// 删除用户（软删除：设置为非活跃状态）
    /// </summary>
    Task<bool> DeleteUserAsync(Guid userId);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword);
}


