# Field Ticket 测试项目

## 项目结构

```
FieldTicket.Tests/
├── Unit/                    # 单元测试
│   └── Services/           # 服务层单元测试
│       ├── TicketServiceTests.cs
│       ├── AttachmentServiceTests.cs
│       ├── TriageServiceTests.cs
│       └── SolutionServiceTests.cs
├── Integration/            # 集成测试
│   ├── ApiEndpointsTests.cs
│   └── TicketWorkflowTests.cs
└── FieldTicket.Tests.csproj
```

## 运行测试

### 运行所有测试

```bash
cd backend/tests/FieldTicket.Tests
dotnet test
```

### 运行单元测试

```bash
dotnet test --filter Category=Unit
```

### 运行集成测试

```bash
dotnet test --filter Category=Integration
```

### 运行特定测试类

```bash
dotnet test --filter FullyQualifiedName~TicketServiceTests
```

### 生成代码覆盖率报告

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 测试框架

- **xUnit**: 测试框架
- **Moq**: Mock 框架
- **FluentAssertions**: 断言库
- **Microsoft.EntityFrameworkCore.InMemory**: 内存数据库
- **Microsoft.AspNetCore.Mvc.Testing**: API 集成测试

## 测试覆盖目标

### Sprint 1 核心功能

- [x] TicketService 单元测试
- [x] AttachmentService 单元测试
- [x] TriageService 单元测试
- [x] SolutionService 单元测试
- [x] 工单工作流集成测试
- [x] API 端点集成测试

### 待添加

- [ ] NotificationRuleService 单元测试
- [ ] VerificationService 单元测试
- [ ] MissingInfoAnalysisService 单元测试
- [ ] WeComContactService 单元测试
- [ ] 认证服务集成测试

## 测试最佳实践

1. **使用内存数据库**: 单元测试使用 InMemory 数据库，避免依赖真实数据库
2. **Mock 外部依赖**: 使用 Moq 模拟外部服务（MinIO、企业微信等）
3. **测试隔离**: 每个测试使用独立的数据库实例
4. **清晰命名**: 测试方法名应该清晰描述测试场景
5. **AAA 模式**: Arrange-Act-Assert 模式组织测试代码

## 持续集成

测试应该在 CI/CD 流程中自动运行，确保代码质量。

