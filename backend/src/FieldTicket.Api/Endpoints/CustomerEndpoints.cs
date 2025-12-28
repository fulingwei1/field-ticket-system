using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 客户相关 API 端点
/// </summary>
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        // 获取客户列表
        group.MapGet("", GetCustomers)
            .WithName("GetCustomers")
            .WithSummary("获取客户列表");

        // 获取客户详情
        group.MapGet("{customerId:guid}", GetCustomer)
            .WithName("GetCustomer")
            .WithSummary("获取客户详情");
    }

    /// <summary>
    /// 获取客户列表
    /// </summary>
    private static async Task<IResult> GetCustomers(
        ICustomerService service = null!,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var customers = await service.GetCustomersAsync(search, page, pageSize);
            return Results.Ok(customers);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取客户详情
    /// </summary>
    private static async Task<IResult> GetCustomer(
        Guid customerId,
        ICustomerService service = null!)
    {
        try
        {
            var customer = await service.GetCustomerAsync(customerId);
            if (customer == null)
            {
                return Results.NotFound(new { message = $"客户 {customerId} 不存在" });
            }
            return Results.Ok(customer);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}



