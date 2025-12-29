using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 多模态AI分析服务接口
/// </summary>
public interface IMultimodalAIService
{
    /// <summary>
    /// 分析图片内容（OCR + 视觉理解）
    /// </summary>
    Task<ImageAnalysisResult> AnalyzeImageAsync(
        Stream imageStream,
        string? contextText = null);

    /// <summary>
    /// 分析多张图片
    /// </summary>
    Task<List<ImageAnalysisResult>> AnalyzeImagesAsync(
        List<Stream> imageStreams,
        string? contextText = null);

    /// <summary>
    /// 分析文本描述
    /// </summary>
    Task<TextAnalysisResult> AnalyzeTextAsync(string text);

    /// <summary>
    /// 综合文本和图片分析
    /// </summary>
    Task<ComprehensiveAnalysisResult> AnalyzeComprehensiveAsync(
        string textDescription,
        List<Stream>? images = null);
}

