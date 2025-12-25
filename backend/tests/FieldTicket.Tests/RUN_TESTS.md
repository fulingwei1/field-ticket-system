# 运行测试指南

## 当前状态

✅ **测试代码已创建并修复**
- 所有测试文件已创建
- 修复了构造函数参数错误
- 修复了类型引用错误
- 代码已通过静态检查（lint）

⚠️ **测试尚未实际运行**
- 由于环境限制，无法在当前环境运行测试
- 需要在本地环境运行测试进行验证

## 运行测试步骤

### 1. 检查环境

确保已安装：
- .NET 8 SDK
- 可以访问项目目录

### 2. 进入测试目录

```bash
cd backend/tests/FieldTicket.Tests
```

### 3. 恢复依赖

```bash
dotnet restore
```

### 4. 构建测试项目

```bash
dotnet build
```

如果构建成功，说明测试代码没有编译错误。

### 5. 运行测试

```bash
# 运行所有测试
dotnet test

# 运行单元测试
dotnet test --filter Category=Unit

# 运行集成测试
dotnet test --filter Category=Integration

# 运行特定测试类
dotnet test --filter FullyQualifiedName~TicketServiceTests

# 生成代码覆盖率
dotnet test --collect:"XPlat Code Coverage"
```

### 6. 使用测试脚本

```bash
chmod +x run-tests.sh
./run-tests.sh
```

## 预期结果

### 成功情况

如果测试代码正确，应该看到：

```
Test Run Successful.
Total tests: 33
     Passed: 33
     Failed: 0
 Total time: X.XXX seconds
```

### 可能的问题

#### 1. 编译错误

**症状**：`dotnet build` 失败

**可能原因**：
- 缺少 NuGet 包
- 命名空间引用错误
- 类型不匹配

**解决方案**：
- 检查 `FieldTicket.Tests.csproj` 中的包引用
- 检查 using 语句
- 检查服务构造函数的参数

#### 2. 测试失败

**症状**：`dotnet test` 显示失败的测试

**可能原因**：
- 业务逻辑实现与测试预期不符
- Mock 对象配置不正确
- 测试数据设置错误

**解决方案**：
- 检查测试断言是否正确
- 检查 Mock 对象的行为
- 检查测试数据设置
- 查看详细的错误信息

#### 3. 依赖问题

**症状**：找不到类型或命名空间

**可能原因**：
- 项目引用不正确
- 命名空间变更

**解决方案**：
- 检查项目引用
- 检查命名空间

## 测试代码修复记录

### 已修复的问题

1. ✅ **TicketServiceTests**: 修复了 `TicketNumberService` 构造函数参数
   - 从：`new TicketNumberService(_dbContext, _loggerMock.Object)`
   - 改为：`new TicketNumberService(_cacheMock.Object)`

2. ✅ **SolutionServiceTests**: 修复了 `SolutionNumberService` 构造函数参数
   - 从：`new SolutionNumberService(_dbContext, _loggerMock.Object)`
   - 改为：`new SolutionNumberService(cacheMock.Object)`

3. ✅ **TicketWorkflowTests**: 修复了重复变量声明
   - 移除了重复的 `cacheMock` 声明

4. ✅ **TicketWorkflowTests**: 修复了 `SubmitVerificationRequest` 的使用
   - 从：`ChecklistResults` (不存在的属性)
   - 改为：`ChecklistResultJson` (正确的属性)

5. ✅ **NotificationRuleServiceTests**: 修复了构造函数参数
   - 添加了 `contactService: null` 参数

## 测试覆盖情况

### 单元测试（29+ 个测试用例）

- ✅ TicketService: 8+ 个测试
- ✅ AttachmentService: 6+ 个测试
- ✅ TriageService: 4+ 个测试
- ✅ SolutionService: 4+ 个测试
- ✅ NotificationRuleService: 4+ 个测试
- ✅ MissingInfoAnalysisService: 3+ 个测试

### 集成测试（4+ 个测试用例）

- ✅ API 端点测试: 3+ 个测试
- ✅ 工单工作流测试: 1+ 个测试

## 下一步

1. **在本地运行测试**：验证测试代码是否正确
2. **修复测试失败**：根据测试结果修复任何问题
3. **提高覆盖率**：添加更多测试用例
4. **CI/CD 集成**：将测试集成到持续集成流程

## 验证清单

运行测试前，请确认：

- [ ] .NET 8 SDK 已安装
- [ ] 可以访问项目目录
- [ ] 所有项目引用正确
- [ ] NuGet 包可以正常下载

运行测试后，请确认：

- [ ] 所有测试通过
- [ ] 没有编译错误
- [ ] 测试执行时间合理
- [ ] 代码覆盖率报告生成成功

---

**最后更新**: 2025-12-23  
**状态**: 测试代码已修复，等待实际运行验证

