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
    private const int MaxConversationRounds = 5;

    public ConversationalDiagnosisService(
        ApplicationDbContext dbContext,
        ILogger<ConversationalDiagnosisService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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

        // TODO: 集成AI服务生成假设
        // 这里先返回模拟数据，后续需要集成RAG服务和LLM服务
        var hypotheses = new List<Shared.Models.HypothesisDto>
        {
            new Shared.Models.HypothesisDto
            {
                HypothesisId = "hypothesis_1",
                Description = "可能是IO信号问题",
                Confidence = 0.7m,
                Evidence = new List<string> { "证据1", "证据2" },
                SupportingKnowledgeIds = new List<string>()
            },
            new Shared.Models.HypothesisDto
            {
                HypothesisId = "hypothesis_2",
                Description = "可能是传感器故障",
                Confidence = 0.6m,
                Evidence = new List<string> { "证据3" },
                SupportingKnowledgeIds = new List<string>()
            },
            new Shared.Models.HypothesisDto
            {
                HypothesisId = "hypothesis_3",
                Description = "可能是参数配置错误",
                Confidence = 0.5m,
                Evidence = new List<string> { "证据4" },
                SupportingKnowledgeIds = new List<string>()
            }
        };

        return hypotheses;
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

        // TODO: 集成AI服务生成验证步骤
        // 这里先返回模拟数据
        var steps = new List<VerificationStepDto>
        {
            new VerificationStepDto
            {
                StepId = Guid.NewGuid(),
                ConversationId = conversationId,
                HypothesisId = hypothesisId,
                StepDescription = "检查PLC中IO状态",
                StepType = "check",
                ExpectedResult = "IO状态正常",
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow
            },
            new VerificationStepDto
            {
                StepId = Guid.NewGuid(),
                ConversationId = conversationId,
                HypothesisId = hypothesisId,
                StepDescription = "检查传感器信号",
                StepType = "test",
                ExpectedResult = "传感器信号正常",
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow
            }
        };

        // 保存验证步骤到数据库
        foreach (var stepDto in steps)
        {
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
}


