# v2.1 必选需求清单（Engineering-Centric）

> **版本**：2.1  
> **定位**：v2.1 不是"加功能"，而是"防系统退化、防经验流失、防形式主义"  
> **最后更新**：2025-12-22

## 📋 需求概述

v2.1 聚焦于**系统质量保障**和**经验沉淀**，确保系统不会退化为"好看但没用的填表工具"。

### 核心价值

| 优先级 | 数量 | 解决的核心问题 |
|--------|------|---------------|
| **P0** | 4 | 防形式主义、防错误数据、防经验流失 |
| **P1** | 3 | 提升效率、稳定客户感知 |
| **P2** | 2 | 管理科学化、人才梯队建设 |
| **P3** | 3 | 长期护城河、组织记忆 |

### 关键判断

> **v2.0 让系统"能用"；  
> v2.1 决定系统"不会坏"；  
> v2.2–v2.3 决定系统"是不是你们公司的长期资产"。**

---

## 🚨 P0（绝对必须｜否则系统必然失真）

> **不做 = 系统迟早变成"好看但没用的填表工具"**

### REQ-001: 工程判断卡质量评分（自动）

**需求ID**：REQ-001  
**优先级**：P0  
**阶段**：v2.1 起必须上线

#### 需求说明

系统对每张判断卡进行"结构与逻辑一致性"自动评分，不参与KPI，只用于：
- 主管 review
- AI 训练样本筛选

#### 核心规则示例

**评分维度**：

1. **完整性检查**
   - 有 `hypothesis` 但无 `eliminated_causes` → 降级
   - `confidence=5` 但 `evidence=0` → 标红
   - `suspected_domain` 与 `next_action` 冲突 → 标记异常

2. **逻辑一致性**
   - 判断边界（decision_boundary）是否完整
   - 关键检查项（key_checks）是否覆盖所有维度
   - 典型失效模式（failure_modes）是否有对应解决方案

3. **可验证性**
   - 下一步动作是否可验证
   - 验证清单是否完整
   - 验收标准是否明确

**评分算法**：
```csharp
public class JudgementCardQualityScore
{
    public int CompletenessScore { get; set; }  // 0-30分
    public int LogicConsistencyScore { get; set; }  // 0-30分
    public int VerifiabilityScore { get; set; }  // 0-20分
    public int EvidenceScore { get; set; }  // 0-20分
    
    public int TotalScore => CompletenessScore + LogicConsistencyScore + 
                            VerifiabilityScore + EvidenceScore;
    
    public QualityLevel Level => TotalScore switch
    {
        >= 80 => QualityLevel.Excellent,
        >= 60 => QualityLevel.Good,
        >= 40 => QualityLevel.Fair,
        _ => QualityLevel.Poor
    };
}
```

#### 不做后果

- 大量"看起来很专业"的空判断卡
- AI 学到错误模式
- 系统数据不可用
- 判断卡库质量持续下降

#### 技术实现

**涉及文件**：
- `backend/src/FieldTicket.Core/Services/IJudgementCardQualityService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/JudgementCardQualityService.cs`

**数据库扩展**：
```sql
ALTER TABLE judgement_cards ADD COLUMN quality_score INT;
ALTER TABLE judgement_cards ADD COLUMN quality_level VARCHAR(20);
ALTER TABLE judgement_cards ADD COLUMN quality_issues JSONB;
ALTER TABLE judgement_cards ADD COLUMN last_quality_check_at TIMESTAMPTZ;
```

**自动检查**：
- 判断卡创建/更新时自动评分
- 定期批量检查（每天）
- 低质量判断卡自动标记

---

### REQ-002: 判断轨迹版本化（Decision History）

**需求ID**：REQ-002  
**优先级**：P0  
**阶段**：v2.1 起必须上线

#### 需求说明

判断卡支持版本化（v1 / v2 / v3），每次修正必须填写"推翻原因"（change_reason），完整记录判断演进过程。

#### 核心功能

1. **版本管理**
   - 判断卡支持多版本
   - 版本号自动递增
   - 版本对比功能

2. **变更记录**
   - 每次修改必须填写"推翻原因"
   - 记录变更人、变更时间
   - 变更前后对比

3. **使用历史**
   - 记录每个版本的使用情况
   - 记录判断结果（正确/错误）
   - 计算版本成功率

#### 不做后果

- 经验无法沉淀
- 新人永远不知道"为什么之前判断错了"
- AI 只能学"结论"，学不到"过程"
- 无法追溯判断演进

