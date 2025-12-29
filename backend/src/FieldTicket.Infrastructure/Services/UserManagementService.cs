using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 用户管理服务实现
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext dbContext,
        ILogger<UserManagementService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserListResponse> GetUsersAsync(UserQueryFilter filter, int page = 1, int pageSize = 20)
    {
        var query = _dbContext.Users.AsQueryable();

        // 应用搜索过滤器
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(u =>
                u.Name.Contains(search) ||
                (u.Username != null && u.Username.Contains(search)) ||
                (u.Mobile != null && u.Mobile.Contains(search)));
        }

        // 按角色过滤
        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            query = query.Where(u => u.Role == filter.Role);
        }

        // 按活跃状态过滤
        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        // 按部门过滤
        if (!string.IsNullOrWhiteSpace(filter.DeptId))
        {
            query = query.Where(u => u.DeptId == filter.DeptId);
        }

        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Name = u.Name,
                Username = u.Username,
                Mobile = u.Mobile,
                DeptId = u.DeptId,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return new UserListResponse
        {
            Items = users,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDetailDto?> GetUserAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return null;
        }

        return new UserDetailDto
        {
            Id = user.Id,
            CorpId = user.CorpId,
            WeComUserId = user.WeComUserId,
            Name = user.Name,
            Username = user.Username,
            Mobile = user.Mobile,
            DeptId = user.DeptId,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserDetailDto> CreateUserAsync(CreateUserRequest request)
    {
        // 验证用户名唯一性
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"用户名 {request.Username} 已存在");
            }
        }

        // 验证企业微信用户ID唯一性
        if (!string.IsNullOrWhiteSpace(request.WeComUserId))
        {
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.WeComUserId == request.WeComUserId);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"企业微信用户ID {request.WeComUserId} 已存在");
            }
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            CorpId = string.Empty, // 如果从企业微信同步，应该设置 CorpId
            WeComUserId = request.WeComUserId ?? string.Empty,
            Name = request.Name,
            Username = request.Username,
            PasswordHash = request.Password, // 注意：当前是明文存储，生产环境应使用 BCrypt
            Mobile = request.Mobile,
            DeptId = request.DeptId,
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId}, Name: {Name}, Username: {Username}", 
            user.Id, user.Name, user.Username);

        return new UserDetailDto
        {
            Id = user.Id,
            CorpId = user.CorpId,
            WeComUserId = user.WeComUserId,
            Name = user.Name,
            Username = user.Username,
            Mobile = user.Mobile,
            DeptId = user.DeptId,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserDetailDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        // 验证用户名唯一性（如果修改了用户名）
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != userId);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"用户名 {request.Username} 已存在");
            }
            user.Username = request.Username;
        }

        // 验证企业微信用户ID唯一性（如果修改了）
        if (!string.IsNullOrWhiteSpace(request.WeComUserId) && request.WeComUserId != user.WeComUserId)
        {
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.WeComUserId == request.WeComUserId && u.Id != userId);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"企业微信用户ID {request.WeComUserId} 已存在");
            }
            user.WeComUserId = request.WeComUserId;
        }

        // 更新字段
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            user.Name = request.Name;
        }

        if (request.Password != null)
        {
            user.PasswordHash = request.Password; // 注意：当前是明文存储，生产环境应使用 BCrypt
        }

        if (request.Mobile != null)
        {
            user.Mobile = request.Mobile;
        }

        if (request.DeptId != null)
        {
            user.DeptId = request.DeptId;
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            user.Role = request.Role;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId}, Name: {Name}", user.Id, user.Name);

        return new UserDetailDto
        {
            Id = user.Id,
            CorpId = user.CorpId,
            WeComUserId = user.WeComUserId,
            Name = user.Name,
            Username = user.Username,
            Mobile = user.Mobile,
            DeptId = user.DeptId,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return false;
        }

        // 软删除：设置为非活跃状态
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User deleted (soft): {UserId}, Name: {Name}", user.Id, user.Name);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return false;
        }

        user.PasswordHash = newPassword; // 注意：当前是明文存储，生产环境应使用 BCrypt
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Password reset for user: {UserId}, Name: {Name}", user.Id, user.Name);

        return true;
    }
}





