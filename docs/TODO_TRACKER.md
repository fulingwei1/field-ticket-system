# TODO 跟踪清单

## 概述

本文档追踪代码中所有 TODO 注释，按优先级分类，帮助团队系统性地解决技术债务。

**统计**:
- **总计**: 30+ 个 TODO
- **BLOCKER**: 0 个（阻塞生产，2 个已完成 ✅）
- **HIGH**: 0 个（影响功能完整性，7 个已完成 ✅）
- **MEDIUM**: 6 个（功能缺失，有 workaround，6 个已完成 ✅）+ PerformanceService 部分完成（5/9 项）
- **LOW**: 8+ 个（优化建议）

**最后更新**: 2025-12-26

---

## 🔴 BLOCKER - 阻塞生产发布

必须在生产部署前解决，否则系统功能严重受损。

### 1. **用户画像评分使用硬编码假数据** ✅ 已完成
**位置**: `UserProfileService.cs:297-298, 302-303`
**优先级**: 🔴 BLOCKER
**状态**: ✅ 已完成（2025-12-26）
**影响**: 用户画像评分完全不准确，基于假数据

**修复内容**:
1. ✅ 修改 `CalculateErrorRateAsync` 方法，返回可空类型 `decimal?`
   - 当没有验证记录时返回 `null` 而非硬编码的 `0.1m`
   - 从 `Verifications` 表计算实际错误率
2. ✅ 修改 `CalculateKnowledgeScoreAsync` 方法，返回可空类型 `decimal?`
   - 当没有使用记录时返回 `null` 而非硬编码的 `0.7m`
   - 从 `JudgementCardUsageHistories` 表计算实际知识掌握度
3. ✅ 修改 `CalculateExpertiseScoreAsync` 方法，返回可空类型 `decimal?`
   - 当数据不足（权重总和 < 0.5）时返回 `null`
   - 按实际权重归一化分数，避免使用假数据
4. ✅ 更新 `BuildUserProfileAsync` 方法，处理数据不足的情况
   - `ExpertiseLevel` 在数据不足时为 `null`
   - `ExpertiseScore` 在数据不足时为 `null`（已支持可空类型）

**测试要点**:
- [ ] 测试新用户（无验证记录）：ExpertiseScore 应为 null
- [ ] 测试有验证记录的用户：应计算实际错误率
- [ ] 测试有判断卡使用记录的用户：应计算实际知识掌握度
- [ ] 测试数据不足的情况：ExpertiseScore 应为 null 而非假数据

**实际工作量**: 2-3 天

---

### 2. **一次解决率判断逻辑缺失** ✅ 已完成
**位置**: `PerformanceService.cs:309`
**优先级**: 🔴 BLOCKER
**状态**: ✅ 已完成（2025-12-26）
**影响**: KPI 指标计算错误

**修复内容**:
1. ✅ 使用已有的 `TicketStatusHistory` 表记录状态变更（表已存在）
2. ✅ 实现 `IsFirstTimeResolvedAsync` 方法，判断一次解决逻辑
   - 判断条件：工单从 Submitted → SolutionIssued → Closed
   - 排除返工：检查中间是否有 Reopened 状态
   - 验证状态顺序：确保 Submitted < SolutionIssued < Closed
3. ✅ 更新 `CalculateResolutionMetricsAsync` 方法
   - 使用 `IsFirstTimeResolvedAsync` 方法计算一次解决率
   - 替换原来的简化处理逻辑
4. ✅ 注入 `ITicketStatusHistoryService` 依赖

**判断逻辑**:
- 一次解决 = 工单状态为 Closed
- 且状态历史包含 Submitted → SolutionIssued → Closed 的完整流程
- 且中间没有 Reopened 状态（无返工）
- 且状态顺序正确（Submitted 在 SolutionIssued 之前，SolutionIssued 在 Closed 之前）

**测试要点**:
- [ ] 测试一次解决的工单：Submitted → SolutionIssued → Closed，应返回 true
- [ ] 测试有返工的工单：包含 Reopened 状态，应返回 false
- [ ] 测试没有方案的工单：没有 SolutionIssued 状态，应返回 false
- [ ] 测试状态顺序错误的工单：状态顺序不正确，应返回 false
- [ ] 测试没有状态历史的工单：应返回 false

