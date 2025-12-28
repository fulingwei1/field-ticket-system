namespace FieldTicket.Core.Services;

/// <summary>
/// 责任归因服务接口
/// </summary>
public interface IResponsibilityAttributionService
{
    /// <summary>
    /// 归因工单问题
    /// </summary>
    Task AttributeResponsibilityAsync(
        Guid ticketId,
        string rootResponsibility,
        bool isPreventable,
        string? notes,
        Guid userId);

    /// <summary>
    /// 获取工单的责任归因信息
    /// </summary>
    Task<ResponsibilityAttributionDto?> GetAttributionAsync(Guid ticketId);

    /// <summary>
    /// 获取责任归因统计
    /// </summary>
    Task<ResponsibilityStatisticsDto> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);
}

/// <summary>
/// 责任归因DTO
/// </summary>
public class ResponsibilityAttributionDto
{
    public Guid TicketId { get; set; }
    public string? RootResponsibility { get; set; }
    public string? RootResponsibilityName { get; set; }
    public bool? IsPreventable { get; set; }
    public string? ResponsibilityNotes { get; set; }
    public Guid? AttributedBy { get; set; }
    public string? AttributedByName { get; set; }
    public DateTime? AttributedAt { get; set; }
}

/// <summary>
/// 责任归因统计DTO
/// </summary>
public class ResponsibilityStatisticsDto
{
    public List<ResponsibilityDistributionDto> Distribution { get; set; } = new();
    public List<PreventabilityStatisticsDto> PreventabilityStats { get; set; } = new();
    public int TotalAttributed { get; set; }
    public int TotalTickets { get; set; }
    public decimal AttributionRate { get; set; }
}

/// <summary>
/// 责任分布DTO
/// </summary>
public class ResponsibilityDistributionDto
{
    public string RootResponsibility { get; set; } = string.Empty;
    public string RootResponsibilityName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// 可预防性统计DTO
/// </summary>
public class PreventabilityStatisticsDto
{
    public bool IsPreventable { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}














