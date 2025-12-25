using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 工单服务接口
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// 创建工单草稿
    /// </summary>
    Task<TicketDto> CreateDraftAsync(CreateTicketRequest request, Guid userId, string? idempotencyKey = null);

    /// <summary>
    /// 更新工单草稿（仅 Draft 状态可更新）
    /// </summary>
    Task<TicketDto> UpdateDraftAsync(Guid ticketId, UpdateTicketRequest request, Guid userId);

    /// <summary>
    /// 提交工单（校验+状态变更）
    /// </summary>
    Task<TicketDto> SubmitTicketAsync(Guid ticketId, Guid userId);

    /// <summary>
    /// 获取工单详情
    /// </summary>
    Task<TicketDto?> GetTicketAsync(Guid ticketId, Guid? userId = null);

    /// <summary>
    /// 获取工单列表
    /// </summary>
    Task<(List<TicketListItemDto> Items, int Total)> GetTicketsAsync(
        TicketQueryFilter filter,
        int page = 1,
        int pageSize = 20);
}

/// <summary>
/// 工单查询过滤器
/// </summary>
public class TicketQueryFilter
{
    public List<string>? Statuses { get; set; }
    public Guid? CustomerId { get; set; }
    public string? DeviceSn { get; set; }
    public char? Domain { get; set; }
    public string? Priority { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}


