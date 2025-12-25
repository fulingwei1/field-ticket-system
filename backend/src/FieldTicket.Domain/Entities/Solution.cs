using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 解决方案实体
/// </summary>
public class Solution
{
    public Guid SolutionId { get; set; }
    
    // 解决方案编号（唯一标识）
    public string SolutionCode { get; set; } = string.Empty; // SOL-YYYY-NNN
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 基本信息
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // 方案类型
    public string SolutionType { get; set; } = string.Empty; // software_update/parameter_change/hardware_replacement/procedure
    
    // 发布类型
    public string ReleaseType { get; set; } = string.Empty; // PLC/SOFTWARE/PARAM/MIXED
    
    // 版本信息
    public string? RequiredSwVersion { get; set; }
    public string? RequiredPlcVersion { get; set; }
    public string? RequiredParamVersion { get; set; }
    
    public string? NewSwVersion { get; set; }
    public string? NewPlcVersion { get; set; }
    public string? NewParamVersion { get; set; }
    
    // 结构化修改内容（JSONB）
    public JsonDocument ChangeDetailJson { get; set; } = JsonDocument.Parse("{}");
    
    // 验证清单（JSONB）
    public JsonDocument VerificationChecklistJson { get; set; } = JsonDocument.Parse("{}");
    
    // 实施步骤
    public string? ImplementationSteps { get; set; }
    public int? EstimatedImplementationTime { get; set; } // 分钟
    
    // 风险评估
    public string RiskLevel { get; set; } = "medium"; // low/medium/high
    public string? RiskDescription { get; set; }
    
    // 回滚方案
    public bool RollbackPossible { get; set; } = true;
    public string? RollbackProcedure { get; set; }
    
    // 状态
    public string Status { get; set; } = "Draft"; // Draft/Published/Archived
    
    // 创建和更新信息
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // 发布信息
    public Guid? PublishedBy { get; set; }
    
    // 版本绑定和有效期
    public List<string> ApplicableSwVersions { get; set; } = new(); // 适用的软件版本列表
    public List<string> ApplicableHwVersions { get; set; } = new(); // 适用的硬件版本列表
    public DateTime? ExpiryDate { get; set; } // 过期日期
    public bool IsExpired { get; set; } = false; // 是否过期
    
    // 来源追溯（Solution 已经有 TicketId 作为来源工单）
    public Guid? VerifiedBy { get; set; } // 验证人ID
    public DateTime? LastVerifiedAt { get; set; } // 最后验证时间
    public int VerificationCount { get; set; } = 0; // 验证次数
}


