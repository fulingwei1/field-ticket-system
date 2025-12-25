# 数据同步策略文档

## 概述

本文档描述了系统中冗余字段的数据同步策略，确保数据一致性。

## 背景

在编译错误修复过程中（2025-12-25），我们在 `Ticket` 实体中添加了几个冗余字段以提高查询性能：

- `CustomerName` - 冗余自 `Customer.Name`
- `DeviceSn` - 冗余自 `Device.Sn`
- `DeviceName` - 冗余自 `Device.Name`

**设计决策**: 选择冗余字段而非每次查询时 JOIN，原因：
1. **性能考虑**: 工单查询是高频操作，避免每次都 JOIN Customer 和 Device 表
2. **历史追溯**: 即使客户或设备信息修改，历史工单仍保留创建时的快照
3. **离线场景**: 移动端离线创建工单时可能没有完整的 Customer/Device 对象

## 冗余字段列表

| 实体 | 冗余字段 | 来源 | 同步时机 | 更新策略 |
|------|---------|------|---------|---------|
| Ticket | CustomerName | Customer.Name | 工单创建时 | **不自动更新** (历史快照) |
| Ticket | DeviceSn | Device.Sn | 工单创建时 | **不自动更新** (历史快照) |
| Ticket | DeviceName | Device.Name | 工单创建时 | **不自动更新** (历史快照) |

## 同步策略

### 策略 1: 历史快照模式（当前实现）

**原则**: 冗余字段在创建时填充，之后**不自动更新**，保留历史快照。

**实现位置**: `TicketService.CreateDraftAsync()` 和 `TicketService.SubmitTicketAsync()`

```csharp
// 创建工单时填充冗余字段
var customer = await _dbContext.Customers.FindAsync(request.CustomerId);
var device = await _dbContext.Devices.FindAsync(request.DeviceId);

var ticket = new Ticket
{
    CustomerId = request.CustomerId,
    CustomerName = customer?.Name,  // 创建时快照
    DeviceId = request.DeviceId,
    DeviceSn = device?.Sn,          // 创建时快照
    DeviceName = device?.Name,      // 创建时快照
    // ...
};
```

**优点**:
- ✅ 简单直接，无需额外同步逻辑
- ✅ 历史工单保留创建时的信息，便于追溯
- ✅ 源数据修改不影响已关闭的工单

**缺点**:
- ❌ 未关闭的工单可能显示过时信息
- ❌ 客户改名后，历史工单仍显示旧名称

**适用场景**: 当前系统选择此策略，因为：
1. 工单主要用于故障追溯，历史信息准确性更重要
2. 客户/设备信息修改频率低
3. 即使信息变更，通过 CustomerId/DeviceId 仍可查询最新信息

### 策略 2: 实时同步模式（未实现，备选方案）

**原则**: 源数据变更时，自动更新所有相关工单的冗余字段。

**实现方式（如果需要）**:

```csharp
// CustomerService.UpdateCustomerAsync() 中添加
public async Task UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request)
{
    var customer = await _dbContext.Customers.FindAsync(customerId);
    customer.Name = request.Name;

    // 同步更新所有未关闭工单的冗余字段
    var openTickets = await _dbContext.Tickets
        .Where(t => t.CustomerId == customerId && t.Status != "Closed")
        .ToListAsync();

    foreach (var ticket in openTickets)
    {
        ticket.CustomerName = request.Name;
    }

    await _dbContext.SaveChangesAsync();
}
```

**优点**:
- ✅ 工单始终显示最新信息
- ✅ 数据一致性高

**缺点**:
- ❌ 实现复杂，需在多处添加同步逻辑
- ❌ 性能开销（每次更新客户/设备需批量更新工单）
- ❌ 丢失历史信息（无法知道工单创建时的客户名称）

### 策略 3: 混合模式（未实现，备选方案）

**原则**: 已关闭工单保留快照，未关闭工单实时同步。

**实现**: 结合策略 1 和策略 2，仅同步未关闭的工单。

## 当前状态

**已实施**: 策略 1（历史快照模式）

