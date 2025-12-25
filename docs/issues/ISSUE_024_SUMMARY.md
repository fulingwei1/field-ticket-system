# Issue #024: 最近变更自动关联 - 实现总结

## ✅ 已完成的工作

### 后端实现

#### 1. 实体类

**文件**：
- `backend/src/FieldTicket.Domain/Entities/DeviceChangeLog.cs` - 设备变更记录实体

**关键字段**：
- `ChangeType`: 变更类型（program_upgrade, param_change, component_replacement, config_change）
- `ChangeDate`: 变更时间
- `ChangeDetail`: 变更详情（JSONB，包含版本变更、换件信息等）
- `ImpactScope`: 影响范围（JSONB，包含影响的步骤、问题域等）

#### 2. 服务接口和实现

**文件**：
- `backend/src/FieldTicket.Core/Services/IRecentChangeAssociationService.cs` - 服务接口
- `backend/src/FieldTicket.Infrastructure/Services/RecentChangeAssociationService.cs` - 服务实现

**核心功能**：
- ✅ 获取工单相关的最近变更（±7天）
- ✅ 计算变更与问题的相关性评分
- ✅ 高相关性变更置顶展示
- ✅ 获取设备的变更历史
- ✅ 记录设备变更

#### 3. 相关性评分算法

**评分维度**：

1. **版本匹配**（高相关性，最高80分）
   - 软件版本匹配：+30分
   - PLC版本匹配：+30分
   - 参数版本匹配：+20分

2. **问题域匹配**（中相关性，最高30分）
   - 影响的问题域匹配：+15分
   - 影响的步骤代码匹配：+15分

3. **时间接近度**（低相关性，最高10分）
   - 1天内：+10分
   - 3天内：+5分

4. **变更类型相关性**（中相关性，最高10分）
   - 程序升级与PLC问题相关：+10分
   - 参数变更与PLC/测试问题相关：+10分
   - 换件与机械/电气问题相关：+10分

**评分范围**：0-100分，只返回评分 > 0 的变更

#### 4. DTO 模型

**文件**：`backend/src/FieldTicket.Core/Services/IRecentChangeAssociationService.cs`

**包含模型**：
- `RelatedChangeDto` - 相关变更DTO（包含相关性评分和原因）
- `DeviceChangeLogDto` - 设备变更记录DTO
- `CreateDeviceChangeRequest` - 创建设备变更请求

#### 5. API 端点

**文件**：`backend/src/FieldTicket.Api/Endpoints/RecentChangeEndpoints.cs`

**端点列表**：
- `GET /api/recent-changes/tickets/{ticketId}` - 获取工单相关的最近变更
- `GET /api/recent-changes/devices/{deviceId}` - 获取设备的变更历史
- `POST /api/recent-changes/devices` - 记录设备变更

**集成到工单详情**：
- `GET /api/tickets/{id}` - 已扩展，返回工单详情和相关变更（如果工单已提交）

#### 6. 数据库配置

**文件**：`backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs`

**已配置**：
- ✅ DeviceChangeLog 实体配置（表名、字段映射、索引）
- ✅ 索引：DeviceId、ChangeDate、复合索引（DeviceId, ChangeDate）

#### 7. 服务注册

**文件**：`backend/src/FieldTicket.Api/Program.cs`

**已注册**：
- ✅ IRecentChangeAssociationService → RecentChangeAssociationService
- ✅ RecentChangeEndpoints

## 📝 技术细节

### 变更类型

支持以下变更类型：
- `program_upgrade`: 程序升级
- `param_change`: 参数变更
- `component_replacement`: 换件记录
- `config_change`: 配置变更

### 变更详情结构

```json
{
  "sw_version": { "old": "v1.2.3", "new": "v1.3.0" },
  "plc_version": { "old": "v2.0.1", "new": "v2.0.2" },
  "param_version": { "old": "v1.0", "new": "v1.1" },
  "component": { "type": "sensor", "old": "A001", "new": "A002" },
  "description": "升级软件版本修复超时问题"
}
```

### 影响范围结构

```json
{
  "affected_steps": ["Step_120", "Step_150"],
  "affected_domains": ["A", "B"],
  "risk_level": "medium"
}
```

