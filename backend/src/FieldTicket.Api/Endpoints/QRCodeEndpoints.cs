using FieldTicket.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace FieldTicket.Api.Endpoints;

/// <summary>
/// 二维码相关端点
/// </summary>
public static class QRCodeEndpoints
{
    public static void MapQRCodeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/qrcodes")
            .WithTags("QRCodes")
            .RequireAuthorization();

        // 生成设备二维码
        group.MapGet("/devices/{deviceId:guid}", GenerateDeviceQRCode)
            .WithName("GenerateDeviceQRCode")
            .WithSummary("生成设备二维码");

        // 批量生成设备二维码
        group.MapPost("/devices/batch", BatchGenerateDeviceQRCodes)
            .WithName("BatchGenerateDeviceQRCodes")
            .WithSummary("批量生成设备二维码（ZIP下载）");

        // 解析二维码内容
        group.MapPost("/parse", ParseQRCode)
            .WithName("ParseQRCode")
            .WithSummary("解析二维码内容，获取设备ID");
    }

    /// <summary>
    /// 生成设备二维码
    /// </summary>
    private static async Task<IResult> GenerateDeviceQRCode(
        Guid deviceId,
        IQRCodeService service)
    {
        try
        {
            var qrCodeBase64 = await service.GenerateDeviceQRCodeAsync(deviceId);
            return Results.Ok(new { qrCode = qrCodeBase64, deviceId });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 批量生成设备二维码
    /// </summary>
    private static async Task<IResult> BatchGenerateDeviceQRCodes(
        [FromBody] BatchGenerateQRCodeRequest request,
        IQRCodeService service)
    {
        try
        {
            var zipPath = await service.BatchGenerateDeviceQRCodesAsync(request.DeviceIds);
            
            // 读取 ZIP 文件并返回
            var zipBytes = await File.ReadAllBytesAsync(zipPath);
            var fileName = $"device_qrcodes_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            
            // 清理临时文件
            try
            {
                File.Delete(zipPath);
            }
            catch
            {
                // 忽略删除错误
            }

            return Results.File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// 解析二维码内容
    /// </summary>
    private static async Task<IResult> ParseQRCode(
        [FromBody] ParseQRCodeRequest request,
        IQRCodeService service,
        IDeviceService deviceService)
    {
        try
        {
            var deviceId = service.ParseDeviceQRCode(request.QrCodeContent);
            
            // 如果是设备SN格式，需要通过SN查找设备ID
            if (!deviceId.HasValue && request.QrCodeContent.StartsWith("device_sn:"))
            {
                var deviceSn = request.QrCodeContent.Substring("device_sn:".Length);
                var devices = await deviceService.SearchDevicesAsync(deviceSn, 1, 1);
                if (devices.Any())
                {
                    deviceId = devices.First().DeviceId;
                }
            }

            if (!deviceId.HasValue)
            {
                return Results.BadRequest(new { message = "无法解析二维码内容" });
            }

            var device = await deviceService.GetDeviceAsync(deviceId.Value);
            return Results.Ok(new { deviceId = deviceId.Value, device });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

/// <summary>
/// 批量生成二维码请求
/// </summary>
public class BatchGenerateQRCodeRequest
{
    public List<Guid> DeviceIds { get; set; } = new();
}

/// <summary>
/// 解析二维码请求
/// </summary>
public class ParseQRCodeRequest
{
    public string QrCodeContent { get; set; } = string.Empty;
}














