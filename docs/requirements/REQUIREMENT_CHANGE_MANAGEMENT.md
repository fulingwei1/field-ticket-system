# 需求变更管理模块设计

> **日期**：2025-12-22  
> **问题**：客户需求经常变化，导致测试设备调试耗费大量时间  
> **目标**：建立需求变更全生命周期管理，减少重复调试，提高效率

---

## 🎯 问题分析

### 当前痛点

1. **需求变更频繁**
   - 客户需求经常变化
   - 变更历史不清晰
   - 变更原因不明确

2. **调试时间浪费**
   - 重复调试相同问题
   - 变更导致的调试时间无法统计
   - 无法评估变更成本

3. **关联性缺失**
   - 需求变更与现场问题无关联
   - 无法追溯问题是否由变更引起
   - 变更影响范围不明确

---

## 💡 解决方案

### 核心思路

1. **需求变更全生命周期管理**
   - 变更申请 → 变更审批 → 变更实施 → 变更验证 → 变更归档
   - 每个环节都有记录和责任人

2. **变更与调试时间关联**
   - 记录每次变更导致的调试时间
   - 统计变更成本
   - 识别高频变更点

3. **变更与现场问题关联**
   - 自动关联变更后的现场问题
   - 分析变更是否导致问题
   - 评估变更风险

4. **变更影响分析**
   - 变更影响范围（设备、项目、客户）
   - 变更风险评估
   - 变更回滚能力

---

## 🏗️ 功能设计

### 1. 需求变更申请

**功能**：
- 创建变更申请
- 填写变更原因、变更内容、影响范围
- 上传变更文档（需求文档、设计文档等）
- 指定变更实施人

**数据结构**：
```sql
CREATE TABLE requirement_changes (
    change_id UUID PRIMARY KEY,
    change_code VARCHAR(50) UNIQUE NOT NULL,  -- REQ-YYYY-NNN
    project_id UUID REFERENCES projects(project_id),
    customer_id UUID REFERENCES customers(customer_id),
    
    -- 变更基本信息
    change_title VARCHAR(200) NOT NULL,
    change_type VARCHAR(50) NOT NULL,  -- 'functional', 'parameter', 'hardware', 'software'
    change_reason TEXT NOT NULL,  -- 变更原因
    change_description TEXT NOT NULL,  -- 变更描述
    change_priority VARCHAR(20) DEFAULT 'medium'  -- 'low', 'medium', 'high', 'urgent'
        CHECK (change_priority IN ('low', 'medium', 'high', 'urgent')),
    
    -- 变更内容
    change_content JSONB NOT NULL,
    /*
    {
      "affected_modules": ["module1", "module2"],
      "affected_devices": ["device_sn1", "device_sn2"],
      "before": {...},
      "after": {...},
      "impact_analysis": "..."
    }
    */
    
    -- 变更状态
    status VARCHAR(20) DEFAULT 'draft'
        CHECK (status IN ('draft', 'submitted', 'approved', 'in_progress', 'testing', 'completed', 'rejected', 'cancelled')),
    
    -- 变更流程
    submitted_by UUID REFERENCES users(id),
    submitted_at TIMESTAMPTZ,
    approved_by UUID REFERENCES users(id),
    approved_at TIMESTAMPTZ,
    assigned_to UUID REFERENCES users(id),  -- 实施人
    
    -- 变更实施
    implementation_started_at TIMESTAMPTZ,
    implementation_completed_at TIMESTAMPTZ,
    testing_started_at TIMESTAMPTZ,
    testing_completed_at TIMESTAMPTZ,
    
    -- 调试时间统计
    total_debug_time INTERVAL,  -- 总调试时间
    debug_sessions JSONB,  -- 调试会话记录
    /*
    [
      {
        "session_id": "uuid",
        "start_time": "2025-12-20T10:00:00Z",
        "end_time": "2025-12-20T12:00:00Z",
        "duration": "2 hours",
        "debugger": "user_id",
        "issues_found": ["issue1", "issue2"],
        "notes": "..."
      }
    ]
    */
    
    -- 变更验证
    verified_by UUID REFERENCES users(id),
    verified_at TIMESTAMPTZ,
    verification_result VARCHAR(20),  -- 'passed', 'failed', 'partial'
    verification_notes TEXT,
    
    -- 关联信息
    related_ticket_ids UUID[],  -- 关联的工单
    related_change_ids UUID[],  -- 关联的其他变更
    
    -- 审计字段
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    created_by UUID REFERENCES users(id)
);

CREATE INDEX idx_req_changes_project ON requirement_changes(project_id);
CREATE INDEX idx_req_changes_customer ON requirement_changes(customer_id);
CREATE INDEX idx_req_changes_status ON requirement_changes(status);
CREATE INDEX idx_req_changes_assigned ON requirement_changes(assigned_to);
CREATE INDEX idx_req_changes_date ON requirement_changes(created_at DESC);
```

