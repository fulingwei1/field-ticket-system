# 问题管理模块 - 详细设计方案

## 一、模块概述

### 1.1 定义

问题管理模块是客服系统的核心，负责从问题的创建、分配、跟踪、到最终解决的全生命周期管理。基于你之前设计的企业微信工单系统（已有核心框架），本文在以下方面进行补充和完善：

- 完整的状态机和流程定义
- 详细的权限和角色设定
- 客服角色的功能补充
- 问题优先级和SLA管理
- 待办清单和提醒机制
- 与其他模块的完整关联

### 1.2 核心功能

| 功能域 | 主要功能 |
|-------|--------|
| **问题创建** | 现场上报、客服代建、自动导入 |
| **问题分配** | 智能分配、手工分配、技术主管审核 |
| **问题跟踪** | 进度更新、超时提醒、状态变更 |
| **问题诊断** | 追问、判断卡推荐、经验复用 |
| **问题解决** | 方案输出、验证执行、反馈确认 |
| **问题关闭** | 满意度评分、知识库入库、工单归档 |
| **工作管理** | 待办清单、绩效统计、工作量分析 |

### 1.3 参与角色

```
┌─────────────────────────────────────────────────┐
│ 客户 (Customer)                                  │
│ ├─ 描述问题                                      │
│ ├─ 确认解决方案                                  │
│ └─ 评分满意度                                    │
└────────────────┬────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────┐
│ 现场工程师 (Field Engineer)                      │
│ ├─ 上报问题（2分钟快速流程）                    │
│ ├─ 上传证据（照片、视频、日志）                 │
│ ├─ 执行验证方案                                 │
│ └─ 填写验证结果                                 │
└────────────────┬────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────┐
│ 客服 (CS)                                        │
│ ├─ 代建工单（可选）                             │
│ ├─ 跟进工单状态                                 │
│ ├─ 催办高级工程师                               │
│ ├─ 与客户沟通确认                               │
│ └─ 推送解决方案给现场                           │
└────────────────┬────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────┐
│ 高级工程师 (Senior Engineer)                     │
│ ├─ 接收分诊任务                                 │
│ ├─ 审查问题事实、发起追问                       │
│ ├─ 推荐判断卡、输出结论                         │
│ ├─ 创建和发布解决方案                           │
│ └─ 关闭工单                                     │
└────────────────┬────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────┐
│ 技术主管 (Tech Lead)                             │
│ ├─ 工单分诊审核                                 │
│ ├─ 资源协调                                     │
│ └─ 性能评估                                     │
└─────────────────────────────────────────────────┘
```

---

## 二、完整的数据库表结构

### 2.1 工单主表（tickets）- 核心业务表

