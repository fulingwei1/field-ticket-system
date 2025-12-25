# 客户信息模块与设备信息模块 - 详细设计方案

## 一、数据存储方案对比分析

### 1.1 企业微信智能表格方案 vs 独立数据库方案

#### 企业微信智能表格的优劣

**优点：**
- ✓ 企业内部人员可直接在企业微信里查看和编辑，无需额外系统登录
- ✓ 已有现成的权限管理和审计日志
- ✓ 数据可视化展示方便，支持多维度筛选和汇总
- ✓ 与企业微信原生系统无缝集成（工单系统可直接读取）
- ✓ 无需维护独立的数据库
- ✓ 成本低，开箱即用

**缺点：**
- ✗ API限制：智能表格API有调用频率限制（不适合高频查询）
- ✗ 实时性不足：工单系统调用时延迟较大（秒级别）
- ✗ 数据一致性风险：智能表格与工单系统数据可能不同步
- ✗ 性能瓶颈：大数据量查询慢（比如10000+个设备的查询）
- ✗ 字段灵活性有限：复杂的验证规则难以实现
- ✗ 难以实现高级功能：如二维码批量生成、设备版本管理等
- ✗ 事务一致性差：多表关联操作容易出现不一致

#### 独立数据库方案的优劣

**优点：**
- ✓ 高性能：毫秒级查询响应
- ✓ 实时同步：工单系统实时访问最新数据
- ✓ 数据一致性强：支持事务和约束
- ✓ 灵活度高：支持复杂的业务逻辑
- ✓ 可扩展性强：支持大数据量
- ✓ 支持高级功能：版本管理、复杂关联、二维码生成等

**缺点：**
- ✗ 需要额外的系统架构和维护
- ✗ 企业内部人员编辑数据需要额外的Web页面
- ✗ 初期投入成本较高
- ✗ 需要数据同步策略（智能表格↔数据库）

---

### 1.2 **建议方案：混合方案**

```
┌──────────────────────────────────────┐
│   企业微信智能表格（主数据源）         │
│   - 用途：HR/管理员日常维护            │
│   - 数据：客户基本信息、设备基本信息   │
└──────────┬───────────────────────────┘
           │ 同步 (增量同步，每天2次)
           ↓
┌──────────────────────────────────────┐
│   独立PostgreSQL数据库（工作数据库）   │
│   - 用途：工单系统运行时访问           │
│   - 数据：客户、设备、版本、关联关系   │
│   - 特点：实时、高性能、完整关系       │
└──────────────────────────────────────┘
           │ 同步 (仅关键字段回写)
           ↓
┌──────────────────────────────────────┐
│   企业微信智能表格（更新反馈）         │
│   - 字段：设备所属工单数、最后问题日期  │
└──────────────────────────────────────┘
```

**为什么选择混合方案：**

1. **管理层面**：日常维护（增删改客户、设备基本信息）在企业微信智能表格中进行，用户体验好
2. **系统层面**：工单系统、小程序都从独立数据库读取，性能有保障
3. **数据流向**：客户/设备的增删改来自智能表格，通过同步引擎流向数据库
4. **反馈机制**：工单系统产生的统计数据（设备问题数、最后维保日期）回写到智能表格展示

---

## 二、客户信息模块详细设计

### 2.1 企业微信智能表格结构

#### 表名：`customers_master`（客户主表）

| 字段名 | 字段类型 | 说明 | 是否必填 | 示例 |
|-------|---------|------|---------|------|
| customer_id | 文本 | 客户唯一编号（系统生成）| 是 | CUST-20250001 |
| customer_name | 文本 | 客户名称 | 是 | 某新能源汽车有限公司 |
| short_name | 文本 | 客户简称 | 否 | 新能源A |
| industry | 单选 | 行业分类 | 是 | 新能源/汽车/消费电子/医疗 |
| region | 单选 | 地区 | 否 | 华东/华北/华南 |
| contact_person | 文本 | 主要联系人 | 是 | 张三 |
| contact_title | 文本 | 联系人职位 | 否 | 生产经理 |
| contact_phone | 电话 | 联系人电话 | 是 | 13800138000 |
| contact_email | 邮箱 | 联系人邮箱 | 否 | zhangsan@xx.com |
| company_address | 文本 | 企业地址 | 是 | 广东省深圳市南山区... |
| site_address | 文本 | 设备现场地址 | 否 | 广东省深圳市龙岗区... |
| contract_value | 数字 | 合同金额(万元) | 否 | 150 |
| status | 单选 | 客户状态 | 是 | 活跃/非活跃/流失/潜在 |
| service_level | 单选 | 服务等级 | 是 | 标准/VIP/关键 |
| notes | 长文本 | 备注 | 否 | 该客户对稳定性要求高 |
| created_at | 日期 | 创建时间 | 系统 | 2025-01-15 |
| updated_at | 日期 | 最后更新时间 | 系统 | 2025-01-20 |

