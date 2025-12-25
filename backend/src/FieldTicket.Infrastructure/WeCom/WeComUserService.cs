using System.Text.Json;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldTicket.Infrastructure.WeCom;

/// <summary>
/// 企业微信用户服务
/// </summary>
public class WeComUserService
{
    private readonly WeComOptions _options;
    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<WeComUserService> _logger;
    private readonly HttpClient _httpClient;

    public WeComUserService(
        IOptions<WeComOptions> options,
        ApplicationDbContext dbContext,
        IDistributedCache cache,
        ILogger<WeComUserService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// 获取企业微信 Access Token（带缓存）
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        const string cacheKey = "wecom:access_token";
        var cachedToken = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        var url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={_options.CorpId}&corpsecret={_options.Secret}";
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<WeComTokenResponse>(content);
        if (result?.Errcode != 0 || string.IsNullOrEmpty(result.AccessToken))
        {
            throw new Exception($"Failed to get WeCom access token: {result?.Errmsg}");
        }

        // 缓存 token（企业微信 token 有效期 7200 秒，我们缓存 7000 秒）
        await _cache.SetStringAsync(cacheKey, result.AccessToken, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(7000)
        });

        return result.AccessToken;
    }

    /// <summary>
    /// 通过 code 获取用户 ID
    /// </summary>
    public async Task<string> GetUserIdByCodeAsync(string code)
    {
        var accessToken = await GetAccessTokenAsync();
        var url = $"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={accessToken}&code={code}";
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<WeComUserInfoResponse>(content);
        if (result?.Errcode != 0 || string.IsNullOrEmpty(result.UserId))
        {
            throw new Exception($"Failed to get user info from WeCom: {result?.Errmsg}");
        }

        return result.UserId;
    }

    /// <summary>
    /// 获取用户详情并同步到数据库
    /// </summary>
    public async Task<UserInfo> GetAndSyncUserAsync(string weComUserId)
    {
        var accessToken = await GetAccessTokenAsync();
        var url = $"https://qyapi.weixin.qq.com/cgi-bin/user/get?access_token={accessToken}&userid={weComUserId}";
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<WeComUserDetailResponse>(content);
        if (result?.Errcode != 0)
        {
            throw new Exception($"Failed to get user detail from WeCom: {result?.Errmsg}");
        }

        // Upsert 到数据库
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.CorpId == _options.CorpId && u.WeComUserId == weComUserId);

        if (user == null)
        {
            user = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                CorpId = _options.CorpId,
                WeComUserId = weComUserId,
                Name = result.Name ?? string.Empty,
                Mobile = result.Mobile,
                DeptId = result.Department?.FirstOrDefault().ToString(),
                Role = "FieldEngineer", // 默认角色，后续可通过配置或企业微信部门映射
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
        }
        else
        {
            user.Name = result.Name ?? user.Name;
            user.Mobile = result.Mobile ?? user.Mobile;
            user.DeptId = result.Department?.FirstOrDefault().ToString() ?? user.DeptId;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return new UserInfo
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            Mobile = user.Mobile,
            Role = user.Role,
            DeptName = user.DeptId // TODO: 查询部门名称
        };
    }

    private class WeComTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class WeComUserInfoResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("userid")]
        public string? UserId { get; set; }
    }

    private class WeComUserDetailResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("department")]
        public int[]? Department { get; set; }
    }
}