### 2. 变更调试时间记录

**功能**：
- 记录每次调试会话
- 自动计算调试时间
- 统计调试成本

**数据结构**：
```sql
CREATE TABLE change_debug_sessions (
    session_id UUID PRIMARY KEY,
    change_id UUID NOT NULL REFERENCES requirement_changes(change_id),
    device_id UUID REFERENCES devices(device_id),
    
    -- 调试会话信息
    session_start TIMESTAMPTZ NOT NULL,
    session_end TIMESTAMPTZ,
    duration INTERVAL,  -- 自动计算
    
    -- 调试人员
    debugger_id UUID NOT NULL REFERENCES users(id),
    assistant_ids UUID[],  -- 协助人员
    
    -- 调试内容
    debug_type VARCHAR(50),  -- 'functional', 'performance', 'compatibility', 'integration'
    issues_found TEXT[],  -- 发现的问题
    solutions_applied TEXT[],  -- 应用的解决方案
    
    -- 调试结果
    status VARCHAR(20) DEFAULT 'in_progress'
        CHECK (status IN ('in_progress', 'completed', 'paused', 'cancelled')),
    result VARCHAR(20),  -- 'success', 'partial', 'failed'
    notes TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_debug_sessions_change ON change_debug_sessions(change_id);
CREATE INDEX idx_debug_sessions_device ON change_debug_sessions(device_id);
CREATE INDEX idx_debug_sessions_debugger ON change_debug_sessions(debugger_id);
```

### 3. 变更与现场问题关联

**功能**：
- 自动关联变更后的现场问题
- 分析问题是否由变更引起
- 评估变更风险

**业务逻辑**：
```csharp
public async Task<List<RelatedTicket>> GetChangeRelatedTicketsAsync(
    Guid changeId,
    int daysAfterChange = 30)
{
    var change = await GetChangeAsync(changeId);
    var affectedDevices = change.ChangeContent["affected_devices"];
    
    // 查找变更后30天内的工单
    var tickets = await _ticketRepository.GetTicketsAsync(
        deviceIds: affectedDevices,
        fromDate: change.ImplementationCompletedAt,
        toDate: change.ImplementationCompletedAt.AddDays(daysAfterChange)
    );
    
    // 计算关联度
    var relatedTickets = tickets.Select(ticket => new RelatedTicket
    {
        TicketId = ticket.TicketId,
        CorrelationScore = CalculateCorrelation(change, ticket),
        IsChangeRelated = CalculateCorrelation(change, ticket) > 0.7
    }).ToList();
    
    return relatedTickets;
}
```

### 4. 变更成本统计

**功能**：
- 统计变更总成本（人力、时间）
- 分析变更频率
- 识别高频变更点

