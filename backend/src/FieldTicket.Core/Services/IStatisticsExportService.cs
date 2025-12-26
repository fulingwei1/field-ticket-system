namespace FieldTicket.Core.Services;

/// <summary>
/// 统计数据导出服务接口
/// </summary>
public interface IStatisticsExportService
{
    /// <summary>
    /// 导出统计概览（Excel/CSV）
    /// </summary>
    Task<byte[]> ExportOverviewAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv");

    /// <summary>
    /// 导出Top问题域统计（Excel/CSV）
    /// </summary>
    Task<byte[]> ExportTopDomainsAsync(
        int topN = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv");

    /// <summary>
    /// 导出闭环时间分布（Excel/CSV）
    /// </summary>
    Task<byte[]> ExportClosureTimeDistributionAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv");

    /// <summary>
    /// 导出状态统计（Excel/CSV）
    /// </summary>
    Task<byte[]> ExportStatusStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv");

    /// <summary>
    /// 导出趋势数据（Excel/CSV）
    /// </summary>
    Task<byte[]> ExportTrendDataAsync(
        string metricType,
        DateTime fromDate,
        DateTime toDate,
        string groupBy = "day",
        string format = "csv");

    /// <summary>
    /// 导出完整统计报告（包含所有统计数据）
    /// </summary>
    Task<byte[]> ExportFullReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv");
}








