# AI引导式工单创建 - 使用指南

> **版本**：1.0  
> **更新日期**：2025-12-29

---

## 📖 概述

AI引导式工单创建功能帮助现场工程师更专业地描述设备问题。通过多轮对话和AI分析，系统能够：

- 🤖 **智能分析**：自动分析文字描述和图片
- 💬 **引导对话**：生成针对性的引导性问题
- ✨ **专业转换**：将口语化描述转换为专业术语
- 📝 **自动生成**：自动生成工单内容

---

## 🚀 快速开始

### 1. 创建会话

```http
POST /api/guided-ticket-creation/sessions
Authorization: Bearer {token}
```

**响应**：
```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "user-id",
  "status": "collecting",
  "turnCount": 0,
  "maxTurns": 3,
  "conversationHistory": [],
  "createdAt": "2025-12-29T10:00:00Z"
}
```

### 2. 提交初始信息

```http
POST /api/guided-ticket-creation/sessions/{sessionId}/initial-info
Authorization: Bearer {token}
Content-Type: multipart/form-data

textDescription: "机器不动了，报警灯亮了"
images: [file1.jpg, file2.jpg]
```

**响应**：
```json
{
  "session": { ... },
  "isComplete": false,
  "questions": [
    {
      "questionId": "q1",
      "question": "报警灯是什么颜色的？",
      "type": "select",
      "options": ["红色", "黄色", "绿色"],
      "hint": "请查看设备上的报警指示灯",
      "professionalTermExample": "红色报警 → 紧急故障报警",
      "whyImportant": "报警灯颜色可以帮助判断故障严重程度"
    }
  ],
  "suggestions": [
    "建议：可以用'设备停止运行，报警指示灯显示红色'来描述"
  ],
  "nextStepHint": "请回答上述问题，以便AI更好地理解问题"
}
```

### 3. 回答问题

```http
POST /api/guided-ticket-creation/sessions/{sessionId}/answer
Authorization: Bearer {token}
Content-Type: application/json

{
  "questionId": "q1",
  "answer": "红色",
  "additionalImages": []
}
```

**响应**：与步骤2类似，包含下一个问题或完成提示。

### 4. 生成工单内容

```http
POST /api/guided-ticket-creation/sessions/{sessionId}/generate-content
Authorization: Bearer {token}
```

**响应**：
```json
{
  "summary": {
    "symptomTitle": "设备停止运行，红色报警指示灯亮起",
    "symptomDetail": "设备在运行过程中突然停止，报警指示灯显示红色...",
    "domain": "B",
    "stepCode": "Step_120_Clamp_Check",
    "stepName": "夹具到位检测",
    "factsJson": {
      "has_alarm": "YES",
      "alarm_color": "红色"
    },
    "versionInfo": {
      "swVersion": "V1.2.3",
      "plcVersion": "V2.1.0",
      "paramVersion": "V1.0"
    },
    "confidence": {
      "overall": 4,
      "title": 5,
      "detail": 4,
      "facts": 3
    }
  },
  "imageAttachmentIds": ["attachment-id-1", "attachment-id-2"],
  "terminologySuggestions": [
    {
      "original": "机器不动了",
      "professional": "设备停止运行，无动作输出",
      "explanation": "使用专业术语描述设备状态"
    }
  ],
  "confidence": {
    "overall": 4,
    "title": 5,
    "detail": 4,
    "facts": 3
  }
}
```

### 5. 创建工单

```http
POST /api/guided-ticket-creation/sessions/{sessionId}/create-ticket
Authorization: Bearer {token}
Content-Type: application/json

{
  "deviceId": "device-id"
}
```

**响应**：
```json
{
  "ticketId": "ticket-id",
  "ticketNo": "T-2025-001",
  "symptomTitle": "设备停止运行，红色报警指示灯亮起",
  "symptomDetail": "...",
  "status": "Draft",
  ...
}
```

---

## 📋 API 端点详情

### 1. 创建会话

**端点**：`POST /api/guided-ticket-creation/sessions`

**说明**：创建一个新的引导式工单创建会话。

**响应状态码**：
- `200 OK` - 会话创建成功
- `401 Unauthorized` - 未授权

---

### 2. 提交初始信息

**端点**：`POST /api/guided-ticket-creation/sessions/{sessionId}/initial-info`

**说明**：提交初始的文字描述和图片。

**请求格式**：`multipart/form-data`

**参数**：
- `textDescription` (string, 必填) - 文字描述
- `images` (file[], 可选) - 图片文件数组

**支持的图片格式**：
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- BMP (.bmp)
- WebP (.webp)

**文件大小限制**：每张图片 ≤ 10MB

**响应状态码**：
- `200 OK` - 提交成功，返回引导性问题
- `400 Bad Request` - 请求参数错误
- `404 Not Found` - 会话不存在
- `500 Internal Server Error` - 服务器错误

---

### 3. 回答问题

**端点**：`POST /api/guided-ticket-creation/sessions/{sessionId}/answer`

**说明**：回答AI生成的引导性问题。

