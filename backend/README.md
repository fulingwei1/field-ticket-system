# Field Ticket Backend

现场问题结构化上报系统 - 后端 API

## 构建状态

| 项目 | 状态 | 说明 |
|------|------|------|
| FieldTicket.Domain | ✅ 构建成功 | 0 错误 |
| FieldTicket.Shared | ✅ 构建成功 | 0 错误 |
| FieldTicket.Core | ✅ 构建成功 | 0 错误 |
| FieldTicket.Infrastructure | ✅ 构建成功 | 0 错误，2 警告（安全性） |
| FieldTicket.Api | ⚠️ 需要修复 | 23 个参数顺序警告 |

**最后更新**: 2025-12-25
**最近修复**: 修复了 85 个编译错误，详见 [CHANGELOG.md](../CHANGELOG.md)

## 技术栈

- .NET 8
- ASP.NET Core Minimal API
- Entity Framework Core 8
- PostgreSQL 16
- Redis 7
- JWT Bearer Authentication

## 项目结构

```
backend/
├── src/
│   ├── FieldTicket.Api/          # API 层
│   ├── FieldTicket.Core/         # 领域服务接口
│   ├── FieldTicket.Domain/       # 领域实体
│   ├── FieldTicket.Infrastructure/ # 基础设施层（数据访问、外部服务）
│   └── FieldTicket.Shared/       # 共享模型
└── tests/
    └── FieldTicket.Tests/        # 单元测试
```

## 快速开始

### 1. 配置环境变量

复制 `appsettings.Development.json` 并配置：

- 数据库连接字符串
- Redis 连接字符串
- 企业微信配置（CorpId、AgentId、Secret、RedirectUri）
- JWT Secret（至少32个字符）

### 2. 运行数据库迁移

```bash
cd backend/src/FieldTicket.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../FieldTicket.Api
dotnet ef database update --startup-project ../FieldTicket.Api
```

### 3. 运行项目

```bash
cd backend/src/FieldTicket.Api
dotnet run
```

API 将在 `http://localhost:5000` 启动，Swagger 文档在 `http://localhost:5000/swagger`

## API 端点

### 认证相关

- `GET /api/auth/wecom/login-url` - 获取企业微信授权URL
- `POST /api/auth/wecom/callback` - 处理企业微信OAuth回调
- `POST /api/auth/refresh` - 刷新Token
- `GET /api/auth/me` - 获取当前用户信息（需要认证）

## 开发说明

### Issue #001: 企业微信登录认证

已完成的功能：

✅ 企业微信 OAuth2 配置
✅ 认证服务接口和实现
✅ 用户信息同步（从企业微信到数据库）
✅ JWT Token 生成和验证
✅ Token 刷新机制
✅ API 端点实现
✅ 错误处理和日志记录

### 待完成

- [ ] 单元测试
- [ ] 集成测试
- [ ] 数据库迁移脚本
- [ ] Docker 镜像构建

## 已知问题

### 1. API 参数顺序警告（23个）

**问题**: 多个 API 端点存在可选参数在必需参数之前的情况
**影响**: 不影响功能，但违反 C# 编码规范
**优先级**: 低
**修复**: 需要重新排列参数顺序

### 2. 安全漏洞警告

**问题**: Package `System.IdentityModel.Tokens.Jwt` 7.0.3 存在已知中等严重性漏洞
**影响**: 潜在安全风险
**优先级**: 中
**修复**: 升级到最新安全版本

### 3. 异步方法警告（多处）

**问题**: 部分异步方法缺少 await 操作符
**影响**: 代码质量
**优先级**: 低
**修复**: 添加实际异步操作或移除 async 关键字

## 技术债务跟踪

详细的技术债务和修复历史请查看：
- [CHANGELOG.md](../CHANGELOG.md) - 变更日志
- [项目 Issues](../../.github/issues/) - 待办任务

## 最近变更

### 2025-12-25 - 编译错误全面修复

✅ **完成**: 修复了所有 85 个编译错误
- 类型转换问题（15+ 处）
- 实体属性访问（20+ 处）
- DTO 属性扩展
- MinIO SDK 升级（4.0.0 → 6.0.3）
- 服务依赖注入修正

详情参见 [CHANGELOG.md](../CHANGELOG.md#未发布---2025-12-25)