#### 表名：`customer_contacts`（客户联系人表）

| 字段名 | 字段类型 | 说明 | 示例 |
|-------|---------|------|------|
| contact_id | 文本 | 联系人ID | CONT-00001 |
| customer_id | 关联 | 关联客户 | CUST-20250001 |
| name | 文本 | 联系人姓名 | 李四 |
| title | 文本 | 职位 | 采购经理 |
| phone | 电话 | 电话 | 13900139000 |
| email | 邮箱 | 邮箱 | lisi@xx.com |
| department | 文本 | 部门 | 采购部 |
| is_primary | 勾选 | 是否主要联系人 | ✓ |
| notes | 长文本 | 备注 | 负责所有技术审批 |

#### 表名：`customer_comms_record`（客户沟通记录表）

| 字段名 | 字段类型 | 说明 | 示例 |
|-------|---------|------|------|
| record_id | 文本 | 记录ID | REC-00001 |
| customer_id | 关联 | 客户 | CUST-20250001 |
| comm_date | 日期 | 沟通日期 | 2025-01-20 |
| comm_type | 单选 | 沟通方式 | 电话/微信/现场/邮件 |
| contact_person | 文本 | 对方联系人 | 张经理 |
| content | 长文本 | 沟通内容 | 关于停线问题的解决... |
| recorder | 文本 | 记录人 | 客服张三 |
| follow_up | 勾选 | 是否需要跟进 | ✓ |
| follow_up_date | 日期 | 跟进日期 | 2025-01-22 |

---

### 2.2 独立数据库表结构（PostgreSQL）

```sql
-- 客户表
CREATE TABLE customers (
    customer_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id VARCHAR(50) UNIQUE NOT NULL,  -- 来自智能表格的ID，用于同步对账
    customer_name VARCHAR(200) NOT NULL,
    short_name VARCHAR(50),
    industry VARCHAR(50),
    region VARCHAR(50),
    contact_person VARCHAR(100) NOT NULL,
    contact_title VARCHAR(100),
    contact_phone VARCHAR(20) NOT NULL,
    contact_email VARCHAR(100),
    company_address TEXT NOT NULL,
    site_address TEXT,
    contract_value DECIMAL(15, 2),           -- 合同金额
    status VARCHAR(20) DEFAULT 'active'      -- active/inactive/lost/potential
        CHECK (status IN ('active', 'inactive', 'lost', 'potential')),
    service_level VARCHAR(20) DEFAULT 'standard'
        CHECK (service_level IN ('standard', 'vip', 'critical')),
    notes TEXT,
    
    -- 统计字段（来自工单系统的回写）
    total_devices INT DEFAULT 0,             -- 该客户的设备总数
    total_tickets INT DEFAULT 0,             -- 该客户的工单总数
    last_ticket_date TIMESTAMPTZ,            -- 最后一次工单时间
    last_service_date TIMESTAMPTZ,           -- 最后一次服务时间
    
    -- 审计字段
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    synced_from_wecom_at TIMESTAMPTZ         -- 上次从企业微信同步的时间
);

-- 创建索引
CREATE INDEX idx_customers_external_id ON customers(external_id);
CREATE INDEX idx_customers_status ON customers(status);
CREATE INDEX idx_customers_service_level ON customers(service_level);

-- 客户联系人表
CREATE TABLE customer_contacts (
    contact_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    external_id VARCHAR(50) UNIQUE,          -- 来自智能表格的ID
    name VARCHAR(100) NOT NULL,
    title VARCHAR(100),
    phone VARCHAR(20),
    email VARCHAR(100),
    department VARCHAR(100),
    is_primary BOOLEAN DEFAULT FALSE,
    notes TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    synced_from_wecom_at TIMESTAMPTZ
);

CREATE INDEX idx_contacts_customer ON customer_contacts(customer_id);
CREATE INDEX idx_contacts_is_primary ON customer_contacts(customer_id, is_primary);

-- 客户沟通记录表
CREATE TABLE customer_communications (
    comm_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    ticket_id UUID REFERENCES tickets(ticket_id) ON DELETE SET NULL,  -- 可关联到工单
    
    comm_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    comm_type VARCHAR(20) NOT NULL
        CHECK (comm_type IN ('call', 'wechat', 'email', 'onsite', 'sms')),
    contact_person VARCHAR(100),
    content TEXT NOT NULL,
    recorded_by_user_id UUID NOT NULL REFERENCES users(id),
    
    follow_up_required BOOLEAN DEFAULT FALSE,
    follow_up_date TIMESTAMPTZ,
    follow_up_note TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_communications_customer ON customer_communications(customer_id);
CREATE INDEX idx_communications_ticket ON customer_communications(ticket_id);
CREATE INDEX idx_communications_date ON customer_communications(comm_date DESC);

-- 客户服务记录（维保历史）
CREATE TABLE customer_service_records (
    service_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    device_id UUID REFERENCES devices(device_id) ON DELETE SET NULL,
    
    service_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    service_type VARCHAR(50) NOT NULL        -- maintenance/inspection/upgrade/repair
        CHECK (service_type IN ('maintenance', 'inspection', 'upgrade', 'repair')),
    service_content TEXT NOT NULL,
    parts_replaced TEXT,                     -- 更换零部件清单
    engineer_id UUID NOT NULL REFERENCES users(id),
    hours_spent DECIMAL(5, 2),               -- 花费小时数
    cost DECIMAL(15, 2),                     -- 费用
    notes TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_service_records_customer ON customer_service_records(customer_id);
CREATE INDEX idx_service_records_device ON customer_service_records(device_id);
CREATE INDEX idx_service_records_date ON customer_service_records(service_date DESC);
```