```sql
-- 工单表（核心）
CREATE TABLE tickets (
    ticket_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_no VARCHAR(20) NOT NULL UNIQUE,       -- 工单编号 TK-YYYYMMDD-NNN
    
    -- 关联信息
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    project_id UUID NOT NULL REFERENCES projects(project_id),
    device_id UUID NOT NULL REFERENCES devices(device_id),
    station_id UUID REFERENCES stations(station_id),
    
    -- 创建者信息
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    created_by_role VARCHAR(20)                  -- FieldEngineer/CS/System
        CHECK (created_by_role IN ('FieldEngineer', 'CS', 'System')),
    
    -- 问题分类信息
    domain CHAR(1) NOT NULL                      -- 问题域：A/B/C/D/E
        CHECK (domain IN ('A', 'B', 'C', 'D', 'E')),
    step_code VARCHAR(20) NOT NULL,              -- 步骤代码：如 Step_120
    step_name VARCHAR(100),                      -- 步骤名称
    
    -- 问题描述
    symptom_title VARCHAR(200) NOT NULL,         -- 一句话症状描述
    symptom_detail TEXT,                         -- 详细症状描述
    
    -- 复现性信息
    repro_rate SMALLINT                          -- 复现率：0-100
        CHECK (repro_rate >= 0 AND repro_rate <= 100),
    reboot_recovers BOOLEAN,                     -- 重启是否恢复
    env_related BOOLEAN,                         -- 是否与环境相关
    env_description TEXT,                        -- 环境描述
    
    -- 版本信息（必填）
    sw_version VARCHAR(50) NOT NULL,
    plc_version VARCHAR(50) NOT NULL,
    param_version VARCHAR(50) NOT NULL,
    
    -- 结构化事实（YES/NO问卷结果）
    facts_json JSONB NOT NULL DEFAULT '{}',
    facts_completed_at TIMESTAMPTZ,              -- 事实问卷完成时间
    
    -- 状态与优先级
    status VARCHAR(20) NOT NULL DEFAULT 'Draft'
        CHECK (status IN ('Draft', 'Submitted', 'Triage', 'SolutionIssued', 'Verifying', 'Closed', 'Reopened')),
    
    priority VARCHAR(5) DEFAULT 'P3'
        CHECK (priority IN ('P1', 'P2', 'P3', 'P4')),
    
    -- 优先级设置规则（自动或手工）
    priority_auto_set_reason VARCHAR(100),       -- 自动设置原因（如：客户是VIP）
    priority_manual_changed BOOLEAN DEFAULT FALSE,
    priority_changed_by_user_id UUID REFERENCES users(id),
    priority_changed_at TIMESTAMPTZ,
    
    -- 分配信息
    assigned_to_user_id UUID REFERENCES users(id),  -- 当前分配给谁（通常是高级工程师）
    assigned_at TIMESTAMPTZ,
    assigned_by_user_id UUID REFERENCES users(id),  -- 分配人
    assignment_count INT DEFAULT 0,              -- 分配次数（重新分配的计数）
    
    -- 分诊结果
    current_jc_code VARCHAR(50),                 -- 当前关联的判断卡编号
    diagnosis_conclusion TEXT,                   -- 诊断结论
    root_cause VARCHAR(200),                     -- 根本原因
    diagnosed_at TIMESTAMPTZ,
    diagnosed_by_user_id UUID REFERENCES users(id),
    
    -- 解决方案关联
    solution_id UUID REFERENCES solutions(solution_id),
    solution_issued_at TIMESTAMPTZ,
    solution_issued_by_user_id UUID REFERENCES users(id),
    
    -- 验证信息
    verification_id UUID REFERENCES verifications(verification_id),
    verification_completed_at TIMESTAMPTZ,
    verification_result VARCHAR(20)              -- passed/failed/partial
        CHECK (verification_result IN ('passed', 'failed', 'partial', null)),
    
    -- SLA管理
    sla_response_minutes INT,                    -- 响应时间SLA（分钟）
    sla_resolve_hours INT,                       -- 解决时间SLA（小时）
    sla_response_deadline TIMESTAMPTZ,           -- 响应截止时间
    sla_resolve_deadline TIMESTAMPTZ,            -- 解决截止时间
    sla_response_breached BOOLEAN DEFAULT FALSE, -- 是否超过响应SLA
    sla_resolve_breached BOOLEAN DEFAULT FALSE,  -- 是否超过解决SLA
    
    -- 客户反馈
    customer_feedback TEXT,                      -- 客户反馈内容
    satisfaction_score SMALLINT                  -- 满意度评分：1-5
        CHECK (satisfaction_score IS NULL OR (satisfaction_score >= 1 AND satisfaction_score <= 5)),
    satisfaction_comment TEXT,                   -- 满意度评论
    satisfied_at TIMESTAMPTZ,
    
    -- 返工处理
    rework_count INT DEFAULT 0,                  -- 返工次数
    last_rework_at TIMESTAMPTZ,
    last_rework_reason VARCHAR(200),
    
    -- 关闭信息
    closed_at TIMESTAMPTZ,
    closed_by_user_id UUID REFERENCES users(id),
    close_reason VARCHAR(50)                     -- resolved/duplicate/invalid/cancelled
        CHECK (close_reason IN ('resolved', 'duplicate', 'invalid', 'cancelled', null)),
    
    -- 时间统计
    first_response_at TIMESTAMPTZ,               -- 首次响应时间
    first_response_minutes INT,                  -- 首次响应耗时（分钟）
    total_resolve_hours DECIMAL(10, 2),          -- 总解决耗时（小时）
    
    -- 审计字段
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE             -- 逻辑删除
);

-- 创建索引
CREATE INDEX idx_tickets_status ON tickets(status);
CREATE INDEX idx_tickets_priority ON tickets(priority);
CREATE INDEX idx_tickets_customer ON tickets(customer_id);
CREATE INDEX idx_tickets_device ON tickets(device_id);
CREATE INDEX idx_tickets_assigned_to ON tickets(assigned_to_user_id);
CREATE INDEX idx_tickets_created_at ON tickets(created_at DESC);
CREATE INDEX idx_tickets_deadline ON tickets(sla_response_deadline, sla_resolve_deadline);
CREATE INDEX idx_tickets_no ON tickets(ticket_no);
```

### 2.2 追问和沟通表

```sql
-- 工单追问表（高级工程师在分诊时发起的追问）
CREATE TABLE ticket_inquiries (
    inquiry_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    inquiry_no VARCHAR(20) NOT NULL,             -- 追问编号：TK-001-INQ-001
    
    inquiry_type VARCHAR(20) NOT NULL           -- 问卷型/自由型
        CHECK (inquiry_type IN ('questionnaire', 'free_form')),
    
    -- 问卷型追问
    jc_code VARCHAR(50),                         -- 关联的判断卡编号
    question_json JSONB,                         -- 问卷内容和结构
    
    -- 自由型追问
    inquiry_title VARCHAR(200),
    inquiry_content TEXT,
    
    status VARCHAR(20) DEFAULT 'Pending'         -- Pending/InProgress/Answered/Expired
        CHECK (status IN ('Pending', 'InProgress', 'Answered', 'Expired')),
    
    -- 追问者
    inquired_by_user_id UUID NOT NULL REFERENCES users(id),
    inquired_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- 回答者
    answered_by_user_id UUID REFERENCES users(id),
    answered_at TIMESTAMPTZ,
    answer_json JSONB,                          -- 回答内容
    answer_text TEXT,
    
    -- 期限管理
    deadline TIMESTAMPTZ NOT NULL,              -- 期望回答期限
    is_overdue BOOLEAN DEFAULT FALSE,
    
    -- 重要程度
    is_blocking BOOLEAN DEFAULT FALSE,          -- 是否阻塞工单进度
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_inquiries_ticket ON ticket_inquiries(ticket_id);
CREATE INDEX idx_inquiries_status ON ticket_inquiries(status);
CREATE INDEX idx_inquiries_deadline ON ticket_inquiries(deadline);

-- 工单评论表（内部讨论）
CREATE TABLE ticket_comments (
    comment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    
    commented_by_user_id UUID NOT NULL REFERENCES users(id),
    comment_type VARCHAR(20) DEFAULT 'normal'   -- normal/mention/@全部/system
        CHECK (comment_type IN ('normal', 'mention', 'broadcast', 'system')),
    
    content TEXT NOT NULL,
    mentions TEXT,                               -- @的用户ID列表（逗号分隔）
    
    -- 关联附件
    attachment_ids TEXT,                         -- 附件ID列表
    
    is_internal BOOLEAN DEFAULT TRUE,            -- 是否仅内部可见（客户看不到）
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_comments_ticket ON ticket_comments(ticket_id);
CREATE INDEX idx_comments_created_at ON ticket_comments(created_at DESC);
```

