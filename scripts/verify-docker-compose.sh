#!/bin/bash
# verify-docker-compose.sh
# 验证 Docker Compose 配置

set -e

echo "=========================================="
echo "Docker Compose 配置验证"
echo "=========================================="
echo ""

# 检查 Docker 和 Docker Compose
echo "1. 检查 Docker 和 Docker Compose..."
if ! command -v docker &> /dev/null; then
    echo "❌ Docker 未安装"
    exit 1
fi
echo "✅ Docker 已安装: $(docker --version)"

if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose 未安装"
    exit 1
fi
echo "✅ Docker Compose 已安装"

# 检查 .env 文件
echo ""
echo "2. 检查环境变量文件..."
if [ ! -f .env ]; then
    echo "⚠️  .env 文件不存在，从 env.example 创建..."
    if [ -f env.example ]; then
        cp env.example .env
        echo "✅ 已创建 .env 文件，请编辑并配置必需的环境变量"
    else
        echo "❌ env.example 文件不存在"
        exit 1
    fi
else
    echo "✅ .env 文件存在"
fi

# 检查必需的环境变量
echo ""
echo "3. 检查必需的环境变量..."
source .env 2>/dev/null || true

REQUIRED_VARS=(
    "POSTGRES_PASSWORD"
    "JWT_SECRET"
    "WECOM_CORP_ID"
    "WECOM_AGENT_ID"
    "WECOM_SECRET"
)

MISSING_VARS=()
for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ] || [[ "${!var}" == *"your_"* ]] || [[ "${!var}" == *"YOUR_"* ]]; then
        MISSING_VARS+=("$var")
    fi
done

if [ ${#MISSING_VARS[@]} -gt 0 ]; then
    echo "⚠️  以下环境变量需要配置："
    for var in "${MISSING_VARS[@]}"; do
        echo "   - $var"
    done
else
    echo "✅ 所有必需的环境变量已配置"
fi

# 验证 Docker Compose 配置
echo ""
echo "4. 验证 Docker Compose 配置..."
if docker-compose config > /dev/null 2>&1 || docker compose config > /dev/null 2>&1; then
    echo "✅ Docker Compose 配置有效"
else
    echo "❌ Docker Compose 配置无效"
    exit 1
fi

# 检查端口占用
echo ""
echo "5. 检查端口占用..."
PORTS=(5000 3000 5432 6379 9000 9001)
OCCUPIED_PORTS=()

for port in "${PORTS[@]}"; do
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1 || netstat -an 2>/dev/null | grep -q ":$port.*LISTEN"; then
        OCCUPIED_PORTS+=("$port")
    fi
done

if [ ${#OCCUPIED_PORTS[@]} -gt 0 ]; then
    echo "⚠️  以下端口已被占用："
    for port in "${OCCUPIED_PORTS[@]}"; do
        echo "   - $port"
    done
    echo "   如果这些端口被其他服务占用，请修改 .env 文件中的端口配置"
else
    echo "✅ 所有端口可用"
fi

# 检查 Dockerfile
echo ""
echo "6. 检查 Dockerfile..."
if [ -f backend/Dockerfile ]; then
    echo "✅ backend/Dockerfile 存在"
else
    echo "❌ backend/Dockerfile 不存在"
    exit 1
fi

if [ -f web-admin/Dockerfile ]; then
    echo "✅ web-admin/Dockerfile 存在"
else
    echo "❌ web-admin/Dockerfile 不存在"
    exit 1
fi

# 检查初始化脚本
echo ""
echo "7. 检查初始化脚本..."
if [ -f scripts/init-db.sh ]; then
    echo "✅ scripts/init-db.sh 存在"
    chmod +x scripts/init-db.sh 2>/dev/null || true
else
    echo "⚠️  scripts/init-db.sh 不存在（可选）"
fi

if [ -f scripts/init-minio.sh ]; then
    echo "✅ scripts/init-minio.sh 存在"
    chmod +x scripts/init-minio.sh 2>/dev/null || true
else
    echo "⚠️  scripts/init-minio.sh 不存在（可选）"
fi

# 总结
echo ""
echo "=========================================="
echo "验证完成"
echo "=========================================="

if [ ${#MISSING_VARS[@]} -gt 0 ] || [ ${#OCCUPIED_PORTS[@]} -gt 0 ]; then
    echo ""
    echo "⚠️  请解决上述问题后，再运行 docker-compose up -d"
    exit 1
else
    echo ""
    echo "✅ 配置验证通过！可以运行以下命令启动服务："
    echo ""
    echo "   docker-compose up -d"
    echo ""
    echo "   或者开发环境："
    echo ""
    echo "   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d"
    echo ""
fi