---

## 三、设备信息模块详细设计

### 3.1 企业微信智能表格结构

#### 表名：`devices_master`（设备主表）

| 字段名 | 字段类型 | 说明 | 是否必填 | 示例 |
|-------|---------|------|---------|------|
| device_id | 文本 | 设备唯一编号 | 是 | DEV-20250001 |
| device_sn | 文本 | 设备序列号 | 是 | SN202501001 |
| qr_code | 文本 | 二维码内容 | 否 | (与SN相同) |
| customer_id | 关联 | 关联客户 | 是 | CUST-20250001 |
| project_id | 关联 | 关联项目 | 是 | PROJ-20250001 |
| device_model | 文本 | 设备型号 | 是 | ATM-2000-V3 |
| device_type | 单选 | 设备类型 | 是 | 测试设备/线体/夹具 |
| manufacture_date | 日期 | 制造日期 | 是 | 2024-12-15 |
| delivery_date | 日期 | 交付日期 | 是 | 2025-01-10 |
| warranty_end_date | 日期 | 保修期至 | 是 | 2026-01-10 |
| install_location | 文本 | 安装地点 | 是 | 厂房B区3号线 |
| is_active | 勾选 | 是否在用 | 是 | ✓ |
| status | 单选 | 设备状态 | 是 | 正常/维修中/已下线 |

#### 表名：`device_versions`（设备版本配置表）

| 字段名 | 字段类型 | 说明 | 示例 |
|-------|---------|------|------|
| version_id | 文本 | 版本记录ID | VER-00001 |
| device_id | 关联 | 关联设备 | DEV-20250001 |
| version_date | 日期 | 版本生效日期 | 2025-01-15 |
| sw_version | 文本 | 软件版本 | V1.2.3 |
| plc_version | 文本 | PLC版本 | V2.0.1 |
| param_version | 文本 | 参数版本 | V1.0 |
| notes | 长文本 | 版本说明 | 修复停线问题 |

---

### 3.2 独立数据库表结构（PostgreSQL）

