# AI引导式工单创建系统 - 实施总结

> **日期**：2025-12-29  
> **状态**：✅ 核心功能已完成  
> **完成度**：90%

---

## ✅ 已完成的工作

### 1. Gemini Vision API 集成 ✅

#### 1.1 扩展 GoogleGeminiService 支持图片分析

**文件**：`backend/src/FieldTicket.Infrastructure/LLM/GoogleGeminiService.cs`

**新增方法**：
- `AnalyzeImageWithTextAsync()` - 分析单张图片（文本+图片）
- `AnalyzeImagesWithTextAsync()` - 分析多张图片

**关键特性**：
- ✅ 支持 base64 图片编码
- ✅ 自动检测并使用 Vision 模型（`gemini-pro-vision`）
- ✅ 支持多种图片格式（JPEG, PNG等）
- ✅ 错误处理和日志记录

**实现细节**：
```csharp
// 支持图片的 GeminiPart
private class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("inlineData")]
    public GeminiInlineData? InlineData { get; set; }
}
```

#### 1.2 完善 MultimodalAIService

**文件**：`backend/src/FieldTicket.Infrastructure/Services/MultimodalAIService.cs`

**改进**：
- ✅ 检测是否使用 Gemini 服务，自动使用 Vision API
- ✅ 支持单张和多张图片分析
- ✅ 降级方案：非 Gemini 服务时使用文本模型

---

### 2. 完整的 GuidedTicketCreationService ✅

#### 2.1 服务接口

**文件**：`backend/src/FieldTicket.Core/Services/IGuidedTicketCreationService.cs`

**核心方法**：
- ✅ `CreateSessionAsync()` - 创建会话
- ✅ `SubmitInitialInfoAsync()` - 提交初始信息
- ✅ `AnswerQuestionAsync()` - 回答引导性问题
- ✅ `GenerateTicketContentAsync()` - 生成工单内容
- ✅ `CreateTicketFromSessionAsync()` - 使用会话创建工单
- ✅ `GetSessionAsync()` - 获取会话状态

#### 2.2 服务实现

**文件**：`backend/src/FieldTicket.Infrastructure/Services/GuidedTicketCreationService.cs`

**核心功能**：

1. **会话管理**
   - ✅ 创建和管理引导式创建会话
   - ✅ 保存对话历史（JSONB）
   - ✅ 状态流转：collecting → analyzing → guiding → completing → completed

2. **多模态AI分析**
   - ✅ 文本分析（专业术语转换、问题域识别）
   - ✅ 图片分析（OCR、视觉理解）
   - ✅ 综合分析（文本+图片）

3. **引导式对话**
   - ✅ 生成引导性问题（针对缺失信息）
   - ✅ 迭代式对话（最多3轮）
   - ✅ 智能判断信息完整性

4. **工单内容生成**
   - ✅ 生成专业的问题描述
   - ✅ 提取事实表
   - ✅ 提取版本信息
   - ✅ 置信度评估

**Prompt工程**：
- ✅ `BuildGuidingQuestionPrompt()` - 引导问题生成Prompt
- ✅ `BuildSummaryPrompt()` - 工单总结生成Prompt
- ✅ 包含完整的上下文信息
- ✅ 明确的输出格式要求（JSON Schema）

---

### 3. 数据库迁移脚本 ✅

#### 3.1 数据库实体

**文件**：`backend/src/FieldTicket.Domain/Entities/GuidedTicketSession.cs`

**实体字段**：
- ✅ 会话基本信息（ID、用户ID、工单ID）
- ✅ 会话状态和轮数控制
- ✅ 初始信息和附件ID列表
- ✅ AI分析结果（JSONB）
- ✅ 对话历史（JSONB）

#### 3.2 数据库配置

**文件**：`backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs`

**配置项**：
- ✅ 表名：`guided_ticket_sessions`
- ✅ 字段类型映射（UUID、JSONB、数组）
- ✅ 索引（用户ID、状态、创建时间）
- ✅ 外键关联（用户、工单）

#### 3.3 迁移脚本

**文件**：`backend/migrations/20251229_create_guided_ticket_sessions.sql`

**SQL脚本包含**：
- ✅ 创建表结构
- ✅ 创建索引
- ✅ 添加注释
- ✅ 外键约束

