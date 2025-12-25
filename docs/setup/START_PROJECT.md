# 项目启动指南

## 📋 前置检查

### 1. 启动 Docker

**macOS**：
- 如果使用 **Docker Desktop**：打开 Docker Desktop 应用
- 如果使用 **OrbStack**：打开 OrbStack 应用

等待 Docker 启动完成（状态栏显示 Docker 图标）。

### 2. 创建环境变量文件

```bash
# 复制环境变量模板
cp env.example .env

# 编辑 .env 文件，至少修改以下配置：
# - POSTGRES_PASSWORD: 数据库密码（建议修改）
# - JWT_SECRET: JWT 密钥（至少32个字符，建议修改）
# - WECOM_*: 企业微信配置（如果暂时没有，可以先使用占位符）
```

**最小配置示例**（开发环境）：
```bash
POSTGRES_PASSWORD=dev_password_123
JWT_SECRET=dev_jwt_secret_key_at_least_32_characters_long
WECOM_CORP_ID=your_corp_id
WECOM_AGENT_ID=your_agent_id
WECOM_SECRET=your_secret
```

---

## 🚀 启动项目

### 方式一：使用 Docker Compose（推荐）

```bash
# 1. 进入项目目录
cd "/Users/flw/非标自动化客服现场管理系统"

# 2. 启动所有服务（后台运行）
docker-compose up -d

# 3. 查看服务状态
docker-compose ps

# 4. 查看日志（实时）
docker-compose logs -f

# 5. 查看特定服务日志
docker-compose logs -f api      # API 服务日志
docker-compose logs -f web      # Web 服务日志
docker-compose logs -f postgres # 数据库日志
```

### 方式二：使用开发环境配置（支持热重载）

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

---

## ⏱️ 等待服务启动

首次启动需要：
1. 下载 Docker 镜像（约 5-10 分钟）
2. 构建项目镜像（约 3-5 分钟）
3. 初始化数据库（约 1-2 分钟）

**总耗时**：约 10-20 分钟（取决于网络速度）

---

## ✅ 验证服务状态

### 1. 检查所有服务是否运行

```bash
docker-compose ps
```

所有服务的状态应该是 `Up` 或 `healthy`。

### 2. 检查 API 健康状态

```bash
curl http://localhost:5000/health
```

应该返回 `Healthy`。

### 3. 检查 API 文档

打开浏览器访问：http://localhost:5000/swagger

### 4. 检查 Web 管理端

打开浏览器访问：http://localhost:3000

### 5. 检查 MinIO 控制台

打开浏览器访问：http://localhost:9001
- 用户名：`minioadmin`
- 密码：`minioadmin`

---

## 📦 服务访问地址

| 服务 | 地址 | 说明 |
|------|------|------|
| Web 管理端 | http://localhost:3000 | React 前端 |
| API 服务 | http://localhost:5000 | .NET API |
| API 文档 | http://localhost:5000/swagger | Swagger UI |
| API 健康检查 | http://localhost:5000/health | 健康检查端点 |
| MinIO 控制台 | http://localhost:9001 | 对象存储管理 |
| PostgreSQL | localhost:5432 | 数据库 |
| Redis | localhost:6379 | 缓存 |

---

## 🛠️ 常用命令

### 停止服务

```bash
# 停止所有服务（保留容器）
docker-compose stop

# 停止并删除容器
docker-compose down

# 停止并删除容器和数据卷（⚠️ 会删除所有数据）
docker-compose down -v
```

### 重启服务

```bash
# 重启所有服务
docker-compose restart

# 重启特定服务
docker-compose restart api
docker-compose restart web
```

### 查看日志

```bash
# 查看所有服务日志
docker-compose logs -f

# 查看最近 100 行日志
docker-compose logs --tail=100

# 查看特定服务日志
docker-compose logs -f api
```

### 进入容器

```bash
# 进入 API 容器
docker-compose exec api sh

# 进入 PostgreSQL 容器
docker-compose exec postgres psql -U app -d fieldticket
```

---

## 🐛 故障排查

### 问题 1: Docker daemon 未运行

**错误信息**：
```
Cannot connect to the Docker daemon
```

**解决方法**：
1. 打开 Docker Desktop 或 OrbStack
2. 等待 Docker 完全启动
3. 运行 `docker ps` 验证

### 问题 2: 端口被占用

**错误信息**：
```
Bind for 0.0.0.0:5000 failed: port is already allocated
```

**解决方法**：
```bash
# 检查端口占用
lsof -i :5000
lsof -i :3000
lsof -i :5432

# 停止占用端口的进程，或修改 .env 文件中的端口配置
```

### 问题 3: 服务启动失败

**解决方法**：
```bash
# 1. 查看详细日志
docker-compose logs [service_name]

# 2. 检查环境变量配置
docker-compose config

# 3. 重新构建镜像
docker-compose build --no-cache

# 4. 重新启动
docker-compose up -d
```

### 问题 4: 数据库连接失败

**解决方法**：
```bash
# 1. 检查 PostgreSQL 是否运行
docker-compose ps postgres

# 2. 检查数据库日志
docker-compose logs postgres

# 3. 验证数据库连接
docker-compose exec postgres psql -U app -d fieldticket -c "SELECT 1;"
```

### 问题 5: API 服务无法访问

**解决方法**：
```bash
# 1. 检查 API 服务状态
docker-compose ps api

# 2. 查看 API 日志
docker-compose logs api

# 3. 检查依赖服务（PostgreSQL、Redis、MinIO）是否正常
docker-compose ps
```

---

## 📝 下一步

服务启动成功后：

1. **访问 Web 管理端**：http://localhost:3000
2. **查看 API 文档**：http://localhost:5000/swagger
3. **配置企业微信**：在 `.env` 文件中配置企业微信参数（如果需要登录功能）
4. **初始化数据**：如果需要，可以运行数据库迁移或初始化脚本

---

## 💡 提示

- 首次启动需要下载镜像，请耐心等待
- 建议在启动前检查 `.env` 文件配置
- 如果遇到问题，先查看日志：`docker-compose logs -f`
- 开发环境建议使用 `docker-compose.dev.yml` 配置（支持热重载）

---

**最后更新**：2025-12-23


