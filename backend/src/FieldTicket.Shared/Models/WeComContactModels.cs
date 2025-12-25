namespace FieldTicket.Shared.Models;

/// <summary>
/// 企业微信部门DTO
/// </summary>
public class WeComDepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// 企业微信用户DTO
/// </summary>
public class WeComUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public int[]? Departments { get; set; }
    public string[]? Tags { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 企业微信群DTO
/// </summary>
public class WeComChatDto
{
    public string ChatId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public string[]? UserList { get; set; }
}

