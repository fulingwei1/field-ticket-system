# Issue #010: 判断卡推荐引擎 - 实现总结

## ✅ 已完成的工作

### 后端实现

#### 1. 服务接口和实现

**文件**：
- `backend/src/FieldTicket.Core/Services/IJudgementCardRecommendationService.cs` - 服务接口
- `backend/src/FieldTicket.Infrastructure/Services/JudgementCardRecommendationService.cs` - 服务实现

**核心功能**：
- ✅ 根据工单信息推荐判断卡（问题域、步骤、关键词、事实特征）
- ✅ 根据工单ID推荐判断卡（自动提取工单信息）
- ✅ 计算匹配得分和匹配原因
- ✅ 返回Top K候选判断卡（默认Top 5）

#### 2. 推荐算法

**评分维度**：

1. **问题域匹配**（高相关性，50分）
   - 如果判断卡的问题域与工单问题域完全匹配：+50分

2. **步骤匹配**（中相关性，最高30分）
   - 完全匹配：+30分
   - 部分匹配：+15分

3. **关键词匹配**（中相关性，最高20分）
   - 从判断卡标题和关键词中提取关键词
   - 与工单症状标题匹配：+10分/词
   - 最高20分

4. **事实特征匹配**（低相关性，最高10分）
   - 判断卡的关键检查项与工单事实表匹配：+5分/项
   - 最高10分

5. **使用统计加分**（低相关性，5分）
   - 如果判断卡使用次数 > 10：+5分

**评分范围**：0-115分（理论上限）

#### 3. DTO 模型

**文件**：`backend/src/FieldTicket.Shared/Models/JudgementCardRecommendationModels.cs`

**包含模型**：
- `RecommendJudgementCardsRequest` - 推荐请求
- `RecommendJudgementCardsResponse` - 推荐响应
- `JudgementCardRecommendationDto` - 推荐结果DTO（包含匹配得分、原因、得分明细）

#### 4. API 端点

**文件**：`backend/src/FieldTicket.Api/Endpoints/JudgementCardRecommendationEndpoints.cs`

**端点列表**：
- `GET /api/judgement-cards/recommend` - 根据参数推荐判断卡
  - 参数：`domain`, `stepCode`, `symptomTitle`, `topK`（默认5）
- `GET /api/judgement-cards/recommend/tickets/{ticketId}` - 根据工单推荐判断卡
  - 参数：`ticketId`（路径参数）, `topK`（查询参数，默认5）

#### 5. 服务注册

**文件**：`backend/src/FieldTicket.Api/Program.cs`

**已注册**：
- ✅ IJudgementCardRecommendationService → JudgementCardRecommendationService
- ✅ JudgementCardRecommendationEndpoints

## 📝 技术细节

### 推荐流程

1. **根据参数推荐**：
   - 接收问题域、步骤代码、症状标题、事实表等参数
   - 查询所有活跃的判断卡
   - 对每个判断卡计算匹配得分
   - 按得分排序，返回Top K

2. **根据工单推荐**：
   - 根据工单ID获取工单信息
   - 提取工单的问题域、步骤代码、症状标题、事实表
   - 调用推荐算法
   - 返回推荐结果

### 关键词提取

**来源**：
- 判断卡标题（按空格、横线、下划线分割）
- 症状结构中的关键词字段（如果存在）

**过滤**：
- 去除长度 <= 1 的词
- 去重

### 步骤匹配逻辑

1. **完全匹配**：
   - 判断卡的适用步骤列表包含工单的步骤代码

2. **部分匹配**：
   - 步骤代码包含判断卡的适用步骤，或反之

### 事实特征匹配逻辑

1. 从判断卡的症状结构中提取关键检查项（`key_checks`）
2. 检查工单的事实表是否包含这些字段
3. 计算匹配项数量，按项加分

### 推荐结果结构

```json
{
  "recommendations": [
    {
      "judgementCard": {
        "jcCode": "JC-C-001",
        "title": "PLC IO判定窗口问题",
        "domain": "C",
        ...
      },
      "matchScore": 85.0,
      "matchReasons": [
        "问题域匹配（C）",
        "步骤完全匹配（Step_120）",
        "关键词匹配：超时、IO",
        "常用判断卡（使用15次）"
      ],
      "scoreBreakdown": {
        "domain_match": 50.0,
        "step_match": 30.0,
        "keyword_match": 20.0,
        "usage_bonus": 5.0
      }
    }
  ],
  "totalCandidates": 25
}
```

## ✅ 验收标准

- [x] 能够根据工单信息推荐判断卡
- [x] 推荐结果按得分排序
- [x] 显示匹配原因
- [x] 返回Top 5候选（可配置）
- [x] 支持根据工单ID推荐
- [ ] 前端集成推荐功能（待实现）

## ⚠️ 待完成

### 前端

- [ ] 分诊页面集成推荐功能
- [ ] 显示推荐判断卡列表
- [ ] 显示匹配得分和原因
- [ ] 支持一键选择推荐判断卡
- [ ] 推荐结果可视化展示

### 功能增强

- [ ] 支持更多匹配维度（设备型号、版本等）
- [ ] 机器学习优化推荐算法
- [ ] 推荐结果反馈机制（用户选择后记录）
- [ ] 推荐准确率统计
- [ ] A/B测试不同推荐算法

## 🔗 相关文件

### 服务
- `backend/src/FieldTicket.Core/Services/IJudgementCardRecommendationService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/JudgementCardRecommendationService.cs`

### API
- `backend/src/FieldTicket.Api/Endpoints/JudgementCardRecommendationEndpoints.cs`

### 模型
- `backend/src/FieldTicket.Shared/Models/JudgementCardRecommendationModels.cs`

### 配置
- `backend/src/FieldTicket.Api/Program.cs`

---

**状态**: ✅ 后端核心功能实现完成，待前端集成

