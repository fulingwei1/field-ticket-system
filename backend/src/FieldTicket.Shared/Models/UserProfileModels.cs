using System.Text.Json;

namespace FieldTicket.Shared.Models;

/// <summary>
/// 用户画像DTO
/// </summary>
public class UserProfileDto
{
    public Guid ProfileId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public JsonDocument CommonFields { get; set; } = JsonDocument.Parse("{}");
    public string? ExpertiseLevel { get; set; }
    public decimal? ExpertiseScore { get; set; }
    public int TotalTickets { get; set; }
    public int? AverageCompletionTime { get; set; }
    public JsonDocument? CommonMistakes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 智能预填充数据
/// </summary>
public class PreFillData
{
    public Dictionary<string, object> Fields { get; set; } = new();
    public List<string> SuggestedQuestions { get; set; } = new();
    public decimal Confidence { get; set; }
}

/// <summary>
/// 更新用户画像请求
/// </summary>
public class UpdateUserProfileRequest
{
    public Guid TicketId { get; set; }
    public JsonDocument? FilledFields { get; set; }
    public int? FillingTime { get; set; }
    public JsonDocument? SkippedFields { get; set; }
}

