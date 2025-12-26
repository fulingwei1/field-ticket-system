using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 对话式诊断服务实现
/// </summary>
public class ConversationalDiagnosisService : IConversationalDiagnosisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConversationalDiagnosisService> _logger;
    private readonly ILLMService? _llmService;
    private const int MaxConversationRounds = 5;

    public ConversationalDiagnosisService(
        ApplicationDbContext dbContext,
        ILogger<ConversationalDiagnosisService> logger,
        ILLMService? llmService = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _llmService = llmService;
    }

    public async Task<DiagnosisConversationDto> StartConversationAsync(Guid ticketId)
    {
        _logger.LogInformation("Starting diagnosis conversation for ticket {TicketId}", ticketId);

        // 验证工单存在
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 检查是否已有活跃的对话
        var existingConversation = await _dbContext.DiagnosisConversations
            .FirstOrDefaultAsync(c => c.TicketId == ticketId && c.Status == "active");

        if (existingConversation != null)
        {
            _logger.LogInformation("Found existing active conversation {ConversationId} for ticket {TicketId}",
                existingConversation.ConversationId, ticketId);
            return MapToDto(existingConversation);
        }

        // 创建新对话
        var conversation = new DiagnosisConversation
        {
            ConversationId = Guid.NewGuid(),
            TicketId = ticketId,
            ConversationRound = 0,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.DiagnosisConversations.Add(conversation);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new diagnosis conversation {ConversationId} for ticket {TicketId}",
            conversation.ConversationId, ticketId);

        return MapToDto(conversation);
    }

    public async Task<List<Shared.Models.HypothesisDto>> GenerateInitialHypothesesAsync(Guid ticketId)
    {
        _logger.LogInformation("Generating initial hypotheses for ticket {TicketId}", ticketId);

        // 获取工单信息
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {ticketId} 不存在");
        }

        // 尝试使用 LLM 生成假设
        if (_llmService != null)
        {
            try
            {
                var isAvailable = await _llmService.IsAvailableAsync();
                if (isAvailable)
                {
                    var prompt = BuildHypothesisPrompt(ticket);
                    var response = await _llmService.GenerateStructuredAsync<HypothesisGenerationResponse>(
                        prompt,
                        options: new LLMRequestOptions
                        {
                            Temperature = 0.3, // 较低温度，更确定性
                            MaxTokens = 1500
                        });

                    return response.Hypotheses.Select((h, index) => new Shared.Models.HypothesisDto
                    {
                        HypothesisId = $"hypothesis_{index + 1}",
                        Description = h.Description,
                        Confidence = ConvertConfidenceToDecimal(h.Confidence),
                        Evidence = h.Evidence ?? new List<string>(),
                        SupportingKnowledgeIds = h.SupportingKnowledgeIds ?? new List<string>()
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate hypotheses using LLM, falling back to rule-based");
            }
        }

        // 降级方案：基于规则的假设生成
        return GenerateRuleBasedHypotheses(ticket);
    }

    public async Task<List<VerificationStepDto>> GenerateVerificationStepsAsync(
        Guid conversationId,
        string hypothesisId)
    {
        _logger.LogInformation("Generating verification steps for conversation {ConversationId}, hypothesis {HypothesisId}",
            conversationId, hypothesisId);

        // 验证对话存在
        var conversation = await _dbContext.DiagnosisConversations
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"对话 {conversationId} 不存在");
        }

        if (conversation.Status != "active")
        {
            throw new InvalidOperationException($"对话 {conversationId} 状态不是 active");
        }

        // 获取工单和假设信息
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == conversation.TicketId);

        if (ticket == null)
        {
            throw new KeyNotFoundException($"工单 {conversation.TicketId} 不存在");
        }

        // 获取假设描述（从对话历史或假设ID）
        var hypothesisDescription = conversation.CurrentHypothesis ?? hypothesisId;

        // 尝试使用 LLM 生成验证步骤
        List<VerificationStepDto> steps;
        if (_llmService != null)
        {
            try
            {
                var isAvailable = await _llmService.IsAvailableAsync();
                if (isAvailable)
                {
                    var prompt = BuildVerificationStepsPrompt(ticket, hypothesisDescription);
                    var response = await _llmService.GenerateStructuredAsync<VerificationStepsGenerationResponse>(
                        prompt,
                        options: new LLMRequestOptions
                        {
                            Temperature = 0.4,
                            MaxTokens = 2000
                        });

                    var stepGuids = response.Steps.Select(_ => Guid.NewGuid()).ToList();
                    steps = response.Steps.Select((s, index) => new VerificationStepDto
                    {
                        StepId = stepGuids[index],
                        ConversationId = conversationId,
                        HypothesisId = hypothesisId,
                        StepDescription = s.StepDescription,
                        StepType = s.StepType ?? "check",
                        ExpectedResult = s.ExpectedResult,
                        VerificationStatus = "pending",
                        CreatedAt = DateTime.UtcNow
                    }).ToList();
                }
                else
                {
                    // LLM 不可用，使用降级方案
                    steps = GenerateRuleBasedVerificationSteps(hypothesisDescription);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate verification steps using LLM, falling back to rule-based");
                steps = GenerateRuleBasedVerificationSteps(hypothesisDescription);
            }
        }
        else
        {
            // 没有 LLM 服务，使用降级方案
            steps = GenerateRuleBasedVerificationSteps(hypothesisDescription);
        }

        // 保存验证步骤到数据库
        foreach (var stepDto in steps)
        {
            // 确保 ConversationId 和 HypothesisId 正确设置
            stepDto.ConversationId = conversationId;
            stepDto.HypothesisId = hypothesisId;

            var step = new HypothesisVerificationStep
            {
                StepId = stepDto.StepId,
                ConversationId = conversationId,
                HypothesisId = hypothesisId,
                StepDescription = stepDto.StepDescription,
                StepType = stepDto.StepType,
                ExpectedResult = stepDto.ExpectedResult,
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.HypothesisVerificationSteps.Add(step);
        }

        await _dbContext.SaveChangesAsync();

        return steps;
    }

    public async Task<ConversationResultDto> SubmitVerificationResultAsync(
        Guid conversationId,
        SubmitVerificationResultRequest request)
    {
        _logger.LogInformation("Submitting verification result for conversation {ConversationId}, step {StepId}",
            conversationId, request.StepId);

        // 验证对话存在
        var conversation = await _dbContext.DiagnosisConversations
            .Include(c => c.VerificationSteps)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"对话 {conversationId} 不存在");
        }

        // 更新验证步骤
        var step = await _dbContext.HypothesisVerificationSteps
            .FirstOrDefaultAsync(s => s.StepId == request.StepId && s.ConversationId == conversationId);

        if (step == null)
        {
            throw new KeyNotFoundException($"验证步骤 {request.StepId} 不存在");
        }

        step.ActualResult = request.ActualResult;
        step.VerificationStatus = request.VerificationStatus;
        step.VerificationNotes = request.VerificationNotes;

        await _dbContext.SaveChangesAsync();

        // 判断是否继续对话
        var shouldContinue = conversation.ConversationRound < MaxConversationRounds &&
                             request.VerificationStatus != "passed";

        return new ConversationResultDto
        {
            ConversationId = conversationId,
            ShouldContinue = shouldContinue,
            NextAction = shouldContinue ? "继续验证其他假设" : "完成诊断"
        };
    }

    public async Task<Shared.Models.HypothesisDto> AdjustHypothesisAsync(
        Guid conversationId,
        AdjustHypothesisRequest request)
    {
        _logger.LogInformation("Adjusting hypothesis for conversation {ConversationId}, hypothesis {HypothesisId}",
            conversationId, request.HypothesisId);

        // 验证对话存在
        var conversation = await _dbContext.DiagnosisConversations
            .Include(c => c.VerificationSteps)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"对话 {conversationId} 不存在");
        }

        // 获取当前假设的验证步骤
        var verificationSteps = conversation.VerificationSteps
            .Where(s => s.HypothesisId == request.HypothesisId)
            .ToList();

        // 计算调整后的置信度
        var adjustedConfidence = CalculateAdjustedConfidence(verificationSteps);

        // 更新对话状态
        conversation.CurrentHypothesis = request.HypothesisId;
        conversation.CurrentConfidence = adjustedConfidence;
        conversation.ConversationRound++;
        conversation.UpdatedAt = DateTime.UtcNow;

        // 更新诊断路径
        UpdateDiagnosisPath(conversation, request.HypothesisId, adjustedConfidence, verificationSteps);

        await _dbContext.SaveChangesAsync();

        return new Shared.Models.HypothesisDto
        {
            HypothesisId = request.HypothesisId,
            Description = conversation.CurrentHypothesis ?? string.Empty,
            Confidence = adjustedConfidence,
            Evidence = new List<string>(),
            SupportingKnowledgeIds = new List<string>()
        };
    }

    public async Task<DiagnosisResultDto> CompleteDiagnosisAsync(Guid conversationId)
    {
        _logger.LogInformation("Completing diagnosis conversation {ConversationId}", conversationId);

        // 验证对话存在
        var conversation = await _dbContext.DiagnosisConversations
            .Include(c => c.VerificationSteps)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"对话 {conversationId} 不存在");
        }

        // 更新对话状态
        conversation.Status = "completed";
        conversation.UpdatedAt = DateTime.UtcNow;

        // 更新诊断路径的最终假设
        if (conversation.DiagnosisPathJson != null)
        {
            var path = JsonSerializer.Deserialize<Dictionary<string, object>>(
                conversation.DiagnosisPathJson.RootElement.GetRawText());

            if (path != null)
            {
                path["final_hypothesis"] = conversation.CurrentHypothesis ?? string.Empty;
                path["final_confidence"] = conversation.CurrentConfidence ?? 0m;
                conversation.DiagnosisPathJson = JsonDocument.Parse(JsonSerializer.Serialize(path));
            }
        }

        await _dbContext.SaveChangesAsync();

        return new DiagnosisResultDto
        {
            ConversationId = conversationId,
            FinalHypothesis = conversation.CurrentHypothesis ?? string.Empty,
            FinalConfidence = conversation.CurrentConfidence ?? 0m,
            DiagnosisPath = conversation.DiagnosisPathJson,
            TotalRounds = conversation.ConversationRound
        };
    }

    public async Task<DiagnosisPathDto> GetDiagnosisPathAsync(Guid conversationId)
    {
        _logger.LogInformation("Getting diagnosis path for conversation {ConversationId}", conversationId);

        // 验证对话存在
        var conversation = await _dbContext.DiagnosisConversations
            .Include(c => c.VerificationSteps)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
        {
            throw new KeyNotFoundException($"对话 {conversationId} 不存在");
        }

        // 解析诊断路径
        var pathDto = new DiagnosisPathDto
        {
            FinalHypothesis = conversation.CurrentHypothesis,
            FinalConfidence = conversation.CurrentConfidence
        };

        if (conversation.DiagnosisPathJson != null)
        {
            var path = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                conversation.DiagnosisPathJson.RootElement.GetRawText());

            if (path != null && path.ContainsKey("rounds"))
            {
                var rounds = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
                    path["rounds"].GetRawText());

                if (rounds != null)
                {
                    foreach (var round in rounds)
                    {
                        var roundDto = new DiagnosisRoundDto
                        {
                            Round = round.ContainsKey("round") ? round["round"].GetInt32() : 0,
                            Hypothesis = round.ContainsKey("hypothesis") ? round["hypothesis"].GetString() ?? string.Empty : string.Empty,
                            Confidence = round.ContainsKey("confidence") ? round["confidence"].GetDecimal() : 0m,
                            AdjustedHypothesis = round.ContainsKey("adjusted_hypothesis") ? round["adjusted_hypothesis"].GetString() : null,
                            AdjustedConfidence = round.ContainsKey("adjusted_confidence") ? round["adjusted_confidence"].GetDecimal() : null,
                            VerificationSteps = new List<VerificationStepDto>()
                        };

                        pathDto.Rounds.Add(roundDto);
                    }
                }
            }
        }

        return pathDto;
    }

    // 私有辅助方法

    private DiagnosisConversationDto MapToDto(DiagnosisConversation conversation)
    {
        return new DiagnosisConversationDto
        {
            ConversationId = conversation.ConversationId,
            TicketId = conversation.TicketId,
            CurrentHypothesis = conversation.CurrentHypothesis,
            CurrentConfidence = conversation.CurrentConfidence,
            ConversationRound = conversation.ConversationRound,
            Status = conversation.Status,
            DiagnosisPath = conversation.DiagnosisPathJson,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private decimal CalculateAdjustedConfidence(List<HypothesisVerificationStep> steps)
    {
        // 简单的置信度调整算法
        // 验证通过：+0.1，验证失败：-0.2，不确定：-0.05
        decimal baseConfidence = 0.7m; // 默认置信度

        foreach (var step in steps)
        {
            switch (step.VerificationStatus)
            {
                case "passed":
                    baseConfidence += 0.1m;
                    break;
                case "failed":
                    baseConfidence -= 0.2m;
                    break;
                case "inconclusive":
                    baseConfidence -= 0.05m;
                    break;
            }
        }

        // 限制置信度范围 [0, 1]
        return Math.Max(0m, Math.Min(1m, baseConfidence));
    }

    private void UpdateDiagnosisPath(
        DiagnosisConversation conversation,
        string hypothesisId,
        decimal adjustedConfidence,
        List<HypothesisVerificationStep> steps)
    {
        Dictionary<string, object> path;

        if (conversation.DiagnosisPathJson != null)
        {
            path = JsonSerializer.Deserialize<Dictionary<string, object>>(
                conversation.DiagnosisPathJson.RootElement.GetRawText()) ?? new Dictionary<string, object>();
        }
        else
        {
            path = new Dictionary<string, object>
            {
                ["rounds"] = new List<object>()
            };
        }

        // 获取或创建 rounds 列表
        var rounds = path.ContainsKey("rounds") && path["rounds"] is List<object> existingRounds
            ? existingRounds
            : new List<object>();

        // 创建当前轮次数据
        var round = new Dictionary<string, object>
        {
            ["round"] = conversation.ConversationRound,
            ["hypothesis"] = hypothesisId,
            ["confidence"] = conversation.CurrentConfidence ?? 0m,
            ["verification_steps"] = steps.Select(s => new Dictionary<string, object>
            {
                ["step_id"] = s.StepId.ToString(),
                ["step_description"] = s.StepDescription,
                ["verification_status"] = s.VerificationStatus,
                ["actual_result"] = s.ActualResult ?? string.Empty
            }).ToList(),
            ["adjusted_hypothesis"] = hypothesisId,
            ["adjusted_confidence"] = adjustedConfidence
        };

        rounds.Add(round);
        path["rounds"] = rounds;

        conversation.DiagnosisPathJson = JsonDocument.Parse(JsonSerializer.Serialize(path));
    }

    /// <summary>
    /// 构建假设生成提示词
    /// </summary>
    private string BuildHypothesisPrompt(Ticket ticket)
    {
        var factsJson = ticket.FactsJson != null
            ? ticket.FactsJson.RootElement.GetRawText()
            : "{}";

        return $@"基于以下工单信息，生成Top-3最可能的诊断假设：

工单信息：
- 问题域：{ticket.Domain}
- 步骤：{ticket.StepCode} {ticket.StepName ?? ""}
- 症状：{ticket.SymptomTitle}
- 详细描述：{ticket.SymptomDetail ?? "无"}
- 复现率：{ticket.ReproRate ?? 0}%
- 重启恢复：{(ticket.RebootRecovers ?? false ? "是" : "否")}
- 环境相关：{(ticket.EnvRelated ?? false ? "是" : "否")}
- 软件版本：{ticket.SwVersion}
- PLC版本：{ticket.PlcVersion}
- 参数版本：{ticket.ParamVersion}
- 事实表：{factsJson}

请生成Top-3假设，每个假设必须：
1. 有明确的描述（具体的问题原因）
2. 有置信度评估（high=0.8-1.0, medium=0.5-0.7, low=0.0-0.4）
3. 有证据支持（至少2条，基于工单信息）
4. 引用相关知识ID（如果有）

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

    /// <summary>
    /// 构建验证步骤生成提示词
    /// </summary>
    private string BuildVerificationStepsPrompt(Ticket ticket, string hypothesisDescription)
    {
        var factsJson = ticket.FactsJson != null
            ? ticket.FactsJson.RootElement.GetRawText()
            : "{}";

        return $@"基于以下工单信息和假设，生成验证步骤：

工单信息：
- 问题域：{ticket.Domain}
- 步骤：{ticket.StepCode} {ticket.StepName ?? ""}
- 症状：{ticket.SymptomTitle}
- 详细描述：{ticket.SymptomDetail ?? "无"}
- 事实表：{factsJson}

假设：{hypothesisDescription}

请生成3-5个验证步骤，每个步骤必须：
1. 有明确的描述（具体要检查什么）
2. 有步骤类型（check=检查, test=测试, measure=测量）
3. 有预期结果（期望看到什么）
4. 步骤应该按逻辑顺序排列

返回JSON格式：
{{
  ""steps"": [
    {{
      ""step_description"": ""步骤描述"",
      ""step_type"": ""check|test|measure"",
      ""expected_result"": ""预期结果""
    }}
  ]
}}";
    }

    /// <summary>
    /// 基于规则的假设生成（降级方案）
    /// </summary>
    private List<Shared.Models.HypothesisDto> GenerateRuleBasedHypotheses(Ticket ticket)
    {
        var hypotheses = new List<Shared.Models.HypothesisDto>();

        // 基于问题域生成通用假设
        var domainHypotheses = ticket.Domain switch
        {
            'A' => new[] { 
                ("机械部件故障", 0.7m, new[] { "问题域A", $"步骤{ticket.StepCode}" }),
                ("动作执行异常", 0.6m, new[] { "问题域A", ticket.SymptomTitle }),
                ("机械磨损", 0.5m, new[] { "问题域A", "长期运行" })
            },
            'B' => new[] { 
                ("电气信号异常", 0.7m, new[] { "问题域B", $"步骤{ticket.StepCode}" }),
                ("IO模块故障", 0.6m, new[] { "问题域B", ticket.SymptomTitle }),
                ("传感器故障", 0.5m, new[] { "问题域B", "信号检测" })
            },
            'C' => new[] { 
                ("PLC程序逻辑错误", 0.7m, new[] { "问题域C", $"步骤{ticket.StepCode}" }),
                ("程序版本不匹配", 0.6m, new[] { "问题域C", $"软件版本{ticket.SwVersion}" }),
                ("程序参数设置错误", 0.5m, new[] { "问题域C", $"参数版本{ticket.ParamVersion}" })
            },
            'D' => new[] { 
                ("测试判定条件错误", 0.7m, new[] { "问题域D", $"步骤{ticket.StepCode}" }),
                ("判定阈值设置不当", 0.6m, new[] { "问题域D", ticket.SymptomTitle }),
                ("测试环境异常", 0.5m, new[] { "问题域D", "环境相关" })
            },
            'E' => new[] { 
                ("系统环境异常", 0.7m, new[] { "问题域E", $"步骤{ticket.StepCode}" }),
                ("网络连接问题", 0.6m, new[] { "问题域E", ticket.SymptomTitle }),
                ("配置参数错误", 0.5m, new[] { "问题域E", "配置相关" })
            },
            _ => new[] { 
                ("未知问题", 0.5m, new[] { "需要进一步排查" }),
                ("建议升级处理", 0.3m, new[] { "问题复杂" })
            }
        };

        for (int i = 0; i < Math.Min(3, domainHypotheses.Length); i++)
        {
            var (description, confidence, evidence) = domainHypotheses[i];
            hypotheses.Add(new Shared.Models.HypothesisDto
            {
                HypothesisId = $"hypothesis_{i + 1}",
                Description = description,
                Confidence = confidence,
                Evidence = evidence.ToList(),
                SupportingKnowledgeIds = new List<string>()
            });
        }

        return hypotheses;
    }

    /// <summary>
    /// 基于规则的验证步骤生成（降级方案）
    /// </summary>
    private List<VerificationStepDto> GenerateRuleBasedVerificationSteps(string hypothesisDescription)
    {
        var steps = new List<VerificationStepDto>
        {
            new VerificationStepDto
            {
                StepId = Guid.NewGuid(),
                ConversationId = Guid.Empty, // 将在保存时设置
                HypothesisId = null, // 将在保存时设置
                StepDescription = $"检查与假设相关的硬件状态：{hypothesisDescription}",
                StepType = "check",
                ExpectedResult = "硬件状态正常",
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow
            },
            new VerificationStepDto
            {
                StepId = Guid.NewGuid(),
                ConversationId = Guid.Empty,
                HypothesisId = null,
                StepDescription = $"验证假设相关的信号或参数：{hypothesisDescription}",
                StepType = "test",
                ExpectedResult = "信号/参数在正常范围内",
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow
            },
            new VerificationStepDto
            {
                StepId = Guid.NewGuid(),
                ConversationId = Guid.Empty,
                HypothesisId = null,
                StepDescription = $"确认假设相关的功能是否正常：{hypothesisDescription}",
                StepType = "check",
                ExpectedResult = "功能正常",
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow
            }
        };

        return steps;
    }

    /// <summary>
    /// 将置信度字符串转换为小数
    /// </summary>
    private decimal ConvertConfidenceToDecimal(string confidence)
    {
        return confidence.ToLower() switch
        {
            "high" => 0.8m,
            "medium" => 0.6m,
            "low" => 0.4m,
            _ => 0.5m
        };
    }

    #region Response Models

    /// <summary>
    /// 假设生成响应模型
    /// </summary>
    private class HypothesisGenerationResponse
    {
        public List<HypothesisItem> Hypotheses { get; set; } = new();
    }

    /// <summary>
    /// 假设项
    /// </summary>
    private class HypothesisItem
    {
        public int Rank { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Confidence { get; set; } = "medium";
        public List<string>? Evidence { get; set; }
        public List<string>? SupportingKnowledgeIds { get; set; }
    }

    /// <summary>
    /// 验证步骤生成响应模型
    /// </summary>
    private class VerificationStepsGenerationResponse
    {
        public List<VerificationStepItem> Steps { get; set; } = new();
    }

    /// <summary>
    /// 验证步骤项
    /// </summary>
    private class VerificationStepItem
    {
        public string StepDescription { get; set; } = string.Empty;
        public string? StepType { get; set; }
        public string? ExpectedResult { get; set; }
    }

    #endregion
}


