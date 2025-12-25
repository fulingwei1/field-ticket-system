# 现场问题结构化上报系统 - 开发规格书

> **版本**: 1.0  
> **最后更新**: 2025-12-18  
> **交付标准**: Docker Compose 一键启动，可运行的完整系统

---

## 一、产品概述

### 1.1 产品目标

1. **现场端**：让初级工程师/助理/客服在现场用手机 **2分钟内** 完成问题提交（结构化事实 + 证据 + 版本信息）
2. **公司端**：让高级工程师 **不依赖电话追问** 即可远程定位，并下发版本化解决方案
3. **全链路可追溯**：问题包 → 判断卡 → 解决方案 → 版本下发 → 现场验证 → 客户沟通记录

### 1.2 非目标（首版不做）

- 不做 PLC/设备的实时控制
- 不做复杂 AI 自动归因（先把数据管线跑通，AI 作为二期增强）
- 不做完整 CRM（只保留与问题处置相关的客户信息字段）

### 1.3 技术栈

| 层级 | 技术选型 | 说明 |
|------|----------|------|
| 移动端 | Flutter 3.x | 一套代码双端，支持离线 |
| Web管理端 | React 18 + Ant Design 5 | 或 Vue3 + Element Plus |
| 后端API | .NET 8 + ASP.NET Core Minimal API | RESTful |
| ORM | EF Core 8 | Code First |
| 数据库 | PostgreSQL 16 | JSON 字段支持 |
| 缓存 | Redis 7 | 幂等性、Session |
| 对象存储 | MinIO | 附件存储 |
| 容器化 | Docker Compose | 一键部署 |

---

## 二、身份认证与权限模型

### 2.1 登录方式：企业微信 OAuth2

```
流程：
1. App 点击"企业微信登录" 
2. 跳转企业微信授权页面（带 state 防 CSRF）
3. 用户确认授权
4. 回调后端 /auth/wecom/callback?code=xxx
5. 后端用 code 换取 access_token + userid
6. 查询用户详情（姓名、部门、手机号）
7. Upsert 到 users 表
8. 签发 JWT（有效期 7 天，支持刷新）
9. 返回 App，存储 Token
```

**企业微信配置清单（运维需准备）：**
- CorpId：企业ID
- AgentId：自建应用ID
- Secret：应用密钥
- 回调域名：OAuth Redirect URL（需备案）
- 可信IP：API服务器IP

### 2.2 角色与权限矩阵

| 权限项 | FieldEngineer | CS | SeniorEngineer | Admin |
|--------|---------------|-----|----------------|-------|
| 创建工单草稿 | ✅ | ✅ | ✅ | ✅ |
| 编辑草稿事实项 | ✅ | ❌ | ❌ | ✅ |
| 提交工单 | ✅ | ✅ | ✅ | ✅ |
| 追加证据（已提交） | ✅ | ✅ | ✅ | ✅ |
| 修改已提交事实项 | ❌ | ❌ | ❌ | ✅ |
| 分诊/关联判断卡 | ❌ | ❌ | ✅ | ✅ |
| 发起追问 | ❌ | ❌ | ✅ | ✅ |
| 创建解决方案 | ❌ | ❌ | ✅ | ✅ |
| 发布解决方案 | ❌ | ❌ | ✅ | ✅ |
| 提交验证结果 | ✅ | ❌ | ✅ | ✅ |
| 关闭工单 | ❌ | ❌ | ✅ | ✅ |
| 重开已关闭工单 | ❌ | ❌ | ❌ | ✅ |
| 客户沟通记录 | ✅ | ✅ | ✅ | ✅ |
| 管理基础数据 | ❌ | ❌ | ❌ | ✅ |
| 管理用户权限 | ❌ | ❌ | ❌ | ✅ |

**权限实现要求**：
- API 层必须校验，不能只靠前端隐藏按钮
- 数据级权限：FieldEngineer 只能看自己创建的工单，SeniorEngineer/Admin 看全部

---

## 三、工单状态机（必须严格遵循）

```
┌─────────┐
│  Draft  │ ← 仅创建者可见，可编辑事实项
└────┬────┘
     │ submit（校验必填项）
     ▼
┌───────────┐
│ Submitted │ ← 进入队列，禁止修改事实项，只能追加证据
└─────┬─────┘
      │ triage（高级工程师）
      ▼
┌─────────┐
│ Triage  │ ← 可发起追问，关联判断卡
└────┬────┘
     │ issue_solution
     ▼
┌────────────────┐
│ SolutionIssued │ ← SOL 发布，自动通知现场
└───────┬────────┘
        │ submit_verification
        ▼
┌───────────┐
│ Verifying │ ← 现场提交验证结果
└─────┬─────┘
      │ close（PASS）或 reopen（FAIL）
      ▼
┌────────┐
│ Closed │ ← 冻结，仅 Admin 可 reopen
└────────┘
```

**状态流转规则**：
- Draft → Submitted：必须通过必填校验
- Submitted → Triage：高级工程师操作
- Triage → SolutionIssued：发布 SOL 时自动流转
- SolutionIssued → Verifying：现场提交验证时自动流转
- Verifying → Closed：验证 PASS 且高级工程师确认
- Verifying → Triage：验证 FAIL，需重新分诊
- Closed → Submitted：仅 Admin 可 reopen

---

## 四、页面清单

### 4.1 App（Flutter）- 现场/客服用

| 序号 | 页面名称 | 核心功能 | 角色 |
|------|----------|----------|------|
| A1 | 登录页 | 企业微信OAuth授权入口 | 所有 |
| A2 | 首页/工单列表 | 我的工单、筛选（客户/设备/状态/问题域/紧急度） | 所有 |
| A3 | 工单创建-Step1 | 扫码/选择设备，预填客户项目信息 | FE/CS |
| A4 | 工单创建-Step2 | 选问题域(A-E)、步骤、版本确认 | FE/CS |
| A5 | 工单创建-Step3 | YES/NO事实表填写 | FE/CS |
| A6 | 工单创建-Step4 | 上传证据（拍照/视频/文件） | FE/CS |
| A7 | 工单创建-Step5 | 预览确认、提交 | FE/CS |
| A8 | 工单详情 | 只读信息+时间线+可执行动作 | 所有 |
| A9 | 证据上传页 | 断点续传、进度显示、重试 | FE |
| A10 | 追问回复页 | 查看追问、提交答案+证据 | FE |
| A11 | SOL详情页 | 查看解决方案、验证清单 | FE |
| A12 | 验证执行页 | Checklist勾选、上传验证证据、提交结果 | FE |
| A13 | 客户沟通记录页 | 新增/查看沟通记录 | FE/CS |
| A14 | 离线草稿箱 | 本地草稿、待提交队列、同步状态 | 所有 |
| A15 | 设置页 | 账号、版本、缓存清理、网络状态 | 所有 |

### 4.2 Web管理端（React）- 高级工程师/管理员用

| 序号 | 页面名称 | 核心功能 | 角色 |
|------|----------|----------|------|
| W1 | 登录页 | 企业微信扫码登录 | SE/Admin |
| W2 | 工单看板 | 四列看板（Submitted/Triage/SolutionIssued/Verifying） | SE/Admin |
| W3 | 工单列表 | 高级筛选、批量操作、导出 | SE/Admin |
| W4 | 工单详情 | 完整时间线、所有关联数据 | SE/Admin |
| W5 | 分诊面板 | 推荐判断卡、关联操作、发起追问 | SE |
| W6 | 解决方案编辑器 | SOL创建/编辑、验证清单、版本选择 | SE |
| W7 | 解决方案列表 | SOL历史、状态筛选 | SE/Admin |
| W8 | 判断卡库 | 卡片列表、版本管理、CRUD | SE/Admin |
| W9 | 判断卡编辑器 | 卡片内容编辑、适用范围配置 | SE/Admin |
| W10 | 版本库 | PLC/软件/参数包管理、上传、发布 | SE/Admin |
| W11 | 统计报表 | 闭环时长、Top问题域、返工率等 | SE/Admin |
| W12 | 客户管理 | 客户档案CRUD | Admin |
| W13 | 项目管理 | 项目档案CRUD | Admin |
| W14 | 设备管理 | 设备档案CRUD、二维码生成 | Admin |
| W15 | 用户管理 | 用户列表、角色分配、启用/禁用 | Admin |
| W16 | 字典管理 | 问题域、步骤、版本渠道等配置 | Admin |

---

## 五、数据库设计（PostgreSQL + EF Core）

### 5.1 用户与组织

```sql
-- 用户表
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    corp_id VARCHAR(64) NOT NULL,           -- 企业微信企业ID
    wecom_userid VARCHAR(64) NOT NULL,      -- 企业微信用户ID
    name VARCHAR(100) NOT NULL,
    mobile VARCHAR(20),
    dept_id VARCHAR(64),
    role VARCHAR(20) NOT NULL CHECK (role IN ('FieldEngineer', 'CS', 'SeniorEngineer', 'Admin')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(corp_id, wecom_userid)
);

-- 部门表（可选，从企业微信同步）
CREATE TABLE departments (
    dept_id VARCHAR(64) PRIMARY KEY,
    corp_id VARCHAR(64) NOT NULL,
    name VARCHAR(200) NOT NULL,
    parent_id VARCHAR(64)
);
```

### 5.2 客户与设备档案

