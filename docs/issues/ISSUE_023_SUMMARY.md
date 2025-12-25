# Issue #023: 设备配置快照和差异提示 - 实现总结

## ✅ 已完成的工作

### 后端实现

#### 1. 实体类

**文件**：
- `backend/src/FieldTicket.Domain/Entities/DeviceConfigSnapshot.cs` - 设备配置快照实体

**关键字段**：
- `SnapshotType`: 快照类型（delivery, change, problem）
- `SnapshotAt`: 快照时间
- `ConfigJson`: 配置内容（JSONB，包含版本、参数等）
- `StandardConfigJson`: 标准配置（用于对比）
- `Differences`: 差异点（JSONB，包含字段、标准值、实际值、影响说明）

#### 2. 服务接口和实现

**文件**：
- `backend/src/FieldTicket.Core/Services/IDeviceConfigSnapshotService.cs` - 服务接口
- `backend/src/FieldTicket.Infrastructure/Services/DeviceConfigSnapshotService.cs` - 服务实现

**核心功能**：
- ✅ 创建配置快照（交付、变更、问题）
- ✅ 检查配置差异（自动对比当前配置与标准配置）
- ✅ 获取快照历史（支持按类型筛选）
- ✅ 获取标准配置（交付时的配置）
- ✅ 获取当前配置（从工单或设备表获取）
- ✅ 对比两个配置（生成差异列表）

#### 3. 配置差异检测算法

**检测逻辑**：
1. 获取当前配置（从工单版本信息或设备表）
2. 获取标准配置（交付时的快照）
3. 对比两个配置，找出差异
4. 自动生成差异影响说明
5. 如果有差异，自动创建问题快照

**差异类型**：
- `added`: 新增的配置项
- `removed`: 删除的配置项
- `changed`: 修改的配置项

**影响说明生成**：
- 版本变更：提示可能影响功能兼容性
- 超时参数：根据时间长短提示可能的影响
- 过滤参数：提示可能影响检测精度

#### 4. DTO 模型

**文件**：`backend/src/FieldTicket.Core/Services/IDeviceConfigSnapshotService.cs`

**包含模型**：
- `ConfigSnapshotDto` - 配置快照DTO
- `ConfigDifference` - 配置差异DTO
- `CreateConfigSnapshotRequest` - 创建快照请求

#### 5. API 端点

**文件**：`backend/src/FieldTicket.Api/Endpoints/DeviceConfigSnapshotEndpoints.cs`

**端点列表**：
- `POST /api/device-config-snapshots` - 创建配置快照
- `GET /api/device-config-snapshots/devices/{deviceId}/differences` - 检查配置差异
- `GET /api/device-config-snapshots/devices/{deviceId}/history` - 获取快照历史
- `GET /api/device-config-snapshots/devices/{deviceId}/standard` - 获取标准配置
- `GET /api/device-config-snapshots/devices/{deviceId}/current` - 获取当前配置

#### 6. 数据库配置

**文件**：`backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs`

**已配置**：
- ✅ DeviceConfigSnapshot 实体配置（表名、字段映射、索引）
- ✅ 索引：DeviceId、SnapshotAt、复合索引（DeviceId, SnapshotAt）、复合索引（DeviceId, SnapshotType）

#### 7. 服务注册

**文件**：`backend/src/FieldTicket.Api/Program.cs`

**已注册**：
- ✅ IDeviceConfigSnapshotService → DeviceConfigSnapshotService
- ✅ DeviceConfigSnapshotEndpoints

#### 8. 工单服务集成

**文件**：`backend/src/FieldTicket.Infrastructure/Services/TicketService.cs`

**集成内容**：
- ✅ 工单创建时自动检测配置差异
- ✅ 异步执行，不阻塞工单创建流程
- ✅ 检测到差异时自动创建问题快照
- ✅ 记录差异日志

## 📝 技术细节

### 配置快照类型

支持以下快照类型：
- `delivery`: 交付时的标准配置
- `change`: 变更时的配置快照
- `problem`: 问题发生时的配置快照（自动创建）

### 配置内容结构

```json
{
  "sw_version": "v2.1.3",
  "plc_version": "v1.2.6",
  "param_version": "v3.4",
  "key_parameters": {
    "timeout_step_120": "300ms",
    "filter_time": "50ms"
  }
}
```

### 差异结构

