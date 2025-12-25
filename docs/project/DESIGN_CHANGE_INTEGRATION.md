# 设计变更系统集成方案

> **日期**：2025-12-22  
> **状态**：接口预留阶段（设计变更系统尚未开发）  
> **目标**：预留接口和对接规范，待设计变更系统开发完成后进行集成

---

## 📋 项目背景

- **设计变更管理系统**：后续将开发独立的设计变更管理系统（不在本客服系统中）
- **客服系统**：需要预留接口和对接规范，以便后续集成
- **集成方式**：通过API接口对接，客服系统从设计变更系统读取数据

---

## 🎯 需求分析

### 核心需求

1. **查询设计变更信息**
   - 设备变更了什么（软件、硬件、参数等）
   - 变更时间、变更原因
   - 变更影响范围

2. **查询物料到货情况**
   - 哪些物料还没有到现场
   - 物料预计到货时间
   - 物料当前状态（采购中、运输中、已到货等）

3. **工作安排支持**
   - 根据物料到货时间安排工作
   - 提前准备变更所需的物料和工具
   - 避免因物料未到而延误工作

---

## 🏗️ 架构设计

### 集成方式

**方案选择**：**只读集成**（Read-Only Integration）

- 设计变更系统是独立系统，客服系统不管理设计变更
- 客服系统通过API从设计变更系统读取数据
- 数据可以缓存到本地，但以外部系统为准

### 数据同步策略

**方案1：定时同步（推荐）**
- 优点：减少外部系统压力，响应快
- 缺点：数据可能有延迟
- 实现：每小时或每天同步一次

**方案2：实时查询**
- 优点：数据实时
- 缺点：依赖外部系统可用性
- 实现：查询时实时调用外部API

**方案3：混合模式（推荐）**
- 定时同步基础数据（设计变更列表、物料列表）
- 实时查询详细信息（变更详情、物料状态）

---

## 📊 数据模型设计

### 1. 设计变更信息（只读缓存）

```sql
-- 设计变更表（从外部系统同步）
CREATE TABLE design_changes_cache (
    change_id VARCHAR(100) PRIMARY KEY,  -- 外部系统的变更ID
    external_system VARCHAR(50) NOT NULL,  -- 外部系统标识
    
    -- 变更基本信息
    change_code VARCHAR(50) NOT NULL,  -- 变更编号，如 DC-2025-001
    change_title VARCHAR(200) NOT NULL,
    change_type VARCHAR(50),  -- 'software', 'hardware', 'parameter', 'assembly'
    change_reason TEXT,
    change_description TEXT,
    
    -- 变更内容
    change_content JSONB,
    /*
    {
      "affected_devices": ["device_sn1", "device_sn2"],
      "affected_projects": ["project1", "project2"],
      "software_changes": [
        {
          "module": "control",
          "version_before": "v1.0",
          "version_after": "v1.1",
          "change_detail": "..."
        }
      ],
      "hardware_changes": [
        {
          "component": "sensor",
          "model_before": "SEN-001",
          "model_after": "SEN-002",
          "change_detail": "..."
        }
      ],
      "parameter_changes": [
        {
          "parameter_name": "timeout",
          "value_before": "300ms",
          "value_after": "500ms",
          "change_detail": "..."
        }
      ]
    }
    */
    
    -- 变更状态
    status VARCHAR(50),  -- 'draft', 'approved', 'in_progress', 'completed', 'cancelled'
    approval_date TIMESTAMPTZ,
    implementation_date TIMESTAMPTZ,
    completion_date TIMESTAMPTZ,
    
    -- 关联信息
    project_id UUID REFERENCES projects(project_id),
    project_code VARCHAR(50),
    customer_id UUID REFERENCES customers(customer_id),
    
    -- 同步信息
    synced_at TIMESTAMPTZ DEFAULT NOW(),
    last_updated_at TIMESTAMPTZ,  -- 外部系统最后更新时间
    sync_status VARCHAR(20) DEFAULT 'success',  -- 'success', 'failed', 'pending'
    sync_error TEXT,
    
    -- 索引
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_design_changes_project ON design_changes_cache(project_id);
CREATE INDEX idx_design_changes_customer ON design_changes_cache(customer_id);
CREATE INDEX idx_design_changes_status ON design_changes_cache(status);
CREATE INDEX idx_design_changes_synced ON design_changes_cache(synced_at DESC);
```