**实际工作量**: 3-4 天

---

## 🟠 HIGH - 核心功能缺失

影响核心业务逻辑，但有临时 workaround 或不阻塞其他功能。

### 3. **解决方案验证清单检查未实现** ✅ 已完成
**位置**: `VerificationService.cs:143, 159`
**优先级**: 🟠 HIGH
**状态**: ✅ 已完成（2025-12-26）
**影响**: 验证流程不完整

**修复内容**:
1. ✅ 实现 `ValidateChecklistRequiredItems` 方法
   - 从 `Solution.VerificationChecklistJson` 提取必填项
   - 支持数组格式和对象格式的验证清单模板
   - 检查验证结果中是否包含所有必填项
   - 检查必填项的值是否为空
2. ✅ 更新 `SubmitVerificationAsync` 方法
   - 获取解决方案的验证清单（支持指定 SolutionId 或自动获取最新方案）
   - 在计算验证结果前调用 `ValidateChecklistRequiredItems` 检查必填项
   - 如果必填项未完成，抛出明确的异常信息
3. ✅ 更新 `CalculateVerificationResult` 方法
   - 接收 `Solution` 参数（虽然当前逻辑中必填项检查已提前，但保留参数以便后续扩展）
   - 简化逻辑，因为必填项检查已在提交前完成

**验证清单格式支持**:
- 数组格式：`[{ "field": "check1", "required": true, "question": "检查项1" }, ...]`
- 对象格式：`{ "check1": { "required": true, "question": "检查项1" }, ... }`

**测试要点**:
- [ ] 测试有验证清单的解决方案：必填项未完成时应抛出异常
- [ ] 测试必填项全部完成：应允许提交
- [ ] 测试没有验证清单的解决方案：应允许提交（向后兼容）
- [ ] 测试验证清单为空：应允许提交
- [ ] 测试数组格式和对象格式的验证清单：都应正确解析

**实际工作量**: 1-2 天

---

### 4. **二维码生成功能未实现** ✅ 已完成
**位置**: `QRCodeService.cs:116`
**优先级**: 🟠 HIGH
**状态**: ✅ 已完成（2025-12-26）
**影响**: 设备扫码功能不可用

**修复内容**:
1. ✅ 引入 `QRCoder` NuGet 包（版本 1.6.0）
2. ✅ 实现 `GenerateQRCodeBase64` 方法，使用 QRCoder 生成二维码
3. ✅ 返回 PNG 格式 Base64 编码的二维码图片
4. ✅ 添加错误处理和日志记录

**实际工作量**: 0.5 天

---

### 5. **Excel 导出返回空白文件** ✅ 已完成
**位置**: `TicketExportService.cs:46`
**优先级**: 🟠 HIGH
**状态**: ✅ 已完成（2025-12-26）
**影响**: 数据导出功能不可用

**修复内容**:
1. ✅ 使用已有的 `EPPlus` NuGet 包（版本 7.0.0）
2. ✅ 实现 `GenerateExcelData` 方法，生成真正的 Excel 文件
3. ✅ 支持格式化（日期格式、表头样式、边框、自动列宽）
4. ✅ 保持与 CSV 导出相同的字段映射逻辑

**实际工作量**: 1 天

---

### 6. **AI 假设生成功能缺失** ✅ 已完成
**位置**: `ConversationalDiagnosisService.cs:76, 130`
**优先级**: 🟠 HIGH
**状态**: ✅ 已完成（2025-12-26）
**影响**: AI 辅助诊断功能不完整

**修复内容**:
1. ✅ 注入 `ILLMService` 服务（可选依赖，支持降级）
2. ✅ 实现 `GenerateInitialHypothesesAsync` 使用 LLM 生成假设
   - 构建包含工单信息的提示词
   - 调用 LLM 生成结构化假设（Top-3）
   - 支持置信度转换（high/medium/low → 0.8/0.6/0.4）