```sql
-- 客户表
CREATE TABLE customers (
    customer_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    short_name VARCHAR(50),
    contact_person VARCHAR(100),
    contact_phone VARCHAR(20),
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 项目表
CREATE TABLE projects (
    project_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50),                       -- 项目编号
    site_address TEXT,                      -- 现场地址
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 工位表
CREATE TABLE stations (
    station_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL REFERENCES projects(project_id),
    name VARCHAR(100) NOT NULL,             -- 如：1号线工位3
    code VARCHAR(50),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 设备表
CREATE TABLE devices (
    device_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL REFERENCES projects(project_id),
    station_id UUID REFERENCES stations(station_id),
    device_sn VARCHAR(100) NOT NULL UNIQUE, -- 设备序列号
    qr_code VARCHAR(64) UNIQUE,             -- 二维码内容（可与SN相同）
    model VARCHAR(100),                     -- 设备型号
    site_address TEXT,                      -- 设备安装地址
    default_sw_ver VARCHAR(50),             -- 默认软件版本
    default_plc_ver VARCHAR(50),            -- 默认PLC版本
    default_param_ver VARCHAR(50),          -- 默认参数版本
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_devices_sn ON devices(device_sn);
CREATE INDEX idx_devices_qr ON devices(qr_code);
```

### 5.3 工单主表

```sql
CREATE TABLE tickets (
    ticket_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_no VARCHAR(20) NOT NULL UNIQUE,  -- 工单编号 TK-YYYYMMDD-NNN
    
    -- 关联信息
    customer_id UUID NOT NULL REFERENCES customers(customer_id),
    project_id UUID NOT NULL REFERENCES projects(project_id),
    device_id UUID NOT NULL REFERENCES devices(device_id),
    station_id UUID REFERENCES stations(station_id),
    
    -- 创建者
    created_by_user_id UUID NOT NULL REFERENCES users(id),
    
    -- 问题描述
    domain CHAR(1) NOT NULL CHECK (domain IN ('A', 'B', 'C', 'D', 'E')),
    step_code VARCHAR(20) NOT NULL,         -- 如 Step_120
    step_name VARCHAR(100),                 -- 步骤名称
    symptom_title VARCHAR(200) NOT NULL,    -- 一句话描述
    symptom_detail TEXT,                    -- 详细描述（可选）
    
    -- 复现性
    repro_rate SMALLINT CHECK (repro_rate >= 0 AND repro_rate <= 100),
    reboot_recovers BOOLEAN,                -- 重启是否恢复
    env_related BOOLEAN,                    -- 是否与环境相关
    
    -- 版本信息（必填）
    sw_version VARCHAR(50) NOT NULL,
    plc_version VARCHAR(50) NOT NULL,
    param_version VARCHAR(50) NOT NULL,
    
    -- 结构化事实（YES/NO表，按问题域分类）
    facts_json JSONB NOT NULL DEFAULT '{}', -- 详细结构见下方说明
    
    -- 现场已执行动作
    actions_taken VARCHAR(50)[] DEFAULT '{}', -- 如 {'REBOOT', 'RELOAD_PROGRAM', 'REPLACE_FIXTURE'}
    actions_taken_note TEXT,                  -- 其他动作说明
    
    -- 上报确认
    confirmed_as_fact BOOLEAN DEFAULT FALSE,  -- 确认为事实（非主观判断）
    confirmed_at TIMESTAMPTZ,
    
    -- 报警信息
    alarm_code VARCHAR(50),                   -- 报警代码
    
    -- 状态与优先级
    status VARCHAR(20) NOT NULL DEFAULT 'Draft' 
        CHECK (status IN ('Draft', 'Submitted', 'Triage', 'SolutionIssued', 'Verifying', 'Closed')),
    priority VARCHAR(5) DEFAULT 'P3' CHECK (priority IN ('P1', 'P2', 'P3', 'P4')),
    
    -- 分诊关联
    current_jc_code VARCHAR(20),            -- 当前关联的判断卡
    assigned_to UUID REFERENCES users(id),  -- 分配给谁处理
    
    -- 时间戳
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    submitted_at TIMESTAMPTZ,
    closed_at TIMESTAMPTZ,
    
    -- 本地草稿同步
    local_draft_id VARCHAR(64),             -- App端本地草稿ID
    idempotency_key VARCHAR(64) UNIQUE      -- 幂等键
);

CREATE INDEX idx_tickets_status ON tickets(status);
CREATE INDEX idx_tickets_customer ON tickets(customer_id);
CREATE INDEX idx_tickets_device ON tickets(device_id);
CREATE INDEX idx_tickets_creator ON tickets(created_by_user_id);
CREATE INDEX idx_tickets_domain ON tickets(domain);
CREATE INDEX idx_tickets_created ON tickets(created_at DESC);

/*
 * facts_json 详细结构说明（YES/NO事实表）
 * 
 * 设计原则：只勾选 + 填数字 + 上传证据，不允许主观判断
 * 值类型：YES/NO/NA（NA表示不适用）或 true/false/null
 */
COMMENT ON COLUMN tickets.facts_json IS '
{
  "mechanical": {
    "action_completed": "YES|NO|NA",          -- 动作是否完成
    "position_reliable": "YES|NO|NA",         -- 到位是否可靠
    "jam_or_noise": "YES|NO|NA",              -- 是否卡滞/异音
    "manual_help_recovers": "YES|NO|NA",      -- 人工辅助后是否恢复
    "notes": "",                              -- 备注
    "attachment_ids": []                      -- 关联的附件ID
  },
  "electrical": {
    "sensor_physical_ok": "YES|NO|NA",        -- 传感器物理状态正常
    "plc_io_changes": "YES|NO|NA",            -- PLC中IO有变化
    "similar_points_ok": "YES|NO|NA",         -- 同类点位是否正常
    "io_screenshot_attachment_ids": []        -- IO截图附件ID
  },
  "plc": {
    "stuck_step_code": "Step_120_Clamp_Check",-- 当前卡在步骤号
    "stuck_step_name": "夹具到位检测",         -- 步骤名称（可选）
    "stuck_fixed": "YES|NO|NA",               -- 是否固定卡在该步骤
    "manual_single_step_pass": "YES|NO|NA",   -- 手动/单步是否可通过
    "alarm_code": "",                         -- 报警代码
    "log_attachment_ids": []                  -- 日志附件ID
  },
  "test": {
    "same_unit_repeat_consistent": "YES|NO|NA",  -- 同一产品重复测试结果一致
    "swap_unit_recovers": "YES|NO|NA",           -- 更换产品是否恢复
    "near_limits": "YES|NO|NA",                  -- 测试值接近上下限
    "fail_item": "",                             -- FAIL测试项名称
    "value": "",                                 -- 当前测试值
    "ll": "",                                    -- 下限(Lower Limit)
    "ul": "",                                    -- 上限(Upper Limit)
    "data_screenshot_attachment_ids": []         -- 数据截图附件ID
  },
  "environment": {
    "repro_rate": 100,                        -- 复现率(0-100)，必填
    "reboot_recovers": true,                  -- 重启后是否恢复
    "env_related": false,                     -- 是否与环境相关
    "temperature_c": null,                    -- 环境温度（摄氏度，可选）
    "time_related": "YES|NO|NA"               -- 是否与时间相关
  }
}
';

/*
 * actions_taken 字典值
 */
COMMENT ON COLUMN tickets.actions_taken IS '
可选值：
- REBOOT: 重启设备
- RELOAD_PROGRAM: 重新下载程序  
- REPLACE_FIXTURE: 更换治具/工装
- REPLACE_PRODUCT: 更换产品
- ADJUST_PARAM: 调整参数
- CLEAN: 清洁/除尘
- OTHER: 其他（详见 actions_taken_note）
';
```

### 5.4 附件表

```sql
CREATE TABLE attachments (
    attachment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    uploaded_by UUID NOT NULL REFERENCES users(id),
    
    -- 文件信息
    file_type VARCHAR(20) NOT NULL CHECK (file_type IN ('photo', 'video', 'log', 'file')),
    file_name VARCHAR(255) NOT NULL,
    file_key VARCHAR(500) NOT NULL,         -- 对象存储 Key
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100),
    sha256 VARCHAR(64),
    
    -- 上传状态（支持断点续传）
    upload_status VARCHAR(20) DEFAULT 'completed' 
        CHECK (upload_status IN ('pending', 'uploading', 'completed', 'failed')),
    upload_id VARCHAR(200),                 -- 分片上传ID
    uploaded_chunks INTEGER DEFAULT 0,
    total_chunks INTEGER,
    
    -- 标注（可选）
    tags JSONB DEFAULT '[]',                -- 日志关键字、截图标注等
    description TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_attachments_ticket ON attachments(ticket_id);
```

### 5.5 判断卡

