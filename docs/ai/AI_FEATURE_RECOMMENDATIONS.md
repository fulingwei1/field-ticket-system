# AI功能改进建议

> **日期**：2025-12-29  
> **基于**：当前代码实现 + 技术选型文档  
> **目标**：提升AI功能质量、可靠性和成本效益

---

## 📊 当前AI功能现状

### ✅ 已实现功能

1. **LLM服务集成** ✅
   - OpenAI GPT-4o
   - 智谱AI GLM-4
   - Google Gemini（刚集成）
   - 统一的 `ILLMService` 接口
   - 结构化输出支持（JSON Schema）

2. **AI深度分析服务** ✅
   - 缺失信息识别（明确+隐含）
   - 上下文理解
   - 多轮对话支持
   - 个性化问题生成

3. **AI辅助分诊服务** ✅
   - 判断卡推荐
   - Top-3假设生成
   - 下一步动作建议
   - 缺失信息问题清单

4. **AI分析服务** ✅
   - 每日/每周工作总结
   - 团队分析
   - 人员安排建议

5. **Prompt工程** ✅
   - 4个专业Prompt模板
   - 结构化输出要求

### ⚠️ 缺失/待改进功能

1. **RAG（检索增强生成）** ❌
   - 向量数据库未集成
   - Embedding服务未实现
   - 证据引用机制不完善

2. **成本监控** ❌
   - Token消耗统计缺失
   - 成本告警未实现
   - 使用量分析缺失

3. **对话历史存储** ❌
   - 多轮对话历史未持久化
   - 无法追溯历史对话

4. **质量监控** ⚠️
   - AI输出采纳率未统计
   - 置信度准确性未评估
   - 错误反馈机制缺失

---

## 🎯 核心改进建议

### 1. 优先级P0：实现RAG系统（检索增强生成）

**问题**：
- 当前AI生成假设时没有基于历史工单和判断卡库的证据
- 无法追溯AI输出的来源
- 不符合"AI作为副驾驶"的设计原则

**建议方案**：

#### 1.1 集成PGVector向量数据库

```csharp
// 新增服务接口
public interface IVectorDBService
{
    Task<List<KnowledgeChunk>> SearchSimilarAsync(
        string queryText, 
        int topK = 5,
        Dictionary<string, object>? filters = null);
    
    Task IndexDocumentAsync(KnowledgeChunk chunk);
    Task BulkIndexAsync(List<KnowledgeChunk> chunks);
}
```

**实施步骤**：
1. 在PostgreSQL中启用 `pgvector` 扩展
2. 创建向量表存储判断卡和历史工单的向量
3. 实现 `PgVectorService` 使用HNSW索引

#### 1.2 实现Embedding服务

```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
}
```

**实施步骤**：
1. 集成OpenAI `text-embedding-3-small` API
2. 实现降级方案（BGE-M3本地模型或通义千问）
3. 添加缓存机制（相同文本不重复调用）

#### 1.3 完善证据引用机制

**当前问题**：
- `AIAssistedTriageService.GenerateHypothesesAsync()` 中注释提到"简化版RAG"，但实际没有实现

**改进方案**：
```csharp
public async Task<List<HypothesisDto>> GenerateHypothesesAsync(
    Guid ticketId, 
    string? jcCode)
{
    // 1. 生成查询向量
    var queryText = BuildQueryText(ticket);
    var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(queryText);
    
    // 2. 向量检索（RAG）
    var evidenceChunks = await _vectorDBService.SearchSimilarAsync(
        queryText,
        topK: 10,
        filters: new Dictionary<string, object>
        {
            { "domain", ticket.Domain },
            { "type", new[] { "judgement_card", "ticket" } }
        });
    
    // 3. 重排序（可选）
    var rerankedChunks = await RerankAsync(queryText, evidenceChunks);
    
    // 4. LLM生成假设（带证据引用）
    var prompt = BuildHypothesisPrompt(ticket, rerankedChunks);
    var hypotheses = await _llmService.GenerateStructuredAsync<HypothesisResponse>(
        prompt, 
        GetHypothesisSchema());
    
    // 5. 关联证据ID
    foreach (var hypothesis in hypotheses.Hypotheses)
    {
        hypothesis.EvidenceIds = MapEvidenceIds(hypothesis, rerankedChunks);
    }
    
    return hypotheses.Hypotheses;
}
```

