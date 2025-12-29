using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Helpers;
using FieldTicket.Infrastructure.Storage;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// AI引导式工单创建服务实现
/// </summary>
public class GuidedTicketCreationService : IGuidedTicketCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMultimodalAIService _multimodalAIService;
    private readonly ITicketService _ticketService;
    private readonly IAttachmentService _attachmentService;
    private readonly ILLMService _llmService;
    private readonly ILogger<GuidedTicketCreationService> _logger;

    public GuidedTicketCreationService(
        ApplicationDbContext dbContext,
        IMultimodalAIService multimodalAIService,
        ITicketService ticketService,
        IAttachmentService attachmentService,
        ILLMService llmService,
        ILogger<GuidedTicketCreationService> logger)
    {
        _dbContext = dbContext;
        _multimodalAIService = multimodalAIService;
        _ticketService = ticketService;
        _attachmentService = attachmentService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<GuidedTicketCreationSession> CreateSessionAsync(Guid userId)
    {
        var session = new GuidedTicketCreationSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            Status = "collecting",
            TurnCount = 0,
            MaxTurns = 3,
            ConversationHistory = new List<GuidedConversationTurn>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 保存到数据库
        var sessionEntity = new GuidedTicketSession
        {
            Id = session.SessionId,
            UserId = userId,
            Status = session.Status,
            TurnCount = session.TurnCount,
            MaxTurns = session.MaxTurns,
            ConversationHistory = JsonSerializer.Serialize(session.ConversationHistory),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };

        _dbContext.GuidedTicketSessions.Add(sessionEntity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created guided ticket creation session {SessionId} for user {UserId}", 
            session.SessionId, userId);

        return session;
    }

    public async Task<GuidedQuestionResponse> SubmitInitialInfoAsync(
        Guid sessionId,
        string textDescription,
        List<Stream>? images = null)
    {
        var session = await GetSessionAsync(sessionId);
        
        if (session.Status != "collecting")
        {
            throw new InvalidOperationException($"Session is in {session.Status} status, cannot submit initial info");
        }

        session.InitialText = textDescription;
        session.Status = "analyzing";
        session.UpdatedAt = DateTime.UtcNow;

        // 保存图片附件（如果有）
        var imageAttachmentIds = new List<Guid>();
        if (images != null && images.Any())
        {
            // 创建一个临时工单草稿用于上传附件
            var tempTicket = new Ticket
            {
                TicketId = Guid.NewGuid(),
                TicketNo = $"TEMP-{session.SessionId:N}",
                CustomerId = Guid.Empty,
                ProjectId = Guid.Empty,
                DeviceId = Guid.Empty,
                CreatedByUserId = session.UserId,
                Domain = 'E',
                StepCode = "TEMP",
                SymptomTitle = "临时工单（用于附件上传）",
                SwVersion = "",
                PlcVersion = "",
                ParamVersion = "",
                FactsJson = System.Text.Json.JsonDocument.Parse("{}"),
                Status = "Draft",
                Priority = "P4",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Tickets.Add(tempTicket);
            await _dbContext.SaveChangesAsync();

            // 上传图片附件
            var imageStreamsForAnalysis = new List<Stream>();
            foreach (var imageStream in images)
            {
                try
                {
                    // 创建流的副本（因为流只能读取一次）
                    using var memoryStream = new MemoryStream();
                    await imageStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    
                    // 生成文件名和检测MIME类型
                    var fileName = $"image_{Guid.NewGuid():N}.jpg";
                    var mimeType = MimeTypeHelper.GetMimeTypeFromFileName(fileName);
                    var fileSize = memoryStream.Length;

                    // 上传附件（使用副本）
                    var attachment = await _attachmentService.UploadAsync(
                        tempTicket.TicketId,
                        memoryStream,
                        fileName,
                        mimeType,
                        fileSize,
                        "photo",
                        session.UserId);

                    imageAttachmentIds.Add(attachment.AttachmentId);
                    
                    // 创建另一个副本用于AI分析
                    memoryStream.Position = 0;
                    var analysisStream = new MemoryStream();
                    await memoryStream.CopyToAsync(analysisStream);
                    analysisStream.Position = 0;
                    imageStreamsForAnalysis.Add(analysisStream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload image for session {SessionId}", sessionId);
                    // 继续处理其他图片
                }
            }
            
            // 使用副本进行AI分析
            images = imageStreamsForAnalysis;

            // 保存临时工单ID到会话（用于后续清理）
            session.TicketId = tempTicket.TicketId;
        }

        session.ImageAttachmentIds = imageAttachmentIds;

        // 添加用户消息到对话历史
        session.ConversationHistory.Add(new GuidedConversationTurn
        {
            TurnNumber = 1,
            Role = "user",
            Content = textDescription,
            AttachmentIds = imageAttachmentIds.Any() ? imageAttachmentIds : null,
            Timestamp = DateTime.UtcNow
        });

        // AI分析
        try
        {
            // 综合分析（文本+图片）
            var comprehensiveAnalysis = await _multimodalAIService.AnalyzeComprehensiveAsync(
                textDescription,
                images);

            session.ComprehensiveAnalysis = comprehensiveAnalysis;
            session.TextAnalysis = comprehensiveAnalysis.Domain != null 
                ? new TextAnalysisResult
                {
                    Domain = comprehensiveAnalysis.Domain,
                    ProfessionalDescription = comprehensiveAnalysis.ProfessionalDescription,
                    KeyInformation = comprehensiveAnalysis.KeyInformation,
                    MissingInfo = comprehensiveAnalysis.MissingInfo,
                    TerminologySuggestions = comprehensiveAnalysis.TerminologySuggestions
                }
                : null;

            // 生成引导性问题
            var guidingQuestions = await GenerateGuidingQuestionsAsync(session, comprehensiveAnalysis);

            session.Status = guidingQuestions.IsComplete ? "completing" : "guiding";
            session.TurnCount = 1;
            session.UpdatedAt = DateTime.UtcNow;

            // 添加AI回复到对话历史
            session.ConversationHistory.Add(new GuidedConversationTurn
            {
                TurnNumber = 2,
                Role = "assistant",
                Content = string.Join("\n", guidingQuestions.Suggestions),
                Questions = guidingQuestions.Questions,
                Timestamp = DateTime.UtcNow
            });

            // 保存会话
            await SaveSessionAsync(session);

            return guidingQuestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze initial info for session {SessionId}", sessionId);
            session.Status = "collecting";
            await SaveSessionAsync(session);
            throw;
        }
    }

    public async Task<GuidedQuestionResponse> AnswerQuestionAsync(
        Guid sessionId,
        string questionId,
        string answer,
        List<Stream>? additionalImages = null)
    {
        var session = await GetSessionAsync(sessionId);

        if (session.Status != "guiding")
        {
            throw new InvalidOperationException($"Session is in {session.Status} status, cannot answer questions");
        }

        if (session.TurnCount >= session.MaxTurns)
        {
            throw new InvalidOperationException("Maximum number of turns reached");
        }

        // 添加用户回答到对话历史
        session.ConversationHistory.Add(new GuidedConversationTurn
        {
            TurnNumber = session.ConversationHistory.Count + 1,
            Role = "user",
            Content = answer,
            Timestamp = DateTime.UtcNow
        });

        // 更新综合分析结果（基于新信息）
        var updatedAnalysis = await UpdateAnalysisWithAnswerAsync(session, questionId, answer);

        // 生成新的引导性问题
        var guidingQuestions = await GenerateGuidingQuestionsAsync(session, updatedAnalysis);

        session.TurnCount++;
        session.Status = guidingQuestions.IsComplete || session.TurnCount >= session.MaxTurns 
            ? "completing" 
            : "guiding";
        session.UpdatedAt = DateTime.UtcNow;

        // 添加AI回复到对话历史
        session.ConversationHistory.Add(new GuidedConversationTurn
        {
            TurnNumber = session.ConversationHistory.Count + 1,
            Role = "assistant",
            Content = string.Join("\n", guidingQuestions.Suggestions),
            Questions = guidingQuestions.Questions,
            Timestamp = DateTime.UtcNow
        });

        // 保存会话
        await SaveSessionAsync(session);

        return new GuidedQuestionResponse
        {
            Session = session,
            IsComplete = guidingQuestions.IsComplete,
            Questions = guidingQuestions.Questions,
            Suggestions = guidingQuestions.Suggestions,
            NextStepHint = guidingQuestions.NextStepHint
        };
    }

    public async Task<GuidedTicketContent> GenerateTicketContentAsync(Guid sessionId)
    {
        var session = await GetSessionAsync(sessionId);

        if (session.Status != "completing" && session.Status != "guiding")
        {
            throw new InvalidOperationException($"Session is in {session.Status} status, cannot generate ticket content");
        }

        // 生成专业总结
        var summary = await GenerateTicketSummaryAsync(session);

        // 更新会话状态
        session.Status = "completing";
        session.UpdatedAt = DateTime.UtcNow;
        await SaveSessionAsync(session);

        _logger.LogInformation("Generated ticket content for session {SessionId}", sessionId);

        return new GuidedTicketContent
        {
            Summary = summary,
            ImageAttachmentIds = session.ImageAttachmentIds,
            TerminologySuggestions = session.ComprehensiveAnalysis?.TerminologySuggestions ?? new List<TerminologySuggestion>(),
            Confidence = summary.Confidence
        };
    }

    public async Task<TicketDto> CreateTicketFromSessionAsync(Guid sessionId, Guid deviceId)
    {
        var session = await GetSessionAsync(sessionId);

        if (session.Status != "completing")
        {
            throw new InvalidOperationException($"Session is in {session.Status} status, cannot create ticket");
        }

        // 生成专业总结
        var summary = await GenerateTicketSummaryAsync(session);

        // 创建工单草稿
        var createRequest = new CreateTicketRequest
        {
            DeviceId = deviceId,
            Domain = summary.Domain.Length > 0 ? summary.Domain[0] : 'E',
            StepCode = summary.StepCode ?? "",
            StepName = summary.StepName,
            SymptomTitle = summary.SymptomTitle,
            SymptomDetail = summary.SymptomDetail,
            FactsJson = ConvertFactsToJsonDocument(summary.FactsJson),
            SwVersion = summary.VersionInfo?.SwVersion ?? "",
            PlcVersion = summary.VersionInfo?.PlcVersion ?? "",
            ParamVersion = summary.VersionInfo?.ParamVersion ?? "",
            ConfirmedAsFact = false
        };

        var ticket = await _ticketService.CreateDraftAsync(createRequest, session.UserId);

        // 关联附件（如果有）
        if (session.ImageAttachmentIds.Any())
        {
            // 如果之前创建了临时工单，将附件关联到新工单
            if (session.TicketId.HasValue && session.TicketId != ticket.TicketId)
            {
                var tempTicketId = session.TicketId.Value;
                var attachments = await _dbContext.Attachments
                    .Where(a => a.TicketId == tempTicketId && session.ImageAttachmentIds.Contains(a.AttachmentId))
                    .ToListAsync();

                foreach (var attachment in attachments)
                {
                    attachment.TicketId = ticket.TicketId;
                }

                await _dbContext.SaveChangesAsync();

                // 删除临时工单
                var tempTicket = await _dbContext.Tickets.FindAsync(tempTicketId);
                if (tempTicket != null)
                {
                    _dbContext.Tickets.Remove(tempTicket);
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Moved {Count} attachments from temp ticket {TempTicketId} to ticket {TicketId}",
                    attachments.Count, tempTicketId, ticket.TicketId);
            }
        }

        // 更新会话
        session.TicketId = ticket.TicketId;
        session.Status = "completed";
        session.UpdatedAt = DateTime.UtcNow;
        session.CompletedAt = DateTime.UtcNow;
        await SaveSessionAsync(session);

        _logger.LogInformation("Created ticket draft {TicketId} from session {SessionId}", 
            ticket.TicketId, sessionId);

        return ticket;
    }

    public async Task<GuidedTicketCreationSession> GetSessionAsync(Guid sessionId)
    {
        var sessionEntity = await _dbContext.GuidedTicketSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (sessionEntity == null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found");
        }

        return MapToSession(sessionEntity);
    }

    public async Task<int> CleanupExpiredSessionsAsync(TimeSpan? expirationTime = null)
    {
        expirationTime ??= TimeSpan.FromHours(24);
        var cutoffTime = DateTime.UtcNow - expirationTime.Value;

        var expiredSessions = await _dbContext.GuidedTicketSessions
            .Where(s => s.Status != "completed" && s.CreatedAt < cutoffTime)
            .ToListAsync();

        var count = expiredSessions.Count;
        
        if (count > 0)
        {
            // 删除关联的临时工单（如果有）
            foreach (var session in expiredSessions)
            {
                if (session.TicketId.HasValue)
                {
                    var tempTicket = await _dbContext.Tickets.FindAsync(session.TicketId.Value);
                    if (tempTicket != null && tempTicket.TicketNo.StartsWith("TEMP-"))
                    {
                        // 删除临时工单的附件
                        var attachments = await _dbContext.Attachments
                            .Where(a => a.TicketId == tempTicket.TicketId)
                            .ToListAsync();
                        
                        _dbContext.Attachments.RemoveRange(attachments);
                        _dbContext.Tickets.Remove(tempTicket);
                    }
                }
            }

            _dbContext.GuidedTicketSessions.RemoveRange(expiredSessions);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("清理了 {Count} 个过期会话", count);
        }

        return count;
    }

    private async Task<GuidedQuestionResponse> GenerateGuidingQuestionsAsync(
        GuidedTicketCreationSession session,
        ComprehensiveAnalysisResult analysis)
    {
        var prompt = BuildGuidingQuestionPrompt(session, analysis);

        var schema = new JsonSchema
        {
            Definition = @"{
  ""type"": ""object"",
  ""properties"": {
    ""is_complete"": { ""type"": ""boolean"" },
    ""questions"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""question_id"": { ""type"": ""string"" },
          ""question"": { ""type"": ""string"" },
          ""type"": { ""type"": ""string"", ""enum"": [""yes_no"", ""text"", ""number"", ""select"", ""file""] },
          ""options"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
          ""hint"": { ""type"": ""string"" },
          ""professional_term_example"": { ""type"": ""string"" },
          ""why_important"": { ""type"": ""string"" }
        },
        ""required"": [""question_id"", ""question"", ""type""]
      }
    },
    ""suggestions"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" }
    },
    ""next_step_hint"": { ""type"": ""string"" }
  },
  ""required"": [""is_complete"", ""questions""]
}"
        };

        var result = await _llmService.GenerateStructuredAsync<GuidedQuestionResponse>(
            prompt,
            schema,
            new LLMRequestOptions { Temperature = 0.4, MaxTokens = 2000 });

        result.Session = session;
        return result;
    }

    private string BuildGuidingQuestionPrompt(
        GuidedTicketCreationSession session,
        ComprehensiveAnalysisResult analysis)
    {
        return $@"
你是一位耐心的技术指导专家，正在帮助一位现场工程师描述设备问题。

## 当前收集的信息

**文字描述**：
{session.InitialText}

**已识别信息**：
- 问题域：{analysis.Domain}
- 关键信息：{string.Join(", ", analysis.KeyInformation?.DeviceModel ?? "", analysis.KeyInformation?.Symptom ?? "")}

**对话历史**：
{FormatConversationHistory(session.ConversationHistory)}

**缺失的关键信息**：
{FormatMissingInfo(analysis.MissingInfo)}

## 你的任务

根据当前信息，生成1-3个引导性问题，帮助工程师更专业地描述问题。

**问题要求**：
1. 问题要简单易懂，避免专业术语（如果必须用，要解释）
2. 问题要有针对性，针对缺失的关键信息
3. 问题要有引导性，帮助工程师用专业术语描述
4. 问题要循序渐进，从简单到复杂

**如果信息已足够完整**：
- 设置 is_complete = true
- 生成确认性问题，确认信息准确性

返回JSON格式：
{{
  ""is_complete"": false,
  ""questions"": [
    {{
      ""question_id"": ""q1"",
      ""question"": ""问题文本"",
      ""type"": ""yes_no|text|number|select|file"",
      ""options"": [""选项1"", ""选项2""],
      ""hint"": ""提示信息（如何回答）"",
      ""professional_term_example"": ""专业术语示例（如果适用）"",
      ""why_important"": ""为什么需要这个信息""
    }}
  ],
  ""suggestions"": [
    ""建议1：可以这样描述..."",
    ""建议2：专业术语是...""
  ],
  ""next_step_hint"": ""下一步提示""
}}";
    }

    private async Task<ComprehensiveAnalysisResult> UpdateAnalysisWithAnswerAsync(
        GuidedTicketCreationSession session,
        string questionId,
        string answer)
    {
        // 基于新答案更新分析结果
        var prompt = $@"
基于以下新信息，更新分析结果：

**原始分析**：
{JsonSerializer.Serialize(session.ComprehensiveAnalysis)}

**新回答**：
问题ID：{questionId}
回答：{answer}

请更新分析结果，特别是：
1. 更新关键信息（如果新回答提供了关键信息）
2. 更新缺失信息清单（移除已补充的信息）
3. 更新专业描述（如果新信息有助于完善描述）

返回更新后的分析结果（JSON格式，与ComprehensiveAnalysisResult相同结构）。";

        var resultJson = await _llmService.GenerateTextAsync(prompt);
        var result = JsonSerializer.Deserialize<ComprehensiveAnalysisResult>(resultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? session.ComprehensiveAnalysis ?? new ComprehensiveAnalysisResult();

        return result;
    }

    private async Task<TicketSummary> GenerateTicketSummaryAsync(GuidedTicketCreationSession session)
    {
        var prompt = BuildSummaryPrompt(session);

        var schema = new JsonSchema
        {
            Definition = @"{
  ""type"": ""object"",
  ""properties"": {
    ""symptom_title"": { ""type"": ""string"" },
    ""symptom_detail"": { ""type"": ""string"" },
    ""domain"": { ""type"": ""string"" },
    ""step_code"": { ""type"": ""string"" },
    ""step_name"": { ""type"": ""string"" },
    ""facts_json"": { ""type"": ""object"", ""additionalProperties"": { ""type"": ""string"" } },
    ""version_info"": {
      ""type"": ""object"",
      ""properties"": {
        ""sw_version"": { ""type"": ""string"" },
        ""plc_version"": { ""type"": ""string"" },
        ""param_version"": { ""type"": ""string"" }
      }
    },
    ""confidence"": {
      ""type"": ""object"",
      ""properties"": {
        ""overall"": { ""type"": ""integer"" },
        ""title"": { ""type"": ""integer"" },
        ""detail"": { ""type"": ""integer"" },
        ""facts"": { ""type"": ""integer"" }
      }
    }
  },
  ""required"": [""symptom_title"", ""symptom_detail"", ""domain""]
}"
        };

        return await _llmService.GenerateStructuredAsync<TicketSummary>(
            prompt,
            schema,
            new LLMRequestOptions { Temperature = 0.3, MaxTokens = 2000 });
    }

    private string BuildSummaryPrompt(GuidedTicketCreationSession session)
    {
        return $@"
你是一位专业的技术文档编写专家。请根据以下信息，生成一份专业的工单问题描述。

## 收集的完整信息

**原始描述**：
{session.InitialText}

**对话补充信息**：
{FormatConversationHistory(session.ConversationHistory)}

**AI分析结果**：
{JsonSerializer.Serialize(session.ComprehensiveAnalysis)}

## 要求

1. **问题标题**（一句话，不含判断）：
   - 使用专业术语
   - 清晰描述现象
   - 不含原因推测

2. **详细描述**：
   - 结构化描述（现象、频率、环境等）
   - 使用专业术语
   - 包含关键细节

3. **事实表提取**：
   - 从对话中提取YES/NO事实
   - 结构化组织

4. **版本信息**（如可识别）：
   - 从图片或对话中提取版本号

返回JSON格式：
{{
  ""symptom_title"": ""专业的问题标题"",
  ""symptom_detail"": ""专业的问题详细描述"",
  ""domain"": ""A|B|C|D|E"",
  ""step_code"": ""步骤代码（如可识别）"",
  ""step_name"": ""步骤名称（如可识别）"",
  ""facts_json"": {{
    ""fact1"": ""YES|NO|UNKNOWN"",
    ""fact2"": ""YES|NO|UNKNOWN""
  }},
  ""version_info"": {{
    ""sw_version"": ""软件版本（如可识别）"",
    ""plc_version"": ""PLC版本（如可识别）"",
    ""param_version"": ""参数版本（如可识别）""
  }},
  ""confidence"": {{
    ""overall"": 1-5,
    ""title"": 1-5,
    ""detail"": 1-5,
    ""facts"": 1-5
  }}
}}";
    }

    private JsonDocument ConvertFactsToJsonDocument(Dictionary<string, string> facts)
    {
        if (facts == null || !facts.Any())
        {
            return JsonDocument.Parse("{}");
        }

        var jsonObject = new Dictionary<string, object>();
        foreach (var fact in facts)
        {
            jsonObject[fact.Key] = fact.Value;
        }

        return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(jsonObject));
    }

    private string FormatConversationHistory(List<GuidedConversationTurn> history)
    {
        return string.Join("\n", history.Select((turn, idx) => 
            $"{idx + 1}. [{turn.Role}] {turn.Content}"));
    }

    private string FormatMissingInfo(List<MissingInfoItem>? missingInfo)
    {
        if (missingInfo == null || !missingInfo.Any())
        {
            return "无缺失信息";
        }

        return string.Join("\n", missingInfo.Select((item, idx) => 
            $"{idx + 1}. {item.Question}（字段：{item.Field}）"));
    }

    private async Task SaveSessionAsync(GuidedTicketCreationSession session)
    {
        var sessionEntity = await _dbContext.GuidedTicketSessions
            .FirstOrDefaultAsync(s => s.Id == session.SessionId);

        if (sessionEntity == null)
        {
            throw new KeyNotFoundException($"Session {session.SessionId} not found");
        }

        sessionEntity.Status = session.Status;
        sessionEntity.TurnCount = session.TurnCount;
        sessionEntity.TicketId = session.TicketId;
        sessionEntity.InitialText = session.InitialText;
        sessionEntity.ImageAttachmentIds = session.ImageAttachmentIds.ToArray();
        sessionEntity.TextAnalysis = session.TextAnalysis != null 
            ? JsonSerializer.Serialize(session.TextAnalysis) 
            : null;
        sessionEntity.ImageAnalyses = session.ImageAnalyses.Any()
            ? session.ImageAnalyses.Select(img => JsonSerializer.Serialize(img)).ToArray()
            : Array.Empty<string>();
        sessionEntity.ComprehensiveAnalysis = session.ComprehensiveAnalysis != null
            ? JsonSerializer.Serialize(session.ComprehensiveAnalysis)
            : null;
        sessionEntity.ConversationHistory = JsonSerializer.Serialize(session.ConversationHistory);
        sessionEntity.UpdatedAt = DateTime.UtcNow;
        if (session.Status == "completed" && sessionEntity.CompletedAt == null)
        {
            sessionEntity.CompletedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    private GuidedTicketCreationSession MapToSession(GuidedTicketSession entity)
    {
        var session = new GuidedTicketCreationSession
        {
            SessionId = entity.Id,
            TicketId = entity.TicketId,
            UserId = entity.UserId,
            Status = entity.Status,
            TurnCount = entity.TurnCount,
            MaxTurns = entity.MaxTurns,
            InitialText = entity.InitialText,
            ImageAttachmentIds = entity.ImageAttachmentIds?.ToList() ?? new List<Guid>(),
            ConversationHistory = JsonSerializer.Deserialize<List<GuidedConversationTurn>>(
                entity.ConversationHistory ?? "[]",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<GuidedConversationTurn>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        };

        if (!string.IsNullOrEmpty(entity.TextAnalysis))
        {
            session.TextAnalysis = JsonSerializer.Deserialize<TextAnalysisResult>(
                entity.TextAnalysis,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        if (entity.ImageAnalyses != null && entity.ImageAnalyses.Any())
        {
            session.ImageAnalyses = entity.ImageAnalyses
                .Select(json => JsonSerializer.Deserialize<ImageAnalysisResult>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ImageAnalysisResult())
                .ToList();
        }

        if (!string.IsNullOrEmpty(entity.ComprehensiveAnalysis))
        {
            session.ComprehensiveAnalysis = JsonSerializer.Deserialize<ComprehensiveAnalysisResult>(
                entity.ComprehensiveAnalysis,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return session;
    }
}