#### 技术实现

**数据库设计**：
```sql
-- 判断卡版本表（已有，需扩展）
ALTER TABLE judgement_cards ADD COLUMN version INTEGER DEFAULT 1;
ALTER TABLE judgement_cards ADD COLUMN is_current BOOLEAN DEFAULT TRUE;
ALTER TABLE judgement_cards ADD COLUMN previous_version_id UUID;

-- 判断卡变更记录表（已有，需扩展）
ALTER TABLE jc_change_logs ADD COLUMN change_reason TEXT NOT NULL;
ALTER TABLE jc_change_logs ADD COLUMN overturned_by UUID REFERENCES users(id);
ALTER TABLE jc_change_logs ADD COLUMN overturned_at TIMESTAMPTZ;

-- 判断卡使用历史表
CREATE TABLE jc_usage_history (
    id BIGSERIAL PRIMARY KEY,
    jc_code VARCHAR(20) NOT NULL,
    jc_version INTEGER NOT NULL,
    ticket_id UUID REFERENCES tickets(ticket_id),
    used_by UUID REFERENCES users(id),
    used_at TIMESTAMPTZ DEFAULT NOW(),
    result VARCHAR(20),  -- 'correct', 'incorrect', 'partial'
    feedback TEXT
);
```

**业务规则**：
- 修改判断卡时，必须创建新版本
- 必须填写"推翻原因"（为什么修改）
- 旧版本保留，标记为 `is_current = false`
- 新工单默认使用最新版本

---

### REQ-003: 设备关键配置快照 + 差异提示

**需求ID**：REQ-003  
**优先级**：P0  
**阶段**：v2.1 起必须上线（哪怕只做关键字段）

#### 需求说明

设备交付配置、变更配置自动留痕，问题发生时自动提示：**当前设备 ≠ 标准配置（差异点）**

#### 核心功能

1. **配置快照**
   - 交付时记录标准配置
   - 每次变更记录配置快照
   - 配置历史可追溯

2. **差异检测**
   - 工单创建时自动检测配置差异
   - 显示差异点（哪些配置不同）
   - 提示可能的影响

3. **配置对比**
   - 当前配置 vs 标准配置
   - 当前配置 vs 上次快照
   - 可视化差异展示

#### 不做后果

- 工程判断长期建立在错误前提上
- 重复排查、误判极多
- 无法快速定位配置问题
- 新人完全不知道配置变更历史

#### 技术实现

**数据库设计**：
```sql
-- 设备配置快照表
CREATE TABLE device_config_snapshots (
    snapshot_id UUID PRIMARY KEY,
    device_id UUID NOT NULL REFERENCES devices(device_id),
    snapshot_type VARCHAR(20) NOT NULL,  -- 'delivery', 'change', 'problem'
    snapshot_at TIMESTAMPTZ NOT NULL,
    
    -- 配置内容（JSONB）
    config_json JSONB NOT NULL,
    /*
    {
      "sw_version": "v2.1.3",
      "plc_version": "v1.2.6",
      "param_version": "v3.4",
      "key_parameters": {
        "timeout_step_120": "300ms",
        "filter_time": "50ms"
      }
    }
    */
    
    -- 标准配置（用于对比）
    standard_config_json JSONB,
    
    -- 差异点
    differences JSONB,
    /*
    [
      {
        "field": "timeout_step_120",
        "standard": "800ms",
        "actual": "300ms",
        "impact": "可能导致超时"
      }
    ]
    */
    
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_config_snapshots_device ON device_config_snapshots(device_id, snapshot_at DESC);
```

**业务逻辑**：
```csharp
public async Task<List<ConfigDifference>> CheckConfigDifferencesAsync(
    Guid deviceId)
{
    var currentConfig = await GetCurrentConfigAsync(deviceId);
    var standardConfig = await GetStandardConfigAsync(deviceId);
    
    var differences = CompareConfigs(currentConfig, standardConfig);
    
    if (differences.Any())
    {
        // 自动创建快照
        await CreateSnapshotAsync(deviceId, "problem", currentConfig, differences);
    }
    
    return differences;
}
```

---

### REQ-004: 问题发生点自动关联"最近变更"

**需求ID**：REQ-004  
**优先级**：P0  
**阶段**：v2.1 起必须上线

#### 需求说明

问题时间 ±7 天内的程序升级、参数变更、换件记录自动置顶展示，帮助工程师快速定位可能原因。

#### 核心功能