```sql
-- 设备表（完整版）
CREATE TABLE devices (
    device_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id VARCHAR(50) UNIQUE NOT NULL,  -- 来自智能表格的ID
    
    -- 基本信息
    device_sn VARCHAR(100) NOT NULL UNIQUE,
    qr_code VARCHAR(64) UNIQUE,
    customer_id UUID NOT NULL REFERENCES customers(customer_id) ON DELETE RESTRICT,
    project_id UUID NOT NULL REFERENCES projects(project_id) ON DELETE RESTRICT,
    station_id UUID REFERENCES stations(station_id),
    
    -- 设备属性
    device_model VARCHAR(100) NOT NULL,
    device_type VARCHAR(50) NOT NULL         -- testing_equipment/line/fixture
        CHECK (device_type IN ('testing_equipment', 'line', 'fixture')),
    
    -- 时间信息
    manufacture_date DATE,
    delivery_date DATE NOT NULL,
    warranty_start_date DATE,
    warranty_end_date DATE,
    
    -- 位置信息
    install_location TEXT NOT NULL,
    latitude DECIMAL(10, 8),                 -- GPS坐标（可选）
    longitude DECIMAL(11, 8),
    
    -- 当前版本信息（冗余，便于查询）
    current_sw_version VARCHAR(50),
    current_plc_version VARCHAR(50),
    current_param_version VARCHAR(50),
    version_updated_at TIMESTAMPTZ,
    
    -- 状态
    is_active BOOLEAN DEFAULT TRUE,
    status VARCHAR(20) DEFAULT 'normal'
        CHECK (status IN ('normal', 'under_maintenance', 'offline')),
    
    -- 统计字段（来自工单系统回写）
    total_tickets INT DEFAULT 0,
    total_problems INT DEFAULT 0,
    last_problem_date TIMESTAMPTZ,
    last_service_date TIMESTAMPTZ,
    average_mttr INTERVAL,                   -- 平均修复时间 (Mean Time To Repair)
    
    -- 审计字段
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    synced_from_wecom_at TIMESTAMPTZ
);

-- 创建索引
CREATE INDEX idx_devices_sn ON devices(device_sn);
CREATE INDEX idx_devices_qr ON devices(qr_code);
CREATE INDEX idx_devices_customer ON devices(customer_id);
CREATE INDEX idx_devices_project ON devices(project_id);
CREATE INDEX idx_devices_status ON devices(status);
CREATE INDEX idx_devices_warranty ON devices(warranty_end_date);

-- 设备版本历史表
CREATE TABLE device_versions (
    version_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id VARCHAR(50) UNIQUE,
    device_id UUID NOT NULL REFERENCES devices(device_id) ON DELETE CASCADE,
    
    version_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    sw_version VARCHAR(50) NOT NULL,
    plc_version VARCHAR(50) NOT NULL,
    param_version VARCHAR(50) NOT NULL,
    
    updated_reason VARCHAR(200),             -- 版本更新原因
    updated_by_user_id UUID REFERENCES users(id),
    
    notes TEXT,
    is_current BOOLEAN DEFAULT FALSE,        -- 是否为当前版本
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_device_versions_device ON device_versions(device_id);
CREATE INDEX idx_device_versions_current ON device_versions(device_id, is_current);
CREATE INDEX idx_device_versions_date ON device_versions(device_id, version_date DESC);

-- 设备配置表（存储特殊的设备参数）
CREATE TABLE device_configurations (
    config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID NOT NULL REFERENCES devices(device_id) ON DELETE CASCADE,
    
    config_key VARCHAR(100) NOT NULL,        -- 配置项键名
    config_value TEXT NOT NULL,              -- 配置值
    description TEXT,                        -- 描述
    
    updated_by_user_id UUID REFERENCES users(id),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(device_id, config_key)
);

-- 设备附件表（设备的图纸、手册等）
CREATE TABLE device_attachments (
    attachment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID NOT NULL REFERENCES devices(device_id) ON DELETE CASCADE,
    
    attachment_type VARCHAR(50) NOT NULL     -- manual/drawing/certificate/maintenance_log
        CHECK (attachment_type IN ('manual', 'drawing', 'certificate', 'maintenance_log', 'other')),
    
    file_key VARCHAR(500) NOT NULL,          -- 对象存储的Key
    file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100),
    
    uploaded_by_user_id UUID NOT NULL REFERENCES users(id),
    uploaded_at TIMESTAMPTZ DEFAULT NOW(),
    
    description TEXT
);

CREATE INDEX idx_device_attachments_device ON device_attachments(device_id);

-- 设备维保计划表
CREATE TABLE device_maintenance_plans (
    plan_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id UUID NOT NULL REFERENCES devices(device_id) ON DELETE CASCADE,
    
    plan_name VARCHAR(100) NOT NULL,         -- 计划名称
    plan_type VARCHAR(50) NOT NULL           -- preventive/corrective
        CHECK (plan_type IN ('preventive', 'corrective')),
    
    maintenance_interval INT,                -- 维保周期（天）
    last_maintenance_date TIMESTAMPTZ,
    next_maintenance_date TIMESTAMPTZ,
    
    maintenance_checklist TEXT,              -- 维保检查清单
    estimated_cost DECIMAL(15, 2),
    notes TEXT,
    
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_maintenance_plans_device ON device_maintenance_plans(device_id);
CREATE INDEX idx_maintenance_plans_next_date ON device_maintenance_plans(next_maintenance_date);
```

