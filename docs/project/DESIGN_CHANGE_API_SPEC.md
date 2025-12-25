# 设计变更系统API接口规范

> **版本**：v1.0  
> **日期**：2025-12-22  
> **说明**：本文档定义设计变更系统需要实现的API接口规范，供客服系统调用

---

## 📋 概述

### 接口基础信息

- **Base URL**：`https://design-change-system.example.com/api/v1`
- **认证方式**：Bearer Token
- **数据格式**：JSON
- **字符编码**：UTF-8

### 通用响应格式

**成功响应**：
```json
{
  "code": 200,
  "message": "success",
  "data": { ... }
}
```

**错误响应**：
```json
{
  "code": 400,
  "message": "错误描述",
  "error": {
    "code": "ERROR_CODE",
    "details": "详细错误信息"
  }
}
```

### HTTP状态码

| 状态码 | 说明 |
|--------|------|
| 200 | 成功 |
| 400 | 参数错误 |
| 401 | 未授权 |
| 403 | 无权限 |
| 404 | 资源不存在 |
| 500 | 服务器错误 |

---

## 🔐 认证接口

### 获取访问令牌

```http
POST /api/v1/auth/token
Content-Type: application/json

Request Body:
{
  "clientId": "field-ticket-system",
  "clientSecret": "your-secret-key"
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

**使用方式**：
```http
Authorization: Bearer {accessToken}
```

---

## 📦 设计变更接口

### 1. 获取设计变更列表

```http
GET /api/v1/design-changes
Authorization: Bearer {token}
```

**Query Parameters**：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| projectCode | string | 否 | 项目编号 |
| deviceSn | string | 否 | 设备序列号 |
| fromDate | datetime | 否 | 开始日期（ISO 8601格式） |
| toDate | datetime | 否 | 结束日期（ISO 8601格式） |
| status | string | 否 | 变更状态（draft, approved, in_progress, completed, cancelled） |
| changeType | string | 否 | 变更类型（software, hardware, parameter, assembly） |
| page | int | 否 | 页码（默认1） |
| pageSize | int | 否 | 每页数量（默认20，最大100） |

**Response 200**：
```json
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
    "pageSize": 20,
    "totalPages": 5
  }
}
```

### 2. 获取设计变更详情

```http
GET /api/v1/design-changes/{changeId}
Authorization: Bearer {token}
```

**Path Parameters**：

| 参数 | 类型 | 说明 |
|------|------|------|
| changeId | string | 变更ID |

**Response 200**：
```json
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
    "changeContent": {
      "affectedDevices": ["SN001", "SN002"],
      "affectedProjects": ["PRJ-001"],
      "softwareChanges": [
        {
          "module": "control",
          "versionBefore": "v1.0",
          "versionAfter": "v1.1",
          "changeDetail": "优化控制算法"
        }
      ],
      "hardwareChanges": [
        {
          "component": "sensor",
          "modelBefore": "SEN-001",
          "modelAfter": "SEN-002",
          "changeDetail": "精度提升10%"
        }
      ],
      "parameterChanges": [
        {
          "parameterName": "timeout",
          "valueBefore": "300ms",
          "valueAfter": "500ms",
          "changeDetail": "增加超时时间"
        }
      ]
    },
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

---

## 📦 物料跟踪接口

### 1. 获取物料跟踪列表

```http
GET /api/v1/materials
Authorization: Bearer {token}
```

**Query Parameters**：

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| changeId | string | 否 | 变更ID |
| projectCode | string | 否 | 项目编号 |
| deviceSn | string | 否 | 设备序列号 |
| status | string | 否 | 物料状态（pending, ordered, in_transit, arrived, installed, cancelled） |
| expectedDateFrom | datetime | 否 | 预计到货开始日期 |
| expectedDateTo | datetime | 否 | 预计到货结束日期 |
| page | int | 否 | 页码（默认1） |
| pageSize | int | 否 | 每页数量（默认20，最大100） |

**Response 200**：
```json
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
    "pageSize": 20,
    "totalPages": 3
  }
}
```

### 2. 获取物料详情

```http
GET /api/v1/materials/{materialId}
Authorization: Bearer {token}
```

**Path Parameters**：

| 参数 | 类型 | 说明 |
|------|------|------|
| materialId | string | 物料ID |

**Response 200**：
```json
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

---

## 🔍 综合查询接口

### 获取设备变更和物料情况（一站式查询）

```http
GET /api/v1/devices/{deviceSn}/change-summary
Authorization: Bearer {token}
```

**Path Parameters**：

| 参数 | 类型 | 说明 |
|------|------|------|
| deviceSn | string | 设备序列号 |

**Response 200**：
```json
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

---

## 📝 数据模型定义

### 设计变更状态枚举

| 值 | 说明 |
|----|------|
| draft | 草稿 |
| approved | 已批准 |
| in_progress | 进行中 |
| completed | 已完成 |
| cancelled | 已取消 |

### 变更类型枚举

| 值 | 说明 |
|----|------|
| software | 软件变更 |
| hardware | 硬件变更 |
| parameter | 参数变更 |
| assembly | 装配变更 |

### 物料状态枚举

| 值 | 说明 |
|----|------|
| pending | 待采购 |
| ordered | 已下单 |
| in_transit | 运输中 |
| arrived | 已到货 |
| installed | 已安装 |
| cancelled | 已取消 |

### 物料类型枚举

| 值 | 说明 |
|----|------|
| component | 组件 |
| tool | 工具 |
| spare_part | 备件 |
| consumable | 耗材 |

---

## 🔧 错误码定义

| 错误码 | HTTP状态码 | 说明 |
|--------|-----------|------|
| INVALID_PARAMETER | 400 | 参数错误 |
| UNAUTHORIZED | 401 | 未授权 |
| FORBIDDEN | 403 | 无权限 |
| NOT_FOUND | 404 | 资源不存在 |
| INTERNAL_ERROR | 500 | 服务器错误 |
| SERVICE_UNAVAILABLE | 503 | 服务不可用 |

---

## 📚 接口测试

### 测试环境

- **测试URL**：`https://test-design-change.example.com/api/v1`
- **测试账号**：联系系统管理员获取

### 测试用例

1. **获取设计变更列表**
   ```bash
   curl -X GET "https://test-design-change.example.com/api/v1/design-changes?projectCode=PRJ-001" \
     -H "Authorization: Bearer {token}"
   ```

2. **获取设备变更摘要**
   ```bash
   curl -X GET "https://test-design-change.example.com/api/v1/devices/SN001/change-summary" \
     -H "Authorization: Bearer {token}"
   ```

---

## 📞 联系方式

如有接口相关问题，请联系：
- **技术负责人**：[待填写]
- **接口文档**：[待填写]
- **技术支持**：[待填写]

---

**最后更新**：2025-12-22


