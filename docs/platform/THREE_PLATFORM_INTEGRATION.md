# 三端打通方案（Web + App + 小程序）

> **版本**：v2.0+  
> **日期**：2025-12-22  
> **核心目标**：Web、移动端App、企业微信小程序三端完全打通，重点打通数据录入功能

---

## 🎯 核心目标

### 三端打通

1. **数据录入统一**：三个平台的数据录入功能完全打通
2. **数据实时同步**：三个平台的数据实时同步，避免数据孤岛
3. **统一数据模型**：三个平台使用统一的数据模型和API接口
4. **用户体验一致**：三个平台的交互体验保持一致

### 重点：数据录入打通

**数据录入功能包括**：
- ✅ 工单创建（5步流程）
- ✅ 事实表填写
- ✅ 附件上传
- ✅ 问诊式补全
- ✅ 验证结果提交
- ✅ 客户沟通记录

---

## 📱 三端功能对比

### 数据录入功能（核心打通）

| 功能 | Web管理端 | 移动端App | 企业微信小程序 | 状态 |
|------|----------|-----------|--------------|------|
| **工单创建** | ✅ | ✅ | ✅ | 已规划 |
| **事实表填写** | ✅ | ✅ | ✅ | 已规划 |
| **附件上传** | ✅ | ✅ | ✅ | 已规划 |
| **问诊式补全** | ✅ | ✅ | ✅ | 已规划 |
| **验证结果提交** | ✅ | ✅ | ✅ | 已规划 |
| **客户沟通记录** | ✅ | ✅ | ✅ | 已规划 |
| **离线草稿** | ❌ | ✅ | ⚠️ 受限 | 已规划 |
| **断点续传** | ✅ | ✅ | ⚠️ 受限 | 已规划 |

### 管理功能

| 功能 | Web管理端 | 移动端App | 企业微信小程序 | 状态 |
|------|----------|-----------|--------------|------|
| **工单分诊** | ✅ | ❌ | ❌ | 已规划 |
| **解决方案创建** | ✅ | ❌ | ❌ | 已规划 |
| **判断卡管理** | ✅ | ❌ | ❌ | 已规划 |
| **统计看板** | ✅ | ⚠️ 简化版 | ⚠️ 简化版 | 已规划 |

---

## 🏗️ 技术架构

### 统一后端API

```
┌─────────────────────────────────────────────────────────────┐
│                     统一后端API层                            │
│              .NET 8 + ASP.NET Core Minimal API              │
│                  PostgreSQL + Redis + MinIO                 │
│                                                             │
│  - 统一数据模型                                              │
│  - 统一API接口                                               │
│  - 平台标识（web|mobile|miniprogram）                        │
│  - 实时数据同步（WebSocket）                                  │
└──────────────────┬──────────────────────────────────────────┘
                   │
        ┌──────────┼──────────┐
        │          │          │
┌───────▼───┐ ┌───▼────┐ ┌───▼──────────────┐
│ Web管理端  │ │移动端App│ │企业微信小程序      │
│ React 18  │ │Flutter │ │原生小程序框架      │
│           │ │        │ │                  │
│ 完整功能   │ │完整功能 │ │核心功能（数据录入）│
└───────────┘ └────────┘ └──────────────────┘
```

### 统一数据模型

**三端共用数据模型**：
```typescript
// 工单创建请求（三端共用）
interface CreateTicketRequest {
  device_id: string;
  domain: string;
  step_code: string;
  facts_json: Record<string, any>;
  attachments: Attachment[];
  platform: 'web' | 'mobile' | 'miniprogram';  // 平台标识
}

// 验证结果提交（三端共用）
interface VerificationResult {
  ticket_id: string;
  result: 'pass' | 'fail';
  checklist: ChecklistItem[];
  evidence: Attachment[];
  platform: 'web' | 'mobile' | 'miniprogram';  // 平台标识
}

// 沟通记录（三端共用）
interface Communication {
  ticket_id: string;
  content: string;
  type: 'call' | 'message' | 'email' | 'other';
  platform: 'web' | 'mobile' | 'miniprogram';  // 平台标识
}
```

