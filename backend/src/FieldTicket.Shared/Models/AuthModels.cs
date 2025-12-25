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
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? DeptName { get; set; }
}