### 2. 物料跟踪信息（只读缓存）

```sql
-- 物料跟踪表（从外部系统同步）
CREATE TABLE material_tracking_cache (
    material_id VARCHAR(100) PRIMARY KEY,  -- 外部系统的物料ID
    external_system VARCHAR(50) NOT NULL,
    
    -- 物料基本信息
    material_code VARCHAR(50) NOT NULL,  -- 物料编号
    material_name VARCHAR(200) NOT NULL,
    material_type VARCHAR(50),  -- 'component', 'tool', 'spare_part', 'consumable'
    material_spec TEXT,
    quantity INT NOT NULL,
    unit VARCHAR(20),  -- 'pcs', 'kg', 'm', etc.
    
    -- 关联信息
    change_id VARCHAR(100) REFERENCES design_changes_cache(change_id),
    project_id UUID REFERENCES projects(project_id),
    device_id UUID REFERENCES devices(device_id),
    device_sn VARCHAR(100),
    
    -- 物料状态
    status VARCHAR(50) NOT NULL,  -- 'pending', 'ordered', 'in_transit', 'arrived', 'installed', 'cancelled'
    current_location VARCHAR(200),  -- 当前位置
    
    -- 时间信息
    order_date TIMESTAMPTZ,
    expected_arrival_date TIMESTAMPTZ,  -- 预计到货时间
    actual_arrival_date TIMESTAMPTZ,  -- 实际到货时间
    installation_date TIMESTAMPTZ,  -- 安装时间
    
    -- 物流信息
    tracking_number VARCHAR(100),  -- 物流单号
    carrier VARCHAR(100),  -- 承运商
    shipping_address TEXT,  -- 收货地址
    
    -- 同步信息
    synced_at TIMESTAMPTZ DEFAULT NOW(),
    last_updated_at TIMESTAMPTZ,
    sync_status VARCHAR(20) DEFAULT 'success',
    sync_error TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_materials_change ON material_tracking_cache(change_id);
CREATE INDEX idx_materials_project ON material_tracking_cache(project_id);
CREATE INDEX idx_materials_device ON material_tracking_cache(device_id);
CREATE INDEX idx_materials_status ON material_tracking_cache(status);
CREATE INDEX idx_materials_arrival ON material_tracking_cache(expected_arrival_date);
```

### 3. 设备变更关联表

```sql
-- 设备与设计变更关联表（用于快速查询）
CREATE TABLE device_change_relations (
    relation_id UUID PRIMARY KEY,
    device_id UUID NOT NULL REFERENCES devices(device_id),
    change_id VARCHAR(100) NOT NULL REFERENCES design_changes_cache(change_id),
    
    -- 关联类型
    relation_type VARCHAR(50),  -- 'direct', 'indirect', 'affected'
    
    -- 变更影响
    impact_level VARCHAR(20),  -- 'low', 'medium', 'high', 'critical'
    impact_description TEXT,
    
    -- 变更状态（针对该设备）
    device_change_status VARCHAR(50),  -- 'pending', 'in_progress', 'completed', 'cancelled'
    device_change_date TIMESTAMPTZ,  -- 该设备变更完成时间
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(device_id, change_id)
);

CREATE INDEX idx_device_change_device ON device_change_relations(device_id);
CREATE INDEX idx_device_change_change ON device_change_relations(change_id);
CREATE INDEX idx_device_change_status ON device_change_relations(device_change_status);
```

---

## 🔌 接口对接规范

### 1. 接口契约定义（设计变更系统需要实现的接口）

> **说明**：以下接口规范是设计变更系统需要实现的接口，客服系统将调用这些接口获取数据。

#### 1.1 设计变更查询接口

