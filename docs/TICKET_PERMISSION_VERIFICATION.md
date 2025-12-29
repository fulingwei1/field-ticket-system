# 工单权限验证文档

本文档用于验证工单读取权限的正确性，确保之前修复的问题不再出现。

---

## 🔍 问题历史

### 之前存在的问题
**提交**: `bdcf659`
**问题描述**: 管理员和工程师无法查看所有工单，只能看到自己创建的工单

**根本原因**:
在 `TicketEndpoints.cs` 中，工单列表查询时强制所有用户只能看自己的工单：
```csharp
// 错误的实现（已修复）
CreatedBy = createdBy ?? userId  // 默认只看自己的
```

---

## ✅ 当前实现（已修复）

### 1. 工单列表查询权限

**位置**: `backend/src/FieldTicket.Api/Endpoints/TicketEndpoints.cs` (第 184-228 行)

**逻辑**:
```csharp
var userId = GetUserId(context);
var userRole = GetUserRole(context);

// 根据角色决定是否过滤创建者
Guid? effectiveCreatedBy = createdBy;
if (effectiveCreatedBy == null && userRole == "FieldEngineer")
{
    effectiveCreatedBy = userId; // FieldEngineer 只能看自己的
}
// Admin 和 Engineer 不会自动添加 createdBy 过滤
```

**权限规则**:
- ✅ **Admin**: 可以查看所有工单
- ✅ **Engineer**: 可以查看所有工单
- ✅ **FieldEngineer**: 只能查看自己创建的工单

---

### 2. 单个工单详情查询权限

**位置**: `backend/src/FieldTicket.Infrastructure/Services/TicketService.cs` (第 307-331 行)

**逻辑**:
```csharp
public async Task<TicketDto?> GetTicketAsync(Guid ticketId, Guid? userId = null)
{
    var ticket = await _dbContext.Tickets
        .FirstOrDefaultAsync(t => t.TicketId == ticketId);

    if (ticket == null)
        return null;

    // 权限检查：FieldEngineer 只能看自己创建的工单
    if (userId.HasValue && ticket.CreatedByUserId != userId.Value)
    {
        var user = await _dbContext.Users.FindAsync(userId.Value);
        if (user != null && user.Role == "FieldEngineer")
        {
            return null; // FieldEngineer 不能查看他人的工单
        }
        // 其他角色可以查看所有工单
    }

    return await MapToDtoAsync(ticket);
}
```

**权限规则**:
- ✅ **Admin**: 可以查看任何工单详情
- ✅ **Engineer**: 可以查看任何工单详情
- ✅ **FieldEngineer**: 只能查看自己创建的工单详情

---

### 3. 工单搜索权限

**位置**: `backend/src/FieldTicket.Api/Endpoints/TicketSearchEndpoints.cs`

**修复提交**: `aafb6d3` - 修复工单搜索功能的权限漏洞

**逻辑**:
```csharp
var userId = GetUserId(context);
var userRole = GetUserRole(context);

// FieldEngineer 只搜索自己的工单
if (userRole == "FieldEngineer" && userId.HasValue)
{
    request.CreatedBy = userId.Value;
}
```

**权限规则**:
- ✅ **Admin**: 可以搜索所有工单
- ✅ **Engineer**: 可以搜索所有工单
- ✅ **FieldEngineer**: 只能搜索自己创建的工单

---

## 🧪 验证测试用例

### 测试准备
1. 准备三个不同角色的账号：
   - Admin 账号
   - Engineer 账号
   - FieldEngineer 账号

2. 准备测试数据：
   - 由 Admin 创建 2 个工单
   - 由 Engineer 创建 2 个工单
   - 由 FieldEngineer 创建 2 个工单

### 测试用例 1: 工单列表查询

#### 测试步骤
```bash
# 1. 使用 Admin 账号登录
curl -H "Authorization: Bearer {admin_token}" \
  http://localhost:5000/api/tickets

# 预期结果：返回所有 6 个工单 ✅
```

