---
title: '[Sprint 1] 实现 Docker Compose 一键部署'
labels: 'sprint-1,devops,priority:high'
assignees: ''
---

## 📋 任务描述

实现 Docker Compose 一键部署配置，包括后端API、Web管理端、PostgreSQL、Redis、MinIO等所有服务的容器化部署。

## 🎯 任务目标

- 所有服务可以通过 Docker Compose 一键启动
- 支持开发环境和生产环境配置
- 提供环境变量模板
- 提供健康检查

## 📐 技术方案

### Docker Compose 配置

**涉及文件**：
- `docker-compose.yml` - 主配置文件
- `docker-compose.dev.yml` - 开发环境配置
- `docker-compose.prod.yml` - 生产环境配置
- `.env.example` - 环境变量模板

### 服务清单

1. **API 服务** (.NET 8)
2. **Web 管理端** (React)
3. **PostgreSQL 16** - 数据库
4. **Redis 7** - 缓存
5. **MinIO** - 对象存储

### 实现步骤

#### 1. 后端 Dockerfile

**涉及文件**：
- `backend/Dockerfile`

**内容**：
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FieldTicket.Api/FieldTicket.Api.csproj", "src/FieldTicket.Api/"]
# ... 复制其他项目文件
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FieldTicket.Api.dll"]
```

#### 2. Web 管理端 Dockerfile

**涉及文件**：
- `web-admin/Dockerfile`

**内容**：
```dockerfile
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

#### 3. Docker Compose 配置

**涉及文件**：
- `docker-compose.yml`

**关键配置**：
- 服务依赖关系
- 网络配置
- 数据卷挂载
- 环境变量
- 健康检查

#### 4. 环境变量模板

**涉及文件**：
- `.env.example`

**包含配置**：
- 数据库连接字符串
- Redis 连接
- MinIO 配置
- 企业微信配置
- JWT 密钥

#### 5. 初始化脚本

**涉及文件**：
- `scripts/init-db.sh` - 数据库初始化
- `scripts/wait-for-it.sh` - 等待服务就绪

## ✅ 验收标准

- [ ] 可以通过 `docker-compose up -d` 一键启动所有服务
- [ ] 所有服务健康检查通过
- [ ] 数据库自动初始化（表结构、初始数据）
- [ ] 环境变量配置正确
- [ ] 服务间网络通信正常
- [ ] 数据持久化（数据卷挂载）
- [ ] 支持开发和生产环境切换
- [ ] 提供清晰的启动和停止文档

## 🔗 相关文档

- [开发规格书 - 部署配置](../field-ticket-system-spec-final.md#十一部署配置)
- [Docker Compose 文档](https://docs.docker.com/compose/)

## 📝 技术细节

### 服务端口映射

- API: `5000:5000`
- Web: `3000:80`
- PostgreSQL: `5432:5432`
- Redis: `6379:6379`
- MinIO API: `9000:9000`
- MinIO Console: `9001:9001`

### 数据卷

- `pgdata` - PostgreSQL 数据
- `redisdata` - Redis 数据
- `miniodata` - MinIO 数据

### 健康检查

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
  interval: 30s
  timeout: 10s
  retries: 3
```

## 🧪 测试要点

- [ ] 测试所有服务正常启动
- [ ] 测试服务间通信
- [ ] 测试数据持久化
- [ ] 测试环境变量配置
- [ ] 测试健康检查
- [ ] 测试服务重启恢复

## 📝 备注

- 生产环境建议使用 Docker Swarm 或 Kubernetes
- 需要配置反向代理（Nginx）用于生产环境
- 需要配置 SSL 证书（生产环境）



