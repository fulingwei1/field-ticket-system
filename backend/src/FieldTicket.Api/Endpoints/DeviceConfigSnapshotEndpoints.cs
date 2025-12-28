using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 设备配置快照相关端点
/// </summary>
public static class DeviceConfigSnapshotEndpoints
{
    public static void MapDeviceConfigSnapshotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/device-config-snapshots")
            .WithTags("DeviceConfigSnapshots")
            .RequireAuthorization();

        // 创建配置快照
        group.MapPost("", CreateSnapshot)
            .WithName("CreateConfigSnapshot")
            .WithSummary("创建配置快照");

        // 检查配置差异
        group.MapGet("/devices/{deviceId}/differences", CheckDifferences)
            .WithName("CheckConfigDifferences")
            .WithSummary("检查配置差异");

        // 获取快照历史
        group.MapGet("/devices/{deviceId}/history", GetSnapshotHistory)
            .WithName("GetSnapshotHistory")
            .WithSummary("获取快照历史");

        // 获取标准配置
        group.MapGet("/devices/{deviceId}/standard", GetStandardConfig)
            .WithName("GetStandardConfig")
            .WithSummary("获取标准配置");

        // 获取当前配置
        group.MapGet("/devices/{deviceId}/current", GetCurrentConfig)
            .WithName("GetCurrentConfig")
            .WithSummary("获取当前配置");
    }

    /// <summary>
    /// 创建配置快照
    /// </summary>
    private static async Task<IResult> CreateSnapshot(
        [FromBody] CreateConfigSnapshotRequest request,
        IDeviceConfigSnapshotService service,
        HttpContext httpContext)
    {
        try
        {
            var userId = GetUserId(httpContext);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var snapshotId = await service.CreateSnapshotAsync(request, userId.Value);
            return Results.Created($"/api/device-config-snapshots/{snapshotId}", new { snapshotId });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 检查配置差异
    /// </summary>
    private static async Task<IResult> CheckDifferences(
        Guid deviceId,
        IDeviceConfigSnapshotService service,
        [FromBody] Dictionary<string, object>? currentConfig)
    {
        try
        {
            var differences = await service.CheckConfigDifferencesAsync(deviceId, currentConfig);
            return Results.Ok(differences);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取快照历史
    /// </summary>
    private static async Task<IResult> GetSnapshotHistory(
        Guid deviceId,
        [FromQuery] string? snapshotType,
        [FromQuery] int? limit,
        IDeviceConfigSnapshotService service)
    {
        try
        {
            var snapshots = await service.GetSnapshotHistoryAsync(deviceId, snapshotType, limit ?? 50);
            return Results.Ok(snapshots);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取标准配置
    /// </summary>
    private static async Task<IResult> GetStandardConfig(
        Guid deviceId,
        IDeviceConfigSnapshotService service)
    {
        try
        {
            var config = await service.GetStandardConfigAsync(deviceId);
            if (config == null)
            {
                return Results.NotFound(new { message = "未找到标准配置" });
            }
            return Results.Ok(config);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    private static async Task<IResult> GetCurrentConfig(
        Guid deviceId,
        IDeviceConfigSnapshotService service)
    {
        try
        {
            var config = await service.GetCurrentConfigAsync(deviceId);
            return Results.Ok(config);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
