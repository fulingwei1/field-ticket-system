using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 用户管理相关 API 端点
/// </summary>
public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/user-management")
            .WithTags("UserManagement")
            .RequireAuthorization();

        // 获取用户列表
        group.MapGet("", async (
            HttpContext context,
            IUserManagementService service,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? deptId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            // 权限检查：只有管理员可以访问
            if (!await IsAdminAsync(context))
            {
                return Results.Forbid();
            }

            var filter = new UserQueryFilter
            {
                Search = search,
                Role = role,
                IsActive = isActive,
                DeptId = deptId
            };

            var result = await service.GetUsersAsync(filter, page, pageSize);
            return Results.Ok(result);
        })
        .WithName("GetUsers")
        .WithSummary("获取用户列表（仅管理员）")
        .Produces<UserListResponse>();

        // 获取用户详情
        group.MapGet("{userId:guid}", async (
            Guid userId,
            HttpContext context,
            IUserManagementService service) =>
        {
            // 权限检查：只有管理员可以访问
            if (!await IsAdminAsync(context))
            {
                return Results.Forbid();
            }

            var user = await service.GetUserAsync(userId);
            if (user == null)
            {
                return Results.NotFound(new { message = $"用户 {userId} 不存在" });
            }

            return Results.Ok(user);
        })
        .WithName("GetUser")
        .WithSummary("获取用户详情（仅管理员）")
        .Produces<UserDetailDto>()
        .Produces(StatusCodes.Status404NotFound);

        // 创建用户
        group.MapPost("", async (
            [FromBody] CreateUserRequest request,
            HttpContext context,
            IUserManagementService service) =>
        {
            // 权限检查：只有管理员可以访问
            if (!await IsAdminAsync(context))
            {
                return Results.Forbid();
            }

            try
            {
                var user = await service.CreateUserAsync(request);
                return Results.Created($"/api/user-management/{user.Id}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("CreateUser")
        .WithSummary("创建用户（仅管理员）")
        .Produces<UserDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // 更新用户
        group.MapPut("{userId:guid}", async (
            Guid userId,
            [FromBody] UpdateUserRequest request,
            HttpContext context,
            IUserManagementService service) =>
        {
            // 权限检查：只有管理员可以访问
            if (!await IsAdminAsync(context))
            {
                return Results.Forbid();
            }

            try
            {
                var user = await service.UpdateUserAsync(userId, request);
                return Results.Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("UpdateUser")
        .WithSummary("更新用户（仅管理员）")
        .Produces<UserDetailDto>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // 删除用户（软删除）
        group.MapDelete("{userId:guid}", async (
            Guid userId,
            HttpContext context,
            IUserManagementService service) =>
        {
            // 权限检查：只有管理员可以访问
            if (!await IsAdminAsync(context))
            {
                return Results.Forbid();
            }

            var result = await service.DeleteUserAsync(userId);
            if (!result)
            {
                return Results.NotFound(new { message = $"用户 {userId} 不存在" });
            }

            return Results.Ok(new { message = "用户已删除" });
        })
        .WithName("DeleteUser")
        .WithSummary("删除用户（仅管理员）")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // 重置密码
        group.MapPost("{userId:guid}/reset-password", async (
            Guid userId,
            [FromBody] ResetPasswordRequest request,
            HttpContext context,
            IUserManagementService service) =>
        {
            // 权限检查：只有管理员可以访问
            if (!await IsAdminAsync(context))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { message = "新密码不能为空" });
            }

            var result = await service.ResetPasswordAsync(userId, request.NewPassword);
            if (!result)
            {
                return Results.NotFound(new { message = $"用户 {userId} 不存在" });
            }

            return Results.Ok(new { message = "密码已重置" });
        })
        .WithName("ResetPassword")
        .WithSummary("重置用户密码（仅管理员）")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }

    private static async Task<bool> IsAdminAsync(HttpContext context)
    {
        var userId = GetUserId(context);
        if (!userId.HasValue)
        {
            return false;
        }

        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.FindAsync(userId.Value);
        return user != null && (user.Role == "Admin" || user.Role == "admin");
    }
}

/// <summary>
/// 重置密码请求
/// </summary>
public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