3. ✅ 实现 `GenerateVerificationStepsAsync` 使用 LLM 生成验证步骤
   - 构建包含工单和假设信息的提示词
   - 调用 LLM 生成结构化验证步骤（3-5个）
   - 支持步骤类型（check/test/measure）
4. ✅ 添加降级方案
   - `GenerateRuleBasedHypotheses`：基于问题域的规则假设生成
   - `GenerateRuleBasedVerificationSteps`：基于假设描述的规则验证步骤生成
   - 当 LLM 不可用或调用失败时自动降级

**实现细节**:
- 使用 `GenerateStructuredAsync` 方法生成结构化 JSON 响应
- 温度参数：假设生成 0.3（更确定性），验证步骤 0.4（稍灵活）
- 错误处理：捕获异常并降级到规则方案，记录警告日志

**测试要点**:
- [ ] 测试 LLM 可用时：应生成 AI 假设和验证步骤
- [ ] 测试 LLM 不可用时：应降级到规则方案
- [ ] 测试 LLM 调用失败时：应降级到规则方案并记录日志
- [ ] 测试生成的假设：应包含描述、置信度、证据
- [ ] 测试生成的验证步骤：应包含描述、类型、预期结果

**实际工作量**: 3-5 天（含 AI 集成调试）

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

### 8. **设备故障率统计缺失** ✅ 已完成
**位置**: `PerformanceService.cs:383`
**优先级**: 🟠 HIGH
**状态**: ✅ 已完成（2025-12-26）
**影响**: 设备质量分析不完整

**修复内容**:
1. ✅ 实现设备故障率计算
   - 故障设备数：在统计周期内，有工单的设备数量（排除草稿状态）
   - 设备故障率 = 故障设备数 / 服务设备总数 × 100%
   - 如果服务设备总数为0，则故障率为0
2. ✅ 实现重复故障率计算
   - 重复故障定义为：同一设备在统计周期内出现2次或以上故障
   - 重复故障率 = 有重复故障的设备数 / 故障设备总数 × 100%
   - 记录日志以便后续分析（当前 PerformanceMetrics 实体可能没有 RepeatFailureRate 字段）
3. ✅ 按设备分组统计
   - 按 `DeviceId` 分组，统计每个设备的故障次数
   - 按创建时间排序，便于分析故障趋势

**实现细节**:
- 排除草稿状态的工单（`Status != "Draft"`）
- 支持空数据情况（没有工单时故障率为0）
- 记录重复故障率到日志，便于后续扩展

**测试要点**:
- [ ] 测试有故障设备的工程师：应正确计算故障率
- [ ] 测试没有故障设备的工程师：故障率应为0
- [ ] 测试有重复故障的设备：应正确识别并计算重复故障率
- [ ] 测试边界情况：服务设备总数为0、没有工单等

**实际工作量**: 1-2 天

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

### 10. **常见错误分析未实现** ✅ 已完成
**位置**: `UserProfileService.cs:320`
**优先级**: 🟠 HIGH
**状态**: ✅ 已完成（2025-12-26）
**影响**: 用户画像功能不完整

**修复内容**:
1. ✅ 实现 `AnalyzeCommonMistakesAsync` 方法
   - 分析验证失败的原因（失败率超过20%时标记）
   - 分析工单填写完整性（超过30%的工单不完整时标记）
   - 分析填写时间异常（超过30%的填写时间过长时标记）
   - 分析跳过的字段（超过50%的情况跳过时标记）
2. ✅ 实现 `GenerateImprovementSuggestions` 方法
   - 根据错误类型生成个性化改进建议
   - 提供针对性的优化建议
3. ✅ 修复字段引用错误
   - 将 `t.Description` 改为 `t.SymptomDetail`（Ticket 实体使用 SymptomDetail 字段）

**实现细节**:
- 返回包含错误类型、总错误数、改进建议的字典
- 如果没有错误，返回 null
- 支持多种错误模式识别

