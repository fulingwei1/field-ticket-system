using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 认证相关 API 端点
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        // 获取企业微信登录URL
        group.MapGet("/wecom/login-url", async (
            [FromQuery] string? state,
            IAuthService authService) =>
        {
            var result = await authService.GetWeComLoginUrlAsync(state);
            return Results.Ok(result);
        })
        .WithName("GetWeComLoginUrl")
        .WithSummary("获取企业微信授权登录URL")
        .Produces<WeComLoginUrlResponse>();

        // 处理企业微信回调
        group.MapPost("/wecom/callback", async (
            [FromBody] WeComCallbackRequest request,
            IAuthService authService) =>
        {
            try
            {
                var result = await authService.HandleWeComCallbackAsync(request.Code, request.State);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("HandleWeComCallback")
        .WithSummary("处理企业微信OAuth回调")
        .Produces<AuthResult>()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);

        // 刷新Token
        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequest request,
            IAuthService authService) =>
        {
            try
            {
                var result = await authService.RefreshTokenAsync(request.RefreshToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("RefreshToken")
        .WithSummary("刷新访问令牌")
        .Produces<AuthResult>()
        .Produces(StatusCodes.Status401Unauthorized);

        // 获取当前用户信息
        group.MapGet("/me", async (
            HttpContext context,
            IAuthService authService) =>
        {
            var token = ExtractTokenFromHeader(context);
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetCurrentUserAsync(token);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(user);
        })
        .WithName("GetCurrentUser")
        .WithSummary("获取当前登录用户信息")
        .RequireAuthorization()
        .Produces<UserInfo>()
        .Produces(StatusCodes.Status401Unauthorized);

        // 企业微信小程序登录
        group.MapPost("/wecom/miniprogram-login", async (
            [FromBody] WeComMiniProgramLoginRequest request,
            IAuthService authService) =>
        {
            try
            {
                var result = await authService.HandleWeComMiniProgramLoginAsync(request.Code);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("WeComMiniProgramLogin")
        .WithSummary("企业微信小程序登录")
        .Produces<AuthResult>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);

        // ========== 用户名密码登录 ==========

        // 用户名密码登录
        group.MapPost("/login", async (
            [FromBody] PasswordLoginRequest request,
            IAuthService authService) =>
        {
            try
            {
                var result = await authService.PasswordLoginAsync(
                    request.Username,
                    request.Password,
                    request.RememberMe
                );
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("PasswordLogin")
        .WithSummary("用户名密码登录")
        .Produces<AuthResult>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);

        // 修改密码
        group.MapPost("/change-password", async (
            HttpContext context,
            [FromBody] ChangePasswordRequest request,
            IAuthService authService) =>
        {
            var token = ExtractTokenFromHeader(context);
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetCurrentUserAsync(token);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var success = await authService.ChangePasswordAsync(
                    Guid.Parse(user.Id),
                    request.OldPassword,
                    request.NewPassword
                );

                return Results.Ok(new { success, message = "Password changed successfully" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("ChangePassword")
        .WithSummary("修改密码")
        .RequireAuthorization()
        .Produces<object>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status500InternalServerError);

        // 重置密码（管理员操作）
        group.MapPost("/reset-password", async (
            HttpContext context,
            [FromBody] ResetPasswordRequest request,
            IAuthService authService) =>
        {
            var token = ExtractTokenFromHeader(context);
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetCurrentUserAsync(token);
            if (user == null || user.Role != "Admin")
            {
                return Results.Forbid();
            }

            try
            {
                var success = await authService.ResetPasswordAsync(
                    Guid.Parse(request.UserId),
                    request.NewPassword,
                    request.MustChangePassword
                );

                return Results.Ok(new { success, message = "Password reset successfully" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("ResetPassword")
        .WithSummary("重置密码（管理员操作）")
        .RequireAuthorization()
        .Produces<object>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError);
    }

    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }
}

/// <summary>
/// 企业微信回调请求
/// </summary>
public class WeComCallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string? State { get; set; }
}

/// <summary>
/// 刷新Token请求
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 企业微信小程序登录请求
/// </summary>
public class WeComMiniProgramLoginRequest
{
    public string Code { get; set; } = string.Empty;
}


