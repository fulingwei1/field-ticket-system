# v1.0 → v2.0 迁移指南

本文档说明从v1.0到v2.0的架构变化和迁移步骤。

## 🔄 核心变化总结

### 1. 核心从"问题管理"迁移到"工程判断中台"

**v1.0**：工单是核心实体  
**v2.0**：判断卡是核心资产，工单是容器

**影响**：
- 判断卡库需要独立管理
- 工单必须关联判断卡才能结案
- 判断卡使用统计和效果评估

### 2. 新增问诊式补全

**v1.0**：提交时校验必填项  
**v2.0**：自动生成缺失信息清单，问诊式补全

### 3. 新增口径输出层

**v1.0**：自由沟通  
**v2.0**：统一话术模板，所有对外消息必须落库

### 4. 知识库拆分

**v1.0**：单一知识库  
**v2.0**：判断型（内部）+ 结论型（客户可见）

### 5. 强制责任归因

**v1.0**：可选  
**v2.0**：结案必须归因，阈值触发整改

### 6. AI作为副驾驶

**v1.0**：AI功能分散  
**v2.0**：AI在工程判断中台，输出必须带证据

## 📋 硬规则实施清单

### 规则1：无判断卡不得结案

**实施步骤**：
1. 修改 `TicketService.CloseTicketAsync()` 方法
2. 添加校验逻辑
3. 更新前端，结案按钮增加校验提示
4. 添加审计日志

### 规则2：低置信度自动升级

**实施步骤**：
1. 分诊时设置置信度字段
2. 添加自动升级逻辑
3. 添加主管通知
4. 前端显示升级状态

### 规则3：对外消息必须落库

**实施步骤**：
1. 创建 `CommunicationGenerationService`
2. 所有对外沟通API统一入口
3. 禁止直接调用外部API
4. 添加审计日志

### 规则4：结案必须归因

**实施步骤**：
1. 修改结案API，添加必填校验
2. 前端添加归因表单
3. 添加归因字段到数据库
4. 添加审计日志

### 规则5：重复问题阈值触发CAPA

**实施步骤**：
1. 实现相似工单查找算法
2. 实现阈值检测
3. 实现整改任务自动创建
4. 添加通知机制

## 🗄️ 数据库迁移

### 新增字段

```sql
-- tickets表新增字段
ALTER TABLE tickets ADD COLUMN current_hypothesis TEXT;
ALTER TABLE tickets ADD COLUMN next_action TEXT;
ALTER TABLE tickets ADD COLUMN confidence SMALLINT CHECK (confidence >= 1 AND confidence <= 5);
ALTER TABLE tickets ADD COLUMN escalation_required BOOLEAN DEFAULT FALSE;
ALTER TABLE tickets ADD COLUMN escalated_to UUID REFERENCES users(id);
ALTER TABLE tickets ADD COLUMN escalated_at TIMESTAMPTZ;

ALTER TABLE tickets ADD COLUMN root_cause VARCHAR(50);
ALTER TABLE tickets ADD COLUMN responsibility_team VARCHAR(50);
ALTER TABLE tickets ADD COLUMN is_preventable BOOLEAN;
ALTER TABLE tickets ADD COLUMN attributed_by UUID REFERENCES users(id);
ALTER TABLE tickets ADD COLUMN attributed_at TIMESTAMPTZ;

-- 添加约束：结案时必须有关键字段
ALTER TABLE tickets ADD CONSTRAINT chk_closed_requires_hypothesis 
    CHECK (
        (status != 'Closed' AND status != 'Resolved') OR 
        (current_hypothesis IS NOT NULL AND next_action IS NOT NULL)
    );

ALTER TABLE tickets ADD CONSTRAINT chk_closed_requires_attribution
    CHECK (
        (status != 'Closed' AND status != 'Resolved') OR 
        (root_cause IS NOT NULL AND responsibility_team IS NOT NULL AND is_preventable IS NOT NULL)
    );
```

### 数据迁移脚本

```sql
-- 为现有工单设置默认值
UPDATE tickets 
SET 
    current_hypothesis = COALESCE(current_hypothesis, '待补充'),
    next_action = COALESCE(next_action, '待补充'),
    confidence = COALESCE(confidence, 3)
WHERE status IN ('Triage', 'SolutionIssued', 'Verifying');
```

## 🔧 代码迁移步骤

### Step 1: 实施硬规则校验

1. 创建 `HardRuleValidator` 服务
2. 在所有关键操作点添加校验
3. 添加错误处理和审计日志

### Step 2: 更新工单结案流程

1. 修改 `CloseTicketAsync()` 方法
2. 添加判断卡校验
3. 添加归因校验
4. 更新前端结案表单

### Step 3: 实施问诊式补全

1. 创建 `MissingInfoAnalysisService`
2. 集成到工单创建流程
3. 更新移动端UI

### Step 4: 实施口径输出层

1. 创建话术模板库
2. 创建 `CommunicationGenerationService`
3. 统一所有对外沟通入口

### Step 5: 实施责任归因和CAPA

1. 添加归因字段和校验
2. 实现相似工单查找
3. 实现整改任务自动创建

## 📊 迁移检查清单

- [ ] 数据库迁移脚本执行
- [ ] 硬规则校验实施
- [ ] 工单结案流程更新
- [ ] 问诊式补全功能完成
- [ ] 口径输出层实施
- [ ] 责任归因功能完成
- [ ] CAPA自动触发功能完成
- [ ] 前端UI更新
- [ ] 移动端UI更新
- [ ] 测试所有硬规则
- [ ] 更新API文档
- [ ] 更新用户文档

---

**最后更新**：2025-12-22