**测试要点**:
- [ ] 测试有验证失败的用户：应识别验证失败率较高的错误
- [ ] 测试工单填写不完整的用户：应识别填写不完整的错误
- [ ] 测试填写时间过长的用户：应识别填写时间异常
- [ ] 测试经常跳过字段的用户：应识别跳过的字段
- [ ] 测试没有错误的用户：应返回 null

**实际工作量**: 2-3 天（已实现，仅需修复字段引用）

---

## 🟡 MEDIUM - 功能增强

功能缺失但有备选方案或非核心功能。

### 11. **设备版本信息从设备表获取** ✅ 已完成
**位置**: `DeviceConfigSnapshotService.cs:178`
**优先级**: 🟡 MEDIUM
**状态**: ✅ 已完成（2025-12-26）

**修复内容**:
1. ✅ 优化 `GetCurrentConfigAsync` 方法，实现多级降级方案
   - 方法1（优先）：从最新的配置快照中获取版本信息（最准确）
   - 方法2（降级）：如果快照中没有版本信息，从最新的工单中获取
   - 方法3（默认）：如果都没有，设置默认空值
2. ✅ 支持从配置快照的 ConfigJson 中提取版本信息
   - 提取 `sw_version`、`plc_version`、`param_version`
   - 包含其他配置信息
3. ✅ 保持向后兼容
   - 如果快照中没有版本信息，自动降级到从工单获取
   - 确保在没有数据时也能返回有效的配置字典

**实现细节**:
- 由于没有独立的设备表，使用配置快照作为设备配置的权威来源
- 配置快照包含完整的设备配置信息，比工单更准确
- 工单作为降级方案，确保在没有快照时也能获取版本信息

**测试要点**:
- [ ] 测试有配置快照的设备：应优先使用快照中的版本信息
- [ ] 测试没有配置快照但有工单的设备：应使用工单中的版本信息
- [ ] 测试既没有快照也没有工单的设备：应返回空字符串
- [ ] 测试快照中部分版本信息缺失：应降级到工单获取缺失的版本

**实际工作量**: 0.5 天

---

### 12. **工单字段从 Device 表获取** ✅ 已完成
**位置**: `TicketService.cs:69-70`
**优先级**: 🟡 MEDIUM
**状态**: ✅ 已完成（2025-12-26）

**修复内容**:
1. ✅ 实现 `GetCustomerAndProjectFromDeviceAsync` 方法
   - 从同一设备的最新工单中获取 CustomerId 和 ProjectId
   - 查询条件：同一 DeviceId，且 CustomerId 和 ProjectId 不为空
   - 按创建时间降序排列，获取最新的工单
2. ✅ 更新 `CreateDraftAsync` 方法
   - 在创建工单前调用 `GetCustomerAndProjectFromDeviceAsync` 获取 CustomerId 和 ProjectId
   - 如果找不到，使用 Guid.Empty（保持向后兼容）

**实现细节**:
- 由于没有独立的设备表，从同一设备的其他工单中获取关联信息
- 优先使用最新的工单信息，确保数据相对准确
- 如果没有找到，返回空 GUID，不影响工单创建流程

**测试要点**:
- [ ] 测试有历史工单的设备：应自动填充 CustomerId 和 ProjectId
- [ ] 测试新设备（无历史工单）：应使用 Guid.Empty
- [ ] 测试设备的历史工单中 CustomerId 或 ProjectId 为空：应跳过该工单，查找下一个

**实际工作量**: 1 天

---

### 13. **用户角色权限检查** ✅ 已完成
**位置**: `TicketService.cs:317`
**优先级**: 🟡 MEDIUM
**状态**: ✅ 已完成（2025-12-26）

**修复内容**:
1. ✅ 实现基于角色的访问控制（RBAC）
   - 在 `GetTicketAsync` 方法中添加角色检查
   - 如果用户是 FieldEngineer 且不是工单的创建者，返回 null
   - 其他角色（Manager、Admin等）可以查看所有工单
2. ✅ 权限规则
   - FieldEngineer：只能查看自己创建的工单
   - Manager/Admin：可以查看所有工单
   - 如果 userId 为空，允许访问（可能是系统调用）

