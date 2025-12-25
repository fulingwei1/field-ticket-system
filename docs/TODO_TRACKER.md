# TODO 跟踪清单

## 概述

本文档追踪代码中所有 TODO 注释，按优先级分类，帮助团队系统性地解决技术债务。

**统计**:
- **总计**: 30+ 个 TODO
- **BLOCKER**: 2 个（阻塞生产）
- **HIGH**: 8 个（影响功能完整性）
- **MEDIUM**: 12 个（功能缺失，有 workaround）
- **LOW**: 8+ 个（优化建议）

**最后更新**: 2025-12-25

---

## 🔴 BLOCKER - 阻塞生产发布

必须在生产部署前解决，否则系统功能严重受损。

### 1. **用户画像评分使用硬编码假数据**
**位置**: `UserProfileService.cs:297-298, 302-303`
**优先级**: 🔴 BLOCKER
**影响**: 用户画像评分完全不准确，基于假数据

```csharp
// TODO: 需要从验证结果中计算错误率
var errorRate = 0.1m; // 默认值

// TODO: 需要从判断卡使用情况计算
var knowledgeScore = 0.7m; // 默认值
```

**问题**:
- 所有用户的错误率都是 0.1，知识掌握度都是 0.7
- 用户画像评分没有实际意义
- 管理层基于错误数据做决策

**修复建议**:
1. 从 `VerificationResult` 表计算实际错误率
2. 从 `JudgementCardUsageLog` 计算知识掌握度
3. 如果数据不足，显示"数据不足"而非假数据

**预计工作量**: 2-3 天

---

### 2. **一次解决率判断逻辑缺失**
**位置**: `PerformanceService.cs:309`
**优先级**: 🔴 BLOCKER
**影响**: KPI 指标计算错误

```csharp
// TODO: 需要根据工单历史判断是否为一次解决
```

**问题**:
- "一次解决率"是核心 KPI 指标
- 当前逻辑无法准确判断是否为一次解决
- 可能需要额外的工单状态追踪表

**修复建议**:
1. 创建 `TicketStatusHistory` 表记录状态变更
2. 判断逻辑: 工单从 Submitted → SolutionIssued → Closed 且中间没有返工
3. 或者在 Solution 表添加 `IsRework` 字段

**预计工作量**: 3-4 天

---

## 🟠 HIGH - 核心功能缺失

影响核心业务逻辑，但有临时 workaround 或不阻塞其他功能。

### 3. **解决方案验证清单检查未实现**
**位置**: `VerificationService.cs:143, 159`
**优先级**: 🟠 HIGH
**影响**: 验证流程不完整

```csharp
// TODO: 从解决方案获取验证清单，检查必填项
// TODO: 从解决方案验证清单检查必填项
```

**修复建议**:
1. 从 `Solution.VerificationChecklist` 提取必填项
2. 验证提交时检查所有必填项是否完成
3. 不完整时返回明确错误信息

**预计工作量**: 1-2 天

---

### 4. **二维码生成功能未实现**
**位置**: `QRCodeService.cs:116`
**优先级**: 🟠 HIGH
**影响**: 设备扫码功能不可用

```csharp
// TODO: 使用 QrCodeNet 或 ZXing 库生成二维码
return Array.Empty<byte>();
```

**修复建议**:
1. 引入 `QRCoder` NuGet 包（轻量级，.NET Standard）
2. 生成包含设备信息的二维码图片
3. 返回 PNG 格式字节数组

**预计工作量**: 0.5 天

---

### 5. **Excel 导出返回空白文件**
**位置**: `TicketExportService.cs:46`
**优先级**: 🟠 HIGH
**影响**: 数据导出功能不可用

```csharp
// TODO: 使用 EPPlus 或 ClosedXML 生成真正的 Excel 文件
return Array.Empty<byte>();
```

**修复建议**:
1. 引入 `ClosedXML` NuGet 包
2. 按现有 columnMapping 生成 Excel
3. 支持格式化（日期、数字格式）

**预计工作量**: 1 天

---

### 6. **AI 假设生成功能缺失**
**位置**: `ConversationalDiagnosisService.cs:76, 130`
**优先级**: 🟠 HIGH
**影响**: AI 辅助诊断功能不完整