```bash
# 2. 使用 Engineer 账号登录
curl -H "Authorization: Bearer {engineer_token}" \
  http://localhost:5000/api/tickets

# 预期结果：返回所有 6 个工单 ✅
```

```bash
# 3. 使用 FieldEngineer 账号登录
curl -H "Authorization: Bearer {field_engineer_token}" \
  http://localhost:5000/api/tickets

# 预期结果：只返回该 FieldEngineer 创建的 2 个工单 ✅
```

#### 验证清单
- [ ] Admin 可以看到所有工单（6个）
- [ ] Engineer 可以看到所有工单（6个）
- [ ] FieldEngineer 只能看到自己的工单（2个）

---

### 测试用例 2: 单个工单详情查询

#### 测试步骤
```bash
# 准备：获取一个由 Engineer 创建的工单 ID
TICKET_ID="engineer_ticket_id"

# 1. 使用 Admin 账号查询
curl -H "Authorization: Bearer {admin_token}" \
  http://localhost:5000/api/tickets/${TICKET_ID}

# 预期结果：成功返回工单详情 ✅
```

```bash
# 2. 使用其他 Engineer 账号查询
curl -H "Authorization: Bearer {other_engineer_token}" \
  http://localhost:5000/api/tickets/${TICKET_ID}

# 预期结果：成功返回工单详情 ✅
```

```bash
# 3. 使用 FieldEngineer 账号查询（不是创建者）
curl -H "Authorization: Bearer {field_engineer_token}" \
  http://localhost:5000/api/tickets/${TICKET_ID}

# 预期结果：返回 404 Not Found ✅
```

```bash
# 4. 使用 FieldEngineer 账号查询自己创建的工单
curl -H "Authorization: Bearer {field_engineer_token}" \
  http://localhost:5000/api/tickets/${FIELD_ENGINEER_TICKET_ID}

# 预期结果：成功返回工单详情 ✅
```

#### 验证清单
- [ ] Admin 可以查看任何工单详情
- [ ] Engineer 可以查看任何工单详情
- [ ] FieldEngineer 可以查看自己创建的工单详情
- [ ] FieldEngineer 不能查看他人创建的工单详情（返回 404）

---

### 测试用例 3: 工单搜索

#### 测试步骤
```bash
# 1. 使用 Admin 账号搜索
curl -X POST -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{"keyword": "测试", "domain": "A"}' \
  http://localhost:5000/api/tickets/search

# 预期结果：返回所有匹配的工单（不限创建者） ✅
```

```bash
# 2. 使用 Engineer 账号搜索
curl -X POST -H "Authorization: Bearer {engineer_token}" \
  -H "Content-Type: application/json" \
  -d '{"keyword": "测试", "domain": "A"}' \
  http://localhost:5000/api/tickets/search

# 预期结果：返回所有匹配的工单（不限创建者） ✅
```

```bash
# 3. 使用 FieldEngineer 账号搜索
curl -X POST -H "Authorization: Bearer {field_engineer_token}" \
  -H "Content-Type: application/json" \
  -d '{"keyword": "测试", "domain": "A"}' \
  http://localhost:5000/api/tickets/search

# 预期结果：只返回该 FieldEngineer 创建的匹配工单 ✅
```

#### 验证清单
- [ ] Admin 搜索时不受创建者限制
- [ ] Engineer 搜索时不受创建者限制
- [ ] FieldEngineer 搜索时自动限制为自己创建的工单

---

## 📋 完整权限矩阵

| 操作 | Admin | Engineer | FieldEngineer |
|------|-------|----------|---------------|
| 查看工单列表 | 全部 ✅ | 全部 ✅ | 仅自己 ✅ |
| 查看工单详情 | 全部 ✅ | 全部 ✅ | 仅自己 ✅ |
| 搜索工单 | 全部 ✅ | 全部 ✅ | 仅自己 ✅ |
| 创建工单 | 允许 ✅ | 允许 ✅ | 允许 ✅ |
| 编辑工单 | 全部 ✅ | 全部 ✅ | 仅自己 ✅ |
| 删除工单 | 允许 ✅ | 允许 ✅ | ❌ 禁止 |

