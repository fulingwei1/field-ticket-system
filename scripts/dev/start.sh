#!/bin/bash

# 项目启动脚本

echo "=========================================="
echo "  非标自动化客服现场管理系统 - 启动脚本"
echo "=========================================="
echo ""

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker daemon 未运行"
    echo "请先启动 Docker Desktop 或 OrbStack"
    exit 1
fi

echo "✅ Docker 已运行"
echo ""

# 检查 .env 文件
if [ ! -f .env ]; then
    echo "⚠️  .env 文件不存在，正在创建..."
    cp env.example .env
    echo "✅ 已创建 .env 文件"
    echo "⚠️  请编辑 .env 文件，至少修改以下配置："
    echo "   - POSTGRES_PASSWORD"
    echo "   - JWT_SECRET"
    echo "   - WECOM_* (企业微信配置)"
    echo ""
    read -p "按 Enter 继续启动（使用默认配置）..."
fi

echo "🚀 启动 Docker Compose 服务..."
echo ""

# 启动服务
docker-compose up -d

echo ""
echo "⏳ 等待服务启动（首次启动可能需要 10-20 分钟）..."
echo ""

# 等待服务启动
sleep 5

# 显示服务状态
echo "📊 服务状态："
docker-compose ps

echo ""
echo "=========================================="
echo "  服务访问地址"
echo "=========================================="
echo "  Web 管理端:  http://localhost:3000"
echo "  API 服务:    http://localhost:5000"
echo "  API 文档:    http://localhost:5000/swagger"
echo "  MinIO 控制台: http://localhost:9001"
echo ""
echo "查看日志: docker-compose logs -f"
echo "停止服务: docker-compose stop"
echo "=========================================="
