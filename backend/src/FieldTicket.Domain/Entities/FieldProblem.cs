namespace FieldTicket.Domain.Entities;

/// <summary>
/// 现场问题实体
/// </summary>
public class FieldProblem
{
    public Guid ProblemId { get; set; }
    
    // 关联信息
    public Guid ProjectId { get; set; } // 项目ID（外键）
    public virtual Project Project { get; set; } = null!;
    public int ProblemSequence { get; set; } // 问题序号（项目内的第几个问题）
    
    // 问题基本信息
    public string ProblemCategory { get; set; } = string.Empty; // 问题分类
    public string ProblemDescription { get; set; } = string.Empty; // 问题描述
    public string? Priority { get; set; } // 优先级：P1/P2/P3/P4
    
    // 时间信息
    public DateTime FoundDate { get; set; } // 发现日期
    public DateTime? CompletedDate { get; set; } // 处理完成日期
    public int? ProcessingDays { get; set; } // 处理周期天数（自动计算）
    
    // 责任信息
    public string PrimaryDepartment { get; set; } = string.Empty; // 主负责部门
    public string PrimaryResponsible { get; set; } = string.Empty; // 主负责人
    public Guid? PrimaryResponsibleId { get; set; } // 主负责人ID（关联User）
    public string? CollaboratingDepartment { get; set; } // 协作部门
    public string? CollaboratingPerson { get; set; } // 协作人员
    public Guid? CollaboratingPersonId { get; set; } // 协作人员ID
    
    // 处理信息
    public string Status { get; set; } = "待分配"; // 处理状态
    public string? Solution { get; set; } // 处理方案
    public string? SolutionDetails { get; set; } // 处理详情
    
    // 验证信息
    public string? VerificationStatus { get; set; } // 验证状态：未验证/验证通过/验证失败
    public string? CustomerFeedback { get; set; } // 客户反馈
    public int? SatisfactionScore { get; set; } // 满意度评分（1-5）
    public DateTime? VerifiedAt { get; set; } // 验证时间
    public string? VerifiedBy { get; set; } // 验证人
    
    // 关联信息
    public Guid? RelatedTicketId { get; set; } // 关联工单ID
    public string? RelatedTicketNo { get; set; } // 关联工单号
    public string? KnowledgeBaseId { get; set; } // 知识库ID
    public bool IsRepeatProblem { get; set; } = false; // 是否重复问题
    public Guid? RelatedHistoryProblemId { get; set; } // 相关历史问题ID
    
    // 备注
    public string? Notes { get; set; } // 备注
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // 关联关系
    public virtual RootCauseAnalysis? RootCauseAnalysis { get; set; } // 根本原因分析
}






