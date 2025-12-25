# 部署说明

## 🚀 快速启动

### 方式一：使用启动脚本（推荐）

```bash
# 给脚本添加执行权限
chmod +x start.sh

# 运行启动脚本
./start.sh
```

### 方式二：手动启动

```bash
# 1. 配置环境变量
cp env.example .env
# 编辑 .env 文件，修改必要的配置

# 2. 启动所有服务
docker-compose up -d

# 3. 查看服务状态
docker-compose ps

# 4. 查看日志
docker-compose logs -f
```

## 📋 前置要求

- Docker 20.10+
- Docker Compose 2.0+
- 至少 4GB 可用内存
- 至少 10GB 可用磁盘空间

## 🔧 配置说明

### 必需配置项

在 `.env` 文件中至少需要配置以下项：

1. **数据库密码**
   ```bash
   POSTGRES_PASSWORD=your_secure_password
   ```

2. **JWT 密钥**（至少32个字符）
   ```bash
   JWT_SECRET=your_jwt_secret_key_at_least_32_characters_long
   ```

3. **企业微信配置**
   ```bash
   WECOM_CORP_ID=your_corp_id
   WECOM_AGENT_ID=your_agent_id
   WECOM_SECRET=your_secret
   ```

### 可选配置项

其他配置项可以使用默认值，或根据实际需求修改。

## 🌐 服务访问地址

启动成功后，可以通过以下地址访问服务：

- **Web 管理端**: http://localhost:3000
- **API 服务**: http://localhost:5000
- **API 文档 (Swagger)**: http://localhost:5000/swagger
- **MinIO 控制台**: http://localhost:9001
  - 默认用户名: `minioadmin`
  - 默认密码: `minioadmin`
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

## 📦 服务说明

| 服务 | 说明 | 端口 |
|------|------|------|
| api | .NET 8 API 服务 | 5000 |
| web | React Web 管理端 | 3000 |
| postgres | PostgreSQL 16 数据库 | 5432 |
| redis | Redis 7 缓存 | 6379 |
| minio | MinIO 对象存储 | 9000/9001 |

## 🔍 常用命令

```bash
# 启动服务
docker-compose up -d

# 停止服务
docker-compose stop

# 停止并删除容器
docker-compose down

# 查看日志
docker-compose logs -f [service_name]

# 重启服务
docker-compose restart [service_name]

# 查看服务状态
docker-compose ps

# 进入容器
docker-compose exec [service_name] sh
```

## 🐛 故障排查

### 服务无法启动

1. 检查端口占用：
   ```bash
   netstat -tuln | grep -E ':(3000|5000|5432|6379|9000|9001)'
   ```

2. 查看服务日志：
   ```bash
   docker-compose logs [service_name]
   ```

3. 检查环境变量：
   ```bash
   docker-compose config
   ```

### 数据库连接失败

确保 PostgreSQL 服务已启动并健康：
```bash
docker-compose ps postgres
docker-compose logs postgres
```

### API 服务无法访问

1. 检查健康状态：
   ```bash
   curl http://localhost:5000/health
   ```

2. 查看 API 日志：
   ```bash
   docker-compose logs -f api
   ```

## 📚 详细文档

更多详细信息请参考：
- [DOCKER_DEPLOYMENT.md](./DOCKER_DEPLOYMENT.md) - 完整部署文档
- [快速启动.md](./快速启动.md) - 本地开发环境启动指南