```sql
CREATE TABLE judgement_cards (
    jc_code VARCHAR(20) PRIMARY KEY,        -- JC-XXX
    version INTEGER NOT NULL DEFAULT 1,
    
    -- 适用范围
    applicable_domains CHAR(1)[] NOT NULL,  -- {'A', 'B', 'C'}
    applicable_steps VARCHAR(20)[],         -- {'Step_100', 'Step_120'}
    keywords VARCHAR(100)[],                -- 关键词匹配
    
    -- 基本信息
    title VARCHAR(200) NOT NULL,            -- 判断卡标题
    description TEXT,                       -- 描述
    
    -- 判断目标（高级工程师要判断什么）
    judgement_target TEXT NOT NULL,         -- 如："夹具到位是否被PLC正确判定"
    
    -- 关键判断依据（需要从上报表获取的信息）
    key_judgement_factors JSONB NOT NULL,   -- 如：["物理到位", "IO变化", "程序步骤"]
    
    -- 现场需确认项（对应上报表的 YES/NO）
    key_checks_json JSONB NOT NULL,         -- [{"item": "动作完成", "domain": "A", "field": "action_completed", "required": true}]
    
    -- 判定边界（什么情况下判定为什么结论）
    decision_boundary_json JSONB,           
    /*
    示例：
    [
      {
        "condition": "机械OK + 物理传感器OK + PLC IO异常 + 固定步骤",
        "conclusion": "程序逻辑/IO判定窗口问题",
        "confidence": "high"
      },
      {
        "condition": "机械OK + 物理传感器异常",
        "conclusion": "传感器故障或安装问题",
        "confidence": "high"
      }
    ]
    */
    
    -- 典型失效模式
    failure_modes_json JSONB,               
    /*
    示例：
    [
      {"mode": "IO去抖不足", "symptom": "信号抖动导致误判", "solution_hint": "增加滤波时间"},
      {"mode": "超时窗口过短", "symptom": "正常动作也超时", "solution_hint": "延长超时时间"}
    ]
    */
    
    -- 标准处置建议
    actions_json JSONB,                     
    /*
    示例：
    [
      {"action": "检查IO滤波参数", "priority": 1},
      {"action": "对比正常设备的步骤耗时", "priority": 2},
      {"action": "检查机械动作到位稳定性", "priority": 3}
    ]
    */
    
    -- 关联的常见解决方案模板
    solution_templates JSONB,               -- 预设的SOL模板
    
    -- 状态
    is_active BOOLEAN DEFAULT TRUE,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 判断卡变更记录
CREATE TABLE jc_change_logs (
    id BIGSERIAL PRIMARY KEY,
    jc_code VARCHAR(20) NOT NULL REFERENCES judgement_cards(jc_code),
    from_version INTEGER NOT NULL,
    to_version INTEGER NOT NULL,
    change_summary TEXT,
    change_detail JSONB,                    -- 详细变更内容
    changed_by UUID REFERENCES users(id),
    changed_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_jc_domains ON judgement_cards USING GIN (applicable_domains);
CREATE INDEX idx_jc_steps ON judgement_cards USING GIN (applicable_steps);
CREATE INDEX idx_jc_keywords ON judgement_cards USING GIN (keywords);
```

### 5.6 分诊与追问

```sql
-- 分诊记录
CREATE TABLE triage_notes (
    id BIGSERIAL PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    by_user_id UUID NOT NULL REFERENCES users(id),
    jc_code VARCHAR(20) REFERENCES judgement_cards(jc_code),
    note TEXT,
    conclusion TEXT,                        -- 当前判断结论
    next_action TEXT,                       -- 下一步动作
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 追问
CREATE TABLE followup_questions (
    id BIGSERIAL PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    asked_by UUID NOT NULL REFERENCES users(id),
    question_text TEXT NOT NULL,
    is_required BOOLEAN DEFAULT FALSE,
    status VARCHAR(20) DEFAULT 'open' CHECK (status IN ('open', 'answered', 'cancelled')),
    answer_text TEXT,
    answered_by UUID REFERENCES users(id),
    answered_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_followups_ticket ON followup_questions(ticket_id);
```

### 5.7 解决方案

```sql
CREATE TABLE solutions (
    solution_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    solution_code VARCHAR(20) NOT NULL UNIQUE, -- SOL-YYYY-NNN
    
    -- 关联
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    jc_code VARCHAR(20) REFERENCES judgement_cards(jc_code),
    
    -- 判断结论（来自分诊）
    diagnosis_conclusion TEXT,              -- 如："程序逻辑/IO判定窗口问题"
    
    -- 修改内容（结构化）
    change_detail_json JSONB NOT NULL,      
    /*
    示例：
    {
      "changes": [
        {
          "type": "PLC_LOGIC",
          "location": "Step_120 到位信号确认逻辑",
          "before": "无滤波",
          "after": "增加50ms滤波",
          "reason": "消除信号抖动"
        },
        {
          "type": "PARAMETER",
          "location": "Step_120 超时时间",
          "before": "300ms",
          "after": "800ms",
          "reason": "适应正常动作时间波动"
        }
      ]
    }
    */
    
    -- 摘要（给现场看的简要说明）
    summary TEXT NOT NULL,                  
    
    -- 发布类型与版本
    release_type VARCHAR(20) NOT NULL 
        CHECK (release_type IN ('PLC', 'SOFTWARE', 'PARAM', 'MECHANICAL', 'PROCESS', 'MIXED')),
    release_version VARCHAR(50),            -- 发布的版本号，如 v1.2.7
    package_key VARCHAR(500),               -- 版本包对象存储Key
    
    -- 验证要求（结构化）
    verification_checklist_json JSONB NOT NULL DEFAULT '{}',
    /*
    完整结构示例：
    {
      "precheck": [
        "确认设备处于停止状态",
        "备份当前 PLC 程序版本"
      ],
      "steps": [
        {
          "id": "S1",
          "text": "下载新版本 PLC v1.2.7",
          "type": "action",
          "required": true
        },
        {
          "id": "S2",
          "text": "连续运行 20 次",
          "type": "test",
          "required": true,
          "acceptance_criteria": "20/20 PASS",
          "record_field": "run_count"
        },
        {
          "id": "S3",
          "text": "记录 Step_120 实际用时",
          "type": "data_collection",
          "required": false,
          "record_field": "step_120_time"
        }
      ],
      "acceptance": "20/20 PASS 且无新增报警",
      "rollback": "若 FAIL，回滚 PLC v1.2.6"
    }
    */
    
    -- 验收标准（文字描述）
    acceptance_criteria TEXT,               -- 如："连续20次无超时报警"
    
    -- 回滚策略
    rollback_plan TEXT,                     -- 如："恢复 PLC v1.2.6，参数不变"
    rollback_version VARCHAR(50),           -- 回滚到的版本
    
    -- 发布范围
    applicable_customers UUID[],            -- 空=所有
    applicable_devices UUID[],              -- 空=所有
    applicable_models VARCHAR(100)[],       -- 适用的设备型号
    
    -- 状态
    status VARCHAR(20) DEFAULT 'Draft' CHECK (status IN ('Draft', 'Published', 'Withdrawn')),
    
    -- 时间与人员
    created_by UUID NOT NULL REFERENCES users(id),
    published_by UUID REFERENCES users(id),
    published_at TIMESTAMPTZ,
    withdrawn_at TIMESTAMPTZ,
    withdrawn_reason TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_solutions_ticket ON solutions(ticket_id);
CREATE INDEX idx_solutions_status ON solutions(status);
CREATE INDEX idx_solutions_jc ON solutions(jc_code);
```

### 5.8 验证记录

```sql
CREATE TABLE verifications (
    verification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    solution_id UUID NOT NULL REFERENCES solutions(solution_id),
    
    -- 执行信息
    executed_by UUID NOT NULL REFERENCES users(id),
    run_count INTEGER NOT NULL DEFAULT 1,   -- 验证次数
    pass_count INTEGER NOT NULL DEFAULT 0,  -- 通过次数
    
    -- 结果
    result VARCHAR(20) NOT NULL CHECK (result IN ('PASS', 'FAIL', 'PARTIAL')),
    checklist_result_json JSONB,            -- 每项检查结果
    notes TEXT,
    
    -- 证据
    evidence_attachment_ids UUID[],         -- 关联的附件ID
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_verifications_ticket ON verifications(ticket_id);
```

### 5.9 客户沟通记录

```sql
-- 客户沟通话术模板
CREATE TABLE comm_templates (
    template_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,       -- 模板编号
    name VARCHAR(100) NOT NULL,             -- 模板名称
    scenario VARCHAR(50) NOT NULL,          -- 使用场景：problem_confirmed/solution_issued/verification_pass
    content_template TEXT NOT NULL,         -- 模板内容，支持变量替换
    /*
    示例：
    "我们已确认问题发生在{step_name}，公司已针对该步骤优化控制逻辑并完成验证，
    当前版本 {release_version} 运行稳定。如有问题请随时联系。"
    
    可用变量：
    - {customer_name}: 客户名称
    - {device_sn}: 设备SN
    - {symptom_title}: 问题描述
    - {step_name}: 步骤名称
    - {diagnosis_conclusion}: 判断结论
    - {solution_code}: 解决方案编号
    - {release_version}: 发布版本
    - {verification_result}: 验证结果
    */
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 预置话术模板
INSERT INTO comm_templates (code, name, scenario, content_template) VALUES
('TPL_PROBLEM_CONFIRMED', '问题已确认', 'problem_confirmed', 
 '您好，我们已收到关于{device_sn}设备的问题反馈（{symptom_title}），目前正在分析中，预计{expected_hours}小时内给出解决方案，请您耐心等待。'),
('TPL_SOLUTION_ISSUED', '方案已发布', 'solution_issued',
 '我们已确认问题发生在{step_name}，公司已针对该步骤优化控制逻辑（{solution_code}），当前版本{release_version}已发布，现场工程师将尽快完成验证。'),
('TPL_VERIFICATION_PASS', '验证通过', 'verification_pass',
 '解决方案{solution_code}已在现场验证通过，设备{device_sn}运行正常。如后续有任何问题，请随时联系我们。');

-- 客户沟通记录
CREATE TABLE customer_comms (
    id BIGSERIAL PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id),
    solution_id UUID REFERENCES solutions(solution_id),  -- 可关联到具体SOL
    by_user_id UUID NOT NULL REFERENCES users(id),
    
    comm_type VARCHAR(20) NOT NULL CHECK (comm_type IN ('call', 'wechat', 'email', 'onsite', 'sms')),
    template_id UUID REFERENCES comm_templates(template_id),  -- 使用的模板
    
    -- 沟通内容
    content TEXT NOT NULL,                  -- 实际沟通内容（模板渲染后或自定义）
    customer_contact VARCHAR(100),          -- 对方联系人
    customer_feedback TEXT,                 -- 客户反馈
    follow_up_required BOOLEAN DEFAULT FALSE,  -- 是否需要跟进
    follow_up_note TEXT,                    -- 跟进说明
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_comms_ticket ON customer_comms(ticket_id);
CREATE INDEX idx_comms_solution ON customer_comms(solution_id);
```

### 5.10 审计日志