**收益**：
- ✅ AI输出可追溯
- ✅ 提高假设准确性
- ✅ 符合"AI作为副驾驶"原则

---

### 2. 优先级P0：实现成本监控

**问题**：
- 无法追踪Token消耗
- 无法评估成本
- 无法设置成本告警

**建议方案**：

#### 2.1 创建Token消耗记录表

```sql
CREATE TABLE llm_usage_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_name VARCHAR(50) NOT NULL, -- 'OpenAI', 'Gemini', 'Zhipu'
    model_name VARCHAR(100) NOT NULL,
    request_type VARCHAR(50) NOT NULL, -- 'GenerateText', 'GenerateStructured', 'Chat'
    input_tokens INTEGER NOT NULL,
    output_tokens INTEGER NOT NULL,
    total_tokens INTEGER NOT NULL,
    cost_usd DECIMAL(10, 6),
    ticket_id UUID REFERENCES tickets(ticket_id),
    user_id UUID REFERENCES users(user_id),
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_llm_usage_logs_created_at ON llm_usage_logs(created_at);
CREATE INDEX idx_llm_usage_logs_ticket_id ON llm_usage_logs(ticket_id);
```

#### 2.2 在LLM服务中添加使用量记录

```csharp
public class GoogleGeminiService : ILLMService
{
    private readonly ApplicationDbContext _dbContext;
    
    public async Task<string> GenerateTextAsync(string prompt, LLMRequestOptions? options = null)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var response = await CallGeminiAPI(...);
            
            // 记录使用量
            await LogUsageAsync(
                serviceName: "GoogleGemini",
                model: options?.Model ?? _defaultModel,
                requestType: "GenerateText",
                inputTokens: EstimateTokens(prompt),
                outputTokens: EstimateTokens(response),
                costUsd: CalculateCost(...)
            );
            
            return response;
        }
        catch (Exception ex)
        {
            // 记录失败（不计费但记录）
            await LogUsageAsync(..., failed: true);
            throw;
        }
    }
}
```

#### 2.3 实现成本监控服务

```csharp
public interface ILLMCostMonitoringService
{
    Task<CostSummaryDto> GetDailyCostAsync(DateTime date);
    Task<CostSummaryDto> GetMonthlyCostAsync(int year, int month);
    Task<List<CostAlertDto>> CheckCostAlertsAsync();
    Task<UsageStatisticsDto> GetUsageStatisticsAsync(DateTime startDate, DateTime endDate);
}
```

**收益**：
- ✅ 实时了解AI使用成本
- ✅ 及时发现异常使用
- ✅ 支持成本优化决策

---

### 3. 优先级P1：实现对话历史存储

**问题**：
- 多轮对话历史未持久化
- 无法追溯历史对话
- 无法基于历史对话优化

**建议方案**：

#### 3.1 创建对话历史表

```sql
CREATE TABLE conversation_histories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    conversation_type VARCHAR(50) NOT NULL, -- 'missing_info', 'triage', 'diagnosis'
    messages JSONB NOT NULL, -- 存储对话消息数组
    metadata JSONB, -- 存储额外元数据
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_conversation_histories_ticket_id ON conversation_histories(ticket_id);
CREATE INDEX idx_conversation_histories_type ON conversation_histories(conversation_type);
```

#### 3.2 在对话服务中持久化历史

```csharp
public async Task<ConversationResponse> ContinueConversationAsync(
    Guid ticketId,
    string userAnswer,
    string questionId)
{
    // 1. 获取或创建对话历史
    var history = await GetOrCreateConversationHistoryAsync(ticketId, "missing_info");
    
    // 2. 添加用户回答
    history.Messages.Add(new ChatMessage
    {
        Role = "user",
        Content = userAnswer,
        Timestamp = DateTime.UtcNow
    });
    
    // 3. 调用LLM生成下一个问题
    var llmResponse = await _llmService.ChatAsync(history.Messages, ...);
    
    // 4. 添加AI回复
    history.Messages.Add(new ChatMessage
    {
        Role = "assistant",
        Content = llmResponse,
        Timestamp = DateTime.UtcNow
    });
    
    // 5. 保存历史
    await _dbContext.SaveChangesAsync();
    
    return ParseResponse(llmResponse);
}
```