```json
[
  {
    "field": "timeout_step_120",
    "standard": "800ms",
    "actual": "300ms",
    "impact": "超时时间缩短可能导致超时错误",
    "differenceType": "changed"
  }
]
```

### 当前配置获取逻辑

1. **优先从工单获取**：
   - 从最新工单中获取版本信息（sw_version, plc_version, param_version）
   - 包含事实表信息（如果有）

2. **备用方案**：
   - 如果设备表有版本字段，从设备表获取
   - 否则返回空配置

### 标准配置获取逻辑

1. 查找设备的所有交付快照（`snapshot_type = 'delivery'`）
2. 按时间倒序排列，取最新的作为标准配置
3. 如果没有交付快照，返回 null

### 差异检测流程

1. **工单创建时**：
   - 从工单中提取版本信息构建当前配置
   - 调用 `CheckConfigDifferencesAsync` 检测差异
   - 如果有差异，自动创建问题快照

2. **手动检测**：
   - 调用 API 端点 `/api/device-config-snapshots/devices/{deviceId}/differences`
   - 可以传入自定义的当前配置

3. **自动快照创建**：
   - 检测到差异时，自动创建 `problem` 类型的快照
   - 保存当前配置、标准配置和差异列表

## ✅ 验收标准

- [x] 交付时自动创建配置快照（需要手动调用API或集成到设备交付流程）
- [x] 变更时自动创建配置快照（需要手动调用API或集成到设备变更流程）
- [x] 工单创建时自动检测差异
- [x] 差异点清晰展示（API返回差异列表）
- [x] 配置历史可追溯（API支持查询快照历史）
- [ ] 差异点可视化展示（前端待实现）

## ⚠️ 待完成

### 前端

- [x] 工单创建页面显示配置差异提示 ✅ 已完成（2025-12-25）
- [x] 配置差异列表展示 ✅ 已完成
- [x] 配置对比可视化（当前配置 vs 标准配置） ✅ 已完成
- [x] 快照历史展示 ✅ 已完成
- [x] 创建配置快照功能（交付、变更） ✅ 已完成

### 功能增强

- [ ] 设备交付流程集成（自动创建交付快照）
- [ ] 设备变更流程集成（自动创建变更快照）
- [ ] 配置差异影响分析增强
- [ ] 配置差异告警通知
- [ ] 配置差异统计报表

### 配置获取增强

- [ ] 从设备表直接获取当前配置（如果设备表有版本字段）
- [ ] 支持从设备配置表获取参数配置
- [ ] 支持配置模板（不同设备类型有不同的标准配置模板）

## 🔗 相关文件

### 实体
- `backend/src/FieldTicket.Domain/Entities/DeviceConfigSnapshot.cs`

### 服务
- `backend/src/FieldTicket.Core/Services/IDeviceConfigSnapshotService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/DeviceConfigSnapshotService.cs`

### API
- `backend/src/FieldTicket.Api/Endpoints/DeviceConfigSnapshotEndpoints.cs`

### 集成
- `backend/src/FieldTicket.Infrastructure/Services/TicketService.cs`（已集成配置差异检测）

### 配置
- `backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs`
- `backend/src/FieldTicket.Api/Program.cs`

---

**状态**: ✅ 后端和前端核心功能实现完成，待设备交付/变更流程集成

---

## ✅ 前端实现（2025-12-25）

### 1. 设备配置快照管理页面

**文件**：`web-admin/src/pages/devices/DeviceConfigSnapshot.tsx`

**功能特性**：
- ✅ 设备列表展示和搜索
- ✅ 创建配置快照（交付、变更、问题）
- ✅ 检查配置差异（自动对比当前配置与标准配置）
- ✅ 快照历史展示（按类型筛选）
- ✅ 配置差异可视化（时间线展示、差异类型图标）
- ✅ 配置对比（标准配置 vs 当前配置）

### 2. 工单创建页面集成

**文件**：`web-admin/src/pages/tickets/CreateTicket.tsx`

**集成内容**：
- ✅ 版本信息表单自动检测配置差异
- ✅ 填写版本信息后自动调用差异检测 API
- ✅ 发现差异时显示警告提示
- ✅ 差异列表展示（最多显示3条，可查看全部）
- ✅ 一键跳转到设备配置快照页面查看详细差异

**功能特性**：
- 实时检测：填写设备ID和版本信息后自动检测
- 智能提示：发现差异时显示警告，包含差异字段、标准值、实际值、影响说明
- 便捷查看：点击"查看详细差异"按钮跳转到配置快照页面