### 2.3 解决方案表

```sql
-- 解决方案表（SOL）
CREATE TABLE solutions (
    solution_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    solution_code VARCHAR(50) NOT NULL UNIQUE,  -- SOL编号：SOL-2025-001
    
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE RESTRICT,
    
    -- 方案基本信息
    title VARCHAR(200) NOT NULL,                -- 方案标题
    description TEXT NOT NULL,                  -- 方案描述
    
    -- 方案分类
    solution_type VARCHAR(50) NOT NULL          -- software_update/parameter_change/hardware_replacement/procedure
        CHECK (solution_type IN ('software_update', 'parameter_change', 'hardware_replacement', 'procedure')),
    
    -- 影响范围
    applicable_models TEXT,                     -- 适用设备型号（逗号分隔）
    affected_devices INT,                       -- 影响设备数
    
    -- 版本信息
    required_sw_version VARCHAR(50),
    required_plc_version VARCHAR(50),
    required_param_version VARCHAR(50),
    
    new_sw_version VARCHAR(50),                 -- 更新后版本
    new_plc_version VARCHAR(50),
    new_param_version VARCHAR(50),
    
    -- 版本包关联
    sw_package_id UUID REFERENCES version_packages(package_id),
    plc_package_id UUID REFERENCES version_packages(package_id),
    param_package_id UUID REFERENCES version_packages(package_id),
    
    -- 详细步骤
    implementation_steps TEXT,                  -- 实施步骤（可以是HTML或Markdown）
    estimated_implementation_time INT,          -- 预计实施时间（分钟）
    
    -- 风险评估
    risk_level VARCHAR(20) DEFAULT 'medium'     -- low/medium/high
        CHECK (risk_level IN ('low', 'medium', 'high')),
    risk_description TEXT,                      -- 风险描述
    
    -- 回滚方案
    rollback_possible BOOLEAN DEFAULT TRUE,
    rollback_procedure TEXT,
    
    -- 验证清单
    verification_checklist JSONB,               -- 验证步骤和检查点
    
    -- 版本管理
    version_number INT DEFAULT 1,               -- 方案版本号
    is_current BOOLEAN DEFAULT TRUE,            -- 是否为当前版本
    previous_version_id UUID REFERENCES solutions(solution_id),
    change_reason TEXT,
    
    -- 状态
    status VARCHAR(20) DEFAULT 'draft'          -- draft/proposed/approved/published/archived
        CHECK (status IN ('draft', 'proposed', 'approved', 'published', 'archived')),
    
    approved_by_user_id UUID REFERENCES users(id),
    approved_at TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_solutions_code ON solutions(solution_code);
CREATE INDEX idx_solutions_ticket ON solutions(ticket_id);
CREATE INDEX idx_solutions_status ON solutions(status);
CREATE INDEX idx_solutions_published_at ON solutions(published_at DESC);
```

### 2.4 验证表

```sql
-- 验证执行表
CREATE TABLE verifications (
    verification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    solution_id UUID NOT NULL REFERENCES solutions(solution_id),
    
    verification_no VARCHAR(20) NOT NULL UNIQUE,  -- VER-2025-001
    
    -- 验证计划
    planned_verification_date TIMESTAMPTZ,
    planned_verification_location VARCHAR(200),   -- 现场地址
    
    -- 验证执行
    verified_by_user_id UUID NOT NULL REFERENCES users(id),
    verification_started_at TIMESTAMPTZ,
    verification_completed_at TIMESTAMPTZ,
    actual_verification_time INT,                 -- 实际耗时（分钟）
    
    -- 验证清单执行（与方案中的清单对应）
    checklist_json JSONB,                         -- 每个检查点的执行结果
    
    -- 验证结果
    result VARCHAR(20) NOT NULL                   -- passed/failed/partial
        CHECK (result IN ('passed', 'failed', 'partial')),
    result_summary TEXT,                          -- 结果总结
    
    -- 如果失败或部分通过
    failure_reason TEXT,
    failure_evidence_ids TEXT,                    -- 失败证据的附件ID
    
    -- 发现的问题
    new_issues_found TEXT,                        -- 验证过程中发现的新问题
    
    -- 签名确认（可选，用于现场签字）
    signed_by_customer_name VARCHAR(100),         -- 客户代表签名
    signed_at TIMESTAMPTZ,
    signature_data BYTEA,                         -- 电子签名
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_verifications_ticket ON verifications(ticket_id);
CREATE INDEX idx_verifications_solution ON verifications(solution_id);
CREATE INDEX idx_verifications_result ON verifications(result);
```

