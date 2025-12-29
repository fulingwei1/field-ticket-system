using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using HypothesisDto = FieldTicket.Core.Services.HypothesisDto;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// AI辅助分诊服务实现（V1版本）
/// </summary>
public class AIAssistedTriageService : IAIAssistedTriageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJudgementCardRecommendationService _recommendationService;
    private readonly IMissingInfoAnalysisService _missingInfoService;
    private readonly ILLMService _llmService;
    private readonly ILogger<AIAssistedTriageService> _logger;

    public AIAssistedTriageService(
        ApplicationDbContext dbContext,
        IJudgementCardRecommendationService recommendationService,
        IMissingInfoAnalysisService missingInfoService,
        ILLMService llmService,
        ILogger<AIAssistedTriageService> logger)
    {
        _dbContext = dbContext;
        _recommendationService = recommendationService;
        _missingInfoService = missingInfoService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<AIAssistedTriageResult> AssistTriageAsync(Guid ticketId)
    {
        _logger.LogInformation("Starting AI-assisted triage for ticket {TicketId}", ticketId);

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        var result = new AIAssistedTriageResult();

        try
        {
            // 1. 推荐判断卡
            var recommendationResponse = await _recommendationService.RecommendJudgementCardsByTicketAsync(
                ticketId, topK: 1);

            if (recommendationResponse.Recommendations.Any())
            {
                var topRecommendation = recommendationResponse.Recommendations.First();
                result.RecommendedJcCode = topRecommendation.JudgementCard.JcCode;
                result.RecommendedJcTitle = topRecommendation.JudgementCard.Title;
                result.Confidence = CalculateConfidence((double)topRecommendation.MatchScore);
            }

            // 2. 生成Top-3假设
            result.Hypotheses = await GenerateHypothesesAsync(ticketId, result.RecommendedJcCode);

            // 3. 生成动作建议
            result.ActionSuggestions = await GenerateActionSuggestionsAsync(
                ticketId,
                result.RecommendedJcCode,
                result.Hypotheses.Select(h => h.Id).ToList());

            // 4. 生成缺失信息问题
            result.MissingInfoQuestions = await GenerateMissingInfoQuestionsAsync(
                ticketId,
                result.RecommendedJcCode);

            // 5. 生成推荐理由
            result.Reasoning = GenerateReasoning(ticket, result);

            // 6. 设置推荐的假设和动作
            if (result.Hypotheses.Any())
            {
                result.RecommendedHypothesis = result.Hypotheses.First().Description;
            }

            if (result.ActionSuggestions.Any())
            {
                result.RecommendedNextAction = result.ActionSuggestions
                    .OrderByDescending(a => a.Priority)
                    .First()
                    .Description;
            }

            _logger.LogInformation("AI-assisted triage completed for ticket {TicketId}, confidence: {Confidence}",
                ticketId, result.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI-assisted triage for ticket {TicketId}", ticketId);
            throw;
        }

        return result;
    }

    public async Task<List<HypothesisDto>> GenerateHypothesesAsync(Guid ticketId, string? jcCode = null)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // V1版本：使用LLM生成假设（简化版RAG）
        var prompt = BuildHypothesisPrompt(ticket, jcCode);

        try
        {
            var response = await _llmService.GenerateStructuredAsync<HypothesisGenerationResponse>(
                prompt,
                options: new LLMRequestOptions
                {
                    Temperature = 0.3, // 较低温度，更确定性
                    MaxTokens = 1500
                });

            return response.Hypotheses.Select((h, index) => new HypothesisDto
            {
                Rank = index + 1,
                Description = h.Description,
                Confidence = h.Confidence,
                Evidence = h.Evidence ?? new List<string>(),
                SupportingKnowledgeIds = h.SupportingKnowledgeIds ?? new List<string>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate hypotheses using LLM, falling back to rule-based");
            
            // 降级到基于规则的假设生成
            return GenerateRuleBasedHypotheses(ticket, jcCode);
        }
    }

    public async Task<List<ActionSuggestionDto>> GenerateActionSuggestionsAsync(
        Guid ticketId,
        string? jcCode = null,
        List<string>? hypothesisIds = null)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 获取判断卡（如果有）
        JudgementCard? jc = null;
        if (!string.IsNullOrEmpty(jcCode))
        {
            jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JcCode == jcCode);
        }

        var suggestions = new List<ActionSuggestionDto>();

        // 如果有关联的判断卡，从判断卡中提取动作建议
        if (jc != null && !string.IsNullOrEmpty(jc.NextActionTemplate))
        {
            suggestions.Add(new ActionSuggestionDto
            {
                Description = jc.NextActionTemplate,
                ActionType = "action",
                IsVerifiable = jc.NextActionTemplate.Contains("检查") || 
                              jc.NextActionTemplate.Contains("验证"),
                Priority = 5,
                RelatedHypothesisIds = hypothesisIds ?? new List<string>()
            });
        }

        // 使用LLM生成额外的动作建议
        try
        {
            var prompt = BuildActionSuggestionPrompt(ticket, jc);
            var response = await _llmService.GenerateStructuredAsync<ActionSuggestionResponse>(
                prompt,
                options: new LLMRequestOptions
                {
                    Temperature = 0.4,
                    MaxTokens = 1000
                });

            suggestions.AddRange(response.Suggestions.Select(s => new ActionSuggestionDto
            {
                Description = s.Description,
                ActionType = s.ActionType ?? "action",
                IsVerifiable = s.IsVerifiable,
                VerificationMethod = s.VerificationMethod,
                Priority = s.Priority,
                RelatedHypothesisIds = hypothesisIds ?? new List<string>()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate action suggestions using LLM");
        }

        return suggestions.OrderByDescending(s => s.Priority).Take(5).ToList();
    }

    public async Task<List<MissingInfoQuestionDto>> GenerateMissingInfoQuestionsAsync(
        Guid ticketId,
        string? jcCode = null)
    {
        // 使用现有的缺失信息分析服务
        var missingInfo = await _missingInfoService.AnalyzeMissingInfoAsync(ticketId, jcCode);
        var questions = await _missingInfoService.GenerateQuestionnaireAsync(missingInfo);

        return questions.Select(q => new MissingInfoQuestionDto
        {
            Question = q.Question,
            QuestionType = q.Type == "explicit" ? "explicit" : "implicit",
            IsRequired = q.Required,
            Explanation = q.Hint,
            SuggestedAnswers = q.Options
        }).ToList();
    }

    #region Private Helper Methods

    private int CalculateConfidence(double matchScore)
    {
        // 将匹配得分（0-100）转换为置信度（1-5）
        return matchScore switch
        {
            >= 80 => 5,
            >= 60 => 4,
            >= 40 => 3,
            >= 20 => 2,
            _ => 1
        };
    }

    private string BuildHypothesisPrompt(Ticket ticket, string? jcCode)
    {
        var factsJson = ticket.FactsJson != null
            ? ticket.FactsJson.RootElement.GetRawText()
            : "{}";

        return $@"基于以下工单信息，生成Top-3最可能的假设：

工单信息：
- 问题域：{ticket.Domain}
- 步骤：{ticket.StepCode}
- 症状：{ticket.SymptomTitle}
- 详细描述：{ticket.SymptomDetail ?? "无"}
- 事实表：{factsJson}

{(string.IsNullOrEmpty(jcCode) ? "" : $"- 推荐判断卡：{jcCode}")}

请生成Top-3假设，每个假设必须：
1. 有明确的描述
2. 有置信度评估（high/medium/low）
3. 有证据支持（至少2条）
4. 引用相关知识（如果有）

返回JSON格式：
{{
  ""hypotheses"": [
    {{
      ""rank"": 1,
      ""description"": ""假设描述"",
      ""confidence"": ""high|medium|low"",
      ""evidence"": [""证据1"", ""证据2""],
      ""supporting_knowledge_ids"": []
    }}
  ]
}}";
    }

    private string BuildActionSuggestionPrompt(Ticket ticket, JudgementCard? jc)
    {
        var jcInfo = jc != null
            ? $"- 判断卡：{jc.JcCode} - {jc.Title}\n- 下一步动作模板：{jc.NextActionTemplate ?? "无"}"
            : "- 判断卡：未选择";

        return $@"基于以下工单信息，生成下一步动作建议：

工单信息：
- 问题域：{ticket.Domain}
- 步骤：{ticket.StepCode}
- 症状：{ticket.SymptomTitle}
{jcInfo}

请生成3-5个动作建议，每个动作必须：
1. 可执行、可验证
2. 有明确的优先级（1-5，5最高）
3. 如果是检查类动作，提供验证方法

返回JSON格式：
{{
  ""suggestions"": [
    {{
      ""description"": ""动作描述"",
      ""action_type"": ""check|action|verify"",
      ""is_verifiable"": true/false,
      ""verification_method"": ""验证方法（可选）"",
      ""priority"": 1-5
    }}
  ]
}}";
    }

    private List<HypothesisDto> GenerateRuleBasedHypotheses(Ticket ticket, string? jcCode)
    {
        // 降级方案：基于规则的假设生成
        var hypotheses = new List<HypothesisDto>();

        // 基于问题域生成通用假设
        var domainHypotheses = ticket.Domain switch
        {
            'A' => new[] { "机械部件故障", "动作执行异常", "机械磨损" },
            'B' => new[] { "电气信号异常", "IO模块故障", "传感器故障" },
            'C' => new[] { "PLC程序逻辑错误", "程序版本不匹配", "程序参数设置错误" },
            'D' => new[] { "测试判定条件错误", "判定阈值设置不当", "测试环境异常" },
            'E' => new[] { "系统环境异常", "网络连接问题", "配置参数错误" },
            _ => new[] { "未知问题", "需要进一步排查", "建议升级处理" }
        };

        for (int i = 0; i < Math.Min(3, domainHypotheses.Length); i++)
        {
            hypotheses.Add(new HypothesisDto
            {
                Rank = i + 1,
                Description = domainHypotheses[i],
                Confidence = i == 0 ? "medium" : "low",
                Evidence = new List<string>
                {
                    $"问题域：{ticket.Domain}",
                    $"步骤：{ticket.StepCode}"
                }
            });
        }

        return hypotheses;
    }

    private string GenerateReasoning(Ticket ticket, AIAssistedTriageResult result)
    {
        var reasons = new List<string>();

        if (!string.IsNullOrEmpty(result.RecommendedJcCode))
        {
            reasons.Add($"推荐判断卡 {result.RecommendedJcCode}，匹配度较高");
        }

        if (result.Hypotheses.Any())
        {
            reasons.Add($"识别出 {result.Hypotheses.Count} 个可能的假设");
        }

        if (result.MissingInfoQuestions.Any())
        {
            reasons.Add($"发现 {result.MissingInfoQuestions.Count} 项缺失信息");
        }

        return string.Join("；", reasons);
    }

    #endregion

    #region Response Models

    private class HypothesisGenerationResponse
    {
        public List<HypothesisItem> Hypotheses { get; set; } = new();
    }

    private class HypothesisItem
    {
        public int Rank { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Confidence { get; set; } = "medium";
        public List<string>? Evidence { get; set; }
        public List<string>? SupportingKnowledgeIds { get; set; }
    }

    private class ActionSuggestionResponse
    {
        public List<ActionSuggestionItem> Suggestions { get; set; } = new();
    }

    private class ActionSuggestionItem
    {
        public string Description { get; set; } = string.Empty;
        public string? ActionType { get; set; }
        public bool IsVerifiable { get; set; }
        public string? VerificationMethod { get; set; }
        public int Priority { get; set; } = 3;
    }

    #endregion
}



