```http
# 获取设计变更列表
GET /api/v1/design-changes
Query Parameters:
  - projectCode (string, optional): 项目编号
  - deviceSn (string, optional): 设备序列号
  - fromDate (datetime, optional): 开始日期
  - toDate (datetime, optional): 结束日期
  - status (string, optional): 变更状态
  - page (int, default=1): 页码
  - pageSize (int, default=20): 每页数量

Response 200:
{
  "code": 200,
  "message": "success",
  "data": {
    "items": [
      {
        "changeId": "DC-2025-001",
        "changeCode": "DC-2025-001",
        "changeTitle": "传感器升级",
        "changeType": "hardware",
        "changeReason": "提升精度",
        "changeDescription": "将传感器从SEN-001升级到SEN-002",
        "changeContent": {
          "affectedDevices": ["SN001", "SN002"],
          "affectedProjects": ["PRJ-001"],
          "softwareChanges": [],
          "hardwareChanges": [
            {
              "component": "sensor",
              "modelBefore": "SEN-001",
              "modelAfter": "SEN-002",
              "changeDetail": "精度提升10%"
            }
          ],
          "parameterChanges": []
        },
        "status": "completed",
        "approvalDate": "2025-12-01T10:00:00Z",
        "implementationDate": "2025-12-05T10:00:00Z",
        "completionDate": "2025-12-10T10:00:00Z",
        "projectCode": "PRJ-001",
        "customerId": "CUST-001"
      }
    ],
    "total": 100,
    "page": 1,
    "pageSize": 20
  }
}

# 获取设计变更详情
GET /api/v1/design-changes/{changeId}

Response 200:
{
  "code": 200,
  "message": "success",
  "data": {
    "changeId": "DC-2025-001",
    "changeCode": "DC-2025-001",
    "changeTitle": "传感器升级",
    "changeType": "hardware",
    "changeReason": "提升精度",
    "changeDescription": "将传感器从SEN-001升级到SEN-002",
    "changeContent": { ... },
    "status": "completed",
    "approvalDate": "2025-12-01T10:00:00Z",
    "implementationDate": "2025-12-05T10:00:00Z",
    "completionDate": "2025-12-10T10:00:00Z",
    "projectCode": "PRJ-001",
    "customerId": "CUST-001",
    "materials": [
      {
        "materialId": "MAT-001",
        "materialCode": "MAT-001",
        "materialName": "新型传感器",
        "status": "arrived",
        "expectedArrivalDate": "2025-12-08T10:00:00Z",
        "actualArrivalDate": "2025-12-08T10:00:00Z"
      }
    ]
  }
}
```

#### 1.2 物料跟踪查询接口

```http
# 获取物料跟踪列表
GET /api/v1/materials
Query Parameters:
  - changeId (string, optional): 变更ID
  - projectCode (string, optional): 项目编号
  - deviceSn (string, optional): 设备序列号
  - status (string, optional): 物料状态 (pending, ordered, in_transit, arrived, installed, cancelled)
  - expectedDateFrom (datetime, optional): 预计到货开始日期
  - expectedDateTo (datetime, optional): 预计到货结束日期
  - page (int, default=1): 页码
  - pageSize (int, default=20): 每页数量

Response 200:
{
  "code": 200,
  "message": "success",
  "data": {
    "items": [
      {
        "materialId": "MAT-001",
        "materialCode": "MAT-001",
        "materialName": "新型传感器",
        "materialType": "component",
        "materialSpec": "精度±0.1mm",
        "quantity": 2,
        "unit": "pcs",
        "changeId": "DC-2025-001",
        "projectCode": "PRJ-001",
        "deviceSn": "SN001",
        "status": "arrived",
        "currentLocation": "现场仓库",
        "orderDate": "2025-12-01T10:00:00Z",
        "expectedArrivalDate": "2025-12-08T10:00:00Z",
        "actualArrivalDate": "2025-12-08T10:00:00Z",
        "installationDate": null,
        "trackingNumber": "SF1234567890",
        "carrier": "顺丰快递",
        "shippingAddress": "XX市XX区XX路XX号"
      }
    ],
    "total": 50,
    "page": 1,
    "pageSize": 20
  }
}

# 获取物料详情
GET /api/v1/materials/{materialId}

Response 200:
{
  "code": 200,
  "message": "success",
  "data": {
    "materialId": "MAT-001",
    "materialCode": "MAT-001",
    "materialName": "新型传感器",
    "materialType": "component",
    "materialSpec": "精度±0.1mm",
    "quantity": 2,
    "unit": "pcs",
    "changeId": "DC-2025-001",
    "projectCode": "PRJ-001",
    "deviceSn": "SN001",
    "status": "arrived",
    "currentLocation": "现场仓库",
    "orderDate": "2025-12-01T10:00:00Z",
    "expectedArrivalDate": "2025-12-08T10:00:00Z",
    "actualArrivalDate": "2025-12-08T10:00:00Z",
    "installationDate": null,
    "trackingNumber": "SF1234567890",
    "carrier": "顺丰快递",
    "shippingAddress": "XX市XX区XX路XX号",
    "changeInfo": {
      "changeId": "DC-2025-001",
      "changeTitle": "传感器升级"
    }
  }
}
```

