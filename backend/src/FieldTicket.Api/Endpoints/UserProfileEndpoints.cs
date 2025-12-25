using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 用户画像相关 API 端点
/// </summary>
public static class UserProfileEndpoints
{
    public static void MapUserProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("UserProfile").RequireAuthorization();

        // 获取用户画像
        group.MapGet("/{userId:guid}/profile", async (
            Guid userId,
            HttpContext context,
            IUserProfileService userProfileService) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只能查看自己的画像
            if (userId != currentUserId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var profile = await userProfileService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(profile);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("GetUserProfile")
        .WithSummary("获取用户画像")
        .Produces<UserProfileDto>();

        // 构建用户画像
        group.MapPost("/{userId:guid}/profile/build", async (
            Guid userId,
            HttpContext context,
            IUserProfileService userProfileService) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只能构建自己的画像
            if (userId != currentUserId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var profile = await userProfileService.BuildUserProfileAsync(userId);
                return Results.Ok(profile);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("BuildUserProfile")
        .WithSummary("构建用户画像")
        .Produces<UserProfileDto>();

        // 智能预填充
        group.MapGet("/{userId:guid}/prefill", async (
            Guid userId,
            [FromQuery] Guid? deviceId,
            HttpContext context,
            IUserProfileService userProfileService) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只能获取自己的预填充数据
            if (userId != currentUserId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var preFillData = await userProfileService.GetPreFillDataAsync(userId, deviceId);
                return Results.Ok(preFillData);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("GetPreFillData")
        .WithSummary("获取智能预填充数据")
        .Produces<PreFillData>();

        // 个性化问题推荐
        group.MapGet("/{userId:guid}/personalized-questions", async (
            Guid userId,
            [FromQuery] Guid ticketId,
            HttpContext context,
            IUserProfileService userProfileService) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只能获取自己的推荐问题
            if (userId != currentUserId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                var questions = await userProfileService.RecommendPersonalizedQuestionsAsync(userId, ticketId);
                return Results.Ok(questions);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("GetPersonalizedQuestions")
        .WithSummary("获取个性化问题推荐")
        .Produces<List<PersonalizedQuestion>>();

        // 更新用户画像
        group.MapPost("/{userId:guid}/profile/update", async (
            Guid userId,
            [FromBody] UpdateUserProfileRequest request,
            HttpContext context,
            IUserProfileService userProfileService) =>
        {
            var currentUserId = GetUserId(context);
            if (currentUserId == null)
            {
                return Results.Unauthorized();
            }

            // 权限检查：只能更新自己的画像
            if (userId != currentUserId.Value)
            {
                return Results.Forbid();
            }

            try
            {
                await userProfileService.UpdateUserProfileAsync(userId, request);
                return Results.Ok(new { success = true, message = "用户画像已更新" });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithName("UpdateUserProfile")
        .WithSummary("更新用户画像")
        .Produces<object>();
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

