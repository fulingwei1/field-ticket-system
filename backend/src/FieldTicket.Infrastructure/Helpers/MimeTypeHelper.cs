namespace FieldTicket.Infrastructure.Helpers;

/// <summary>
/// MIME类型辅助类
/// </summary>
public static class MimeTypeHelper
{
    private static readonly Dictionary<string, string> MimeTypeMap = new()
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" },
        { ".mp4", "video/mp4" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".txt", "text/plain" },
        { ".log", "text/plain" },
        { ".pdf", "application/pdf" },
        { ".zip", "application/zip" }
    };

    /// <summary>
    /// 根据文件名获取MIME类型
    /// </summary>
    public static string GetMimeTypeFromFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return "application/octet-stream";
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return MimeTypeMap.TryGetValue(extension, out var mimeType) 
            ? mimeType 
            : "application/octet-stream";
    }

    /// <summary>
    /// 检测是否为图片文件
    /// </summary>
    public static bool IsImageFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp";
    }

    /// <summary>
    /// 检测是否为视频文件
    /// </summary>
    public static bool IsVideoFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".mp4" or ".avi" or ".mov" or ".mkv" or ".wmv";
    }
}

