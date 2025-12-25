namespace FieldTicket.Core.Services;

/// <summary>
/// 工单去重检测服务接口
/// </summary>
public interface IDuplicateDetectionService
{
    /// <summary>
    /// 检测重复工单
    /// </summary>
    Task<List<DuplicateCandidate>> DetectDuplicatesAsync(
        Guid ticketId,
        int maxResults = 10);

    /// <summary>
    /// 合并工单
    /// </summary>
    Task<MergeResult> MergeTicketsAsync(
        Guid sourceTicketId,
        Guid targetTicketId,
        string reason,
        Guid userId);

    /// <summary>
    /// 计算两个工单的相似度
    /// </summary>
    Task<SimilarityScore> CalculateSimilarityAsync(
        Guid ticketId1,
        Guid ticketId2);

    /// <summary>
    /// 获取工单的合并历史
    /// </summary>
    Task<List<MergeHistoryDto>> GetMergeHistoryAsync(Guid ticketId);
}

/// <summary>
/// 重复工单候选
/// </summary>
public class DuplicateCandidate
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string SymptomTitle { get; set; } = string.Empty;
    public char Domain { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public SimilarityScore Similarity { get; set; } = null!;
}

/// <summary>
/// 相似度评分
/// </summary>
public class SimilarityScore
{
    public decimal Overall { get; set; } // 综合相似度（0-1）
    public decimal Device { get; set; } // 设备相似度（0-1）
    public decimal Symptom { get; set; } // 症状相似度（0-1）
    public decimal Domain { get; set; } // 问题域相似度（0-1）
    public decimal Time { get; set; } // 时间相似度（0-1）
    public Dictionary<string, decimal> Breakdown { get; set; } = new(); // 详细评分
}

/// <summary>
/// 合并结果
/// </summary>
public class MergeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid TargetTicketId { get; set; }
    public Guid SourceTicketId { get; set; }
}

/// <summary>
/// 合并历史DTO
/// </summary>
public class MergeHistoryDto
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string MergeReason { get; set; } = string.Empty;
    public Guid? MergedBy { get; set; }
    public string? MergedByName { get; set; }
    public DateTime MergedAt { get; set; }
    public bool IsSource { get; set; } // true: 被合并的工单, false: 合并到的工单
}

