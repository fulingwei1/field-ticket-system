using System.Text.Json;
using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 工单模板服务接口
/// </summary>
public interface ITicketTemplateService
{
    /// <summary>
    /// 创建模板（从工单或手动创建）
    /// </summary>
    Task<TicketTemplateDto> CreateTemplateAsync(CreateTicketTemplateRequest request, Guid userId);

    /// <summary>
    /// 从工单创建模板
    /// </summary>
    Task<TicketTemplateDto> CreateTemplateFromTicketAsync(Guid ticketId, string name, string? description, Guid userId);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task<TicketTemplateDto> UpdateTemplateAsync(Guid templateId, UpdateTicketTemplateRequest request, Guid userId);

    /// <summary>
    /// 删除模板（软删除）
    /// </summary>
    Task DeleteTemplateAsync(Guid templateId, Guid userId);

    /// <summary>
    /// 获取模板详情
    /// </summary>
    Task<TicketTemplateDto> GetTemplateAsync(Guid templateId, Guid userId);

    /// <summary>
    /// 获取模板列表
    /// </summary>
    Task<List<TicketTemplateDto>> GetTemplatesAsync(Guid userId, string? category = null, bool includePublic = true);

    /// <summary>
    /// 使用模板创建工单草稿
    /// </summary>
    Task<CreateTicketRequest> ApplyTemplateAsync(Guid templateId, Guid? deviceId, Guid userId);
}

/// <summary>
/// 工单模板DTO
/// </summary>
public class TicketTemplateDto
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsPublic { get; set; }
    public JsonDocument TemplateData { get; set; } = JsonDocument.Parse("{}");
}

/// <summary>
/// 创建模板请求
/// </summary>
public class CreateTicketTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public JsonDocument TemplateData { get; set; } = JsonDocument.Parse("{}");
}

/// <summary>
/// 更新模板请求
/// </summary>
public class UpdateTicketTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool? IsPublic { get; set; }
    public JsonDocument? TemplateData { get; set; }
}










