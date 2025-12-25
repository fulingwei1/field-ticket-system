# 判断卡推荐功能实现总结

> **完成日期**：2025-12-22  
> **状态**：✅ 实现完成

---

## ✅ 已完成的工作

### 1. 后端实现 ✅

#### 服务接口和实现
**文件**：
- ✅ `backend/src/FieldTicket.Core/Services/IJudgementCardRecommendationService.cs` - 服务接口
- ✅ `backend/src/FieldTicket.Infrastructure/Services/JudgementCardRecommendationService.cs` - 服务实现

**核心功能**：
- ✅ 根据工单信息推荐判断卡
- ✅ 多维度匹配评分算法：
  - 问题域匹配：+50分
  - 步骤匹配：+30分（完全匹配）或 +15分（部分匹配）
  - 关键词匹配：+10分/词（最高20分）
  - 事实特征匹配：+5分/项（最高10分）
  - 使用统计加分：+5分（如果使用次数>10）
- ✅ 返回Top K候选判断卡（默认5个）
- ✅ 显示匹配得分和匹配原因

#### 数据模型
**文件**：`backend/src/FieldTicket.Shared/Models/JudgementCardRecommendationModels.cs`

**包含模型**：
- `RecommendJudgementCardsRequest` - 推荐请求
- `JudgementCardRecommendationDto` - 推荐结果DTO
- `RecommendJudgementCardsResponse` - 推荐响应

#### API端点
**文件**：`backend/src/FieldTicket.Api/Endpoints/JudgementCardRecommendationEndpoints.cs`

**端点**：
- ✅ `GET /api/judgement-cards/recommend` - 推荐判断卡

**查询参数**：
- `domain` - 问题域（可选）
- `stepCode` - 步骤代码（可选）
- `symptomTitle` - 症状标题（可选）
- `topK` - 返回Top K个结果（默认5）

---

### 2. 前端实现 ✅

#### 服务层
**文件**：`web-admin/src/services/judgementCardRecommendationService.ts`

**功能**：
- ✅ `recommendJudgementCards()` - 调用推荐API

#### 分诊面板集成
**文件**：`web-admin/src/pages/tickets/TriagePanel.tsx`

**功能**：
- ✅ 自动加载推荐判断卡（基于工单的问题域、步骤、症状）
- ✅ 显示推荐列表（包含匹配得分和匹配原因）
- ✅ 点击推荐项自动选择判断卡
- ✅ 显示判断卡使用统计
- ✅ 刷新推荐功能

**UI特性**：
- 推荐列表显示在分诊表单上方
- 高亮显示已选中的判断卡
- 显示匹配得分和匹配原因标签
- 显示使用次数统计

---

## 📊 推荐算法详解

### 评分规则

1. **问题域匹配**（+50分）
   - 如果工单的问题域与判断卡的问题域完全匹配，得50分

2. **步骤匹配**（+30分或+15分）
   - 完全匹配：+30分
   - 部分匹配：+15分

3. **关键词匹配**（+10分/词，最高20分）
   - 从判断卡标题和症状结构中提取关键词
   - 与工单症状标题进行匹配
   - 每个匹配的关键词得10分，最高20分

4. **事实特征匹配**（+5分/项，最高10分）
   - 检查工单的事实特征（FactsJson）是否匹配判断卡的关键检查项（key_checks）
   - 每个匹配的特征得5分，最高10分

5. **使用统计加分**（+5分）
   - 如果判断卡使用次数>10次，额外得5分

### 匹配原因说明

推荐结果会显示详细的匹配原因，帮助用户理解为什么推荐这个判断卡：
- "问题域匹配（C）"
- "步骤完全匹配（Step_120）"
- "关键词匹配：超时、到位检测"
- "事实特征匹配（3项）"
- "常用判断卡（使用25次）"

---

## 🎯 使用场景

### 场景1：分诊时自动推荐
1. 高级工程师打开分诊面板
2. 系统自动根据工单信息推荐Top 5判断卡
3. 工程师查看推荐列表，了解匹配原因
4. 点击推荐项自动选择判断卡
5. 继续完成分诊流程

### 场景2：手动刷新推荐
1. 工程师可以点击"刷新推荐"按钮
2. 系统重新计算推荐结果
3. 更新推荐列表

---

## ✅ 验收标准

- [x] 能够根据工单信息推荐判断卡
- [x] 推荐结果按得分排序
- [x] 显示匹配原因
- [x] 返回Top 5候选（可配置）
- [x] 前端集成到分诊面板
- [x] 支持点击推荐项自动选择

---

## 🔗 相关文件

### 后端
- `backend/src/FieldTicket.Core/Services/IJudgementCardRecommendationService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/JudgementCardRecommendationService.cs`
- `backend/src/FieldTicket.Shared/Models/JudgementCardRecommendationModels.cs`
- `backend/src/FieldTicket.Api/Endpoints/JudgementCardRecommendationEndpoints.cs`
- `backend/src/FieldTicket.Api/Program.cs` - 服务注册

### 前端
- `web-admin/src/services/judgementCardRecommendationService.ts`
- `web-admin/src/pages/tickets/TriagePanel.tsx`

---

## 📝 技术细节

### 关键词提取
- 从判断卡标题中提取关键词（按空格、横线、下划线分割）
- 从症状结构的`keywords`字段中提取关键词
- 去重并过滤长度<=1的关键词

### 步骤匹配
- 完全匹配：工单步骤代码在判断卡的适用步骤列表中
- 部分匹配：工单步骤代码包含判断卡的步骤代码，或反之

### 事实特征匹配
- 解析判断卡的`SymptomStructure.key_checks`字段
- 检查工单的`FactsJson`中是否包含这些字段
- 统计匹配的特征数量

---

## 🚀 后续优化建议

1. **AI增强推荐**
   - 使用RAG检索相关历史工单
   - 基于历史工单的成功案例推荐判断卡

2. **学习用户习惯**
   - 记录用户选择的判断卡
   - 根据用户历史选择调整推荐权重

3. **推荐效果评估**
   - 跟踪推荐判断卡的使用率
   - 评估推荐准确率
   - 优化推荐算法

---

**最后更新**：2025-12-22  
**状态**：✅ 实现完成

