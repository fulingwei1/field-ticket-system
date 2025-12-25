# v2.0 数据模型设计文档

> **版本**：2.0  
> **创建日期**：2025-12-22  
> **最后更新**：2025-12-22  
> **数据库**：PostgreSQL 14+

---

## 📋 目录

1. [概述](#概述)
2. [ER图](#er图)
3. [核心表设计](#核心表设计)
4. [数据迁移脚本](#数据迁移脚本)
5. [索引设计](#索引设计)
6. [约束和规则](#约束和规则)

---

## 概述

### 设计原则

1. **判断卡为核心**：`judgement_cards` 是核心资产，所有工单必须关联判断卡
2. **版本控制**：判断卡支持版本化，保证知识可追溯
3. **硬规则约束**：通过数据库约束和业务逻辑确保数据质量
4. **审计留痕**：所有关键操作都有时间戳和操作人记录

### 核心实体关系

```
judgement_cards (核心)
    ├── judgement_card_versions (版本历史)
    ├── tickets (工单关联)
    └── knowledge_base_items (知识库关联)

tickets (工单)
    ├── customer_communications (客户沟通)
    ├── missing_info_checklists (缺失信息清单)
    ├── corrective_actions (整改任务)
    └── attachments (附件)

knowledge_base_items (知识库)
    ├── judgement_type (判断型，内部)
    └── conclusion_type (结论型，客户可见)
```

---

## ER图

### 实体关系概览

```
┌─────────────────────────────────────────────────────────────────┐
│                        核心实体层                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐         ┌──────────────────┐             │
│  │ judgement_cards  │◄────────┤ tickets          │             │
│  │ (判断卡)         │         │ (工单)           │             │
│  └────────┬─────────┘         └────────┬─────────┘             │
│           │                           │                        │
│           │                           │                        │
│  ┌────────▼─────────┐         ┌───────▼──────────┐            │
│  │ jc_versions      │         │ customer_comms   │            │
│  │ (版本历史)       │         │ (客户沟通)       │            │
│  └──────────────────┘         └───────────────────┘            │
│                                                                 │
│  ┌──────────────────┐         ┌──────────────────┐             │
│  │ knowledge_items │         │ corrective_acts  │             │
│  │ (知识库)        │         │ (整改任务)       │             │
│  └──────────────────┘         └──────────────────┘             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        基础实体层                                │
├─────────────────────────────────────────────────────────────────┤
│  users │ customers │ projects │ devices │ stations │ attachments│
└─────────────────────────────────────────────────────────────────┘
```

### 详细关系说明

1. **judgement_cards → tickets** (1:N)
   - 一个判断卡可以被多个工单使用
   - 工单必须关联一个判断卡（硬规则）

2. **judgement_cards → judgement_card_versions** (1:N)
   - 一个判断卡可以有多个版本
   - 版本化保证知识可追溯

3. **tickets → customer_communications** (1:N)
   - 一个工单可以有多次客户沟通
   - 所有对外消息必须落库（硬规则）

4. **tickets → corrective_actions** (N:M)
   - 一个工单可以关联多个整改任务
   - 一个整改任务可以关联多个工单

5. **judgement_cards → knowledge_base_items** (N:M)
   - 判断卡可以关联多个知识库项
   - 知识库项可以关联多个判断卡

---

## 核心表设计

### 1. judgement_cards（判断卡表）- 核心资产

```sql
-- 判断卡表（核心资产）
CREATE TABLE judgement_cards (
    -- 主键
    jc_code VARCHAR(20) PRIMARY KEY,  -- 判断卡编码，如 JC-A-001
    
    -- 基本信息
    title VARCHAR(200) NOT NULL,                    -- 判断卡标题
    description TEXT,                                -- 描述
    
    -- 适用范围
    domain CHAR(1) NOT NULL                         -- 问题域：A/B/C/D/E
        CHECK (domain IN ('A', 'B', 'C', 'D', 'E')),
    applicable_step_codes VARCHAR(50)[],            -- 适用的步骤代码列表
    applicable_device_models VARCHAR(100)[],        -- 适用的设备型号列表
    
    -- 判断卡内容（结构化）
    symptoms JSONB NOT NULL DEFAULT '[]',           -- 症状列表（结构化）
    differentiation_points JSONB NOT NULL DEFAULT '[]',  -- 区分点列表
    investigation_path JSONB NOT NULL DEFAULT '[]', -- 排查路径（树状结构）
    exclusion_items JSONB NOT NULL DEFAULT '[]',    -- 排除项列表
    
    -- 关联关系
    parent_jc_code VARCHAR(20)                     -- 父判断卡（用于分叉树）
        REFERENCES judgement_cards(jc_code) ON DELETE SET NULL,
    related_jc_codes VARCHAR(20)[],                 -- 相关判断卡列表
    
    -- 版本信息
    current_version INT NOT NULL DEFAULT 1,        -- 当前版本号
    is_active BOOLEAN NOT NULL DEFAULT TRUE,       -- 是否启用
    
    -- 使用统计
    usage_count INT NOT NULL DEFAULT 0,             -- 使用次数
    success_count INT NOT NULL DEFAULT 0,           -- 成功次数
    success_rate DECIMAL(5,2),                      -- 成功率（0-100）
    last_used_at TIMESTAMPTZ,                      -- 最后使用时间
    
    -- 质量评分
    quality_score DECIMAL(3,2),                     -- 质量评分（0-5）
    quality_score_count INT NOT NULL DEFAULT 0,     -- 评分次数
    
    -- 创建和更新信息
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID REFERENCES users(id),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- 备注
    notes TEXT                                      -- 备注信息
);

-- 索引
CREATE INDEX idx_jc_domain ON judgement_cards(domain);
CREATE INDEX idx_jc_parent ON judgement_cards(parent_jc_code);
CREATE INDEX idx_jc_active ON judgement_cards(is_active);
CREATE INDEX idx_jc_usage_count ON judgement_cards(usage_count DESC);
CREATE INDEX idx_jc_success_rate ON judgement_cards(success_rate DESC NULLS LAST);
CREATE INDEX idx_jc_last_used ON judgement_cards(last_used_at DESC NULLS LAST);
```

### 2. judgement_card_versions（判断卡版本表）

```sql
-- 判断卡版本历史表
CREATE TABLE judgement_card_versions (
    version_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 关联判断卡
    jc_code VARCHAR(20) NOT NULL REFERENCES judgement_cards(jc_code) ON DELETE CASCADE,
    version INT NOT NULL,                           -- 版本号
    
    -- 版本内容（快照）
    title VARCHAR(200) NOT NULL,
    description TEXT,
    symptoms JSONB NOT NULL DEFAULT '[]',
    differentiation_points JSONB NOT NULL DEFAULT '[]',
    investigation_path JSONB NOT NULL DEFAULT '[]',
    exclusion_items JSONB NOT NULL DEFAULT '[]',
    
    -- 版本变更信息
    change_reason TEXT,                             -- 变更原因
    change_type VARCHAR(50),                        -- 变更类型：CREATE/UPDATE/DEPRECATE
    changed_fields TEXT[],                          -- 变更字段列表
    
    -- 版本状态
    is_current BOOLEAN NOT NULL DEFAULT FALSE,      -- 是否为当前版本
    
    -- 审计信息
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- 唯一约束：同一判断卡的版本号唯一
    UNIQUE(jc_code, version)
);

-- 索引
CREATE INDEX idx_jc_version_code ON judgement_card_versions(jc_code);
CREATE INDEX idx_jc_version_current ON judgement_card_versions(jc_code, is_current) WHERE is_current = TRUE;
```

### 3. tickets（工单表）- v2.0 扩展字段

```sql
-- 工单表（v2.0扩展）
-- 注意：此脚本只包含v2.0新增字段，基础字段已存在

-- 添加v2.0需要的字段
ALTER TABLE tickets 
    -- 判断卡关联（已有，但需要改为NOT NULL）
    -- current_jc_code VARCHAR(20)  -- 已存在，需要添加NOT NULL约束
    
    -- 判断信息（硬规则：结案时必须填写）
    ADD COLUMN IF NOT EXISTS current_hypothesis TEXT,           -- 当前假设
    ADD COLUMN IF NOT EXISTS next_action TEXT,                  -- 下一步动作
    ADD COLUMN IF NOT EXISTS confidence SMALLINT                -- 置信度（1-5）
        CHECK (confidence >= 1 AND confidence <= 5),
    
    -- 升级信息
    ADD COLUMN IF NOT EXISTS escalation_required BOOLEAN DEFAULT FALSE,  -- 是否需要升级
    ADD COLUMN IF NOT EXISTS escalated_to UUID REFERENCES users(id),   -- 升级给谁
    ADD COLUMN IF NOT EXISTS escalation_reason TEXT,                    -- 升级原因
    
    -- 责任归因（硬规则：结案时必须填写）
    ADD COLUMN IF NOT EXISTS root_cause TEXT,                   -- 根因
    ADD COLUMN IF NOT EXISTS responsibility_team VARCHAR(50),   -- 责任团队
    ADD COLUMN IF NOT EXISTS is_preventable BOOLEAN,             -- 是否可预防
    
    -- 问诊式补全
    ADD COLUMN IF NOT EXISTS missing_info_completed BOOLEAN DEFAULT FALSE,  -- 缺失信息是否已补全
    ADD COLUMN IF NOT EXISTS missing_info_completed_at TIMESTAMPTZ;         -- 补全时间

-- 添加外键约束（判断卡）
ALTER TABLE tickets 
    ADD CONSTRAINT fk_ticket_jc_code 
    FOREIGN KEY (current_jc_code) 
    REFERENCES judgement_cards(jc_code) 
    ON DELETE RESTRICT;

-- 添加索引
CREATE INDEX IF NOT EXISTS idx_ticket_jc_code ON tickets(current_jc_code);
CREATE INDEX IF NOT EXISTS idx_ticket_confidence ON tickets(confidence);
CREATE INDEX IF NOT EXISTS idx_ticket_escalation ON tickets(escalation_required, escalated_to);
CREATE INDEX IF NOT EXISTS idx_ticket_root_cause ON tickets(root_cause, responsibility_team);
```

### 4. customer_communications（客户沟通表）

```sql
-- 客户沟通表（硬规则：所有对外消息必须落库）
CREATE TABLE customer_communications (
    comm_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 关联工单
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    
    -- 沟通信息
    comm_type VARCHAR(20) NOT NULL                 -- 沟通类型：EMAIL/WECHAT/PHONE/ONSITE
        CHECK (comm_type IN ('EMAIL', 'WECHAT', 'PHONE', 'ONSITE', 'SYSTEM')),
    direction VARCHAR(10) NOT NULL                  -- 方向：OUTBOUND/INBOUND
        CHECK (direction IN ('OUTBOUND', 'INBOUND')),
    
    -- 内容
    subject VARCHAR(500),                          -- 主题（邮件）
    content TEXT NOT NULL,                         -- 内容
    template_id VARCHAR(50),                       -- 使用的模板ID（如果有）
    is_template_based BOOLEAN DEFAULT FALSE,      -- 是否基于模板
    
    -- AI辅助生成
    is_ai_generated BOOLEAN DEFAULT FALSE,        -- 是否AI生成
    ai_model VARCHAR(100),                         -- AI模型
    ai_confidence DECIMAL(3,2),                    -- AI置信度
    evidence_sources JSONB,                        -- 证据来源（RAG）
    
    -- 发送信息
    sent_by UUID NOT NULL REFERENCES users(id),  -- 发送人
    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),    -- 发送时间
    recipient_email VARCHAR(255),                  -- 收件人邮箱
    recipient_wechat_id VARCHAR(100),             -- 收件人微信ID
    
    -- 状态
    status VARCHAR(20) DEFAULT 'DRAFT'            -- DRAFT/SENT/DELIVERED/READ/FAILED
        CHECK (status IN ('DRAFT', 'SENT', 'DELIVERED', 'READ', 'FAILED')),
    status_updated_at TIMESTAMPTZ,
    
    -- 审核信息
    reviewed_by UUID REFERENCES users(id),        -- 审核人
    reviewed_at TIMESTAMPTZ,                       -- 审核时间
    review_notes TEXT,                             -- 审核备注
    
    -- 审计字段
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 索引
CREATE INDEX idx_comm_ticket ON customer_communications(ticket_id);
CREATE INDEX idx_comm_sent_by ON customer_communications(sent_by);
CREATE INDEX idx_comm_sent_at ON customer_communications(sent_at DESC);
CREATE INDEX idx_comm_type ON customer_communications(comm_type);
CREATE INDEX idx_comm_status ON customer_communications(status);
```

### 5. missing_info_checklists（缺失信息清单表）

```sql
-- 缺失信息清单表（问诊式补全）
CREATE TABLE missing_info_checklists (
    checklist_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 关联工单
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    
    -- 清单信息
    checklist_version INT NOT NULL DEFAULT 1,     -- 清单版本（支持多次补全）
    generated_by VARCHAR(20) DEFAULT 'AI',         -- 生成方式：AI/MANUAL
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- 生成时间
    
    -- 问题项列表（JSONB）
    questions JSONB NOT NULL DEFAULT '[]',         -- 问题列表
    
    -- 完成状态
    is_completed BOOLEAN DEFAULT FALSE,            -- 是否已完成
    completed_at TIMESTAMPTZ,                      -- 完成时间
    completed_by UUID REFERENCES users(id),        -- 完成人
    
    -- 审计字段
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 问题项JSONB结构示例：
-- [
--   {
--     "question_id": "q1",
--     "question": "设备是否重启过？",
--     "type": "yes_no",
--     "required": true,
--     "hint": "如果重启过，请说明重启后的状态",
--     "answer": null,
--     "answered_at": null
--   },
--   {
--     "question_id": "q2",
--     "question": "请提供报警代码",
--     "type": "text",
--     "required": true,
--     "hint": "可在设备屏幕上查看",
--     "answer": null,
--     "answered_at": null
--   }
-- ]

-- 索引
CREATE INDEX idx_checklist_ticket ON missing_info_checklists(ticket_id);
CREATE INDEX idx_checklist_completed ON missing_info_checklists(is_completed, completed_at);
```

### 6. corrective_actions（整改任务表）

```sql
-- 整改任务表（CAPA）
CREATE TABLE corrective_actions (
    action_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 基本信息
    action_no VARCHAR(50) NOT NULL UNIQUE,         -- 整改任务编号：CAPA-YYYYMMDD-NNN
    title VARCHAR(200) NOT NULL,                   -- 任务标题
    description TEXT,                              -- 任务描述
    
    -- 触发信息
    trigger_type VARCHAR(20) NOT NULL              -- 触发类型：THRESHOLD/MANUAL
        CHECK (trigger_type IN ('THRESHOLD', 'MANUAL')),
    trigger_threshold INT,                         -- 触发阈值（如果是阈值触发）
    trigger_date DATE NOT NULL,                    -- 触发日期
    
    -- 关联信息
    root_cause TEXT NOT NULL,                      -- 根因
    responsibility_team VARCHAR(50) NOT NULL,       -- 责任团队
    device_model VARCHAR(100),                    -- 设备型号
    related_ticket_ids UUID[],                    -- 关联的工单ID列表
    
    -- 任务状态
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN'     -- OPEN/IN_PROGRESS/COMPLETED/CLOSED/CANCELLED
        CHECK (status IN ('OPEN', 'IN_PROGRESS', 'COMPLETED', 'CLOSED', 'CANCELLED')),
    
    -- 任务信息
    assigned_to UUID REFERENCES users(id),        -- 负责人
    due_date DATE,                                 -- 截止日期
    priority VARCHAR(5) DEFAULT 'P3'               -- 优先级：P1/P2/P3/P4
        CHECK (priority IN ('P1', 'P2', 'P3', 'P4')),
    
    -- 执行信息
    action_plan TEXT,                              -- 行动计划
    implementation_details TEXT,                   -- 实施详情
    verification_method TEXT,                     -- 验证方法
    
    -- 完成信息
    completed_at TIMESTAMPTZ,                      -- 完成时间
    completed_by UUID REFERENCES users(id),       -- 完成人
    completion_notes TEXT,                         -- 完成备注
    
    -- 效果评估
    effectiveness_score DECIMAL(3,2),              -- 效果评分（0-5）
    effectiveness_notes TEXT,                     -- 效果评估备注
    evaluated_at TIMESTAMPTZ,                      -- 评估时间
    evaluated_by UUID REFERENCES users(id),         -- 评估人
    
    -- 审计字段
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID REFERENCES users(id),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 索引
CREATE INDEX idx_capa_status ON corrective_actions(status);
CREATE INDEX idx_capa_assigned ON corrective_actions(assigned_to);
CREATE INDEX idx_capa_team ON corrective_actions(responsibility_team);
CREATE INDEX idx_capa_trigger_date ON corrective_actions(trigger_date DESC);
CREATE INDEX idx_capa_due_date ON corrective_actions(due_date);
```

### 7. knowledge_base_items（知识库项表）

```sql
-- 知识库项表（判断型+结论型）
CREATE TABLE knowledge_base_items (
    item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 基本信息
    item_code VARCHAR(50) NOT NULL UNIQUE,         -- 知识库项编码
    title VARCHAR(200) NOT NULL,                   -- 标题
    content TEXT NOT NULL,                         -- 内容
    
    -- 类型
    item_type VARCHAR(20) NOT NULL                 -- 类型：JUDGEMENT/CONCLUSION
        CHECK (item_type IN ('JUDGEMENT', 'CONCLUSION')),
    
    -- 判断型知识库（内部）
    -- 当 item_type = 'JUDGEMENT' 时使用
    symptoms JSONB,                                -- 症状列表
    differentiation_points JSONB,                  -- 区分点
    investigation_path JSONB,                      -- 排查路径
    common_mistakes JSONB,                         -- 常见误判
    
    -- 结论型知识库（客户可见）
    -- 当 item_type = 'CONCLUSION' 时使用
    faq_question TEXT,                             -- FAQ问题
    faq_answer TEXT,                               -- FAQ答案
    operation_guide TEXT,                           -- 操作指引
    verified_solution TEXT,                        -- 已验证方案
    version_compatibility JSONB,                    -- 版本兼容性
    risk_warnings TEXT[],                           -- 风险提示
    
    -- 适用范围
    domain CHAR(1),                                -- 问题域：A/B/C/D/E
    applicable_step_codes VARCHAR(50)[],           -- 适用的步骤代码
    applicable_device_models VARCHAR(100)[],       -- 适用的设备型号
    
    -- 关联判断卡
    related_jc_codes VARCHAR(20)[],               -- 关联的判断卡编码列表
    
    -- 使用统计
    view_count INT NOT NULL DEFAULT 0,            -- 查看次数
    like_count INT NOT NULL DEFAULT 0,            -- 点赞次数
    helpful_count INT NOT NULL DEFAULT 0,          -- 有帮助次数
    
    -- 质量评分
    quality_score DECIMAL(3,2),                    -- 质量评分（0-5）
    
    -- 状态
    is_published BOOLEAN DEFAULT FALSE,           -- 是否发布（结论型需要发布才能客户可见）
    published_at TIMESTAMPTZ,                      -- 发布时间
    
    -- 审计字段
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID REFERENCES users(id),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 索引
CREATE INDEX idx_kb_type ON knowledge_base_items(item_type);
CREATE INDEX idx_kb_domain ON knowledge_base_items(domain);
CREATE INDEX idx_kb_published ON knowledge_base_items(is_published, published_at DESC);
CREATE INDEX idx_kb_view_count ON knowledge_base_items(view_count DESC);
```

### 8. 其他基础表（已存在，仅列出关键字段）

```sql
-- 用户表（已存在）
-- users (id, corp_id, wecom_userid, name, role, ...)

-- 客户表（需要确认是否存在）
CREATE TABLE IF NOT EXISTS customers (
    customer_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_code VARCHAR(50) NOT NULL UNIQUE,
    customer_name VARCHAR(200) NOT NULL,
    -- ... 其他字段
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 项目表（需要确认是否存在）
CREATE TABLE IF NOT EXISTS projects (
    project_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_code VARCHAR(50) NOT NULL UNIQUE,
    project_name VARCHAR(200) NOT NULL,
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    -- ... 其他字段
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 设备表（需要确认是否存在）
CREATE TABLE IF NOT EXISTS devices (
    device_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_code VARCHAR(50) NOT NULL UNIQUE,
    device_model VARCHAR(100) NOT NULL,
    project_id UUID NOT NULL REFERENCES projects(project_id),
    -- ... 其他字段
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 工位表（需要确认是否存在）
CREATE TABLE IF NOT EXISTS stations (
    station_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    station_code VARCHAR(50) NOT NULL UNIQUE,
    station_name VARCHAR(200) NOT NULL,
    device_id UUID NOT NULL REFERENCES devices(device_id),
    -- ... 其他字段
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## 数据迁移脚本

### 迁移脚本：v1.0 → v2.0

```sql
-- ============================================
-- v2.0 数据迁移脚本
-- 从 v1.0 迁移到 v2.0
-- 创建时间: 2025-12-22
-- ============================================

BEGIN;

-- 1. 创建判断卡相关表
CREATE TABLE IF NOT EXISTS judgement_cards (
    -- ... 见上面的完整DDL
);

CREATE TABLE IF NOT EXISTS judgement_card_versions (
    -- ... 见上面的完整DDL
);

-- 2. 扩展工单表（添加v2.0字段）
ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS current_hypothesis TEXT,
    ADD COLUMN IF NOT EXISTS next_action TEXT,
    ADD COLUMN IF NOT EXISTS confidence SMALLINT
        CHECK (confidence >= 1 AND confidence <= 5),
    ADD COLUMN IF NOT EXISTS escalation_required BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS escalated_to UUID REFERENCES users(id),
    ADD COLUMN IF NOT EXISTS escalation_reason TEXT,
    ADD COLUMN IF NOT EXISTS root_cause TEXT,
    ADD COLUMN IF NOT EXISTS responsibility_team VARCHAR(50),
    ADD COLUMN IF NOT EXISTS is_preventable BOOLEAN,
    ADD COLUMN IF NOT EXISTS missing_info_completed BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS missing_info_completed_at TIMESTAMPTZ;

-- 3. 创建客户沟通表
CREATE TABLE IF NOT EXISTS customer_communications (
    -- ... 见上面的完整DDL
);

-- 4. 创建缺失信息清单表
CREATE TABLE IF NOT EXISTS missing_info_checklists (
    -- ... 见上面的完整DDL
);

-- 5. 创建整改任务表
CREATE TABLE IF NOT EXISTS corrective_actions (
    -- ... 见上面的完整DDL
);

-- 6. 创建知识库项表
CREATE TABLE IF NOT EXISTS knowledge_base_items (
    -- ... 见上面的完整DDL
);

-- 7. 数据迁移：为现有工单设置默认值
-- 注意：历史工单的 current_jc_code 可能为空，需要处理
UPDATE tickets 
SET 
    escalation_required = FALSE,
    missing_info_completed = TRUE  -- 历史工单视为已补全
WHERE 
    escalation_required IS NULL;

-- 8. 创建初始判断卡（示例）
-- 从历史工单中提取常见问题，创建初始判断卡库
-- 这部分需要根据实际业务数据来执行

-- 9. 创建索引
CREATE INDEX IF NOT EXISTS idx_ticket_jc_code ON tickets(current_jc_code);
CREATE INDEX IF NOT EXISTS idx_ticket_confidence ON tickets(confidence);
CREATE INDEX IF NOT EXISTS idx_ticket_escalation ON tickets(escalation_required, escalated_to);
CREATE INDEX IF NOT EXISTS idx_ticket_root_cause ON tickets(root_cause, responsibility_team);

COMMIT;
```

### 回滚脚本（如果需要）

```sql
-- ============================================
-- v2.0 回滚脚本
-- 回滚到 v1.0
-- ============================================

BEGIN;

-- 删除新增的表
DROP TABLE IF EXISTS knowledge_base_items CASCADE;
DROP TABLE IF EXISTS corrective_actions CASCADE;
DROP TABLE IF EXISTS missing_info_checklists CASCADE;
DROP TABLE IF EXISTS customer_communications CASCADE;
DROP TABLE IF EXISTS judgement_card_versions CASCADE;
DROP TABLE IF EXISTS judgement_cards CASCADE;

-- 删除新增的字段（注意：需要先删除外键约束）
ALTER TABLE tickets 
    DROP CONSTRAINT IF EXISTS fk_ticket_jc_code,
    DROP COLUMN IF EXISTS current_hypothesis,
    DROP COLUMN IF EXISTS next_action,
    DROP COLUMN IF EXISTS confidence,
    DROP COLUMN IF EXISTS escalation_required,
    DROP COLUMN IF EXISTS escalated_to,
    DROP COLUMN IF EXISTS escalation_reason,
    DROP COLUMN IF EXISTS root_cause,
    DROP COLUMN IF EXISTS responsibility_team,
    DROP COLUMN IF EXISTS is_preventable,
    DROP COLUMN IF EXISTS missing_info_completed,
    DROP COLUMN IF EXISTS missing_info_completed_at;

-- 删除索引
DROP INDEX IF EXISTS idx_ticket_jc_code;
DROP INDEX IF EXISTS idx_ticket_confidence;
DROP INDEX IF EXISTS idx_ticket_escalation;
DROP INDEX IF EXISTS idx_ticket_root_cause;

COMMIT;
```

---

## 索引设计

### 索引策略

1. **主键索引**：所有表都有主键，自动创建主键索引
2. **外键索引**：所有外键字段都创建索引
3. **查询优化索引**：根据常见查询模式创建索引
4. **复合索引**：多字段查询使用复合索引

### 索引清单

```sql
-- 判断卡表索引
CREATE INDEX idx_jc_domain ON judgement_cards(domain);
CREATE INDEX idx_jc_parent ON judgement_cards(parent_jc_code);
CREATE INDEX idx_jc_active ON judgement_cards(is_active);
CREATE INDEX idx_jc_usage_count ON judgement_cards(usage_count DESC);
CREATE INDEX idx_jc_success_rate ON judgement_cards(success_rate DESC NULLS LAST);
CREATE INDEX idx_jc_last_used ON judgement_cards(last_used_at DESC NULLS LAST);

-- 工单表索引（v2.0新增）
CREATE INDEX idx_ticket_jc_code ON tickets(current_jc_code);
CREATE INDEX idx_ticket_confidence ON tickets(confidence);
CREATE INDEX idx_ticket_escalation ON tickets(escalation_required, escalated_to);
CREATE INDEX idx_ticket_root_cause ON tickets(root_cause, responsibility_team);

-- 客户沟通表索引
CREATE INDEX idx_comm_ticket ON customer_communications(ticket_id);
CREATE INDEX idx_comm_sent_by ON customer_communications(sent_by);
CREATE INDEX idx_comm_sent_at ON customer_communications(sent_at DESC);
CREATE INDEX idx_comm_type ON customer_communications(comm_type);
CREATE INDEX idx_comm_status ON customer_communications(status);

-- 缺失信息清单表索引
CREATE INDEX idx_checklist_ticket ON missing_info_checklists(ticket_id);
CREATE INDEX idx_checklist_completed ON missing_info_checklists(is_completed, completed_at);

-- 整改任务表索引
CREATE INDEX idx_capa_status ON corrective_actions(status);
CREATE INDEX idx_capa_assigned ON corrective_actions(assigned_to);
CREATE INDEX idx_capa_team ON corrective_actions(responsibility_team);
CREATE INDEX idx_capa_trigger_date ON corrective_actions(trigger_date DESC);
CREATE INDEX idx_capa_due_date ON corrective_actions(due_date);

-- 知识库项表索引
CREATE INDEX idx_kb_type ON knowledge_base_items(item_type);
CREATE INDEX idx_kb_domain ON knowledge_base_items(domain);
CREATE INDEX idx_kb_published ON knowledge_base_items(is_published, published_at DESC);
CREATE INDEX idx_kb_view_count ON knowledge_base_items(view_count DESC);
```

---

## 约束和规则

### 数据库约束

1. **NOT NULL约束**：
   - `judgement_cards.jc_code`：判断卡编码必填
   - `tickets.current_jc_code`：工单必须关联判断卡（v2.0硬规则）
   - `tickets.current_hypothesis`：结案时必须填写（业务逻辑约束）
   - `tickets.next_action`：结案时必须填写（业务逻辑约束）

2. **CHECK约束**：
   - `judgement_cards.domain`：问题域只能是 A/B/C/D/E
   - `tickets.confidence`：置信度范围 1-5
   - `customer_communications.comm_type`：沟通类型限制
   - `corrective_actions.status`：状态值限制

3. **外键约束**：
   - 所有外键都设置了适当的 `ON DELETE` 行为
   - `judgement_cards` → `users`（创建人）
   - `tickets` → `judgement_cards`（判断卡）
   - `customer_communications` → `tickets`（工单）

### 业务规则（硬规则）

这些规则需要在应用层实现，数据库约束作为辅助：

1. **规则1：无判断卡不得结案**
   ```sql
   -- 数据库约束（辅助）
   ALTER TABLE tickets 
       ALTER COLUMN current_jc_code SET NOT NULL;
   
   -- 业务逻辑：结案时检查 current_hypothesis 和 next_action
   ```

2. **规则2：低置信度自动升级**
   ```sql
   -- 数据库约束（辅助）
   ALTER TABLE tickets 
       ADD CONSTRAINT chk_confidence_range 
       CHECK (confidence >= 1 AND confidence <= 5);
   
   -- 业务逻辑：confidence <= 2 时自动设置 escalation_required = TRUE
   ```

3. **规则3：对外消息必须落库**
   ```sql
   -- 数据库约束：所有客户沟通必须记录
   -- 业务逻辑：所有对外沟通必须通过 customer_communications 表
   ```

4. **规则4：结案必须归因**
   ```sql
   -- 业务逻辑：结案时检查 root_cause, responsibility_team, is_preventable
   -- 可以通过触发器或应用层逻辑实现
   ```

5. **规则5：重复问题阈值触发CAPA**
   ```sql
   -- 业务逻辑：通过定时任务或触发器实现
   -- 检查同型号+同归因，30天≥N次的情况
   ```

---

## 数据模型验证

### 验证清单

- [ ] 所有表都已创建
- [ ] 所有索引都已创建
- [ ] 所有外键约束都已设置
- [ ] 所有CHECK约束都已设置
- [ ] 数据迁移脚本已测试
- [ ] 回滚脚本已测试
- [ ] 性能测试通过（索引效果）
- [ ] 硬规则业务逻辑已实现

### 验证SQL

```sql
-- 检查所有表是否存在
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name IN (
    'judgement_cards',
    'judgement_card_versions',
    'customer_communications',
    'missing_info_checklists',
    'corrective_actions',
    'knowledge_base_items'
  );

-- 检查所有索引是否存在
SELECT indexname 
FROM pg_indexes 
WHERE tablename IN (
    'judgement_cards',
    'judgement_card_versions',
    'tickets',
    'customer_communications',
    'missing_info_checklists',
    'corrective_actions',
    'knowledge_base_items'
  );

-- 检查外键约束
SELECT
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'public';
```

---

**文档版本**：1.0  
**最后更新**：2025-12-22  
**维护人**：开发团队


