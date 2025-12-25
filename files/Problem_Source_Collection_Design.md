# 问题来源与收集机制 - 详细设计方案

## 一、问题来源分析

### 1.1 问题的多维度来源

```
┌─────────────────────────────────────────────────────┐
│                  问题来源多样化                      │
├─────────────────────────────────────────────────────┤
│                                                     │
│  来源角色维度：                                     │
│  ├─ 客户现场工人/操作员                            │
│  ├─ 客户工程师/技术负责人                          │
│  ├─ 客户采购/质量部门                              │
│  ├─ 客户其他部门（仓储、物流等）                  │
│  └─ 其他（代理商、合作伙伴）                      │
│                                                     │
│  接收渠道维度：                                     │
│  ├─ 我们的项目经理 (PM)                           │
│  ├─ 我们的销售                                     │
│  ├─ 我们的客服工程师（CSE）直接                   │
│  ├─ 我们的技术支持热线/邮件                       │
│  └─ 其他（客服、行政等）                          │
│                                                     │
│  问题处理维度：                                     │
│  ├─ 客服工程师现场直接处理（简单问题）            │
│  ├─ 项目经理收集后转办给相应工程师                │
│  ├─ 销售反馈，PM转办处理                          │
│  └─ 跨部门协作处理                                │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### 1.2 问题分类（按来源和处理方式）

| 问题类型 | 来源 | 接收者 | 处理方式 | 记录要求 | 示例 |
|--------|------|--------|--------|---------|------|
| **A-现场直接** | 客户现场 | CSE | CSE直接处理 | 必须记录 | 参数调整、重启、简单配置 |
| **B-PM转办** | 客户→PM | PM | PM转给工程师 | 必须记录+追踪 | 需求变更、技术方案 |
| **C-销售反馈** | 客户→销售 | 销售 | 销售→PM→工程师 | 必须记录+追踪 | 客户投诉、功能需求 |
| **D-支持热线** | 客户→热线 | 客服 | 客服处理或转给CSE | 必须记录 | 远程支持、技术咨询 |
| **E-邮件反馈** | 客户→邮箱 | 相关人员 | 分类后转办 | 必须记录 | 文字描述的问题 |

---

## 二、问题来源收集系统设计

### 2.1 多入口问题收集架构

```
┌──────────────────────────────────────────────────────────────┐
│              问题收集系统 - 多入口架构                        │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  入口1: 现场工程师                                           │
│  └─→ 小程序 → [问题上报] → 自动创建工单                    │
│                                                               │
│  入口2: 项目经理                                             │
│  └─→ Web端 + 企业微信 → [新建任务/工单] → 转办给工程师   │
│                                                               │
│  入口3: 销售反馈                                             │
│  └─→ CRM系统 + 企业微信 → [反馈记录] → 转给PM             │
│                                                               │
│  入口4: 支持热线/邮件                                       │
│  └─→ 邮件系统 + 电话记录 → [自动入库] → 工单系统          │
│                                                               │
│  入口5: 客户客服直接反馈                                    │
│  └─→ 客户自服务门户 → [在线表单] → 自动工单              │
│                                                               │
└──────────────────────────────────────────────────────────────┘
         ↓↓↓↓↓↓↓↓↓
┌──────────────────────────────────────────────────────────────┐
│              统一问题收集库                                   │
│  （所有问题汇聚到同一个数据库）                              │
└──────────────────────────────────────────────────────────────┘
         ↓↓↓↓↓↓↓↓↓
┌──────────────────────────────────────────────────────────────┐
│    智能路由与分配                                           │
│    ├─ 直接处理（CSE）                                       │
│    ├─ PM转办                                                │
│    ├─ 跨部门协作                                            │
│    └─ 知识库推荐                                            │
└──────────────────────────────────────────────────────────────┘
         ↓↓↓↓↓↓↓↓↓
┌──────────────────────────────────────────────────────────────┐
│    问题处理与跟踪                                           │
│    ├─ 直接处理 → 记录解决方案 → 沉淀到知识库               │
│    ├─ 转办处理 → 追踪进度 → 确认解决 → 沉淀              │
│    └─ 待办提醒 → 超期预警 → 结果反馈                      │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 问题来源表设计

