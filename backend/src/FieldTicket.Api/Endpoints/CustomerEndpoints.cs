using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        ICustomerService service,
        HttpContext context,
        ILoggerFactory loggerFactory,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var logger = loggerFactory.CreateLogger("CustomerEndpoints");
        try
        {
            // 记录认证信息用于调试
            var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            logger?.LogInformation(
                "GetCustomers - IsAuthenticated: {IsAuthenticated}, UserId: {UserId}, AuthHeaderPresent: {AuthHeaderPresent}",
                isAuthenticated, userId, !string.IsNullOrEmpty(authHeader));

            if (!isAuthenticated)
            {
                var authPreview = string.IsNullOrEmpty(authHeader)
                    ? "N/A"
                    : authHeader.Substring(0, Math.Min(20, authHeader.Length));
                logger?.LogWarning("GetCustomers - Unauthenticated request. AuthHeader: {AuthHeader}", 
                    authPreview);
                return Results.Unauthorized();
            }

            var customers = await service.GetCustomersAsync(search, page, pageSize);
            logger?.LogInformation("GetCustomers - Returning {Count} customers", customers.Count);
            return Results.Ok(customers);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "GetCustomers - Error occurred");
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



