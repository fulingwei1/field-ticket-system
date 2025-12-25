namespace FieldTicket.Core.Services;

/// <summary>
/// 客户沟通模板服务接口
/// </summary>
public interface ICommunicationTemplateService
{
    /// <summary>
    /// 获取所有模板
    /// </summary>
    Task<List<CommunicationTemplateDto>> GetTemplatesAsync(string? scenario = null);

    /// <summary>
    /// 获取模板详情
    /// </summary>
    Task<CommunicationTemplateDto?> GetTemplateAsync(Guid templateId);

    /// <summary>
    /// 根据编号获取模板
    /// </summary>
    Task<CommunicationTemplateDto?> GetTemplateByCodeAsync(string code);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<Guid> CreateTemplateAsync(CreateCommunicationTemplateRequest request, Guid userId);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task UpdateTemplateAsync(Guid templateId, UpdateCommunicationTemplateRequest request);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task DeleteTemplateAsync(Guid templateId);

    /// <summary>
    /// 渲染模板（替换变量）
    /// </summary>
    Task<string> RenderTemplateAsync(Guid templateId, Dictionary<string, string> variables);

    /// <summary>
    /// 保存沟通记录
    /// </summary>
    Task<Guid> SaveCommunicationAsync(CreateCommunicationRequest request, Guid userId);

    /// <summary>
    /// 获取工单的沟通记录
    /// </summary>
    Task<List<CustomerCommunicationDto>> GetTicketCommunicationsAsync(Guid ticketId);
}

/// <summary>
/// 沟通模板DTO
/// </summary>
public class CommunicationTemplateDto
{
    public Guid TemplateId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建模板请求
/// </summary>
public class CreateCommunicationTemplateRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 更新模板请求
/// </summary>
public class UpdateCommunicationTemplateRequest
{
    public string? Name { get; set; }
    public string? Scenario { get; set; }
    public string? ContentTemplate { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// 创建沟通记录请求
/// </summary>
public class CreateCommunicationRequest
{
    public Guid TicketId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = "wechat"; // call, message, email, other
    public string? CustomerFeedback { get; set; }
    public string? Platform { get; set; } // web, mobile, miniprogram
}

/// <summary>
/// 客户沟通记录DTO
/// </summary>
public class CustomerCommunicationDto
{
    public Guid CommunicationId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty;
    public Guid CommunicatedBy { get; set; }
    public string? CommunicatedByName { get; set; }
    public DateTime CommunicatedAt { get; set; }
    public string? CustomerFeedback { get; set; }
}


