namespace FieldTicket.Core.Services;

/// <summary>
/// 统计服务接口
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// 获取统计概览
    /// </summary>
    Task<StatisticsOverviewDto> GetOverviewAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// 获取Top问题域统计
    /// </summary>
    Task<List<DomainStatisticsDto>> GetTopDomainsAsync(
        int topN = 5,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// 获取闭环时间分布
    /// </summary>
    Task<ClosureTimeDistributionDto> GetClosureTimeDistributionAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// 获取工单状态统计
    /// </summary>
    Task<List<StatusStatisticsDto>> GetStatusStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// 获取趋势数据
    /// </summary>
    Task<List<TrendDataPointDto>> GetTrendDataAsync(
        string metricType, // "tickets", "closure_time", "resolution_rate"
        DateTime fromDate,
        DateTime toDate,
        string groupBy = "day"); // "day", "week", "month"
}

/// <summary>
/// 统计概览DTO
/// </summary>
public class StatisticsOverviewDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public TimeSpan? AverageClosureTime { get; set; }
    public decimal? ClosureRate { get; set; }
    public decimal? ReopenRate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// 问题域统计DTO
/// </summary>
public class DomainStatisticsDto
{
    public char Domain { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public decimal Percentage { get; set; }
    public TimeSpan? AverageClosureTime { get; set; }
}

/// <summary>
/// 闭环时间分布DTO
/// </summary>
public class ClosureTimeDistributionDto
{
    public List<TimeRangeCountDto> Distribution { get; set; } = new();
    public TimeSpan? AverageTime { get; set; }
    public TimeSpan? MedianTime { get; set; }
    public TimeSpan? P95Time { get; set; }
}

/// <summary>
/// 时间范围计数DTO
/// </summary>
public class TimeRangeCountDto
{
    public string Range { get; set; } = string.Empty; // "0-1天", "1-3天", "3-7天", "7-15天", "15-30天", "30+天"
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// 状态统计DTO
/// </summary>
public class StatusStatisticsDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// 趋势数据点DTO
/// </summary>
public class TrendDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string? Label { get; set; }
}

