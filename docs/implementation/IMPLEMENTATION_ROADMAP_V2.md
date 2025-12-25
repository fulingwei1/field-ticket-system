# v2.0 实施路线图（Sprint级别）

> **版本**：2.0  
> **创建日期**：2025-12-22  
> **最后更新**：2025-12-22  
> **总工期**：6周（3个Sprint，每个Sprint 2周）

---

## 📋 目录

1. [概述](#概述)
2. [Sprint规划](#sprint规划)
3. [Sprint 1：MVP核心功能](#sprint-1mvp核心功能)
4. [Sprint 2：可用性增强 + v2.1质量保障](#sprint-2可用性增强--v21质量保障)
5. [Sprint 3：体验优化 + 长期价值](#sprint-3体验优化--长期价值)
6. [验收标准](#验收标准)
7. [风险与应对](#风险与应对)

---

## 概述

### 总体目标

构建以"工程判断中台"为核心的智能客服系统v2.0，实现：
1. **判断卡为核心资产**：判断卡库管理、版本控制、使用统计
2. **AI辅助判断**：RAG技术，AI辅助生成假设和动作建议
3. **硬规则保障**：5条硬规则确保系统不退化
4. **知识积累**：判断型+结论型知识库分离

### 团队配置

- **后端开发**：1人（.NET 8）
- **前端/移动端开发**：1人（React + Flutter）
- **总投入**：74人天（6周 × 2人）

### 里程碑

| 里程碑 | 时间 | 交付物 |
|--------|------|--------|
| **M1：数据模型完成** | Sprint 1 Week 1 | 数据库迁移脚本、实体模型 |
| **M2：判断卡核心功能** | Sprint 1 Week 2 | 判断卡CRUD、版本管理 |
| **M3：硬规则实现** | Sprint 2 Week 1 | 5条硬规则业务逻辑 |
| **M4：AI功能MVP** | Sprint 2 Week 2 | RAG检索、AI生成 |
| **M5：知识库功能** | Sprint 3 Week 1 | 知识库管理、分离 |
| **M6：系统上线** | Sprint 3 Week 2 | 完整测试、上线部署 |

---

## Sprint规划

### Sprint概览

```
┌─────────────────────────────────────────────────────────────┐
│                    Sprint 1 (Week 1-2)                      │
│              MVP核心功能                                      │
│  - 数据模型设计                                              │
│  - 判断卡核心功能                                            │
│  - 工单扩展字段                                              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    Sprint 2 (Week 3-4)                      │
│          可用性增强 + v2.1质量保障                            │
│  - 硬规则实现                                                │
│  - AI功能MVP                                                 │
│  - 问诊式补全                                                │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    Sprint 3 (Week 5-6)                       │
│           体验优化 + 长期价值                                 │
│  - 知识库功能                                                │
│  - 整改任务（CAPA）                                          │
│  - 性能优化、测试、上线                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## Sprint 1：MVP核心功能

**时间**：Week 1-2（2周）  
**目标**：完成数据模型和判断卡核心功能

### Week 1：数据模型与基础设施

#### Day 1-2：数据模型设计

**任务分解**：
1. **Day 1上午**：评审数据模型设计文档
   - 团队评审 `docs/DATA_MODEL_DESIGN_V2.md`
   - 确认表结构设计
   - 确认字段类型和约束

2. **Day 1下午**：创建数据库迁移脚本
   - 创建 `backend/migrations/AddV2JudgementCardTables.sql`
   - 包含所有7个核心表的DDL
   - 包含索引创建语句
   - 包含外键约束

3. **Day 2上午**：创建实体模型（C#）
   - `backend/src/FieldTicket.Domain/Entities/JudgementCard.cs`
   - `backend/src/FieldTicket.Domain/Entities/JudgementCardVersion.cs`
   - `backend/src/FieldTicket.Domain/Entities/CustomerCommunication.cs`
   - `backend/src/FieldTicket.Domain/Entities/MissingInfoChecklist.cs`
   - `backend/src/FieldTicket.Domain/Entities/CorrectiveAction.cs`
   - `backend/src/FieldTicket.Domain/Entities/KnowledgeBaseItem.cs`

4. **Day 2下午**：配置DbContext
   - 更新 `ApplicationDbContext.cs`
   - 配置所有新实体的映射
   - 配置索引和外键

**交付物清单**：
- [ ] `docs/DATA_MODEL_DESIGN_V2.md` - 数据模型设计文档（已完成）
- [ ] `backend/migrations/AddV2JudgementCardTables.sql` - 数据库迁移脚本
- [ ] `backend/src/FieldTicket.Domain/Entities/JudgementCard.cs` - 判断卡实体
- [ ] `backend/src/FieldTicket.Domain/Entities/JudgementCardVersion.cs` - 判断卡版本实体
- [ ] `backend/src/FieldTicket.Domain/Entities/CustomerCommunication.cs` - 客户沟通实体
- [ ] `backend/src/FieldTicket.Domain/Entities/MissingInfoChecklist.cs` - 缺失信息清单实体
- [ ] `backend/src/FieldTicket.Domain/Entities/CorrectiveAction.cs` - 整改任务实体
- [ ] `backend/src/FieldTicket.Domain/Entities/KnowledgeBaseItem.cs` - 知识库项实体
- [ ] `backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs` - DbContext配置更新

**验收标准**：

**功能验收**：
- [ ] 所有7个表结构定义完整（字段、类型、约束）
- [ ] 迁移脚本语法正确，可以成功执行
- [ ] 实体模型属性与数据库表字段一一对应
- [ ] DbContext配置正确（表名、字段映射、索引、外键）

**质量验收**：
- [ ] 实体模型符合C#命名规范
- [ ] 所有必填字段有NOT NULL约束
- [ ] 所有外键有正确的ON DELETE行为
- [ ] 索引设计合理（查询性能考虑）

**文档验收**：
- [ ] 数据模型设计文档完整
- [ ] 迁移脚本有注释说明
- [ ] 实体模型有XML文档注释

#### Day 3-4：数据库迁移

**任务分解**：
1. **Day 3上午**：准备迁移环境
   - 备份现有数据库
   - 准备测试数据库
   - 验证PostgreSQL版本（需要14+）

2. **Day 3下午**：执行迁移脚本
   - 在测试环境执行迁移
   - 验证表创建成功
   - 验证索引创建成功
   - 验证外键约束正确

3. **Day 4上午**：数据模型验证
   - 执行验证SQL脚本
   - 检查表结构
   - 检查索引
   - 检查约束

4. **Day 4下午**：EF Core迁移（可选）
   - 创建EF Core迁移：`dotnet ef migrations add AddV2JudgementCardTables`
   - 验证迁移文件正确
   - 测试迁移回滚

**交付物清单**：
- [ ] `backend/migrations/AddV2JudgementCardTables.sql` - 迁移脚本（已执行）
- [ ] `backend/migrations/AddV2JudgementCardTables_Validation.sql` - 验证SQL脚本
- [ ] `backend/migrations/AddV2JudgementCardTables_Rollback.sql` - 回滚脚本
- [ ] `docs/migrations/V2_MIGRATION_REPORT.md` - 迁移报告
- [ ] `backend/src/FieldTicket.Infrastructure/Migrations/YYYYMMDDHHMMSS_AddV2JudgementCardTables.cs` - EF Core迁移文件（可选）

**验收标准**：

**功能验收**：
- [ ] 所有7个表创建成功（judgement_cards, judgement_card_versions, customer_communications, missing_info_checklists, corrective_actions, knowledge_base_items）
- [ ] 所有索引创建成功（至少15个索引）
- [ ] 所有外键约束正确（至少10个外键）
- [ ] CHECK约束正确（domain, confidence, status等）

**质量验收**：
- [ ] 验证SQL全部通过（表存在、索引存在、约束存在）
- [ ] 迁移脚本可以回滚（回滚脚本测试通过）
- [ ] 数据库性能正常（查询响应时间 < 100ms）

**文档验收**：
- [ ] 迁移报告包含：执行时间、创建的表、创建的索引、创建的约束
- [ ] 回滚脚本完整可用

#### Day 5：API基础框架

**任务分解**：
1. **Day 5上午**：创建Service层接口
   - `backend/src/FieldTicket.Core/Services/IJudgementCardService.cs`
   - 定义CRUD方法签名
   - 定义版本管理方法签名

2. **Day 5下午**：创建Repository层接口（如果需要）
   - `backend/src/FieldTicket.Core/Repositories/IJudgementCardRepository.cs`（可选）
   - 或直接在Service中使用DbContext

3. **Day 5下午**：创建API端点框架
   - `backend/src/FieldTicket.Api/Endpoints/JudgementCardEndpoints.cs`
   - 定义路由和端点方法框架
   - 配置权限要求

**交付物清单**：
- [ ] `backend/src/FieldTicket.Core/Services/IJudgementCardService.cs` - Service接口
  - `Task<JudgementCardDto> CreateAsync(CreateJudgementCardRequest request, Guid userId)`
  - `Task<JudgementCardDto?> GetByCodeAsync(string jcCode)`
  - `Task<List<JudgementCardDto>> GetListAsync(JudgementCardQueryRequest request)`
  - `Task<JudgementCardDto> UpdateAsync(string jcCode, UpdateJudgementCardRequest request, Guid userId)`
  - `Task DeleteAsync(string jcCode, Guid userId)`
- [ ] `backend/src/FieldTicket.Api/Endpoints/JudgementCardEndpoints.cs` - API端点框架
  - `POST /api/judgement-cards` - 创建判断卡
  - `GET /api/judgement-cards` - 查询判断卡列表
  - `GET /api/judgement-cards/{jcCode}` - 查询判断卡详情
  - `PUT /api/judgement-cards/{jcCode}` - 更新判断卡
  - `DELETE /api/judgement-cards/{jcCode}` - 删除判断卡

**验收标准**：

**功能验收**：
- [ ] Service接口方法签名完整（参数、返回类型）
- [ ] API端点路由正确（符合RESTful规范）
- [ ] 权限要求配置正确（RequireAuthorization）

**质量验收**：
- [ ] 接口定义清晰（命名规范、参数类型明确）
- [ ] 符合现有架构模式（参考TicketEndpoints、SolutionEndpoints）
- [ ] 代码风格一致

**文档验收**：
- [ ] Service接口有XML文档注释
- [ ] API端点有WithSummary和WithDescription

### Week 2：判断卡核心功能

#### Day 6-7：判断卡CRUD

**任务分解**：
1. **Day 6上午**：实现Service层
   - `backend/src/FieldTicket.Infrastructure/Services/JudgementCardService.cs`
   - 实现CreateAsync方法
   - 实现GetByCodeAsync方法
   - 实现GetListAsync方法（支持分页、筛选）

2. **Day 6下午**：实现创建和查询API
   - 实现 `POST /api/judgement-cards` - 创建判断卡
   - 实现 `GET /api/judgement-cards` - 查询判断卡列表
   - 实现 `GET /api/judgement-cards/{jcCode}` - 查询判断卡详情
   - 添加数据验证（FluentValidation）

3. **Day 7上午**：实现更新和删除API
   - 实现 `PUT /api/judgement-cards/{jcCode}` - 更新判断卡
   - 实现 `DELETE /api/judgement-cards/{jcCode}` - 删除判断卡（软删除：is_active = false）
   - 实现权限检查（只有创建者或Admin可以更新/删除）

4. **Day 7下午**：创建DTO和Request模型
   - `backend/src/FieldTicket.Shared/Models/JudgementCardModels.cs`
   - CreateJudgementCardRequest
   - UpdateJudgementCardRequest
   - JudgementCardQueryRequest
   - JudgementCardDto

**交付物清单**：
- [ ] `backend/src/FieldTicket.Infrastructure/Services/JudgementCardService.cs` - Service实现
- [ ] `backend/src/FieldTicket.Api/Endpoints/JudgementCardEndpoints.cs` - API端点实现
- [ ] `backend/src/FieldTicket.Shared/Models/JudgementCardModels.cs` - DTO和Request模型
- [ ] `backend/src/FieldTicket.Core/Validators/JudgementCardValidator.cs` - 数据验证器（可选）
- [ ] `backend/src/FieldTicket.Api/Program.cs` - 服务注册更新

**验收标准**：

**功能验收**：
- [ ] `POST /api/judgement-cards` - 创建判断卡成功，返回创建的判断卡
- [ ] `GET /api/judgement-cards` - 查询列表成功，支持分页、筛选（domain, is_active）
- [ ] `GET /api/judgement-cards/{jcCode}` - 查询详情成功，返回完整信息
- [ ] `PUT /api/judgement-cards/{jcCode}` - 更新成功，返回更新后的判断卡
- [ ] `DELETE /api/judgement-cards/{jcCode}` - 删除成功（软删除），is_active = false

**质量验收**：
- [ ] 数据验证正确（必填字段、格式验证、业务规则验证）
- [ ] 错误处理完善（404、400、500等）
- [ ] 权限检查正确（只有创建者或Admin可以更新/删除）
- [ ] 单元测试覆盖率 ≥ 70%（至少5个测试用例）

**文档验收**：
- [ ] API文档更新（Swagger显示正确）
- [ ] 代码有XML文档注释
- [ ] 错误响应有清晰的错误消息

#### Day 8-9：判断卡版本管理

**任务分解**：
1. **Day 8上午**：实现版本创建逻辑
   - 在JudgementCardService中添加CreateVersionAsync方法
   - 自动快照当前判断卡内容（title, description, symptoms等）
   - 自动递增版本号（current_version + 1）
   - 保存到judgement_card_versions表

2. **Day 8下午**：实现版本历史查询
   - 实现 `GET /api/judgement-cards/{jcCode}/versions` - 查询版本历史
   - 支持按版本号排序（降序）
   - 返回版本列表（版本号、创建时间、创建人、变更原因）

3. **Day 9上午**：实现版本回滚功能
   - 实现 `POST /api/judgement-cards/{jcCode}/versions/{version}/rollback` - 版本回滚
   - 从指定版本恢复内容到当前判断卡
   - 创建新版本记录回滚操作

4. **Day 9下午**：完善版本管理功能
   - 添加版本对比功能（可选）
   - 添加版本变更原因必填验证
   - 添加单元测试

**交付物清单**：
- [ ] `JudgementCardService.CreateVersionAsync` - 创建版本方法
- [ ] `JudgementCardService.GetVersionsAsync` - 查询版本历史方法
- [ ] `JudgementCardService.RollbackVersionAsync` - 版本回滚方法
- [ ] `POST /api/judgement-cards/{jcCode}/versions` - 创建新版本API
- [ ] `GET /api/judgement-cards/{jcCode}/versions` - 查询版本历史API
- [ ] `POST /api/judgement-cards/{jcCode}/versions/{version}/rollback` - 版本回滚API
- [ ] `backend/src/FieldTicket.Shared/Models/JudgementCardVersionModels.cs` - 版本相关DTO

**验收标准**：

**功能验收**：
- [ ] `POST /api/judgement-cards/{jcCode}/versions` - 创建新版本成功
  - 自动快照当前内容（所有字段）
  - 版本号自动递增
  - 保存到judgement_card_versions表
- [ ] `GET /api/judgement-cards/{jcCode}/versions` - 查询版本历史成功
  - 返回所有版本列表（按版本号降序）
  - 每个版本包含完整内容快照
- [ ] `POST /api/judgement-cards/{jcCode}/versions/{version}/rollback` - 版本回滚成功
  - 从指定版本恢复内容
  - 创建新版本记录回滚操作

**质量验收**：
- [ ] 版本创建时内容快照完整（所有字段都保存）
- [ ] 版本历史查询正确（按版本号排序）
- [ ] 版本回滚功能正常（内容正确恢复）
- [ ] 单元测试覆盖率 ≥ 70%（至少3个测试用例）

**文档验收**：
- [ ] API文档更新（Swagger显示正确）
- [ ] 代码有XML文档注释

#### Day 10：工单扩展字段

**任务分解**：
1. **Day 10上午**：扩展Ticket实体
   - 更新 `backend/src/FieldTicket.Domain/Entities/Ticket.cs`
   - 添加v2.0字段：current_hypothesis, next_action, confidence, escalation_required, escalated_to, escalation_reason, root_cause, responsibility_team, is_preventable, missing_info_completed, missing_info_completed_at
   - 更新ApplicationDbContext配置

2. **Day 10下午**：实现工单关联判断卡功能
   - 实现 `PUT /api/tickets/{ticketId}/judgement-card` - 关联判断卡
   - 更新工单详情API，返回判断卡信息
   - 更新工单列表API，支持按判断卡筛选

**交付物清单**：
- [ ] `backend/src/FieldTicket.Domain/Entities/Ticket.cs` - Ticket实体扩展
- [ ] `backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs` - DbContext配置更新
- [ ] `backend/migrations/AddV2TicketFields.sql` - 工单扩展字段迁移脚本
- [ ] `PUT /api/tickets/{ticketId}/judgement-card` - 关联判断卡API
- [ ] `GET /api/tickets/{ticketId}` - 工单详情API（返回判断卡信息）
- [ ] `backend/src/FieldTicket.Shared/Models/TicketModels.cs` - TicketDto更新（包含判断卡信息）

**验收标准**：

**功能验收**：
- [ ] Ticket实体包含所有v2.0字段（11个新字段）
- [ ] `PUT /api/tickets/{ticketId}/judgement-card` - 关联判断卡成功
  - 验证判断卡存在
  - 更新current_jc_code字段
  - 返回更新后的工单
- [ ] `GET /api/tickets/{ticketId}` - 工单详情包含判断卡信息
  - 返回current_jc_code
  - 返回判断卡详情（如果关联）
- [ ] 工单列表支持按判断卡筛选（可选）

**质量验收**：
- [ ] 数据库迁移脚本可以成功执行
- [ ] 数据验证正确（判断卡存在性验证）
- [ ] 单元测试覆盖率 ≥ 70%（至少2个测试用例）

**文档验收**：
- [ ] API文档更新（Swagger显示正确）
- [ ] 代码有XML文档注释

### Sprint 1 验收标准

**功能验收**：
- [ ] 判断卡CRUD功能完整
- [ ] 判断卡版本管理功能完整
- [ ] 工单可以关联判断卡
- [ ] 所有API功能正常

**质量验收**：
- [ ] 单元测试覆盖率 ≥ 70%
- [ ] API集成测试通过
- [ ] 代码审查通过
- [ ] 性能测试通过（响应时间 < 500ms）

**文档验收**：
- [ ] API文档更新
- [ ] 数据模型文档完成
- [ ] 开发文档更新

---

## Sprint 2：可用性增强 + v2.1质量保障

**时间**：Week 3-4（2周）  
**目标**：实现硬规则和AI功能MVP

### Week 3：硬规则实现

#### Day 11-12：硬规则1-2实现

**任务**：
1. **规则1：无判断卡不得结案**
   - 实现结案时检查逻辑
   - 验证 `current_hypothesis` 和 `next_action` 必填

2. **规则2：低置信度自动升级**
   - 实现置信度计算逻辑
   - 实现自动升级逻辑（confidence ≤ 2）

**交付物**：
- [ ] `CloseTicketAsync` 方法实现硬规则1
- [ ] `UpdateTicketConfidenceAsync` 方法实现硬规则2
- [ ] 单元测试覆盖硬规则逻辑

**验收标准**：
- 无判断卡时无法结案（返回错误）
- 低置信度时自动设置升级标志
- 硬规则逻辑正确

#### Day 13-14：硬规则3-4实现

**任务**：
1. **规则3：对外消息必须落库**
   - 创建客户沟通表
   - 实现客户沟通记录API
   - 实现话术模板功能

2. **规则4：结案必须归因**
   - 实现结案时归因检查逻辑
   - 验证 `root_cause`, `responsibility_team`, `is_preventable` 必填

**交付物**：
- [ ] `customer_communications` 表创建
- [ ] `POST /api/tickets/{ticketId}/communications` - 创建客户沟通
- [ ] `CloseTicketAsync` 方法实现硬规则4
- [ ] 话术模板管理功能

**验收标准**：
- 所有对外消息必须通过API记录
- 结案时必须填写归因信息
- 话术模板功能正常

#### Day 15：硬规则5实现

**任务**：
1. **规则5：重复问题阈值触发CAPA**
   - 创建整改任务表
   - 实现阈值检查逻辑
   - 实现自动创建整改任务

**交付物**：
- [ ] `corrective_actions` 表创建
- [ ] `CheckAndCreateCorrectiveActionAsync` 方法
- [ ] 定时任务（检查重复问题）

**验收标准**：
- 满足阈值时自动创建整改任务
- 整改任务关联相关工单
- 定时任务正常运行

### Week 4：AI功能MVP

#### Day 16-17：RAG基础设施

**任务**：
1. 安装PGVector扩展
2. 搭建Embedding服务
3. 实现向量检索功能

**交付物**：
- [ ] PGVector扩展安装完成
- [ ] Embedding服务可用
- [ ] `VectorSearchService` 实现
- [ ] 向量检索API测试通过

**验收标准**：
- PGVector扩展正常工作
- Embedding服务可以生成向量
- 向量检索功能正常（响应时间 < 500ms）

#### Day 18-19：AI生成功能

**任务**：
1. 实现Top-3假设生成
2. 实现下一步动作建议
3. 实现缺失信息清单生成

**交付物**：
- [ ] `POST /api/tickets/{ticketId}/ai/hypotheses` - 生成Top-3假设
- [ ] `POST /api/tickets/{ticketId}/ai/next-actions` - 生成下一步动作
- [ ] `POST /api/tickets/{ticketId}/ai/missing-info` - 生成缺失信息清单

**验收标准**：
- AI生成功能正常
- 输出格式正确（JSON）
- 证据引用正确
- 置信度计算正确

#### Day 20：问诊式补全

**任务**：
1. 实现缺失信息清单管理
2. 实现问诊式补全流程
3. 集成到工单创建流程

**交付物**：
- [ ] `missing_info_checklists` 表创建
- [ ] `POST /api/tickets/{ticketId}/missing-info/complete` - 完成补全
- [ ] 工单创建时自动生成缺失信息清单

**验收标准**：
- 缺失信息清单生成正确
- 问诊式补全流程顺畅
- 工单创建流程集成完成

### Sprint 2 验收标准

**功能验收**：
- [ ] 5条硬规则全部实现
- [ ] AI功能MVP完成
- [ ] 问诊式补全功能完成
- [ ] 所有API功能正常

**质量验收**：
- [ ] 硬规则单元测试覆盖率 ≥ 90%
- [ ] AI功能集成测试通过
- [ ] 性能测试通过（AI响应时间 < 10s）
- [ ] 代码审查通过

**文档验收**：
- [ ] AI技术选型文档完成
- [ ] API文档更新
- [ ] 硬规则实现文档完成

---

## Sprint 3：体验优化 + 长期价值

**时间**：Week 5-6（2周）  
**目标**：知识库功能、整改任务、性能优化、测试上线

### Week 5：知识库与整改任务

#### Day 21-22：知识库功能

**任务**：
1. 创建知识库项表
2. 实现判断型知识库管理
3. 实现结论型知识库管理
4. 实现知识库与判断卡关联

**交付物**：
- [ ] `knowledge_base_items` 表创建
- [ ] `POST /api/knowledge-base/items` - 创建知识库项
- [ ] `GET /api/knowledge-base/items` - 查询知识库项
- [ ] `PUT /api/knowledge-base/items/{itemId}` - 更新知识库项
- [ ] 知识库与判断卡关联功能

**验收标准**：
- 知识库项CRUD功能正常
- 判断型/结论型知识库分离正确
- 知识库与判断卡关联正确

#### Day 23-24：整改任务（CAPA）

**任务**：
1. 实现整改任务管理
2. 实现整改任务与工单关联
3. 实现整改任务状态流转
4. 实现效果评估功能

**交付物**：
- [ ] `GET /api/corrective-actions` - 查询整改任务列表
- [ ] `GET /api/corrective-actions/{actionId}` - 查询整改任务详情
- [ ] `PUT /api/corrective-actions/{actionId}/status` - 更新状态
- [ ] `POST /api/corrective-actions/{actionId}/evaluate` - 效果评估

**验收标准**：
- 整改任务管理功能完整
- 状态流转正确
- 效果评估功能正常

#### Day 25：性能优化

**任务**：
1. 优化数据库查询（添加索引）
2. 优化AI调用（缓存策略）
3. 优化API响应时间

**交付物**：
- [ ] 数据库索引优化完成
- [ ] AI结果缓存实现
- [ ] API性能优化报告

**验收标准**：
- API响应时间 < 500ms（非AI接口）
- AI响应时间 < 10s
- 数据库查询性能提升 ≥ 30%

### Week 6：测试与上线

#### Day 26-27：集成测试

**任务**：
1. 编写E2E测试用例
2. 执行集成测试
3. 修复发现的问题

**交付物**：
- [ ] E2E测试用例（11个核心用例）
- [ ] 集成测试报告
- [ ] Bug修复清单

**验收标准**：
- 11个核心E2E测试用例全部通过
- 集成测试通过率 ≥ 95%
- 关键Bug已修复

#### Day 28-29：性能测试与优化

**任务**：
1. 执行性能测试
2. 分析性能瓶颈
3. 优化性能问题

**交付物**：
- [ ] 性能测试报告
- [ ] 性能优化方案
- [ ] 优化后的性能指标

**验收标准**：
- 并发100用户，响应时间 < 1s
- 数据库连接池配置正确
- 内存使用正常

#### Day 30：上线部署

**任务**：
1. 准备生产环境
2. 执行数据库迁移
3. 部署应用
4. 验证功能

**交付物**：
- [ ] 生产环境部署完成
- [ ] 数据库迁移完成
- [ ] 功能验证通过
- [ ] 上线文档完成

**验收标准**：
- 生产环境部署成功
- 所有功能正常
- 监控告警配置完成

### Sprint 3 验收标准

**功能验收**：
- [ ] 知识库功能完整
- [ ] 整改任务功能完整
- [ ] 所有功能正常

**质量验收**：
- [ ] E2E测试全部通过
- [ ] 性能测试通过
- [ ] 代码审查通过
- [ ] 安全测试通过

**文档验收**：
- [ ] 部署文档完成
- [ ] 用户手册完成
- [ ] 运维文档完成

---

## 验收标准

### 功能验收标准

#### 核心功能

1. **判断卡管理**
   - [ ] 判断卡CRUD功能完整
   - [ ] 判断卡版本管理功能完整
   - [ ] 判断卡使用统计正确

2. **工单扩展**
   - [ ] 工单可以关联判断卡
   - [ ] 工单包含v2.0扩展字段
   - [ ] 工单状态流转正确

3. **硬规则**
   - [ ] 5条硬规则全部实现
   - [ ] 硬规则逻辑正确
   - [ ] 硬规则错误提示清晰

4. **AI功能**
   - [ ] Top-3假设生成功能正常
   - [ ] 下一步动作建议功能正常
   - [ ] 缺失信息清单生成功能正常
   - [ ] 证据引用正确

5. **知识库**
   - [ ] 判断型知识库管理功能完整
   - [ ] 结论型知识库管理功能完整
   - [ ] 知识库与判断卡关联正确

6. **整改任务**
   - [ ] 整改任务自动创建功能正常
   - [ ] 整改任务管理功能完整
   - [ ] 效果评估功能正常

### 质量验收标准

#### 代码质量

- [ ] 单元测试覆盖率 ≥ 70%
- [ ] 硬规则单元测试覆盖率 ≥ 90%
- [ ] 代码审查通过
- [ ] 无严重代码异味

#### 性能质量

- [ ] API响应时间 < 500ms（非AI接口）
- [ ] AI响应时间 < 10s
- [ ] 数据库查询性能优化完成
- [ ] 并发100用户，响应时间 < 1s

#### 安全质量

- [ ] 输入验证完善
- [ ] SQL注入防护
- [ ] XSS防护
- [ ] 认证授权正确

### 文档验收标准

- [ ] API文档完整
- [ ] 数据模型文档完成
- [ ] AI技术选型文档完成
- [ ] 部署文档完成
- [ ] 用户手册完成

---

## 风险与应对

### 风险1：数据迁移风险

**风险描述**：
- 数据库迁移可能失败
- 历史数据可能丢失

**应对措施**：
1. 迁移前完整备份数据库
2. 在测试环境先执行迁移
3. 准备回滚脚本
4. 分阶段迁移（先迁移表结构，再迁移数据）

### 风险2：AI功能开发延期

**风险描述**：
- RAG实现复杂度高
- AI API调用可能不稳定

**应对措施**：
1. 提前搭建AI基础设施
2. 准备降级方案（本地模型）
3. 分阶段实现（先实现基础功能，再优化）
4. 预留缓冲时间

### 风险3：硬规则实现复杂

**风险描述**：
- 硬规则逻辑复杂
- 可能影响现有功能

**应对措施**：
1. 先实现硬规则逻辑，再集成到业务流程
2. 充分的单元测试
3. 灰度发布（先部分用户，再全量）

### 风险4：性能问题

**风险描述**：
- AI调用可能影响性能
- 数据库查询可能慢

**应对措施**：
1. 异步处理AI调用
2. 添加缓存机制
3. 数据库索引优化
4. 性能测试提前进行

---

## 依赖关系

### 技术依赖

1. **数据库**：PostgreSQL 14+（需要支持PGVector）
2. **AI服务**：OpenAI API（需要API密钥）
3. **向量数据库**：PGVector扩展

### 业务依赖

1. **判断卡库初始化**：需要从历史工单提取或人工创建
2. **知识库初始化**：需要初始知识库内容
3. **用户培训**：需要用户培训新功能

---

## 关键里程碑检查点

### M1：数据模型完成（Sprint 1 Week 1）

**检查时间**：Sprint 1 Week 1 结束（Day 5）

**检查内容**：
- [ ] 数据模型设计文档完成（`docs/DATA_MODEL_DESIGN_V2.md`）
- [ ] 数据库迁移脚本完成（`backend/migrations/AddV2JudgementCardTables.sql`）
- [ ] 实体模型代码完成（7个实体类）
- [ ] DbContext配置完成（`ApplicationDbContext.cs`）
- [ ] 数据库迁移执行成功（测试环境）

**验收标准**：

**功能验收**：
- [ ] 所有7个表结构定义完整（字段、类型、约束）
- [ ] 迁移脚本可以成功执行（无错误）
- [ ] 实体模型与数据库表结构一致（字段映射正确）
- [ ] 所有索引创建成功（至少15个索引）
- [ ] 所有外键约束正确（至少10个外键）

**质量验收**：
- [ ] 验证SQL全部通过（表存在、索引存在、约束存在）
- [ ] 实体模型符合C#命名规范
- [ ] DbContext配置正确（表名、字段映射）

**文档验收**：
- [ ] 数据模型设计文档完整
- [ ] 迁移脚本有注释说明
- [ ] 实体模型有XML文档注释

**检查清单**：
```sql
-- 检查表是否存在
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name IN (
    'judgement_cards',
    'judgement_card_versions',
    'customer_communications',
    'missing_info_checklists',
    'corrective_actions',
    'knowledge_base_items'
  );

-- 检查索引是否存在
SELECT indexname FROM pg_indexes 
WHERE tablename IN (
    'judgement_cards',
    'judgement_card_versions',
    'customer_communications',
    'missing_info_checklists',
    'corrective_actions',
    'knowledge_base_items'
  );

-- 检查外键约束
SELECT tc.table_name, kcu.column_name, 
       ccu.table_name AS foreign_table_name
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'public';
```

### M2：判断卡核心功能（Sprint 1 Week 2）

**检查时间**：Sprint 1 Week 2 结束（Day 10）

**检查内容**：
- [ ] 判断卡CRUD功能完成（5个API端点）
- [ ] 判断卡版本管理完成（3个API端点）
- [ ] 工单关联判断卡功能完成（1个API端点）
- [ ] Service层实现完成
- [ ] DTO和Request模型完成

**验收标准**：

**功能验收**：
- [ ] `POST /api/judgement-cards` - 创建判断卡成功
- [ ] `GET /api/judgement-cards` - 查询列表成功（支持分页、筛选）
- [ ] `GET /api/judgement-cards/{jcCode}` - 查询详情成功
- [ ] `PUT /api/judgement-cards/{jcCode}` - 更新成功
- [ ] `DELETE /api/judgement-cards/{jcCode}` - 删除成功（软删除）
- [ ] `POST /api/judgement-cards/{jcCode}/versions` - 创建新版本成功
- [ ] `GET /api/judgement-cards/{jcCode}/versions` - 查询版本历史成功
- [ ] `POST /api/judgement-cards/{jcCode}/versions/{version}/rollback` - 版本回滚成功
- [ ] `PUT /api/tickets/{ticketId}/judgement-card` - 关联判断卡成功

**质量验收**：
- [ ] 单元测试覆盖率 ≥ 70%（至少10个测试用例）
- [ ] API集成测试通过（至少9个测试用例）
- [ ] 数据验证正确（必填字段、格式验证）
- [ ] 错误处理完善（404、400、500等）
- [ ] 权限检查正确

**性能验收**：
- [ ] API响应时间 < 500ms（非查询列表接口）
- [ ] 查询列表接口响应时间 < 1s（100条数据）

**文档验收**：
- [ ] API文档更新（Swagger显示正确）
- [ ] 代码有XML文档注释
- [ ] 错误响应有清晰的错误消息

**检查清单**：
- [ ] 使用Postman或Swagger测试所有API端点
- [ ] 验证数据验证（必填字段、格式验证）
- [ ] 验证权限检查（未授权用户无法访问）
- [ ] 验证错误处理（404、400、500等）
- [ ] 运行单元测试（覆盖率 ≥ 70%）
- [ ] 运行集成测试（所有测试通过）

### M3：硬规则实现（Sprint 2 Week 1）

**检查时间**：Sprint 2 Week 1 结束（Day 15）

**检查内容**：
- [ ] 硬规则1：无判断卡不得结案（实现完成）
- [ ] 硬规则2：低置信度自动升级（实现完成）
- [ ] 硬规则3：对外消息必须落库（实现完成）
- [ ] 硬规则4：结案必须归因（实现完成）
- [ ] 硬规则5：重复问题阈值触发CAPA（实现完成）

**验收标准**：

**功能验收**：
- [ ] 硬规则1：无判断卡时结案返回错误（400 Bad Request）
- [ ] 硬规则1：current_hypothesis和next_action为空时结案返回错误
- [ ] 硬规则2：confidence ≤ 2时自动设置escalation_required = true
- [ ] 硬规则2：自动升级时记录escalated_to和escalation_reason
- [ ] 硬规则3：所有对外消息必须通过customer_communications表记录
- [ ] 硬规则3：话术模板功能正常（创建、查询、使用）
- [ ] 硬规则4：结案时root_cause、responsibility_team、is_preventable必填
- [ ] 硬规则4：缺少归因信息时结案返回错误（400 Bad Request）
- [ ] 硬规则5：满足阈值时自动创建整改任务
- [ ] 硬规则5：整改任务正确关联相关工单

**质量验收**：
- [ ] 硬规则单元测试覆盖率 ≥ 90%（至少15个测试用例）
- [ ] 硬规则集成测试通过（至少5个E2E测试用例）
- [ ] 错误提示清晰（用户友好的错误消息）
- [ ] 硬规则逻辑正确（边界条件测试通过）

**性能验收**：
- [ ] 硬规则检查响应时间 < 100ms
- [ ] 阈值检查定时任务正常运行（每天执行一次）

**文档验收**：
- [ ] 硬规则实现文档完成（说明每个规则的实现方式）
- [ ] 代码有XML文档注释
- [ ] 错误响应有清晰的错误消息

**检查清单**：
- [ ] 测试硬规则1：尝试结案无判断卡的工单（应该失败）
- [ ] 测试硬规则2：设置confidence = 1，验证自动升级
- [ ] 测试硬规则3：尝试直接发送消息（应该失败，必须通过API）
- [ ] 测试硬规则4：尝试结案无归因信息的工单（应该失败）
- [ ] 测试硬规则5：创建3个相同根因的工单，验证自动创建整改任务
- [ ] 运行硬规则单元测试（覆盖率 ≥ 90%）
- [ ] 运行硬规则集成测试（所有测试通过）

### M4：AI功能MVP（Sprint 2 Week 2）

**检查时间**：Sprint 2 Week 2 结束（Day 20）

**检查内容**：
- [ ] RAG基础设施完成（PGVector、Embedding服务）
- [ ] AI生成功能完成（Top-3假设、下一步动作、缺失信息清单）
- [ ] 问诊式补全功能完成（缺失信息清单管理、补全流程）
- [ ] 向量检索功能完成
- [ ] AI结果缓存实现

**验收标准**：

**功能验收**：
- [ ] PGVector扩展安装成功，向量检索功能正常
- [ ] Embedding服务可用，可以生成向量
- [ ] `POST /api/tickets/{ticketId}/ai/hypotheses` - 生成Top-3假设成功
  - 返回3个假设，按置信度排序
  - 每个假设包含证据引用（evidence_ids）
  - 每个假设包含置信度（1-5）
- [ ] `POST /api/tickets/{ticketId}/ai/next-actions` - 生成下一步动作成功
  - 返回1-3个动作建议
  - 每个动作包含验证方法
- [ ] `POST /api/tickets/{ticketId}/ai/missing-info` - 生成缺失信息清单成功
  - 返回问题清单（JSON格式）
  - 每个问题包含类型、是否必填、提示信息
- [ ] `POST /api/tickets/{ticketId}/missing-info/complete` - 完成补全成功
- [ ] 工单创建时自动生成缺失信息清单

**质量验收**：
- [ ] AI输出格式正确（JSON结构符合预期）
- [ ] 证据引用正确（引用的判断卡/工单确实相关）
- [ ] 置信度计算正确（基于证据数量、相关性、判断卡成功率）
- [ ] AI功能集成测试通过（至少5个测试用例）
- [ ] 降级方案可用（AI服务不可用时使用本地模型或缓存）

**性能验收**：
- [ ] 向量检索响应时间 < 500ms
- [ ] AI生成响应时间 < 10s（Top-3假设）
- [ ] AI生成响应时间 < 5s（下一步动作、缺失信息清单）
- [ ] AI结果缓存命中率 ≥ 30%

**文档验收**：
- [ ] AI技术选型文档完成（`docs/AI_TECHNOLOGY_SELECTION_V2.md`）
- [ ] Prompt设计文档完成（包含所有Prompt模板）
- [ ] 代码有XML文档注释
- [ ] API文档更新（Swagger显示正确）

**检查清单**：
- [ ] 测试向量检索：使用测试工单查询相关判断卡（应该返回相关结果）
- [ ] 测试Top-3假设生成：使用测试工单生成假设（应该返回3个假设，带证据引用）
- [ ] 测试下一步动作生成：使用测试工单生成动作（应该返回可验证的动作）
- [ ] 测试缺失信息清单生成：使用不完整的工单生成清单（应该识别缺失信息）
- [ ] 测试问诊式补全流程：创建工单→生成清单→补全信息→提交工单（流程顺畅）
- [ ] 测试AI结果缓存：相同工单第二次查询应该使用缓存（响应时间 < 100ms）
- [ ] 测试降级方案：模拟AI服务不可用，验证降级方案可用
- [ ] 运行AI功能集成测试（所有测试通过）

### M5：知识库功能（Sprint 3 Week 1）

**检查时间**：Sprint 3 Week 1 结束（Day 25）

**检查内容**：
- [ ] 知识库管理功能完成（判断型+结论型）
- [ ] 整改任务功能完成（CAPA）
- [ ] 知识库与判断卡关联功能完成
- [ ] 整改任务与工单关联功能完成

**验收标准**：

**功能验收**：
- [ ] `POST /api/knowledge-base/items` - 创建知识库项成功
  - 支持判断型（JUDGEMENT）和结论型（CONCLUSION）
  - 判断型包含：symptoms, differentiation_points, investigation_path
  - 结论型包含：faq_question, faq_answer, operation_guide, verified_solution
- [ ] `GET /api/knowledge-base/items` - 查询知识库项成功
  - 支持按类型筛选（JUDGEMENT/CONCLUSION）
  - 支持按问题域筛选（domain）
  - 支持分页
- [ ] `PUT /api/knowledge-base/items/{itemId}` - 更新知识库项成功
- [ ] 知识库与判断卡关联功能正常（related_jc_codes字段）
- [ ] `GET /api/corrective-actions` - 查询整改任务列表成功
  - 支持按状态筛选（OPEN/IN_PROGRESS/COMPLETED/CLOSED）
  - 支持按责任团队筛选
  - 支持分页
- [ ] `GET /api/corrective-actions/{actionId}` - 查询整改任务详情成功
- [ ] `PUT /api/corrective-actions/{actionId}/status` - 更新状态成功
- [ ] `POST /api/corrective-actions/{actionId}/evaluate` - 效果评估成功
- [ ] 整改任务与工单关联功能正常（related_ticket_ids字段）

**质量验收**：
- [ ] 判断型/结论型知识库分离正确（数据隔离）
- [ ] 知识库项CRUD功能正常（创建、查询、更新、删除）
- [ ] 整改任务状态流转正确（OPEN → IN_PROGRESS → COMPLETED → CLOSED）
- [ ] 效果评估功能正常（可以评分、记录评估备注）
- [ ] 单元测试覆盖率 ≥ 70%（至少10个测试用例）
- [ ] 集成测试通过（至少5个测试用例）

**性能验收**：
- [ ] 知识库查询响应时间 < 500ms（100条数据）
- [ ] 整改任务查询响应时间 < 500ms（100条数据）

**文档验收**：
- [ ] API文档更新（Swagger显示正确）
- [ ] 代码有XML文档注释
- [ ] 知识库使用说明文档完成

**检查清单**：
- [ ] 测试知识库创建：创建判断型知识库项（应该成功）
- [ ] 测试知识库创建：创建结论型知识库项（应该成功）
- [ ] 测试知识库查询：按类型筛选（应该只返回对应类型）
- [ ] 测试知识库关联：关联判断卡（应该正确关联）
- [ ] 测试整改任务创建：满足阈值时自动创建（应该成功）
- [ ] 测试整改任务状态流转：更新状态（应该正确流转）
- [ ] 测试效果评估：评估整改任务（应该成功记录评分）
- [ ] 运行知识库和整改任务单元测试（覆盖率 ≥ 70%）
- [ ] 运行知识库和整改任务集成测试（所有测试通过）

### M6：系统上线（Sprint 3 Week 2）

**检查时间**：Sprint 3 Week 2 结束（Day 30）

**检查内容**：
- [ ] E2E测试全部通过（11个核心用例）
- [ ] 性能测试通过（并发100用户）
- [ ] 生产环境部署完成
- [ ] 功能验证通过
- [ ] 监控告警配置完成

**验收标准**：

**功能验收**：
- [ ] 11个核心E2E测试用例全部通过：
  1. 创建判断卡 → 查询判断卡 → 更新判断卡 → 删除判断卡
  2. 创建判断卡版本 → 查询版本历史 → 版本回滚
  3. 创建工单 → 关联判断卡 → 填写假设和动作 → 结案
  4. 创建工单 → 设置低置信度 → 验证自动升级
  5. 创建工单 → 生成客户沟通 → 验证消息落库
  6. 创建工单 → 填写归因信息 → 结案
  7. 创建3个相同根因工单 → 验证自动创建整改任务
  8. 创建工单 → 生成Top-3假设 → 验证证据引用
  9. 创建工单 → 生成下一步动作 → 验证动作可验证
  10. 创建工单 → 生成缺失信息清单 → 补全信息 → 提交工单
  11. 创建知识库项 → 关联判断卡 → 查询知识库项
- [ ] 所有API功能正常（至少30个API端点）
- [ ] 所有硬规则正确实施（5条硬规则）
- [ ] 所有AI功能正常（Top-3假设、下一步动作、缺失信息清单）

**质量验收**：
- [ ] 单元测试覆盖率 ≥ 70%（整体）
- [ ] 硬规则单元测试覆盖率 ≥ 90%
- [ ] 集成测试通过率 ≥ 95%
- [ ] E2E测试全部通过（11个核心用例）
- [ ] 代码审查通过（无严重代码异味）
- [ ] 安全测试通过（输入验证、SQL注入防护、XSS防护）

**性能验收**：
- [ ] API响应时间 < 500ms（非AI接口，P95）
- [ ] AI响应时间 < 10s（Top-3假设，P95）
- [ ] 并发100用户，响应时间 < 1s（P95）
- [ ] 数据库查询性能优化完成（查询时间 < 100ms）
- [ ] 数据库连接池配置正确（最大连接数、超时时间）

**部署验收**：
- [ ] 生产环境部署成功（应用启动正常）
- [ ] 数据库迁移执行成功（所有表创建成功）
- [ ] 环境变量配置正确（数据库连接、API密钥等）
- [ ] 监控告警配置完成（应用健康检查、错误告警、性能告警）
- [ ] 日志配置完成（结构化日志、日志级别）

**文档验收**：
- [ ] 部署文档完成（包含部署步骤、环境配置、回滚方案）
- [ ] 用户手册完成（包含功能说明、操作指南）
- [ ] 运维文档完成（包含监控指标、告警规则、故障处理）
- [ ] API文档完整（Swagger显示所有端点）

**检查清单**：
- [ ] 运行所有E2E测试（11个核心用例全部通过）
- [ ] 执行性能测试（并发100用户，响应时间 < 1s）
- [ ] 检查生产环境部署（应用启动正常，数据库连接正常）
- [ ] 验证所有功能（至少测试30个API端点）
- [ ] 检查监控告警（健康检查正常，告警规则配置正确）
- [ ] 检查日志（日志正常输出，结构化格式正确）
- [ ] 验证硬规则（5条硬规则全部正确实施）
- [ ] 验证AI功能（Top-3假设、下一步动作、缺失信息清单正常）
- [ ] 检查数据库性能（查询响应时间 < 100ms）
- [ ] 检查代码覆盖率（单元测试覆盖率 ≥ 70%）

---

**文档版本**：1.0  
**最后更新**：2025-12-22  
**维护人**：开发团队