---

## 🐛 已知问题和修复历史

### 问题 1: 管理员和工程师无法查看所有工单
**状态**: ✅ 已修复
**提交**: `bdcf659`
**修复日期**: 2025-12-28
**影响版本**: 之前的版本
**修复内容**: 修改工单列表查询逻辑，只对 FieldEngineer 添加 createdBy 过滤

### 问题 2: 搜索权限绕过漏洞
**状态**: ✅ 已修复
**提交**: `aafb6d3`
**修复日期**: 2025-12-29
**严重性**: 🔴 P0 高危
**影响版本**: 之前的版本
**修复内容**: 在搜索端点添加角色检查，FieldEngineer 自动限制为只搜索自己的工单

---

## 🔒 安全建议

### 1. 定期审计
- 每月审计一次用户权限配置
- 检查是否有权限提升或绕过

### 2. 日志监控
- 监控 403/404 错误频率
- 警惕异常的权限访问模式

### 3. 代码审查
- 所有涉及权限的代码必须经过审查
- 禁止硬编码的权限绕过逻辑

### 4. 测试覆盖
- 每次发布前执行完整的权限测试
- 使用自动化测试脚本

---

## 📝 验证脚本

```bash
#!/bin/bash
# ticket_permission_test.sh
# 自动化权限测试脚本

set -e

# 配置
API_URL="http://localhost:5000"
ADMIN_TOKEN="your_admin_token"
ENGINEER_TOKEN="your_engineer_token"
FIELD_ENGINEER_TOKEN="your_field_engineer_token"

echo "开始工单权限测试..."

# 测试 1: Admin 查看工单列表
echo "测试 1: Admin 查看工单列表"
ADMIN_RESULT=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" \
  "$API_URL/api/tickets")
ADMIN_COUNT=$(echo $ADMIN_RESULT | jq '.total')
echo "Admin 可见工单数: $ADMIN_COUNT"

# 测试 2: Engineer 查看工单列表
echo "测试 2: Engineer 查看工单列表"
ENGINEER_RESULT=$(curl -s -H "Authorization: Bearer $ENGINEER_TOKEN" \
  "$API_URL/api/tickets")
ENGINEER_COUNT=$(echo $ENGINEER_RESULT | jq '.total')
echo "Engineer 可见工单数: $ENGINEER_COUNT"

# 测试 3: FieldEngineer 查看工单列表
echo "测试 3: FieldEngineer 查看工单列表"
FIELD_RESULT=$(curl -s -H "Authorization: Bearer $FIELD_ENGINEER_TOKEN" \
  "$API_URL/api/tickets")
FIELD_COUNT=$(echo $FIELD_RESULT | jq '.total')
echo "FieldEngineer 可见工单数: $FIELD_COUNT"

# 验证结果
if [ "$ADMIN_COUNT" -eq "$ENGINEER_COUNT" ]; then
  echo "✅ Admin 和 Engineer 看到相同数量的工单"
else
  echo "❌ Admin 和 Engineer 看到不同数量的工单"
  exit 1
fi

if [ "$FIELD_COUNT" -lt "$ADMIN_COUNT" ]; then
  echo "✅ FieldEngineer 看到的工单少于 Admin（符合预期）"
else
  echo "❌ FieldEngineer 看到的工单数量异常"
  exit 1
fi

echo "所有测试通过！✅"
```

---

## ✅ 验证签名

**验证人**: ___________
**验证日期**: ___________
**验证结果**: [ ] 通过  [ ] 失败

**备注**:
_________________________________________________
_________________________________________________
_________________________________________________

---

**文档版本**: 1.0
**最后更新**: 2025-12-29
**负责人**: 技术团队