**表结构**：
```sql
CREATE TABLE guided_ticket_sessions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    ticket_id UUID REFERENCES tickets(ticket_id),
    status VARCHAR(20) NOT NULL DEFAULT 'collecting',
    turn_count INTEGER NOT NULL DEFAULT 0,
    max_turns INTEGER NOT NULL DEFAULT 3,
    initial_text TEXT,
    image_attachment_ids UUID[],
    text_analysis JSONB,
    image_analyses JSONB[],
    comprehensive_analysis JSONB,
    conversation_history JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP
);
```

---

### 4. API 端点 ✅

**文件**：`backend/src/FieldTicket.Api/Endpoints/GuidedTicketCreationEndpoints.cs`

**端点列表**：

1. **POST /api/guided-ticket-creation/sessions**
   - 创建新的引导式工单创建会话

2. **POST /api/guided-ticket-creation/sessions/{sessionId}/initial-info**
   - 提交初始信息（文字+图片）
   - 支持 multipart/form-data

3. **POST /api/guided-ticket-creation/sessions/{sessionId}/answer**
   - 回答引导性问题
   - 支持上传额外图片

4. **POST /api/guided-ticket-creation/sessions/{sessionId}/generate-content**
   - 生成工单内容（AI生成的专业描述）

5. **POST /api/guided-ticket-creation/sessions/{sessionId}/create-ticket**
   - 使用AI生成的内容创建工单草稿
   - 需要提供设备ID

6. **GET /api/guided-ticket-creation/sessions/{sessionId}**
   - 获取会话状态

**安全特性**：
- ✅ JWT 认证
- ✅ 用户权限验证（只能访问自己的会话）
- ✅ 输入验证

---

### 5. 数据模型 ✅

**文件**：`backend/src/FieldTicket.Shared/Models/GuidedTicketCreationModels.cs`

**模型列表**：
- ✅ `GuidedTicketCreationSession` - 会话模型
- ✅ `ConversationTurn` - 对话轮次
- ✅ `GuidedQuestion` - 引导性问题
- ✅ `GuidedQuestionResponse` - 问题响应
- ✅ `ImageAnalysisResult` - 图片分析结果
- ✅ `TextAnalysisResult` - 文本分析结果
- ✅ `ComprehensiveAnalysisResult` - 综合分析结果
- ✅ `TicketSummary` - 工单总结
- ✅ `GuidedTicketContent` - 引导式工单内容

---

## 📋 使用流程

### 完整流程示例

```bash
# 1. 创建会话
POST /api/guided-ticket-creation/sessions
Response: { "sessionId": "xxx", ... }

# 2. 提交初始信息
POST /api/guided-ticket-creation/sessions/{sessionId}/initial-info
Content-Type: multipart/form-data
Body: {
  "textDescription": "机器不动了，报警灯亮了",
  "images": [file1, file2]
}
Response: {
  "isComplete": false,
  "questions": [
    {
      "questionId": "q1",
      "question": "报警灯是什么颜色的？",
      "type": "select",
      "options": ["红色", "黄色", "绿色"],
      "hint": "请查看设备上的报警指示灯",
      "professionalTermExample": "红色报警 → 紧急故障报警"
    }
  ],
  "suggestions": [
    "建议：可以用'设备停止运行，报警指示灯显示红色'来描述"
  ]
}

# 3. 回答问题
POST /api/guided-ticket-creation/sessions/{sessionId}/answer
Body: {
  "questionId": "q1",
  "answer": "红色"
}
Response: {
  "isComplete": false,
  "questions": [...], // 下一个问题
  ...
}

# 4. 生成工单内容
POST /api/guided-ticket-creation/sessions/{sessionId}/generate-content
Response: {
  "summary": {
    "symptomTitle": "设备停止运行，红色报警指示灯亮起",
    "symptomDetail": "设备在运行过程中突然停止，报警指示灯显示红色...",
    "domain": "B",
    "factsJson": { ... },
    "versionInfo": { ... }
  },
  "confidence": { "overall": 4, ... }
}

# 5. 创建工单
POST /api/guided-ticket-creation/sessions/{sessionId}/create-ticket
Body: {
  "deviceId": "xxx"
}
Response: {
  "ticketId": "xxx",
  "ticketNo": "T-2025-001",
  ...
}
```

---

## 🔧 技术实现细节

### 1. Gemini Vision API 调用

```csharp
// 图片转换为base64
var imageBase64 = Convert.ToBase64String(imageBytes);

// 构建请求（文本+图片）
var requestBody = new GeminiGenerateContentRequest
{
    Contents = new[]
    {
        new GeminiContent
        {
            Parts = new[]
            {
                new GeminiPart { Text = prompt },
                new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = "image/jpeg",
                        Data = imageBase64
                    }
                }
            }
        }
    }
};
```

