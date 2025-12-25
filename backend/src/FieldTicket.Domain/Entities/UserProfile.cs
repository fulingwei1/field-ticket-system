using System.Text.Json;

namespace FieldTicket.Domain.Entities;

/// <summary>
/// 用户画像实体
/// </summary>
public class UserProfile
{
    public Guid ProfileId { get; set; }
    public Guid UserId { get; set; }
    
    // 填写习惯（JSONB）
    public JsonDocument CommonFields { get; set; } = JsonDocument.Parse("{}");
    /*
    {
      "frequently_used": {
        "device_model": "Model-A",
        "problem_domain": "C",
        "common_symptoms": ["超时", "到位检测"]
      },
      "preferences": {
        "question_style": "detailed",
        "detail_level": "high"
      }
    }
    */
    
    // 专业度评估
    public string? ExpertiseLevel { get; set; }  // 'beginner', 'intermediate', 'expert'
    public decimal? ExpertiseScore { get; set; }
    
    // 学习数据
    public int TotalTickets { get; set; } = 0;
    public int? AverageCompletionTime { get; set; }  // 秒
    public JsonDocument? CommonMistakes { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // 导航属性
    public User? User { get; set; }
}

