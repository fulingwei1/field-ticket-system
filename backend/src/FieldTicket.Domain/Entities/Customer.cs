namespace FieldTicket.Domain.Entities;

/// <summary>
/// 客户实体
/// </summary>
public class Customer
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? IndustryType { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}