---

## 四、功能清单

### 4.1 客户信息管理功能

| 功能 | 说明 | 使用者 | 实现位置 |
|------|------|--------|--------|
| **客户档案查询** | 按客户名称、行业、地区等筛选查询 | 客服、工程师、管理员 | Web + App |
| **客户详情查看** | 查看客户完整信息、联系人、历史问题、维保记录 | 客服、工程师 | Web + App |
| **客户档案编辑** | 编辑客户基本信息 | 管理员 | 企业微信智能表格 + Web同步 |
| **客户联系人管理** | 添加/编辑/删除客户联系人 | 管理员 | 企业微信智能表格 |
| **客户沟通记录** | 记录与客户的沟通内容（电话、微信、现场等） | 客服、工程师 | App + Web |
| **客户沟通历史查询** | 查看某客户的历史沟通记录 | 客服、工程师、管理员 | Web + App |
| **客户服务记录** | 记录对客户的维保、现场支持等服务 | 工程师 | App |
| **客户统计报表** | 客户工单量、平均解决周期、满意度等统计 | 管理员 | Web报表 |
| **客户导入/导出** | 批量导入/导出客户信息 | 管理员 | Web |

### 4.2 设备信息管理功能

| 功能 | 说明 | 使用者 | 实现位置 |
|------|------|--------|--------|
| **设备档案查询** | 按客户、项目、型号、状态等筛选查询设备 | 客服、工程师、现场 | Web + App |
| **设备详情查看** | 查看设备完整信息、版本、历史问题、维保计划 | 客服、工程师、现场 | Web + App |
| **设备档案编辑** | 编辑设备基本信息 | 管理员 | 企业微信智能表格 + Web同步 |
| **设备二维码生成** | 批量生成设备二维码用于现场标识 | 管理员 | Web |
| **设备扫码查询** | 现场扫描二维码快速获取设备信息 | 现场工程师 | App |
| **设备版本管理** | 记录和管理设备的软件、PLC、参数版本 | 工程师、管理员 | Web + App |
| **设备版本历史** | 查看设备版本变更历史 | 工程师、管理员 | Web |
| **设备附件管理** | 上传和管理设备的图纸、手册、证书等 | 管理员、工程师 | Web |
| **维保计划管理** | 制定和管理设备的预防性维保计划 | 工程师、管理员 | Web |
| **维保提醒** | 到期维保的自动提醒 | 管理员、工程师 | Web + App |
| **设备保修期查询** | 查询设备保修期状态 | 客服、工程师 | Web + App |
| **设备统计报表** | 设备工单量、故障率、停线时间等统计 | 管理员 | Web报表 |
| **设备导入/导出** | 批量导入/导出设备信息 | 管理员 | Web |

---

## 五、页面设计清单

### 5.1 Web管理端页面（客户和设备管理）

#### 客户信息管理

| 页面ID | 页面名称 | 功能 | 访问权限 |
|--------|--------|------|--------|
| P1 | 客户列表 | 客户查询、筛选、排序、批量操作 | Admin, CS |
| P2 | 客户详情 | 查看客户完整信息、联系人、历史问题、维保服务 | Admin, CS |
| P3 | 客户编辑 | 编辑客户基本信息 | Admin |
| P4 | 联系人管理 | 添加/编辑/删除联系人 | Admin |
| P5 | 沟通记录列表 | 查看客户沟通历史 | Admin, CS, SE |
| P6 | 沟通记录编辑 | 新增/编辑沟通记录 | Admin, CS, SE |
| P7 | 服务记录列表 | 查看客户维保服务历史 | Admin, CS, SE |
| P8 | 服务记录编辑 | 新增/编辑服务记录 | Admin, SE |
| P9 | 客户报表 | 客户统计报表（工单量、响应时间、满意度等） | Admin |
| P10 | 客户导入 | 批量导入客户信息 | Admin |

#### 设备信息管理