### 2.5 优先级规则表

```sql
-- 优先级规则定义（用于自动设置优先级）
CREATE TABLE priority_rules (
    rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_name VARCHAR(100) NOT NULL,
    
    -- 规则条件（任何一个满足就触发）
    condition_type VARCHAR(50) NOT NULL         -- customer_service_level/device_impact/environment/custom
        CHECK (condition_type IN ('customer_service_level', 'device_impact', 'environment', 'custom')),
    
    condition_value JSONB,                      -- 条件的具体值
    /*
    示例：
    {
        "service_level": "vip",                 -- 如果客户是VIP
        "impact": "production_line_stop"        -- 如果影响生产线停线
    }
    */
    
    result_priority VARCHAR(5) NOT NULL         -- P1/P2/P3/P4
        CHECK (result_priority IN ('P1', 'P2', 'P3', 'P4')),
    
    description TEXT,
    
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 创建默认规则
INSERT INTO priority_rules (rule_name, condition_type, condition_value, result_priority, description) VALUES
('VIP客户问题', 'customer_service_level', '{"service_level": "vip"}', 'P1', '客户是VIP，直接P1'),
('生产线停线', 'device_impact', '{"impact": "production_stop", "duration_hours": 0}', 'P1', '生产线已停线，直接P1'),
('生产线影响', 'device_impact', '{"impact": "production_impact"}', 'P2', '影响生产，设为P2'),
('非生产设备', 'device_impact', '{"impact": "non_critical"}', 'P4', '非关键设备，设为P4');
```

### 2.6 SLA规则表

```sql
-- SLA规则定义
CREATE TABLE sla_rules (
    sla_rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_name VARCHAR(100) NOT NULL,
    
    -- 规则匹配条件
    match_priority VARCHAR(5),                  -- 如果优先级是...
    match_domain VARCHAR(1),                    -- 如果问题域是...
    match_service_level VARCHAR(20),            -- 如果客户等级是...
    
    -- SLA指标
    response_minutes INT NOT NULL,              -- 响应时间（分钟）
    resolve_hours INT NOT NULL,                 -- 解决时间（小时）
    
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 创建默认SLA
INSERT INTO sla_rules (rule_name, match_priority, match_service_level, response_minutes, resolve_hours) VALUES
('P1-VIP客户', 'P1', 'vip', 30, 4),
('P1-标准客户', 'P1', 'standard', 60, 8),
('P2-VIP客户', 'P2', 'vip', 60, 24),
('P2-标准客户', 'P2', 'standard', 120, 48),
('P3-所有客户', 'P3', null, 240, 120),
('P4-所有客户', 'P4', null, 480, 240);
```

### 2.7 待办清单视图（虚拟表）

```sql
-- 创建视图：客服的待办清单
CREATE VIEW cs_todo_list AS
SELECT 
    t.ticket_id,
    t.ticket_no,
    t.customer_id,
    c.customer_name,
    t.device_id,
    d.device_sn,
    t.status,
    t.priority,
    
    -- 超时状态
    CASE 
        WHEN t.sla_response_breached THEN 'response_timeout'
        WHEN t.sla_resolve_breached THEN 'resolve_timeout'
        ELSE 'normal'
    END as timeout_status,
    
    -- 待办类型
    CASE 
        WHEN t.status = 'Submitted' THEN '待分诊'
        WHEN t.status = 'Triage' AND t.assigned_to_user_id IS NULL THEN '待分配'
        WHEN t.status = 'SolutionIssued' THEN '待通知客户'
        WHEN t.status = 'Verifying' THEN '待验证确认'
        ELSE '其他'
    END as todo_type,
    
    t.created_at,
    t.sla_response_deadline,
    t.sla_resolve_deadline,
    EXTRACT(MINUTE FROM (t.sla_response_deadline - NOW())) as minutes_to_response_deadline
    
FROM tickets t
LEFT JOIN customers c ON t.customer_id = c.customer_id
LEFT JOIN devices d ON t.device_id = d.device_id
WHERE t.status IN ('Submitted', 'Triage', 'SolutionIssued', 'Verifying')
    AND t.is_deleted = FALSE
ORDER BY 
    CASE WHEN t.sla_response_breached THEN 0 ELSE 1 END,  -- 超时优先
    t.priority,
    t.created_at;

-- 创建视图：工程师的待办清单
CREATE VIEW engineer_todo_list AS
SELECT 
    t.ticket_id,
    t.ticket_no,
    t.customer_id,
    c.customer_name,
    t.device_id,
    d.device_sn,
    t.status,
    t.priority,
    t.assigned_to_user_id,
    u.name as assigned_engineer,
    
    CASE 
        WHEN t.status = 'Triage' THEN '待诊断'
        WHEN t.status = 'Triage' AND t.current_jc_code IS NOT NULL THEN '待输出结论'
        WHEN t.status = 'SolutionIssued' THEN '待验证执行'
        ELSE '其他'
    END as todo_type,
    
    t.created_at,
    t.sla_resolve_deadline,
    EXTRACT(HOUR FROM (t.sla_resolve_deadline - NOW())) as hours_to_deadline
    
FROM tickets t
LEFT JOIN customers c ON t.customer_id = c.customer_id
LEFT JOIN devices d ON t.device_id = d.device_id
LEFT JOIN users u ON t.assigned_to_user_id = u.id
WHERE t.status IN ('Triage', 'SolutionIssued', 'Verifying')
    AND t.is_deleted = FALSE
ORDER BY 
    t.priority,
    t.sla_resolve_deadline;
```