```sql
-- 问题来源追踪表
CREATE TABLE problem_sources (
    source_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 问题的原始来源
    original_source_type VARCHAR(50) NOT NULL     -- customer_staff/customer_engineer/customer_buyer/other
        CHECK (original_source_type IN ('customer_staff', 'customer_engineer', 'customer_buyer', 'other')),
    
    original_reporter_name VARCHAR(100),          -- 原始报告人名字
    original_reporter_role VARCHAR(100),          -- 原始报告人职位
    original_report_date TIMESTAMPTZ,             -- 原始报告时间
    original_report_channel VARCHAR(50),          -- 报告渠道：phone/wechat/in_person/email/other
    
    -- 我们这边的接收者
    received_by_user_id UUID NOT NULL REFERENCES users(id),  -- 谁接收的
    received_by_role VARCHAR(50) NOT NULL                     -- PM/sales/CSE/support/other
        CHECK (received_by_role IN ('PM', 'sales', 'CSE', 'support', 'other')),
    
    received_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    received_channel VARCHAR(50),                 -- 接收渠道：wechat/email/phone/app/web/crm
    
    -- 关联关系
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    project_id UUID REFERENCES projects(project_id),
    device_id UUID REFERENCES devices(device_id),
    ticket_id UUID REFERENCES tickets(ticket_id),    -- 最终关联的工单
    
    -- 处理路由
    routing_type VARCHAR(50) NOT NULL             -- 路由类型
        CHECK (routing_type IN ('direct_handle', 'pm_transfer', 'sales_to_pm', 'support_escalate', 'cross_team')),
    
    transfer_to_user_id UUID REFERENCES users(id),   -- 转给谁（如果有转办）
    transfer_reason TEXT,                            -- 转办理由
    transfer_at TIMESTAMPTZ,                         -- 转办时间
    
    -- 状态跟踪
    status VARCHAR(20) DEFAULT 'received'
        CHECK (status IN ('received', 'in_progress', 'resolved', 'transferred', 'escalated')),
    
    resolved_at TIMESTAMPTZ,
    solution_summary TEXT,                        -- 解决方案摘要
    
    -- 审计
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_problem_sources_customer ON problem_sources(customer_id);
CREATE INDEX idx_problem_sources_ticket ON problem_sources(ticket_id);
CREATE INDEX idx_problem_sources_received_by ON problem_sources(received_by_user_id);
CREATE INDEX idx_problem_sources_received_at ON problem_sources(received_at DESC);
```

---

## 三、直接处理流程（现场CSE）

### 3.1 CSE直接处理的问题特征

```
适合直接处理的问题（A类问题）：
├─ 问题复杂度低
│  └─ 例：参数值调整、设备重启、简单的工艺调整
│
├─ 解决方案已知且有记录
│  └─ 例：历史上出现过并已解决的问题
│
├─ 解决时间短（< 2小时）
│  └─ 例：现场可立即修复的问题
│
├─ 影响范围小
│  └─ 例：仅影响一条产线，不影响其他生产
│
└─ 不需要跨部门协作
   └─ 例：设备配置、软件重装等CSE自己能搞定
```

### 3.2 CSE直接处理流程

```
┌─────────────────────────────────────────┐
│ 客户现场工人反馈问题                     │
└────────────┬────────────────────────────┘
             │ 电话/微信/当面
             ↓
┌─────────────────────────────────────────┐
│ CSE在小程序中"快速记录问题"              │
│ 字段：                                  │
│ ├─ 问题来源角色：现场工人/工程师/采购  │
│ ├─ 原始报告人：xxx                      │
│ ├─ 接收渠道：电话/微信/当面            │
│ ├─ 问题描述                            │
│ ├─ 初步判断：是否可直接处理            │
│ └─ 问题分类（A/B/C类）                │
└────────────┬────────────────────────────┘
             │
        ┌────┴──────┐
        │ 是否直接处理? │
        └────┬───────┘
      是     │      否
      ├──────┘  ┌──────────────────────────┐
      │         │ 转给PM/高工处理          │
      │         │ 设置状态：待转办         │
      │         │ 通知相关人员             │
      │         └──────────────────────────┘
      │
      ↓
┌─────────────────────────────────────────┐
│ CSE执行处理操作                         │
│ 记录：                                  │
│ ├─ 处理步骤和操作                      │
│ ├─ 使用的工具/方案                    │
│ ├─ 耗时                                │
│ ├─ 中间状态照片/日志                  │
│ └─ 最终结果（成功/失败/部分）         │
└────────────┬────────────────────────────┘
             │
        ┌────┴──────┐
        │ 解决了吗?  │
        └────┬───────┘
      是     │      否
      ├──────┘  ┌──────────────────────────┐
      │         │ 升级为标准工单           │
      │         │ 转给高工处理             │
      │         └──────────────────────────┘
      │
      ↓
┌─────────────────────────────────────────┐
│ 在"问题直接处理记录"中保存所有信息     │
│ 包括：                                  │
│ ├─ 问题描述和背景                      │
│ ├─ 识别的根本原因                      │
│ ├─ 执行的解决方案（步骤详细）         │
│ ├─ 验证方式（重复测试等）             │
│ ├─ 耗时和费用（如有）                 │
│ ├─ 客户反馈和满意度                   │
│ └─ 经验教训                            │
└────────────┬────────────────────────────┘
             │
      ┌──────┴─────────────────┐
      │ 自动沉淀到知识库        │
      │ （类似FAQ/案例库）      │
      └──────────────────────────┘
```

