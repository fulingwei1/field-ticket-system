namespace FieldTicket.Core.Services;

/// <summary>
/// 工单导出服务接口
/// </summary>
public interface ITicketExportService
{
    /// <summary>
    /// 导出工单列表（Excel格式）
    /// </summary>
    Task<byte[]> ExportToExcelAsync(
        TicketQueryFilter filter,
        List<string>? fields = null);

    /// <summary>
    /// 导出工单列表（CSV格式）
    /// </summary>
    Task<byte[]> ExportToCsvAsync(
        TicketQueryFilter filter,
        List<string>? fields = null);

    /// <summary>
    /// 导出工单详情（Excel格式）
    /// </summary>
    Task<byte[]> ExportTicketDetailToExcelAsync(Guid ticketId);
}




