---

## 三、完整的状态机定义

### 3.1 状态转移图

```
┌──────────┐
│  Draft   │  (仅创建者可见)
└────┬─────┘
     │ 创建者点击"提交"
     ↓
┌──────────────┐
│  Submitted   │  (进入工作队列)
└────┬─────────┘
     │ 自动或手工分配给高级工程师
     ↓
┌──────────────┐
│   Triage     │  (诊断阶段)
│              │
│ 可能的操作：  │
│ 1. 发起追问   │
│ 2. 使用判断卡 │
│ 3. 输出结论   │
└────┬─────────┘
     │ 输出诊断结论和根本原因
     ↓
┌──────────────────┐
│ SolutionIssued   │  (方案已发布)
│                  │
│ 可能的操作：      │
│ 1. 现场推送方案   │
│ 2. 等待现场验证   │
└────┬─────────────┘
     │ 现场完成验证
     ↓
┌──────────────┐
│  Verifying   │  (验证阶段)
└──┬─────────┬─┘
   │         │
   │ 验证失败 │ 验证通过
   │         │
   ↓         ↓
┌────────────┐  ┌──────────┐
│ Reopened   │  │  Closed  │  (工单结束)
│ (返工)     │  │          │
└────────────┘  └──────────┘
```

### 3.2 状态定义与权限

| 状态 | 说明 | 谁能看 | 谁能操作 | 可执行操作 |
|-----|------|-------|--------|----------|
| **Draft** | 草稿状态 | 仅创建者 | 创建者 | 编辑、提交、删除 |
| **Submitted** | 已提交，待分诊 | 创建者、管理员 | 客服、管理员 | 补充信息、分配、催办 |
| **Triage** | 分诊中，诊断阶段 | 所有相关人 | 高级工程师、技术主管 | 发起追问、推荐判断卡、输出结论 |
| **SolutionIssued** | 方案已发布 | 所有相关人 | 客服、工程师 | 推送方案给现场、催促验证 |
| **Verifying** | 现场验证中 | 所有相关人 | 现场工程师、客服 | 执行验证、上传结果、反馈结果 |
| **Closed** | 工单已关闭 | 所有相关人 | 高级工程师、管理员 | 查看、重新打开（Admin） |
| **Reopened** | 返工（重新分诊） | 所有相关人 | 高级工程师、技术主管 | 继续诊断、输出新方案 |

### 3.3 状态转移权限矩阵

```
当前状态 → 目标状态    | FieldEngineer | CS | SeniorEng | TechLead | Admin
─────────────────────────────────────────────────────────────────────
Draft → Submitted      | ✓            | ✓  | -         | -        | ✓
Submitted → Triage     | -            | ✓  | ✓         | ✓        | ✓
Triage → SolutionIssued| -            | -  | ✓         | ✓        | ✓
SolutionIssued → Verifying | ✓        | -  | ✓         | -        | ✓
Verifying → Closed     | ✓            | -  | ✓         | ✓        | ✓
Any → Reopened         | -            | -  | ✓         | ✓        | ✓
Closed → Reopened      | -            | -  | -         | -        | ✓
```

---

## 四、优先级和SLA管理

### 4.1 优先级确定流程

```
新工单创建
   ↓
自动评估优先级（基于规则）
   ↓
┌─────────────────────────────────┐
│ 检查以下因素（顺序）             │
├─────────────────────────────────┤
│ 1. 客户服务等级（VIP → P1）     │
│ 2. 设备影响程度                 │
│    - 生产线停线 → P1             │
│    - 生产受影响 → P2             │
│    - 测试设备 → P3              │
│    - 非关键设备 → P4            │
│ 3. 时间敏感性                   │
│ 4. 自定义规则                   │
└─────────────────────────────────┘
   ↓
设置优先级 + 对应的SLA
   ↓
技术主管可手工调整优先级
   ↓
系统计算响应/解决截止时间
```

### 4.2 默认SLA规则

| 优先级 | VIP客户 | 标准客户 | 备注 |
|-------|--------|---------|------|
| **P1** | 30分钟响应<br>4小时解决 | 1小时响应<br>8小时解决 | 生产线停线等紧急情况 |
| **P2** | 1小时响应<br>24小时解决 | 2小时响应<br>48小时解决 | 生产有影响 |
| **P3** | 4小时响应<br>5天解决 | 4小时响应<br>5天解决 | 一般问题 |
| **P4** | 8小时响应<br>10天解决 | 8小时响应<br>10天解决 | 非关键问题 |

