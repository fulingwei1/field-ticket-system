using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldTicket.Infrastructure.WeCom;

/// <summary>
/// 企业微信通讯录服务实现
/// </summary>
public class WeComContactService : IWeComContactService
{
    private readonly WeComOptions _options;
    private readonly WeComUserService _userService;
    private readonly RoleMappingConfig _roleMappingConfig;
    private readonly IDistributedCache _cache;
    private readonly ILogger<WeComContactService> _logger;
    private readonly HttpClient _httpClient;

    public WeComContactService(
        IOptions<WeComOptions> options,
        WeComUserService userService,
        RoleMappingConfig roleMappingConfig,
        IDistributedCache cache,
        ILogger<WeComContactService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _userService = userService;
        _roleMappingConfig = roleMappingConfig;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<WeComDepartmentDto>> GetDepartmentsAsync(int? parentId = null)
    {
        try
        {
            var accessToken = await _userService.GetAccessTokenAsync();
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/department/list?access_token={accessToken}";
            if (parentId.HasValue)
            {
                url += $"&id={parentId.Value}";
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<WeComDepartmentListResponse>(content);
            if (result?.Errcode != 0)
            {
                throw new Exception($"Failed to get departments: {result?.Errmsg}");
            }

            return result.Department?.Select(d => new WeComDepartmentDto
            {
                Id = d.Id,
                Name = d.Name ?? string.Empty,
                ParentId = d.ParentId == 0 ? null : d.ParentId,
                Order = d.Order
            }).ToList() ?? new List<WeComDepartmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门列表失败");
            throw;
        }
    }

    public async Task<List<WeComUserDto>> GetUsersByDepartmentAsync(int departmentId, bool fetchChild = false)
    {
        try
        {
            var accessToken = await _userService.GetAccessTokenAsync();
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/user/list?access_token={accessToken}&department_id={departmentId}&fetch_child={(fetchChild ? 1 : 0)}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<WeComUserListResponse>(content);
            if (result?.Errcode != 0)
            {
                throw new Exception($"Failed to get users: {result?.Errmsg}");
            }

            return result.UserList?.Select(u => new WeComUserDto
            {
                UserId = u.UserId ?? string.Empty,
                Name = u.Name ?? string.Empty,
                Mobile = u.Mobile,
                Email = u.Email,
                Avatar = u.Avatar,
                Departments = u.Department,
                Tags = u.ExtAttr?.FirstOrDefault(e => e.Type == 0)?.Text?.Value?.Split(',') ?? Array.Empty<string>(),
                IsActive = u.Status == 1
            }).ToList() ?? new List<WeComUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取部门用户列表失败，部门ID：{DepartmentId}", departmentId);
            throw;
        }
    }

    public async Task<WeComUserDto?> GetUserDetailAsync(string userId)
    {
        try
        {
            var accessToken = await _userService.GetAccessTokenAsync();
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/user/get?access_token={accessToken}&userid={userId}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<WeComUserDetailResponse>(content);
            if (result?.Errcode != 0)
            {
                if (result?.Errcode == 60111) // 用户不存在
                {
                    return null;
                }
                throw new Exception($"Failed to get user detail: {result?.Errmsg}");
            }

            return new WeComUserDto
            {
                UserId = result.UserId ?? string.Empty,
                Name = result.Name ?? string.Empty,
                Mobile = result.Mobile,
                Email = result.Email,
                Avatar = result.Avatar,
                Departments = result.Department,
                Tags = result.ExtAttr?.FirstOrDefault(e => e.Type == 0)?.Text?.Value?.Split(',') ?? Array.Empty<string>(),
                IsActive = result.Status == 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详情失败，用户ID：{UserId}", userId);
            throw;
        }
    }

    public async Task<List<WeComUserDto>> GetUsersByRoleAsync(string role)
    {
        var users = new List<WeComUserDto>();

        // 通过标签筛选
        var tags = _roleMappingConfig.GetWeComTagsForRole(role);
        if (tags.Count > 0)
        {
            var allUsers = await GetAllUsersAsync();
            foreach (var user in allUsers)
            {
                if (user.Tags != null && user.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                {
                    users.Add(user);
                }
            }
        }

        // 通过部门筛选
        var departmentNames = _roleMappingConfig.GetDepartmentNamesForRole(role);
        if (departmentNames.Count > 0)
        {
            var allDepartments = await GetDepartmentsAsync();
            var targetDepartmentIds = allDepartments
                .Where(d => departmentNames.Contains(d.Name, StringComparer.OrdinalIgnoreCase))
                .Select(d => d.Id)
                .ToList();

            foreach (var deptId in targetDepartmentIds)
            {
                var deptUsers = await GetUsersByDepartmentAsync(deptId, fetchChild: true);
                users.AddRange(deptUsers);
            }
        }

        // 去重
        return users.GroupBy(u => u.UserId).Select(g => g.First()).ToList();
    }

    public async Task<List<WeComUserDto>> GetUsersByDepartmentNameAsync(string departmentName)
    {
        var departments = await GetDepartmentsAsync();
        var targetDepartment = departments.FirstOrDefault(d => 
            d.Name.Equals(departmentName, StringComparison.OrdinalIgnoreCase));

        if (targetDepartment == null)
        {
            _logger.LogWarning("部门不存在：{DepartmentName}", departmentName);
            return new List<WeComUserDto>();
        }

        return await GetUsersByDepartmentAsync(targetDepartment.Id, fetchChild: true);
    }

    public async Task<List<WeComChatDto>> GetChatsAsync()
    {
        try
        {
            var accessToken = await _userService.GetAccessTokenAsync();
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/appchat/list?access_token={accessToken}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<WeComChatListResponse>(content);
            if (result?.Errcode != 0)
            {
                throw new Exception($"Failed to get chats: {result?.Errmsg}");
            }

            return result.ChatList?.Select(c => new WeComChatDto
            {
                ChatId = c.ChatId ?? string.Empty,
                Name = c.Name ?? string.Empty,
                Owner = c.Owner,
                UserList = c.UserList
            }).ToList() ?? new List<WeComChatDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取群列表失败");
            throw;
        }
    }

    public async Task<List<WeComUserDto>> GetAllUsersAsync(int? departmentId = null)
    {
        var users = new List<WeComUserDto>();

        if (departmentId.HasValue)
        {
            return await GetUsersByDepartmentAsync(departmentId.Value, fetchChild: true);
        }

        // 获取所有部门
        var departments = await GetDepartmentsAsync();
        foreach (var dept in departments)
        {
            var deptUsers = await GetUsersByDepartmentAsync(dept.Id, fetchChild: false);
            users.AddRange(deptUsers);
        }

        // 去重
        return users.GroupBy(u => u.UserId).Select(g => g.First()).ToList();
    }

    #region 企业微信 API 响应模型

    private class WeComDepartmentListResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [JsonPropertyName("department")]
        public List<WeComDepartmentItem>? Department { get; set; }
    }

    private class WeComDepartmentItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("parentid")]
        public int ParentId { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }

    private class WeComUserListResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [JsonPropertyName("userlist")]
        public List<WeComUserItem>? UserList { get; set; }
    }

    private class WeComUserItem
    {
        [JsonPropertyName("userid")]
        public string? UserId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("department")]
        public int[]? Department { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("extattr")]
        public List<WeComExtAttr>? ExtAttr { get; set; }
    }

    private class WeComUserDetailResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [JsonPropertyName("userid")]
        public string? UserId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("department")]
        public int[]? Department { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("extattr")]
        public List<WeComExtAttr>? ExtAttr { get; set; }
    }

    private class WeComExtAttr
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("text")]
        public WeComExtAttrText? Text { get; set; }
    }

    private class WeComExtAttrText
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    private class WeComChatListResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [JsonPropertyName("chatlist")]
        public List<WeComChatItem>? ChatList { get; set; }
    }

    private class WeComChatItem
    {
        [JsonPropertyName("chatid")]
        public string? ChatId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("userlist")]
        public string[]? UserList { get; set; }
    }

    #endregion
}

