using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 企业微信通讯录服务接口
/// </summary>
public interface IWeComContactService
{
    /// <summary>
    /// 获取部门列表
    /// </summary>
    Task<List<WeComDepartmentDto>> GetDepartmentsAsync(int? parentId = null);

    /// <summary>
    /// 获取部门下的用户列表
    /// </summary>
    Task<List<WeComUserDto>> GetUsersByDepartmentAsync(int departmentId, bool fetchChild = false);

    /// <summary>
    /// 获取用户详情（包含标签）
    /// </summary>
    Task<WeComUserDto?> GetUserDetailAsync(string userId);

    /// <summary>
    /// 根据角色获取用户列表（通过角色映射配置）
    /// </summary>
    Task<List<WeComUserDto>> GetUsersByRoleAsync(string role);

    /// <summary>
    /// 根据部门名称获取用户列表
    /// </summary>
    Task<List<WeComUserDto>> GetUsersByDepartmentNameAsync(string departmentName);

    /// <summary>
    /// 获取企业微信群列表
    /// </summary>
    Task<List<WeComChatDto>> GetChatsAsync();

    /// <summary>
    /// 获取所有用户（可选按部门筛选）
    /// </summary>
    Task<List<WeComUserDto>> GetAllUsersAsync(int? departmentId = null);
}

