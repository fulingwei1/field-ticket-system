using FieldTicket.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 统计数据导出服务实现
/// </summary>
public class StatisticsExportService : IStatisticsExportService
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsExportService> _logger;

    public StatisticsExportService(
        IStatisticsService statisticsService,
        ILogger<StatisticsExportService> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    public async Task<byte[]> ExportOverviewAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv")
    {
        _logger.LogInformation("Exporting statistics overview, format: {Format}", format);

        var overview = await _statisticsService.GetOverviewAsync(fromDate, toDate);

        if (format.ToLower() == "csv")
        {
            return GenerateOverviewCsv(overview);
        }
        else
        {
            // Excel 格式暂时返回 CSV
            return GenerateOverviewCsv(overview);
        }
    }

    public async Task<byte[]> ExportTopDomainsAsync(
        int topN = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv")
    {
        _logger.LogInformation("Exporting top domains statistics, format: {Format}", format);

        var domains = await _statisticsService.GetTopDomainsAsync(topN, fromDate, toDate);

        if (format.ToLower() == "csv")
        {
            return GenerateTopDomainsCsv(domains);
        }
        else
        {
            return GenerateTopDomainsCsv(domains);
        }
    }

    public async Task<byte[]> ExportClosureTimeDistributionAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv")
    {
        _logger.LogInformation("Exporting closure time distribution, format: {Format}", format);

        var distribution = await _statisticsService.GetClosureTimeDistributionAsync(fromDate, toDate);

        if (format.ToLower() == "csv")
        {
            return GenerateClosureTimeDistributionCsv(distribution);
        }
        else
        {
            return GenerateClosureTimeDistributionCsv(distribution);
        }
    }

    public async Task<byte[]> ExportStatusStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv")
    {
        _logger.LogInformation("Exporting status statistics, format: {Format}", format);

        var statusStats = await _statisticsService.GetStatusStatisticsAsync(fromDate, toDate);

        if (format.ToLower() == "csv")
        {
            return GenerateStatusStatisticsCsv(statusStats);
        }
        else
        {
            return GenerateStatusStatisticsCsv(statusStats);
        }
    }

    public async Task<byte[]> ExportTrendDataAsync(
        string metricType,
        DateTime fromDate,
        DateTime toDate,
        string groupBy = "day",
        string format = "csv")
    {
        _logger.LogInformation("Exporting trend data, format: {Format}", format);

        var trends = await _statisticsService.GetTrendDataAsync(metricType, fromDate, toDate, groupBy);

        if (format.ToLower() == "csv")
        {
            return GenerateTrendDataCsv(trends, metricType, groupBy);
        }
        else
        {
            return GenerateTrendDataCsv(trends, metricType, groupBy);
        }
    }

    public async Task<byte[]> ExportFullReportAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string format = "csv")
    {
        _logger.LogInformation("Exporting full statistics report, format: {Format}", format);

        var csv = new StringBuilder();

        // 添加时间范围
        csv.AppendLine("统计报告");
        csv.AppendLine($"时间范围: {(fromDate?.ToString("yyyy-MM-dd") ?? "全部")} 至 {(toDate?.ToString("yyyy-MM-dd") ?? "全部")}");
        csv.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine();

        // 1. 统计概览
        csv.AppendLine("=== 统计概览 ===");
        var overview = await _statisticsService.GetOverviewAsync(fromDate, toDate);
        csv.AppendLine($"总工单数,{overview.TotalTickets}");
        csv.AppendLine($"开放工单数,{overview.OpenTickets}");
        csv.AppendLine($"已关闭工单数,{overview.ClosedTickets}");
        csv.AppendLine($"平均闭环时长,{FormatTimeSpan(overview.AverageClosureTime)}");
        csv.AppendLine($"闭环率,{overview.ClosureRate?.ToString("F2") ?? "N/A"}%");
        csv.AppendLine($"重开工单率,{overview.ReopenRate?.ToString("F2") ?? "N/A"}%");
        csv.AppendLine();

        // 2. Top问题域
        csv.AppendLine("=== Top问题域统计 ===");
        csv.AppendLine("问题域,问题域名称,工单数,占比,平均闭环时间");
        var domains = await _statisticsService.GetTopDomainsAsync(10, fromDate, toDate);
        foreach (var domain in domains)
        {
            csv.AppendLine($"{domain.Domain},{domain.DomainName},{domain.TicketCount},{domain.Percentage:F2}%,{FormatTimeSpan(domain.AverageClosureTime)}");
        }
        csv.AppendLine();

        // 3. 闭环时间分布
        csv.AppendLine("=== 闭环时间分布 ===");
        csv.AppendLine("时间范围,工单数,占比");
        var distribution = await _statisticsService.GetClosureTimeDistributionAsync(fromDate, toDate);
        foreach (var item in distribution.Distribution)
        {
            csv.AppendLine($"{item.Range},{item.Count},{item.Percentage:F2}%");
        }
        csv.AppendLine();

        // 4. 状态统计
        csv.AppendLine("=== 工单状态统计 ===");
        csv.AppendLine("状态,工单数,占比");
        var statusStats = await _statisticsService.GetStatusStatisticsAsync(fromDate, toDate);
        foreach (var stat in statusStats)
        {
            csv.AppendLine($"{stat.Status},{stat.Count},{stat.Percentage:F2}%");
        }
        csv.AppendLine();

        // 5. 趋势数据（工单数）
        csv.AppendLine("=== 工单数趋势 ===");
        csv.AppendLine("日期,工单数");
        var ticketTrends = await _statisticsService.GetTrendDataAsync("tickets", fromDate ?? DateTime.Now.AddMonths(-1), toDate ?? DateTime.Now, "day");
        foreach (var trend in ticketTrends)
        {
            csv.AppendLine($"{trend.Date:yyyy-MM-dd},{trend.Value}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateOverviewCsv(StatisticsOverviewDto overview)
    {
        var csv = new StringBuilder();
        csv.AppendLine("指标,数值");
        csv.AppendLine($"总工单数,{overview.TotalTickets}");
        csv.AppendLine($"开放工单数,{overview.OpenTickets}");
        csv.AppendLine($"已关闭工单数,{overview.ClosedTickets}");
        csv.AppendLine($"平均闭环时长,{FormatTimeSpan(overview.AverageClosureTime)}");
        csv.AppendLine($"闭环率,{overview.ClosureRate?.ToString("F2") ?? "N/A"}%");
        csv.AppendLine($"重开工单率,{overview.ReopenRate?.ToString("F2") ?? "N/A"}%");
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateTopDomainsCsv(List<DomainStatisticsDto> domains)
    {
        var csv = new StringBuilder();
        csv.AppendLine("问题域,问题域名称,工单数,占比,平均闭环时间");
        foreach (var domain in domains)
        {
            csv.AppendLine($"{domain.Domain},{domain.DomainName},{domain.TicketCount},{domain.Percentage:F2}%,{FormatTimeSpan(domain.AverageClosureTime)}");
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateClosureTimeDistributionCsv(ClosureTimeDistributionDto distribution)
    {
        var csv = new StringBuilder();
        csv.AppendLine("时间范围,工单数,占比");
        foreach (var item in distribution.Distribution)
        {
            csv.AppendLine($"{item.Range},{item.Count},{item.Percentage:F2}%");
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateStatusStatisticsCsv(List<StatusStatisticsDto> statusStats)
    {
        var csv = new StringBuilder();
        csv.AppendLine("状态,工单数,占比");
        foreach (var stat in statusStats)
        {
            csv.AppendLine($"{stat.Status},{stat.Count},{stat.Percentage:F2}%");
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateTrendDataCsv(List<TrendDataPointDto> trends, string metricType, string groupBy)
    {
        var csv = new StringBuilder();
        csv.AppendLine($"日期,{metricType}");
        foreach (var trend in trends)
        {
            csv.AppendLine($"{trend.Date:yyyy-MM-dd},{trend.Value}");
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private string FormatTimeSpan(TimeSpan? timeSpan)
    {
        if (!timeSpan.HasValue)
        {
            return "N/A";
        }

        var ts = timeSpan.Value;
        if (ts.TotalDays >= 1)
        {
            return $"{ts.Days}天{ts.Hours}小时";
        }
        else if (ts.TotalHours >= 1)
        {
            return $"{ts.Hours}小时{ts.Minutes}分钟";
        }
        else
        {
            return $"{ts.Minutes}分钟";
        }
    }
}

