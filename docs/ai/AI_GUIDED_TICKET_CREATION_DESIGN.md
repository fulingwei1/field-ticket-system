# AI引导式工单创建系统设计

> **日期**：2025-12-29  
> **目标**：帮助学历较低的客服工程师更专业地描述现场技术问题  
> **核心功能**：多模态AI分析 + 迭代式引导 + 自动填充工单

---

## 📋 需求分析

### 用户痛点

1. **描述不专业**：工程师学历较低，技术术语使用不当
2. **信息不完整**：不知道需要提供哪些关键信息
3. **填写困难**：工单表单复杂，不知道如何填写
4. **效率低下**：需要反复沟通才能获得完整信息

### 解决方案

**AI引导式工单创建流程**：
1. 工程师上传文字描述和图片
2. AI分析并生成引导性问题
3. 工程师根据问题补充信息
4. 迭代2-3轮，逐步完善
5. AI生成专业的问题总结
6. 自动填充到工单表单

---

## 🎯 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│               AI引导式工单创建流程                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  步骤1: 初始上传                                            │
│  ┌──────────────┐      ┌──────────────┐                   │
│  │ 文字描述     │      │ 图片/视频    │                   │
│  │ (自由描述)   │      │ (现场照片)   │                   │
│  └──────┬───────┘      └──────┬───────┘                   │
│         │                     │                            │
│         └─────────┬───────────┘                            │
│                   ▼                                        │
│         ┌─────────────────────┐                            │
│         │  多模态AI分析       │                            │
│         │  - OCR文字识别      │                            │
│         │  - 图片内容理解     │                            │
│         │  - 文本语义分析     │                            │
│         └─────────┬───────────┘                            │
│                   ▼                                        │
│  步骤2: 生成引导性问题                                      │
│  ┌─────────────────────────────────────┐                   │
│  │  AI生成引导性问题                   │                   │
│  │  - 问题域识别（A/B/C/D/E）          │                   │
│  │  - 关键信息缺失识别                 │                   │
│  │  - 专业术语建议                     │                   │                   │
│  └─────────┬───────────────────────────┘                   │
│            ▼                                               │
│  步骤3: 工程师回答                                          │
│  ┌─────────────────────────────────────┐                   │
│  │  工程师根据问题补充信息             │                   │
│  │  - 回答引导性问题                   │                   │
│  │  - 上传补充图片                     │                   │
│  └─────────┬───────────────────────────┘                   │
│            ▼                                               │
│  步骤4: 迭代完善（2-3轮）                                   │
│  ┌─────────────────────────────────────┐                   │
│  │  AI继续分析，生成更深入的问题       │                   │
│  │  或确认信息完整性                   │                   │
│  └─────────┬───────────────────────────┘                   │
│            ▼                                               │
│  步骤5: 生成专业总结                                        │
│  ┌─────────────────────────────────────┐                   │
│  │  AI生成专业的问题描述               │                   │
│  │  - 标准化的问题标题                 │                   │
│  │  - 结构化的详细描述                 │                   │
│  │  - 提取的关键信息                   │                   │
│  └─────────┬───────────────────────────┘                   │
│            ▼                                               │
│  步骤6: 自动填充工单                                        │
│  ┌─────────────────────────────────────┐                   │
│  │  自动填充工单表单                   │                   │
│  │  - 问题域、步骤                     │                   │
│  │  - 问题描述                         │                   │
│  │  - 事实表                           │                   │
│  │  - 版本信息（如可识别）             │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔧 技术实现方案

### 1. 多模态AI分析服务

#### 1.1 图片分析（OCR + 视觉理解）

**使用Google Gemini Vision API**（支持多模态）：

```csharp
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
    /// 综合文本和图片分析
    /// </summary>
    Task<ComprehensiveAnalysisResult> AnalyzeComprehensiveAsync(
        string textDescription,
        List<Stream>? images = null);
}
```

**实现示例**：

```csharp
public class MultimodalAIService : IMultimodalAIService
{
    private readonly ILLMService _llmService;
    private readonly ILogger<MultimodalAIService> _logger;
    
    public async Task<ImageAnalysisResult> AnalyzeImageAsync(
        Stream imageStream, 
        string? contextText = null)
    {
        // 使用Gemini Vision API分析图片
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

        // 将图片转换为base64
        var imageBase64 = await ConvertToBase64Async(imageStream);
        
        // 调用Gemini Vision API
        var result = await _llmService.GenerateStructuredAsync<ImageAnalysisResult>(
            prompt,
            schema: GetImageAnalysisSchema(),
            options: new LLMRequestOptions 
            { 
                Model = "gemini-pro-vision" // 使用支持视觉的模型
            });
        
        return result;
    }
}
```

