# API 文档

本文档描述系统的 RESTful API 接口。

## 🔐 认证

所有 API 请求（除登录接口外）都需要在 Header 中携带 JWT Token：

```
Authorization: Bearer {token}
```

## 📋 通用响应格式

### 成功响应

```json
{
  "success": true,
  "data": { ... },
  "message": "操作成功"
}
```

### 错误响应

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "错误描述",
    "details": { ... }
  }
}
```

## 🔑 认证 API

### 获取企业微信登录URL

```http
GET /api/auth/wecom/login-url
```

**响应**：
```json
{
  "url": "https://open.weixin.qq.com/connect/oauth2/authorize?...",
  "state": "csrf_token"
}
```

### 企业微信回调

```http
POST /api/auth/wecom/callback
Content-Type: application/json

{
  "code": "授权码",
  "state": "状态码"
}
```

**响应**：
```json
{
  "token": "jwt_token",
  "refresh_token": "refresh_token",
  "expires_in": 604800,
  "user": {
    "id": "user_id",
    "name": "用户姓名",
    "role": "FieldEngineer"
  }
}
```

### 获取当前用户

```http
GET /api/me
Authorization: Bearer {token}
```

### 刷新Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refresh_token": "refresh_token"
}
```

## 🎫 工单 API

### 创建工单草稿

```http
POST /api/tickets
Authorization: Bearer {token}
Content-Type: application/json
X-Idempotency-Key: {uuid}

{
  "deviceId": "device_uuid",
  "domain": "C",
  "stepCode": "Step_120",
  "symptomTitle": "夹具到位后程序超时",
  "swVersion": "v2.1.3",
  "plcVersion": "v1.2.6",
  "paramVersion": "v3.4",
  "factsJson": { ... }
}
```

### 更新工单草稿

```http
PUT /api/tickets/{ticketId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "symptomTitle": "更新后的描述",
  "factsJson": { ... }
}
```

### 提交工单

```http
POST /api/tickets/{ticketId}/submit
Authorization: Bearer {token}
```

**响应**：
```json
{
  "success": true,
  "ticketNo": "TK-20251222-001",
  "status": "Submitted"
}
```

## 🔔 通知规则 API

### 获取通知规则列表

```http
GET /api/notification-rules?customerId={id}&projectId={id}&deviceId={id}&triggerEvent=ticket_submitted
Authorization: Bearer {token}
```

**响应**：
```json
{
  "items": [
    {
      "ruleId": "rule_uuid",
      "ruleLevel": "project",
      "projectId": "project_uuid",
      "triggerEvent": "ticket_submitted",
      "recipientsConfig": {
        "roles": ["project_manager", "sales"],
        "userIds": ["userid1"],
        "chatIds": ["chatid1"]
      },
      "isActive": true
    }
  ]
}
```

### 创建/更新通知规则

```http
POST /api/notification-rules
PUT /api/notification-rules/{ruleId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "ruleLevel": "project",
  "projectId": "project_uuid",
  "triggerEvent": "ticket_submitted",
  "recipientsConfig": {
    "roles": ["project_manager", "engineer"],
    "userIds": ["userid1", "userid2"],
    "chatIds": ["chatid1"]
  }
}
```

### 删除通知规则

```http
DELETE /api/notification-rules/{ruleId}
Authorization: Bearer {token}
```

### 获取企业微信通讯录

```http
GET /api/wecom/contacts?role=project_manager&department=dept_id
Authorization: Bearer {token}
```

**响应**：
```json
{
  "users": [
    {
      "userid": "userid1",
      "name": "张三",
      "department": ["部门1"],
      "tags": ["项目经理", "PM"],
      "roles": ["project_manager"]
    }
  ],
  "chats": [
    {
      "chatid": "chatid1",
      "name": "技术讨论群",
      "memberCount": 10
    }
  ]
}
```

### 获取通知日志

```http
GET /api/tickets/{ticketId}/notification-logs
Authorization: Bearer {token}
```

### 工单列表

```http
GET /api/tickets?status=Submitted&page=1&pageSize=20
Authorization: Bearer {token}
```

### 工单详情

```http
GET /api/tickets/{ticketId}
Authorization: Bearer {token}
```

## 📎 附件 API

### 上传附件

```http
POST /api/attachments
Authorization: Bearer {token}
Content-Type: multipart/form-data

ticketId: {ticket_id}
file: (binary)
fileType: photo|video|log|file
```

### 获取下载URL

```http
GET /api/attachments/{attachmentId}/download-url
Authorization: Bearer {token}
```

## 🔍 分诊 API

### 分诊工单

```http
POST /api/tickets/{ticketId}/triage
Authorization: Bearer {token}
Content-Type: application/json

{
  "jcCode": "JC-017",
  "note": "分诊备注",
  "conclusion": "程序逻辑/IO判定窗口问题"
}
```

### 获取推荐判断卡

```http
GET /api/judgement-cards/recommend?domain=C&stepCode=Step_120
Authorization: Bearer {token}
```

## 💡 解决方案 API

### 创建解决方案

```http
POST /api/tickets/{ticketId}/solutions
Authorization: Bearer {token}
Content-Type: application/json

{
  "summary": "方案摘要",
  "changeDetailJson": { ... },
  "verificationChecklistJson": { ... },
  "releaseType": "PLC"
}
```

### 发布解决方案

```http
POST /api/solutions/{solutionId}/publish
Authorization: Bearer {token}
```

## ✅ 验证 API

### 提交验证结果

```http
POST /api/tickets/{ticketId}/verifications
Authorization: Bearer {token}
Content-Type: application/json

{
  "solutionId": "solution_uuid",
  "runCount": 20,
  "passCount": 20,
  "result": "PASS",
  "checklistResultJson": { ... },
  "evidenceAttachmentIds": ["attachment_id"]
}
```

## 📊 统计 API

### 概览统计

```http
GET /api/stats/overview?dateFrom=2025-01-01&dateTo=2025-01-31
Authorization: Bearer {token}
```

### Top问题域

```http
GET /api/stats/top-domains?dateFrom=2025-01-01&dateTo=2025-01-31&limit=5
Authorization: Bearer {token}
```

## 🔢 状态码

- `200 OK` - 请求成功
- `201 Created` - 创建成功
- `400 Bad Request` - 请求参数错误
- `401 Unauthorized` - 未认证
- `403 Forbidden` - 无权限
- `404 Not Found` - 资源不存在
- `422 Unprocessable Entity` - 验证失败
- `500 Internal Server Error` - 服务器错误

## 📝 完整 API 文档

更详细的 API 文档请访问：
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI 规范: `/api/openapi.json`

---

**最后更新**：2025-12-22

