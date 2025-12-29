namespace FieldTicket.Domain.Entities;

/// <summary>
/// 引导式工单创建会话实体
/// </summary>
public class GuidedTicketSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TicketId { get; set; }

    /// <summary>
    /// 会话状态：collecting, analyzing, guiding, completing, completed
    /// </summary>
    public string Status { get; set; } = "collecting";

    public int TurnCount { get; set; }
    public int MaxTurns { get; set; } = 3;

    /// <summary>
    /// 初始文字描述
    /// </summary>
    public string? InitialText { get; set; }

    /// <summary>
    /// 图片附件ID列表（PostgreSQL数组）
    /// </summary>
    public Guid[]? ImageAttachmentIds { get; set; }

    /// <summary>
    /// 文本分析结果（JSONB）
    /// </summary>
    public string? TextAnalysis { get; set; }

    /// <summary>
    /// 图片分析结果列表（PostgreSQL数组，每个元素是JSONB）
    /// </summary>
    public string[]? ImageAnalyses { get; set; }

    /// <summary>
    /// 综合分析结果（JSONB）
    /// </summary>
    public string? ComprehensiveAnalysis { get; set; }

    /// <summary>
    /// 对话历史（JSONB）
    /// </summary>
    public string ConversationHistory { get; set; } = "[]";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