**请求体**：
```json
{
  "questionId": "q1",
  "answer": "红色",
  "additionalImages": []
}
```

**参数**：
- `questionId` (string, 必填) - 问题ID
- `answer` (string, 必填) - 回答内容
- `additionalImages` (file[], 可选) - 额外图片

**响应状态码**：
- `200 OK` - 回答成功，返回下一个问题或完成提示
- `400 Bad Request` - 请求参数错误或达到最大轮数
- `404 Not Found` - 会话不存在
- `500 Internal Server Error` - 服务器错误

---

### 4. 生成工单内容

**端点**：`POST /api/guided-ticket-creation/sessions/{sessionId}/generate-content`

**说明**：生成专业的工单内容（不创建工单）。

**响应状态码**：
- `200 OK` - 生成成功
- `400 Bad Request` - 会话状态不正确
- `404 Not Found` - 会话不存在
- `500 Internal Server Error` - 服务器错误

---

### 5. 创建工单

**端点**：`POST /api/guided-ticket-creation/sessions/{sessionId}/create-ticket`

**说明**：使用AI生成的内容创建工单草稿。

**请求体**：
```json
{
  "deviceId": "device-id"
}
```

**参数**：
- `deviceId` (Guid, 必填) - 设备ID

**响应状态码**：
- `200 OK` - 工单创建成功
- `400 Bad Request` - 请求参数错误或会话状态不正确
- `404 Not Found` - 会话或设备不存在
- `500 Internal Server Error` - 服务器错误

---

### 6. 获取会话状态

**端点**：`GET /api/guided-ticket-creation/sessions/{sessionId}`

**说明**：获取会话的当前状态和对话历史。

**响应状态码**：
- `200 OK` - 获取成功
- `403 Forbidden` - 无权访问此会话
- `404 Not Found` - 会话不存在

---

## 💡 使用示例

### 完整流程示例（cURL）

```bash
# 1. 创建会话
SESSION_ID=$(curl -X POST http://localhost:5001/api/guided-ticket-creation/sessions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  | jq -r '.sessionId')

# 2. 提交初始信息
curl -X POST "http://localhost:5001/api/guided-ticket-creation/sessions/$SESSION_ID/initial-info" \
  -H "Authorization: Bearer $TOKEN" \
  -F "textDescription=机器不动了，报警灯亮了" \
  -F "images=@/path/to/image1.jpg" \
  -F "images=@/path/to/image2.jpg"

# 3. 回答问题
curl -X POST "http://localhost:5001/api/guided-ticket-creation/sessions/$SESSION_ID/answer" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "questionId": "q1",
    "answer": "红色"
  }'

# 4. 生成工单内容
curl -X POST "http://localhost:5001/api/guided-ticket-creation/sessions/$SESSION_ID/generate-content" \
  -H "Authorization: Bearer $TOKEN"

# 5. 创建工单
curl -X POST "http://localhost:5001/api/guided-ticket-creation/sessions/$SESSION_ID/create-ticket" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "device-id"
  }'
```

---

## 🔍 会话状态说明

| 状态 | 说明 | 可执行操作 |
|------|------|-----------|
| `collecting` | 收集初始信息 | 提交初始信息 |
| `analyzing` | AI分析中 | 等待 |
| `guiding` | 引导对话中 | 回答问题 |
| `completing` | 生成工单内容 | 生成内容、创建工单 |
| `completed` | 已完成 | 查看会话 |

---

## ⚠️ 注意事项

1. **会话有效期**：会话创建后24小时内有效
2. **最大轮数**：默认最多3轮对话，可在创建会话时配置
3. **图片限制**：
   - 每张图片 ≤ 10MB
   - 每次最多上传5张图片
   - 支持的格式：jpg, jpeg, png, gif, bmp, webp
4. **设备ID**：创建工单时必须提供设备ID
5. **临时工单**：系统会自动创建临时工单用于附件上传，创建正式工单后会自动清理

---

## 🐛 常见问题

### Q: 为什么需要创建临时工单？

A: 因为附件上传需要关联到工单，但在引导式创建流程中，我们还没有创建正式工单。系统会创建一个临时工单用于上传附件，创建正式工单后会自动将附件关联到正式工单并删除临时工单。

### Q: 可以跳过某些问题吗？

A: 可以，但可能会影响AI生成内容的准确性。建议尽可能回答所有问题。

### Q: 如何修改已提交的信息？

A: 当前版本不支持修改，建议创建新的会话。

### Q: 图片分析失败怎么办？

A: 系统会继续使用文本分析，但可能会影响分析结果的准确性。请确保图片清晰且格式正确。

---

## 📚 相关文档

- [AI功能改进建议](./AI_FEATURE_RECOMMENDATIONS.md)
- [AI引导式工单创建设计文档](./AI_GUIDED_TICKET_CREATION_DESIGN.md)
- [实施总结](./AI_GUIDED_TICKET_IMPLEMENTATION_SUMMARY.md)

---

**文档版本**：1.0  
**最后更新**：2025-12-29  
**维护人**：开发团队