### 4.3 SLA计算和提醒

```sql
-- 计算SLA是否超期（触发器或定时任务）
UPDATE tickets
SET 
    sla_response_breached = CASE 
        WHEN first_response_at IS NULL AND NOW() > sla_response_deadline THEN TRUE
        ELSE FALSE
    END,
    sla_resolve_breached = CASE 
        WHEN status NOT IN ('Closed') AND NOW() > sla_resolve_deadline THEN TRUE
        ELSE FALSE
    END,
    updated_at = NOW()
WHERE status IN ('Submitted', 'Triage', 'SolutionIssued', 'Verifying');

-- 触发提醒通知（通过消息推送）
-- 1. 响应超期：通知客服催办
-- 2. 解决超期：通知技术主管 + 客服催办
-- 3. 即将超期（提前1小时）：预警通知
```

---

## 五、工作流程详解

### 5.1 现场工程师上报流程（2分钟快速流程）

```
1. 打开小程序
2. 点击"新建工单"
3. 扫描设备二维码（或搜索客户/设备）
   ↓ 自动填充：customer_id, project_id, device_id, device_model, 
              current_sw_version, current_plc_version, current_param_version
4. 选择问题域（A/B/C/D/E）
   ↓ 自动推荐该域下的常见步骤
5. 选择步骤（Step_120等）
6. 一句话描述症状（symptom_title）
   ↓ 自动计算初步优先级
7. 选择复现率（0%, 25%, 50%, 75%, 100%）
8. 是否重启恢复？（是/否）
9. 上传证据（照片、视频）- 可选，可离线保存
10. 点击"提交"
    ↓ 生成工单编号 TK-YYYYMMDD-NNN
    ↓ 工单状态 = Submitted
    ↓ 触发自动分配流程
```

**时间目标**：整个流程 < 2分钟

### 5.2 客服跟进流程

```
工单Submitted后，客服看到待办清单：

┌─────────────────────────────┐
│ 我的待办 - 15项              │
├─────────────────────────────┤
│ 🔴 超时待处理 (3)             │
│   [TK-001] 生产线停线，超期待分诊 → 催高工
│   [TK-002] 方案已出，超期未通知客户 → 推送方案
│   [TK-003] 等待验证超期 → 催现场工程师
│                             │
│ 🟡 今日待处理 (5)             │
│   [TK-010] 已分诊，待客户反馈
│   [TK-011] 方案已发，等验证
│   ...
│                             │
│ 🟢 正常进行中 (7)             │
│   [TK-020] 刚上报，自动分诊中
│   ...
└─────────────────────────────┘

客服点击某个工单，进入详情：

┌───────────────────────────────────┐
│ 工单 TK-001                        │
├───────────────────────────────────┤
│ 客户：某新能源汽车                 │
│ 设备：ATM-2000-01                  │
│ 症状：停线问题                     │
│ 优先级：P1 (VIP客户)              │
│ 状态：Submitted (待分诊)           │
│                                  │
│ 时间轴：                           │
│ ├─ 创建：10:00                    │
│ ├─ 自动分配给：高工张三            │
│ └─ (未响应，已超期)               │
│                                  │
│ 操作：                             │
│ [重新分配] [催办高工] [备注]       │
└───────────────────────────────────┘
```

### 5.3 高级工程师诊断流程

```
高工看到分诊待办：

┌──────────────────────────────────┐
│ 诊断待办 - 8项                    │
├──────────────────────────────────┤
│ P1级 (2)：                        │
│   TK-001 - 生产线停线问题          │
│   TK-002 - VIP客户反馈停线         │
│ P2级 (4)：...                     │
│ P3级 (2)：...                     │
└──────────────────────────────────┘

高工点击工单，进入分诊面板：

┌────────────────────────────────────┐
│ 分诊面板 - TK-001                  │
├────────────────────────────────────┤
│ 问题：设备频繁停线                 │
│ 现象：重启可恢复                   │
│                                   │
│ 推荐判断卡：                       │
│ ┌──────────────────────┐          │
│ │ JC-120-1: 软件异常卡 │ 推荐度99% │
│ │ JC-120-2: 硬件异常卡 │ 推荐度30% │
│ │ JC-120-3: 环境问题卡 │ 推荐度15% │
│ └──────────────────────┘          │
│                                   │
│ 选择判断卡后，展示问卷：           │
│ ┌──────────────────────┐          │
│ │ Q1: 停线前有错误日志? │ Y/N/不清 │
│ │ Q2: 重启立即恢复?    │ Y/N      │
│ │ Q3: 重启后立即再发生? │ Y/N      │
│ └──────────────────────┘          │
│                                   │
│ 或发起追问给现场工程师：           │
│ [发起追问] 需要以下信息：          │
│ - 完整的错误日志                   │
│ - 停线时间间隔                     │
│ - 当前PLC版本                      │
│                                   │
│ 输出结论（所有信息齐全后）：       │
│ 根本原因：PLC V2.0.1存在逻辑错误   │
│ 建议方案：升级到V2.0.2              │
│ [发布解决方案]                     │
└────────────────────────────────────┘
```