**收益**：
- ✅ 完整的对话追溯
- ✅ 支持对话质量分析
- ✅ 支持对话优化

---

### 4. 优先级P1：优化Prompt工程

**当前问题**：
- Prompt模板较简单
- 缺少few-shot示例
- 缺少角色定义

**改进建议**：

#### 4.1 添加角色定义和上下文

```csharp
public static string GetHypothesisPrompt(
    TicketDto ticket,
    List<KnowledgeChunk> evidenceChunks)
{
    return $@"你是一位经验丰富的设备故障诊断专家，拥有10年以上的现场问题处理经验。

## 你的职责
1. 基于历史案例和判断卡，生成最可能的故障假设
2. 每个假设必须引用至少1个证据来源
3. 评估每个假设的置信度（1-5分）
4. 不准自由发挥，必须基于证据

## 工单信息
- 问题域：{ticket.Domain}
- 症状：{ticket.SymptomTitle}
- 详细描述：{ticket.SymptomDetail}
- 设备型号：{ticket.DeviceModel}
- 版本信息：SW={ticket.SwVersion}, PLC={ticket.PlcVersion}

## 相关证据（按相关性排序）
{FormatEvidenceChunks(evidenceChunks)}

## 输出要求
1. 生成3个最可能的假设，按可能性从高到低排序
2. 每个假设必须引用至少1个证据来源（证据ID）
3. 每个假设给出置信度（1-5分）
4. 如果证据不足，置信度必须 ≤ 2

## 输出格式（JSON）
{GetHypothesisJsonSchema()}

## 示例
{GetFewShotExamples()}";
}
```

#### 4.2 添加Few-Shot示例

```csharp
private static string GetFewShotExamples()
{
    return @"
示例1：
输入：问题域=A，症状=设备无法启动
证据：[判断卡JC-A-001: 电源故障排查]
输出：
{
  ""hypotheses"": [
    {
      ""rank"": 1,
      ""hypothesis"": ""电源模块故障，导致设备无法启动"",
      ""confidence"": 4,
      ""evidence_ids"": [""jc_JC-A-001""],
      ""reasoning"": ""根据判断卡JC-A-001，电源故障是设备无法启动的常见原因""
    }
  ]
}";
}
```

**收益**：
- ✅ 提高AI输出质量
- ✅ 减少幻觉（hallucination）
- ✅ 提高一致性

---

### 5. 优先级P2：实现质量监控和反馈机制

**问题**：
- 无法评估AI输出质量
- 无法收集用户反馈
- 无法持续优化

**建议方案**：

#### 5.1 创建AI输出质量记录表

```sql
CREATE TABLE ai_output_quality_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    output_type VARCHAR(50) NOT NULL, -- 'hypothesis', 'action', 'question'
    ai_output JSONB NOT NULL,
    user_feedback VARCHAR(20), -- 'accepted', 'rejected', 'modified'
    user_modified_output JSONB,
    confidence_score INTEGER,
    actual_accuracy DECIMAL(3, 2), -- 实际准确率（如果可验证）
    created_at TIMESTAMP DEFAULT NOW()
);
```

#### 5.2 实现质量评估服务

```csharp
public interface IAIOualityMonitoringService
{
    Task RecordAIOutputAsync(AIOutputRecord record);
    Task RecordUserFeedbackAsync(Guid outputId, UserFeedback feedback);
    Task<QualityMetricsDto> GetQualityMetricsAsync(DateTime startDate, DateTime endDate);
    Task<List<QualityIssueDto>> IdentifyQualityIssuesAsync();
}
```

**收益**：
- ✅ 持续改进AI质量
- ✅ 识别问题Prompt
- ✅ 优化模型选择

---

### 6. 优先级P2：优化成本效益

**当前问题**：
- 所有工单都使用相同的AI模型
- 没有根据优先级选择模型
- 缺少缓存机制

**改进建议**：

#### 6.1 实现智能模型选择

