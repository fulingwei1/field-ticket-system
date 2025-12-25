using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 提交验证结果请求
/// </summary>
public class SubmitVerificationRequest
{
    /// <summary>
    /// 关联的解决方案ID（可选）
    /// </summary>
    public Guid? SolutionId { get; set; }
    
    /// <summary>
    /// 验证次数
    /// </summary>
    public int RunCount { get; set; }
    
    /// <summary>
    /// 通过次数
    /// </summary>
    public int PassCount { get; set; }
    
    /// <summary>
    /// 失败次数
    /// </summary>
    public int FailCount { get; set; }
    
    /// <summary>
    /// 验证清单结果（JSON格式）
    /// </summary>
    public JsonDocument ChecklistResultJson { get; set; } = JsonDocument.Parse("{}");
    
    /// <summary>
    /// 证据附件ID列表
    /// </summary>
    public List<Guid> EvidenceAttachmentIds { get; set; } = new();
    
    /// <summary>
    /// 验证备注
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// 验证结果DTO
/// </summary>
public class VerificationDto
{
    public Guid VerificationId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? SolutionId { get; set; }
    public Guid ExecutedBy { get; set; }
    public string? ExecutedByName { get; set; }
    
    public int RunCount { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    
    public string Result { get; set; } = string.Empty;
    
    public JsonDocument ChecklistResultJson { get; set; } = JsonDocument.Parse("{}");
    public List<Guid> EvidenceAttachmentIds { get; set; } = new();
    
    public string? Note { get; set; }
    
    public DateTime VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


