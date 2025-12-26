using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 企业微信通讯录相关 API 端点
/// </summary>
public static class WeComContactEndpoints
{
    public static void MapWeComContactEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/wecom/contacts")
            .WithTags("WeComContacts")
            .RequireAuthorization();

        // 获取部门列表
        group.MapGet("departments", GetDepartments)
            .WithName("GetDepartments")
            .WithSummary("获取企业微信部门列表")
            .Produces<List<WeComDepartmentDto>>();

        // 获取部门下的用户列表
        group.MapGet("departments/{departmentId}/users", GetDepartmentUsers)
            .WithName("GetDepartmentUsers")
            .WithSummary("获取部门下的用户列表")
            .Produces<List<WeComUserDto>>();

        // 获取用户详情
        group.MapGet("users/{userId}", GetUserDetail)
            .WithName("GetUserDetail")
            .WithSummary("获取用户详情")
            .Produces<WeComUserDto>()
            .Produces(StatusCodes.Status404NotFound);

        // 根据角色获取用户列表
        group.MapGet("users/by-role/{role}", GetUsersByRole)
            .WithName("GetUsersByRole")
            .WithSummary("根据角色获取用户列表")
            .Produces<List<WeComUserDto>>();

        // 根据部门名称获取用户列表
        group.MapGet("users/by-department/{departmentName}", GetUsersByDepartmentName)
            .WithName("GetUsersByDepartmentName")
            .WithSummary("根据部门名称获取用户列表")
            .Produces<List<WeComUserDto>>();

        // 获取群列表
        group.MapGet("chats", GetChats)
            .WithName("GetChats")
            .WithSummary("获取企业微信群列表")
            .Produces<List<WeComChatDto>>();

        // 获取所有用户
        group.MapGet("users", GetAllUsers)
            .WithName("GetAllUsers")
            .WithSummary("获取所有用户")
            .Produces<List<WeComUserDto>>();
    }

    private static async Task<IResult> GetDepartments(
        [FromQuery] int? parentId,
        IWeComContactService service)
    {
        try
        {
            var departments = await service.GetDepartmentsAsync(parentId);
            return Results.Ok(departments);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetDepartmentUsers(
        int departmentId,
        IWeComContactService service = null!,
        [FromQuery] bool fetchChild = false)
    {
        try
        {
            var users = await service.GetUsersByDepartmentAsync(departmentId, fetchChild);
            return Results.Ok(users);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetUserDetail(
        string userId,
        IWeComContactService service = null!)
    {
        try
        {
            var user = await service.GetUserDetailAsync(userId);
            if (user == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(user);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetUsersByRole(
        string role,
        IWeComContactService service = null!)
    {
        try
        {
            var users = await service.GetUsersByRoleAsync(role);
            return Results.Ok(users);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetUsersByDepartmentName(
        string departmentName,
        IWeComContactService service = null!)
    {
        try
        {
            var users = await service.GetUsersByDepartmentNameAsync(departmentName);
            return Results.Ok(users);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetChats(
        IWeComContactService service = null!)
    {
        try
        {
            var chats = await service.GetChatsAsync();
            return Results.Ok(chats);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetAllUsers(
        [FromQuery] int? departmentId,
        IWeComContactService service = null!)
    {
        try
        {
            var users = await service.GetAllUsersAsync(departmentId);
            return Results.Ok(users);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}

