using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 客户服务实现
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ApplicationDbContext dbContext,
        ILogger<CustomerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? search = null, int page = 1, int pageSize = 100)
    {
        try
        {
            _logger.LogInformation("GetCustomersAsync called with search={Search}, page={Page}, pageSize={PageSize}", search, page, pageSize);
            
            var query = _dbContext.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => 
                    c.CustomerName.Contains(search) ||
                    (c.CustomerCode != null && c.CustomerCode.Contains(search)) ||
                    (c.IndustryType != null && c.IndustryType.Contains(search)));
            }

            var customers = await query
                .OrderBy(c => c.CustomerName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    CustomerCode = c.CustomerCode,
                    IndustryType = c.IndustryType,
                    ContactPerson = c.ContactPerson,
                    ContactPhone = c.ContactPhone
                })
                .ToListAsync();

            _logger.LogInformation("GetCustomersAsync returned {Count} customers", customers.Count);
            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCustomersAsync");
            throw;
        }
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        var customer = await _dbContext.Customers
            .Where(c => c.CustomerId == customerId)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerName,
                CustomerCode = c.CustomerCode,
                IndustryType = c.IndustryType,
                ContactPerson = c.ContactPerson,
                ContactPhone = c.ContactPhone
            })
            .FirstOrDefaultAsync();

        return customer;
    }
}