1. **变更记录关联**
   - 自动查找问题时间 ±7 天内的变更
   - 变更类型：程序升级、参数变更、换件、配置修改
   - 按时间倒序展示

2. **相关性评分**
   - 计算变更与问题的相关性
   - 高相关性变更置顶
   - 显示变更详情和影响范围

3. **变更历史可视化**
   - 时间轴展示
   - 变更类型图标
   - 变更人信息

#### 不做后果

- 新人完全没有"时间敏感性"
- 老工程师要靠记忆兜底
- 无法快速定位问题根因
- 重复排查已知变更

#### 技术实现

**数据库设计**：
```sql
-- 设备变更记录表
CREATE TABLE device_change_logs (
    change_id UUID PRIMARY KEY,
    device_id UUID NOT NULL REFERENCES devices(device_id),
    change_type VARCHAR(50) NOT NULL,  -- 'program_upgrade', 'param_change', 'part_replacement', 'config_change'
    change_date TIMESTAMPTZ NOT NULL,
    
    -- 变更详情
    change_detail JSONB NOT NULL,
    /*
    {
      "type": "program_upgrade",
      "from_version": "v1.2.6",
      "to_version": "v1.2.7",
      "changed_by": "user_id",
      "reason": "修复超时问题"
    }
    */
    
    -- 影响范围
    impact_scope JSONB,
    
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_change_logs_device_date ON device_change_logs(device_id, change_date DESC);
```

**业务逻辑**：
```csharp
public async Task<List<RecentChange>> GetRecentChangesAsync(
    Guid ticketId)
{
    var ticket = await GetTicketAsync(ticketId);
    var problemDate = ticket.CreatedAt;
    
    var changes = await _changeLogRepository.FindRecentAsync(
        deviceId: ticket.DeviceId,
        fromDate: problemDate.AddDays(-7),
        toDate: problemDate.AddDays(7)
    );
    
    // 计算相关性
    var scoredChanges = changes.Select(c => new
    {
        Change = c,
        RelevanceScore = CalculateRelevanceScore(c, ticket)
    })
    .OrderByDescending(x => x.RelevanceScore)
    .ToList();
    
    return scoredChanges.Select(x => x.Change).ToList();
}
```

---

## ⚡ P1（强烈建议｜决定效率与专业度）

> **不做 = 系统能用，但效率和口碑起不来**

### REQ-005: "不可复现"工程化状态

**需求ID**：REQ-005  
**优先级**：P1  
**阶段**：v2.1

#### 需求说明

明确状态：`NOT_REPRODUCIBLE`，强制填写已尝试复现路径和假设触发条件，避免技术债被"关单"掩盖。

#### 核心功能

1. **状态定义**
   - 新增工单状态：`NOT_REPRODUCIBLE`
   - 状态说明：问题无法复现，但已记录相关信息

2. **必填信息**
   - 已尝试复现路径（步骤清单）
   - 假设触发条件（可能的原因）
   - 复现尝试次数
   - 最后一次尝试时间

3. **后续跟踪**
   - 支持重新打开（如果问题再次出现）
   - 关联到新工单（如果确认是同一问题）
   - 定期回访提醒（可选）

#### 不做后果

- 大量技术债被"关单"掩盖
- 后续再爆问题，毫无历史依据
- 无法统计不可复现问题比例
- 无法改进复现方法

#### 技术实现

**数据库设计**：
```sql
-- 扩展工单状态
ALTER TABLE tickets DROP CONSTRAINT IF EXISTS chk_ticket_status;
ALTER TABLE tickets ADD CONSTRAINT chk_ticket_status 
    CHECK (status IN ('Draft', 'Submitted', 'Triage', 'SolutionIssued', 
                      'Verifying', 'Closed', 'Reopened', 'NotReproducible'));

-- 不可复现记录表
CREATE TABLE non_reproducible_records (
    record_id UUID PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    
    -- 复现尝试
    attempts_made TEXT NOT NULL,  -- 已尝试的复现路径
    attempt_count INT NOT NULL,
    last_attempt_at TIMESTAMPTZ,
    
    -- 假设触发条件
    suspected_conditions TEXT,  -- 假设的触发条件
    
    -- 相关证据
    evidence_attachment_ids UUID[],
    
    recorded_by UUID REFERENCES users(id),
    recorded_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

### REQ-006: 潜在风险问题挂起（WATCHLIST）

**需求ID**：REQ-006  
**优先级**：P1  
**阶段**：v2.1

#### 需求说明

非生产阻断，但有潜在风险的问题，支持定期回访提醒和客户侧可见状态，避免"没事先关"导致后续责任不可追溯。

#### 核心功能

1. **挂起状态**
   - 新增工单状态：`WATCHLIST`
   - 状态说明：潜在风险，持续观察

2. **回访机制**
   - 定期回访提醒（如30天、60天）
   - 回访检查清单
   - 回访记录

3. **客户可见**
   - 客户可以看到"观察中"状态
   - 定期更新进展
   - 风险解除后关闭

#### 不做后果

- "没事先关"，一旦停线，责任不可追溯
- 客户感知差（问题悬而未决）
- 无法统计潜在风险问题
- 无法主动预防

#### 技术实现

**数据库设计**：
```sql
-- 扩展工单状态（已在REQ-005中定义）

