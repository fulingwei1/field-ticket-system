using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 工单状态历史记录扩展方法
/// </summary>
public static class TicketStatusHistoryExtensions
{
    /// <summary>
    /// 记录工单状态变更（异步，不阻塞主流程）
    /// </summary>
    public static void RecordStatusChangeAsync(
        this ITicketStatusHistoryService service,
        Ticket ticket,
        string newStatus,
        Guid changedBy,
        string? changedByName = null,
        string? changeReason = null,
        string changeType = "manual",
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        string? notes = null)
    {
        // 异步执行，不阻塞主流程
        _ = Task.Run(async () =>
        {
            try
            {
                await service.RecordStatusChangeAsync(
                    ticketId: ticket.TicketId,
                    fromStatus: ticket.Status,
                    toStatus: newStatus,
                    changedBy: changedBy,
                    changedByName: changedByName,
                    changeReason: changeReason,
                    changeType: changeType,
                    relatedEntityId: relatedEntityId,
                    relatedEntityType: relatedEntityType,
                    notes: notes
                );
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，避免影响主流程
                // 这里可以注入ILogger来记录错误
            }
        });
    }
}