```sql
CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL,       -- ticket/solution/jc/user
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,            -- create/update/submit/publish/close
    changed_fields JSONB,                   -- {"status": ["Draft", "Submitted"]}
    old_values JSONB,
    new_values JSONB,
    performed_by UUID REFERENCES users(id),
    performed_at TIMESTAMPTZ DEFAULT NOW(),
    ip_address INET,
    user_agent TEXT
);

CREATE INDEX idx_audit_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_time ON audit_logs(performed_at DESC);
```

### 5.11 版本库

```sql
CREATE TABLE version_packages (
    package_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    package_type VARCHAR(20) NOT NULL CHECK (package_type IN ('PLC', 'SOFTWARE', 'PARAM')),
    version_code VARCHAR(50) NOT NULL,
    
    file_key VARCHAR(500) NOT NULL,         -- 对象存储Key
    file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    checksum VARCHAR(64),                   -- MD5/SHA256
    
    release_notes TEXT,
    applicable_models VARCHAR(100)[],       -- 适用设备型号
    
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'released', 'deprecated')),
    released_at TIMESTAMPTZ,
    released_by UUID REFERENCES users(id),
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_version_unique ON version_packages(package_type, version_code);
```

---

## 六、API 契约

### 6.1 认证 API

```yaml
# 获取企业微信授权URL
GET /api/auth/wecom/login-url
Response:
  url: string          # 完整的OAuth授权URL
  state: string        # CSRF防护token

# 企业微信回调
POST /api/auth/wecom/callback
Body:
  code: string         # 授权码
  state: string        # 状态码
Response:
  token: string        # JWT
  refresh_token: string
  expires_in: number   # 秒
  user:
    id: string
    name: string
    role: string
    dept_name: string

# 获取当前用户
GET /api/me
Headers: Authorization: Bearer {token}
Response:
  id: string
  name: string
  mobile: string
  role: string
  dept_name: string
  permissions: string[]

# 刷新Token
POST /api/auth/refresh
Body:
  refresh_token: string
Response:
  token: string
  refresh_token: string
  expires_in: number
```

### 6.2 基础数据 API

```yaml
# 客户列表
GET /api/customers
Query: search, page, pageSize, isActive
Response: { items: Customer[], total: number }

# 项目列表
GET /api/projects
Query: customerId, search, page, pageSize
Response: { items: Project[], total: number }

# 设备列表
GET /api/devices
Query: projectId, search, page, pageSize
Response: { items: Device[], total: number }

# 通过二维码/SN查设备（扫码用）
GET /api/devices/by-code
Query: code (SN或二维码内容)
Response: Device (包含customer, project信息)
```

### 6.3 工单 API

```yaml
# 创建工单草稿
POST /api/tickets
Headers: 
  Authorization: Bearer {token}
  X-Idempotency-Key: {uuid}      # 幂等键
Body:
  localDraftId: string?          # App本地草稿ID
  deviceId: string
  domain: string                 # A/B/C/D/E
  stepCode: string
  symptomTitle: string
  swVersion: string
  plcVersion: string
  paramVersion: string
  factsJson: object
Response:
  ticketId: string
  ticketNo: string
  status: "Draft"

# 更新工单（仅Draft状态可改事实项）
PUT /api/tickets/{ticketId}
Body: (同创建，部分字段)
Response: Ticket

# 提交工单
POST /api/tickets/{ticketId}/submit
Validation:
  - domain 必填
  - stepCode 必填
  - swVersion/plcVersion/paramVersion 必填
  - factsJson 至少3项
  - 至少1个附件
Response:
  success: true
  ticketNo: string
  status: "Submitted"
Error 422:
  code: "VALIDATION_FAILED"
  errors: [{ field: string, message: string }]

# 工单列表
GET /api/tickets
Query:
  status: string[]
  customerId: string
  deviceSn: string
  domain: string
  priority: string
  createdBy: string              # "me" 或用户ID
  dateFrom: datetime
  dateTo: datetime
  page: number
  pageSize: number
  sortBy: string
  sortOrder: "asc"|"desc"
Response: { items: TicketListItem[], total: number }

# 工单详情（含时间线）
GET /api/tickets/{ticketId}
Response:
  ticket: Ticket
  attachments: Attachment[]
  triageNotes: TriageNote[]
  followups: Followup[]
  solutions: Solution[]
  verifications: Verification[]
  customerComms: CustomerComm[]
  timeline: TimelineEvent[]      # 完整操作时间线

# 追加证据（Submitted后仍可）
POST /api/tickets/{ticketId}/append-evidence
Body:
  attachmentIds: string[]        # 已上传的附件ID
Response: { success: true }
```

### 6.4 附件 API（断点续传）

```yaml
# 初始化上传（获取分片信息）
POST /api/attachments/init
Body:
  ticketId: string
  fileName: string
  fileSize: number
  fileType: "photo"|"video"|"log"|"file"
  mimeType: string
  sha256: string?
Response:
  attachmentId: string
  uploadId: string               # 分片上传ID
  chunkSize: number              # 每片大小（建议5MB）
  totalChunks: number
  uploadUrls: string[]           # 预签名URL列表（或单个上传endpoint）

# 上传分片
PUT /api/attachments/{attachmentId}/chunks/{chunkIndex}
Body: binary
Response: { success: true, uploaded: number }

# 完成上传
POST /api/attachments/{attachmentId}/complete
Body:
  parts: [{ partNumber: number, etag: string }]
Response:
  attachmentId: string
  fileKey: string
  status: "completed"

# 获取下载URL
GET /api/attachments/{attachmentId}/download-url
Response:
  url: string                    # 预签名下载URL
  expiresIn: number
```

### 6.5 分诊与追问 API

```yaml
# 获取推荐判断卡
GET /api/judgement-cards/recommend
Query:
  domain: string
  stepCode: string
  keywords: string               # 关键词，逗号分隔
Response:
  items: JudgementCard[]
  matchScores: { jcCode: string, score: number }[]

# 提交分诊
POST /api/tickets/{ticketId}/triage
Body:
  jcCode: string                 # 关联的判断卡
  note: string
  conclusion: string
  nextAction: string
Response: { success: true, newStatus: "Triage" }

# 创建追问
POST /api/tickets/{ticketId}/followups
Body:
  questionText: string
  isRequired: boolean
Response: Followup

# 回答追问
POST /api/tickets/{ticketId}/followups/{followupId}/answer
Body:
  answerText: string
  attachmentIds: string[]?
Response: { success: true }
```

### 6.6 解决方案 API

```yaml
# 创建解决方案
POST /api/tickets/{ticketId}/solutions
Body:
  summary: string
  changeDetail: string
  releaseType: string
  releaseVersion: string?
  packageKey: string?
  verificationChecklistJson: ChecklistItem[]
  acceptanceCriteria: string?
  rollbackPlan: string?
Response: Solution

# 编辑解决方案
PUT /api/solutions/{solutionId}
Body: (同创建)
Response: Solution

# 发布解决方案
POST /api/solutions/{solutionId}/publish
Response:
  success: true
  solutionCode: string           # SOL-2025-001
  publishedAt: datetime

# 撤回解决方案
POST /api/solutions/{solutionId}/withdraw
Body:
  reason: string
Response: { success: true }
```

### 6.7 验证 API

```yaml
# 提交验证结果
POST /api/tickets/{ticketId}/verifications
Body:
  solutionId: string
  runCount: number
  passCount: number
  result: "PASS"|"FAIL"|"PARTIAL"
  checklistResultJson: object
  notes: string?
  evidenceAttachmentIds: string[]
Response: Verification

# 获取验证历史
GET /api/tickets/{ticketId}/verifications
Response: Verification[]
```

### 6.8 客户沟通 API

```yaml
# 新增沟通记录
POST /api/tickets/{ticketId}/customer-comms
Body:
  commType: "call"|"wechat"|"email"|"onsite"
  templateUsed: string?
  content: string
  customerFeedback: string?
Response: CustomerComm

# 获取沟通记录
GET /api/tickets/{ticketId}/customer-comms
Response: CustomerComm[]
```

### 6.9 统计 API

```yaml
# 概览统计
GET /api/stats/overview
Query: dateFrom, dateTo
Response:
  totalTickets: number
  openTickets: number
  avgClosureHours: number
  firstTimeResolutionRate: number   # 无需追问即出SOL的比例

# Top问题域
GET /api/stats/top-domains
Query: dateFrom, dateTo, limit
Response:
  items: { domain: string, count: number, percentage: number }[]

# 闭环时间分布
GET /api/stats/closure-time
Query: dateFrom, dateTo
Response:
  avgHours: number
  medianHours: number
  p90Hours: number
  distribution: { range: string, count: number }[]
```

---

## 七、企业微信通知

### 7.1 通知场景与模板

```yaml
工单提交成功:
  接收人: 高级工程师组（按domain分发或轮值）
  渠道: 应用消息
  模板: |
    【新工单】{ticket_no}
    客户：{customer_name}
    设备：{device_sn}
    问题：{symptom_title}
    紧急度：{priority}
    提交人：{creator_name}
    <a href="{web_url}">点击处理</a>

追问已发出:
  接收人: 工单创建者
  渠道: 应用消息
  模板: |
    【追问】工单 {ticket_no} 有新问题
    问题：{question_text}
    请在App中尽快补充回复

追问已回复:
  接收人: 分诊的高级工程师
  渠道: 应用消息
  模板: |
    【追问回复】工单 {ticket_no}
    现场已回复追问，请继续处理

SOL已发布:
  接收人: 工单创建者 + 关联客服
  渠道: 应用消息
  模板: |
    【解决方案】{solution_code} 已发布
    工单：{ticket_no}
    方案：{summary}
    请按验证清单执行并反馈结果
    <a href="{app_deeplink}">打开App查看</a>

验证结果已提交:
  接收人: SOL发布者
  渠道: 应用消息
  模板: |
    【验证反馈】工单 {ticket_no}
    验证结果：{result}
    执行人：{executor_name}
    通过率：{pass_count}/{total_count}
    <a href="{web_url}">查看详情</a>

工单已关闭:
  接收人: 工单相关人员
  渠道: 应用消息
  模板: |
    【工单关闭】{ticket_no}
    客户：{customer_name}
    问题：{symptom_title}
    闭环耗时：{closure_hours}小时
```