-- 观察清单表
CREATE TABLE watchlist_items (
    item_id UUID PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    
    -- 风险描述
    risk_description TEXT NOT NULL,
    risk_level VARCHAR(20),  -- 'low', 'medium', 'high'
    
    -- 回访计划
    next_review_date DATE,
    review_interval_days INT DEFAULT 30,
    
    -- 回访记录
    review_count INT DEFAULT 0,
    last_reviewed_at TIMESTAMPTZ,
    
    -- 状态
    is_active BOOLEAN DEFAULT TRUE,
    resolved_at TIMESTAMPTZ,
    
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_watchlist_next_review ON watchlist_items(next_review_date) 
    WHERE is_active = TRUE;
```

---

### REQ-007: 对外口径结构完整性校验

**需求ID**：REQ-007  
**优先级**：P1  
**阶段**：v2.1

#### 需求说明

客户侧回复必须包含：当前进展、技术判断、措施、下一步时间点，确保客户感知稳定。

#### 核心功能

1. **结构校验**
   - 客户侧回复必须包含4个要素：
     - 当前进展
     - 技术判断
     - 措施
     - 下一步时间点

2. **模板约束**
   - 话术模板必须包含这4个要素
   - AI生成内容必须包含这4个要素
   - 手动输入时提示缺失项

3. **完整性检查**
   - 发送前自动检查
   - 缺失项高亮提示
   - 禁止发送不完整的消息

#### 不做后果

- 客户感知极不稳定
- 投诉、催单增加
- 沟通质量无法保证
- 品牌形象受损

#### 技术实现

**校验逻辑**：
```csharp
public class CommunicationStructureValidator
{
    public ValidationResult Validate(CommunicationDraft draft)
    {
        var errors = new List<string>();
        
        // 检查4个要素
        if (!ContainsCurrentProgress(draft.Content))
            errors.Add("缺少：当前进展");
        
        if (!ContainsTechnicalJudgement(draft.Content))
            errors.Add("缺少：技术判断");
        
        if (!ContainsMeasures(draft.Content))
            errors.Add("缺少：措施");
        
        if (!ContainsNextStep(draft.Content))
            errors.Add("缺少：下一步时间点");
        
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

---

## 📊 P2（中期价值｜管理与组织能力跃迁）

> **不做 = 管理只能靠感觉**

### REQ-008: 工程师隐性负载统计

**需求ID**：REQ-008  
**优先级**：P2  
**阶段**：v2.2

#### 需求说明

统计但不公开排名：被@次数、升级接手次数、判断被复用次数，用于识别核心工程师和隐性负载。

#### 核心功能

1. **负载指标**
   - 被@次数（协作负载）
   - 升级接手次数（救火负载）
   - 判断被复用次数（知识贡献）
   - 低置信度接手次数

2. **统计分析**
   - 个人负载报告（仅本人可见）
   - 团队负载分布（仅主管可见）
   - 负载趋势分析

3. **应用场景**
   - 调薪依据
   - 梯队建设
   - 工作分配优化

#### 不做后果

- 核心工程师被长期透支
- 调薪、梯队建设全靠主观
- 无法识别隐性负载
- 人才流失风险

#### 技术实现

**数据库设计**：
```sql
-- 工程师负载统计表
CREATE TABLE engineer_load_stats (
    stat_id UUID PRIMARY KEY,
    engineer_id UUID NOT NULL REFERENCES users(id),
    stat_date DATE NOT NULL,
    
    -- 负载指标
    mentioned_count INT DEFAULT 0,  -- 被@次数
    escalation_taken_count INT DEFAULT 0,  -- 升级接手次数
    judgement_reused_count INT DEFAULT 0,  -- 判断被复用次数
    low_confidence_taken_count INT DEFAULT 0,  -- 低置信度接手次数
    
    -- 工单指标
    tickets_assigned INT DEFAULT 0,
    tickets_closed INT DEFAULT 0,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(engineer_id, stat_date)
);

CREATE INDEX idx_load_stats_engineer_date ON engineer_load_stats(engineer_id, stat_date DESC);
```

---

### REQ-009: 新人成长曲线可视化

**需求ID**：REQ-009  
**优先级**：P2  
**阶段**：v2.2

#### 需求说明

可视化展示：判断卡质量随时间变化、confidence与最终正确率偏差、AI建议采纳率，用于评估培训效果。

#### 核心功能

1. **成长指标**
   - 判断卡质量趋势
   - 置信度准确性（confidence vs 实际正确率）
   - AI建议采纳率
   - 一次解决率趋势

2. **可视化**
   - 个人成长曲线
   - 与团队平均对比
   - 里程碑标记

3. **应用场景**
   - 培训效果评估
   - 个人发展计划
   - 晋升依据

#### 不做后果

- 无法判断培训是否有效
- "干得久 ≠ 干得好"
- 无法识别成长瓶颈
- 人才培养缺乏数据支撑

---

## 🏛️ P3（长期护城河｜组织记忆）

> **不做 = 三年后系统仍然要"靠老工程师救火"**

### REQ-010: 知识有效期与版本绑定

**需求ID**：REQ-010  
**优先级**：P3  
**阶段**：v2.3

#### 需求说明

知识条目绑定软件版本、硬件版本，超出范围提示"可能过期"，确保知识库可信度。

#### 核心功能

1. **版本绑定**
   - 知识条目关联软件版本范围
   - 知识条目关联硬件版本范围
   - 版本变更时提示知识可能过期

2. **有效期管理**
   - 知识有效期设置
   - 过期知识自动标记
   - 过期知识使用警告

3. **版本适配**
   - 查询时自动过滤版本不匹配的知识
   - 显示版本兼容性
   - 提示使用风险

#### 不做后果

- 知识库不敢用
- AI 输出可信度下降
- 版本不匹配导致误判
- 知识库维护成本高

---

### REQ-011: 知识来源可追溯（内部）

**需求ID**：REQ-011  
**优先级**：P3  
**阶段**：v2.3

#### 需求说明

每条判断型知识可追溯：来源工单、来源工程师、最后验证时间，确保知识可信度和可维护性。

#### 核心功能

1. **来源追溯**
   - 知识来源工单
   - 知识创建人
   - 知识验证人
   - 最后验证时间

2. **可信度评估**
   - 基于来源工程师经验
   - 基于验证次数
   - 基于使用效果

3. **维护机制**
   - 定期验证提醒
   - 失效知识标记
   - 知识更新流程

#### 不做后果

- 出问题找不到"懂的人"
- AI 无法加权可信样本
- 知识质量无法保证
- 知识维护困难

---

### REQ-012: 异常成功率与KPI反作弊预警

**需求ID**：REQ-012  
**优先级**：P3  
**阶段**：v2.3

#### 需求说明

监控：confidence长期偏高、一次解决率异常但重复问题率高，防止KPI驱动系统变形。

#### 核心功能

1. **异常检测**
   - confidence长期偏高（如平均>4.5）
   - 一次解决率高但重复问题率高
   - 结案速度快但返工率高

2. **预警机制**
   - 自动标记异常
   - 通知主管
   - 生成异常报告

3. **反作弊**
   - 检测数据异常模式
   - 识别可能的作弊行为
   - 保护数据真实性

#### 不做后果

- KPI 驱动系统变形
- 数据越来越假
- 无法真实反映问题
- 系统失去价值

---

## 📋 实施路线图

### v2.1（P0 + P1，3-4周）

**P0需求**（必须）：
- REQ-001: 判断卡质量评分
- REQ-002: 判断轨迹版本化
- REQ-003: 设备配置快照
- REQ-004: 最近变更关联

**P1需求**（强烈建议）：
- REQ-005: 不可复现状态
- REQ-006: 潜在风险挂起
- REQ-007: 口径结构校验

### v2.2（P2，2-3周）

- REQ-008: 工程师负载统计
- REQ-009: 新人成长曲线

### v2.3（P3，2-3周）

- REQ-010: 知识有效期绑定
- REQ-011: 知识来源追溯
- REQ-012: KPI反作弊预警

---

**最后更新**：2025-12-22  
**版本**：2.1



