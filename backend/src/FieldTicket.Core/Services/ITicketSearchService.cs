namespace FieldTicket.Core.Services;

/// <summary>
/// 工单全文搜索服务接口
/// </summary>
public interface ITicketSearchService
{
    /// <summary>
    /// 全文搜索工单
    /// </summary>
    Task<SearchResultDto> SearchTicketsAsync(SearchRequest request);

    /// <summary>
    /// 获取搜索建议（自动完成）
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(string query, int limit = 10);
}

/// <summary>
/// 搜索请求
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 搜索字段：all, title, description, facts, actions
    /// </summary>
    public string? SearchField { get; set; }

    /// <summary>
    /// 状态筛选
    /// </summary>
    public List<string>? Statuses { get; set; }

    /// <summary>
    /// 问题域筛选
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 优先级筛选
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// 客户ID筛选
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 设备SN筛选
    /// </summary>
    public string? DeviceSn { get; set; }

    /// <summary>
    /// 创建时间范围（开始）
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// 创建时间范围（结束）
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 搜索结果
/// </summary>
public class SearchResultDto
{
    /// <summary>
    /// 工单列表
    /// </summary>
    public List<TicketSearchResultItem> Items { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

/// <summary>
/// 工单搜索结果项
/// </summary>
public class TicketSearchResultItem
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? StepCode { get; set; }
    public string? SymptomTitle { get; set; }
    public string? SymptomDetail { get; set; }
    public string? Priority { get; set; }
    public string? CustomerName { get; set; }
    public string? DeviceSn { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 匹配片段（高亮显示用）
    /// </summary>
    public List<string> Matches { get; set; } = new();

    /// <summary>
    /// 相关性得分
    /// </summary>
    public double RelevanceScore { get; set; }
}








