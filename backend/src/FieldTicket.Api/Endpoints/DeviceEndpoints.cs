using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 设备相关端点
/// </summary>
public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        // 获取设备列表
        group.MapGet("", GetDevices)
            .WithName("GetDevices")
            .WithSummary("获取设备列表");

        // 获取设备详情
        group.MapGet("{deviceId:guid}", GetDevice)
            .WithName("GetDevice")
            .WithSummary("获取设备详情");

        // 搜索设备
        group.MapGet("search", SearchDevices)
            .WithName("SearchDevices")
            .WithSummary("搜索设备");
    }

    /// <summary>
    /// 获取设备列表
    /// </summary>
    private static async Task<IResult> GetDevices(
        [FromQuery] Guid? projectId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IDeviceService service)
    {
        try
        {
            var devices = await service.GetDevicesAsync(page > 0 ? page : 1, pageSize > 0 ? pageSize : 50, projectId);
            return Results.Ok(devices);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取设备详情
    /// </summary>
    private static async Task<IResult> GetDevice(
        Guid deviceId,
        IDeviceService service)
    {
        try
        {
            var device = await service.GetDeviceAsync(deviceId);
            if (device == null)
            {
                return Results.NotFound(new { message = $"设备 {deviceId} 不存在" });
            }
            return Results.Ok(device);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 搜索设备
    /// </summary>
    private static async Task<IResult> SearchDevices(
        [FromQuery] string? keyword,
        [FromQuery] Guid? projectId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IDeviceService service)
    {
        try
        {
            var devices = await service.SearchDevicesAsync(keyword ?? string.Empty, page > 0 ? page : 1, pageSize > 0 ? pageSize : 50, projectId);
            return Results.Ok(devices);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}