**待办事项**:
- [ ] 添加数据库触发器或后台任务，检测长期数据不一致（可选）
- [ ] 在 UI 显示工单时，提供"查看最新客户/设备信息"链接
- [ ] 添加管理员工具，手动修正数据不一致（如果必要）

## 查询最佳实践

### ✅ 正确做法 - 使用冗余字段

```csharp
// 工单列表查询 - 直接使用冗余字段
var tickets = await _dbContext.Tickets
    .Where(t => t.Status == "Submitted")
    .Select(t => new TicketListItem
    {
        TicketNo = t.TicketNo,
        CustomerName = t.CustomerName,  // ✅ 使用冗余字段
        DeviceSn = t.DeviceSn,          // ✅ 使用冗余字段
        Status = t.Status
    })
    .ToListAsync();
```

### ⚠️ 需要最新信息 - 使用 JOIN

```csharp
// 工单详情查询 - 需要最新客户/设备信息时使用 JOIN
var ticketDetail = await _dbContext.Tickets
    .Include(t => t.Customer)
    .Include(t => t.Device)
    .Where(t => t.TicketId == ticketId)
    .Select(t => new TicketDetailDto
    {
        TicketNo = t.TicketNo,
        CustomerNameAtCreation = t.CustomerName,    // 创建时快照
        CurrentCustomerName = t.Customer.Name,      // ⚠️ 当前最新名称
        DeviceSnAtCreation = t.DeviceSn,           // 创建时快照
        CurrentDeviceSn = t.Device.Sn,             // ⚠️ 当前最新SN
    })
    .FirstOrDefaultAsync();
```

### ❌ 错误做法 - 期望冗余字段自动更新

```csharp
// ❌ 错误假设
// 不要期望修改 Customer.Name 后，Ticket.CustomerName 会自动更新
var customer = await _dbContext.Customers.FindAsync(customerId);
customer.Name = "新名称";
await _dbContext.SaveChangesAsync();

// ❌ 相关工单的 CustomerName 仍然是旧值！
var tickets = await _dbContext.Tickets
    .Where(t => t.CustomerId == customerId)
    .ToListAsync();
// tickets[0].CustomerName != "新名称"
```

## 数据一致性检查

### 手动检查脚本

```sql
-- 检查工单冗余字段与源表是否一致
SELECT
    t.TicketNo,
    t.CustomerName AS TicketCustomerName,
    c.Name AS ActualCustomerName,
    t.DeviceSn AS TicketDeviceSn,
    d.Sn AS ActualDeviceSn,
    t.Status,
    t.CreatedAt
FROM Tickets t
JOIN Customers c ON t.CustomerId = c.Id
JOIN Devices d ON t.DeviceId = d.Id
WHERE
    t.CustomerName != c.Name
    OR t.DeviceSn != d.Sn
    AND t.Status IN ('Draft', 'Submitted', 'Triage', 'SolutionIssued', 'Verifying')
ORDER BY t.CreatedAt DESC;
```

### 预期结果

**正常情况**:
- 已关闭工单（Closed）: 可以存在不一致（历史快照）
- 未关闭工单: 如果客户/设备信息未变更，应该一致

**异常情况**:
- 如果大量未关闭工单不一致，可能需要调查：
  1. 客户/设备信息是否频繁修改
  2. 是否需要切换到策略 2 或策略 3

## 相关代码位置

| 文件 | 说明 |
|------|------|
| `FieldTicket.Domain/Entities/Ticket.cs:15-19` | 冗余字段定义 |
| `FieldTicket.Infrastructure/Services/TicketService.cs` | 工单创建时填充冗余字段 |
| `FieldTicket.Infrastructure/Services/TicketSearchService.cs:92` | 查询使用冗余字段示例 |

## 修订历史

| 日期 | 版本 | 修订内容 | 作者 |
|------|------|---------|------|
| 2025-12-25 | 1.0 | 初始版本，记录当前快照策略 | Code Review |

## 参考资料

- [编译错误修复指南](./BUILD_FIX_GUIDE.md#2-实体属性访问错误)
- [CHANGELOG.md](../CHANGELOG.md#2-实体属性访问修复-20-处)

---

**维护者**: 开发团队
**最后更新**: 2025-12-25
