using FieldTicket.Core.Services;
using FieldTicket.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldTicket.Infrastructure.LLM;

/// <summary>
/// Google Gemini服务实现
/// </summary>
public class GoogleGeminiService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleGeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public GoogleGeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleGeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // 从配置读取Google Gemini设置（支持环境变量）
        _apiKey = _configuration["GoogleGemini:ApiKey"] ?? 
                  Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY") ?? 
                  throw new InvalidOperationException("GoogleGemini:ApiKey not configured. Set GOOGLE_GEMINI_API_KEY environment variable or configure in appsettings.json");
        _baseUrl = _configuration["GoogleGemini:BaseUrl"] ?? 
                   Environment.GetEnvironmentVariable("GOOGLE_GEMINI_BASE_URL") ?? 
                   "https://generativelanguage.googleapis.com/v1beta";
        _defaultModel = _configuration["GoogleGemini:Model"] ?? 
                        Environment.GetEnvironmentVariable("GOOGLE_GEMINI_MODEL") ?? 
                        "gemini-pro";

        // 配置HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FieldTicket-System/1.0");
    }

    public async Task<string> GenerateTextAsync(string prompt, LLMRequestOptions? options = null)
    {
        try
        {
            var model = options?.Model ?? _defaultModel;
            var requestBody = new GeminiGenerateContentRequest
            {
                Contents = new[]
                {
                    new GeminiContent
                    {
                        Parts = new[]
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = options?.Temperature ?? 0.7,
                    MaxOutputTokens = options?.MaxTokens ?? 2000
                }
            };

            var url = $"/models/{model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            if (result?.Candidates == null || result.Candidates.Length == 0)
            {
                throw new InvalidOperationException("No response from Google Gemini");
            }

            return result.Candidates[0].Content.Parts[0].Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate text with Google Gemini");
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

            var fullPrompt = $"{systemPrompt}\n\n{prompt}";

            var model = options?.Model ?? _defaultModel;
            var requestBody = new GeminiGenerateContentRequest
            {
                Contents = new[]
                {
                    new GeminiContent
                    {
                        Parts = new[]
                        {
                            new GeminiPart { Text = fullPrompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = options?.Temperature ?? 0.3, // 结构化输出使用较低温度
                    MaxOutputTokens = options?.MaxTokens ?? 2000,
                    ResponseMimeType = "application/json" // 强制JSON输出
                }
            };

            var url = $"/models/{model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            if (result?.Candidates == null || result.Candidates.Length == 0)
            {
                throw new InvalidOperationException("No response from Google Gemini");
            }

            var jsonContent = result.Candidates[0].Content.Parts[0].Text ?? "{}";
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
            _logger.LogError(ex, "Failed to generate structured output with Google Gemini");
            throw;
        }
    }

    public async Task<string> ChatAsync(List<ChatMessage> messages, LLMRequestOptions? options = null)
    {
        try
        {
            var model = options?.Model ?? _defaultModel;
            
            // 将消息转换为Gemini格式
            // Gemini API 使用交替的 user 和 model 角色
            var contents = new List<GeminiContent>();
            string? systemInstruction = null;

            foreach (var message in messages)
            {
                if (message.Role == "system")
                {
                    // System 消息作为 systemInstruction 处理，或者合并到第一个 user 消息
                    if (systemInstruction == null)
                    {
                        systemInstruction = message.Content;
                    }
                    else
                    {
                        systemInstruction += "\n\n" + message.Content;
                    }
                }
                else
                {
                    contents.Add(new GeminiContent
                    {
                        Parts = new[]
                        {
                            new GeminiPart { Text = message.Content }
                        },
                        Role = message.Role == "assistant" ? "model" : "user"
                    });
                }
            }

            // 如果有 system instruction，添加到请求中
            if (!string.IsNullOrEmpty(systemInstruction))
            {
                // 将 system instruction 作为第一个 user 消息的一部分
                if (contents.Count > 0 && contents[0].Role == "user")
                {
                    contents[0].Parts[0].Text = $"{systemInstruction}\n\n{contents[0].Parts[0].Text}";
                }
                else if (contents.Count == 0)
                {
                    // 如果没有其他消息，创建一个 user 消息包含 system instruction
                    contents.Add(new GeminiContent
                    {
                        Parts = new[] { new GeminiPart { Text = systemInstruction } },
                        Role = "user"
                    });
                }
            }

            var requestBody = new GeminiGenerateContentRequest
            {
                Contents = contents.ToArray(),
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = options?.Temperature ?? 0.7,
                    MaxOutputTokens = options?.MaxTokens ?? 2000
                }
            };

            var url = $"/models/{model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            if (result?.Candidates == null || result.Candidates.Length == 0)
            {
                throw new InvalidOperationException("No response from Google Gemini");
            }

            return result.Candidates[0].Content.Parts[0].Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to chat with Google Gemini");
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

    /// <summary>
    /// 使用Vision API分析图片（支持多模态）
    /// </summary>
    public async Task<string> AnalyzeImageWithTextAsync(
        string prompt,
        Stream imageStream,
        string mimeType = "image/jpeg",
        LLMRequestOptions? options = null)
    {
        try
        {
            // 将图片转换为base64
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var imageBase64 = Convert.ToBase64String(imageBytes);

            var model = options?.Model ?? _defaultModel;
            // 如果模型不支持vision，使用gemini-pro-vision
            if (!model.Contains("vision") && !model.Contains("gemini-1.5"))
            {
                model = "gemini-pro-vision";
            }

            var requestBody = new GeminiGenerateContentRequest
            {
                Contents = new[]
                {
                    new GeminiContent
                    {
                        Parts = new[]
                        {
                            new GeminiPart { Text = prompt },
                            new GeminiPart
                            {
                                InlineData = new GeminiInlineData
                                {
                                    MimeType = mimeType,
                                    Data = imageBase64
                                }
                            }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = options?.Temperature ?? 0.7,
                    MaxOutputTokens = options?.MaxTokens ?? 2000,
                    ResponseMimeType = options?.Stream == false ? "application/json" : null
                }
            };

            var url = $"/models/{model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            if (result?.Candidates == null || result.Candidates.Length == 0)
            {
                throw new InvalidOperationException("No response from Google Gemini Vision");
            }

            return result.Candidates[0].Content.Parts[0].Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze image with Google Gemini Vision");
            throw;
        }
    }

    /// <summary>
    /// 使用Vision API分析多张图片
    /// </summary>
    public async Task<string> AnalyzeImagesWithTextAsync(
        string prompt,
        List<(Stream stream, string mimeType)> images,
        LLMRequestOptions? options = null)
    {
        try
        {
            var model = options?.Model ?? _defaultModel;
            if (!model.Contains("vision") && !model.Contains("gemini-1.5"))
            {
                model = "gemini-pro-vision";
            }

            var parts = new List<GeminiPart> { new GeminiPart { Text = prompt } };

            foreach (var (imageStream, mimeType) in images)
            {
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var imageBase64 = Convert.ToBase64String(imageBytes);

                parts.Add(new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = mimeType,
                        Data = imageBase64
                    }
                });
            }

            var requestBody = new GeminiGenerateContentRequest
            {
                Contents = new[]
                {
                    new GeminiContent
                    {
                        Parts = parts.ToArray()
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = options?.Temperature ?? 0.7,
                    MaxOutputTokens = options?.MaxTokens ?? 2000,
                    ResponseMimeType = options?.Stream == false ? "application/json" : null
                }
            };

            var url = $"/models/{model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            if (result?.Candidates == null || result.Candidates.Length == 0)
            {
                throw new InvalidOperationException("No response from Google Gemini Vision");
            }

            return result.Candidates[0].Content.Parts[0].Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze images with Google Gemini Vision");
            throw;
        }
    }

    // Google Gemini API请求模型
    private class GeminiGenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();

        [JsonPropertyName("role")]
        public string? Role { get; set; }
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiInlineData? InlineData { get; set; }
    }

    private class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }

        [JsonPropertyName("responseMimeType")]
        public string? ResponseMimeType { get; set; }
    }

    // Google Gemini API响应模型
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();
    }
}

