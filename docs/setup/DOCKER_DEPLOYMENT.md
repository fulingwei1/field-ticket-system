# Docker Compose 部署指南

本文档介绍如何使用 Docker Compose 一键部署现场问题反馈系统。

## 📋 前置要求

- Docker Engine 20.10+
- Docker Compose 2.0+
- 至少 4GB 可用内存
- 至少 10GB 可用磁盘空间

## 🚀 快速开始

### 1. 配置环境变量

复制环境变量模板并修改：

```bash
cp env.example .env
```

编辑 `.env` 文件，配置以下必需项：

```bash
# 数据库密码（必须修改）
POSTGRES_PASSWORD=your_secure_password_here

# JWT 密钥（必须修改，至少32个字符）
JWT_SECRET=your_jwt_secret_key_at_least_32_characters_long_please_change_this

# 企业微信配置（必须配置）
WECOM_CORP_ID=your_corp_id
WECOM_AGENT_ID=your_agent_id
WECOM_SECRET=your_secret
WECOM_REDIRECT_URI=https://your-domain.com/api/auth/wecom/callback
WECOM_WEB_BASE_URL=http://localhost:3000

# Redis 密码（可选，建议设置）
REDIS_PASSWORD=your_redis_password
```

### 2. 启动所有服务

```bash
# 生产环境
docker-compose up -d

# 开发环境（支持热重载）
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### 3. 检查服务状态

```bash
# 查看所有服务状态
docker-compose ps

# 查看服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f api
docker-compose logs -f web
```

### 4. 初始化数据库

数据库表结构会在 API 服务启动时自动创建（通过 Entity Framework Core Migrations）。

如果需要手动运行迁移：

```bash
docker-compose exec api dotnet ef database update --project /src/src/FieldTicket.Infrastructure --startup-project /src/src/FieldTicket.Api
```

### 5. 验证部署

- **API 服务**: http://localhost:5000/health
- **Web 管理端**: http://localhost:3000
- **MinIO Console**: http://localhost:9001 (默认用户名/密码: minioadmin/minioadmin)
- **Swagger API 文档**: http://localhost:5000/swagger

## 📦 服务说明

### 服务列表

| 服务 | 端口 | 说明 |
|------|------|------|
| **api** | 5000 | .NET 8 API 服务 |
| **web** | 3000 | React Web 管理端 |
| **postgres** | 5432 | PostgreSQL 16 数据库 |
| **redis** | 6379 | Redis 7 缓存 |
| **minio** | 9000/9001 | MinIO 对象存储（API/Console） |

### 数据持久化

所有数据存储在 Docker 卷中：

- `pgdata` - PostgreSQL 数据
- `redisdata` - Redis 数据
- `miniodata` - MinIO 数据

查看卷：

```bash
docker volume ls | grep fieldticket
```

备份数据：

```bash
# 备份 PostgreSQL
docker-compose exec postgres pg_dump -U app fieldticket > backup.sql

# 备份 Redis
docker-compose exec redis redis-cli SAVE
docker cp fieldticket-redis:/data/dump.rdb ./redis-backup.rdb
```

## 🔧 常用操作

### 停止服务

```bash
docker-compose down
```

### 停止并删除数据卷（⚠️ 危险操作）

```bash
docker-compose down -v
```

### 重启服务

```bash
docker-compose restart api
docker-compose restart web
```

### 查看服务日志

```bash
# 所有服务
docker-compose logs -f

# 特定服务
docker-compose logs -f api
docker-compose logs -f postgres
```

### 进入容器

```bash
# API 容器
docker-compose exec api sh

# PostgreSQL 容器
docker-compose exec postgres psql -U app -d fieldticket

# Redis 容器
docker-compose exec redis redis-cli
```

## 🐛 故障排查

### 服务无法启动

1. **检查端口占用**：
   ```bash
   # 检查端口是否被占用
   lsof -i :5000
   lsof -i :3000
   lsof -i :5432
   ```

2. **检查环境变量**：
   ```bash
   # 验证环境变量是否正确加载
   docker-compose config
   ```

3. **查看服务日志**：
   ```bash
   docker-compose logs api
   ```

### 数据库连接失败

1. **检查 PostgreSQL 是否运行**：
   ```bash
   docker-compose ps postgres
   ```

2. **检查数据库健康状态**：
   ```bash
   docker-compose exec postgres pg_isready -U app
   ```

3. **检查连接字符串**：
   确保 `.env` 中的 `POSTGRES_PASSWORD` 与 `docker-compose.yml` 中的配置一致。

### MinIO 连接失败

1. **检查 MinIO 是否运行**：
   ```bash
   docker-compose ps minio
   ```

2. **访问 MinIO Console**：
   http://localhost:9001

3. **检查存储桶**：
   存储桶会在首次上传附件时自动创建。

### Redis 连接失败

1. **检查 Redis 是否运行**：
   ```bash
   docker-compose ps redis
   ```

2. **测试 Redis 连接**：
   ```bash
   docker-compose exec redis redis-cli ping
   ```

3. **如果设置了密码**：
   ```bash
   docker-compose exec redis redis-cli -a your_password ping
   ```

## 🔒 生产环境建议

### 1. 使用 HTTPS

在生产环境中，建议使用反向代理（如 Nginx）配置 HTTPS：

```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /api {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### 2. 配置防火墙

只开放必要的端口：

```bash
# 允许 HTTP/HTTPS
ufw allow 80/tcp
ufw allow 443/tcp

# 禁止直接访问数据库和 Redis（仅内网访问）
# PostgreSQL 和 Redis 不应暴露到公网
```

### 3. 定期备份

设置定期备份脚本：

```bash
#!/bin/bash
# backup.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="./backups"

mkdir -p $BACKUP_DIR

# 备份 PostgreSQL
docker-compose exec -T postgres pg_dump -U app fieldticket > $BACKUP_DIR/postgres_$DATE.sql

# 备份 Redis
docker-compose exec redis redis-cli SAVE
docker cp fieldticket-redis:/data/dump.rdb $BACKUP_DIR/redis_$DATE.rdb

# 清理旧备份（保留最近7天）
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
find $BACKUP_DIR -name "*.rdb" -mtime +7 -delete
```

### 4. 监控和日志

- 使用 Docker 日志驱动收集日志
- 配置健康检查监控
- 使用 Prometheus + Grafana 监控服务指标

## 📝 环境变量说明

完整的环境变量列表请参考 `env.example` 文件。

### 必需配置

- `POSTGRES_PASSWORD` - PostgreSQL 密码
- `JWT_SECRET` - JWT 密钥（至少32个字符）
- `WECOM_CORP_ID` - 企业微信企业ID
- `WECOM_AGENT_ID` - 企业微信应用ID
- `WECOM_SECRET` - 企业微信应用密钥
- `WECOM_REDIRECT_URI` - 企业微信回调URL
- `WECOM_WEB_BASE_URL` - Web前端基础URL

### 可选配置

- `REDIS_PASSWORD` - Redis 密码（建议设置）
- `MINIO_ROOT_USER` - MinIO 根用户（默认: minioadmin）
- `MINIO_ROOT_PASSWORD` - MinIO 根密码（默认: minioadmin）

## 🔗 相关文档

- [Docker Compose 官方文档](https://docs.docker.com/compose/)
- [项目 README](../README.md)
- [开发指南](../docs/DEVELOPMENT_GUIDE.md)

