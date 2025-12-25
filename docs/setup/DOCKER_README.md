# Docker Compose 一键部署指南

本文档介绍如何使用 Docker Compose 一键部署现场问题反馈系统。

## 📋 前置要求

- Docker 20.10+
- Docker Compose 2.0+
- 至少 4GB 可用内存
- 至少 10GB 可用磁盘空间

## 🚀 快速开始

### 1. 配置环境变量

```bash
# 复制环境变量模板
cp env.example .env

# 编辑 .env 文件，修改必要的配置
# 至少需要修改：
# - POSTGRES_PASSWORD: 数据库密码
# - JWT_SECRET: JWT 密钥（至少32个字符）
# - WECOM_CORP_ID, WECOM_AGENT_ID, WECOM_SECRET: 企业微信配置
```

### 2. 启动所有服务

```bash
# 构建并启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 3. 初始化数据库

```bash
# 等待数据库就绪后，运行数据库迁移
docker-compose exec api dotnet ef database update \
  --project /src/src/FieldTicket.Infrastructure/FieldTicket.Infrastructure.csproj \
  --startup-project /src/src/FieldTicket.Api/FieldTicket.Api.csproj
```

### 4. 访问服务

- **Web 管理端**: http://localhost:3000
- **API 服务**: http://localhost:5000
- **API 文档 (Swagger)**: http://localhost:5000/swagger
- **MinIO 控制台**: http://localhost:9001 (用户名/密码: minioadmin/minioadmin)
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

## 📦 服务说明

### 服务列表

| 服务 | 端口 | 说明 |
|------|------|------|
| api | 5000 | .NET 8 API 服务 |
| web | 3000 | React Web 管理端 |
| postgres | 5432 | PostgreSQL 16 数据库 |
| redis | 6379 | Redis 7 缓存 |
| minio | 9000/9001 | MinIO 对象存储 |

### 数据持久化

所有数据存储在 Docker 卷中：

- `pgdata`: PostgreSQL 数据
- `redisdata`: Redis 数据
- `miniodata`: MinIO 数据

即使删除容器，数据也会保留。要完全删除数据：

```bash
docker-compose down -v
```

## 🔧 常用命令

### 启动和停止

```bash
# 启动所有服务
docker-compose up -d

# 停止所有服务
docker-compose stop

# 停止并删除容器
docker-compose down

# 停止并删除容器和数据卷
docker-compose down -v
```

### 查看日志

```bash
# 查看所有服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f api
docker-compose logs -f web
```

### 重启服务

```bash
# 重启所有服务
docker-compose restart

# 重启特定服务
docker-compose restart api
```

### 进入容器

```bash
# 进入 API 容器
docker-compose exec api sh

# 进入 PostgreSQL 容器
docker-compose exec postgres psql -U app -d fieldticket
```

## 🛠️ 开发环境

使用开发环境配置（支持热重载）：

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

## 🔍 健康检查

所有服务都配置了健康检查：

```bash
# 检查服务健康状态
docker-compose ps

# 手动检查 API 健康
curl http://localhost:5000/health
```

## 📝 环境变量说明

### 必需配置

- `POSTGRES_PASSWORD`: PostgreSQL 数据库密码
- `JWT_SECRET`: JWT 密钥（至少32个字符）
- `WECOM_CORP_ID`: 企业微信企业ID
- `WECOM_AGENT_ID`: 企业微信应用ID
- `WECOM_SECRET`: 企业微信应用密钥

### 可选配置

- `POSTGRES_DB`: 数据库名称（默认: fieldticket）
- `POSTGRES_USER`: 数据库用户（默认: app）
- `MINIO_ROOT_USER`: MinIO 用户名（默认: minioadmin）
- `MINIO_ROOT_PASSWORD`: MinIO 密码（默认: minioadmin）

完整配置请参考 `env.example` 文件。

## 🐛 故障排查

### 服务无法启动

1. 检查端口是否被占用：
```bash
# 检查端口占用
netstat -tuln | grep -E ':(3000|5000|5432|6379|9000|9001)'
```

2. 查看服务日志：
```bash
docker-compose logs [service_name]
```

3. 检查环境变量配置：
```bash
docker-compose config
```

### 数据库连接失败

1. 确保 PostgreSQL 服务已启动：
```bash
docker-compose ps postgres
```

2. 检查数据库连接字符串：
```bash
docker-compose exec api env | grep ConnectionStrings
```

### API 服务无法访问

1. 检查 API 服务健康状态：
```bash
curl http://localhost:5000/health
```

2. 查看 API 日志：
```bash
docker-compose logs -f api
```

3. 检查依赖服务（PostgreSQL、Redis、MinIO）是否正常

## 🔒 生产环境建议

1. **修改默认密码**：修改所有默认密码（数据库、MinIO、JWT密钥等）

2. **使用 HTTPS**：配置反向代理（Nginx）和 SSL 证书

3. **数据备份**：定期备份数据库和 MinIO 数据

4. **监控和日志**：配置日志收集和监控系统

5. **资源限制**：为容器设置资源限制（CPU、内存）

6. **网络安全**：使用 Docker 网络隔离，限制端口暴露

## 📚 相关文档

- [Docker Compose 官方文档](https://docs.docker.com/compose/)
- [.NET Docker 镜像](https://hub.docker.com/_/microsoft-dotnet)
- [PostgreSQL Docker 镜像](https://hub.docker.com/_/postgres)
- [Redis Docker 镜像](https://hub.docker.com/_/redis)
- [MinIO Docker 镜像](https://hub.docker.com/r/minio/minio)

## 💡 提示

- 首次启动可能需要几分钟时间下载镜像
- 建议在启动前检查 `.env` 文件配置
- 生产环境建议使用 Docker Swarm 或 Kubernetes 进行编排


