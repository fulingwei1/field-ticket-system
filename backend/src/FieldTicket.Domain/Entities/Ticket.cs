using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 工单实体
/// </summary>
public class Ticket
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;

    // 关联信息
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DeviceId { get; set; }
    public string? DeviceSn { get; set; }
    public string? DeviceName { get; set; }
    public Guid? StationId { get; set; }

    // 创建者
    public Guid CreatedByUserId { get; set; }

    // 问题描述
    public char Domain { get; set; } // A/B/C/D/E
    public string StepCode { get; set; } = string.Empty;
    public string? StepName { get; set; }
    public string SymptomTitle { get; set; } = string.Empty;
    public string? SymptomDetail { get; set; }

    // 复现性
    public int? ReproRate { get; set; }
    public bool? RebootRecovers { get; set; }
    public bool? EnvRelated { get; set; }

    // 版本信息
    public string SwVersion { get; set; } = string.Empty;
    public string PlcVersion { get; set; } = string.Empty;
    public string ParamVersion { get; set; } = string.Empty;
    public string? HwVersion { get; set; }

    // 结构化事实（JSONB）
    public JsonDocument FactsJson { get; set; } = JsonDocument.Parse("{}");

    // 现场已执行动作
    public List<string> ActionsTaken { get; set; } = new();
    public string? ActionsTakenNote { get; set; }

    // 上报确认
    public bool ConfirmedAsFact { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // 报警信息
    public string? AlarmCode { get; set; }

    // 状态与优先级
    public string Status { get; set; } = "Draft"; // Draft/Submitted/Triage/SolutionIssued/Verifying/Closed
    public string Priority { get; set; } = "P3"; // P1/P2/P3/P4

    // 分诊关联
    public string? CurrentJcCode { get; set; }
    public Guid? AssignedTo { get; set; }

    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // 本地草稿同步
    public string? LocalDraftId { get; set; }
    public string? IdempotencyKey { get; set; }

    // 责任归因（HR-004: 结案必须归因）
    public string? RootCause { get; set; } // 根因描述
    public string? RootResponsibility { get; set; } // 根因分类：'design', 'software', 'parameter', 'assembly', 'documentation', 'other', 'unknown'
    public string? ResponsibilityTeam { get; set; } // 责任团队
    public bool? IsPreventable { get; set; } // 是否可预防
    public string? ResponsibilityNotes { get; set; } // 归因备注
    public Guid? AttributedBy { get; set; } // 归因操作人
    public DateTime? AttributedAt { get; set; } // 归因操作时间

    // 去重和合并
    public Guid? DuplicateOf { get; set; } // 标记为重复工单，指向主工单
    public Guid? MergedInto { get; set; } // 已合并到目标工单
    public string? MergeReason { get; set; } // 合并原因
    public Guid? MergedBy { get; set; } // 合并操作人
    public DateTime? MergedAt { get; set; } // 合并时间

    // 标签（用于批量标记和分类）
    public List<string> Tags { get; set; } = new(); // 标签列表，存储为JSONB
}


