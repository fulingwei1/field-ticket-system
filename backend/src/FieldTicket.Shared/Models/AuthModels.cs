namespace FieldTicket.Shared.Models;

/// <summary>
/// 企业微信登录URL响应
/// </summary>
public class WeComLoginUrlResponse
{
    public string Url { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// 用户名密码登录请求
/// </summary>
public class PasswordLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 重置密码请求（管理员操作）
/// </summary>
public class ResetPasswordRequest
{
    public string UserId { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
}

/// <summary>
/// 创建用户请求
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string LoginType { get; set; } = "Password";
}

/// <summary>
/// 认证结果
/// </summary>
public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserInfo User { get; set; } = null!;
}

/// <summary>
/// 用户信息
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? DeptName { get; set; }
    public string LoginType { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = false;
}