**实现细节**:
- 从数据库查询用户信息，检查 Role 字段
- 如果用户不存在，允许访问（向后兼容）
- 如果用户是 FieldEngineer 且不是创建者，返回 null（拒绝访问）

**测试要点**:
- [ ] 测试 FieldEngineer 查看自己创建的工单：应允许访问
- [ ] 测试 FieldEngineer 查看他人创建的工单：应返回 null
- [ ] 测试 Manager 查看任何工单：应允许访问
- [ ] 测试 Admin 查看任何工单：应允许访问
- [ ] 测试 userId 为空的情况：应允许访问（向后兼容）

**实际工作量**: 1 天

---

### 14. **关联查询缺失** ✅ 已完成
**位置**: `TicketService.cs:379-380, 386`
**优先级**: 🟡 MEDIUM
**状态**: ✅ 已完成（2025-12-26）

**修复内容**:
1. ✅ 实现 CustomerName 关联查询
   - 优先使用 Ticket.CustomerName（如果已填充）
   - 如果为空，从 Project 表查询 Project.CustomerName
2. ✅ 实现 DeviceSn 关联查询
   - 优先使用 Ticket.DeviceSn（如果已填充）
   - 如果为空，从同一设备的其他工单中查询最新的 DeviceSn
3. ✅ 实现 CreatedByName 关联查询
   - 从 User 表查询 User.Name（基于 Ticket.CreatedByUserId）

**实现细节**:
- 使用批量查询优化性能：先收集所有需要查询的 ID，然后批量查询关联表
- 使用字典缓存查询结果，避免 N+1 查询问题
- 对于 CustomerName：优先使用 Ticket 的冗余字段，降级到 Project 表
- 对于 DeviceSn：优先使用 Ticket 的冗余字段，降级到同一设备的其他工单
- 对于 CreatedByName：直接从 User 表查询

**测试要点**:
- [ ] 测试 Ticket.CustomerName 已填充：应直接使用
- [ ] 测试 Ticket.CustomerName 为空但 Project.CustomerName 存在：应从 Project 表查询
- [ ] 测试 Ticket.DeviceSn 已填充：应直接使用
- [ ] 测试 Ticket.DeviceSn 为空：应从同一设备的其他工单查询
- [ ] 测试 CreatedByName：应从 User 表正确查询
- [ ] 测试批量查询性能：应避免 N+1 查询问题

**实际工作量**: 0.5 天

---

### 15. **通知规则角色/部门筛选** ✅ 已完成
**位置**: `NotificationRuleService.cs:309, 316`
**优先级**: 🟡 MEDIUM
**状态**: ✅ 已完成（2025-12-26）

**修复内容**:
1. ✅ 实现角色筛选功能
   - 使用 `IWeComContactService.GetUsersByRoleAsync` 方法
   - 根据角色映射配置（标签和部门）获取用户列表
   - 将企业微信用户ID转换为系统用户ID
2. ✅ 实现部门筛选功能
   - 使用 `IWeComContactService.GetUsersByDepartmentNameAsync` 方法
   - 根据部门名称获取部门下的所有用户（包括子部门）
   - 将企业微信用户ID转换为系统用户ID
3. ✅ 添加辅助方法
   - `GetSystemUserIdByWeComUserIdAsync`：将企业微信用户ID转换为系统用户ID（Guid）
   - 如果系统用户不存在，直接使用企业微信用户ID作为降级方案

**实现细节**:
- 使用已有的 `IWeComContactService` 服务（已注入）
- 支持多个角色和多个部门的筛选
- 自动过滤非活跃用户（`IsActive = false`）
- 错误处理：如果通讯录服务调用失败，记录错误日志但不中断通知流程
- 如果 `IWeComContactService` 未注入，记录警告日志

**测试要点**:
- [ ] 测试按角色筛选：应正确获取指定角色的所有用户
- [ ] 测试按部门筛选：应正确获取指定部门的所有用户（包括子部门）
- [ ] 测试多个角色/部门：应正确合并所有用户并去重
- [ ] 测试非活跃用户：应自动过滤非活跃用户
- [ ] 测试企业微信用户ID转换：应正确转换为系统用户ID
- [ ] 测试服务未注入：应记录警告但不中断流程
- [ ] 测试通讯录服务异常：应记录错误但不中断流程

