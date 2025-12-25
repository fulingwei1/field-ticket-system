using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 验证记录实体
/// </summary>
public class Verification
{
    public Guid VerificationId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 关联解决方案
    public Guid? SolutionId { get; set; }
    
    // 执行人
    public Guid ExecutedBy { get; set; }
    
    // 验证数据
    public int RunCount { get; set; } // 验证次数
    public int PassCount { get; set; } // 通过次数
    public int FailCount { get; set; } // 失败次数
    
    // 验证结果
    public string Result { get; set; } = string.Empty; // PASS/FAIL/PARTIAL
    
    // 验证清单结果（JSONB）
    public JsonDocument ChecklistResultJson { get; set; } = JsonDocument.Parse("{}");
    
    // 证据附件ID列表
    public List<Guid> EvidenceAttachmentIds { get; set; } = new();
    
    // 验证备注
    public string? Note { get; set; }
    
    // 验证时间
    public DateTime VerifiedAt { get; set; }
    
    // 创建和更新信息
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public Ticket Ticket { get; set; } = null!;
    public Solution? Solution { get; set; }
    public User Executor { get; set; } = null!;
}


