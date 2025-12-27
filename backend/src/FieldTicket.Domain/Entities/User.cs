namespace FieldTicket.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    public Guid Id { get; set; }

    // 企业微信登录字段
    public string? CorpId { get; set; }
    public string? WeComUserId { get; set; }

    // 基本信息
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? DeptId { get; set; }
    public string Role { get; set; } = string.Empty;

    // 用户名密码登录字段
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }

    // 登录类型：WeCom（企业微信）或 Password（用户名密码）
    public string LoginType { get; set; } = "WeCom";

    // 密码相关
    public DateTime? LastPasswordChangeAt { get; set; }
    public bool MustChangePassword { get; set; } = false;

    // 状态
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