---

## 🔄 数据同步机制

### 实时同步方案

#### 1. WebSocket实时推送

**后端实现**：
```csharp
// TicketHub.cs
public class TicketHub : Hub
{
    public async Task JoinTicketGroup(string ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ticketId);
    }
    
    public async Task BroadcastTicketUpdate(string ticketId, TicketDto ticket)
    {
        await Clients.Group(ticketId).SendAsync("TicketUpdated", ticket);
    }
}
```

**前端实现**：
- **Web端**：使用 SignalR 客户端
- **App端**：使用 WebSocket 客户端
- **小程序**：使用 wx.connectSocket()

#### 2. 轮询同步（备用）

**小程序实现**：
```typescript
// 每5秒轮询一次（WebSocket连接失败时使用）
setInterval(async () => {
  const tickets = await api.getMyTickets();
  this.updateLocalTickets(tickets);
}, 5000);
```

### 数据冲突处理

**策略**：时间戳优先，最后写入优先

```csharp
// 冲突检测
if (localVersion < serverVersion) {
    // 本地数据过期，使用服务器数据
    return serverData;
} else if (localVersion > serverVersion) {
    // 本地数据更新，合并或提示用户
    return MergeData(localData, serverData);
}
```

---

## 📋 实施计划

### Sprint 2: 小程序基础框架 + 工单创建（2周）

**目标**：搭建小程序基础框架，实现工单创建功能

- ✅ #041 企业微信小程序基础框架
- ✅ #042 小程序工单创建功能（数据录入打通）

**交付物**：
- 小程序可以正常登录
- 小程序可以创建工单
- 数据与Web/App端实时同步

### Sprint 3: 小程序验证和沟通功能（2周）

**目标**：完善小程序数据录入功能

- ✅ #043 小程序验证和沟通功能（数据录入打通）

**交付物**：
- 小程序可以提交验证结果
- 小程序可以记录客户沟通
- 三端数据录入功能完全打通

---

## ✅ 验收标准

### 功能验收

- [ ] 三端数据录入功能完全打通
- [ ] 数据实时同步（延迟 < 5秒）
- [ ] 数据一致性100%
- [ ] 平台标识正确记录

### 性能验收

- [ ] 小程序页面加载时间 < 2秒
- [ ] API响应时间 < 500ms
- [ ] 数据同步延迟 < 5秒

### 用户体验验收

- [ ] 三端交互体验一致
- [ ] 小程序操作流畅
- [ ] 错误提示清晰

---

## 🔗 相关文档

- [企业微信小程序规划](./WECHAT_MINIPROGRAM_PLAN.md)
- [平台覆盖说明](./PLATFORM_COVERAGE.md)
- [小程序基础框架Issue](../.github/issues/sprint-2/041-企业微信小程序基础框架.md)
- [小程序工单创建Issue](../.github/issues/sprint-2/042-小程序工单创建功能.md)
- [小程序验证和沟通Issue](../.github/issues/sprint-3/043-小程序验证和沟通功能.md)

---

## 📝 关键要点

### 1. 统一API接口

所有数据录入操作使用统一的API接口：
- `POST /api/tickets` - 工单创建
- `PUT /api/tickets/{id}/facts` - 事实表填写
- `POST /api/tickets/{id}/attachments` - 附件上传
- `POST /api/tickets/{id}/verification` - 验证结果提交
- `POST /api/tickets/{id}/communications` - 沟通记录

### 2. 平台标识

所有API请求必须携带平台标识：
- Header: `X-Platform: web|mobile|miniprogram`
- 或参数: `platform: "web|mobile|miniprogram"`

用于：
- 统计分析
- 数据来源追踪
- 平台特定逻辑处理

### 3. 数据同步

- **实时同步**：WebSocket推送（Web端、App端、小程序）
- **轮询同步**：小程序每5秒轮询一次（备用）
- **冲突处理**：时间戳优先，最后写入优先

---

**最后更新**：2025-12-22  
**状态**：✅ 已规划，等待实施



