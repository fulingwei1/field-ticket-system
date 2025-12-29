using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 整改任务实体
/// </summary>
public class CorrectiveAction
{
    public Guid ActionId { get; set; }
    
    /// <summary>
    /// 整改任务编号（CAPA-YYYY-NNN）
    /// </summary>
    public string ActionCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 触发类型：threshold, manual
    /// </summary>
    public string TriggerType { get; set; } = "manual";
    
    /// <summary>
    /// 触发规则配置（JSON）
    /// </summary>
    public JsonDocument? TriggerRule { get; set; }
    
    /// <summary>
    /// 关联工单ID列表
    /// </summary>
    public List<Guid> RelatedTicketIds { get; set; } = new();
    
    /// <summary>
    /// 问题描述
    /// </summary>
    public string ProblemDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 根因分类
    /// </summary>
    public string? RootResponsibility { get; set; }
    
    /// <summary>
    /// 整改计划
    /// </summary>
    public string ActionPlan { get; set; } = string.Empty;
    
    /// <summary>
    /// 负责人ID
    /// </summary>
    public Guid? ResponsiblePersonId { get; set; }
    
    /// <summary>
    /// 目标完成日期
    /// </summary>
    public DateTime? TargetCompletionDate { get; set; }
    
    /// <summary>
    /// 执行状态：open, in_progress, completed, closed, cancelled
    /// </summary>
    public string Status { get; set; } = "open";
    
    /// <summary>
    /// 执行记录
    /// </summary>
    public string? ExecutionNotes { get; set; }
    
    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// 完成人ID
    /// </summary>
    public Guid? CompletedBy { get; set; }
    
    /// <summary>
    /// 效果评估（JSON）
    /// </summary>
    public JsonDocument? EffectivenessCheck { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

















