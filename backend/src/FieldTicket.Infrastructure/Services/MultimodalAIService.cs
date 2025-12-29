using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.LLM;
using FieldTicket.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 多模态AI分析服务实现
/// </summary>
public class MultimodalAIService : IMultimodalAIService
{
    private readonly ILLMService _llmService;
    private readonly GoogleGeminiService? _geminiService;
    private readonly ILogger<MultimodalAIService> _logger;

    public MultimodalAIService(
        ILLMService llmService,
        ILogger<MultimodalAIService> logger)
    {
        _llmService = llmService;
        _logger = logger;
        
        // 如果使用的是Gemini服务，保存引用以使用Vision API
        if (llmService is GoogleGeminiService geminiService)
        {
            _geminiService = geminiService;
        }
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(
        Stream imageStream,
        string? contextText = null)
    {
        try
        {
            var prompt = $@"
请分析这张现场设备问题的图片：

{contextText ?? ""}

请识别：
1. 设备类型和型号（如可识别）
2. 问题现象（如：报警灯、错误信息、异常状态）
3. 关键信息（如：屏幕显示、指示灯状态、机械位置）
4. 专业术语建议（用专业术语描述看到的现象）

返回JSON格式：
{{
  ""device_info"": {{
    ""type"": ""设备类型"",
    ""model"": ""型号（如可识别）""
  }},
  ""problem_phenomena"": [
    {{
      ""description"": ""现象描述"",
      ""location"": ""位置"",
      ""professional_term"": ""专业术语""
    }}
  ],
  ""key_information"": [
    ""关键信息1"",
    ""关键信息2""
  ],
  ""ocr_text"": ""从图片中识别的文字"",
  ""suggestions"": [
    ""建议补充的信息""
  ]
}}";

            string resultJson;
            
            // 如果使用Gemini服务，使用Vision API
            if (_geminiService != null)
            {
                // 检测图片MIME类型（简化版，实际应该从stream检测）
                var mimeType = "image/jpeg"; // 默认，实际应该检测
                resultJson = await _geminiService.AnalyzeImageWithTextAsync(
                    prompt,
                    imageStream,
                    mimeType,
                    new LLMRequestOptions { MaxTokens = 2000, Temperature = 0.3 });
            }
            else
            {
                // 降级方案：使用文本模型（需要先OCR）
                _logger.LogWarning("Using text-only model for image analysis. Consider using Gemini Vision API.");
                resultJson = await _llmService.GenerateTextAsync(prompt);
            }

            var result = JsonSerializer.Deserialize<ImageAnalysisResult>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ImageAnalysisResult();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze image");
            throw;
        }
    }

    public async Task<List<ImageAnalysisResult>> AnalyzeImagesAsync(
        List<Stream> imageStreams,
        string? contextText = null)
    {
        var results = new List<ImageAnalysisResult>();
        foreach (var imageStream in imageStreams)
        {
            var result = await AnalyzeImageAsync(imageStream, contextText);
            results.Add(result);
        }
        return results;
    }

    public async Task<TextAnalysisResult> AnalyzeTextAsync(string text)
    {
        try
        {
            var prompt = $@"
你是一位经验丰富的设备故障诊断专家。请分析以下现场工程师的描述，并给出专业建议。

## 原始描述
{text}

## 请完成以下任务：

1. **问题域识别**：判断问题属于哪个域（A-机械/动作、B-电气/IO、C-PLC/程序流程、D-通信、E-系统/偶发/环境）

2. **专业术语转换**：将口语化描述转换为专业术语
   - 例如：""机器不动了"" → ""设备停止运行，无动作输出""
   - 例如：""报警了"" → ""系统报警，报警代码：XXX""

3. **关键信息提取**：
   - 设备信息（型号、版本）
   - 问题现象（症状、频率）
   - 环境信息（温度、湿度等）

4. **缺失信息识别**：识别需要补充的关键信息

返回JSON格式：
{{
  ""domain"": ""A|B|C|D|E"",
  ""professional_description"": {{
    ""title"": ""专业的问题标题（一句话）"",
    ""detail"": ""专业的问题详细描述""
  }},
  ""key_information"": {{
    ""device_model"": ""设备型号"",
    ""symptom"": ""症状描述"",
    ""frequency"": ""发生频率"",
    ""environment"": ""环境信息""
  }},
  ""missing_info"": [
    {{
      ""field"": ""字段名"",
      ""question"": ""需要补充的问题"",
      ""reason"": ""为什么需要这个信息""
    }}
  ],
  ""terminology_suggestions"": [
    {{
      ""original"": ""原始描述"",
      ""professional"": ""专业术语"",
      ""explanation"": ""解释""
    }}
  ]
}}";

            var resultJson = await _llmService.GenerateTextAsync(prompt);
            var result = JsonSerializer.Deserialize<TextAnalysisResult>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new TextAnalysisResult();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze text");
            throw;
        }
    }

    public async Task<ComprehensiveAnalysisResult> AnalyzeComprehensiveAsync(
        string textDescription,
        List<Stream>? images = null)
    {
        try
        {
            // 1. 分析文本
            var textAnalysis = await AnalyzeTextAsync(textDescription);

            // 2. 分析图片（如果有）
            var imageAnalyses = new List<ImageAnalysisResult>();
            if (images != null && images.Any())
            {
                imageAnalyses = await AnalyzeImagesAsync(images, textDescription);
            }

            // 3. 综合分析
            var comprehensivePrompt = $@"
综合以下信息，生成完整的分析结果：

## 文本分析结果
{JsonSerializer.Serialize(textAnalysis)}

## 图片分析结果
{JsonSerializer.Serialize(imageAnalyses)}

## 请综合以上信息，生成：
1. 最终的问题域判断
2. 专业的问题描述
3. 完整的关键信息
4. 缺失信息清单
5. 专业术语建议

返回JSON格式（与TextAnalysisResult相同结构）：
{{
  ""domain"": ""A|B|C|D|E"",
  ""professional_description"": {{
    ""title"": ""专业的问题标题"",
    ""detail"": ""专业的问题详细描述""
  }},
  ""key_information"": {{
    ""device_model"": ""设备型号"",
    ""symptom"": ""症状描述"",
    ""frequency"": ""发生频率"",
    ""environment"": ""环境信息""
  }},
  ""missing_info"": [
    {{
      ""field"": ""字段名"",
      ""question"": ""需要补充的问题"",
      ""reason"": ""为什么需要这个信息""
    }}
  ],
  ""terminology_suggestions"": [
    {{
      ""original"": ""原始描述"",
      ""professional"": ""专业术语"",
      ""explanation"": ""解释""
    }}
  ],
  ""suggestions"": [
    ""综合建议1"",
    ""综合建议2""
  ]
}}";

            var resultJson = await _llmService.GenerateTextAsync(comprehensivePrompt);
            var result = JsonSerializer.Deserialize<ComprehensiveAnalysisResult>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ComprehensiveAnalysisResult();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform comprehensive analysis");
            throw;
        }
    }

    private async Task<string> ConvertToBase64Async(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();
        return Convert.ToBase64String(bytes);
    }
}

