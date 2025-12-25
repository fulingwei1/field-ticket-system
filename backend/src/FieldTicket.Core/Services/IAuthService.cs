using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 获取企业微信授权URL
    /// </summary>
    Task<WeComLoginUrlResponse> GetWeComLoginUrlAsync(string? state = null);

    /// <summary>
    /// 处理企业微信回调，换取Token
    /// </summary>
    Task<AuthResult> HandleWeComCallbackAsync(string code, string? state);

    /// <summary>
    /// 刷新Token
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync(string token);

    /// <summary>
    /// 处理企业微信小程序登录
    /// </summary>
    Task<AuthResult> HandleWeComMiniProgramLoginAsync(string code);
}


