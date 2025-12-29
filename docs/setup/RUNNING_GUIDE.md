# 项目运行指南

## 快速启动

### 1. 环境准备

确保已安装：
- Docker & Docker Compose v2
- 已配置 `.env` 文件（参考 `env.example``)

### 2. 启动服务

```bash
# 使用项目名称启动（解决目录名包含中文的问题）
docker compose --project-name fieldticket up -d

# 查看服务状态
docker compose --project-name fieldticket ps

# 查看日志
docker compose --project-name fieldticket logs -f
```

### 3. 访问服务

- **API 服务**: http://localhost:5001
- **API 文档 (Swagger)**: http://localhost:5001/swagger
- **健康检查**: http://localhost:5001/health
- **Web 管理端**: http://localhost:3001
- **MinIO 控制台**: http://localhost:9001 (用户名/密码: minioadmin/minioadmin)
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

### 4. 端口说明

由于 macOS 系统可能占用端口 5000 和 3000，项目已配置为使用：
- API 端口: **5001** (原 5000)
- Web 端口: **3001** (原 3000)

如需修改端口，请编辑 `.env` 文件中的 `API_PORT` 和 `WEB_PORT`。

### 5. 常见问题

#### 问题 1: 端口被占用

**错误信息**: `bind: address already in use`

**解决方案**:
1. 检查端口占用: `lsof -i :5001` 或 `lsof -i :3001`
2. 修改 `.env` 文件中的端口配置
3. 重启服务: `docker compose --project-name fieldticket restart`

#### 问题 2: 容器名称冲突

**错误信息**: `Conflict. The container name "/fieldticket-xxx" is already in use`

**解决方案**:
```bash
# 停止并删除旧容器
docker stop fieldticket-postgres fieldticket-redis fieldticket-minio
docker rm fieldticket-postgres fieldticket-redis fieldticket-minio

# 重新启动
docker compose --project-name fieldticket up -d
```

#### 问题 3: Docker Compose 项目名称错误

**错误信息**: `project name must not be empty`

**解决方案**:
使用 `--project-name fieldticket` 参数：
```bash
docker compose --project-name fieldticket up -d
```

#### 问题 4: API 服务启动失败 - 端点名称重复

**错误信息**: `Duplicate endpoint name 'XXX' found`

**解决方案**:
已修复的重复端点名称：
- `CompareVersions` → `CompareJudgementCardVersions` 和 `CompareKnowledgeVersions`
- `GetTicketCommunications` → `GetTicketCommunications` 和 `GetTicketCommunicationsByTemplate`

如果遇到新的重复端点名称，请检查并重命名其中一个。

### 6. 服务管理命令

```bash
# 启动所有服务
docker compose --project-name fieldticket up -d

# 停止所有服务
docker compose --project-name fieldticket down

# 重启服务
docker compose --project-name fieldticket restart

# 查看日志
docker compose --project-name fieldticket logs -f api
docker compose --project-name fieldticket logs -f web

# 重建并启动
docker compose --project-name fieldticket up -d --build

# 查看服务状态
docker compose --project-name fieldticket ps
```

### 7. 数据库初始化

数据库会在首次启动时自动初始化。如果需要手动运行迁移：

```bash
# 进入 API 容器
docker exec -it fieldticket-api bash

# 运行迁移（如果配置了 EF Core Migrations）
dotnet ef database update
```

### 8. 验证服务运行

```bash
# 检查 API 健康状态
curl http://localhost:5001/health

# 检查 Web 服务
curl http://localhost:3001

# 检查数据库连接
docker exec -it fieldticket-postgres psql -U app -d fieldticket -c "SELECT version();"
```

### 9. 开发模式

使用开发环境配置（支持热重载）：

```bash
docker compose --project-name fieldticket -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 10. 生产部署

使用生产环境配置：

```bash
docker compose --project-name fieldticket -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 故障排查

### 查看详细日志

```bash
# 查看所有服务日志
docker compose --project-name fieldticket logs

# 查看特定服务日志
docker compose --project-name fieldticket logs api
docker compose --project-name fieldticket logs web
docker compose --project-name fieldticket logs postgres
```

### 检查容器健康状态

```bash
docker compose --project-name fieldticket ps
```

健康状态应为 `healthy` 或 `Up`。

### 重新构建镜像

如果代码有更新，需要重新构建：

```bash
# 重新构建并启动
docker compose --project-name fieldticket up -d --build

# 仅重新构建 API
docker compose --project-name fieldticket build api
docker compose --project-name fieldticket up -d api
```

## 注意事项

1. **项目名称**: 由于项目目录名包含中文字符，必须使用 `--project-name fieldticket` 参数
2. **端口配置**: 确保 `.env` 文件中的端口配置与系统可用端口一致
3. **环境变量**: 必须配置 `JWT_SECRET`、`POSTGRES_PASSWORD` 等关键环境变量
4. **数据持久化**: 数据存储在 Docker volumes 中，删除容器不会丢失数据，但删除 volumes 会丢失数据

## 更新记录

- 2025-12-28: 修复重复端点名称问题，更新端口配置为 5001/3001








