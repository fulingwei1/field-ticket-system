using FieldTicket.Domain.Entities;
using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 判断卡版本管理服务接口
/// </summary>
public interface IJudgementCardVersionService
{
    /// <summary>
    /// 创建新版本（必须填写变更原因）
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <param name="request">更新请求</param>
    /// <param name="changeReason">变更原因（必须填写）</param>
    /// <param name="userId">用户ID</param>
    /// <returns>新版本的判断卡</returns>
    Task<JudgementCardDto> CreateNewVersionAsync(
        string jcCode,
        UpdateJudgementCardRequest request,
        string changeReason,
        Guid userId);

    /// <summary>
    /// 获取判断卡的所有版本
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <returns>版本列表</returns>
    Task<List<JudgementCardVersionDto>> GetVersionsAsync(string jcCode);

    /// <summary>
    /// 获取指定版本的判断卡
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <param name="version">版本号</param>
    /// <returns>判断卡</returns>
    Task<JudgementCardDto?> GetVersionAsync(string jcCode, int version);

    /// <summary>
    /// 版本对比
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <param name="version1">版本1</param>
    /// <param name="version2">版本2</param>
    /// <returns>版本对比结果</returns>
    Task<VersionComparisonDto> CompareVersionsAsync(string jcCode, int version1, int version2);

    /// <summary>
    /// 获取判断卡的使用历史
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <param name="version">版本号（可选）</param>
    /// <returns>使用历史列表</returns>
    Task<List<JudgementCardUsageHistoryDto>> GetUsageHistoryAsync(string jcCode, int? version = null);

    /// <summary>
    /// 记录判断卡使用历史
    /// </summary>
    /// <param name="jcCode">判断卡编号</param>
    /// <param name="version">版本号</param>
    /// <param name="ticketId">工单ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="result">使用结果（可选）</param>
    /// <param name="feedback">反馈（可选）</param>
    Task RecordUsageAsync(
        string jcCode,
        int version,
        Guid ticketId,
        Guid userId,
        string? result = null,
        string? feedback = null);
}

/// <summary>
/// 更新判断卡请求
/// </summary>
public class UpdateJudgementCardRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public char? Domain { get; set; }
    public System.Text.Json.JsonDocument? SymptomStructure { get; set; }
    public System.Text.Json.JsonDocument? TroubleshootingPath { get; set; }
    public string? HypothesisTemplate { get; set; }
    public string? NextActionTemplate { get; set; }
    public System.Text.Json.JsonDocument? EscalationConditions { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// 判断卡版本DTO
/// </summary>
public class JudgementCardVersionDto
{
    public Guid JudgementCardId { get; set; }
    public string JcCode { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public Guid? ParentJcId { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// 版本对比结果DTO
/// </summary>
public class VersionComparisonDto
{
    public string JcCode { get; set; } = string.Empty;
    public JudgementCardVersionDto Version1 { get; set; } = null!;
    public JudgementCardVersionDto Version2 { get; set; } = null!;
    public List<FieldChange> Changes { get; set; } = new();
}

/// <summary>
/// 字段变更
/// </summary>
public class FieldChange
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = string.Empty; // Added, Modified, Removed
}

/// <summary>
/// 判断卡使用历史DTO
/// </summary>
public class JudgementCardUsageHistoryDto
{
    public Guid UsageHistoryId { get; set; }
    public string JcCode { get; set; } = string.Empty;
    public int JcVersion { get; set; }
    public Guid TicketId { get; set; }
    public string? TicketNo { get; set; }
    public Guid UsedBy { get; set; }
    public string? UsedByName { get; set; }
    public DateTime UsedAt { get; set; }
    public string? Result { get; set; }
    public string? Feedback { get; set; }
}

