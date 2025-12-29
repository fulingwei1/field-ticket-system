using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 项目实体
/// </summary>
public class Project
{
    public Guid ProjectId { get; set; }
    
    // 基本信息
    public string ProjectNo { get; set; } = string.Empty; // 项目号（唯一）
    public string ProjectName { get; set; } = string.Empty; // 项目名称
    public Guid CustomerId { get; set; } // 客户ID（关联Customer表）
    public string? CustomerName { get; set; } // 客户名称（冗余字段，便于查询）
    
    // 设备信息
    public string? DeviceType { get; set; } // 设备类型：线体/单机/其他
    public string? IndustryType { get; set; } // 行业类型：汽车/白电/3C等
    
    // 财务信息
    public decimal? SalesAmount { get; set; } // 销售金额
    public int Quantity { get; set; } = 1; // 数量
    
    // 时间信息
    public DateTime? OrderDate { get; set; } // 下单日期
    public DateTime? RequiredDeliveryDate { get; set; } // 要求交货日期
    public DateTime? ActualDeliveryDate { get; set; } // 实际交货日期
    public int? DeliveryDelayDays { get; set; } // 交货延期天数（自动计算）
    
    // 状态信息
    public string ProjectStatus { get; set; } = "进行中"; // 进行中/已交付/已验证/有问题
    
    // 人员信息
    public Guid? ProjectManagerId { get; set; } // 项目经理ID
    public string? ProjectManagerName { get; set; } // 项目经理名称
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // 关联关系
    public virtual ICollection<FieldProblem> Problems { get; set; } = new List<FieldProblem>();
}














