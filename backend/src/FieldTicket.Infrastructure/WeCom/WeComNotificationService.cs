using System.Text;
using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldTicket.Infrastructure.WeCom;

/// <summary>
/// 企业微信通知服务实现
/// </summary>
public class WeComNotificationService : IWeComNotificationService
{
    private readonly WeComOptions _options;
    private readonly ApplicationDbContext _dbContext;
    private readonly WeComUserService _userService;
    private readonly ILogger<WeComNotificationService> _logger;
    private readonly HttpClient _httpClient;

    public WeComNotificationService(
        IOptions<WeComOptions> options,
        ApplicationDbContext dbContext,
        WeComUserService userService,
        ILogger<WeComNotificationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _dbContext = dbContext;
        _userService = userService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// 发送通知
    /// </summary>
    public async Task SendNotificationAsync(string[] toUserIds, string content, int retryCount = 3)
    {
        if (toUserIds == null || toUserIds.Length == 0)
        {
            _logger.LogWarning("通知接收人列表为空，跳过发送");
            return;
        }

        var accessToken = await _userService.GetAccessTokenAsync();
        var url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={accessToken}";

        var requestBody = new
        {
            touser = string.Join("|", toUserIds),
            msgtype = "text",
            agentid = _options.AgentId,
            text = new
            {
                content = content
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        int attempt = 0;
        while (attempt < retryCount)
        {
            try
            {
                attempt++;
                var response = await _httpClient.PostAsync(url, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<WeComMessageResponse>(responseContent);

                if (result?.Errcode == 0)
                {
                    _logger.LogInformation("企业微信通知发送成功，接收人：{UserIds}", string.Join(",", toUserIds));
                    return;
                }

                // 处理需要刷新 token 的错误
                if (result?.Errcode == 40001 || result?.Errcode == 40014)
                {
                    _logger.LogWarning("Access token 失效，尝试刷新后重试。错误码：{Errcode}", result.Errcode);
                    
                    // 清除缓存的 token（通过 WeComUserService 的缓存机制）
                    // 由于 token 是通过 IDistributedCache 缓存的，会在下次调用 GetAccessTokenAsync 时自动刷新
                    
                    // 重新获取 token
                    accessToken = await _userService.GetAccessTokenAsync();
                    url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={accessToken}";
                    
                    if (attempt < retryCount)
                    {
                        await Task.Delay(1000 * attempt); // 指数退避
                        continue;
                    }
                }

                // 其他错误，记录日志但不重试
                _logger.LogError("企业微信通知发送失败。错误码：{Errcode}，错误信息：{Errmsg}，接收人：{UserIds}",
                    result?.Errcode, result?.Errmsg, string.Join(",", toUserIds));
                
                if (attempt >= retryCount)
                {
                    throw new Exception($"企业微信通知发送失败，已重试 {retryCount} 次。错误码：{result?.Errcode}，错误信息：{result?.Errmsg}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "企业微信通知发送异常，第 {Attempt} 次尝试", attempt);
                
                if (attempt >= retryCount)
                {
                    throw;
                }
                
                await Task.Delay(1000 * attempt); // 指数退避
            }
        }
    }

    /// <summary>
    /// 发送工单提交通知
    /// </summary>
    public async Task NotifyTicketSubmittedAsync(Guid ticketId)
    {
        try
        {
            var ticket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                _logger.LogWarning("工单 {TicketId} 不存在，跳过通知", ticketId);
                return;
            }

            // 获取创建者信息
            var creator = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == ticket.CreatedByUserId);

            // 获取高级工程师列表（按角色筛选）
            var seniorEngineers = await _dbContext.Users
                .Where(u => u.Role == "SeniorEngineer" && u.IsActive)
                .Select(u => u.WeComUserId)
                .Where(uid => !string.IsNullOrEmpty(uid))
                .ToListAsync();

            if (seniorEngineers.Count == 0)
            {
                _logger.LogWarning("没有找到高级工程师，跳过工单提交通知");
                return;
            }

            // 准备模板变量
            var variables = new Dictionary<string, string>
            {
                { "ticket_no", ticket.TicketNo ?? "草稿" },
                { "customer_name", "待获取" }, // TODO: 从设备获取客户名称
                { "device_sn", "待获取" }, // TODO: 从设备获取设备序列号
                { "symptom_title", ticket.SymptomTitle },
                { "priority", ticket.Priority },
                { "creator_name", creator?.Name ?? "未知" },
                { "web_url", $"{GetWebBaseUrl()}/tickets/{ticketId}" }
            };

            var content = NotificationTemplates.ReplaceVariables(NotificationTemplates.TicketSubmitted, variables);

            // 发送通知
            await SendNotificationAsync(seniorEngineers.ToArray(), content);

            _logger.LogInformation("工单 {TicketId} 提交通知已发送给 {Count} 位高级工程师", 
                ticketId, seniorEngineers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送工单提交通知失败，工单ID：{TicketId}", ticketId);
            // 不抛出异常，避免影响主流程
        }
    }

    /// <summary>
    /// 发送解决方案发布通知
    /// </summary>
    public async Task NotifySolutionPublishedAsync(Guid solutionId)
    {
        try
        {
            var solution = await _dbContext.Solutions
                .FirstOrDefaultAsync(s => s.SolutionId == solutionId);

            if (solution == null)
            {
                _logger.LogWarning("解决方案 {SolutionId} 不存在，跳过通知", solutionId);
                return;
            }

            var ticket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.TicketId == solution.TicketId);

            if (ticket == null)
            {
                _logger.LogWarning("工单 {TicketId} 不存在，跳过解决方案发布通知", solution.TicketId);
                return;
            }

            // 获取工单创建者
            var creator = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == ticket.CreatedByUserId);

            // 获取接收人列表（工单创建者 + 客服）
            var recipients = new List<string>();
            
            if (creator != null && !string.IsNullOrEmpty(creator.WeComUserId))
            {
                recipients.Add(creator.WeComUserId);
            }

            // 获取客服列表（按角色筛选）
            var customerService = await _dbContext.Users
                .Where(u => u.Role == "CustomerService" && u.IsActive)
                .Select(u => u.WeComUserId)
                .Where(uid => !string.IsNullOrEmpty(uid))
                .ToListAsync();

            recipients.AddRange(customerService);

            if (recipients.Count == 0)
            {
                _logger.LogWarning("没有找到接收人，跳过解决方案发布通知");
                return;
            }

            // 准备模板变量
            var variables = new Dictionary<string, string>
            {
                { "solution_code", solution.SolutionCode },
                { "ticket_no", ticket.TicketNo ?? "草稿" },
                { "summary", solution.Title },
                { "app_deeplink", $"fieldticket://solution/{solutionId}" } // 移动端深度链接
            };

            var content = NotificationTemplates.ReplaceVariables(NotificationTemplates.SolutionPublished, variables);

            // 发送通知
            await SendNotificationAsync(recipients.ToArray(), content);

            _logger.LogInformation("解决方案 {SolutionId} 发布通知已发送给 {Count} 位接收人", 
                solutionId, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送解决方案发布通知失败，解决方案ID：{SolutionId}", solutionId);
            // 不抛出异常，避免影响主流程
        }
    }

    /// <summary>
    /// 获取Web基础URL（从配置）
    /// </summary>
    private string GetWebBaseUrl()
    {
        return _options.WebBaseUrl;
    }

    private class WeComMessageResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("errmsg")]
        public string? Errmsg { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("invaliduser")]
        public string? InvalidUser { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("invalidparty")]
        public string? InvalidParty { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("invalidtag")]
        public string? InvalidTag { get; set; }
    }
}