```csharp
public class SmartLLMService : ILLMService
{
    public async Task<string> GenerateTextAsync(string prompt, LLMRequestOptions? options = null)
    {
        // 根据工单优先级和复杂度选择模型
        var model = SelectModel(ticketPriority, promptComplexity);
        
        // 低优先级工单使用更便宜的模型
        if (ticketPriority == Priority.Low)
        {
            return await _geminiService.GenerateTextAsync(prompt, 
                new LLMRequestOptions { Model = "gemini-pro" });
        }
        
        // 高优先级工单使用更好的模型
        return await _openAIService.GenerateTextAsync(prompt,
            new LLMRequestOptions { Model = "gpt-4o" });
    }
}
```

#### 6.2 实现结果缓存

```csharp
public class CachedLLMService : ILLMService
{
    private readonly IMemoryCache _cache;
    
    public async Task<string> GenerateTextAsync(string prompt, LLMRequestOptions? options = null)
    {
        // 生成缓存键（基于prompt的hash）
        var cacheKey = GenerateCacheKey(prompt, options);
        
        // 检查缓存
        if (_cache.TryGetValue(cacheKey, out string cachedResult))
        {
            return cachedResult;
        }
        
        // 调用LLM
        var result = await _innerLLMService.GenerateTextAsync(prompt, options);
        
        // 缓存结果（24小时）
        _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
        
        return result;
    }
}
```

**收益**：
- ✅ 降低30-50%成本
- ✅ 提高响应速度
- ✅ 优化资源使用

---

## 📋 实施优先级和时间表

### Phase 1：核心功能（2-3周）

**Week 1-2：RAG系统**
- [ ] 集成PGVector
- [ ] 实现Embedding服务
- [ ] 实现向量检索
- [ ] 完善证据引用机制

**Week 3：成本监控**
- [ ] 创建使用量记录表
- [ ] 实现使用量记录
- [ ] 实现成本监控服务
- [ ] 添加成本告警

### Phase 2：质量提升（2周）

**Week 4-5：对话历史 + Prompt优化**
- [ ] 实现对话历史存储
- [ ] 优化Prompt模板
- [ ] 添加Few-Shot示例
- [ ] 实现质量监控

### Phase 3：成本优化（1周）

**Week 6：成本优化**
- [ ] 实现智能模型选择
- [ ] 实现结果缓存
- [ ] 优化Token使用
- [ ] 成本分析报告

---

## 🎯 预期收益

### 功能收益

| 改进项 | 预期收益 |
|--------|---------|
| RAG系统 | AI输出准确率提升20-30%，可追溯性100% |
| 成本监控 | 成本透明度100%，异常检测及时性提升 |
| 对话历史 | 对话质量分析能力，用户体验提升 |
| Prompt优化 | AI输出质量提升15-25% |
| 质量监控 | 持续改进能力，问题识别率提升 |

### 成本收益

| 优化项 | 预期节省 |
|--------|---------|
| 结果缓存 | 节省30-50%成本 |
| 智能模型选择 | 节省20-30%成本 |
| Token优化 | 节省10-15%成本 |
| **总计** | **节省40-60%成本** |

---

## 🔍 技术债务清理

### 1. 统一LLM服务接口

**当前问题**：
- 不同LLM服务的实现细节不同
- 错误处理不统一
- 配置方式不一致

**建议**：
- 统一错误处理机制
- 统一配置方式
- 添加重试机制

### 2. 完善降级策略

**当前问题**：
- 降级策略较简单
- 缺少多级降级

**建议**：
- 实现多级降级（GPT-4o → Gemini → 规则引擎）
- 添加降级日志
- 监控降级率

### 3. 优化错误处理

**当前问题**：
- 错误信息不够详细
- 缺少错误分类

**建议**：
- 实现错误分类（网络错误、API错误、超时等）
- 添加详细错误日志
- 实现错误告警

---

## 📝 总结

### 核心建议优先级

1. **P0 - 必须实现**：
   - ✅ RAG系统（证据引用）
   - ✅ 成本监控

2. **P1 - 强烈建议**：
   - ✅ 对话历史存储
   - ✅ Prompt优化

3. **P2 - 建议实现**：
   - ✅ 质量监控
   - ✅ 成本优化

### 关键成功因素

1. **数据质量**：确保判断卡和历史工单数据质量
2. **Prompt质量**：持续优化Prompt模板
3. **成本控制**：建立成本监控和告警机制
4. **用户反馈**：建立反馈收集机制
5. **持续改进**：基于数据持续优化

---

**文档版本**：1.0  
**最后更新**：2025-12-29  
**维护人**：开发团队

