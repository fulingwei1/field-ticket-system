using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace FieldTicket.Infrastructure.LLM;

/// <summary>
/// 智谱AI服务实现
/// </summary>
public class ZhipuService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZhipuService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public ZhipuService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ZhipuService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // 从配置读取智谱AI设置（支持环境变量）
        _apiKey = _configuration["Zhipu:ApiKey"] ?? 
                  Environment.GetEnvironmentVariable("ZHIPU_API_KEY") ?? 
                  throw new InvalidOperationException("Zhipu:ApiKey not configured. Set ZHIPU_API_KEY environment variable or configure in appsettings.json");
        _baseUrl = _configuration["Zhipu:BaseUrl"] ?? 
                   Environment.GetEnvironmentVariable("ZHIPU_BASE_URL") ?? 
                   "https://open.bigmodel.cn/api/paas/v4";
        _defaultModel = _configuration["Zhipu:Model"] ?? 
                        Environment.GetEnvironmentVariable("ZHIPU_MODEL") ?? 
                        "glm-4";

        // 配置HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FieldTicket-System/1.0");
    }

    public async Task<string> GenerateTextAsync(string prompt, LLMRequestOptions? options = null)
    {
        try
        {
            var messages = new[]
            {
                new { role = "user", content = prompt }
            };

            var requestBody = new
            {
                model = options?.Model ?? _defaultModel,
                messages = messages,
                temperature = options?.Temperature ?? 0.7,
                max_tokens = options?.MaxTokens ?? 2000,
                stream = options?.Stream ?? false
            };

            var response = await _httpClient.PostAsJsonAsync("/chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ZhipuResponse>();
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("No response from Zhipu AI");
            }

            return result.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate text with Zhipu AI");
            throw;
        }
    }

    public async Task<T> GenerateStructuredAsync<T>(string prompt, JsonSchema? schema = null, LLMRequestOptions? options = null) where T : class
    {
        try
        {
            // 构建系统提示，要求返回JSON格式
            var systemPrompt = "你是一个专业的AI助手。请严格按照JSON格式返回结果。";
            if (schema != null)
            {
                systemPrompt += $"\n\n请遵循以下JSON Schema：\n{schema.Definition}";
            }

            var messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            };

            var requestBody = new
            {
                model = options?.Model ?? _defaultModel,
                messages = messages,
                temperature = options?.Temperature ?? 0.3, // 结构化输出使用较低温度
                max_tokens = options?.MaxTokens ?? 2000,
                response_format = new { type = "json_object" } // 强制JSON输出
            };

            var response = await _httpClient.PostAsJsonAsync("/chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ZhipuResponse>();
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("No response from Zhipu AI");
            }

            var jsonContent = result.Choices[0].Message.Content;
            var parsed = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                throw new InvalidOperationException("Failed to parse JSON response");
            }

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate structured output with Zhipu AI");
            throw;
        }
    }

    public async Task<string> ChatAsync(List<ChatMessage> messages, LLMRequestOptions? options = null)
    {
        try
        {
            var requestMessages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray();

            var requestBody = new
            {
                model = options?.Model ?? _defaultModel,
                messages = requestMessages,
                temperature = options?.Temperature ?? 0.7,
                max_tokens = options?.MaxTokens ?? 2000,
                stream = options?.Stream ?? false
            };

            var response = await _httpClient.PostAsJsonAsync("/chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ZhipuResponse>();
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("No response from Zhipu AI");
            }

            return result.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to chat with Zhipu AI");
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // 发送一个简单的测试请求
            var testResponse = await GenerateTextAsync("Hello", new LLMRequestOptions { MaxTokens = 10 });
            return !string.IsNullOrEmpty(testResponse);
        }
        catch
        {
            return false;
        }
    }

    // 智谱AI API响应模型（与OpenAI兼容）
    private class ZhipuResponse
    {
        public List<Choice> Choices { get; set; } = new();
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}


