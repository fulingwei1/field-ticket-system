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
    public string? DeptName { get; set; }
    public string Role { get; set; } = string.Empty;

    // 上级关系
    public Guid? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }

    // 身份证后4位（加密存储，用于密码验证）
    public string? IdCardLastFour { get; set; }

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

    /// <summary>
    /// 是否已开通账户（导入后需要管理员手动开通）
    /// </summary>
    public bool IsActivated { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