#### 1.3 综合查询接口（推荐）

```http
# 获取设备变更和物料情况（一站式查询）
GET /api/v1/devices/{deviceSn}/change-summary

Response 200:
{
  "code": 200,
  "message": "success",
  "data": {
    "deviceId": "DEV-001",
    "deviceSn": "SN001",
    "recentChanges": [
      {
        "changeId": "DC-2025-001",
        "changeTitle": "传感器升级",
        "changeType": "hardware",
        "changeDate": "2025-12-10T10:00:00Z",
        "status": "completed",
        "materials": [
          {
            "materialCode": "MAT-001",
            "materialName": "新型传感器",
            "status": "arrived",
            "arrivalDate": "2025-12-08T10:00:00Z"
          }
        ]
      }
    ],
    "pendingMaterials": [
      {
        "materialCode": "MAT-002",
        "materialName": "控制板",
        "expectedArrivalDate": "2025-12-25T10:00:00Z",
        "status": "in_transit",
        "changeId": "DC-2025-002",
        "changeTitle": "控制板升级"
      }
    ],
    "upcomingChanges": [
      {
        "changeId": "DC-2025-002",
        "changeTitle": "控制板升级",
        "expectedDate": "2025-12-28T10:00:00Z",
        "pendingMaterials": 2,
        "status": "approved"
      }
    ]
  }
}
```

#### 1.4 认证和授权

```http
# 认证方式：Bearer Token
Authorization: Bearer {access_token}

# Token获取（如果需要）
POST /api/v1/auth/token
Body:
{
  "clientId": "field-ticket-system",
  "clientSecret": "your-secret"
}

Response 200:
{
  "code": 200,
  "message": "success",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "tokenType": "Bearer"
  }
}
```

### 2. 接口实现规范

#### 2.1 响应格式

所有接口统一使用以下响应格式：

```json
{
  "code": 200,           // HTTP状态码
  "message": "success",   // 消息
  "data": { ... }        // 数据（成功时）
}
```

错误响应：

```json
{
  "code": 400,
  "message": "参数错误",
  "error": {
    "code": "INVALID_PARAMETER",
    "details": "deviceSn不能为空"
  }
}
```

#### 2.2 分页规范

所有列表接口支持分页：

```json
{
  "items": [...],
  "total": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

#### 2.3 状态码定义

| 状态码 | 说明 |
|--------|------|
| 200 | 成功 |
| 400 | 参数错误 |
| 401 | 未授权 |
| 403 | 无权限 |
| 404 | 资源不存在 |
| 500 | 服务器错误 |

### 3. 客服系统接口客户端（预留）

```csharp
// 客服系统中的接口客户端接口定义（待实现）
public interface IDesignChangeSystemClient
{
    // 获取设计变更列表
    Task<ApiResponse<List<DesignChangeDto>>> GetDesignChangesAsync(
        string projectCode = null,
        string deviceSn = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string status = null,
        int page = 1,
        int pageSize = 20);
    