### 7.2 技术实现

```csharp
// 通知服务接口
public interface IWeComNotificationService
{
    Task SendAsync(NotificationRequest request);
}

// 请求模型
public record NotificationRequest
{
    public string[] ToUserIds { get; init; }    // 企业微信userid
    public string TemplateKey { get; init; }     // 模板标识
    public Dictionary<string, string> Data { get; init; }
}

// 实现要点：
// 1. 获取 access_token（带缓存，7200秒有效）
// 2. 调用 POST https://qyapi.weixin.qq.com/cgi-bin/message/send
// 3. 失败重试（最多3次）
// 4. 记录发送日志
```

---

## 八、离线与弱网支持

### 8.1 App 本地存储结构（SQLite）

```sql
-- 本地草稿
CREATE TABLE local_drafts (
    local_id TEXT PRIMARY KEY,              -- 本地UUID
    server_id TEXT,                         -- 提交成功后的服务端ID
    
    -- 业务数据（JSON存储完整表单）
    form_data TEXT NOT NULL,                -- JSON
    
    -- 同步状态
    sync_status TEXT DEFAULT 'local',       -- local/pending/syncing/synced/error
    error_message TEXT,
    retry_count INTEGER DEFAULT 0,
    
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- 待上传附件
CREATE TABLE pending_attachments (
    local_id TEXT PRIMARY KEY,
    draft_local_id TEXT,                    -- 关联草稿
    server_id TEXT,                         -- 上传成功后的服务端ID
    
    file_path TEXT NOT NULL,                -- 本地文件路径
    file_name TEXT NOT NULL,
    file_type TEXT NOT NULL,
    file_size INTEGER NOT NULL,
    sha256 TEXT,
    
    upload_status TEXT DEFAULT 'pending',   -- pending/uploading/completed/failed
    upload_id TEXT,                         -- 分片上传ID
    uploaded_chunks INTEGER DEFAULT 0,
    total_chunks INTEGER,
    retry_count INTEGER DEFAULT 0,
    last_error TEXT,
    
    created_at TEXT NOT NULL
);

-- 待同步操作队列
CREATE TABLE sync_queue (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    operation_type TEXT NOT NULL,           -- submit/append_evidence/answer_followup/submit_verification
    payload TEXT NOT NULL,                  -- JSON
    idempotency_key TEXT NOT NULL UNIQUE,
    status TEXT DEFAULT 'pending',          -- pending/processing/completed/failed
    retry_count INTEGER DEFAULT 0,
    last_error TEXT,
    created_at TEXT NOT NULL
);
```

### 8.2 同步策略

```dart
// 网络状态监听
class SyncManager {
  // 网络恢复时自动触发
  void onNetworkRestored() async {
    // 1. 先同步待上传附件
    await _syncPendingAttachments();
    
    // 2. 再同步待提交操作
    await _syncPendingOperations();
  }
  
  // 附件上传（断点续传）
  Future<void> _syncPendingAttachments() async {
    final pending = await db.query('pending_attachments',
      where: 'upload_status IN (?, ?)',
      whereArgs: ['pending', 'failed'],
      orderBy: 'created_at ASC'
    );
    
    for (final attachment in pending) {
      try {
        await _resumeUpload(attachment);
      } catch (e) {
        await _markUploadFailed(attachment, e.toString());
      }
    }
  }
  
  // 操作队列（幂等提交）
  Future<void> _syncPendingOperations() async {
    final queue = await db.query('sync_queue',
      where: 'status = ?',
      whereArgs: ['pending'],
      orderBy: 'id ASC'
    );
    
    for (final op in queue) {
      try {
        await _executeOperation(op);
        await _markCompleted(op);
      } catch (e) {
        if (_isRetryable(e)) {
          await _incrementRetry(op, e.toString());
        } else {
          await _markFailed(op, e.toString());
        }
      }
    }
  }
}
```

### 8.3 幂等性实现（服务端）

```csharp
// 中间件
public class IdempotencyMiddleware
{
    private readonly IDistributedCache _cache;
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var key))
        {
            await next(context);
            return;
        }
        
        var cacheKey = $"idempotency:{key}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            // 返回缓存的响应
            var response = JsonSerializer.Deserialize<IdempotencyResponse>(cached);
            context.Response.StatusCode = response.StatusCode;
            await context.Response.WriteAsync(response.Body);
            return;
        }
        
        // 执行请求
        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;
        
        await next(context);
        
        // 缓存响应（24小时）
        memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
        
        var toCache = new IdempotencyResponse
        {
            StatusCode = context.Response.StatusCode,
            Body = responseBody
        };
        
        await _cache.SetStringAsync(cacheKey, 
            JsonSerializer.Serialize(toCache),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }
        );
        
        memoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalBody);
    }
}
```

---

## 九、必填校验规则

### 9.1 工单提交校验

```yaml
POST /api/tickets/{id}/submit 校验清单:

必填字段:
  domain:
    - 不能为空
    - 必须是 A/B/C/D/E 之一（对应：机械/电气/PLC/测试/环境）
  
  stepCode:
    - 强烈建议必填（MVP可选，但至少要选"模块"）
    - 格式建议：Step_NNN_Name（如 Step_120_Clamp_Check）
  
  symptomTitle:
    - 不能为空
    - 长度 10-200 字符
    - 一句话描述现象，不含判断
  
  swVersion:
    - 不能为空
    - 格式 vX.Y.Z 或自由格式
  
  plcVersion:
    - 不能为空
  
  paramVersion:
    - 不能为空
  
  factsJson:
    - 不能为空
    - 必须是有效JSON对象
    - environment.repro_rate 必填（0-100）
    - 与所选问题域对应的维度至少要填（非全NA）:
      - domain=A → mechanical 至少1项非NA
      - domain=B → electrical 至少1项非NA
      - domain=C → plc.stuck_step_code必填 + 至少1项非NA
      - domain=D → test.fail_item必填（如有FAIL）
      - domain=E → environment 必填
  
  confirmedAsFact:
    - 必须为 true
    - 确认"我确认以上为现场事实，不包含个人判断"

附件要求:
  - 至少1个附件（photo/video/log任一）
  - 条件规则（MVP可选，二期实现）:
    - 如果 domain = A 且 symptom 包含"卡料" → 必须有 video
    - 如果 domain = C → 建议有 log

校验失败响应:
  HTTP 422 Unprocessable Entity
  {
    "code": "VALIDATION_FAILED",
    "message": "提交校验失败",
    "errors": [
      { "field": "domain", "code": "REQUIRED", "message": "问题域不能为空" },
      { "field": "factsJson.environment.repro_rate", "code": "REQUIRED", "message": "复现率必填" },
      { "field": "factsJson.plc", "code": "INCOMPLETE", "message": "选择PLC域时，PLC事实表至少填写1项" },
      { "field": "attachments", "code": "MIN_COUNT", "message": "至少需要上传1个证据" },
      { "field": "confirmedAsFact", "code": "REQUIRED", "message": "请确认以上为现场事实" }
    ]
  }
```

### 9.2 解决方案发布校验

```yaml
POST /api/solutions/{id}/publish 校验清单:

必填字段:
  summary:
    - 不能为空
    - 长度 20-2000 字符
  
  releaseType:
    - 必须是有效类型
  
  verificationChecklistJson:
    - 不能为空数组
    - 至少1项验证检查项
    - 每项必须有 item 和 acceptanceCriteria

关联校验:
  - 工单必须处于 Triage 状态
  - 创建者必须有 SeniorEngineer 或 Admin 角色
```

---

## 十、业务流程联动（上报表 → 判断卡 → 解决方案）

### 10.1 核心设计原则

```
上报表 = 「事实」（初级工程师只采集，不判断）
判断卡 = 「结论」（高级工程师根据事实判断）

经验被"前移到系统里"——初级工程师不需要知道为什么，只需要：勾选 + 截图 + 填版本
```

### 10.2 联动流程图

```
现场异常
    ↓
【现场问题结构化上报表】（事实输入）
    │
    │ 初级工程师填写：
    │ - 问题域（A/B/C/D/E 单选）
    │ - YES/NO 事实表（5个维度）
    │ - 版本信息
    │ - 证据附件
    │ - 已执行动作
    │ - 事实确认签名
    ↓
系统自动匹配 + 高级工程师确认
    ↓
【工程判断卡】（工程判断与归因）
    │
    │ 高级工程师：
    │ - 查看推荐的判断卡
    │ - 根据"判定边界"得出结论
    │ - 参考"典型失效模式"
    ↓
【解决方案】（版本化输出）
    │
    │ 输出内容：
    │ - SOL编号
    │ - 修改内容（结构化）
    │ - 发布版本
    │ - 验证要求
    │ - 回滚策略
    ↓
版本化下发
    ↓
【现场验证】
    │
    │ 现场工程师：
    │ - 按验证清单执行
    │ - 上传验证证据
    │ - 提交结果
    ↓
【客户沟通】（统一话术）
    │
    │ 使用预设模板：
    │ - 问题已确认模板
    │ - 方案已发布模板
    │ - 验证通过模板
    ↓
工单关闭
```

### 10.3 完整示例：夹具到位超时问题

**场景**：夹具到位后，程序超时，设备不继续动作

---

**Step 1：现场上报表（初级工程师填写）**

