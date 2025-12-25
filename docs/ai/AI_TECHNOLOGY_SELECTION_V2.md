# v2.0 AI技术选型文档

> **版本**：2.0  
> **创建日期**：2025-12-22  
> **最后更新**：2025-12-22  
> **评估对象**：AI辅助判断系统（RAG + LLM）

---

## 📋 目录

1. [概述](#概述)
2. [技术选型](#技术选型)
3. [RAG实现方案](#rag实现方案)
4. [成本评估](#成本评估)
5. [性能指标](#性能指标)
6. [风险与应对](#风险与应对)
7. [实施计划](#实施计划)

---

## 概述

### AI功能需求

根据 `ARCHITECTURE_V2.md`，v2.0 需要实现以下AI功能：

1. **结构化填卡（Triage）**
   - 根据工单信息，自动推荐判断卡
   - 填充判断卡的关键字段（症状、区分点等）

2. **Top-3假设+证据引用（RAG）**
   - 基于历史工单和判断卡，生成Top-3假设
   - 每个假设必须带证据引用（RAG）

3. **下一步动作建议**
   - 基于判断卡和当前假设，建议下一步动作
   - 动作必须是可验证的步骤

4. **缺失信息问题清单（问诊式补全）**
   - 分析工单信息，识别缺失的关键信息
   - 生成问诊式问题清单

5. **客户沟通草稿生成**
   - 基于话术模板和工单信息，生成客户沟通草稿
   - 必须使用专业表达，固定结构

### 技术约束

1. **AI作为"副驾驶"**：输出必须带证据，不准自由发挥
2. **输出必须可追溯**：所有AI输出必须记录证据来源
3. **低置信度自动升级**：置信度 ≤ 2 时自动升级
4. **成本控制**：需要考虑Token消耗和API调用成本

---

## 技术选型

### 1. LLM选型

#### 选项对比

| 选项 | 优点 | 缺点 | 成本 | 推荐度 |
|------|------|------|------|--------|
| **GPT-4 Turbo** | 性能强、中文好、API稳定 | 成本较高 | $0.01/1K input, $0.03/1K output | ⭐⭐⭐⭐ |
| **GPT-4o** | 性能强、成本较低 | 中文能力略弱于GPT-4 | $0.005/1K input, $0.015/1K output | ⭐⭐⭐⭐⭐ |
| **Claude 3.5 Sonnet** | 推理能力强、输出质量高 | API可用性、成本 | $0.003/1K input, $0.015/1K output | ⭐⭐⭐⭐ |
| **本地模型（Qwen2.5）** | 成本低、数据安全 | 性能较弱、需要GPU | 硬件成本 | ⭐⭐⭐ |

#### 推荐方案：GPT-4o（主）+ 本地模型（降级）

**理由**：
1. **GPT-4o** 成本效益比最优，中文能力足够
2. **本地模型** 作为降级方案，保证服务可用性
3. 混合方案：正常情况用GPT-4o，异常情况降级到本地模型

**实施策略**：
- 主服务：GPT-4o API
- 降级服务：本地部署 Qwen2.5-72B（如果有GPU资源）
- 或使用：阿里云通义千问API（成本更低）

### 2. 向量数据库选型

#### 选项对比

| 选项 | 优点 | 缺点 | 成本 | 推荐度 |
|------|------|------|------|--------|
| **PGVector（PostgreSQL扩展）** | 与现有DB集成、免费、SQL查询 | 性能一般、规模受限 | 免费 | ⭐⭐⭐⭐⭐ |
| **Milvus** | 性能强、开源、可扩展 | 需要单独部署、运维复杂 | 免费（自托管） | ⭐⭐⭐ |
| **Pinecone** | 托管服务、易用 | 成本较高、数据出境 | $70/月起 | ⭐⭐ |
| **Weaviate** | 开源、功能丰富 | 需要单独部署 | 免费（自托管） | ⭐⭐⭐ |

#### 推荐方案：PGVector（PostgreSQL扩展）

**理由**：
1. **与现有数据库集成**：无需单独部署，降低运维成本
2. **免费开源**：无额外成本
3. **SQL查询**：可以利用现有SQL技能
4. **数据规模**：预计判断卡库规模 < 10万条，PGVector足够

**实施策略**：
- 使用 `pgvector` 扩展（PostgreSQL 14+）
- 向量维度：1536（OpenAI embedding）或 1024（其他模型）
- 索引类型：HNSW（高性能近似最近邻搜索）

### 3. Embedding模型选型

#### 选项对比

| 选项 | 维度 | 成本 | 中文能力 | 推荐度 |
|------|------|------|----------|--------|
| **text-embedding-3-small** | 1536 | $0.02/1M tokens | 好 | ⭐⭐⭐⭐⭐ |
| **text-embedding-3-large** | 3072 | $0.13/1M tokens | 很好 | ⭐⭐⭐ |
| **text-embedding-ada-002** | 1536 | $0.10/1M tokens | 一般 | ⭐⭐ |
| **本地模型（BGE-M3）** | 1024 | 免费 | 很好 | ⭐⭐⭐⭐ |

#### 推荐方案：text-embedding-3-small（主）+ BGE-M3（降级）

**理由**：
1. **text-embedding-3-small** 成本低，性能足够
2. **BGE-M3** 作为降级方案，保证服务可用性
3. 混合方案：正常情况用OpenAI，异常情况降级到本地模型

---

## RAG实现方案

### 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                      RAG系统架构                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐      ┌──────────────┐                   │
│  │  工单输入    │──────│  Embedding    │                   │
│  │  (文本)      │      │  生成         │                   │
│  └──────────────┘      └──────┬───────┘                   │
│                               │                            │
│                               ▼                            │
│  ┌──────────────┐      ┌──────────────┐                   │
│  │  向量检索    │◄──────│  PGVector     │                   │
│  │  (Top-K)     │      │  向量库       │                   │
│  └──────┬───────┘      └──────────────┘                   │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐      ┌──────────────┐                   │
│  │  重排序      │──────│  证据文档     │                   │
│  │  (Rerank)    │      │  (判断卡/工单)│                   │
│  └──────┬───────┘      └──────────────┘                   │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐      ┌──────────────┐                   │
│  │  LLM生成     │──────│  结构化输出   │                   │
│  │  (GPT-4o)    │      │  + 证据引用   │                   │
│  └──────────────┘      └──────────────┘                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 1. 数据准备

#### 向量化数据源

1. **判断卡库**
   - 症状描述
   - 区分点
   - 排查路径
   - 排除项

2. **历史工单**
   - 症状描述
   - 根因分析
   - 解决方案

3. **知识库项**
   - 判断型知识库（内部）
   - 结论型知识库（客户可见）

#### 数据预处理

```python
# 伪代码示例
def prepare_rag_documents():
    """
    准备RAG文档
    """
    documents = []
    
    # 1. 判断卡文档
    for jc in judgement_cards:
        doc = {
            "id": f"jc_{jc.jc_code}",
            "type": "judgement_card",
            "content": f"""
            判断卡：{jc.title}
            症状：{jc.symptoms}
            区分点：{jc.differentiation_points}
            排查路径：{jc.investigation_path}
            """,
            "metadata": {
                "jc_code": jc.jc_code,
                "domain": jc.domain,
                "usage_count": jc.usage_count,
                "success_rate": jc.success_rate
            }
        }
        documents.append(doc)
    
    # 2. 历史工单文档
    for ticket in closed_tickets:
        if ticket.root_cause and ticket.current_hypothesis:
            doc = {
                "id": f"ticket_{ticket.ticket_id}",
                "type": "ticket",
                "content": f"""
                工单：{ticket.symptom_title}
                症状：{ticket.symptom_detail}
                假设：{ticket.current_hypothesis}
                根因：{ticket.root_cause}
                解决方案：{ticket.solution}
                """,
                "metadata": {
                    "ticket_id": ticket.ticket_id,
                    "domain": ticket.domain,
                    "device_model": ticket.device.model
                }
            }
            documents.append(doc)
    
    return documents
```

### 2. 向量检索策略

#### 检索流程

```python
# 伪代码示例
async def retrieve_evidence(ticket: Ticket, top_k: int = 10):
    """
    检索相关证据
    """
    # 1. 生成查询向量
    query_text = f"""
    问题域：{ticket.domain}
    症状：{ticket.symptom_title}
    详细描述：{ticket.symptom_detail}
    设备型号：{ticket.device.model}
    """
    
    query_embedding = await generate_embedding(query_text)
    
    # 2. 向量检索（PGVector）
    similar_docs = await vector_db.similarity_search(
        query_embedding,
        top_k=top_k,
        filter={
            "domain": ticket.domain,  # 过滤：同问题域
            "type": ["judgement_card", "ticket"]  # 只检索判断卡和历史工单
        }
    )
    
    # 3. 重排序（可选，使用交叉编码器）
    reranked_docs = await rerank(query_text, similar_docs, top_k=5)
    
    return reranked_docs
```

#### 检索优化策略

1. **混合检索**：
   - 向量检索（语义相似度）
   - 关键词检索（精确匹配）
   - 组合排序

2. **过滤策略**：
   - 问题域过滤（domain）
   - 设备型号过滤（device_model）
   - 时间过滤（最近N天的工单优先）

3. **重排序**：
   - 使用交叉编码器（Cross-Encoder）重排序
   - 考虑判断卡使用统计（usage_count, success_rate）

### 3. Prompt设计

#### Top-3假设生成Prompt

```python
TOP3_HYPOTHESIS_PROMPT = """
你是一位经验丰富的设备故障诊断专家。根据以下工单信息和历史案例，生成Top-3假设。

## 工单信息
- 问题域：{domain}
- 症状：{symptom_title}
- 详细描述：{symptom_detail}
- 设备型号：{device_model}
- 版本信息：SW={sw_version}, PLC={plc_version}, Param={param_version}

## 相关证据（按相关性排序）
{evidence_documents}

## 要求
1. 生成3个最可能的假设，按可能性从高到低排序
2. 每个假设必须引用至少1个证据来源（证据ID）
3. 每个假设给出置信度（1-5分）
4. 输出格式为JSON：
```json
{
  "hypotheses": [
    {
      "rank": 1,
      "hypothesis": "假设描述",
      "confidence": 4,
      "evidence_ids": ["jc_JC-A-001", "ticket_xxx"],
      "reasoning": "推理过程"
    },
    ...
  ]
}
```

## 约束
- 不准自由发挥，必须基于证据
- 如果证据不足，置信度必须 ≤ 2
- 每个假设必须可验证（有明确的验证步骤）
"""
```

#### 下一步动作建议Prompt

```python
NEXT_ACTION_PROMPT = """
根据当前假设和判断卡，建议下一步动作。

## 当前假设
{current_hypothesis}

## 判断卡信息
{judgement_card_info}

## 要求
1. 建议1-3个下一步动作
2. 每个动作必须是可验证的步骤
3. 动作按优先级排序
4. 输出格式为JSON：
```json
{
  "actions": [
    {
      "priority": 1,
      "action": "动作描述",
      "verification": "如何验证",
      "expected_result": "预期结果"
    },
    ...
  ]
}
```
"""
```

#### 缺失信息清单生成Prompt

```python
MISSING_INFO_PROMPT = """
分析工单信息，识别缺失的关键信息，生成问诊式问题清单。

## 工单信息
{ticket_info}

## 判断卡要求的信息
{judgement_card_required_info}

## 要求
1. 识别缺失的关键信息
2. 生成问诊式问题（引导式填写）
3. 每个问题标注是否必填
4. 输出格式为JSON：
```json
{
  "questions": [
    {
      "question_id": "q1",
      "question": "问题文本",
      "type": "yes_no|text|number|file",
      "required": true,
      "hint": "提示信息"
    },
    ...
  ]
}
```
"""
```

### 4. 置信度计算

#### 置信度算法

```python
def calculate_confidence(hypothesis: dict, evidence_docs: list) -> int:
    """
    计算假设的置信度（1-5分）
    """
    score = 0
    
    # 1. 证据数量（0-2分）
    evidence_count = len(hypothesis["evidence_ids"])
    if evidence_count >= 3:
        score += 2
    elif evidence_count >= 2:
        score += 1
    
    # 2. 证据相关性（0-2分）
    avg_relevance = sum([doc["relevance_score"] for doc in evidence_docs]) / len(evidence_docs)
    if avg_relevance >= 0.8:
        score += 2
    elif avg_relevance >= 0.6:
        score += 1
    
    # 3. 判断卡成功率（0-1分）
    jc_success_rate = hypothesis.get("jc_success_rate", 0)
    if jc_success_rate >= 0.8:
        score += 1
    
    # 确保范围在1-5
    return min(max(score, 1), 5)
```

---

## 成本评估

### 成本估算（月度）

#### 假设条件

- 工单数量：1000单/月
- 平均工单长度：500 tokens（输入）
- 平均AI输出：300 tokens（输出）
- 向量检索：每次检索10个文档

#### 成本明细

| 项目 | 单价 | 月用量 | 月成本 | 说明 |
|------|------|--------|--------|------|
| **GPT-4o API** | | | | |
| - Input | $0.005/1K | 500K tokens | $2.50 | 工单分析 |
| - Output | $0.015/1K | 300K tokens | $4.50 | 假设生成 |
| **Embedding** | | | | |
| - text-embedding-3-small | $0.02/1M | 100K tokens | $0.002 | 向量化 |
| **PGVector** | 免费 | - | $0 | 向量数据库 |
| **总计** | | | **$7.00** | 约50元/月 |

#### 成本优化策略

1. **缓存策略**：
   - 相同工单症状的AI结果缓存24小时
   - 预计节省30%成本

2. **批量处理**：
   - 非实时场景批量调用API
   - 预计节省10%成本

3. **降级方案**：
   - 低优先级工单使用本地模型
   - 预计节省20%成本

4. **优化后成本**：约 **$4.50/月**（约32元/月）

### 成本随规模增长

| 工单数/月 | 月成本（优化前） | 月成本（优化后） |
|-----------|------------------|------------------|
| 1,000 | $7.00 | $4.50 |
| 5,000 | $35.00 | $22.50 |
| 10,000 | $70.00 | $45.00 |
| 50,000 | $350.00 | $225.00 |

---

## 性能指标

### 响应时间要求

| 功能 | 目标响应时间 | 最大响应时间 |
|------|--------------|--------------|
| 向量检索 | < 100ms | < 500ms |
| AI生成（Top-3假设） | < 3s | < 10s |
| AI生成（下一步动作） | < 2s | < 5s |
| 缺失信息清单生成 | < 2s | < 5s |

### 准确率要求

| 指标 | 目标值 | 说明 |
|------|--------|------|
| 判断卡推荐准确率 | ≥ 70% | Top-3中包含正确判断卡 |
| AI假设准确率 | ≥ 60% | Top-1假设被采纳 |
| 证据引用准确率 | ≥ 80% | 引用的证据确实相关 |

### 可用性要求

| 指标 | 目标值 | 说明 |
|------|--------|------|
| API可用性 | ≥ 99.5% | 月度可用时间 |
| 降级成功率 | ≥ 95% | AI服务不可用时的降级成功率 |

---

## 风险与应对

### 风险1：AI输出质量不稳定

**风险描述**：
- AI可能生成不准确或无关的假设
- 证据引用可能错误

**应对措施**：
1. **人工审核机制**：
   - 低置信度（≤2）自动升级，人工审核
   - 高置信度（≥4）也需要人工确认

2. **质量监控**：
   - 记录AI输出采纳率
   - 定期评估AI输出质量

3. **Prompt优化**：
   - 持续优化Prompt，提高输出质量
   - A/B测试不同Prompt版本

### 风险2：API服务不可用

**风险描述**：
- OpenAI API可能不可用
- 网络问题导致调用失败

**应对措施**：
1. **降级方案**：
   - 自动降级到本地模型（BGE-M3 + Qwen2.5）
   - 或降级到阿里云通义千问API

2. **重试机制**：
   - 失败自动重试3次
   - 指数退避策略

3. **缓存策略**：
   - 缓存常见问题的AI结果
   - 服务不可用时使用缓存

### 风险3：成本超支

**风险描述**：
- Token消耗超出预算
- API调用频率过高

**应对措施**：
1. **成本监控**：
   - 实时监控Token消耗
   - 设置成本告警阈值

2. **限流策略**：
   - 限制API调用频率
   - 低优先级工单延迟处理

3. **优化策略**：
   - 使用更便宜的模型（GPT-4o而非GPT-4）
   - 优化Prompt长度

### 风险4：数据隐私和安全

**风险描述**：
- 工单数据可能泄露
- 向量数据库可能被攻击

**应对措施**：
1. **数据脱敏**：
   - 敏感信息脱敏后再向量化
   - 客户信息不进入向量库

2. **访问控制**：
   - 向量数据库访问控制
   - API密钥安全管理

3. **合规性**：
   - 遵守数据保护法规
   - 考虑使用本地模型（数据不出域）

---

## 实施计划

### Phase 1：基础设施搭建（1周）

**任务**：
1. 安装PGVector扩展
2. 搭建Embedding服务
3. 搭建LLM API调用服务
4. 实现基础RAG流程

**交付物**：
- PGVector数据库配置完成
- Embedding服务可用
- LLM API调用服务可用
- 基础RAG流程POC

### Phase 2：数据准备（1周）

**任务**：
1. 准备判断卡向量化数据
2. 准备历史工单向量化数据
3. 批量生成向量并入库
4. 验证向量检索效果

**交付物**：
- 判断卡向量库完成
- 历史工单向量库完成
- 向量检索功能可用

### Phase 3：AI功能开发（2周）

**任务**：
1. 实现Top-3假设生成
2. 实现下一步动作建议
3. 实现缺失信息清单生成
4. 实现客户沟通草稿生成
5. 实现置信度计算

**交付物**：
- Top-3假设生成功能
- 下一步动作建议功能
- 缺失信息清单生成功能
- 客户沟通草稿生成功能

### Phase 4：优化与测试（1周）

**任务**：
1. Prompt优化
2. 性能优化
3. 成本优化
4. 集成测试
5. 压力测试

**交付物**：
- 优化后的AI功能
- 测试报告
- 性能报告
- 成本报告

---

## 技术栈总结

| 组件 | 选型 | 版本/配置 |
|------|------|-----------|
| **LLM** | GPT-4o | OpenAI API |
| **降级LLM** | Qwen2.5-72B | 本地部署（可选） |
| **Embedding** | text-embedding-3-small | OpenAI API |
| **降级Embedding** | BGE-M3 | 本地部署（可选） |
| **向量数据库** | PGVector | PostgreSQL 14+ |
| **向量维度** | 1536 | OpenAI embedding |
| **索引类型** | HNSW | 高性能近似最近邻 |
| **编程语言** | C# (.NET 8) | 后端 |
| **Python库** | - | Embedding服务（可选） |

---

**文档版本**：1.0  
**最后更新**：2025-12-22  
**维护人**：开发团队