    // 获取设计变更详情
    Task<ApiResponse<DesignChangeDetailDto>> GetDesignChangeDetailAsync(string changeId);
    
    // 获取物料跟踪列表
    Task<ApiResponse<List<MaterialTrackingDto>>> GetMaterialTrackingAsync(
        string changeId = null,
        string projectCode = null,
        string deviceSn = null,
        string status = null,
        DateTime? expectedDateFrom = null,
        DateTime? expectedDateTo = null,
        int page = 1,
        int pageSize = 20);
    
    // 获取物料详情
    Task<ApiResponse<MaterialTrackingDetailDto>> GetMaterialDetailAsync(string materialId);
    
    // 获取设备变更和物料情况（一站式查询）
    Task<ApiResponse<DeviceChangeSummaryDto>> GetDeviceChangeSummaryAsync(string deviceSn);
}
```

### 2. 数据同步服务

```csharp
public class DesignChangeSyncService
{
    private readonly IDesignChangeSystemClient _externalClient;
    private readonly IDesignChangeRepository _repository;
    
    // 同步设计变更数据
    public async Task SyncDesignChangesAsync(
        string projectCode = null,
        DateTime? fromDate = null)
    {
        try
        {
            // 从外部系统获取数据
            var changes = await _externalClient.GetDesignChangesAsync(
                projectCode: projectCode,
                fromDate: fromDate);
            
            // 更新本地缓存
            foreach (var change in changes)
            {
                await _repository.UpsertDesignChangeAsync(change);
            }
            
            // 更新同步状态
            await _repository.UpdateSyncStatusAsync("success", null);
        }
        catch (Exception ex)
        {
            // 记录同步错误
            await _repository.UpdateSyncStatusAsync("failed", ex.Message);
            throw;
        }
    }
    
    // 同步物料跟踪数据
    public async Task SyncMaterialTrackingAsync(
        string changeId = null,
        string projectCode = null)
    {
        try
        {
            var materials = await _externalClient.GetMaterialTrackingAsync(
                changeId: changeId,
                projectCode: projectCode);
            
            foreach (var material in materials)
            {
                await _repository.UpsertMaterialTrackingAsync(material);
            }
            
            await _repository.UpdateSyncStatusAsync("success", null);
        }
        catch (Exception ex)
        {
            await _repository.UpdateSyncStatusAsync("failed", ex.Message);
            throw;
        }
    }
}
```

---

## 📡 API接口设计

### 1. 设计变更查询API

```csharp
// 获取设备相关的设计变更
GET /api/design-changes/device/{deviceId}
GET /api/design-changes/device/{deviceSn}

// 获取项目相关的设计变更
GET /api/design-changes/project/{projectId}
GET /api/design-changes/project/{projectCode}

// 获取设计变更详情
GET /api/design-changes/{changeId}

// 搜索设计变更
GET /api/design-changes/search?keyword=xxx&type=software&status=completed

// 同步设计变更数据（管理员）
POST /api/design-changes/sync?projectCode=xxx
```

### 2. 物料跟踪查询API

```csharp
// 获取设备相关的物料
GET /api/materials/device/{deviceId}
GET /api/materials/device/{deviceSn}

// 获取变更相关的物料
GET /api/materials/change/{changeId}

// 获取项目相关的物料
GET /api/materials/project/{projectId}

// 获取待到货物料（现场工程师常用）
GET /api/materials/pending?deviceId=xxx&expectedDateFrom=xxx&expectedDateTo=xxx

// 获取物料详情
GET /api/materials/{materialId}

