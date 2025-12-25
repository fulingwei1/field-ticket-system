namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// Prompt模板
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// 缺失信息分析Prompt模板
    /// </summary>
    public static string GetMissingInfoAnalysisPrompt(
        string domain,
        string stepCode,
        string symptomTitle,
        string? symptomDetail,
        string factsJson,
        string? jcRequirements,
        string? relatedTickets)
    {
        return $@"分析以下工单信息，识别可能缺失的关键信息：

工单信息：
- 问题域：{domain}
- 步骤：{stepCode}
- 问题描述：{symptomTitle}
- 详细描述：{symptomDetail ?? "无"}
- 事实表：{factsJson}

判断卡要求：
{jcRequirements ?? "无"}

历史相似工单：
{relatedTickets ?? "无"}

请识别：
1. 明确缺失的信息（基于判断卡要求）
2. 隐含缺失的信息（基于上下文分析）
3. 可能有助于诊断的额外信息

返回JSON格式：
{{
  ""explicit_missing"": [
    {{
      ""field"": ""字段名"",
      ""question"": ""问题描述"",
      ""type"": ""yes_no|number|text|file|select"",
      ""required"": true/false,
      ""reason"": ""缺失原因""
    }}
  ],
  ""implicit_missing"": [
    {{
      ""field"": ""字段名"",
      ""question"": ""问题描述"",
      ""type"": ""yes_no|number|text|file|select"",
      ""required"": true/false,
      ""confidence"": 1-5,
      ""reason"": ""识别理由""
    }}
  ],
  ""additional_info"": [
    {{
      ""field"": ""字段名"",
      ""question"": ""问题描述"",
      ""type"": ""yes_no|number|text|file|select"",
      ""required"": false,
      ""reason"": ""建议理由""
    }}
  ]
}}";
    }

    /// <summary>
    /// 上下文理解Prompt模板
    /// </summary>
    public static string GetContextUnderstandingPrompt(
        string symptomTitle,
        string? symptomDetail,
        string domain,
        string stepCode)
    {
        return $@"分析以下工单信息，理解上下文语义：

工单信息：
- 问题域：{domain}
- 步骤：{stepCode}
- 问题描述：{symptomTitle}
- 详细描述：{symptomDetail ?? "无"}

请分析：
1. 问题严重程度（1-5，5最严重）
2. 问题类型分类
3. 关键实体提取（设备、部件、动作等）
4. 情感分析（positive/neutral/negative）

返回JSON格式：
{{
  ""severity"": 1-5,
  ""categories"": [""分类1"", ""分类2""],
  ""entities"": [""实体1"", ""实体2""],
  ""sentiment"": ""positive|neutral|negative""
}}";
    }

    /// <summary>
    /// 个性化问题生成Prompt模板
    /// </summary>
    public static string GetPersonalizedQuestionPrompt(
        List<string> missingFields,
        string contextInfo,
        string? userProfile)
    {
        return $@"基于以下缺失信息，生成个性化问题清单：

缺失字段：{string.Join(", ", missingFields)}
上下文信息：{contextInfo}
用户画像：{userProfile ?? "无"}

请为每个缺失字段生成一个问题，要求：
1. 问题清晰易懂
2. 根据上下文个性化表达
3. 优先级排序（1-5，5最高）
4. 提供提示信息（可选）

返回JSON格式：
{{
  ""questions"": [
    {{
      ""field"": ""字段名"",
      ""question"": ""个性化问题"",
      ""type"": ""yes_no|number|text|file|select"",
      ""required"": true/false,
      ""priority"": 1-5,
      ""hint"": ""提示信息"",
      ""personalization_reason"": ""个性化原因""
    }}
  ]
}}";
    }

    /// <summary>
    /// 多轮对话Prompt模板
    /// </summary>
    public static string GetConversationPrompt(
        string currentQuestion,
        string userAnswer,
        string conversationHistory,
        List<string> remainingRequirements)
    {
        return $@"基于以下对话历史，决定下一个问题：

当前问题：{currentQuestion}
用户回答：{userAnswer}

对话历史：
{conversationHistory}

待收集信息：
{string.Join(", ", remainingRequirements)}

请决定：
1. 下一个问题（如果有）
2. 是否完成（如果所有信息已收集）

返回JSON格式：
{{
  ""next_question"": {{
    ""field"": ""字段名"",
    ""question"": ""问题描述"",
    ""type"": ""yes_no|number|text|file|select"",
    ""required"": true/false
  }},
  ""is_complete"": true/false,
  ""collected_info"": {{
    ""字段名"": ""值""
  }}
}}";
    }
}