### 相关性计算逻辑

1. **时间范围**：默认查找工单提交时间前后7天内的变更
2. **版本匹配**：检查变更详情中的版本信息是否与工单版本匹配
3. **问题域匹配**：检查影响范围是否包含工单的问题域和步骤
4. **变更类型匹配**：根据变更类型和工单问题域判断相关性
5. **排序**：按相关性评分降序，评分相同按变更时间降序

## ✅ 验收标准

- [x] 自动查找±7天内的变更
- [x] 相关性评分准确
- [x] 高相关性变更置顶
- [x] 变更详情清晰展示
- [ ] 变更历史可视化（前端待实现）
- [ ] 变更记录功能（需要集成到设备管理流程）

## ⚠️ 待完成

### 前端

- [x] 工单详情页面显示相关变更 ✅ 已完成
- [x] 相关变更列表展示（按相关性排序） ✅ 已完成
- [x] 相关性评分和原因展示 ✅ 已完成
- [x] 变更详情展示 ✅ 已完成
- [ ] 变更历史可视化（可选功能）

### 功能增强

- [ ] 设备变更记录功能（集成到设备管理流程）
- [ ] 变更记录自动创建（从设备版本更新、参数变更等触发）
- [ ] 变更影响范围自动分析
- [ ] 变更与工单的关联统计

## 🔗 相关文件

### 实体
- `backend/src/FieldTicket.Domain/Entities/DeviceChangeLog.cs`

### 服务
- `backend/src/FieldTicket.Core/Services/IRecentChangeAssociationService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/RecentChangeAssociationService.cs`

### API
- `backend/src/FieldTicket.Api/Endpoints/RecentChangeEndpoints.cs`
- `backend/src/FieldTicket.Api/Endpoints/TicketEndpoints.cs`（已扩展）

### 配置
- `backend/src/FieldTicket.Infrastructure/Data/ApplicationDbContext.cs`
- `backend/src/FieldTicket.Api/Program.cs`

---

## ✅ 前端实现（2025-12-24）

### 1. 前端服务层

**文件**：`web-admin/src/services/recentChangeService.ts`

**功能**：
- ✅ `getRelatedChanges` - 获取工单相关的最近变更
- ✅ `getDeviceChanges` - 获取设备的变更历史
- ✅ `recordChange` - 记录设备变更

### 2. 最近变更展示组件

**文件**：`web-admin/src/components/tickets/RecentChanges.tsx`

**功能特性**：
- ✅ 自动加载工单相关的最近变更（±7天）
- ✅ 按相关性评分排序展示
- ✅ 变更类型图标和标签（程序升级、参数变更、换件、配置变更）
- ✅ 相关性评分可视化（颜色编码：高/中/低）
- ✅ 相关性原因展示（Alert组件）
- ✅ 变更详情展示（版本变更、换件信息等）
- ✅ 影响范围展示（问题域、步骤、风险等级）
- ✅ 变更时间展示
- ✅ 记录人信息展示
- ✅ 空状态和错误处理

### 3. 集成到工单详情页面

**文件**：`web-admin/src/pages/tickets/TicketDetail.tsx`

**集成内容**：
- ✅ 添加"最近变更"标签页
- ✅ 仅在工单已提交时显示（非草稿状态）
- ✅ 自动加载相关变更数据

### 4. 功能特性

**变更类型识别**：
- 程序升级（蓝色，CodeOutlined）
- 参数变更（橙色，SettingOutlined）
- 换件记录（绿色，ToolOutlined）
- 配置变更（紫色，ConfigOutlined）

**相关性评分可视化**：
- 70分以上：红色（高相关性）
- 50-69分：橙色（中高相关性）
- 30-49分：蓝色（中相关性）
- 30分以下：灰色（低相关性）

**变更详情展示**：
- 版本变更（软件版本、PLC版本、参数版本）
- 换件信息（类型、旧件、新件）
- 变更描述
- 影响范围（问题域、步骤、风险等级）

---

**状态**: ✅ 后端和前端核心功能实现完成，待变更记录功能集成到设备管理流程

