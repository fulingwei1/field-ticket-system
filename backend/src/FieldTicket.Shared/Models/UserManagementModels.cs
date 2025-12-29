namespace FieldTicket.Shared.Models;

/// <summary>
/// 用户列表项 DTO
/// </summary>
public class UserListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Mobile { get; set; }
    public string? DeptId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 用户详情 DTO
/// </summary>
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string CorpId { get; set; } = string.Empty;
    public string WeComUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Mobile { get; set; }
    public string? DeptId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建用户请求
/// </summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Mobile { get; set; }
    public string? DeptId { get; set; }
    public string Role { get; set; } = "FieldEngineer";
    public bool IsActive { get; set; } = true;
    public string? WeComUserId { get; set; }
}

/// <summary>
/// 更新用户请求
/// </summary>
public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Mobile { get; set; }
    public string? DeptId { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? WeComUserId { get; set; }
}

/// <summary>
/// 用户查询过滤器
/// </summary>
public class UserQueryFilter
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? DeptId { get; set; }
}

/// <summary>
/// 用户列表响应
/// </summary>
public class UserListResponse
{
    public List<UserListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}







