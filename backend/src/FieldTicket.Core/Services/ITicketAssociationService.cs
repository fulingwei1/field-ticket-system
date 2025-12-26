namespace FieldTicket.Core.Services;

/// <summary>
/// 工单关联分析服务接口
/// </summary>
public interface ITicketAssociationService
{
    /// <summary>
    /// 获取工单的所有关联工单
    /// </summary>
    Task<TicketAssociationDto> GetAssociationsAsync(Guid ticketId);

    /// <summary>
    /// 获取相似工单（基于去重检测）
    /// </summary>
    Task<List<AssociatedTicketDto>> GetSimilarTicketsAsync(Guid ticketId, int maxResults = 10);

    /// <summary>
    /// 获取设备关联工单
    /// </summary>
    Task<List<AssociatedTicketDto>> GetDeviceRelatedTicketsAsync(Guid ticketId, int maxResults = 10);

    /// <summary>
    /// 获取客户关联工单
    /// </summary>
    Task<List<AssociatedTicketDto>> GetCustomerRelatedTicketsAsync(Guid ticketId, int maxResults = 10);

    /// <summary>
    /// 获取问题域关联工单
    /// </summary>
    Task<List<AssociatedTicketDto>> GetDomainRelatedTicketsAsync(Guid ticketId, int maxResults = 10);
}

/// <summary>
/// 工单关联信息DTO
/// </summary>
public class TicketAssociationDto
{
    public Guid TicketId { get; set; }
    public List<AssociatedTicketDto> SimilarTickets { get; set; } = new();
    public List<AssociatedTicketDto> DeviceRelatedTickets { get; set; } = new();
    public List<AssociatedTicketDto> CustomerRelatedTickets { get; set; } = new();
    public List<AssociatedTicketDto> DomainRelatedTickets { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// 关联工单DTO
/// </summary>
public class AssociatedTicketDto
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string SymptomTitle { get; set; } = string.Empty;
    public char Domain { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? AssociationType { get; set; } // "similar", "device", "customer", "domain"
    public decimal? SimilarityScore { get; set; } // 相似度评分（0-1）
    public string? AssociationReason { get; set; } // 关联原因
}