#### 1.2 文本分析（语义理解 + 专业术语建议）

```csharp
public async Task<TextAnalysisResult> AnalyzeTextAsync(string text)
{
    var prompt = $@"
你是一位经验丰富的设备故障诊断专家。请分析以下现场工程师的描述，并给出专业建议。

## 原始描述
{text}

## 请完成以下任务：

1. **问题域识别**：判断问题属于哪个域（A-机械/动作、B-电气/IO、C-PLC/程序流程、D-通信、E-系统/偶发/环境）

2. **专业术语转换**：将口语化描述转换为专业术语
   - 例如："机器不动了" → "设备停止运行，无动作输出"
   - 例如："报警了" → "系统报警，报警代码：XXX"

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

    return await _llmService.GenerateStructuredAsync<TextAnalysisResult>(
        prompt,
        schema: GetTextAnalysisSchema());
}
```

### 2. 引导式对话服务

#### 2.1 对话状态管理

```csharp
public class GuidedTicketCreationSession
{
    public Guid SessionId { get; set; }
    public Guid? TicketId { get; set; } // 如果已创建草稿
    public Guid UserId { get; set; }
    
    // 收集的信息
    public string? InitialText { get; set; }
    public List<Guid> ImageAttachmentIds { get; set; } = new();
    public List<ConversationTurn> ConversationHistory { get; set; } = new();
    
    // AI分析结果
    public TextAnalysisResult? TextAnalysis { get; set; }
    public List<ImageAnalysisResult> ImageAnalyses { get; set; } = new();
    public ComprehensiveAnalysisResult? ComprehensiveAnalysis { get; set; }
    
    // 当前状态
    public SessionStatus Status { get; set; } // Collecting, Analyzing, Guiding, Completing
    public int TurnCount { get; set; }
    public int MaxTurns { get; set; } = 3;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ConversationTurn
{
    public int TurnNumber { get; set; }
    public string Role { get; set; } // "user" | "assistant"
    public string Content { get; set; }
    public List<Guid>? AttachmentIds { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### 2.2 引导问题生成

```csharp
public interface IGuidedTicketCreationService
{
    /// <summary>
    /// 创建新的引导式工单创建会话
    /// </summary>
    Task<GuidedTicketCreationSession> CreateSessionAsync(Guid userId);
    
    /// <summary>
    /// 提交初始信息（文字+图片）
    /// </summary>
    Task<GuidedQuestionResponse> SubmitInitialInfoAsync(
        Guid sessionId,
        string textDescription,
        List<Stream>? images = null);
    
    /// <summary>
    /// 回答引导性问题
    /// </summary>
    Task<GuidedQuestionResponse> AnswerQuestionAsync(
        Guid sessionId,
        string questionId,
        string answer,
        List<Stream>? additionalImages = null);
    
    /// <summary>
    /// 生成工单草稿
    /// </summary>
    Task<TicketDto> GenerateTicketDraftAsync(Guid sessionId);
    
