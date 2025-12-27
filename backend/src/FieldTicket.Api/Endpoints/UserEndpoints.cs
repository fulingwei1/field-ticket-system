using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Domain.Entities;
using FieldTicket.Shared.Models;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 用户管理端点
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // GET /api/users - 获取用户列表
        group.MapGet("", async (
            ApplicationDbContext dbContext,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? deptId = null
        ) =>
        {
            var query = dbContext.Users.AsQueryable();

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(u =>
                    u.Name.Contains(searchQuery) ||
                    (u.Username != null && u.Username.Contains(searchQuery)) ||
                    (u.Mobile != null && u.Mobile.Contains(searchQuery)) ||
                    (u.Email != null && u.Email.Contains(searchQuery))
                );
            }

            // 角色过滤
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // 状态过滤
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // 部门过滤
            if (!string.IsNullOrWhiteSpace(deptId))
            {
                query = query.Where(u => u.DeptId == deptId);
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                items = users,
                total,
                page,
                pageSize
            });
        })
        .WithName("GetUsers")
        .WithOpenApi();

        // GET /api/users/{id} - 获取单个用户
        group.MapGet("/{id:guid}", async (
            Guid id,
            ApplicationDbContext dbContext
        ) =>
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return Results.NotFound(new { message = "用户不存在" });
            }

            return Results.Ok(user);
        })
        .WithName("GetUserById")
        .WithOpenApi();

        // POST /api/users - 创建用户（管理员）
        group.MapPost("", async (
            [FromBody] CreateUserRequest request,
            ApplicationDbContext dbContext,
            HttpContext context
        ) =>
        {
            // 验证管理员权限
            var currentUserRole = context.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (currentUserRole != "Admin")
            {
                return Results.Forbid();
            }

            // 验证用户名是否已存在
            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var existingUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);
                if (existingUser != null)
                {
                    return Results.BadRequest(new { message = "用户名已存在" });
                }
            }

            // 验证邮箱是否已存在
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingEmail = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingEmail != null)
                {
                    return Results.BadRequest(new { message = "邮箱已存在" });
                }
            }

            // 创建用户
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Name = request.Name,
                Email = request.Email,
                Mobile = request.Mobile,
                Role = request.Role,
                LoginType = request.LoginType,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 如果是密码登录，设置密码哈希
            if (request.LoginType == "Password" && !string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.LastPasswordChangeAt = DateTime.UtcNow;
            }

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}", user);
        })
        .WithName("CreateUser")
        .WithOpenApi();

        // PUT /api/users/{id} - 更新用户
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequest request,
            ApplicationDbContext dbContext,
            HttpContext context
        ) =>
        {
            // 验证管理员权限
            var currentUserRole = context.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (currentUserRole != "Admin")
            {
                return Results.Forbid();
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return Results.NotFound(new { message = "用户不存在" });
            }

            // 更新字段
            if (request.Role != null)
            {
                user.Role = request.Role;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Results.Ok(user);
        })
        .WithName("UpdateUser")
        .WithOpenApi();

        // PATCH /api/users/{id}/status - 更新用户状态
        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateUserStatusRequest request,
            ApplicationDbContext dbContext,
            HttpContext context
        ) =>
        {
            // 验证管理员权限
            var currentUserRole = context.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (currentUserRole != "Admin")
            {
                return Results.Forbid();
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return Results.NotFound(new { message = "用户不存在" });
            }

            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Results.Ok(new { success = true });
        })
        .WithName("UpdateUserStatus")
        .WithOpenApi();

        // DELETE /api/users/{id} - 删除用户
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ApplicationDbContext dbContext,
            HttpContext context
        ) =>
        {
            // 验证管理员权限
            var currentUserRole = context.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (currentUserRole != "Admin")
            {
                return Results.Forbid();
            }

            var user = await dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return Results.NotFound(new { message = "用户不存在" });
            }

            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { success = true });
        })
        .WithName("DeleteUser")
        .WithOpenApi();

        // GET /api/users/statistics - 获取用户统计
        group.MapGet("/statistics", async (
            ApplicationDbContext dbContext
        ) =>
        {
            var totalUsers = await dbContext.Users.CountAsync();
            var activeUsers = await dbContext.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;

            var roleDistribution = await dbContext.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            return Results.Ok(new
            {
                totalUsers,
                activeUsers,
                inactiveUsers,
                roleDistribution = roleDistribution.ToDictionary(r => r.Role, r => r.Count)
            });
        })
        .WithName("GetUserStatistics")
        .WithOpenApi();
    }
}

/// <summary>
/// 更新用户请求
/// </summary>
public class UpdateUserRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// 更新用户状态请求
/// </summary>
public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
