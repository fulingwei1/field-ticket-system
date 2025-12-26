namespace FieldTicket.Domain.Entities;

/// <summary>
/// 缺失信息补全对话历史实体
/// </summary>
public class MissingInfoConversationHistory
{
    public Guid ConversationId { get; set; }
    
    // 关联工单
    public Guid TicketId { get; set; }
    
    // 对话轮次
    public int RoundNumber { get; set; }
    
    // 问题ID
    public string QuestionId { get; set; } = string.Empty;
    
    // 问题内容
    public string Question { get; set; } = string.Empty;
    
    // 问题类型
    public string QuestionType { get; set; } = string.Empty; // yes_no, text, choice
    
    // 用户回答
    public string? UserAnswer { get; set; }
    
    // 回答时间
    public DateTime? AnsweredAt { get; set; }
    
    // 是否已回答
    public bool IsAnswered { get; set; } = false;
    
    // 是否跳过
    public bool IsSkipped { get; set; } = false;
    
    // 创建时间
    public DateTime CreatedAt { get; set; }
    
    // 导航属性
    public Ticket? Ticket { get; set; }
}