### 3.3 直接处理记录表设计

```sql
-- CSE直接处理的问题记录表
CREATE TABLE direct_issue_handling (
    handling_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    handling_no VARCHAR(20) NOT NULL UNIQUE,          -- DIH-YYYYMMDD-NNN
    
    -- 来源信息
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    device_id UUID REFERENCES devices(device_id),
    project_id UUID NOT NULL REFERENCES projects(project_id),
    
    -- 原始问题信息
    original_reporter_name VARCHAR(100),
    original_reporter_role VARCHAR(100),              -- 现场工人/工程师/采购等
    original_report_channel VARCHAR(50),              -- phone/wechat/in_person
    original_report_date TIMESTAMPTZ,
    
    -- 问题描述
    issue_title VARCHAR(200) NOT NULL,
    issue_description TEXT NOT NULL,
    issue_symptoms TEXT,                              -- 具体表现
    issue_impact VARCHAR(200),                        -- 影响范围（停线/产能降低/质量问题等）
    
    -- 处理信息
    handled_by_user_id UUID NOT NULL REFERENCES users(id),  -- 哪个CSE处理的
    handle_start_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    handle_end_time TIMESTAMPTZ,
    handle_duration_minutes INT,                      -- 处理耗时
    
    -- 问题分析
    root_cause VARCHAR(500),                          -- 根本原因分析
    is_known_issue BOOLEAN DEFAULT FALSE,             -- 是否是历史问题
    similar_issue_history TEXT,                       -- 是否遇到过类似问题
    
    -- 解决方案详解
    solution_title VARCHAR(200),
    solution_description TEXT NOT NULL,               -- 详细的解决步骤
    
    solution_type VARCHAR(50)                         -- 参数调整/重启/软件更新/配置修改/其他
        CHECK (solution_type IN ('parameter_change', 'reboot', 'software_update', 'config_change', 'procedure', 'other')),
    
    -- 解决验证
    verification_method TEXT,                         -- 如何验证问题已解决
    verification_result VARCHAR(20)                   -- passed/failed/partial
        CHECK (verification_result IN ('passed', 'failed', 'partial')),
    
    verification_details TEXT,                        -- 验证的具体细节
    
    -- 客户反馈
    customer_feedback TEXT,                           -- 客户对解决方案的反馈
    customer_satisfaction SMALLINT                    -- 1-5评分
        CHECK (customer_satisfaction IS NULL OR (customer_satisfaction >= 1 AND customer_satisfaction <= 5)),
    
    -- 知识库关联
    is_added_to_knowledge_base BOOLEAN DEFAULT FALSE, -- 是否已添加到知识库
    kb_reference_id UUID REFERENCES knowledge_base_articles(article_id),
    
    -- 成本信息
    parts_cost DECIMAL(10, 2),                        -- 零件费用（如有）
    travel_cost DECIMAL(10, 2),                       -- 差旅费（如有）
    notes TEXT,
    
    -- 状态
    status VARCHAR(20) DEFAULT 'completed'            -- completed/needs_escalation/failed
        CHECK (status IN ('completed', 'needs_escalation', 'failed')),
    
    escalated_to_ticket_id UUID REFERENCES tickets(ticket_id),  -- 如果升级为工单
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 创建索引
CREATE INDEX idx_direct_handling_customer ON direct_issue_handling(customer_id);
CREATE INDEX idx_direct_handling_handled_by ON direct_issue_handling(handled_by_user_id);
CREATE INDEX idx_direct_handling_created_at ON direct_issue_handling(created_at DESC);
CREATE INDEX idx_direct_handling_status ON direct_issue_handling(status);
```

---

## 四、转办流程（PM/销售转办）

### 4.1 PM转办流程