// 同步物料数据（管理员）
POST /api/materials/sync?changeId=xxx&projectCode=xxx
```

### 3. 综合查询API（现场工程师常用）

```csharp
// 获取设备变更和物料情况（一站式查询）
GET /api/devices/{deviceId}/change-summary
/*
返回：
{
  "deviceId": "xxx",
  "deviceSn": "SN001",
  "recentChanges": [
    {
      "changeId": "DC-2025-001",
      "changeTitle": "传感器升级",
      "changeType": "hardware",
      "changeDate": "2025-12-20",
      "status": "completed",
      "materials": [
        {
          "materialCode": "MAT-001",
          "materialName": "新型传感器",
          "status": "arrived",
          "arrivalDate": "2025-12-18"
        }
      ]
    }
  ],
  "pendingMaterials": [
    {
      "materialCode": "MAT-002",
      "materialName": "控制板",
      "expectedArrivalDate": "2025-12-25",
      "status": "in_transit"
    }
  ],
  "upcomingChanges": [
    {
      "changeId": "DC-2025-002",
      "changeTitle": "软件升级",
      "expectedDate": "2025-12-28",
      "pendingMaterials": 2
    }
  ]
}
*/
```

---

## 🖥️ 前端功能设计

### 1. 设备变更查询页面

**功能**：
- 按设备查询设计变更
- 显示变更列表（变更编号、标题、类型、时间、状态）
- 点击查看变更详情（变更内容、影响范围）
- 显示关联的物料信息

**页面路径**：`/devices/{deviceId}/changes`

### 2. 物料跟踪页面

**功能**：
- 按设备查询物料
- 显示物料列表（物料编号、名称、状态、预计到货时间）
- 筛选待到货物料
- 显示物料详情（物流信息、当前位置）

**页面路径**：`/devices/{deviceId}/materials`

### 3. 工作安排看板（现场工程师专用）

**功能**：
- 显示待处理的变更
- 显示待到货的物料（按预计到货时间排序）
- 显示工作建议（根据物料到货时间）
- 支持筛选和排序

**页面路径**：`/field-engineer/work-plan`

**看板内容**：
```
┌─────────────────────────────────────────────────┐
│ 工作安排看板                                     │
├─────────────────────────────────────────────────┤
│ 今日到货物料 (3)                                 │
│ - MAT-001 传感器 (预计 10:00)                    │
│ - MAT-002 控制板 (预计 14:00)                    │
│ - MAT-003 线缆 (预计 16:00)                      │
├─────────────────────────────────────────────────┤
│ 本周到货物料 (5)                                 │
│ - MAT-004 电机 (12-25)                          │
│ - MAT-005 减速器 (12-26)                        │
├─────────────────────────────────────────────────┤
│ 待处理变更 (2)                                  │
│ - DC-2025-001 传感器升级 (物料已到齐)            │
│ - DC-2025-002 软件升级 (等待物料)                │
└─────────────────────────────────────────────────┘
```

### 4. 移动端快速查询

**功能**：
- 扫码设备二维码，快速查看变更和物料
- 显示关键信息（变更内容、物料状态）
- 支持离线查看（缓存数据）

**页面路径**：`/mobile/devices/{deviceSn}/changes`

---

## 🔄 数据同步机制（待实现）

### 1. 同步策略

**方案选择**：混合模式（定时同步 + 实时查询）

- **定时同步**：每小时同步一次基础数据（减少外部系统压力）
- **实时查询**：查询时如果缓存过期，实时调用外部API（保证数据实时性）

### 2. 数据缓存（可选）

客服系统可以选择缓存数据以提高性能：

- 缓存设计变更列表（1小时过期）
- 缓存物料跟踪列表（1小时过期）
- 缓存设备变更摘要（30分钟过期）

### 3. 实现说明

> **注意**：以下实现代码是预留的，待设计变更系统开发完成后实现。

```csharp
// 后台定时任务（每小时执行一次）- 待实现
public class DesignChangeSyncJob
{
    private readonly IDesignChangeSystemClient _client;
    private readonly IDesignChangeCacheRepository _cacheRepository;
    
