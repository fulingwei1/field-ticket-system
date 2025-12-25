using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// LLM服务接口
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// 调用LLM生成文本
    /// </summary>
    Task<string> GenerateTextAsync(string prompt, LLMRequestOptions? options = null);

    /// <summary>
    /// 调用LLM生成结构化输出（JSON）
    /// </summary>
    Task<T> GenerateStructuredAsync<T>(string prompt, JsonSchema? schema = null, LLMRequestOptions? options = null) where T : class;

    /// <summary>
    /// 调用LLM进行对话
    /// </summary>
    Task<string> ChatAsync(List<ChatMessage> messages, LLMRequestOptions? options = null);

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// LLM请求选项
/// </summary>
public class LLMRequestOptions
{
    /// <summary>
    /// 模型名称（如：gpt-4o, gpt-4-turbo）
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 温度（0-2，越高越随机）
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// 最大Token数
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 是否启用流式输出
    /// </summary>
    public bool Stream { get; set; } = false;
}

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 角色（system/user/assistant）
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// JSON Schema（用于结构化输出）
/// </summary>
public class JsonSchema
{
    /// <summary>
    /// Schema定义（JSON字符串）
    /// </summary>
    public string Definition { get; set; } = string.Empty;
}