```
┌──────────────────────────────┐
│ 项目经理收到客户反馈          │
│ （通过微信/邮件/电话/会议）   │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ PM在系统中新建"转办任务"      │
│ 信息：                       │
│ ├─ 问题来源                 │
│ │  ├─ 原始来自：客户某部门  │
│ │  ├─ 反馈渠道：电话/邮件  │
│ │  └─ 反馈人：xxx          │
│ ├─ 问题描述（详细）         │
│ ├─ 问题分类（设备/工艺等）  │
│ ├─ 优先级（P1-P4）         │
│ ├─ 转给谁处理              │
│ │  ├─ 指定工程师           │
│ │  ├─ 部门（自动分配）     │
│ │  └─ 团队                │
│ ├─ 期望完成时间             │
│ └─ 备注                    │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ 系统自动：                   │
│ ├─ 关联问题来源记录          │
│ ├─ 创建追踪工单              │
│ ├─ 通知接收人（企业微信）    │
│ ├─ 计算SLA截止时间          │
│ └─ 添加到接收人的待办         │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ 工程师接收任务后处理          │
│ ├─ 进行问题分析诊断          │
│ ├─ 可能需要反馈给PM         │
│ │  └─ 需要更多信息           │
│ │  └─ 可能影响其他项目       │
│ │  └─ 成本和时间评估         │
│ ├─ 执行解决方案              │
│ ├─ 验证问题解决              │
│ └─ 记录所有处理过程          │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ PM收到完成通知              │
│ ├─ 审核解决方案              │
│ ├─ 可能需要现场验证          │
│ ├─ 向客户反馈解决情况        │
│ └─ 记录转办结果              │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ 工单关闭 + 经验沉淀          │
│ ├─ 添加到项目问题库          │
│ ├─ 添加到通用知识库          │
│ └─ 更新客户项目档案          │
└──────────────────────────────┘
```

### 4.2 PM转办任务表设计

```sql
-- PM转办任务表
CREATE TABLE pm_transfer_tasks (
    task_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    task_no VARCHAR(20) NOT NULL UNIQUE,              -- PMT-YYYYMMDD-NNN
    
    -- 来源信息
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    project_id UUID NOT NULL REFERENCES projects(project_id),
    device_id UUID REFERENCES devices(device_id),
    
    -- 问题信息
    issue_title VARCHAR(200) NOT NULL,
    issue_description TEXT NOT NULL,
    original_source TEXT,                             -- 问题的原始来源（谁反馈的）
    original_reporter_info TEXT,                      -- 原始报告人信息
    
    -- 报告渠道和时间
    report_channel VARCHAR(50),                       -- email/phone/wechat/meeting/other
    report_date TIMESTAMPTZ,
    
    -- PM信息
    pm_user_id UUID NOT NULL REFERENCES users(id),   -- 哪个PM创建的
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- 转给谁
    assigned_to_user_id UUID NOT NULL REFERENCES users(id),  -- 分配给哪个工程师
    assigned_department VARCHAR(100),                  -- 如果需要分配给整个部门
    assignment_reason TEXT,                           -- 为什么分配给这个人/部门
    assigned_at TIMESTAMPTZ,
    
    -- 优先级和SLA
    priority VARCHAR(5) DEFAULT 'P3'
        CHECK (priority IN ('P1', 'P2', 'P3', 'P4')),
    
    expected_complete_date TIMESTAMPTZ,               -- PM期望的完成时间
    sla_deadline TIMESTAMPTZ,                         -- 系统计算的SLA截止时间
    
    -- 处理进度
    status VARCHAR(20) DEFAULT 'assigned'
        CHECK (status IN ('assigned', 'in_progress', 'pending_info', 'completed', 'closed', 'escalated')),
    
    -- 工程师处理信息
    handled_by_user_id UUID REFERENCES users(id),    -- 实际处理的人（可能和assigned_to不同）
    handle_start_time TIMESTAMPTZ,
    handle_end_time TIMESTAMPTZ,
    
    -- 处理结果
    solution_summary TEXT,                            -- 解决方案摘要
    solution_details TEXT,                            -- 解决方案详细说明
    root_cause TEXT,                                  -- 根本原因分析
    
    -- 验证信息
    verification_method TEXT,                         -- 如何验证
    verification_completed BOOLEAN DEFAULT FALSE,
    verification_result VARCHAR(20),                  -- passed/failed/partial
    
    -- 成本信息
    actual_cost DECIMAL(15, 2),                       -- 实际成本（如有）
    estimated_cost DECIMAL(15, 2),                    -- 估计成本
    
    -- PM反馈
    pm_feedback TEXT,                                 -- PM对解决方案的评价
    pm_approval BOOLEAN DEFAULT FALSE,
    pm_approved_at TIMESTAMPTZ,
    
    -- 客户反馈
    customer_feedback TEXT,
    customer_satisfaction SMALLINT
        CHECK (customer_satisfaction IS NULL OR (customer_satisfaction >= 1 AND customer_satisfaction <= 5)),
    
    -- 关联关系
    related_ticket_id UUID REFERENCES tickets(ticket_id),  -- 如果转成正式工单
    related_direct_handling_id UUID REFERENCES direct_issue_handling(handling_id),  -- 如果是某个直接处理的追踪
    
    -- 知识库
    is_added_to_kb BOOLEAN DEFAULT FALSE,
    kb_article_id UUID REFERENCES knowledge_base_articles(article_id),
    
    notes TEXT,
    
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_pm_transfer_pm ON pm_transfer_tasks(pm_user_id);
CREATE INDEX idx_pm_transfer_assigned_to ON pm_transfer_tasks(assigned_to_user_id);
CREATE INDEX idx_pm_transfer_customer ON pm_transfer_tasks(customer_id);
CREATE INDEX idx_pm_transfer_status ON pm_transfer_tasks(status);
CREATE INDEX idx_pm_transfer_deadline ON pm_transfer_tasks(sla_deadline);
```