```json
{
  "基本信息": {
    "客户": "某汽车零部件公司",
    "设备SN": "JKB-2024-0156",
    "软件版本": "v2.1.3",
    "PLC版本": "v1.2.6",
    "参数版本": "v3.4"
  },
  "问题域": "C",  // PLC/程序流程
  "事实表": {
    "A_mechanical": {
      "action_completed": true,
      "position_reliable": true,
      "stuck_or_noise": false,
      "manual_assist_recovers": null
    },
    "B_electrical": {
      "sensor_physical_ok": true,
      "plc_io_changed": false,  // 关键：IO没有变化
      "same_type_points_ok": true
    },
    "C_plc_program": {
      "stuck_step_code": "Step_120",
      "stuck_step_name": "夹具到位检测",
      "always_stuck_here": true,  // 关键：固定卡在这里
      "manual_step_passable": true
    },
    "E_reproducibility": {
      "repro_100_percent": true,
      "reboot_recovers": false
    }
  },
  "已执行动作": ["REBOOT", "RELOAD_PROGRAM"],
  "事实确认": true,
  "附件": ["photo_io_screen.jpg", "video_clamp_action.mp4"]
}
```

👆 **到这里，初级工程师的任务结束**

---

**Step 2：系统匹配判断卡**

系统根据 `domain=C` + `step=Step_120` + 关键词匹配，推荐：

```
工程判断卡 JC-017
《夹具到位信号与程序判定一致性》

匹配得分: 92%
匹配依据: 
- 问题域匹配 ✓
- 步骤匹配 ✓  
- 关键词"到位"匹配 ✓
- IO异常+固定步骤 → 高置信度
```

---

**Step 3：高级工程师远程判断（无需去现场）**

根据上报表 + 判断卡的"判定边界"：

| 条件 | 上报表数据 | 判定 |
|------|-----------|------|
| 机械动作 | ✅ 完成 | OK |
| 物理传感器 | ✅ 正常 | OK |
| PLC IO变化 | ❌ 无变化 | **异常** |
| 卡在固定步骤 | ✅ 是 | **确认** |

➡ **判断结论**：程序逻辑/IO判定窗口问题

参考判断卡的"典型失效模式"：
- IO去抖不足 → 解决方案：增加滤波时间
- 超时窗口过短 → 解决方案：延长超时时间

---

**Step 4：生成解决方案**

```json
{
  "solution_code": "SOL-2025-032",
  "jc_code": "JC-017",
  "diagnosis_conclusion": "程序逻辑/IO判定窗口问题",
  "change_detail_json": {
    "changes": [
      {
        "type": "PLC_LOGIC",
        "location": "Step_120 到位信号确认逻辑",
        "before": "无滤波",
        "after": "增加50ms滤波",
        "reason": "消除信号抖动"
      },
      {
        "type": "PARAMETER",
        "location": "Step_120 超时时间",
        "before": "300ms",
        "after": "800ms",
        "reason": "适应正常动作时间波动"
      }
    ]
  },
  "release_type": "PLC",
  "release_version": "v1.2.7",
  "verification_checklist_json": [
    {"item": "下载新版本 PLC v1.2.7", "type": "action", "required": true},
    {"item": "连续运行 20 次", "type": "test", "required": true, "acceptance_criteria": "20/20 PASS"},
    {"item": "记录 Step_120 实际用时", "type": "data_collection", "required": true}
  ],
  "rollback_plan": "恢复 PLC v1.2.6",
  "rollback_version": "v1.2.6"
}
```

---

**Step 5：现场验证**

```json
{
  "solution_id": "SOL-2025-032",
  "executed_by": "张工",
  "run_count": 20,
  "pass_count": 20,
  "result": "PASS",
  "checklist_result_json": {
    "下载新版本": {"completed": true, "note": "v1.2.7 下载成功"},
    "连续运行20次": {"completed": true, "pass": 20, "fail": 0},
    "Step_120用时": {"completed": true, "data": "平均 450ms，最大 620ms"}
  },
  "evidence_attachments": ["verification_log.txt", "run_result_screenshot.png"]
}
```

---

**Step 6：客户沟通（使用模板）**

```
模板: TPL_SOLUTION_ISSUED

渲染后内容:
"我们已确认问题发生在【夹具到位检测】步骤，公司已针对该步骤优化控制逻辑（SOL-2025-032），
当前版本 v1.2.7 运行稳定。如有问题请随时联系。"
```

### 10.4 判断卡推荐算法（规则引擎，不需要AI）

**设计原则**：先做可控的规则引擎，效果就会很好。推荐仅做候选，不自动绑定，避免误联动。

```python
def recommend_judgement_cards(ticket):
    """
    根据工单信息推荐判断卡
    返回: [(jc_code, score, match_reasons)]
    
    评分规则（满分100）：
    - 问题域匹配：+50分（必须匹配才进入候选）
    - 步骤匹配：+30分（完全匹配）或 +15分（部分匹配）
    - 关键词匹配：+10分/词（最高20分）
    - 事实特征匹配：+5分/项（最高10分）
    """
    candidates = []
    
    for jc in all_judgement_cards:
        # 问题域不匹配直接跳过
        if ticket.domain not in jc.applicable_domains:
            continue
            
        score = 50  # 问题域匹配基础分
        reasons = [f"问题域 {ticket.domain} 匹配"]
        
        # 2. 步骤匹配（权重：30%）
        if ticket.step_code in jc.applicable_steps:
            score += 30
            reasons.append(f"步骤 {ticket.step_code} 完全匹配")
        elif any(step in ticket.step_code for step in jc.applicable_steps):
            score += 15
            reasons.append("步骤部分匹配")
        
        # 3. 关键词匹配（权重：20%，上限）
        symptom_text = f"{ticket.symptom_title} {ticket.facts_json.get('plc', {}).get('alarm_code', '')}"
        symptom_keywords = extract_keywords(symptom_text)
        matched_keywords = set(symptom_keywords) & set(jc.keywords)
        keyword_score = min(len(matched_keywords) * 10, 20)
        if keyword_score > 0:
            score += keyword_score
            reasons.append(f"关键词匹配: {list(matched_keywords)[:3]}")
        
        # 4. 事实表特征匹配（权重：10%）
        fact_matches = match_facts_to_key_checks(ticket.facts_json, jc.key_checks_json)
        if fact_matches > 0:
            score += min(fact_matches * 5, 10)
            reasons.append(f"事实特征匹配 {fact_matches} 项")
        
        if score >= 50:  # 至少问题域匹配
            candidates.append({
                "jc_code": jc.jc_code,
                "version": jc.version,
                "title": jc.title,
                "score": score,
                "reasons": reasons
            })
    
    # 按分数降序，返回Top 5
    return sorted(candidates, key=lambda x: -x["score"])[:5]


def match_facts_to_key_checks(facts_json, key_checks_json):
    """
    检查事实表与判断卡的关键检查项匹配度
    返回匹配的项数
    """
    matches = 0
    for check in key_checks_json:
        domain = check.get("domain")  # mechanical/electrical/plc/test
        field = check.get("field")
        expected = check.get("expected")  # YES/NO
        
        actual = facts_json.get(domain, {}).get(field)
        if actual == expected:
            matches += 1
    return matches


def extract_keywords(text):
    """
    从文本中提取关键词（中文分词 + 停用词过滤）
    实际实现可用 jieba 分词
    """
    # 简化示例：按常见分隔符拆分
    import re
    words = re.split(r'[，。、\s]+', text)
    # 过滤停用词和短词
    stopwords = {'的', '了', '是', '在', '有', '和', '与'}
    return [w for w in words if len(w) >= 2 and w not in stopwords]
```

**API 响应示例**：

```json
GET /judgement-cards/recommend?domain=C&step=Step_120_Clamp_Check&q=超时

{
  "candidates": [
    {
      "jc_code": "JC-017",
      "version": "1.0",
      "title": "夹具到位信号与程序判定一致性",
      "score": 92,
      "reasons": [
        "问题域 C 匹配",
        "步骤 Step_120 完全匹配",
        "关键词匹配: ['到位', '超时']",
        "事实特征匹配 2 项"
      ]
    },
    {
      "jc_code": "JC-023",
      "version": "2.1",
      "title": "步骤超时通用判断",
      "score": 78,
      "reasons": [
        "问题域 C 匹配",
        "关键词匹配: ['超时']"
      ]
    }
  ]
}
```

---

## 十一、部署配置

### 11.1 Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Default=Host=postgres;Database=fieldticket;Username=app;Password=${DB_PASSWORD}
      - Redis__Connection=redis:6379
      - MinIO__Endpoint=minio:9000
      - MinIO__AccessKey=${MINIO_ACCESS_KEY}
      - MinIO__SecretKey=${MINIO_SECRET_KEY}
      - MinIO__Bucket=attachments
      - WeCom__CorpId=${WECOM_CORP_ID}
      - WeCom__AgentId=${WECOM_AGENT_ID}
      - WeCom__Secret=${WECOM_SECRET}
      - WeCom__RedirectUri=${WECOM_REDIRECT_URI}
      - Jwt__Secret=${JWT_SECRET}
      - Jwt__Issuer=field-ticket-api
      - Jwt__ExpiresInHours=168
    depends_on:
      - postgres
      - redis
      - minio
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    build:
      context: ./web-admin
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - REACT_APP_API_URL=http://api:5000
    depends_on:
      - api

  postgres:
    image: postgres:16-alpine
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    environment:
      - POSTGRES_DB=fieldticket
      - POSTGRES_USER=app
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    ports:
      - "5432:5432"

  redis:
    image: redis:7-alpine
    volumes:
      - redisdata:/data
    command: redis-server --appendonly yes
    ports:
      - "6379:6379"

  minio:
    image: minio/minio
    command: server /data --console-address ":9001"
    ports:
      - "9000:9000"
      - "9001:9001"
    volumes:
      - miniodata:/data
    environment:
      - MINIO_ROOT_USER=${MINIO_ACCESS_KEY}
      - MINIO_ROOT_PASSWORD=${MINIO_SECRET_KEY}

