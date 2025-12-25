# 🚀 快速启动指南

## ⚠️ 当前问题

根据检查，发现以下问题：
1. **Docker daemon 未运行** - 需要先启动 Docker Desktop 或 OrbStack
2. **.env 文件缺失** - 需要创建环境变量配置文件

## 📋 解决步骤

### 步骤 1: 启动 Docker

**macOS 用户**：
- 打开 **Docker Desktop** 或 **OrbStack** 应用
- 等待 Docker 完全启动（状态栏显示 Docker 图标）
- 验证：在终端运行 `docker ps`，应该能看到容器列表或空列表（而不是错误）

### 步骤 2: 创建 .env 文件

在项目根目录执行：

```bash
cd "/Users/flw/非标自动化客服现场管理系统"
cp env.example .env
```

然后编辑 `.env` 文件，至少修改以下配置：

```bash
# 数据库密码（必须修改）
POSTGRES_PASSWORD=fieldticket_dev_2025

# JWT 密钥（必须修改，至少32个字符）
JWT_SECRET=fieldticket_jwt_secret_dev_key_32_chars_min

# 企业微信配置（如果暂时没有，可以先使用占位符）
WECOM_CORP_ID=your_corp_id
WECOM_AGENT_ID=your_agent_id
WECOM_SECRET=your_secret
WECOM_REDIRECT_URI=http://localhost:5000/api/auth/wecom/callback
```

### 步骤 3: 启动项目

```bash
# 方式一：使用启动脚本
./start.sh

# 方式二：手动启动
docker-compose up -d
```

### 步骤 4: 等待服务启动

首次启动需要：
- 下载 Docker 镜像（5-10分钟）
- 构建项目镜像（3-5分钟）
- 初始化数据库（1-2分钟）

**总耗时**：约 10-20 分钟

### 步骤 5: 检查服务状态

```bash
# 查看所有服务状态
docker-compose ps

# 查看日志
docker-compose logs -f

# 检查 API 健康
curl http://localhost:5000/health
```

## ✅ 验证服务

启动成功后，访问以下地址：

- **Web 管理端**: http://localhost:3000
- **API 服务**: http://localhost:5000
- **API 文档**: http://localhost:5000/swagger
- **MinIO 控制台**: http://localhost:9001 (用户名/密码: minioadmin/minioadmin)

## 🐛 常见问题

### 问题：Docker daemon 未运行

**错误信息**：
```
Cannot connect to the Docker daemon
```

**解决方法**：
1. 打开 Docker Desktop 或 OrbStack
2. 等待完全启动
3. 运行 `docker ps` 验证

### 问题：端口被占用

**错误信息**：
```
Bind for 0.0.0.0:3000 failed: port is already allocated
```

**解决方法**：
```bash
# 检查端口占用
lsof -i :3000
lsof -i :5000

# 停止占用端口的进程，或修改 .env 中的端口配置
```

### 问题：服务启动失败

**解决方法**：
```bash
# 查看详细日志
docker-compose logs [service_name]

# 重新构建并启动
docker-compose build --no-cache
docker-compose up -d
```

## 📞 需要帮助？

如果遇到问题：
1. 查看详细日志：`docker-compose logs -f`
2. 检查服务状态：`docker-compose ps`
3. 参考完整文档：`./START_PROJECT.md`


