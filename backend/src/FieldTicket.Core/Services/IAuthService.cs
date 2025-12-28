using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    // ========== 企业微信登录 ==========

    /// <summary>
    /// 获取企业微信授权URL
    /// </summary>
    Task<WeComLoginUrlResponse> GetWeComLoginUrlAsync(string? state = null);

    /// <summary>
    /// 处理企业微信回调，换取Token
    /// </summary>
    Task<AuthResult> HandleWeComCallbackAsync(string code, string? state);

    /// <summary>
    /// 处理企业微信小程序登录
    /// </summary>
    Task<AuthResult> HandleWeComMiniProgramLoginAsync(string code);

    // ========== 用户名密码登录 ==========

    /// <summary>
    /// 用户名密码登录
    /// </summary>
    Task<AuthResult> PasswordLoginAsync(string username, string password, bool rememberMe = false);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);

    /// <summary>
    /// 重置密码（管理员操作）
    /// </summary>
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword = true);

    // ========== 公共方法 ==========

    /// <summary>
    /// 刷新Token
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync(string token);
}