**数据结构**：
```sql
CREATE TABLE change_cost_statistics (
    stat_id UUID PRIMARY KEY,
    change_id UUID NOT NULL REFERENCES requirement_changes(change_id),
    
    -- 时间成本
    total_debug_time INTERVAL,
    total_testing_time INTERVAL,
    total_implementation_time INTERVAL,
    
    -- 人力成本
    total_engineer_hours DECIMAL(10, 2),
    engineer_costs JSONB,  -- 按人员统计
    /*
    {
      "engineer1": {"hours": 8, "cost": 1000},
      "engineer2": {"hours": 4, "cost": 500}
    }
    */
    
    -- 变更成本
    change_cost DECIMAL(10, 2),  -- 总成本
    cost_breakdown JSONB,  -- 成本明细
    
    -- 统计时间
    calculated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### 5. 变更影响分析

**功能**：
- 分析变更影响范围
- 评估变更风险
- 提供变更建议

**数据结构**：
```sql
CREATE TABLE change_impact_analysis (
    analysis_id UUID PRIMARY KEY,
    change_id UUID NOT NULL REFERENCES requirement_changes(change_id),
    
    -- 影响范围
    affected_devices_count INT,
    affected_projects_count INT,
    affected_customers_count INT,
    
    -- 风险评估
    risk_level VARCHAR(20),  -- 'low', 'medium', 'high', 'critical'
    risk_factors JSONB,
    /*
    {
      "factors": [
        {"factor": "影响核心功能", "level": "high"},
        {"factor": "需要硬件改动", "level": "critical"}
      ]
    }
    */
    
    -- 建议
    recommendations TEXT[],
    rollback_plan TEXT,
    
    analyzed_at TIMESTAMPTZ DEFAULT NOW(),
    analyzed_by UUID REFERENCES users(id)
);
```

---

## 🔄 业务流程

### 变更申请流程

```
1. 创建变更申请
   ↓
2. 填写变更信息（原因、内容、影响范围）
   ↓
3. 提交审批
   ↓
4. 审批通过/拒绝
   ↓
5. 分配实施人
   ↓
6. 开始实施
   ↓
7. 记录调试时间
   ↓
8. 测试验证
   ↓
9. 完成变更
   ↓
10. 关联现场问题
   ↓
11. 统计变更成本
```

### 变更与问题关联流程

```
1. 变更完成
   ↓
2. 自动查找变更后30天内的工单
   ↓
3. 计算关联度
   ↓
4. 标记高关联度工单
   ↓
5. 分析问题是否由变更引起
   ↓
6. 更新变更风险评估
```

---

## 📊 统计与分析

### 1. 变更频率统计

- 按项目统计变更频率
- 按客户统计变更频率
- 按变更类型统计
- 识别高频变更点

### 2. 调试时间统计

- 按变更统计调试时间
- 按工程师统计调试时间
- 按设备统计调试时间
- 识别耗时最长的变更类型

### 3. 变更成本分析

- 变更总成本
- 变更成本趋势
- 变更ROI分析
- 识别高成本变更

### 4. 变更风险分析

- 变更导致的问题数量
- 变更成功率
- 变更回滚率
- 识别高风险变更

---

## 🎯 预期效果

### 短期效果（1-3个月）

1. **变更可追溯**
   - 所有变更都有记录
   - 变更历史清晰可查

2. **调试时间可统计**
   - 每次变更的调试时间可记录
   - 调试成本可量化

3. **问题可关联**
   - 变更后的问题可自动关联
   - 问题原因可追溯

### 长期效果（6-12个月）

1. **变更成本降低**
   - 识别高频变更点，提前预防
   - 优化变更流程，减少重复调试

2. **变更风险降低**
   - 基于历史数据评估风险
   - 提前识别高风险变更

3. **客户满意度提升**
   - 变更响应更快
   - 变更质量更高

---

## 🔗 与现有系统集成

### 1. 与工单系统集成

- 工单创建时自动检查最近变更
- 变更完成后自动关联相关工单
- 工单详情页显示关联变更

### 2. 与设备管理集成

- 变更自动更新设备配置
- 设备配置快照包含变更信息
- 设备版本历史包含变更记录

### 3. 与项目交付集成

- 变更自动同步到项目交付系统
- 项目交付系统可查看变更历史
- 变更影响项目交付计划

---

## 📝 实施建议

### Phase 1: 基础功能（Sprint 3）

- 变更申请和审批流程
- 调试时间记录
- 变更与工单关联

### Phase 2: 统计分析（Sprint 4）

- 变更成本统计
- 变更频率分析
- 变更风险分析

### Phase 3: 智能分析（Sprint 5）

- AI辅助变更风险评估
- 变更影响预测
- 变更建议生成

---

**最后更新**：2025-12-22


