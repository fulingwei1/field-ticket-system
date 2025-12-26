namespace FieldTicket.Domain.Entities;

/// <summary>
/// 客户沟通记录实体
/// </summary>
public class CustomerCommunication
{
    public Guid CommunicationId { get; set; }
    
    /// <summary>
    /// 关联工单ID
    /// </summary>
    public Guid TicketId { get; set; }
    
    /// <summary>
    /// 使用的模板ID
    /// </summary>
    public Guid? TemplateId { get; set; }
    
    /// <summary>
    /// 沟通内容（替换变量后的最终内容）
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 沟通方式：wechat/phone/email/other
    /// </summary>
    public string CommunicationType { get; set; } = "wechat";
    
    /// <summary>
    /// 沟通人（用户ID）
    /// </summary>
    public Guid CommunicatedBy { get; set; }
    
    /// <summary>
    /// 沟通时间
    /// </summary>
    public DateTime CommunicatedAt { get; set; }
    
    /// <summary>
    /// 客户反馈（可选）
    /// </summary>
    public string? CustomerFeedback { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}









