namespace FieldTicket.Shared.Models;

/// <summary>
/// 判断卡推荐请求
/// </summary>
public class RecommendJudgementCardsRequest
{
    public char? Domain { get; set; }
    public string? StepCode { get; set; }
    public string? SymptomTitle { get; set; }
    public Dictionary<string, object>? FactsJson { get; set; }
    public int TopK { get; set; } = 5;
}

/// <summary>
/// 判断卡推荐结果DTO
/// </summary>
public class JudgementCardRecommendationDto
{
    public JudgementCardDto JudgementCard { get; set; } = null!;
    public decimal MatchScore { get; set; }
    public List<string> MatchReasons { get; set; } = new();
    public Dictionary<string, decimal> ScoreBreakdown { get; set; } = new();
}

/// <summary>
/// 判断卡推荐响应
/// </summary>
public class RecommendJudgementCardsResponse
{
    public List<JudgementCardRecommendationDto> Recommendations { get; set; } = new();
    public int TotalCandidates { get; set; }
}