    public async Task ExecuteAsync()
    {
        // 同步最近30天的设计变更
        var fromDate = DateTime.UtcNow.AddDays(-30);
        
        // 同步所有活跃项目
        var projects = await _projectRepository.GetActiveProjectsAsync();
        
        foreach (var project in projects)
        {
            try
            {
                // 从设计变更系统获取数据
                var changes = await _client.GetDesignChangesAsync(
                    projectCode: project.ProjectCode,
                    fromDate: fromDate);
                
                // 更新本地缓存
                await _cacheRepository.UpsertDesignChangesAsync(changes.Data);
            }
            catch (Exception ex)
            {
                // 记录同步错误
                _logger.LogError(ex, "同步设计变更失败: {ProjectCode}", project.ProjectCode);
            }
        }
    }
}

// 实时查询（按需）- 待实现
public async Task<DeviceChangeSummary> GetDeviceChangeSummaryAsync(
    Guid deviceId)
{
    var device = await _deviceRepository.GetByIdAsync(deviceId);
    
    // 检查缓存是否过期（超过1小时）
    var cachedData = await _cacheRepository.GetCachedDataAsync(deviceId);
    if (cachedData == null || cachedData.SyncedAt < DateTime.UtcNow.AddHours(-1))
    {
        // 实时查询设计变更系统
        var response = await _designChangeClient.GetDeviceChangeSummaryAsync(
            deviceSn: device.DeviceSn);
        
        if (response.Code == 200)
        {
            // 更新缓存
            await _cacheRepository.UpsertDeviceChangeSummaryAsync(
                deviceId, response.Data);
        }
    }
    
    // 返回数据
    return await _cacheRepository.GetDeviceChangeSummaryAsync(deviceId);
}
```

---

## 📱 移动端集成

### 1. 工单创建时自动关联变更

**功能**：
- 创建工单时，自动检查设备是否有未完成的设计变更
- 提示工程师注意变更可能影响问题诊断
- 显示相关变更信息

### 2. 快速查询入口

**功能**：
- 在设备详情页添加"设计变更"和"物料跟踪"入口
- 支持扫码快速查询
- 支持离线查看（缓存数据）

---

## 🔐 权限控制

### 1. 查询权限

- **现场工程师**：可以查询自己负责的设备/项目的变更和物料
- **项目经理**：可以查询项目下所有设备的变更和物料
- **管理员**：可以查询所有数据，可以手动触发同步

### 2. 数据权限

- 只读权限：所有用户只能查询，不能修改
- 同步权限：只有管理员可以手动触发同步

---

## 📊 统计和分析

### 1. 变更统计

- 按项目统计变更数量
- 按类型统计变更分布
- 按状态统计变更进度

### 2. 物料统计

- 待到货物料数量
- 物料到货及时率
- 物料延误分析

### 3. 工作安排分析

- 根据物料到货时间，推荐工作安排
- 识别可能延误的工作
- 优化工作顺序

---

## 🚀 实施计划

### 当前阶段：接口预留（Sprint 3）

**客服系统需要做的工作**：
1. ✅ 定义接口契约和对接规范（本文档）
2. ✅ 预留数据模型（可选，用于缓存）
3. ⏳ 预留接口客户端接口定义
4. ⏳ 预留API端点（待设计变更系统开发完成后实现）

**设计变更系统需要做的工作**：
1. ⏳ 实现接口契约中定义的所有接口
2. ⏳ 提供认证和授权机制
3. ⏳ 提供接口文档和测试环境

### Phase 1: 接口对接（设计变更系统开发完成后）

1. 实现接口客户端
2. 实现数据同步服务
3. 实现基础查询API
4. 联调测试

### Phase 2: 前端功能（接口对接完成后）

1. 设备变更查询页面
2. 物料跟踪页面
3. 移动端快速查询

### Phase 3: 工作安排看板（前端功能完成后）

1. 工作安排看板
2. 工作建议功能
3. 统计分析

---

## 🔗 相关文档

- [系统架构v2.0](./ARCHITECTURE_V2.md)
- [集成对接层设计](./ARCHITECTURE_OVERVIEW.md#集成对接层)
- [设备管理模块](../.github/issues/sprint-2/023-设备配置快照和差异提示.md)

---

**最后更新**：2025-12-22