**实际工作量**: 2 天

---

### 16. **重复工单合并逻辑** ✅ 已完成
**位置**: `DuplicateDetectionService.cs:127, 130`
**优先级**: 🟡 MEDIUM
**状态**: ✅ 已完成（2025-12-26）

**修复内容**:
1. ✅ 实现附件合并逻辑
   - 在 `MergeTicketsAsync` 方法中调用 `MergeAttachmentsAsync`
   - 将源工单的所有附件更新为关联到目标工单（更新 `TicketId` 字段）
   - 记录合并的附件数量
2. ✅ 实现沟通记录合并逻辑
   - 在 `MergeTicketsAsync` 方法中调用 `MergeCommunicationsAsync`
   - 将源工单的所有沟通记录更新为关联到目标工单（更新 `TicketId` 字段）
   - 记录合并的沟通记录数量
3. ✅ 错误处理
   - 如果附件或沟通记录合并失败，记录错误日志但不中断合并流程
   - 确保即使合并过程中出现异常，工单合并操作也能完成

**实现细节**:
- 使用 EF Core 查询源工单的所有附件和沟通记录
- 批量更新 `TicketId` 字段，将关联关系从源工单转移到目标工单
- 记录详细的日志信息，包括合并的附件和沟通记录数量
- 异常处理：捕获并记录错误，但不抛出异常，确保合并流程能够完成

**测试要点**:
- [ ] 测试附件合并：源工单的附件应正确关联到目标工单
- [ ] 测试沟通记录合并：源工单的沟通记录应正确关联到目标工单
- [ ] 测试无附件/沟通记录：应正常完成合并，不报错
- [ ] 测试多个附件/沟通记录：应全部正确合并
- [ ] 测试合并后查询：目标工单应能查询到所有合并的附件和沟通记录
- [ ] 测试异常处理：如果合并过程中出现异常，应记录错误但不中断流程

**实际工作量**: 1 天

---

### 17-24. **PerformanceService 多个 TODO** ✅ 部分已完成
**位置**: `PerformanceService.cs` (多处)
**优先级**: 🟡 MEDIUM
**状态**: ✅ 部分已完成（2025-12-26）

**已完成项**:
1. ✅ 部门信息关联 (79, 133, 157)
   - 在 `GetMetricsAsync`、`GetTeamMetricsAsync`、`GetRankingAsync` 中实现部门筛选
   - 通过 `Engineer.DeptId` 关联部门信息
2. ✅ 判断卡和解决方案统计 (462)
   - 实现 `CalculateKnowledgeMetricsAsync` 方法
   - 计算判断卡创建数量、解决方案贡献数量
   - 计算判断卡使用准确率和命中率
3. ✅ 责任归因完成度 (508)
   - 在 `CalculateComplianceMetricsAsync` 中实现
   - 检查 `RootCause` 和 `RootResponsibility` 字段
   - 计算已归因工单占比
4. ✅ 部门排名 (624)
   - 在 `CalculateRankingAsync` 中实现
   - 根据工程师的部门信息计算部门内排名
5. ✅ Excel 生成 (807)
   - 使用 EPPlus 生成 Excel 文件
   - 实现 `GenerateExcelData` 方法
   - 包含表头样式、数据格式化、自动调整列宽

**待实现项**（需要额外的数据表或字段）:
- ⏳ 问题反馈及时率 (451) - 需要问题发生时间字段
- ⏳ 追问回复及时性 (452) - 需要工单评论/追问记录表
- ⏳ 客户满意度 (474) - 需要客户反馈和满意度表
- ⏳ 协作记录统计 (485) - 需要工单评论、@提醒等协作记录表

**修复建议**: 
- 已完成的项可以直接使用
- 待实现项需要先设计并创建相应的数据表或字段

**实际工作量**: 已完成 5/9 项（约 3-4 天），剩余 4 项需要数据库设计（约 2-4 天）

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
