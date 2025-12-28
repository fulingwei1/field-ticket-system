namespace FieldTicket.Core.Services;

/// <summary>
/// 客户服务接口
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// 获取客户列表
    /// </summary>
    Task<List<CustomerDto>> GetCustomersAsync(string? search = null, int page = 1, int pageSize = 100);

    /// <summary>
    /// 根据客户ID获取客户信息
    /// </summary>
    Task<CustomerDto?> GetCustomerAsync(Guid customerId);
}

/// <summary>
/// 客户DTO
/// </summary>
public class CustomerDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? IndustryType { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
}



