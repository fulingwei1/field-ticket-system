using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Auth;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.WeCom;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly Microsoft.Extensions.Options.IOptions<WeComOptions> _weComOptions;
    private readonly WeComUserService _weComUserService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        Microsoft.Extensions.Options.IOptions<WeComOptions> weComOptions,
        WeComUserService weComUserService,
        JwtTokenService jwtTokenService,
        ApplicationDbContext dbContext,
        IDistributedCache cache,
        ILogger<AuthService> logger)
    {
        _weComOptions = weComOptions;
        _weComUserService = weComUserService;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeComLoginUrlResponse> GetWeComLoginUrlAsync(string? state = null)
    {
        // 生成 state（CSRF 防护）
        state ??= Guid.NewGuid().ToString("N");

        // 缓存 state（5分钟有效期）
        await _cache.SetStringAsync($"wecom:state:{state}", "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        // 构建企业微信授权URL
        var options = _weComOptions.Value;
        var url = $"https://open.weixin.qq.com/connect/oauth2/authorize?" +
                  $"appid={options.CorpId}&" +
                  $"redirect_uri={Uri.EscapeDataString(options.RedirectUri)}&" +
                  $"response_type=code&" +
                  $"scope=snsapi_base&" +
                  $"state={state}&" +
                  $"agentid={options.AgentId}#wechat_redirect";

        return new WeComLoginUrlResponse
        {
            Url = url,
            State = state
        };
    }

    public async Task<AuthResult> HandleWeComCallbackAsync(string code, string? state)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Code is required", nameof(code));
        }

        // 验证 state（CSRF 防护 - 必需）
        if (string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("OAuth callback attempted without state parameter - possible CSRF attack");
            throw new UnauthorizedAccessException("State parameter is required for CSRF protection");
        }

        var cachedState = await _cache.GetStringAsync($"wecom:state:{state}");
        if (string.IsNullOrEmpty(cachedState))
        {
            _logger.LogWarning("OAuth callback with invalid state parameter: {State}", state);
            throw new UnauthorizedAccessException("Invalid or expired state parameter");
        }

        // 使用后删除 state
        await _cache.RemoveAsync($"wecom:state:{state}");

        try
        {
            // 1. 通过 code 获取 userid
            var weComUserId = await _weComUserService.GetUserIdByCodeAsync(code);

            // 2. 获取用户详情并同步到数据库
            var userInfo = await _weComUserService.GetAndSyncUserAsync(weComUserId);

            // 3. 生成 JWT Token
            var token = _jwtTokenService.GenerateToken(userInfo);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // 4. 缓存 refresh token（30天有效期）
            await _cache.SetStringAsync($"refresh_token:{refreshToken}", userInfo.Id, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            });

            _logger.LogInformation("User {UserId} logged in successfully via WeCom", userInfo.Id);

            return new AuthResult
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = 7 * 24 * 60 * 60, // 7 days in seconds
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle WeCom callback");
            throw;
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new ArgumentException("Refresh token is required", nameof(refreshToken));
        }

        // 验证 refresh token
        var userId = await _cache.GetStringAsync($"refresh_token:{refreshToken}");
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // 获取用户信息
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        var userInfo = new UserInfo
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            Mobile = user.Mobile,
            Role = user.Role,
            DeptName = user.DeptId
        };

        // 生成新的 token
        var newToken = _jwtTokenService.GenerateToken(userInfo);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // 删除旧的 refresh token
        await _cache.RemoveAsync($"refresh_token:{refreshToken}");

        // 缓存新的 refresh token
        await _cache.SetStringAsync($"refresh_token:{newRefreshToken}", userId, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });

        return new AuthResult
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 7 * 24 * 60 * 60,
            User = userInfo
        };
    }

    public async Task<UserInfo?> GetCurrentUserAsync(string token)
    {
        var principal = _jwtTokenService.ValidateToken(token);
        if (principal == null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
        {
            return null;
        }

        return new UserInfo
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            Mobile = user.Mobile,
            Role = user.Role,
            DeptName = user.DeptId
        };
    }

    public async Task<AuthResult> HandleWeComMiniProgramLoginAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Code is required", nameof(code));
        }

        try
        {
            // 1. 通过 code 获取 userid（小程序和Web端使用相同的API）
            var weComUserId = await _weComUserService.GetUserIdByCodeAsync(code);

            // 2. 获取用户详情并同步到数据库
            var userInfo = await _weComUserService.GetAndSyncUserAsync(weComUserId);

            // 3. 生成 JWT Token
            var token = _jwtTokenService.GenerateToken(userInfo);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // 4. 缓存 refresh token（30天有效期）
            await _cache.SetStringAsync($"refresh_token:{refreshToken}", userInfo.Id, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            });

            _logger.LogInformation("User {UserId} logged in successfully via WeCom MiniProgram", userInfo.Id);

            return new AuthResult
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = 7 * 24 * 60 * 60, // 7 days in seconds
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle WeCom MiniProgram login");
            throw;
        }
    }

    // ========== 用户名密码登录实现 ==========

    public async Task<AuthResult> PasswordLoginAsync(string username, string password, bool rememberMe = false)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required", nameof(password));
        }

        try
        {
            // 1. 查找用户
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.LoginType == "Password");

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login failed: user not found or inactive - {Username}", username);
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // 检查账户是否已开通
            if (!user.IsActivated)
            {
                _logger.LogWarning("Login failed: account not activated - {Username}", username);
                throw new UnauthorizedAccessException("账户未开通，请联系管理员");
            }

            // 2. 验证密码
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: invalid password - {Username}", username);
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // 3. 构建用户信息
            var userInfo = new UserInfo
            {
                Id = user.Id.ToString(),
                Username = user.Username ?? string.Empty,
                Name = user.Name,
                Mobile = user.Mobile,
                Email = user.Email,
                Role = user.Role,
                DeptName = user.DeptId,
                LoginType = user.LoginType,
                MustChangePassword = user.MustChangePassword
            };

            // 4. 生成Token
            var token = _jwtTokenService.GenerateToken(userInfo);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // 5. 缓存 refresh token
            var expirationDays = rememberMe ? 30 : 7;
            await _cache.SetStringAsync($"refresh_token:{refreshToken}", user.Id.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(expirationDays)
            });

            _logger.LogInformation("User {UserId} logged in successfully via Password", user.Id);

            return new AuthResult
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = expirationDays * 24 * 60 * 60,
                User = userInfo
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle password login for {Username}", username);
            throw new Exception("Login failed. Please try again later.");
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            throw new ArgumentException("Old password is required", nameof(oldPassword));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password is required", nameof(newPassword));
        }

        if (newPassword.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters long", nameof(newPassword));
        }

        try
        {
            var user = await _dbContext.Users.FindAsync(userId);

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive");
            }

            if (user.LoginType != "Password")
            {
                throw new InvalidOperationException("Cannot change password for non-password login users");
            }

            // 验证旧密码
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid old password");
            }

            // 更新密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChangeAt = DateTime.UtcNow;
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed password successfully", userId);

            return true;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to change password for user {UserId}", userId);
            throw new Exception("Failed to change password. Please try again later.");
        }
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword = true)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password is required", nameof(newPassword));
        }

        if (newPassword.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters long", nameof(newPassword));
        }

        try
        {
            var user = await _dbContext.Users.FindAsync(userId);

            if (user == null)
            {
                throw new ArgumentException("User not found", nameof(userId));
            }

            if (user.LoginType != "Password")
            {
                throw new InvalidOperationException("Cannot reset password for non-password login users");
            }

            // 重置密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChangeAt = DateTime.UtcNow;
            user.MustChangePassword = mustChangePassword;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Password reset for user {UserId} by admin", userId);

            return true;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to reset password for user {UserId}", userId);
            throw new Exception("Failed to reset password. Please try again later.");
        }
    }
}