### 4.3 销售反馈转PM流程

```
┌──────────────────────────────┐
│ 销售收到客户反馈              │
│ （投诉/需求/建议）            │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ 销售在CRM系统中记录            │
│ ├─ 客户说了什么              │
│ ├─ 什么时间说的              │
│ ├─ 谁说的                   │
│ ├─ 反馈内容                 │
│ └─ 是否紧急                 │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ 销售通知相关的PM             │
│ （邮件/微信/会议）            │
│ ├─ 简述问题                 │
│ ├─ 客户重视程度             │
│ └─ 建议的处理方式           │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ PM确认收到，在系统中创建      │
│ 转办任务（PM_TRANSFER_TASK）  │
│ ├─ 标注来源：销售反馈         │
│ ├─ 关联销售信息              │
│ ├─ 分配给合适的工程师        │
│ ├─ 设置优先级                │
│ └─ 记录所有沟通过程          │
└────────────┬─────────────────┘
             │
             ↓
┌──────────────────────────────┐
│ 后续同PM转办相同流程          │
│ ├─ 工程师处理                │
│ ├─ PM验收                   │
│ ├─ 沉淀知识                 │
│ └─ 销售跟进客户满意度        │
└──────────────────────────────┘
```

---

## 五、支持热线/邮件问题收集

### 5.1 邮件自动入库流程

```
客户邮件
  ↓
邮件系统（自动转发到专用邮箱）
  ↓
邮件爬虫自动读取
  ↓
NLP自动分类
├─ 关键词识别（停线/故障/功能等）
├─ 客户识别（从邮件地址/签名）
├─ 项目识别
└─ 紧急程度判断
  ↓
自动创建问题记录
├─ 类型识别：是否已知问题
├─ 建议的接收人
└─ 初步优先级
  ↓
相关人员通知（企业微信推送）
```

### 5.2 热线问题记录表

```sql
-- 支持热线/邮件问题入库
CREATE TABLE support_channel_issues (
    issue_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    issue_no VARCHAR(20) NOT NULL UNIQUE,             -- SCI-YYYYMMDD-NNN
    
    -- 接收渠道信息
    channel_type VARCHAR(20) NOT NULL                 -- email/phone/wechat/form
        CHECK (channel_type IN ('email', 'phone', 'wechat', 'form')),
    
    channel_origin_id VARCHAR(500),                   -- 邮件ID/电话ID/消息ID等
    
    -- 来电/邮件人信息
    caller_name VARCHAR(100),
    caller_phone VARCHAR(20),
    caller_email VARCHAR(100),
    caller_company VARCHAR(200),                      -- 可能来自第三方
    
    -- 问题信息
    issue_title VARCHAR(200) NOT NULL,
    issue_description TEXT NOT NULL,
    
    -- 自动分类结果
    auto_classified_category VARCHAR(100),            -- NLP自动分类
    is_known_issue BOOLEAN,                           -- 是否是已知问题
    suggested_receiver_ids TEXT,                      -- 建议的接收人
    
    -- 接收人
    received_by_user_id UUID NOT NULL REFERENCES users(id),
    received_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- 处理
    status VARCHAR(20) DEFAULT 'received'
        CHECK (status IN ('received', 'in_progress', 'resolved', 'transferred', 'invalid')),
    
    assigned_to_user_id UUID REFERENCES users(id),
    handled_at TIMESTAMPTZ,
    
    -- 解决信息
    resolution_summary TEXT,
    resolution_sent_at TIMESTAMPTZ,
    
    -- 客户反馈
    customer_reply TEXT,
    satisfaction_score SMALLINT,
    
    -- 关联关系
    related_direct_handling_id UUID REFERENCES direct_issue_handling(handling_id),
    related_pm_task_id UUID REFERENCES pm_transfer_tasks(task_id),
    related_ticket_id UUID REFERENCES tickets(ticket_id),
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_support_issues_received_by ON support_channel_issues(received_by_user_id);
CREATE INDEX idx_support_issues_channel ON support_channel_issues(channel_type);
CREATE INDEX idx_support_issues_status ON support_channel_issues(status);
```