    /// <summary>
    /// 获取会话状态
    /// </summary>
    Task<GuidedTicketCreationSession> GetSessionAsync(Guid sessionId);
}
```

**引导问题生成Prompt**：

```csharp
private string BuildGuidingQuestionPrompt(
    GuidedTicketCreationSession session,
    ComprehensiveAnalysisResult analysis)
{
    return $@"
你是一位耐心的技术指导专家，正在帮助一位现场工程师描述设备问题。

## 当前收集的信息

**文字描述**：
{session.InitialText}

**图片分析结果**：
{FormatImageAnalyses(session.ImageAnalyses)}

**已识别信息**：
- 问题域：{analysis.Domain}
- 关键信息：{string.Join(", ", analysis.KeyInformation)}

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
- 生成确认性问题，确认信息准确性
- 或直接进入总结阶段

返回JSON格式：
{{
  ""is_complete"": false,
  ""questions"": [
    {{
      ""question_id"": ""q1"",
      ""question"": ""问题文本"",
      ""type"": ""yes_no|text|number|select|file"",
      ""options"": [""选项1"", ""选项2""], // 如果是select类型
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
```

### 3. 工单自动填充

#### 3.1 生成专业总结

```csharp
private string BuildSummaryPrompt(
    GuidedTicketCreationSession session,
    ComprehensiveAnalysisResult analysis)
{
    return $@"
你是一位专业的技术文档编写专家。请根据以下信息，生成一份专业的工单问题描述。

## 收集的完整信息

**原始描述**：
{session.InitialText}

**对话补充信息**：
{FormatConversationHistory(session.ConversationHistory)}

**图片分析结果**：
{FormatImageAnalyses(session.ImageAnalyses)}

**已识别信息**：
- 问题域：{analysis.Domain}
- 设备信息：{analysis.DeviceInfo}
- 关键信息：{string.Join(", ", analysis.KeyInformation)}

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
```

#### 3.2 自动填充工单

```csharp
public async Task<TicketDto> GenerateTicketDraftAsync(Guid sessionId)
{
    var session = await GetSessionAsync(sessionId);
    
    // 生成专业总结
    var summaryPrompt = BuildSummaryPrompt(session, session.ComprehensiveAnalysis!);
    var summary = await _llmService.GenerateStructuredAsync<TicketSummary>(
        summaryPrompt,
        schema: GetTicketSummarySchema());
    
    // 创建工单草稿
    var createRequest = new CreateTicketRequest
    {
        Domain = summary.Domain[0], // 取第一个字符
        StepCode = summary.StepCode ?? "",
        StepName = summary.StepName,
        SymptomTitle = summary.SymptomTitle,
        SymptomDetail = summary.SymptomDetail,
        FactsJson = summary.FactsJson,
        SwVersion = summary.VersionInfo?.SwVersion ?? "",
        PlcVersion = summary.VersionInfo?.PlcVersion ?? "",
        ParamVersion = summary.VersionInfo?.ParamVersion ?? "",
        Status = "Draft"
    };
    
    // 创建工单
    var ticket = await _ticketService.CreateDraftAsync(createRequest, session.UserId);
    
    // 关联附件
    foreach (var attachmentId in session.ImageAttachmentIds)
    {
        await _attachmentService.AssociateWithTicketAsync(attachmentId, ticket.TicketId);
    }
    
    // 保存会话关联
    session.TicketId = ticket.TicketId;
    session.Status = SessionStatus.Completed;
    await SaveSessionAsync(session);
    
    return ticket;
}
```

---

## 📊 数据库设计

### 1. 引导式工单创建会话表

```sql
CREATE TABLE guided_ticket_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id),
    ticket_id UUID REFERENCES tickets(ticket_id),
    
    -- 会话状态
    status VARCHAR(20) NOT NULL, -- 'collecting', 'analyzing', 'guiding', 'completing', 'completed'
    turn_count INTEGER DEFAULT 0,
    max_turns INTEGER DEFAULT 3,
    
    -- 初始信息
    initial_text TEXT,
    image_attachment_ids UUID[],
    
    -- AI分析结果（JSONB）
    text_analysis JSONB,
    image_analyses JSONB[],
    comprehensive_analysis JSONB,
    
    -- 对话历史（JSONB）
    conversation_history JSONB NOT NULL DEFAULT '[]'::jsonb,
    
    -- 元数据
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    completed_at TIMESTAMP
);

CREATE INDEX idx_guided_sessions_user_id ON guided_ticket_sessions(user_id);
CREATE INDEX idx_guided_sessions_status ON guided_ticket_sessions(status);
CREATE INDEX idx_guided_sessions_created_at ON guided_ticket_sessions(created_at);
```

### 2. 对话历史结构（JSONB）

```json
{
  "turns": [
    {
      "turn_number": 1,
      "role": "user",
      "content": "机器不动了，报警灯亮了",
      "attachment_ids": ["uuid1", "uuid2"],
      "timestamp": "2025-12-29T10:00:00Z"
    },
    {
      "turn_number": 2,
      "role": "assistant",
      "content": "请告诉我报警灯是什么颜色的？",
      "questions": [
        {
          "question_id": "q1",
          "question": "报警灯是什么颜色的？",
          "type": "select",
          "options": ["红色", "黄色", "绿色"]
        }
      ],
      "timestamp": "2025-12-29T10:00:05Z"
    }
  ]
}
```

---

## 🎨 前端实现

### 1. 引导式工单创建组件

```tsx
// GuidedTicketCreation.tsx
const GuidedTicketCreation: React.FC = () => {
  const [session, setSession] = useState<GuidedSession | null>(null);
  const [currentStep, setCurrentStep] = useState<'upload' | 'guiding' | 'review'>('upload');
  const [loading, setLoading] = useState(false);
  
  // 步骤1: 初始上传
  const handleInitialSubmit = async (text: string, images: File[]) => {
    setLoading(true);
    try {
      const response = await guidedTicketService.submitInitialInfo(
        session!.sessionId,
        text,
        images
      );
      
      setSession(response.session);
      setCurrentStep('guiding');
    } finally {
      setLoading(false);
    }
  };
  
  // 步骤2-4: 回答引导性问题
  const handleAnswerQuestion = async (questionId: string, answer: string) => {
    setLoading(true);
    try {
      const response = await guidedTicketService.answerQuestion(
        session!.sessionId,
        questionId,
        answer
      );
      
      setSession(response.session);
      
      // 如果信息已完整，进入审核阶段
      if (response.isComplete) {
        setCurrentStep('review');
      }
    } finally {
      setLoading(false);
    }
  };
  
  // 步骤5-6: 生成并填充工单
  const handleGenerateTicket = async () => {
    setLoading(true);
    try {
      const ticket = await guidedTicketService.generateTicketDraft(
        session!.sessionId
      );
      
      // 跳转到工单编辑页面，显示AI生成的内容
      navigate(`/tickets/create?sessionId=${session!.sessionId}&ticketId=${ticket.ticketId}`);
    } finally {
      setLoading(false);
    }
  };
  
  return (
    <div className="guided-ticket-creation">
      {currentStep === 'upload' && (
        <InitialUploadStep 
          onSubmit={handleInitialSubmit}
          loading={loading}
        />
      )}
      
      {currentStep === 'guiding' && (
        <GuidingStep 
          session={session}
          onAnswer={handleAnswerQuestion}
          loading={loading}
        />
      )}
      
      {currentStep === 'review' && (
        <ReviewStep 
          session={session}
          onGenerate={handleGenerateTicket}
          loading={loading}
        />
      )}
    </div>
  );
};
```

### 2. 初始上传步骤组件

```tsx
const InitialUploadStep: React.FC<{
  onSubmit: (text: string, images: File[]) => Promise<void>;
  loading: boolean;
}> = ({ onSubmit, loading }) => {
  const [text, setText] = useState('');
  const [images, setImages] = useState<File[]>([]);
  
  return (
    <Card title="第一步：描述问题">
      <Form layout="vertical">
        <Form.Item label="问题描述（用你自己的话描述）">
          <TextArea
            rows={6}
            placeholder="例如：机器不动了，报警灯亮了..."
            value={text}
            onChange={(e) => setText(e.target.value)}
          />
          <div style={{ marginTop: 8, color: '#666', fontSize: 12 }}>
            💡 提示：不用担心用词不专业，AI会帮你转换成专业术语
          </div>
        </Form.Item>
        
        <Form.Item label="上传现场图片（可选）">
          <Upload
            multiple
            beforeUpload={(file) => {
              setImages([...images, file]);
              return false;
            }}
          >
            <Button icon={<UploadOutlined />}>选择图片</Button>
          </Upload>
          <Image.PreviewGroup>
            {images.map((img, idx) => (
              <Image
                key={idx}
                src={URL.createObjectURL(img)}
                width={100}
                style={{ marginTop: 8, marginRight: 8 }}
              />
            ))}
          </Image.PreviewGroup>
        </Form.Item>
        
        <Button
          type="primary"
          onClick={() => onSubmit(text, images)}
          loading={loading}
          disabled={!text.trim()}
        >
          开始AI分析
        </Button>
      </Form>
    </Card>
  );
};
```

### 3. 引导步骤组件

```tsx
const GuidingStep: React.FC<{
  session: GuidedSession;
  onAnswer: (questionId: string, answer: string) => Promise<void>;
  loading: boolean;
}> = ({ session, onAnswer, loading }) => {
  const currentTurn = session.conversationHistory[session.conversationHistory.length - 1];
  const questions = currentTurn.questions || [];
  
  return (
    <Card title="AI正在帮您完善问题描述">
      {/* 显示AI分析结果 */}
      <Alert
        message="AI分析结果"
        description={
          <div>
            <p><strong>问题域：</strong>{session.comprehensiveAnalysis?.domain}</p>
            <p><strong>专业术语建议：</strong></p>
            <ul>
              {session.comprehensiveAnalysis?.terminologySuggestions?.map((s, i) => (
                <li key={i}>
                  "{s.original}" → <strong>"{s.professional}"</strong>
                  <span style={{ color: '#666' }}>（{s.explanation}）</span>
                </li>
              ))}
            </ul>
          </div>
        }
        type="info"
        style={{ marginBottom: 24 }}
      />
      
      {/* 引导性问题 */}
      <div>
        <h3>请回答以下问题：</h3>
        {questions.map((q) => (
          <Card key={q.questionId} style={{ marginBottom: 16 }}>
            <div style={{ marginBottom: 8 }}>
              <strong>{q.question}</strong>
              {q.hint && (
                <div style={{ color: '#666', fontSize: 12, marginTop: 4 }}>
                  💡 {q.hint}
                </div>
              )}
              {q.professionalTermExample && (
                <div style={{ color: '#1890ff', fontSize: 12, marginTop: 4 }}>
                  ✨ 专业术语示例：{q.professionalTermExample}
                </div>
              )}
            </div>
            
            {q.type === 'yes_no' && (
              <Radio.Group onChange={(e) => onAnswer(q.questionId, e.target.value)}>
                <Radio value="YES">是</Radio>
                <Radio value="NO">否</Radio>
              </Radio.Group>
            )}
            
            {q.type === 'text' && (
              <Input
                placeholder="请输入..."
                onPressEnter={(e) => onAnswer(q.questionId, e.target.value)}
              />
            )}
            
            {q.type === 'select' && (
              <Select
                placeholder="请选择..."
                onChange={(value) => onAnswer(q.questionId, value)}
              >
                {q.options?.map((opt) => (
                  <Option key={opt} value={opt}>{opt}</Option>
                ))}
              </Select>
            )}
          </Card>
        ))}
      </div>
      
      {/* 进度提示 */}
      <div style={{ marginTop: 24, textAlign: 'center', color: '#666' }}>
        第 {session.turnCount} / {session.maxTurns} 轮对话
      </div>
    </Card>
  );
};
```

---

## 🚀 实施计划

### Phase 1：核心功能（2周）

**Week 1**：
- [ ] 实现多模态AI分析服务（图片+文本）
- [ ] 实现引导式对话服务
- [ ] 创建数据库表结构

**Week 2**：
- [ ] 实现工单自动填充
- [ ] 前端引导式创建组件
- [ ] 集成测试

### Phase 2：优化（1周）

**Week 3**：
- [ ] Prompt优化
- [ ] 用户体验优化
- [ ] 性能优化

---

## 📈 预期效果

### 功能收益

| 指标 | 改进前 | 改进后 | 提升 |
|------|--------|--------|------|
| 工单描述专业性 | 30% | 85% | +183% |
| 信息完整率 | 50% | 90% | +80% |
| 工单创建时间 | 15分钟 | 5分钟 | -67% |
| 需要返工次数 | 2-3次 | 0.5次 | -75% |

### 用户体验提升

1. **降低门槛**：工程师可以用口语描述，AI帮助转换
2. **引导清晰**：AI逐步引导，不会遗漏关键信息
3. **专业提升**：自动转换为专业术语
4. **效率提升**：减少反复沟通

---

## 🔍 关键技术点

### 1. Google Gemini Vision API

**优势**：
- 支持多模态（文本+图片）
- 成本较低
- API稳定

**使用示例**：
```csharp
// 使用Gemini Pro Vision分析图片
var prompt = "分析这张图片中的设备问题";
var imageBase64 = ConvertToBase64(imageStream);

var request = new GeminiVisionRequest
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
                    InlineData = new InlineData
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

### 2. 迭代式对话管理

**关键点**：
- 维护对话状态
- 控制对话轮数（避免无限循环）
- 智能判断信息完整性

### 3. 专业术语转换

**策略**：
- 建立术语词典
- 使用Few-Shot示例
- 提供术语解释

---

## 📝 总结

这个AI引导式工单创建系统将：

1. ✅ **降低门槛**：工程师可以用口语描述
2. ✅ **提升专业性**：AI自动转换为专业术语
3. ✅ **提高效率**：减少反复沟通
4. ✅ **保证质量**：确保信息完整

**核心价值**：让学历较低的工程师也能创建专业的工单，提升整体服务质量。

---

**文档版本**：1.0  
**最后更新**：2025-12-29  
**维护人**：开发团队

