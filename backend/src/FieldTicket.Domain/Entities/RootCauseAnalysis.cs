namespace FieldTicket.Domain.Entities;

/// <summary>
/// 根本原因分析实体
/// </summary>
public class RootCauseAnalysis
{
    public Guid AnalysisId { get; set; }
    
    // 关联信息
    public Guid ProblemId { get; set; } // 问题ID（外键）
    public virtual FieldProblem Problem { get; set; } = null!;
    
    // 5Why分析
    public string? Why1 { get; set; } // 为什么会出现这个问题？
    public string? Why2 { get; set; } // 为什么会有这个原因？
    public string? Why3 { get; set; } // 为什么？
    public string? Why4 { get; set; } // 为什么？
    public string? Why5 { get; set; } // 为什么？
    
    // 根本原因
    public string RootCause { get; set; } = string.Empty; // 根本原因
    public string? RootCauseCategory { get; set; } // 根本原因分类：设计/工艺/管理/其他
    
    // 预防措施
    public string? PreventiveMeasures { get; set; } // 预防措施
    public string? VerificationMethod { get; set; } // 验证方法
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? AnalyzedBy { get; set; } // 分析人
    public DateTime? AnalyzedAt { get; set; } // 分析时间
}

















