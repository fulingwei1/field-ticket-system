using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// AI分析结果实体
/// </summary>
public class AiAnalysisResult
{
    public Guid AnalysisId { get; set; }
    
    // 分析范围
    public string AnalysisType { get; set; } = string.Empty; // 'daily_summary', 'weekly_summary', 'team_analysis', 'scheduling_suggestion'
    public DateOnly AnalysisDate { get; set; }
    public Guid? EngineerId { get; set; } // 如果为空，则为团队分析
    public Guid? DepartmentId { get; set; }
    
    // 分析内容
    public string Summary { get; set; } = string.Empty; // AI生成的总结
    public JsonDocument? KeyInsights { get; set; } // 关键洞察（JSONB）
    /*
    {
      "workload_distribution": {
        "field_service": 60,
        "remote_support": 30,
        "other": 10
      },
      "peak_periods": ["09:00-11:00", "14:00-16:00"],
      "bottlenecks": ["设备故障处理", "客户沟通"]
    }
    */
    
    // 建议
    public JsonDocument? Suggestions { get; set; } // AI生成的建议（JSONB）
    /*
    {
      "scheduling_suggestions": [
        {
          "engineer_id": "uuid",
          "suggestion": "建议增加现场服务时间，减少远程支持时间",
          "reason": "现场服务效率更高",
          "priority": "high"
        }
      ],
      "workload_balance": [
        {
          "from_engineer": "uuid",
          "to_engineer": "uuid",
          "suggestion": "建议将部分工单从A工程师转移到B工程师",
          "reason": "B工程师在该领域更专业",
          "expected_improvement": "预计提升20%效率"
        }
      ]
    }
    */
    
    // 绩效分析
    public JsonDocument? PerformanceAnalysis { get; set; } // 绩效分析（JSONB）
    /*
    {
      "top_performers": ["engineer1", "engineer2"],
      "improvement_areas": [
        {
          "engineer_id": "uuid",
          "area": "响应时间",
          "current": "2小时",
          "target": "1小时",
          "improvement_suggestion": "建议优化工作流程"
        }
      ]
    }
    */
    
    // AI模型信息
    public string? AiModel { get; set; } // 使用的AI模型
    public decimal? ConfidenceScore { get; set; } // 置信度（0-1）
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    
    // 导航属性
    public User? Engineer { get; set; }
    public User? Creator { get; set; }
}