| 页面ID | 页面名称 | 功能 | 访问权限 |
|--------|--------|------|--------|
| P11 | 设备列表 | 设备查询、筛选、排序、批量操作 | Admin, CS, SE |
| P12 | 设备详情 | 查看设备完整信息、版本、历史问题、维保计划 | Admin, CS, SE |
| P13 | 设备编辑 | 编辑设备基本信息 | Admin |
| P14 | 设备二维码生成 | 批量生成和下载二维码 | Admin |
| P15 | 设备版本列表 | 查看设备的版本历史 | Admin, CS, SE |
| P16 | 设备版本编辑 | 新增/编辑设备版本 | Admin, SE |
| P17 | 设备附件管理 | 上传/下载/删除设备附件 | Admin, SE |
| P18 | 维保计划列表 | 查看和管理维保计划 | Admin, SE |
| P19 | 维保计划编辑 | 新增/编辑维保计划 | Admin, SE |
| P20 | 设备报表 | 设备统计报表（工单量、故障率等） | Admin |
| P21 | 设备导入 | 批量导入设备信息 | Admin |

### 5.2 小程序页面（现场工程师使用）

| 页面ID | 页面名称 | 功能 | 说明 |
|--------|--------|------|------|
| A1 | 客户查询 | 输入客户名或扫描二维码快速查找客户 | 快速导航 |
| A2 | 客户详情 | 查看客户信息、联系人、历史问题列表 | 只读 |
| A3 | 设备快查 | 通过设备SN或扫描二维码快速查找设备 | 快速导航 |
| A4 | 设备详情 | 查看设备完整信息、版本、历史问题 | 只读+版本查询 |
| A5 | 沟通记录新增 | 快速记录与客户的沟通内容 | 支持离线草稿 |

---

## 六、工单系统的关联关系

### 6.1 数据关联图

```
┌─────────────────┐
│   customers     │
└────────┬────────┘
         │ 1:N
         ↓
┌─────────────────┐
│   devices       │◄────────┐
└────────┬────────┘         │
         │ 1:N              │
         ↓                  │
┌─────────────────┐         │
│   tickets       ├─────────┘
└────────┬────────┘ (device_id)
         │ 1:N
         ↓
┌─────────────────────────────┐
│   customer_communications   │
│   (关联到工单和客户)         │
└─────────────────────────────┘
```

### 6.2 关键关联说明

**1. 工单创建时关联客户和设备**
```sql
-- 工单表中的外键关系
ALTER TABLE tickets ADD CONSTRAINT fk_tickets_customer 
    FOREIGN KEY (customer_id) REFERENCES customers(customer_id);

ALTER TABLE tickets ADD CONSTRAINT fk_tickets_device 
    FOREIGN KEY (device_id) REFERENCES devices(device_id);
```

**2. 工单中查询设备时，自动带出客户信息**
```
工单创建流程：
1. 现场工程师扫描设备二维码 → 通过device_sn查询设备
2. 自动关联到该设备所属的customer_id和project_id
3. 工程师无需再手动输入客户和项目
```

**3. 工单关闭时，更新设备的统计信息**
```sql
-- 工单关闭时触发更新
UPDATE devices 
SET 
  total_tickets = (SELECT COUNT(*) FROM tickets WHERE device_id = devices.device_id),
  last_problem_date = (SELECT MAX(created_at) FROM tickets WHERE device_id = devices.device_id),
  average_mttr = INTERVAL '...'
WHERE device_id = ?
```

**4. 客户沟通记录关联到具体工单**
```sql
-- 可选：沟通记录可关联到具体的工单
CREATE TABLE customer_communications (
    ...
    ticket_id UUID REFERENCES tickets(ticket_id) ON DELETE SET NULL,
    ...
);
```

---

## 七、数据同步策略

### 7.1 智能表格 → 数据库 同步

**同步时机**：每天凌晨2点和中午12点自动同步（或手动触发）

**同步流程**：

```
1. 从企业微信智能表格API读取所有客户记录
2. 对比本地数据库：
   - 新增记录：INSERT到数据库
   - 修改记录：UPDATE对应记录
   - 删除记录：标记为inactive（逻辑删除，不真正删除）
3. 将同步时间戳写入数据库的 synced_from_wecom_at 字段
4. 记录同步日志：成功数、失败数、耗时
```

**伪代码**：