---

## 六、问题汇聚与智能路由

### 6.1 问题智能路由规则

```sql
-- 问题路由规则表
CREATE TABLE problem_routing_rules (
    rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_name VARCHAR(100) NOT NULL,
    
    -- 匹配条件（任何一个满足就触发）
    match_keywords TEXT,                              -- 关键词列表
    match_category VARCHAR(100),                      -- 问题分类
    match_product_type VARCHAR(100),                  -- 产品类型
    match_customer_id UUID REFERENCES customers(customer_id),  -- 特定客户
    match_department VARCHAR(100),                    -- 部门
    
    -- 路由决策
    route_type VARCHAR(50) NOT NULL                   -- direct_handle/pm_transfer/escalate/support
        CHECK (route_type IN ('direct_handle', 'pm_transfer', 'escalate', 'support')),
    
    route_to_user_id UUID REFERENCES users(id),       -- 默认转给谁
    route_to_department VARCHAR(100),                 -- 或转给哪个部门
    
    priority VARCHAR(5) DEFAULT 'P3',                 -- 自动设置的优先级
    
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 示例规则
INSERT INTO problem_routing_rules VALUES
('direct_handle', '参数调整', null, 'testing_equipment', null, null, 'direct_handle', CSE1_ID, null, 'P4', 'CSE可直接处理的参数调整问题'),
('pm_transfer', '停线问题', null, null, null, null, 'pm_transfer', null, 'PM', 'P1', '所有停线问题都转给PM处理'),
('escalate', '硬件故障', null, null, null, null, 'escalate', null, '机械部门', 'P1', '硬件故障问题升级到机械部门'),
('support', '邮件问题', null, null, null, null, 'support', null, null, 'P3', '邮件进来的问题标记为P3');
```

### 6.2 问题汇聚视图

```sql
-- 统一的问题视图（所有来源的问题）
CREATE VIEW unified_problem_queue AS
SELECT 
    -- 统一字段
    'direct_handling' as problem_type,
    handling_id as problem_id,
    handling_no as problem_no,
    customer_id,
    project_id,
    device_id,
    issue_title as problem_title,
    issue_description as problem_desc,
    handled_by_user_id as assigned_to,
    handle_start_time as created_at,
    status,
    NULL::UUID as ticket_id,
    customer_satisfaction,
    'CSE直接处理' as source_desc
    
FROM direct_issue_handling

UNION ALL

SELECT 
    'pm_transfer',
    task_id,
    task_no,
    customer_id,
    project_id,
    device_id,
    issue_title,
    issue_description,
    assigned_to_user_id,
    created_at,
    status,
    related_ticket_id,
    customer_satisfaction,
    'PM转办' as source_desc
    
FROM pm_transfer_tasks

UNION ALL

SELECT 
    'support_channel',
    issue_id,
    issue_no,
    NULL::UUID,  -- 可能没有customer_id
    NULL::UUID,
    NULL::UUID,
    issue_title,
    issue_description,
    assigned_to_user_id,
    received_at,
    status,
    related_ticket_id,
    satisfaction_score,
    '支持热线/邮件' as source_desc
    
FROM support_channel_issues

UNION ALL

SELECT 
    'standard_ticket',
    ticket_id,
    ticket_no,
    customer_id,
    project_id,
    device_id,
    CONCAT(step_name, ': ', symptom_title),
    symptom_detail,
    assigned_to_user_id,
    created_at,
    status,
    ticket_id,
    satisfaction_score,
    '标准工单系统' as source_desc
    
FROM tickets;

-- 查询示例：获取所有未解决的问题
SELECT * FROM unified_problem_queue 
WHERE status NOT IN ('resolved', 'completed', 'closed')
ORDER BY created_at DESC;
```

---

## 七、经验积累与知识库

### 7.1 自动沉淀流程