### 5.4 现场验证流程

```
工单状态变为 SolutionIssued 后，现场工程师收到通知：

小程序推送：
"方案已发布：TK-001 - 软件版本升级到V2.0.2"

现场工程师点击查看方案：

┌─────────────────────────────────┐
│ 解决方案 - SOL-2025-001          │
├─────────────────────────────────┤
│ 标题：PLC软件版本升级             │
│ 风险等级：中                      │
│                                 │
│ 实施步骤：                       │
│ 1. 备份当前参数                  │
│ 2. 停止生产线                    │
│ 3. 下载新版本文件                │
│ 4. 通过USB导入新PLC版本          │
│ 5. 验证系统启动                  │
│                                 │
│ 回滚方案：如失败，可恢复旧版本   │
│                                 │
│ 验证清单：                       │
│ ☐ 系统能正常启动                │
│ ☐ 控制逻辑正确执行              │
│ ☐ 运行2小时无异常               │
│ ☐ 通讯正常                      │
│                                 │
│ [开始验证]                       │
└─────────────────────────────────┘

点击"开始验证"后，进入验证执行：

┌──────────────────────────────────┐
│ 验证执行 - VER-2025-001           │
├──────────────────────────────────┤
│ 验证人：现场工程师张三            │
│ 验证日期：2025-01-20             │
│ 验证地点：广东深圳龙岗区...      │
│                                  │
│ 执行清单：                       │
│ ☐ 系统能正常启动                 │
│   [✓标记] 上传证据（截图）      │
│ ☐ 控制逻辑正确执行               │
│   [执行中...] 预计15:30完成      │
│ ☐ 运行2小时无异常                │
│ ☐ 通讯正常                       │
│                                  │
│ 验证结果：                       │
│ ◯ 通过                            │
│ ◯ 失败                            │
│ ◯ 部分通过                       │
│                                  │
│ [上传最终证据] [提交验证结果]     │
└──────────────────────────────────┘

验证完成后：
- 工单状态变为 Verifying
- 高级工程师收到验证结果通知
- 如果通过：高级工程师审核后关闭工单
- 如果失败：工单返工（状态 = Reopened），高级工程师重新诊断
```

---

## 六、客服端详细功能设计

### 6.1 客服待办清单页面

**页面名称**：我的工作台

**布局**：
```
┌─────────────────────────────────────────────┐
│ 我的工作台                                   │
├─────────────────────────────────────────────┤
│ 筛选：[优先级] [状态] [日期]  [搜索...]     │
├─────────────────────────────────────────────┤
│                                             │
│ 🔴 紧急待处理 (3)                           │
│ ──────────────────────────────────────────│
│ │ TK-001  │ ATM-2000  │ 停线       │ 10:30 │
│ │ 某车企  │ P1 P1超期  │ 待催高工   │ 超4小时
│ │ TK-002  │ ATM-2000  │ 停线       │ 11:00 │
│ │ 某车企  │ P1 P1超期  │ 等方案     │ 超2小时
│                                             │
│ 🟡 今日待处理 (8)                           │
│ ──────────────────────────────────────────│
│ │ TK-010  │ ATM-1000  │ 卡顿问题   │ 14:20 │
│ │ 某3C企  │ P2        │ 等验证     │ 正常  │
│                                             │
│ 🟢 本周待处理 (15)                          │
│ ──────────────────────────────────────────│
│ │ TK-020  │ XXX       │ ...       │ ...   │
│
└─────────────────────────────────────────────┘
```

**功能**：
- 实时筛选和排序
- 点击工单进入详情和追踪
- 快速操作：催办、推送方案、备注

### 6.2 工单跟踪详情页面

**关键字段**：
- 工单基本信息（编号、客户、设备、症状）
- 时间轴（创建→分诊→方案→验证→关闭）
- 当前待办（什么事儿卡住了，谁该做）
- 快速操作（催办、推送、确认）
- 内部评论（与工程师的讨论）

---

## 七、权限控制详解

### 7.1 工单字段级权限

```
字段名              | 创建时 | 提交后 | 诊断中 | 验证中 | 关闭后
─────────────────────────────────────────────────────
symptom_title      | FE/CS | FE/CS | FE    | -     | -
facts_json         | FE/CS | FE/CS | SE    | -     | -
priority           | Auto  | TL可改 | TL可改 | -    | -
assigned_to        | -     | CS/TL | TL可改 | -    | -
diagnosis_conclusion | -    | -     | SE可填 | SE   | -
solution_id        | -     | -     | SE可填 | SE   | -
verification_result | -    | -     | -     | FE可填 | -
satisfaction_score | -    | -     | -     | -     | Cust可填
```

### 7.2 操作权限检查清单

