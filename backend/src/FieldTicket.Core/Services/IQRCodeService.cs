namespace FieldTicket.Core.Services;

/// <summary>
/// 二维码服务接口
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// 生成设备二维码（返回Base64图片）
    /// </summary>
    Task<string> GenerateDeviceQRCodeAsync(Guid deviceId);

    /// <summary>
    /// 批量生成设备二维码（返回ZIP文件路径）
    /// </summary>
    Task<string> BatchGenerateDeviceQRCodesAsync(List<Guid> deviceIds);

    /// <summary>
    /// 解析二维码内容，获取设备ID
    /// </summary>
    Guid? ParseDeviceQRCode(string qrCodeContent);
}