```csharp
// TODO: 集成AI服务生成假设
// TODO: 集成AI服务生成验证步骤
```

**修复建议**:
1. 集成 Azure OpenAI 或其他 LLM API
2. 基于工单信息生成诊断假设
3. 基于假设生成验证步骤

**预计工作量**: 3-5 天（含 AI 集成调试）

---

### 7. **工单响应时间计算不准确**
**位置**: `PerformanceService.cs:337`
**优先级**: 🟠 HIGH
**影响**: KPI 指标不准确

```csharp
// TODO: 需要工单响应记录表来计算准确的响应时间
```

**修复建议**:
1. 创建 `TicketResponseLog` 表
2. 记录工程师首次查看/响应时间
3. 计算实际响应时间（不是简单的创建到分诊时间）

**预计工作量**: 2 天

---

### 8. **设备故障率统计缺失**
**位置**: `PerformanceService.cs:383`
**优先级**: 🟠 HIGH
**影响**: 设备质量分析不完整

```csharp
// TODO: 计算设备故障率和重复故障率
```

**修复建议**:
1. 按设备型号聚合故障次数
2. 计算同一设备的重复故障率
3. 用于预测性维护决策

**预计工作量**: 1-2 天

---

### 9. **团队和部门过滤功能缺失**
**位置**: `NewcomerGrowthService.cs:78`, `EngineerLoadStatService.cs:190`
**优先级**: 🟠 HIGH
**影响**: 多团队环境下数据混乱

```csharp
// TODO: 根据团队ID过滤工程师（需要团队表）
```

**修复建议**:
1. 创建 `Team` 表和 `UserTeamMapping` 表
2. 在查询时按团队过滤
3. 支持跨团队数据对比

**预计工作量**: 2 天

---

### 10. **常见错误分析未实现**
**位置**: `UserProfileService.cs:320`
**优先级**: 🟠 HIGH
**影响**: 用户画像功能不完整

```csharp
// TODO: 实现常见错误分析
return null;
```

**修复建议**:
1. 分析用户的工单填写错误模式
2. 识别高频错误类型（如漏填字段、错误域分类）
3. 返回个性化改进建议

**预计工作量**: 2-3 天

---

## 🟡 MEDIUM - 功能增强

功能缺失但有备选方案或非核心功能。

### 11. **设备版本信息从设备表获取**
**位置**: `DeviceConfigSnapshotService.cs:178`
**优先级**: 🟡 MEDIUM

```csharp
// TODO: 如果设备表有版本字段，从这里获取
```

**修复建议**: 在 Device 表添加 SwVersion, PlcVersion, ParamVersion 字段（如果合适）

**预计工作量**: 0.5 天

---

### 12. **工单字段从 Device 表获取**
**位置**: `TicketService.cs:69-70`
**优先级**: 🟡 MEDIUM

```csharp
CustomerId = Guid.Empty, // TODO: 从设备获取
ProjectId = Guid.Empty, // TODO: 从设备获取
```

**当前状态**: 已部分修复（通过冗余字段）
**进一步优化**: Device 表添加 CustomerId, ProjectId 关联

**预计工作量**: 1 天

---

### 13. **用户角色权限检查**
**位置**: `TicketService.cs:317`
**优先级**: 🟡 MEDIUM

```csharp
// TODO: 检查用户角色，如果是 FieldEngineer 则返回 null
```

**修复建议**: 实现基于角色的访问控制（RBAC）

**预计工作量**: 1 天

---

### 14. **关联查询缺失**
**位置**: `TicketService.cs:379-380, 386`
**优先级**: 🟡 MEDIUM

```csharp
CustomerName = string.Empty, // TODO: 关联查询
DeviceSn = string.Empty, // TODO: 关联查询
CreatedByName = string.Empty, // TODO: 关联查询
```

**当前状态**: ✅ 已修复（通过冗余字段 CustomerName, DeviceSn）
**CreatedByName**: 仍需要从 User 表查询

**预计工作量**: 0.5 天

---

### 15. **通知规则角色/部门筛选**
**位置**: `NotificationRuleService.cs:309, 316`
**优先级**: 🟡 MEDIUM