```python
def sync_customers_from_wecom():
    # 从企业微信智能表格获取数据
    wecom_customers = get_wecom_table_data('customers_master')
    
    for wecom_record in wecom_customers:
        external_id = wecom_record['customer_id']
        
        local_record = db.query(Customer).filter_by(external_id=external_id).first()
        
        if local_record is None:
            # 新增
            new_customer = Customer(
                external_id=external_id,
                customer_name=wecom_record['customer_name'],
                ...
                synced_from_wecom_at=now()
            )
            db.add(new_customer)
        else:
            # 更新
            local_record.customer_name = wecom_record['customer_name']
            local_record.updated_at = now()
            local_record.synced_from_wecom_at = now()
    
    db.commit()
    log_sync_result(success=len(wecom_customers), failed=0)
```

### 7.2 数据库 → 智能表格 回写

**同步时机**：每小时回写一次（或实时）

**回写字段**：仅回写统计和状态字段

| 回写字段 | 来源 | 说明 |
|--------|------|------|
| total_devices | devices表 | 该客户有多少设备 |
| total_tickets | tickets表 | 该客户有多少工单 |
| last_problem_date | tickets表 | 最后一次问题日期 |
| last_service_date | service_records表 | 最后一次维保日期 |
| device total_tickets | tickets表 | 该设备有多少工单 |
| device last_problem_date | tickets表 | 该设备最后一次问题日期 |
| device status | devices表 | 设备当前状态 |

---

## 八、实施建议

### 8.1 第一期：基础建设（第1-2周）

- [ ] 在企业微信智能表格中创建客户、设备、联系人等表
- [ ] 设计并创建独立PostgreSQL数据库及所有表结构
- [ ] 开发数据同步引擎（智能表格→数据库）
- [ ] 开发基础的客户列表和设备列表Web页面

### 8.2 第二期：功能完善（第3-4周）

- [ ] 完成客户详情、设备详情页面
- [ ] 完成客户/设备编辑页面
- [ ] 实现二维码生成和扫码查询功能
- [ ] 小程序中集成客户/设备快查功能
- [ ] 测试与联调

### 8.3 第三期：高级功能（第5-6周）

- [ ] 版本管理、维保计划、附件管理等功能
- [ ] 统计报表
- [ ] 数据回写（数据库→智能表格）
- [ ] 性能优化和容错处理

---

## 九、关键配置项

### 9.1 企业微信API配置

```
企业微信智能表格API:
- 企业ID (corp_id)：xxxxxx
- 应用Secret：xxxxxx
- 表ID (customers_master)：xxxxxx
- 表ID (devices_master)：xxxxxx
- API调用频率限制：[需根据企业微信限制调整]
```

### 9.2 数据库连接配置

```
PostgreSQL连接：
- Host：[生产数据库地址]
- Port：5432
- Database：ticket_system_db
- User：[用户名]
- Password：[密码]
- SSL：enabled
- 连接池大小：20-50
```

### 9.3 对象存储配置（用于附件上传）

```
阿里云OSS 或 腾讯云COS：
- Bucket：devices-attachments
- Region：[根据实际选择]
- Access Key ID：[xxxx]
- Access Key Secret：[xxxx]
```

---

## 十、风险与应对

| 风险 | 影响 | 概率 | 应对措施 |
|------|------|------|--------|
| 企业微信表格API限流 | 同步延迟 | 中 | 实现本地缓存、提前同步、批量操作 |
| 数据不一致 | 工单创建失败 | 低 | 对账机制、事务处理、重试机制 |
| 设备扫码失败 | 现场体验差 | 低 | 提供手工查询备选方案、离线数据缓存 |
| 版本管理混乱 | 排故困难 | 中 | 严格的版本命名规范、审计日志 |
| 维保计划未执行 | 设备故障增多 | 中 | 自动提醒、KPI指标追踪 |

---

## 十一、后续验证检查清单

- [ ] 企业微信智能表格已创建并配置
- [ ] PostgreSQL数据库已创建并建立所有表
- [ ] 同步引擎运行正常（测试至少1次完整同步）
- [ ] Web页面可正常访问客户/设备列表
- [ ] 小程序可正常扫码查询
- [ ] 工单系统可正常关联客户和设备
- [ ] 性能测试通过（1万+ 设备查询响应 < 2秒）
- [ ] 安全审计通过（权限控制、数据隔离）

---

**文件版本**：v1.0  
**最后更新**：2025年12月  
**建议开始实施**：1周内启动第一期建设
