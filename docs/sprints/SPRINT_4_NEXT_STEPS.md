# Sprint 4 下一步工作建议

> **创建日期**：2025-12-22  
> **当前状态**：Phase 1-2 后端实现完成

---

## ✅ 已完成工作

### Phase 1: 核心AI能力增强
1. ✅ 多轮对话式诊断（#034）- 后端完成
2. ✅ 置信度校准系统（#035）- 后端完成

### Phase 2: 归因和知识管理
3. ✅ AI辅助归因（#036）- 后端完成
4. ✅ 知识图谱构建（#037）- 后端完成
5. ✅ 知识版本管理（#038）- 后端完成

### 补充工作
6. ✅ Ticket实体归因字段 - 已添加

---

## 🎯 立即执行的工作

### 1. 数据库迁移（优先级：P0）

**必须立即执行**以下数据库迁移脚本：

```bash
# 执行顺序
1. AddTicketAttributionFields.sql          # 添加工单归因字段
2. AddConversationalDiagnosisTables.sql     # 多轮对话式诊断
3. AddConfidenceCalibrationTables.sql       # 置信度校准系统
4. AddKnowledgeGraphTables.sql              # 知识图谱构建
5. AddKnowledgeVersionTables.sql            # 知识版本管理
```

**执行方法**：
```sql
-- 在PostgreSQL中依次执行
\i backend/migrations/AddTicketAttributionFields.sql
\i backend/migrations/AddConversationalDiagnosisTables.sql
\i backend/migrations/AddConfidenceCalibrationTables.sql
\i backend/migrations/AddKnowledgeGraphTables.sql
\i backend/migrations/AddKnowledgeVersionTables.sql
```

---

### 2. API测试（优先级：P0）

**使用Swagger UI测试所有新API端点**：

#### 多轮对话式诊断（7个端点）
- `POST /api/tickets/{ticketId}/diagnosis/start`
- `POST /api/tickets/{ticketId}/diagnosis/hypotheses`
- `POST /api/conversations/{conversationId}/verification-steps`
- `POST /api/conversations/{conversationId}/verification-results`
- `POST /api/conversations/{conversationId}/adjust-hypothesis`
- `POST /api/conversations/{conversationId}/complete`
- `GET /api/conversations/{conversationId}/diagnosis-path`

#### 置信度校准系统（6个端点）
- `POST /api/confidence/calibrate`
- `POST /api/confidence/train-model`
- `GET /api/confidence/evaluate?modelId={modelId}`
- `GET /api/confidence/distribution`
- `POST /api/confidence/feedback`
- `GET /api/confidence/active-model`

#### AI辅助归因（4个端点）
- `POST /api/tickets/{ticketId}/ai-attribution/suggest`
- `POST /api/tickets/{ticketId}/ai-attribution/check-consistency`
- `GET /api/attribution/evaluate?fromDate=...&toDate=...`
- `GET /api/attribution/statistics?fromDate=...&toDate=...`

#### 知识图谱构建（5个端点）
- `POST /api/knowledge-graph/build`
- `GET /api/knowledge-graph/search?query={query}`
- `GET /api/knowledge-graph/recommend?ticketId={ticketId}`
- `GET /api/knowledge-graph/relations?nodeId={nodeId}`
- `POST /api/knowledge-graph/mine-relations`

#### 知识版本管理（5个端点）
- `POST /api/knowledge/{knowledgeId}/versions`
- `GET /api/knowledge/{knowledgeId}/versions`
- `GET /api/knowledge/versions/{versionId1}/compare/{versionId2}`
- `POST /api/knowledge/{knowledgeId}/versions/{versionId}/rollback`
- `GET /api/knowledge/expired`

---

### 3. 前端实现（优先级：P1）

根据实施计划，建议按以下顺序实现前端：

#### 第一优先级：多轮对话式诊断前端
**文件**：
- `web-admin/src/pages/diagnosis/ConversationalDiagnosis.tsx`
- `web-admin/src/components/diagnosis/HypothesisPanel.tsx`
- `web-admin/src/components/diagnosis/VerificationSteps.tsx`
- `web-admin/src/components/diagnosis/DiagnosisPathVisualization.tsx`

**功能**：
- 对话式诊断UI
- 假设展示和选择
- 验证步骤填写
- 诊断路径可视化

#### 第二优先级：置信度校准系统前端
**文件**：
- `web-admin/src/pages/confidence/CalibrationDashboard.tsx`
- `web-admin/src/components/confidence/ConfidenceDistribution.tsx`
- `web-admin/src/components/confidence/CalibrationEffectiveness.tsx`

