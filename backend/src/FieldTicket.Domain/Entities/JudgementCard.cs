using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 判断卡实体
/// </summary>
public class JudgementCard
{
    public Guid JudgementCardId { get; set; }
    
    // 判断卡编号（唯一标识）
    public string JcCode { get; set; } = string.Empty; // JC-YYYY-NNN
    
    // 基本信息
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // 问题域
    public char Domain { get; set; } // A/B/C/D/E
    
    // 症状结构化（JSONB）
    public JsonDocument SymptomStructure { get; set; } = JsonDocument.Parse("{}");
    
    // 排查路径（JSONB）
    public JsonDocument TroubleshootingPath { get; set; } = JsonDocument.Parse("{}");
    
    // 当前假设模板
    public string? HypothesisTemplate { get; set; }
    
    // 下一步动作模板
    public string? NextActionTemplate { get; set; }
    
    // 升级条件
    public JsonDocument? EscalationConditions { get; set; }
    
    // 状态
    public string Status { get; set; } = "Active"; // Active/Deprecated/Archived
    
    // 版本信息
    public int Version { get; set; } = 1;
    public Guid? ParentJcId { get; set; } // 父版本ID
    public bool IsCurrent { get; set; } = true; // 是否为当前版本
    
    // 创建和更新信息
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // 使用统计
    public int UsageCount { get; set; } = 0;
    public DateTime? LastUsedAt { get; set; }
    
    // 版本绑定和有效期
    public List<string> ApplicableSwVersions { get; set; } = new(); // 适用的软件版本列表
    public List<string> ApplicableHwVersions { get; set; } = new(); // 适用的硬件版本列表
    public DateTime? ExpiryDate { get; set; } // 过期日期
    public bool IsExpired { get; set; } = false; // 是否过期
    
    // 来源追溯
    public Guid? SourceTicketId { get; set; } // 来源工单ID
    public Guid? VerifiedBy { get; set; } // 验证人ID
    public DateTime? LastVerifiedAt { get; set; } // 最后验证时间
    public int VerificationCount { get; set; } = 0; // 验证次数
}


