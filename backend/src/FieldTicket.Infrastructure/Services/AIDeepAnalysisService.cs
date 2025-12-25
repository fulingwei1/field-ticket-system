using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.LLM;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// AI深度分析缺失信息服务实现
/// </summary>
public class AIDeepAnalysisService : IAIDeepAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMissingInfoAnalysisService _missingInfoService;
    private readonly ILLMService _llmService;
    private readonly ILogger<AIDeepAnalysisService> _logger;

    public AIDeepAnalysisService(
        ApplicationDbContext dbContext,
        IMissingInfoAnalysisService missingInfoService,
        ILLMService llmService,
        ILogger<AIDeepAnalysisService> logger)
    {
        _dbContext = dbContext;
        _missingInfoService = missingInfoService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<DeepAnalysisResult> AnalyzeTicketAsync(Guid ticketId, string? jcCode = null)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 获取工单DTO
        var ticketDto = await MapToTicketDtoAsync(ticket);

        // 获取判断卡（如果有）
        JudgementCardDto? jcDto = null;
        if (!string.IsNullOrEmpty(jcCode))
        {
            var jc = await _dbContext.JudgementCards
                .FirstOrDefaultAsync(j => j.JcCode == jcCode);
            if (jc != null)
            {
                jcDto = new JudgementCardDto
                {
                    JudgementCardId = jc.JudgementCardId,
                    JcCode = jc.JcCode,
                    Title = jc.Title,
                    Description = jc.Description,
                    Domain = jc.Domain,
                    SymptomStructure = jc.SymptomStructure,
                    TroubleshootingPath = jc.TroubleshootingPath,
                    HypothesisTemplate = jc.HypothesisTemplate,
                    NextActionTemplate = jc.NextActionTemplate,
                    Status = jc.Status,
                    Version = jc.Version,
                    UsageCount = jc.UsageCount,
                    LastUsedAt = jc.LastUsedAt,
                    CreatedAt = jc.CreatedAt,
                    UpdatedAt = jc.UpdatedAt
                };
            }
        }

        // 1. 明确缺失的信息（基于规则）
        var explicitMissing = await _missingInfoService.AnalyzeMissingInfoAsync(ticketId, jcCode);

        // 2. 隐含缺失的信息（基于AI分析）
        var implicitMissing = await IdentifyImplicitRequirementsAsync(ticketDto, jcDto);

        // 3. 上下文理解
        var contextInfo = await UnderstandContextAsync(ticketDto);

        // 4. 额外信息建议
        var additionalInfo = await SuggestAdditionalInfoAsync(ticketDto, jcDto, contextInfo);

        // 5. 生成个性化问题
        var personalizedQuestions = await GeneratePersonalizedQuestionsAsync(ticketId, implicitMissing);

        return new DeepAnalysisResult
        {
            ExplicitMissing = explicitMissing,
            ImplicitMissing = implicitMissing,
            AdditionalInfo = additionalInfo,
            ContextInfo = contextInfo,
            PersonalizedQuestions = personalizedQuestions
        };
    }

    public async Task<List<ImplicitInfoRequirement>> IdentifyImplicitRequirementsAsync(
        TicketDto ticket,
        JudgementCardDto? jc)
    {
        var requirements = new List<ImplicitInfoRequirement>();

        try
        {
            // 检查LLM服务是否可用
            var llmAvailable = await _llmService.IsAvailableAsync();
            
            if (llmAvailable)
            {
                // 使用LLM进行深度分析
                requirements = await IdentifyImplicitRequirementsWithLLMAsync(ticket, jc);
            }
            else
            {
                // 降级到规则引擎
                _logger.LogWarning("LLM service not available, falling back to rule-based analysis");
                requirements = await IdentifyImplicitRequirementsWithRulesAsync(ticket, jc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify implicit requirements with LLM, falling back to rules");
            // 降级到规则引擎
            requirements = await IdentifyImplicitRequirementsWithRulesAsync(ticket, jc);
        }

        return requirements;
    }

    /// <summary>
    /// 使用LLM识别隐含信息需求
    /// </summary>
    private async Task<List<ImplicitInfoRequirement>> IdentifyImplicitRequirementsWithLLMAsync(
        TicketDto ticket,
        JudgementCardDto? jc)
    {
        // 准备上下文信息
        var jcRequirements = jc != null
            ? JsonSerializer.Serialize(jc.SymptomStructure)
            : null;

        var relatedTickets = await FindRelatedTicketsAsync(ticket);
        var relatedTicketsInfo = relatedTickets.Any()
            ? string.Join("\n", relatedTickets.Select(t => $"- {t.TicketNo}: {t.Reason}"))
            : null;

        // 构建Prompt
        var prompt = PromptTemplates.GetMissingInfoAnalysisPrompt(
            ticket.Domain.ToString(),
            ticket.StepCode,
            ticket.SymptomTitle,
            ticket.SymptomDetail,
            ticket.FactsJson.RootElement.GetRawText(),
            jcRequirements,
            relatedTicketsInfo
        );

        // 定义JSON Schema
        var schema = new JsonSchema
        {
            Definition = @"{
  ""type"": ""object"",
  ""properties"": {
    ""explicit_missing"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""field"": { ""type"": ""string"" },
          ""question"": { ""type"": ""string"" },
          ""type"": { ""type"": ""string"", ""enum"": [""yes_no"", ""number"", ""text"", ""file"", ""select""] },
          ""required"": { ""type"": ""boolean"" },
          ""reason"": { ""type"": ""string"" }
        },
        ""required"": [""field"", ""question"", ""type"", ""required""]
      }
    },
    ""implicit_missing"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""field"": { ""type"": ""string"" },
          ""question"": { ""type"": ""string"" },
          ""type"": { ""type"": ""string"", ""enum"": [""yes_no"", ""number"", ""text"", ""file"", ""select""] },
          ""required"": { ""type"": ""boolean"" },
          ""confidence"": { ""type"": ""integer"", ""minimum"": 1, ""maximum"": 5 },
          ""reason"": { ""type"": ""string"" }
        },
        ""required"": [""field"", ""question"", ""type"", ""required"", ""confidence""]
      }
    },
    ""additional_info"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""field"": { ""type"": ""string"" },
          ""question"": { ""type"": ""string"" },
          ""type"": { ""type"": ""string"", ""enum"": [""yes_no"", ""number"", ""text"", ""file"", ""select""] },
          ""required"": { ""type"": ""boolean"" },
          ""reason"": { ""type"": ""string"" }
        },
        ""required"": [""field"", ""question"", ""type"", ""required""]
      }
    }
  },
  ""required"": [""explicit_missing"", ""implicit_missing"", ""additional_info""]
}"
        };

        // 调用LLM
        var llmResponse = await _llmService.GenerateStructuredAsync<LLMMissingInfoAnalysisResponse>(
            prompt,
            schema,
            new LLMRequestOptions
            {
                Model = "gpt-4o",
                Temperature = 0.3,
                MaxTokens = 2000
            }
        );

        // 转换为ImplicitInfoRequirement
        var requirements = new List<ImplicitInfoRequirement>();

        // 处理隐含缺失信息
        foreach (var item in llmResponse.ImplicitMissing)
        {
            requirements.Add(new ImplicitInfoRequirement
            {
                Field = item.Field,
                Question = item.Question,
                Type = item.Type,
                Required = item.Required,
                Confidence = item.Confidence ?? 3,
                Reason = item.Reason
            });
        }

        return requirements;
    }

    /// <summary>
    /// 使用规则引擎识别隐含信息需求（降级方案）
    /// </summary>
    private async Task<List<ImplicitInfoRequirement>> IdentifyImplicitRequirementsWithRulesAsync(
        TicketDto ticket,
        JudgementCardDto? jc)
    {
        var requirements = new List<ImplicitInfoRequirement>();

        // 基于工单描述的语义分析
        if (!string.IsNullOrWhiteSpace(ticket.SymptomDetail))
        {
            // 分析描述中的关键词
            var keywords = ExtractKeywords(ticket.SymptomDetail);
            
            // 检查是否提到"动作"但未明确是否完成
            if (keywords.Contains("动作") || keywords.Contains("操作") || keywords.Contains("执行"))
            {
                if (!HasFieldValue(ticket.FactsJson, "action_completed"))
                {
                    requirements.Add(new ImplicitInfoRequirement
                    {
                        Field = "action_completed",
                        Question = "根据您的描述，动作是否已完成？",
                        Type = "yes_no",
                        Required = true,
                        Confidence = 4,
                        Reason = "描述中提到了动作相关操作，但未明确是否完成"
                    });
                }
            }

            // 检查是否提到"IO"但未明确是否有变化
            if (keywords.Contains("IO") || keywords.Contains("输入输出") || keywords.Contains("信号"))
            {
                if (!HasFieldValue(ticket.FactsJson, "plc_io_changes"))
                {
                    requirements.Add(new ImplicitInfoRequirement
                    {
                        Field = "plc_io_changes",
                        Question = "PLC中的IO信号是否有变化？",
                        Type = "yes_no",
                        Required = true,
                        Confidence = 4,
                        Reason = "描述中提到了IO相关操作，但未明确是否有变化"
                    });
                }
            }

            // 检查是否提到"报警"但未提供报警代码
            if (keywords.Contains("报警") || keywords.Contains("告警") || keywords.Contains("错误"))
            {
                if (string.IsNullOrWhiteSpace(ticket.AlarmCode))
                {
                    requirements.Add(new ImplicitInfoRequirement
                    {
                        Field = "alarm_code",
                        Question = "请提供具体的报警代码或错误信息",
                        Type = "text",
                        Required = false,
                        Confidence = 3,
                        Reason = "描述中提到了报警，但未提供具体代码"
                    });
                }
            }
        }

        // 基于问题域的特殊检查
        switch (ticket.Domain)
        {
            case 'A': // 机械问题
                if (!HasFieldValue(ticket.FactsJson, "mechanical_loose") &&
                    !HasFieldValue(ticket.FactsJson, "mechanical_wear") &&
                    !HasFieldValue(ticket.FactsJson, "mechanical_damage"))
                {
                    requirements.Add(new ImplicitInfoRequirement
                    {
                        Field = "mechanical_condition",
                        Question = "请描述机械部件的具体状态（松动、磨损、损坏等）",
                        Type = "text",
                        Required = false,
                        Confidence = 3,
                        Reason = "机械问题通常需要了解机械部件的具体状态"
                    });
                }
                break;

            case 'B': // 电气问题
                if (!HasFieldValue(ticket.FactsJson, "electrical_short") &&
                    !HasFieldValue(ticket.FactsJson, "electrical_open") &&
                    !HasFieldValue(ticket.FactsJson, "electrical_noise"))
                {
                    requirements.Add(new ImplicitInfoRequirement
                    {
                        Field = "electrical_condition",
                        Question = "请描述电气系统的具体状态（短路、断路、干扰等）",
                        Type = "text",
                        Required = false,
                        Confidence = 3,
                        Reason = "电气问题通常需要了解电气系统的具体状态"
                    });
                }
                break;
        }

        // 基于历史工单的常见遗漏
        var relatedTickets = await FindRelatedTicketsAsync(ticket);
        if (relatedTickets.Any())
        {
            var commonMissingFields = AnalyzeCommonMissingFields(relatedTickets);
            foreach (var field in commonMissingFields)
            {
                if (!HasFieldValue(ticket.FactsJson, field))
                {
                    requirements.Add(new ImplicitInfoRequirement
                    {
                        Field = field,
                        Question = $"根据相似工单，通常需要了解：{GetFieldQuestion(field)}",
                        Type = "yes_no",
                        Required = false,
                        Confidence = 2,
                        Reason = "相似工单中经常需要此信息",
                        RelatedTicketIds = relatedTickets.Select(t => t.TicketId).ToList()
                    });
                }
            }
        }

        return requirements;
    }

    public async Task<List<PersonalizedQuestion>> GeneratePersonalizedQuestionsAsync(
        Guid ticketId,
        List<ImplicitInfoRequirement> requirements)
    {
        var questions = new List<PersonalizedQuestion>();

        // 按置信度和优先级排序
        var sortedRequirements = requirements
            .OrderByDescending(r => r.Confidence)
            .ThenByDescending(r => r.Required)
            .ToList();

        foreach (var (req, index) in sortedRequirements.Select((r, i) => (r, i)))
        {
            questions.Add(new PersonalizedQuestion
            {
                QuestionId = $"Q{index + 1}",
                Question = req.Question,
                Type = req.Type,
                Required = req.Required,
                Field = req.Field,
                Priority = req.Confidence,
                PersonalizationReason = req.Reason
            });
        }

        return questions;
    }

    public async Task<ConversationResult> ContinueConversationAsync(
        Guid ticketId,
        string userAnswer,
        string questionId)
    {
        try
        {
            // 获取工单和对话历史
            var ticket = await _dbContext.Tickets
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"工单 {ticketId} 不存在");
            }

            // 从数据库获取对话历史
            var conversationHistory = await GetConversationHistoryAsync(ticketId);

            // 获取待收集的信息
            var missingInfo = await _missingInfoService.AnalyzeMissingInfoAsync(ticketId, ticket.CurrentJcCode);
            var remainingRequirements = missingInfo
                .Where(m => !string.IsNullOrEmpty(m.Field))
                .Select(m => m.Field)
                .ToList();

            // 检查LLM服务是否可用
            var llmAvailable = await _llmService.IsAvailableAsync();
            
            if (llmAvailable)
            {
                // 使用LLM进行多轮对话
                var prompt = PromptTemplates.GetConversationPrompt(
                    $"问题ID: {questionId}",
                    userAnswer,
                    conversationHistory,
                    remainingRequirements
                );

                var schema = new JsonSchema
                {
                    Definition = @"{
  ""type"": ""object"",
  ""properties"": {
    ""next_question"": {
      ""type"": ""object"",
      ""properties"": {
        ""field"": { ""type"": ""string"" },
        ""question"": { ""type"": ""string"" },
        ""type"": { ""type"": ""string"", ""enum"": [""yes_no"", ""number"", ""text"", ""file"", ""select""] },
        ""required"": { ""type"": ""boolean"" }
      }
    },
    ""is_complete"": { ""type"": ""boolean"" },
    ""collected_info"": { ""type"": ""object"" }
  },
  ""required"": [""is_complete"", ""collected_info""]
}"
                };

                var llmResponse = await _llmService.GenerateStructuredAsync<LLMConversationResponse>(
                    prompt,
                    schema,
                    new LLMRequestOptions { Model = "gpt-4o", Temperature = 0.5 }
                );

                var result = new ConversationResult
                {
                    IsComplete = llmResponse.IsComplete,
                    CollectedInfo = llmResponse.CollectedInfo
                };

                if (llmResponse.NextQuestion != null)
                {
                    result.NextQuestion = new PersonalizedQuestion
                    {
                        QuestionId = $"Q{Guid.NewGuid().ToString().Substring(0, 8)}",
                        Question = llmResponse.NextQuestion.Question,
                        Type = llmResponse.NextQuestion.Type,
                        Required = llmResponse.NextQuestion.Required,
                        Field = llmResponse.NextQuestion.Field
                    };
                }

                // 保存用户回答到对话历史
                var currentRound = await _dbContext.MissingInfoConversationHistories
                    .Where(h => h.TicketId == ticketId)
                    .Select(h => h.RoundNumber)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                
                await SaveConversationHistoryAsync(
                    ticketId,
                    currentRound + 1,
                    questionId,
                    result.NextQuestion?.Question ?? "",
                    result.NextQuestion?.Type ?? "text",
                    userAnswer,
                    false);

                return result;
            }
            else
            {
                // 降级到简单逻辑
                _logger.LogWarning("LLM service not available, using simple conversation logic");
                return new ConversationResult
                {
                    IsComplete = remainingRequirements.Count == 0,
                    CollectedInfo = new Dictionary<string, object> { { questionId, userAnswer } }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue conversation with LLM");
            return new ConversationResult
            {
                IsComplete = true,
                CollectedInfo = new Dictionary<string, object> { { questionId, userAnswer } }
            };
        }
    }

    // 辅助方法

    private async Task<ContextInfo> UnderstandContextAsync(TicketDto ticket)
    {
        SemanticInfo? semanticInfo = null;

        try
        {
            // 检查LLM服务是否可用
            var llmAvailable = await _llmService.IsAvailableAsync();
            
            if (llmAvailable)
            {
                // 使用LLM进行语义分析
                var prompt = PromptTemplates.GetContextUnderstandingPrompt(
                    ticket.SymptomTitle,
                    ticket.SymptomDetail,
                    ticket.Domain.ToString(),
                    ticket.StepCode
                );

                var schema = new JsonSchema
                {
                    Definition = @"{
  ""type"": ""object"",
  ""properties"": {
    ""severity"": { ""type"": ""integer"", ""minimum"": 1, ""maximum"": 5 },
    ""categories"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""entities"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""sentiment"": { ""type"": ""string"", ""enum"": [""positive"", ""neutral"", ""negative""] }
  },
  ""required"": [""severity"", ""categories"", ""entities"", ""sentiment""]
}"
                };

                var llmResponse = await _llmService.GenerateStructuredAsync<LLMContextUnderstandingResponse>(
                    prompt,
                    schema,
                    new LLMRequestOptions { Model = "gpt-4o", Temperature = 0.3 }
                );

                semanticInfo = new SemanticInfo
                {
                    Severity = llmResponse.Severity,
                    Categories = llmResponse.Categories,
                    Entities = llmResponse.Entities,
                    Sentiment = llmResponse.Sentiment
                };
            }
            else
            {
                // 降级到规则引擎
                semanticInfo = AnalyzeSemantics(ticket.SymptomDetail ?? ticket.SymptomTitle);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to understand context with LLM, falling back to rules");
            semanticInfo = AnalyzeSemantics(ticket.SymptomDetail ?? ticket.SymptomTitle);
        }

        // 关键信息提取
        var keyInfo = ExtractKeyInfo(ticket);

        // 关联历史工单
        var relatedTickets = await FindRelatedTicketsAsync(ticket);

        // 设备上下文
        var deviceContext = await GetDeviceContextAsync(ticket.DeviceId);

        return new ContextInfo
        {
            SemanticInfo = semanticInfo,
            KeyInfo = keyInfo,
            RelatedTickets = relatedTickets,
            DeviceContext = deviceContext
        };
    }

    private async Task<List<AdditionalInfoSuggestion>> SuggestAdditionalInfoAsync(
        TicketDto ticket,
        JudgementCardDto? jc,
        ContextInfo? contextInfo)
    {
        var suggestions = new List<AdditionalInfoSuggestion>();

        // 基于上下文建议额外信息
        if (contextInfo?.SemanticInfo?.Severity >= 4)
        {
            suggestions.Add(new AdditionalInfoSuggestion
            {
                Field = "emergency_contact",
                Question = "是否需要紧急联系客户？",
                Type = "yes_no",
                Required = false,
                Reason = "问题严重程度较高，建议确认是否需要紧急处理"
            });
        }

        return suggestions;
    }

    private List<string> ExtractKeywords(string text)
    {
        // 简单的关键词提取（实际应该使用NLP库）
        var keywords = new List<string>();
        var commonKeywords = new[] { "动作", "操作", "执行", "IO", "输入输出", "信号", "报警", "告警", "错误" };
        
        foreach (var keyword in commonKeywords)
        {
            if (text.Contains(keyword))
            {
                keywords.Add(keyword);
            }
        }

        return keywords;
    }

    private bool HasFieldValue(JsonDocument factsJson, string field)
    {
        if (factsJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return factsJson.RootElement.TryGetProperty(field, out var prop) &&
               prop.ValueKind != JsonValueKind.Null &&
               !string.IsNullOrWhiteSpace(prop.GetString());
    }

    private SemanticInfo AnalyzeSemantics(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SemanticInfo { Severity = 3, Sentiment = "neutral" };
        }

        // 简单的语义分析（实际应该使用AI）
        var severity = 3;
        if (text.Contains("严重") || text.Contains("紧急") || text.Contains("故障"))
        {
            severity = 5;
        }
        else if (text.Contains("问题") || text.Contains("异常"))
        {
            severity = 4;
        }

        return new SemanticInfo
        {
            Severity = severity,
            Categories = new List<string> { "technical" },
            Entities = ExtractKeywords(text),
            Sentiment = "neutral"
        };
    }

    private List<KeyInfo> ExtractKeyInfo(TicketDto ticket)
    {
        var keyInfo = new List<KeyInfo>();

        if (!string.IsNullOrWhiteSpace(ticket.SymptomTitle))
        {
            keyInfo.Add(new KeyInfo
            {
                Type = "symptom_title",
                Content = ticket.SymptomTitle,
                Importance = 5
            });
        }

        if (ticket.ReproRate.HasValue)
        {
            keyInfo.Add(new KeyInfo
            {
                Type = "repro_rate",
                Content = $"{ticket.ReproRate}%",
                Importance = 4
            });
        }

        return keyInfo;
    }

    private async Task<List<RelatedTicket>> FindRelatedTicketsAsync(TicketDto ticket)
    {
        // 查找相似工单（基于问题域和步骤代码）
        var related = await _dbContext.Tickets
            .Where(t =>
                t.Domain == ticket.Domain &&
                t.StepCode == ticket.StepCode &&
                t.TicketId != ticket.TicketId &&
                t.Status == "Closed")
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RelatedTicket
            {
                TicketId = t.TicketId,
                TicketNo = t.TicketNo,
                Similarity = 0.7, // 简化的相似度计算
                Reason = "相同问题域和步骤代码"
            })
            .ToListAsync();

        return related;
    }

    private async Task<DeviceContext?> GetDeviceContextAsync(Guid deviceId)
    {
        // 获取设备的历史问题统计
        var historicalIssues = await _dbContext.Tickets
            .Where(t => t.DeviceId == deviceId && t.Status == "Closed")
            .GroupBy(t => t.Domain)
            .Select(g => new { Domain = g.Key, Count = g.Count() })
            .ToListAsync();

        var issuesDict = historicalIssues.ToDictionary(x => x.Domain.ToString(), x => x.Count);

        return new DeviceContext
        {
            HistoricalIssues = issuesDict,
            CommonPatterns = new List<string>()
        };
    }

    private List<string> AnalyzeCommonMissingFields(List<RelatedTicket> relatedTickets)
    {
        // 简化的分析：返回常见字段
        return new List<string> { "action_completed", "plc_io_changes" };
    }

    private string GetFieldQuestion(string field)
    {
        return field switch
        {
            "action_completed" => "动作是否完成？",
            "plc_io_changes" => "PLC中IO是否有变化？",
            _ => $"请提供 {field} 的信息"
        };
    }

    private async Task<TicketDto> MapToTicketDtoAsync(Ticket ticket)
    {
        // 简化的映射（实际应该使用AutoMapper或完整映射）
        return new TicketDto
        {
            TicketId = ticket.TicketId,
            TicketNo = ticket.TicketNo,
            DeviceId = ticket.DeviceId,
            Domain = ticket.Domain,
            StepCode = ticket.StepCode,
            StepName = ticket.StepName,
            SymptomTitle = ticket.SymptomTitle,
            SymptomDetail = ticket.SymptomDetail,
            ReproRate = ticket.ReproRate,
            RebootRecovers = ticket.RebootRecovers,
            EnvRelated = ticket.EnvRelated,
            SwVersion = ticket.SwVersion,
            PlcVersion = ticket.PlcVersion,
            ParamVersion = ticket.ParamVersion,
            FactsJson = ticket.FactsJson,
            ActionsTaken = ticket.ActionsTaken ?? new List<string>(),
            ActionsTakenNote = ticket.ActionsTakenNote,
            AlarmCode = ticket.AlarmCode,
            ConfirmedAsFact = ticket.ConfirmedAsFact,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CurrentJcCode = ticket.CurrentJcCode,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }

    /// <summary>
    /// 获取对话历史
    /// </summary>
    private async Task<string> GetConversationHistoryAsync(Guid ticketId)
    {
        var histories = await _dbContext.MissingInfoConversationHistories
            .Where(h => h.TicketId == ticketId && h.IsAnswered)
            .OrderBy(h => h.RoundNumber)
            .ThenBy(h => h.CreatedAt)
            .ToListAsync();

        if (!histories.Any())
        {
            return "";
        }

        var historyText = new System.Text.StringBuilder();
        foreach (var history in histories)
        {
            historyText.AppendLine($"Q{history.RoundNumber}: {history.Question}");
            if (!string.IsNullOrEmpty(history.UserAnswer))
            {
                historyText.AppendLine($"A{history.RoundNumber}: {history.UserAnswer}");
            }
        }

        return historyText.ToString();
    }

    /// <summary>
    /// 保存对话历史
    /// </summary>
    private async Task SaveConversationHistoryAsync(
        Guid ticketId,
        int roundNumber,
        string questionId,
        string question,
        string questionType,
        string? userAnswer = null,
        bool isSkipped = false)
    {
        var history = new MissingInfoConversationHistory
        {
            ConversationId = Guid.NewGuid(),
            TicketId = ticketId,
            RoundNumber = roundNumber,
            QuestionId = questionId,
            Question = question,
            QuestionType = questionType,
            UserAnswer = userAnswer,
            AnsweredAt = userAnswer != null ? DateTime.UtcNow : null,
            IsAnswered = !string.IsNullOrEmpty(userAnswer),
            IsSkipped = isSkipped,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MissingInfoConversationHistories.Add(history);
        await _dbContext.SaveChangesAsync();
    }
}