```csharp
// 按角色筛选（TODO: 需要实现企业微信通讯录服务）
// 按部门筛选（TODO: 需要实现企业微信通讯录服务）
```

**修复建议**: 集成企业微信通讯录 API

**预计工作量**: 2 天

---

### 16. **重复工单合并逻辑**
**位置**: `DuplicateDetectionService.cs:127, 130`
**优先级**: 🟡 MEDIUM

```csharp
// TODO: 实现附件合并逻辑
// TODO: 实现沟通记录合并逻辑
```

**修复建议**: 将两个工单的附件和评论合并到主工单

**预计工作量**: 1 天

---

### 17-24. **PerformanceService 多个 TODO**
**位置**: `PerformanceService.cs` (多处)
**优先级**: 🟡 MEDIUM

包括:
- 部门信息关联 (79, 133, 157)
- 问题反馈及时率 (451)
- 追问回复及时性 (452)
- 判断卡和解决方案统计 (462)
- 客户满意度 (474)
- 协作记录统计 (485)
- 责任归因完成度 (508)
- 部门排名 (624)
- Excel 生成 (807)

**修复建议**: 逐个实现缺失的统计功能

**预计总工作量**: 5-8 天

---

## 🟢 LOW - 优化建议

不影响功能，但提升用户体验或代码质量。

### 25. **AI建议采纳率统计**
**位置**: `NewcomerGrowthService.cs:316`
**优先级**: 🟢 LOW

```csharp
// AI建议采纳率（TODO: 需要记录AI建议采纳情况）
```

**修复建议**: 添加用户采纳/拒绝 AI 建议的追踪

**预计工作量**: 1 天

---

### 26. **设备配置快照获取方法改进**
**位置**: `DeviceConfigSnapshotService.cs:178`
**优先级**: 🟢 LOW

已有 workaround，仅为优化

---

### 27. **@提及统计**
**位置**: `EngineerLoadStatService.cs:51`
**优先级**: 🟢 LOW

```csharp
stat.MentionedCount = 0; // TODO: 实现评论表的@统计
```

**修复建议**: 解析评论内容，统计 @ 次数

**预计工作量**: 0.5 天

---

### 28. **智能阈值设备类型关联**
**位置**: `SmartThresholdService.cs:40`
**优先级**: 🟢 LOW

```csharp
// TODO: 需要关联设备表获取设备类型
```

**修复建议**: 按设备类型分类阈值

**预计工作量**: 0.5 天

---

### 29. **相似度评分详情**
**位置**: `DuplicateDetectionService.cs:363`
**优先级**: 🟢 LOW

```csharp
SimilarityScores = JsonDocument.Parse("{}"), // TODO: 保存详细相似度评分
```

**修复建议**: 保存标题、领域、设备等维度的相似度细分

**预计工作量**: 0.5 天

---

## 处理优先级建议

### 阶段 1: 修复 BLOCKER（2-3 周）
1. 用户画像评分真实数据
2. 一次解决率判断逻辑

### 阶段 2: 完成 HIGH 优先级（3-4 周）
3. 验证清单检查
4. 二维码生成
5. Excel 导出
6. AI 集成（可延后）
7. 响应时间计算
8. 设备故障率
9. 团队/部门过滤
10. 常见错误分析

### 阶段 3: MEDIUM 和 LOW（逐步优化）
- 根据用户反馈优先级调整
- 结合新需求一起实现

---

## 追踪流程

### 1. 创建 Issue
为每个 BLOCKER 和 HIGH 优先级 TODO 创建 GitHub Issue

### 2. 分配责任人
- BLOCKER: 高级工程师
- HIGH: 中/高级工程师
- MEDIUM/LOW: 新人工程师（练手）

### 3. 定期Review
- 每月审查 TODO 进度
- 更新优先级
- 移除已完成的 TODO 注释

---

## 相关文档

- [编译错误修复指南](./BUILD_FIX_GUIDE.md)
- [CHANGELOG.md](../CHANGELOG.md)
- [Sprint 计划](../ROADMAP.md)

---

**维护者**: 开发团队
**最后更新**: 2025-12-25