volumes:
  pgdata:
  redisdata:
  miniodata:
```

### 11.2 环境变量模板

```bash
# .env.example
DB_PASSWORD=your_secure_password_here
JWT_SECRET=your_jwt_secret_at_least_32_chars

MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=your_minio_secret

WECOM_CORP_ID=ww1234567890
WECOM_AGENT_ID=1000001
WECOM_SECRET=your_wecom_secret
WECOM_REDIRECT_URI=https://your-domain.com/api/auth/wecom/callback
```

---

## 十二、验收标准

### 12.1 功能验收用例

```yaml
TC-001 企业微信登录:
  前置: 配置有效的企业微信测试账号
  步骤:
    1. App点击"企业微信登录"
    2. 跳转企业微信授权页
    3. 确认授权
    4. 返回App
  预期:
    - 登录成功，显示用户姓名
    - 获得有效JWT
    - 可访问工单列表

TC-002 扫码创建工单:
  角色: FieldEngineer
  步骤:
    1. 扫描设备二维码
    2. 确认预填的客户/项目/设备信息
    3. 选择域=A，步骤=Step_120
    4. 输入症状="设备启动异常，报警代码E001"
    5. 确认版本（预填或修改）
    6. 填写事实表（至少3项YES/NO）
    7. 拍照上传1张
    8. 点击提交
  预期:
    - 提交成功，获得工单编号
    - 状态变为 Submitted
    - 高级工程师收到企业微信通知
    - 全程耗时 < 2分钟

TC-003 必填校验拦截:
  角色: FieldEngineer
  步骤:
    1. 创建工单草稿
    2. 不选问题域
    3. 不上传证据
    4. 点击提交
  预期:
    - 提交失败，HTTP 422
    - 显示具体错误项

TC-004 离线草稿与补传:
  角色: FieldEngineer
  步骤:
    1. 开启飞行模式
    2. 创建工单草稿，填写完整，拍照
    3. 点击提交
    4. 提示"已保存，待网络恢复后自动提交"
    5. 关闭飞行模式
  预期:
    - 网络恢复后自动上传附件
    - 自动提交工单
    - 通知发送成功
    - 本地草稿状态更新为"已同步"

TC-005 分诊并发布SOL:
  角色: SeniorEngineer
  前置: 存在一个 Submitted 状态工单
  步骤:
    1. Web端打开工单详情
    2. 点击"分诊"
    3. 查看推荐判断卡，选择一个
    4. 填写分诊结论
    5. 创建解决方案，填写验证清单（至少1项）
    6. 点击发布
  预期:
    - 生成SOL编号（SOL-YYYY-NNN格式）
    - 工单状态变为 SolutionIssued
    - 现场工程师收到企业微信通知

TC-006 验证闭环:
  角色: FieldEngineer
  前置: 存在 SolutionIssued 状态工单
  步骤:
    1. App查看SOL详情
    2. 执行验证，勾选通过项
    3. 上传验证截图
    4. 提交验证结果 PASS
  预期:
    - 验证记录保存成功
    - 工单状态变为 Verifying
    - 高级工程师收到通知
    - 高级工程师可关闭工单

TC-007 权限拦截:
  角色: FieldEngineer
  步骤: 尝试调用 POST /api/solutions/{id}/publish
  预期: HTTP 403 Forbidden

TC-008 附件断点续传:
  角色: FieldEngineer
  步骤:
    1. 选择一个50MB视频
    2. 开始上传
    3. 上传到30%时断网
    4. 恢复网络
    5. 自动继续上传
  预期:
    - 从30%处继续，不重新上传
    - 最终上传成功

TC-009 判断卡自动推荐:
  角色: SeniorEngineer
  前置: 
    - 系统已导入判断卡 JC-017（适用域C，步骤Step_120）
    - 存在一个工单：domain=C, step=Step_120, symptom="夹具到位超时"
  步骤:
    1. Web端打开工单详情
    2. 点击"分诊"
    3. 查看系统推荐的判断卡列表
  预期:
    - JC-017 出现在推荐列表前3位
    - 显示匹配得分和匹配原因
    - 点击可查看判断卡详情

TC-010 完整闭环流程:
  角色: 多角色协作
  步骤:
    1. [FieldEngineer] 创建并提交工单（含完整事实表）
    2. [SeniorEngineer] 收到通知，打开工单
    3. [SeniorEngineer] 选择推荐的判断卡，填写结论
    4. [SeniorEngineer] 创建并发布解决方案
    5. [FieldEngineer] 收到通知，查看SOL
    6. [FieldEngineer] 执行验证，提交PASS结果
    7. [SeniorEngineer] 关闭工单
  预期:
    - 全流程可在Web/App时间线完整追溯
    - 每个状态变更都有审计日志
    - 企业微信通知正常发送

TC-011 客户沟通话术模板:
  角色: CS
  前置: 存在已发布SOL的工单
  步骤:
    1. App打开工单详情
    2. 点击"客户沟通"
    3. 选择"方案已发布"模板
    4. 查看预览（变量已替换）
    5. 确认发送/保存记录
  预期:
    - 模板变量正确替换（设备SN、方案编号、版本号等）
    - 沟通记录保存成功
    - 可查看历史沟通记录
```

### 12.2 指标验收（上线2周测量）

| 指标 | 目标值 | 测量方式 |
|------|--------|----------|
| 工单提交完整率 | ≥ 95% | 提交成功数 / 创建草稿数 |
| 一次定位成功率 | ≥ 60% | 无追问直接出SOL / 总工单数 |
| 平均闭环时间 | 下降 ≥ 30% | (提交→关闭) 对比上线前 |
| 高级工程师现场介入 | 下降 ≥ 40% | 季度口径对比 |

---

## 十三、开发任务书（可直接交付）

```
项目：现场问题结构化上报系统
版本：MVP

【交付物】
1. Flutter App（Android APK + iOS Archive）
2. React Web管理端（Docker镜像）
3. .NET 8 API服务（Docker镜像）
4. PostgreSQL数据库（EF Core Migrations）
5. Docker Compose一键启动脚本
6. Swagger/OpenAPI文档
7. E2E测试报告（11个核心用例通过）

【技术要求】
- 后端：.NET 8 + ASP.NET Core Minimal API + EF Core 8
- 前端：React 18 + Ant Design 5 + TypeScript
- 移动端：Flutter 3.x + Provider状态管理
- 数据库：PostgreSQL 16
- 缓存：Redis 7
- 对象存储：MinIO

【登录方式】
- 企业微信OAuth2（内部应用）
- API签发JWT（有效期7天）

【核心功能】
1. 工单全生命周期：Draft→Submitted→Triage→SolutionIssued→Verifying→Closed
2. 结构化上报：问题域(A-E) + 步骤 + 版本 + YES/NO事实表（5维度） + 证据附件
3. 判断卡推荐算法：按问题域+步骤+关键词+事实特征匹配，返回Top 5
4. 解决方案版本化发布：结构化修改内容 + 验证清单 + 回滚策略
5. 客户沟通话术模板：预置3个场景模板，支持变量替换
6. 附件分片上传（断点续传）
7. 离线草稿与弱网补传
8. 企业微信消息通知（5个场景）
9. 权限控制（4角色）
10. 审计日志

【事实表结构（5个维度）】
- A_mechanical: 机械/动作确认（4项YES/NO）
- B_electrical: 电气/IO确认（3项YES/NO）
- C_plc_program: PLC/程序确认（步骤号+2项YES/NO+报警代码）
- D_test_judgement: 测试/判定确认（3项YES/NO+测试值/上下限）
- E_reproducibility: 复现性确认（复现率+重启恢复+环境相关）

【判断卡结构】
- 判断目标
- 关键判断依据
- 现场需确认项（对应事实表字段）
- 判定边界（条件→结论映射）
- 典型失效模式（模式+症状+解决方案提示）
- 标准处置建议

【解决方案结构】
- 修改内容（结构化：类型+位置+修改前+修改后+原因）
- 验证清单（类型：action/test/data_collection + 验收标准）
- 回滚策略（版本号）

【质量要求】
- 所有API必须有Swagger文档
- 数据库使用EF Core Code First + Migrations
- 实现幂等性（X-Idempotency-Key）
- 完整的审计日志（谁在什么时候改了什么）
- 11个核心E2E测试用例通过

【必填校验（提交时拦截）】
- 问题域：必选A-E之一
- 步骤：必填，格式Step_NNN
- 版本：软件/PLC/参数三项必填
- 事实表：至少填写与所选问题域对应的维度
- 证据：至少1个附件
- 事实确认：必须勾选"我确认以上为现场事实"

【权限硬约束】
- FieldEngineer：不能编辑Solution，不能改已提交事实项
- CS：不能发布Solution，不能提交验证
- SeniorEngineer：可分诊、发布Solution、关闭工单
- Admin：全部权限

【数据可见范围】
- FieldEngineer：仅能看自己创建的工单
- CS：按客户组可见（同一客户的所有工单）
- SeniorEngineer：全局可见
- Admin：全局可见

