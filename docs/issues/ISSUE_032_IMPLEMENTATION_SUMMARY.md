# Issue #032: 判断卡知识图谱构建实现总结

> **Issue**: #032  
> **标题**: 判断卡知识图谱构建  
> **优先级**: P1  
> **Sprint**: Sprint 3  
> **完成日期**: 2025-12-24  
> **状态**: ✅ 已完成

---

## 📋 功能概述

构建判断卡知识图谱，挖掘判断卡之间的关系（相似、依赖、演进），实现知识关联和可视化，支持学习路径推荐。

## 🎯 核心功能

### 1. 关系挖掘
- **相似关系**：基于问题域、标题、症状、排查路径计算相似度
- **依赖关系**：识别判断卡之间的依赖关系
- **演进关系**：识别判断卡的演进关系（版本演进）
- **相关关系**：识别相同问题域、不同步骤的相关判断卡

### 2. 相似度计算
- **算法**：多因子加权计算
  - 问题域匹配（权重30%）
  - 标题相似度（权重20%）
  - 症状相似度（权重40%）
  - 排查路径相似度（权重10%）
- **阈值**：相似度 ≥ 0.6 时创建相似关系

### 3. 学习路径推荐
- **基于问题域**：按问题域分组推荐学习路径
- **基于依赖关系**：考虑判断卡之间的依赖关系
- **基于难度**：根据判断卡使用情况评估难度

## 🏗️ 技术实现

### 后端实现

#### 1. 数据模型

**JudgementCardRelation 实体**：
```csharp
public class JudgementCardRelation
{
    public Guid RelationId { get; set; }
    public string SourceJcCode { get; set; }
    public string TargetJcCode { get; set; }
    public string RelationType { get; set; } // similar, depends_on, evolves_from, related_to
    public decimal RelationStrength { get; set; } // 0.0-1.0
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 2. 服务层

**IJudgementCardRelationService**：
- `MineRelationsAsync`: 挖掘指定判断卡的关系
- `CalculateSimilarityAsync`: 计算两个判断卡的相似度
- `MineAllRelationsAsync`: 批量挖掘所有判断卡关系
- `GetRelationsAsync`: 获取判断卡的关系列表
- `RecommendLearningPathsAsync`: 推荐学习路径

#### 3. API 端点

**判断卡关系管理**：
- `POST /api/judgement-cards/{jcCode}/mine-relations` - 挖掘判断卡关系
- `GET /api/judgement-cards/{jcCode}/relations` - 获取判断卡关系列表
- `GET /api/judgement-cards/similarity?jcCode1={code1}&jcCode2={code2}` - 计算相似度
- `POST /api/admin/judgement-cards/mine-all-relations` - 批量挖掘所有关系
- `GET /api/learning-paths/recommend?userId={userId}` - 推荐学习路径

### 数据库设计

**judgement_card_relations 表**：
```sql
CREATE TABLE judgement_card_relations (
    relation_id UUID PRIMARY KEY,
    source_jc_code VARCHAR(20) NOT NULL REFERENCES judgement_cards(jc_code),
    target_jc_code VARCHAR(20) NOT NULL REFERENCES judgement_cards(jc_code),
    relation_type VARCHAR(50) NOT NULL,
    relation_strength DECIMAL(3,2) DEFAULT 0.5,
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

**索引**：
- `idx_jc_relations_source` - source_jc_code
- `idx_jc_relations_target` - target_jc_code
- `idx_jc_relations_type` - relation_type

### 前端实现

**知识图谱可视化**：
- 已有 `KnowledgeGraphVisualization.tsx` 页面
- 使用 SimpleGraph 组件进行可视化
- 支持节点搜索和关系查看

## ✅ 验收标准

- [x] 可以挖掘判断卡之间的关系
- [x] 相似度计算功能实现（基于多因子加权）
- [x] 知识图谱可以可视化展示（已有页面）
- [x] 可以推荐学习路径
- [x] 学习路径基于问题域和依赖关系

## 🔄 使用场景

### 场景1：挖掘判断卡关系
1. 选择判断卡，触发关系挖掘
2. 系统自动计算与其他判断卡的相似度
3. 识别依赖关系和演进关系
4. 保存关系数据

### 场景2：查看判断卡关系
1. 在判断卡详情页面查看相关判断卡
2. 显示关系类型和强度
3. 可以跳转到相关判断卡

### 场景3：学习路径推荐
1. 用户查看推荐的学习路径
2. 按问题域分组展示
3. 显示学习步骤和难度

## 📝 后续优化建议

1. **关系挖掘优化**：
   - 使用更复杂的相似度算法（如余弦相似度、Jaccard相似度）
   - 基于使用历史挖掘关系
   - 使用机器学习模型识别关系

2. **学习路径优化**：
   - 基于用户能力评估推荐个性化路径
   - 考虑学习进度和掌握情况
   - 支持学习路径跟踪和反馈

3. **可视化增强**：
   - 使用更强大的图谱可视化库（如 D3.js、vis.js）
   - 支持交互式图谱操作
   - 支持关系筛选和过滤

4. **性能优化**：
   - 关系挖掘可以异步执行
   - 缓存关系数据
   - 支持增量更新

## 📝 相关文档

- [Issue #032 原始需求](.github/issues/sprint-3/032-判断卡知识图谱构建.md)
- [Issue #010 判断卡推荐引擎](../issues/ISSUE_010_IMPLEMENTATION_SUMMARY.md) - 相关功能
- [Issue #037 知识图谱构建](../issues/ISSUE_037_IMPLEMENTATION_SUMMARY.md) - 通用知识图谱

---

**实现完成日期**: 2025-12-24  
**实现人员**: AI Assistant  
**代码审查**: 待审查  
**测试状态**: 待测试

















