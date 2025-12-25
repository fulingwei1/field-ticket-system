using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 创建版本请求
/// </summary>
public class CreateVersionRequest
{
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    public string ChangeReason { get; set; } = string.Empty;
    public string? VersionDescription { get; set; }
}

/// <summary>
/// 知识版本DTO
/// </summary>
public class KnowledgeVersionDto
{
    public Guid VersionId { get; set; }
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
    public string? VersionDescription { get; set; }
    public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCurrent { get; set; }
    public string? ChangeType { get; set; }
    public string? ChangeReason { get; set; }
    public string? ChangeSummary { get; set; }
}

/// <summary>
/// 版本对比结果DTO
/// </summary>
public class VersionComparisonDto
{
    public KnowledgeVersionDto Version1 { get; set; } = null!;
    public KnowledgeVersionDto Version2 { get; set; } = null!;
    public List<VersionDifference> Differences { get; set; } = new();
}

/// <summary>
/// 版本差异
/// </summary>
public class VersionDifference
{
    public string Field { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public string ChangeType { get; set; } = string.Empty; // 'added', 'deleted', 'modified'
}

/// <summary>
/// 过期知识DTO
/// </summary>
public class ExpiredKnowledgeDto
{
    public Guid KnowledgeId { get; set; }
    public string KnowledgeType { get; set; } = string.Empty;
    public string KnowledgeName { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int DaysSinceExpiry { get; set; }
}

/// <summary>
/// 版本回滚请求
/// </summary>
public class RollbackVersionRequest
{
    public Guid TargetVersionId { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
}