```
问题处理完成
    ↓
判断是否需要沉淀
├─ 是否是首次出现问题？
├─ 是否有明确的解决方案？
├─ 是否通过了验证？
└─ 是否有可复用价值？
    ↓
自动提取关键信息
├─ 问题的特征和症状
├─ 根本原因分析
├─ 解决方案步骤
├─ 验证方法
└─ 注意事项
    ↓
添加到知识库
├─ FAQ（常见问题库）
├─ 案例库（类似问题的案例）
├─ 故障排查指南
└─ 最佳实践
    ↓
关联到类似的历史问题
└─ 标记"类似问题已解决"
```

### 7.2 知识库自动推荐

```sql
-- 新问题创建时，自动推荐相似的解决方案
CREATE TABLE knowledge_base_recommendations (
    recommendation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    problem_source_type VARCHAR(50),        -- direct_handling/pm_transfer/ticket
    problem_id UUID,
    problem_title VARCHAR(200),
    
    -- 推荐的知识库文章
    kb_article_id UUID NOT NULL REFERENCES knowledge_base_articles(article_id),
    article_title VARCHAR(200),
    similarity_score DECIMAL(3, 2),         -- 相似度评分 0-1
    
    -- 推荐理由
    recommendation_reason TEXT,
    recommendation_score DECIMAL(5, 2),    -- 综合评分
    
    -- 是否被采用
    is_adopted BOOLEAN DEFAULT FALSE,
    adopted_at TIMESTAMPTZ,
    adoption_result VARCHAR(50),            -- solved/partially_solved/not_applicable
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 八、待办清单与追踪

### 8.1 统一的待办视图

```
对于不同角色，系统显示不同的待办清单：

【现场CSE的待办】
├─ 需要我直接处理的快速问题（< 2小时）
├─ PM转给我的任务
└─ 需要跟进的验证结果

【PM的待办】
├─ 销售反馈需要处理的问题
├─ 需要转办给工程师的任务
├─ 需要跟进的工程师处理进度
├─ 需要验收的解决方案
└─ 需要向客户反馈的结果

【工程师的待办】
├─ PM转给我的诊断任务
├─ 需要反馈给PM更多信息
├─ 需要验证的解决方案
└─ 需要记录的经验总结

【支持部门的待办】
├─ 邮件/热线进来的问题
├─ 需要分类转办的问题
└─ 需要反馈客户的结果
```

### 8.2 跨部门追踪示例

```
销售反馈客户投诉（停线）
    ↓
销售→PM：这个客户停线了，很着急！
    ↓
PM在系统中创建 PM_TRANSFER_TASK
├─ 问题：停线（P1）
├─ 来源：销售反馈，客户A
├─ 转给：机械工程师张三
└─ 期望：今天中午前解决
    ↓
系统自动：
├─ 推送给张三（企业微信、App、邮件）
├─ 添加到张三的待办
├─ 开始计算SLA
└─ 创建追踪记录
    ↓
张三收到后：
├─ 在待办中点击"开始处理"
├─ 初步诊断：控制柜温度过高
├─ 需要现场确认，通知PM去现场
└─ 更新任务状态
    ↓
PM现场确认后：
├─ 建议更换散热器（需要采购）
├─ 估计费用2000元，停线1小时
└─ 等待客户确认
    ↓
客户确认同意后：
├─ 张三执行更换
├─ 拍照记录整个过程
├─ 测试验证，设备恢复正常
└─ 在系统中提交完成报告
    ↓
PM审核：
├─ 查看张三的处理记录
├─ 验证设备确实正常了
├─ 标记任务完成
└─ 向销售和客户反馈解决结果
    ↓
系统自动：
├─ 提取问题、原因、方案沉淀到知识库
├─ 关联到设备的维修历史
├─ 更新客户的项目档案
└─ 记录处理成本和时间统计
```

---

## 九、数据流向和关系图

```
┌─────────────────────────────────────────────────────────┐
│                 问题来源追踪表                           │
│              (problem_sources)                          │
│                                                         │
│ 记录每个问题的原始来源、接收者、路由方式              │
│ 可关联到：直接处理、PM转办、工单、支持渠道            │
└────────────┬────────────────────────────────────────────┘
             │
      ┌──────┼──────┬──────┬──────┐
      ↓      ↓      ↓      ↓      ↓