**功能**：
- 置信度分布可视化
- 校准效果分析
- 模型性能监控

#### 第三优先级：AI辅助归因前端
**文件**：
- `web-admin/src/pages/attribution/AIAttribution.tsx`
- `web-admin/src/components/attribution/AttributionSuggestion.tsx`
- `web-admin/src/components/attribution/ConsistencyCheck.tsx`

**功能**：
- 归因建议展示
- 一致性检查结果
- 归因统计图表

#### 第四优先级：知识图谱和版本管理前端
**文件**：
- `web-admin/src/pages/knowledge-graph/KnowledgeGraphVisualization.tsx`
- `web-admin/src/pages/knowledge/VersionHistory.tsx`
- `web-admin/src/components/knowledge/VersionComparison.tsx`

**功能**：
- 知识图谱可视化
- 版本历史查看
- 版本对比功能

---

### 4. AI集成优化（优先级：P2）

#### 多轮对话式诊断
- [ ] 集成RAG服务生成假设（替换模拟数据）
- [ ] 集成LLM服务生成验证步骤
- [ ] 优化假设调整算法

#### 置信度校准系统
- [ ] 实现机器学习校准模型（当前只有线性模型）
- [ ] 优化校准因子计算
- [ ] 实现反馈学习机制

#### AI辅助归因
- [ ] 优化相似工单查找算法
- [ ] 实现更智能的归因模式分析
- [ ] 集成LLM生成归因建议

---

## 📋 测试清单

### 单元测试
- [ ] `ConversationalDiagnosisService` 测试
- [ ] `ConfidenceCalibrationService` 测试
- [ ] `AIAttributionService` 测试
- [ ] `KnowledgeGraphService` 测试
- [ ] `KnowledgeVersionService` 测试

### 集成测试
- [ ] 多轮对话式诊断完整流程
- [ ] 置信度校准完整流程
- [ ] AI辅助归因完整流程
- [ ] 知识图谱构建和检索
- [ ] 知识版本管理完整流程

### API测试
- [ ] 所有27个新API端点
- [ ] 错误处理测试
- [ ] 权限控制测试
- [ ] 性能测试

---

## 🎯 建议的下一步行动

### 选项1：继续完善Sprint 4功能（推荐）
**理由**：前端实现可以让功能完整可用，便于测试和验证

**工作内容**：
1. 实现多轮对话式诊断前端
2. 实现置信度校准系统前端
3. 实现AI辅助归因前端
4. 实现知识图谱可视化前端

**预计时间**：2-3周

---

### 选项2：开始Sprint 1核心功能补全
**理由**：根据推进建议，Sprint 1核心功能是基础

**工作内容**：
1. 附件上传功能（Issue #003）
2. 问诊式补全缺失信息（Issue #009）
3. 验证结果提交（Issue #006）

**预计时间**：2-3周

---

### 选项3：开始小程序开发
**理由**：小程序是重要的移动端入口

**工作内容**：
1. 企业微信小程序基础框架（Issue #041）
2. 小程序工单创建功能（Issue #042）

**预计时间**：2-3周

---

## 📊 当前进度总结

### Sprint 4 完成度

| Phase | 功能 | 后端 | 前端 | 完成度 |
|-------|------|------|------|--------|
| Phase 1 | 多轮对话式诊断 | ✅ | ❌ | 50% |
| Phase 1 | 置信度校准系统 | ✅ | ❌ | 50% |
| Phase 2 | AI辅助归因 | ✅ | ❌ | 50% |
| Phase 2 | 知识图谱构建 | ✅ | ❌ | 50% |
| Phase 2 | 知识版本管理 | ✅ | ❌ | 50% |
| **总计** | **5个功能** | **✅** | **❌** | **50%** |

### 整体项目完成度

- **Sprint 4后端**：100% ✅
- **Sprint 4前端**：0% ❌
- **Sprint 4总体**：50%

---

## 💡 建议

**推荐方案**：先完成前端实现，让Sprint 4功能完整可用，然后再开始其他工作。

**理由**：
1. Sprint 4功能已经设计完整，前端实现相对直接
2. 前端完成后可以立即测试和验证功能
3. 完整的Sprint 4功能可以作为演示成果
4. 为后续功能提供参考和基础

---

**最后更新**：2025-12-22  
**状态**：✅ 后端完成，等待前端实现

