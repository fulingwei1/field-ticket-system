# 新功能使用指南

本文档介绍最新版本中添加的新功能及其使用方法。

---

## 📋 目录

1. [批量标记功能](#批量标记功能)
2. [知识沉淀查看](#知识沉淀查看)
3. [异步导入任务](#异步导入任务)
4. [权限改进说明](#权限改进说明)

---

## 1. 批量标记功能

### 功能概述
批量标记功能允许用户为多个工单同时添加标签，用于分类和管理工单。

### 使用场景
- 将相关工单标记为同一类别（如"紧急"、"客户投诉"、"重复问题"）
- 快速筛选和查找特定类型的工单
- 团队协作中标识工单优先级或状态

### API 使用方法

#### 端点
```
POST /api/tickets/batch/tag
```

#### 请求体
```json
{
  "ticketIds": [
    "guid-1",
    "guid-2",
    "guid-3"
  ],
  "tags": [
    "紧急",
    "客户投诉"
  ]
}
```

#### 响应
```json
{
  "success": true,
  "message": "成功标记 3 个工单",
  "totalCount": 3,
  "successCount": 3,
  "failureCount": 0,
  "skippedCount": 0,
  "errors": []
}
```

### 前端集成示例

```typescript
import ticketBatchService from '@/services/ticketBatchService';

// 批量添加标签
const handleBatchTag = async () => {
  const selectedTicketIds = ['guid-1', 'guid-2', 'guid-3'];
  const tags = ['紧急', '客户投诉'];

  try {
    const result = await ticketBatchService.batchTag(selectedTicketIds, tags);

    if (result.success) {
      message.success(result.message);
    } else {
      message.error(result.message);
      // 显示错误详情
      result.errors.forEach(error => {
        console.error(`工单 ${error.ticketNo} 失败: ${error.errorMessage}`);
      });
    }
  } catch (error) {
    message.error('批量标记失败');
  }
};
```

### 注意事项
- ✅ 标签会自动去重（忽略大小写）
- ✅ 如果标签已存在，会被跳过（计入 skippedCount）
- ✅ 所有操作都会记录到操作日志
- ⚠️ 需要相应权限才能批量标记工单

---

## 2. 知识沉淀查看

### 功能概述
当工单关闭时，系统会自动将工单信息沉淀为"现场问题记录"，便于后续分析和知识复用。

### 查看方式

#### 方法 1: 工单详情页
1. 打开一个已关闭的工单
2. 切换到"知识沉淀"标签页
3. 查看自动生成的问题记录

#### 方法 2: API 查询
```
GET /api/field-problems/by-ticket/{ticketId}
```

**响应示例**:
```json
{
  "problemId": "guid",
  "projectName": "项目A",
  "problemSequence": 123,
  "problemCategory": "电气/IO",
  "problemDescription": "设备启动后报警...",
  "priority": "High",
  "status": "Closed",
  "foundDate": "2025-01-01",
  "completedDate": "2025-01-05",
  "processingDays": 4,
  "primaryDepartment": "技术部",
  "primaryResponsible": "张三",
  "solution": "更换继电器",
  "isRepeatProblem": false,
  "relatedHistoryProblemId": null,
  "satisfactionScore": 5
}
```

### 前端集成示例

```typescript
import fieldProblemService from '@/services/fieldProblemService';

const TicketDetail = () => {
  const [fieldProblem, setFieldProblem] = useState(null);

  useEffect(() => {
    const loadProblem = async () => {
      if (ticket.status === 'Closed') {
        const problem = await fieldProblemService.getProblemByTicket(ticketId);
        setFieldProblem(problem);
      }
    };

    loadProblem();
  }, [ticketId, ticket.status]);

  return (
    <>
      {ticket.status === 'Closed' && (
        <TabPane tab="知识沉淀" key="knowledge">
          {fieldProblem ? (
            <ProblemDetails problem={fieldProblem} />
          ) : (
            <Alert message="问题记录生成中..." type="info" />
          )}
        </TabPane>
      )}
    </>
  );
};
```

### 显示信息
- ✅ 问题分类和描述
- ✅ 处理周期和负责人
- ✅ 解决方案详情
- ✅ 是否为重复问题（高亮显示）
- ✅ 关联的历史问题
- ✅ 客户满意度评分

---

## 3. 异步导入任务

### 功能概述
异步导入功能支持大批量数据导入，避免 HTTP 请求超时，并提供实时进度跟踪。

### 支持的导入类型
- 员工批量导入 (EmployeeImport)
- 设备批量导入 (DeviceImport)
- 客户批量导入 (CustomerImport)

### 使用流程

#### 步骤 1: 创建导入任务
```
POST /api/employees/import/excel
```

**响应**:
```json
{
  "taskId": "guid",
  "message": "导入任务已创建，正在后台处理"
}
```

#### 步骤 2: 查询任务状态
```
GET /api/import-tasks/{taskId}
```

**响应**:
```json
{
  "id": "guid",
  "taskType": "EmployeeImport",
  "status": "Processing",
  "statusDisplay": "处理中",
  "totalCount": 100,
  "processedCount": 45,
  "successCount": 43,
  "failedCount": 2,
  "skippedCount": 0,
  "progressPercentage": 45,
  "currentMessage": "正在导入第 45/100 条记录...",
  "fileName": "employees.xlsx",
  "createdAt": "2025-01-01T10:00:00Z",
  "startedAt": "2025-01-01T10:00:05Z"
}
```

#### 步骤 3: 获取任务详情（含结果）
```
GET /api/import-tasks/{taskId}/detail
```

**响应**:
```json
{
  "id": "guid",
  "status": "Completed",
  "totalCount": 100,
  "successCount": 95,
  "failedCount": 3,
  "skippedCount": 2,
  "result": {
    "successList": [...],
    "errorList": [...],
    "summary": "成功导入 95 个员工，失败 3 个，跳过 2 个"
  }
}
```

### 前端轮询示例

```typescript
import importTaskService from '@/services/importTaskService';

const handleImport = async (file: File) => {
  // 1. 上传文件并创建任务
  const { taskId } = await uploadAndCreateTask(file);

  // 2. 轮询任务状态
  try {
    const result = await importTaskService.pollTaskUntilComplete(
      taskId,
      (task) => {
        // 进度回调
        console.log(`进度: ${task.progressPercentage}%`);
        console.log(task.currentMessage);

        // 更新 UI
        setProgress(task.progressPercentage);
        setMessage(task.currentMessage);
      },
      2000 // 每 2 秒轮询一次
    );

    // 3. 处理完成
    if (result.successCount > 0) {
      message.success(`成功导入 ${result.successCount} 条记录`);
    }

    if (result.failedCount > 0) {
      message.warning(`失败 ${result.failedCount} 条记录`);
      showErrorDetails(result.result.errorList);
    }
  } catch (error) {
    message.error('导入失败: ' + error.message);
  }
};
```

### 管理功能

#### 查询任务列表
```
POST /api/import-tasks/query
```

**请求体**:
```json
{
  "page": 1,
  "pageSize": 20,
  "taskType": "EmployeeImport",
  "status": "Completed"
}
```

#### 删除任务
```
DELETE /api/import-tasks/{taskId}
```

#### 清理已完成的旧任务
```
POST /api/import-tasks/cleanup?daysOld=30
```

**响应**:
```json
{
  "deletedCount": 15,
  "message": "成功清理 15 个已完成的旧任务"
}
```

### 权限说明
- 普通用户：只能查看自己创建的任务
- 管理员：可以查看所有任务，可以执行清理操作

---

## 4. 权限改进说明

### 搜索权限修复

#### 修复前
所有用户（包括现场工程师）都可以通过搜索功能查看所有工单。

#### 修复后
- **现场工程师**: 只能搜索自己创建的工单
- **工程师**: 可以搜索所有工单
- **管理员**: 可以搜索所有工单

#### 示例
```typescript
// 现场工程师搜索时，系统会自动添加创建者过滤
const searchAsFieldEngineer = async () => {
  // 即使不指定 createdBy，系统也会自动过滤
  const results = await ticketSearchService.search({
    keyword: "报警",
    domain: "B"
    // createdBy 会被自动设置为当前用户ID
  });

  // 只返回当前用户创建的工单
};

// 工程师/管理员搜索时，不受限制
const searchAsEngineer = async () => {
  const results = await ticketSearchService.search({
    keyword: "报警",
    domain: "B"
    // 不会自动添加 createdBy 过滤
  });

  // 返回所有匹配的工单
};
```

### 验证方法
1. 使用现场工程师账号登录
2. 执行搜索操作
3. 确认只能看到自己创建的工单
4. 切换到工程师/管理员账号
5. 确认可以看到所有工单

---

## 📚 相关文档

- [Pull Request 总结](../PULL_REQUEST_SUMMARY.md) - 详细的技术变更说明
- [数据库迁移指南](../backend/scripts/) - 数据库脚本执行说明
- [API 文档](http://localhost:5000/swagger) - 完整的 API 文档（开发环境）

---

## 🆘 常见问题

### Q1: 批量标记时部分工单失败怎么办？
**A**: 检查返回的 `errors` 数组，其中包含每个失败工单的详细错误信息。常见原因：
- 工单不存在
- 没有权限操作该工单
- 标签格式不正确

### Q2: 为什么工单关闭后没有显示知识沉淀？
**A**: 可能的原因：
- 问题记录正在生成中，请稍后刷新页面
- 工单关闭时触发器未正常执行，检查后台日志
- 数据库迁移未执行，检查 `field_problems` 表是否存在

### Q3: 异步导入任务卡在 Pending 状态？
**A**: 检查：
- 后台服务是否正常运行（`ImportTaskBackgroundService`）
- 查看应用日志是否有错误信息
- 重启应用服务

### Q4: 现场工程师反馈搜索不到其他人的工单？
**A**: 这是预期行为，不是 bug。现场工程师出于安全考虑，只能搜索自己创建的工单。如需查看其他工单，请联系管理员授予工程师或更高权限。

---

## 📞 技术支持

如有问题，请：
1. 查看应用日志: `/var/log/field-ticket/`
2. 检查数据库连接和迁移状态
3. 联系开发团队

**开发团队**: [Your Team Contact]
**文档更新日期**: 2025-12-29
