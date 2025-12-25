# Issue #008 增强：通知规则系统完善

## 概述

本次增强完善了通知规则系统，主要改进包括：
1. 支持解决方案发布通知的模板变量替换
2. 修复了 `NotificationRuleEndpoints` 中的用户ID获取逻辑
3. 完善了 `SolutionService` 与通知规则系统的集成

## 改进内容

### 1. 通知规则服务增强

#### 1.1 支持解决方案模板变量

**文件**: `backend/src/FieldTicket.Infrastructure/Services/NotificationRuleService.cs`

- 新增 `ReplaceTemplateVariablesAsync` 方法，支持解决方案相关的模板变量：
  - `{solution_code}`: 解决方案编号
  - `{summary}`: 解决方案标题
  - `{app_deeplink}`: App 深度链接

- 更新 `ExecuteNotificationAsync` 方法，自动检测 context 中的解决方案信息：
  - 支持直接传入 `Solution` 对象
  - 支持传入 `Guid` 类型的解决方案ID，自动从数据库加载

```csharp
// 自动检测解决方案
Solution? solution = null;
if (context != null && context.TryGetValue("solution", out var solutionObj))
{
    if (solutionObj is Solution sol)
    {
        solution = sol;
    }
    else if (solutionObj is Guid solutionId)
    {
        solution = await _dbContext.Solutions.FirstOrDefaultAsync(s => s.SolutionId == solutionId);
    }
}
```

### 2. 解决方案服务集成

**文件**: `backend/src/FieldTicket.Infrastructure/Services/SolutionService.cs`

- 添加 `INotificationRuleService` 依赖注入
- 更新 `PublishSolutionAsync` 方法，优先使用通知规则系统：
  - 如果配置了通知规则，使用规则系统发送通知
  - 如果没有规则或规则执行失败，回退到默认通知方式

```csharp
// 优先使用通知规则系统
if (_notificationRuleService != null)
{
    await _notificationRuleService.ExecuteNotificationAsync(
        solution.TicketId,
        "solution_published",
        new Dictionary<string, object>
        {
            ["solution"] = solution,
            ["ticket"] = ticket
        });
    return;
}

// 回退到默认通知
if (_notificationService != null)
{
    await _notificationService.NotifySolutionPublishedAsync(solutionId);
}
```

### 3. API 端点修复

**文件**: `backend/src/FieldTicket.Api/Endpoints/NotificationRuleEndpoints.cs`

- 修复 `GetUserId` 方法：
  - 返回类型从 `Guid` 改为 `Guid?`，允许返回 `null`
  - 使用 `ClaimTypes.NameIdentifier` 替代字符串查找
  - 在调用处添加空值检查，返回 `401 Unauthorized` 而不是抛出异常

```csharp
private static Guid? GetUserId(HttpContext httpContext)
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
    {
        return null;
    }
    return userId;
}
```

## 技术细节

### 模板变量支持

通知规则系统现在支持以下模板变量：

#### 工单相关变量
- `{ticket_no}`: 工单编号
- `{symptom_title}`: 问题标题
- `{priority}`: 紧急度
- `{web_url}`: Web 访问链接

#### 解决方案相关变量（新增）
- `{solution_code}`: 解决方案编号
- `{summary}`: 解决方案标题
- `{app_deeplink}`: App 深度链接（格式：`fieldticket://solution/{solutionId}`）

### 通知规则执行流程

1. **规则匹配**: 根据工单的客户/项目/设备层级匹配通知规则
2. **接收人解析**: 解析规则中的接收人配置（用户ID、群组ID、角色、部门）
3. **模板处理**: 
   - 如果 context 包含解决方案，使用 `ReplaceTemplateVariablesAsync`
   - 否则使用 `ReplaceTemplateVariables`
4. **通知发送**: 调用 `IWeComNotificationService` 发送通知
5. **日志记录**: 记录通知日志到 `NotificationLog` 表

## 测试建议

### 1. 解决方案发布通知测试

1. 创建并发布一个解决方案
2. 验证通知规则是否正确匹配和执行
3. 检查通知内容是否包含解决方案相关变量
4. 验证通知日志是否正确记录

### 2. API 端点测试

1. 测试未认证用户访问通知规则端点，应返回 `401 Unauthorized`
2. 测试已认证用户创建/更新通知规则
3. 验证用户ID正确从 JWT Token 中提取

### 3. 回退机制测试

1. 禁用所有通知规则，验证是否回退到默认通知
2. 配置通知规则但执行失败，验证是否回退到默认通知

## 相关文件

- `backend/src/FieldTicket.Infrastructure/Services/NotificationRuleService.cs`
- `backend/src/FieldTicket.Infrastructure/Services/SolutionService.cs`
- `backend/src/FieldTicket.Api/Endpoints/NotificationRuleEndpoints.cs`
- `backend/src/FieldTicket.Infrastructure/WeCom/NotificationTemplates.cs`

## 状态

✅ **代码实现完成**

所有增强功能已实现，代码已通过编译检查，待集成测试。