```python
# 伪代码示例
def can_update_field(user_role, field_name, ticket_status):
    """检查用户是否可以修改某字段"""
    
    permissions = {
        'symptom_title': ['FieldEngineer', 'CS'],
        'priority': ['Admin', 'TechLead'],
        'diagnosis_conclusion': ['SeniorEngineer'],
        'verification_result': ['FieldEngineer'],
        'satisfaction_score': ['Customer'],
        ...
    }
    
    # 进一步的状态限制
    if field_name == 'symptom_title' and ticket_status not in ['Draft', 'Submitted']:
        return False
    
    if field_name == 'diagnosis_conclusion' and ticket_status != 'Triage':
        return False
    
    return user_role in permissions.get(field_name, [])
```

---

## 八、实施建议

### 8.1 第一期：核心工单流程（第1-2周）

- [x] 已有：核心数据表结构、状态机、角色权限
- [ ] **新增**：
  - [ ] 追问机制（ticket_inquiries表）
  - [ ] SLA规则和计算（priority_rules, sla_rules表）
  - [ ] 客服待办清单视图
  - [ ] 工单详情页面的完整权限控制

### 8.2 第二期：工作管理（第3-4周）

- [ ] 待办清单展示和实时更新
- [ ] SLA超期提醒（推送通知）
- [ ] 工单催办功能
- [ ] 工作量分析报表

### 8.3 第三期：高级功能（第5-6周）

- [ ] 优先级自动调整
- [ ] 高级搜索和过滤
- [ ] 工单历史查询和统计
- [ ] 与知识库的关联

---

## 九、关键的SQL查询示例

### 9.1 获取客服的待办清单

```sql
SELECT 
    t.ticket_id, t.ticket_no, t.priority, t.status,
    c.customer_name, d.device_sn,
    t.created_at,
    t.sla_response_deadline, t.sla_resolve_deadline,
    CASE 
        WHEN t.sla_response_breached THEN 'response_timeout'
        WHEN t.sla_resolve_breached THEN 'resolve_timeout'
        ELSE 'normal'
    END as timeout_status,
    CASE 
        WHEN t.status = 'Submitted' THEN '待分诊'
        WHEN t.status = 'Triage' AND t.assigned_to_user_id IS NULL THEN '待分配'
        WHEN t.status = 'SolutionIssued' THEN '待推送方案'
        WHEN t.status = 'Verifying' THEN '待确认验证'
    END as todo_action
    
FROM tickets t
LEFT JOIN customers c ON t.customer_id = c.customer_id
LEFT JOIN devices d ON t.device_id = d.device_id
WHERE t.status IN ('Submitted', 'Triage', 'SolutionIssued', 'Verifying')
    AND t.is_deleted = FALSE
ORDER BY 
    CASE WHEN t.sla_response_breached THEN 0 ELSE 1 END,  -- 超时优先
    t.priority,
    t.created_at;
```

### 9.2 统计工单处理时间

```sql
SELECT 
    assigned_to_user_id,
    u.name as engineer_name,
    COUNT(*) as total_tickets,
    COUNT(CASE WHEN verification_result = 'passed' THEN 1 END) as passed_tickets,
    AVG(EXTRACT(HOUR FROM total_resolve_hours)) as avg_resolve_hours,
    AVG(rework_count) as avg_rework_count,
    AVG(CASE WHEN satisfaction_score IS NOT NULL THEN satisfaction_score END) as avg_satisfaction
    
FROM tickets t
LEFT JOIN users u ON t.assigned_to_user_id = u.id
WHERE t.created_at >= NOW() - INTERVAL '30 days'
    AND t.status = 'Closed'
GROUP BY assigned_to_user_id, u.name
ORDER BY total_tickets DESC;
```

### 9.3 优先级自动计算

```sql
-- 当创建工单时，自动确定优先级
UPDATE tickets t
SET 
    priority = pr.result_priority,
    priority_auto_set_reason = pr.rule_name,
    sla_response_minutes = sr.response_minutes,
    sla_resolve_hours = sr.resolve_hours,
    sla_response_deadline = NOW() + (sr.response_minutes || ' minutes')::INTERVAL,
    sla_resolve_deadline = NOW() + (sr.resolve_hours || ' hours')::INTERVAL
FROM priority_rules pr
LEFT JOIN customers c ON t.customer_id = c.customer_id
LEFT JOIN sla_rules sr ON sr.match_priority = pr.result_priority
WHERE t.status = 'Draft'
    AND (
        (pr.condition_type = 'customer_service_level' 
            AND c.service_level = pr.condition_value->>'service_level')
        OR (pr.condition_type = 'device_impact' 
            AND t.device_id IS NOT NULL)
    )
    AND pr.is_active = TRUE
    AND sr.is_active = TRUE
    AND sr.match_service_level = c.service_level;
```

---

## 十、风险和应对

| 风险 | 影响 | 应对 |
|------|------|------|
| SLA计算复杂，容易出错 | 工单优先级错误 | 建立验证机制、定期审查 |
| 追问机制过复杂，现场工程师填写困难 | 上报质量差 | 简化问卷、提供示例、逐步推进 |
| 高级工程师工作量过大 | 诊断延迟 | 优化判断卡库、自动化建议 |
| 权限控制不当导致数据泄露 | 安全风险 | 定期审计、完整日志记录 |

---

**文件版本**：v1.0  
**最后更新**：2025年12月  
**建议开始实施**：基于客户和设备模块完成后启动
