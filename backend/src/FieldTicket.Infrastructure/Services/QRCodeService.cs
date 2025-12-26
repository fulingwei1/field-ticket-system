using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using QRCoder;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 二维码服务实现
/// </summary>
public class QRCodeService : IQRCodeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<QRCodeService> _logger;
    private readonly string _tempDirectory;

    public QRCodeService(
        ApplicationDbContext dbContext,
        ILogger<QRCodeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "qrcodes");
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<string> GenerateDeviceQRCodeAsync(Guid deviceId)
    {
        // 验证设备是否存在（从工单表中检查）
        var deviceExists = await _dbContext.Tickets
            .AnyAsync(t => t.DeviceId == deviceId);

        if (!deviceExists)
        {
            throw new KeyNotFoundException($"设备 {deviceId} 不存在");
        }

        // 生成二维码内容（格式：device:{deviceId}）
        var qrContent = $"device:{deviceId}";

        // 使用 QRCoder 库生成二维码
        var qrCodeBase64 = GenerateQRCodeBase64(qrContent);

        return qrCodeBase64;
    }

    public async Task<string> BatchGenerateDeviceQRCodesAsync(List<Guid> deviceIds)
    {
        var zipPath = Path.Combine(_tempDirectory, $"qrcodes_{DateTime.UtcNow:yyyyMMddHHmmss}.zip");

        using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var deviceId in deviceIds)
            {
                try
                {
                    var qrCodeBase64 = await GenerateDeviceQRCodeAsync(deviceId);
                    
                    // 生成文件名
                    var fileName = $"device_{deviceId}.png";

                    // 将 Base64 转换为字节数组并添加到 ZIP
                    var imageBytes = Convert.FromBase64String(qrCodeBase64);
                    var entry = zipArchive.CreateEntry(fileName);
                    using (var entryStream = entry.Open())
                    {
                        await entryStream.WriteAsync(imageBytes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "生成设备 {DeviceId} 的二维码失败", deviceId);
                }
            }
        }

        return zipPath;
    }

    public Guid? ParseDeviceQRCode(string qrCodeContent)
    {
        if (string.IsNullOrEmpty(qrCodeContent))
        {
            return null;
        }

        // 解析格式：device:{deviceId} 或 device_sn:{deviceSn}
        if (qrCodeContent.StartsWith("device:"))
        {
            var deviceIdStr = qrCodeContent.Substring("device:".Length);
            if (Guid.TryParse(deviceIdStr, out var deviceId))
            {
                return deviceId;
            }
        }
        else if (qrCodeContent.StartsWith("device_sn:"))
        {
            var deviceSn = qrCodeContent.Substring("device_sn:".Length);
            // 需要通过设备SN查找设备ID（异步操作，这里返回null，由调用方处理）
            return null;
        }

        return null;
    }

    /// <summary>
    /// 生成二维码Base64
    /// </summary>
    private string GenerateQRCodeBase64(string content)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成二维码失败: {Content}", content);
            throw new InvalidOperationException($"生成二维码失败: {ex.Message}", ex);
        }
    }
}

