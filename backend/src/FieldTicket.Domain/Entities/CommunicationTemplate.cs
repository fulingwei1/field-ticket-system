namespace FieldTicket.Domain.Entities;

/// <summary>
/// 客户沟通模板实体
/// </summary>
public class CommunicationTemplate
{
    public Guid TemplateId { get; set; }
    
    /// <summary>
    /// 模板编号（唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 使用场景：problem_confirmed/solution_issued/verification_pass
    /// </summary>
    public string Scenario { get; set; } = string.Empty;
    
    /// <summary>
    /// 模板内容，支持变量替换
    /// </summary>
    public string ContentTemplate { get; set; } = string.Empty;
    
    /// <summary>
    /// 变量定义（JSON）
    /// </summary>
    public System.Text.Json.JsonDocument? Variables { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 创建人
    /// </summary>
    public Guid? CreatedBy { get; set; }
}