┌─────────┐ ┌──────────┐ ┌────────┐ ┌──────────┐ ┌──────────┐
│ 直接处理 │ │ PM转办   │ │ 工单   │ │ 支持热线 │ │ 销售反馈 │
│ handling │ │ transfer │ │ ticket │ │ support  │ │ (CRM)    │
└────┬────┘ └────┬─────┘ └────┬───┘ └────┬─────┘ └────┬─────┘
     │           │            │          │            │
     │           ↓            │          │            │
     │        ┌─────────────────────────────────────┐ │
     └────────│ 统一问题汇聚库/知识库               │ │
              │ (unified_problem_queue)             │ │
              │                                     │ │
              │ 包含所有来源的问题，用于报表/分析 │ │
              └─────────────────────────────────────┘ │
                     ↑                                 │
                     │                                 │
                     └─────────────────────────────────┘
```

---

## 十、实施建议

### 10.1 第一阶段：问题收集机制（第1-2周）

**关键任务：**
1. ✓ 建立问题来源追踪表
2. ✓ 建立直接处理记录表
3. ✓ 现场CSE工具升级（扫码后支持快速问题记录）
4. ✓ PM转办任务系统上线
5. ✓ 定义问题分类和路由规则

**涉及角色培训：**
- CSE：如何快速记录问题、何时转办
- PM：如何在系统中转办任务、如何追踪进度
- 销售：如何在CRM中记录客户反馈

### 10.2 第二阶段：经验积累（第3-4周）

**关键任务：**
1. ✓ 建立知识库自动沉淀机制
2. ✓ 关联历史问题，建立案例库
3. ✓ 相似问题自动推荐
4. ✓ FAQ的自动整理

### 10.3 第三阶段：智能分析（第5-6周）

**关键任务：**
1. ✓ 问题统计报表（来源、处理方式、成功率等）
2. ✓ CSE工作量分析（解决速度、满意度等）
3. ✓ PM转办效率分析
4. ✓ 客户问题频率监控

---

## 十一、关键的SQL查询

### 11.1 获取特定客户的所有问题（不论来源）

```sql
-- 获取客户A的所有处理过的问题
SELECT 
    'direct' as source,
    handling_no, issue_title, handle_duration_minutes, 
    handle_end_time, verification_result, customer_satisfaction
FROM direct_issue_handling
WHERE customer_id = '客户A_ID' AND status = 'completed'

UNION ALL

SELECT 
    'pm_transfer',
    task_no, issue_title, 
    EXTRACT(MINUTE FROM (handle_end_time - handle_start_time)),
    handle_end_time, verification_result, customer_satisfaction
FROM pm_transfer_tasks
WHERE customer_id = '客户A_ID' AND status = 'completed'

ORDER BY handle_end_time DESC;
```

### 11.2 CSE的工作统计

```sql
-- 某CSE本月的处理统计
SELECT 
    COUNT(*) as total_handled,
    COUNT(CASE WHEN verification_result = 'passed' THEN 1 END) as solved_count,
    COUNT(CASE WHEN verification_result = 'failed' THEN 1 END) as failed_count,
    AVG(handle_duration_minutes) as avg_time,
    AVG(customer_satisfaction) as avg_satisfaction,
    SUM(CASE WHEN status = 'needs_escalation' THEN 1 ELSE 0 END) as escalation_count
FROM direct_issue_handling
WHERE handled_by_user_id = 'CSE_ID'
    AND EXTRACT(MONTH FROM handle_end_time) = EXTRACT(MONTH FROM NOW());
```

### 11.3 问题来源热力图

```sql
-- 过去三个月，按来源分析问题数量
SELECT 
    original_source_type,
    received_by_role,
    COUNT(*) as problem_count,
    AVG(CAST(EXTRACT(EPOCH FROM (resolved_at - created_at))/3600 AS NUMERIC)) as avg_resolve_hours
FROM problem_sources
WHERE created_at >= NOW() - INTERVAL '3 months'
GROUP BY original_source_type, received_by_role
ORDER BY problem_count DESC;
```

---

**这个设计的核心优势：**

✅ **多源统一**：所有问题来源（现场、PM、销售、支持热线）都汇聚到同一个系统
✅ **完整追踪**：从问题出现→接收→处理→验证→沉淀，全流程可见
✅ **双轨并行**：既支持CSE直接快速处理，也支持PM转办的正式流程
✅ **经验沉淀**：每个问题都成为知识库的资源，避免重复处理
✅ **灵活分析**：可从多维度分析问题来源、处理效率、CSE绩效等

你现在可以：
1. 通过"直接处理"快速解决简单问题，提高响应速度
2. 通过"PM转办"规范复杂问题的处理流程
3. 通过"问题来源追踪"完整了解问题的全生命周期
4. 通过"知识库沉淀"积累经验，提高未来的处理效率