【附件限制】
- 视频：≤ 200MB，格式 mp4/mov/avi
- 日志：≤ 20MB，格式 txt/log/csv/zip
- 图片：≤ 10MB，格式 jpg/png/gif（上传时自动压缩）
- 其他文件：≤ 50MB
```

---

## 十四、UI 结构（低保真，给 AI 画页面用）

### 14.1 App 工单创建（向导式 4 步）

```
┌──────────────────────────────────┐
│  [进度条: ●○○○]  Step 1/4        │
│                                  │
│  ┌────────────────────────────┐  │
│  │  [扫码] 或 [手动选择设备]   │  │
│  └────────────────────────────┘  │
│                                  │
│  扫码结果：                       │
│  客户：某汽车零部件公司           │
│  项目：XX产线                    │
│  设备SN：JKB-2024-0156          │
│  工位：1号线工位3               │
│                                  │
│  版本信息（请确认或修改）：       │
│  软件版本：[v2.1.3    ▼]        │
│  PLC版本： [v1.2.6    ▼]        │
│  参数版本：[v3.4      ▼]        │
│                                  │
│           [下一步 →]             │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  [进度条: ●●○○]  Step 2/4        │
│                                  │
│  问题域（必选一个）：             │
│  ○ A 机械/动作                   │
│  ○ B 电气/IO                    │
│  ● C PLC/程序流程                │
│  ○ D 测试/判定                   │
│  ○ E 系统/偶发/环境              │
│                                  │
│  步骤号：[Step_120_Clamp_Check]  │
│  现象一句话：                     │
│  [夹具到位后程序超时，设备不动作] │
│                                  │
│  复现率：[100]%                   │
│  ☑ 100%复现  ☐ 重启后恢复        │
│                                  │
│    [← 上一步]    [下一步 →]      │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  [进度条: ●●●○]  Step 3/4        │
│                                  │
│  事实确认（只勾选，不解释）       │
│                                  │
│  ┌─ 机械/动作 ─────────────────┐ │
│  │ 动作完成？    [YES] [NO] [NA]│ │
│  │ 到位可靠？    [YES] [NO] [NA]│ │
│  │ 卡滞/异音？   [YES] [NO] [NA]│ │
│  │ 人工辅助恢复？[YES] [NO] [NA]│ │
│  └─────────────────────────────┘ │
│                                  │
│  ┌─ 电气/IO ───────────────────┐ │
│  │ 传感器物理OK？[YES] [NO] [NA]│ │
│  │ PLC IO有变化？[YES] [NO] [NA]│ │
│  │ 同类点位OK？  [YES] [NO] [NA]│ │
│  └─────────────────────────────┘ │
│                                  │
│  ┌─ PLC/程序（当前选中域）─────┐ │
│  │ 固定卡在此步？[YES] [NO] [NA]│ │
│  │ 手动可通过？  [YES] [NO] [NA]│ │
│  │ 报警代码：[E001          ]  │ │
│  └─────────────────────────────┘ │
│                                  │
│    [← 上一步]    [下一步 →]      │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│  [进度条: ●●●●]  Step 4/4        │
│                                  │
│  上传证据（至少1个）             │
│                                  │
│  ┌────────┐ ┌────────┐          │
│  │ [拍照] │ │ [视频] │          │
│  └────────┘ └────────┘          │
│  ┌────────┐ ┌────────┐          │
│  │ [日志] │ │ [文件] │          │
│  └────────┘ └────────┘          │
│                                  │
│  已上传：                        │
│  ☑ io_screen.jpg (1.2MB) [删除] │
│  ☑ clamp_video.mp4 (15MB) [删除]│
│                                  │
│  ☑ 我确认以上为现场事实，        │
│    不包含个人判断                │
│                                  │
│    [← 上一步]    [提交 ✓]        │
└──────────────────────────────────┘
```

### 14.2 Web 工单详情（高级工程师视角）

```
┌─────────────────────────────────────────────────────────────────────────┐
│  工单 TK-20251218-001                              [分诊] [创建SOL]     │
├────────────────────┬────────────────────────┬───────────────────────────┤
│                    │                        │                           │
│  【工单摘要】       │  【事实表】             │  【推荐判断卡】            │
│                    │                        │                           │
│  客户：某汽车公司   │  机械/动作              │  ┌─────────────────────┐ │
│  设备：JKB-0156    │  ✓动作完成  ✓到位可靠  │  │ JC-017 (92分)       │ │
│  问题域：C PLC     │  ✗卡滞异音  -人工恢复  │  │ 夹具到位信号判定    │ │
│  步骤：Step_120    │                        │  │ [查看] [关联]       │ │
│  现象：夹具超时    │  电气/IO               │  └─────────────────────┘ │
│                    │  ✓传感器OK  ✗IO变化   │  ┌─────────────────────┐ │
│  软件：v2.1.3      │  ✓同类OK              │  │ JC-023 (78分)       │ │
│  PLC：v1.2.6       │                        │  │ 步骤超时通用判断    │ │
│  参数：v3.4        │  PLC/程序              │  │ [查看] [关联]       │ │
│                    │  卡在：Step_120        │  └─────────────────────┘ │
│  复现率：100%      │  ✓固定卡此  ✓手动可过 │                           │
│  重启恢复：否      │  报警：E001            │  已关联：JC-017           │
│                    │                        │                           │
│  附件：            │  测试/判定              │  【分诊结论】             │
│  📷 io_screen.jpg  │  （未填写）             │  ┌─────────────────────┐ │
│  🎬 clamp.mp4      │                        │  │ 程序逻辑/IO判定窗口 │ │
│                    │  环境                   │  │ 问题                │ │
│                    │  复现：100%  重启：否   │  └─────────────────────┘ │
│                    │                        │                           │
├────────────────────┴────────────────────────┴───────────────────────────┤
│  【时间线】                                                              │
│  ────────────────────────────────────────────────────────────────────── │
│  12-18 09:30  张工 提交工单                                             │
│  12-18 10:15  李工 开始分诊，关联 JC-017                                │
│  12-18 10:30  李工 创建追问：请确认信号滤波参数                          │
│  12-18 11:00  张工 回复追问：当前无滤波                                  │
│  12-18 11:30  李工 发布 SOL-2025-032                                    │
│  12-18 14:00  张工 提交验证：20/20 PASS                                 │
│  12-18 14:30  李工 关闭工单                                             │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 十五、通知策略（企业微信）

### 15.1 通知触发点与接收人

| 事件 | 接收人 | 渠道 | 优先级 |
|------|--------|------|--------|
| 工单提交 | 分诊负责人/高级工程师群 | 应用消息 | 高 |
| 追问创建 | 工单创建者 | 应用消息 | 高 |
| 追问回复 | 分诊的高级工程师 | 应用消息 | 中 |
| SOL发布 | 工单创建者 + 客服 | 应用消息 | 高 |
| 验证提交 | SOL发布者 | 应用消息 | 中 |
| 工单关闭 | 相关人员 + 客服 | 应用消息 | 低 |

### 15.2 通知内容格式

```
【{事件类型}】{工单编号}
客户：{customer_name}
设备：{device_sn}
问题域：{domain} - {step_code}
状态：{status}
{额外信息}
[点击查看详情]({url})
```

---

## 十六、安全与合规

### 16.1 访问控制

- 附件下载：预签名URL，有效期15分钟
- API认证：JWT + HTTPS
- 敏感操作：需要二次确认（如删除、撤回SOL）

### 16.2 审计日志必记录项

| 操作 | 记录内容 |
|------|----------|
| 工单提交 | 谁、什么时候、完整facts_json快照 |
| SOL发布 | 谁、什么时候、变更内容、版本号 |
| SOL撤回 | 谁、什么时候、撤回原因 |
| 验证提交 | 谁、什么时候、结果、证据列表 |
| 工单关闭/重开 | 谁、什么时候、原因 |

### 16.3 数据驻留

- 默认：云端部署（Docker Compose）
- 可选：客户要求本地化时，提供私有部署包

---

## 十七、分阶段开发计划

### Sprint 1（MVP闭环，2周）

**目标**：跑通核心流程，可Demo

| 模块 | 功能 | 优先级 |
|------|------|--------|
| 认证 | 企业微信登录 + JWT | P0 |
| 工单 | Draft/Submit + facts_json | P0 |
| 附件 | 上传（不做断点，先做重试） | P0 |
| 分诊 | 绑定JC、创建SOL | P0 |
| 通知 | 提交/SOL发布通知 | P0 |
| 验证 | 提交验证结果 | P0 |
| 部署 | Docker Compose一键启动 | P0 |

**交付物**：可运行的系统，8个核心用例通过

### Sprint 2（可用性增强，2周）

| 模块 | 功能 | 优先级 |
|------|------|--------|
| 离线 | 草稿本地存储 + 补传队列 | P1 |
| 附件 | 分片断点续传 | P1 |
| 推荐 | 判断卡推荐规则引擎 | P1 |
| 统计 | 基础看板（闭环时长、Top问题域） | P1 |
| 通知 | 追问/验证通知 | P1 |

### Sprint 3（体验优化，2周）

| 模块 | 功能 | 优先级 |
|------|------|--------|
| 扫码 | 设备二维码生成 + 扫码预填 | P2 |
| 话术 | 客户沟通模板 | P2 |
| 搜索 | 工单全文搜索 | P2 |
| 导出 | 工单/统计Excel导出 | P2 |
| UI | 深色模式、多语言（可选） | P3 |

```

---

## 附录A：目录结构建议

```
field-ticket-system/
├── docker-compose.yml
├── .env.example
├── README.md
│
├── backend/                          # .NET 8 API
│   ├── src/
│   │   ├── FieldTicket.Api/          # API层
│   │   ├── FieldTicket.Core/         # 领域模型
│   │   ├── FieldTicket.Infrastructure/# 数据访问、外部服务
│   │   └── FieldTicket.Shared/       # 共享模型
│   ├── tests/
│   │   └── FieldTicket.Tests/
│   └── Dockerfile
│
├── web-admin/                        # React管理端
│   ├── src/
│   │   ├── pages/
│   │   ├── components/
│   │   ├── services/
│   │   └── stores/
│   └── Dockerfile
│
├── mobile-app/                       # Flutter App
│   ├── lib/
│   │   ├── pages/
│   │   ├── widgets/
│   │   ├── services/
│   │   ├── models/
│   │   └── providers/
│   └── pubspec.yaml
│
└── docs/
    ├── api-spec.yaml                 # OpenAPI规范
    ├── database-erd.png              # ER图
    └── test-cases.md                 # 测试用例
```
