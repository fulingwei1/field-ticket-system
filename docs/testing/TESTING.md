# 测试指南

本文档介绍如何测试现场问题反馈系统的各个功能模块。

## 📋 测试环境准备

### 1. 启动服务

```bash
# 使用 Docker Compose 启动所有服务
docker-compose up -d

# 或使用启动脚本
./start.sh
```

### 2. 等待服务就绪

```bash
# 检查服务状态
docker-compose ps

# 查看服务日志
docker-compose logs -f
```

## 🧪 测试脚本

### 快速测试

运行完整测试套件：

```bash
chmod +x scripts/test-full.sh
./scripts/test-full.sh
```

### 分项测试

#### 1. 服务健康检查

```bash
chmod +x scripts/test-services.sh
./scripts/test-services.sh
```

检查内容：
- Docker 服务状态
- PostgreSQL 健康状态
- Redis 健康状态
- MinIO 健康状态
- API 服务健康状态
- Web 服务健康状态

#### 2. 数据库测试

```bash
chmod +x scripts/test-database.sh
./scripts/test-database.sh
```

检查内容：
- 数据库连接
- 表结构存在性
- 表记录数统计

#### 3. API 端点测试

```bash
chmod +x scripts/test-api.sh
./scripts/test-api.sh
```

测试内容：
- 健康检查端点
- Swagger 文档
- 认证端点
- 需要认证的端点（401 响应）

## 🔍 手动测试

### 1. 健康检查

```bash
# API 健康检查
curl http://localhost:5000/health

# 预期响应
# {"status":"healthy","timestamp":"2025-12-22T10:00:00Z"}
```

### 2. API 文档

访问 Swagger UI：
```
http://localhost:5000/swagger
```

### 3. 认证测试

#### 获取企业微信登录 URL

```bash
curl http://localhost:5000/api/auth/wecom/login-url

# 预期响应
# {"url":"https://open.weixin.qq.com/...","state":"..."}
```

### 4. 工单创建测试

#### 创建工单草稿（需要认证）

```bash
# 1. 先获取 JWT Token（通过企业微信登录）
# 2. 使用 Token 创建工单

curl -X POST http://localhost:5000/api/tickets \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Idempotency-Key: test-$(date +%s)" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000001",
    "projectId": "00000000-0000-0000-0000-000000000001",
    "deviceId": "00000000-0000-0000-0000-000000000001",
    "domain": "A",
    "stepCode": "S1",
    "symptomTitle": "测试问题",
    "swVersion": "v1.0.0",
    "plcVersion": "v1.0.0",
    "paramVersion": "v1.0.0",
    "factsJson": {}
  }'
```

### 5. 附件上传测试

```bash
# 上传附件（需要认证和工单ID）
curl -X POST http://localhost:5000/api/tickets/{ticketId}/attachments \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/test.jpg" \
  -F "fileType=image"
```

### 6. Web 前端测试

访问 Web 管理端：
```
http://localhost:3000
```

测试流程：
1. 登录页面
2. 创建工单
3. 上传附件
4. 提交工单
5. 查看工单列表

## 📊 测试检查清单

### 后端 API

- [ ] 健康检查端点正常
- [ ] Swagger 文档可访问
- [ ] 认证端点正常
- [ ] 工单创建 API 正常
- [ ] 工单查询 API 正常
- [ ] 附件上传 API 正常
- [ ] 分诊 API 正常
- [ ] 解决方案 API 正常
- [ ] 验证提交 API 正常

### 数据库

- [ ] PostgreSQL 连接正常
- [ ] 所有表结构存在
- [ ] 数据库迁移成功
- [ ] 索引创建成功

### 服务依赖

- [ ] PostgreSQL 服务正常
- [ ] Redis 服务正常
- [ ] MinIO 服务正常
- [ ] 服务间网络通信正常

### Web 前端

- [ ] 页面可以正常访问
- [ ] 登录功能正常
- [ ] 工单创建页面正常
- [ ] 文件上传功能正常
- [ ] 路由跳转正常

## 🐛 常见问题

### 服务无法启动

1. 检查端口占用：
```bash
netstat -tuln | grep -E ':(3000|5000|5432|6379|9000|9001)'
```

2. 查看服务日志：
```bash
docker-compose logs [service_name]
```

### API 返回 500 错误

1. 检查数据库连接：
```bash
docker-compose exec postgres psql -U app -d fieldticket -c "SELECT 1;"
```

2. 检查 Redis 连接：
```bash
docker-compose exec redis redis-cli ping
```

3. 检查 MinIO 连接：
```bash
curl http://localhost:9000/minio/health/live
```

### 数据库表不存在

运行数据库迁移：
```bash
docker-compose exec api dotnet ef database update \
  --project /src/backend/src/FieldTicket.Infrastructure/FieldTicket.Infrastructure.csproj \
  --startup-project /src/backend/src/FieldTicket.Api/FieldTicket.Api.csproj
```

## 📝 测试报告模板

```
测试日期: YYYY-MM-DD
测试人员: [姓名]
测试环境: [开发/测试/生产]

测试结果:
- 服务健康检查: [通过/失败]
- 数据库测试: [通过/失败]
- API 端点测试: [通过/失败]
- Web 前端测试: [通过/失败]

问题记录:
1. [问题描述]
   - 复现步骤: ...
   - 预期结果: ...
   - 实际结果: ...
   - 状态: [待修复/已修复]
```

## 🔗 相关文档

- [DOCKER_DEPLOYMENT.md](./DOCKER_DEPLOYMENT.md) - 部署文档
- [API_DOCUMENTATION.md](./docs/API_DOCUMENTATION.md) - API 文档
- [DEVELOPMENT_GUIDE.md](./docs/DEVELOPMENT_GUIDE.md) - 开发指南