### 2. 引导问题生成逻辑

```csharp
// 基于缺失信息和对话历史生成问题
var prompt = BuildGuidingQuestionPrompt(session, analysis);
var questions = await _llmService.GenerateStructuredAsync<GuidedQuestionResponse>(
    prompt,
    schema: GetGuidingQuestionSchema());
```

### 3. 工单内容生成

```csharp
// 综合所有信息生成专业描述
var summary = await GenerateTicketSummaryAsync(session);
// 返回包含所有字段的 GuidedTicketContent
```

---

## 📊 数据库表结构

### guided_ticket_sessions 表

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| user_id | UUID | 用户ID（外键） |
| ticket_id | UUID | 工单ID（外键，可选） |
| status | VARCHAR(20) | 会话状态 |
| turn_count | INTEGER | 当前轮数 |
| max_turns | INTEGER | 最大轮数 |
| initial_text | TEXT | 初始文字描述 |
| image_attachment_ids | UUID[] | 图片附件ID数组 |
| text_analysis | JSONB | 文本分析结果 |
| image_analyses | JSONB[] | 图片分析结果数组 |
| comprehensive_analysis | JSONB | 综合分析结果 |
| conversation_history | JSONB | 对话历史 |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |
| completed_at | TIMESTAMP | 完成时间 |

**索引**：
- `idx_guided_sessions_user_id` - 用户ID索引
- `idx_guided_sessions_status` - 状态索引
- `idx_guided_sessions_created_at` - 创建时间索引

---

## 🚀 下一步工作

### 待完成功能

1. **图片上传处理** ⏳
   - [ ] 在提交初始信息时上传图片到MinIO
   - [ ] 保存附件ID到会话
   - [ ] 创建工单时关联附件

2. **设备选择集成** ⏳
   - [ ] 在会话中添加设备选择步骤
   - [ ] 或返回AI内容，让用户在前端选择设备

3. **前端组件** ⏳
   - [ ] 初始上传组件
   - [ ] 引导对话组件
   - [ ] 工单预览组件

4. **错误处理优化** ⏳
   - [ ] 更详细的错误信息
   - [ ] 重试机制
   - [ ] 降级策略

5. **性能优化** ⏳
   - [ ] 图片分析缓存
   - [ ] 异步处理
   - [ ] 批量处理

---

## 📝 已知限制

1. **设备ID必需**
   - 当前实现中，创建工单需要设备ID
   - 建议：在会话中添加设备选择步骤，或返回AI内容让用户在前端选择

2. **图片上传**
   - 当前实现中，图片流需要先上传到MinIO才能关联
   - 建议：在提交初始信息时先上传图片，保存附件ID

3. **Gemini Vision API**
   - 当前使用 `gemini-pro-vision` 模型
   - 注意：某些地区可能不支持，需要检查API可用性

---

## ✅ 验收标准

### 功能验收

- [x] Gemini Vision API 集成 ✅
- [x] 多模态AI分析服务 ✅
- [x] 引导式对话服务 ✅
- [x] 工单内容生成 ✅
- [x] 数据库表结构 ✅
- [x] API 端点 ✅
- [ ] 图片上传处理 ⏳
- [ ] 前端组件 ⏳

### 技术验收

- [x] 代码编译通过 ✅
- [x] 无Linter错误 ✅
- [x] 数据库迁移脚本 ✅
- [x] 服务注册 ✅
- [ ] 单元测试 ⏳
- [ ] 集成测试 ⏳

---

## 🎯 总结

本次实施**成功完成了**AI引导式工单创建系统的核心功能：

1. ✅ **Gemini Vision API 集成**：支持图片分析
2. ✅ **完整的引导式服务**：会话管理、对话引导、内容生成
3. ✅ **数据库支持**：完整的表结构和迁移脚本
4. ✅ **API 端点**：6个完整的REST API端点

**核心价值**：
- 🤖 AI帮助工程师更专业地描述问题
- 📸 支持图片分析，自动提取关键信息
- 💬 迭代式引导，确保信息完整
- ✨ 自动生成专业的问题描述

**下一步**：实现图片上传处理和前端组件，即可完整使用！

---

**文档版本**：1.0  
**最后更新**：2025-12-29  
**维护人**：开发团队

