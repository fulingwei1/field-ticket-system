#!/bin/bash

# Web管理端一键启动脚本
# 用法: ./start.sh [dev|build|preview]

set -e

echo "========================================"
echo "  现场工单系统 - Web管理端启动脚本"
echo "========================================"
echo ""

# 检查Node.js
if ! command -v node &> /dev/null; then
    echo "❌ 错误: 未检测到 Node.js"
    echo "请先安装 Node.js (>= 16.x)"
    echo "访问: https://nodejs.org/"
    exit 1
fi

echo "✅ Node.js 版本: $(node --version)"
echo "✅ npm 版本: $(npm --version)"
echo ""

# 检查依赖
if [ ! -d "node_modules" ]; then
    echo "📦 检测到未安装依赖，正在安装..."
    npm install
    echo ""
fi

# 获取本机IP
get_local_ip() {
    # Linux
    if command -v ip &> /dev/null; then
        ip addr show | grep 'inet ' | grep -v '127.0.0.1' | awk '{print $2}' | cut -d/ -f1 | head -1
    # macOS
    elif command -v ifconfig &> /dev/null; then
        ifconfig | grep 'inet ' | grep -v '127.0.0.1' | awk '{print $2}' | head -1
    else
        echo "未知"
    fi
}

LOCAL_IP=$(get_local_ip)

# 根据参数选择操作
MODE=${1:-dev}

case $MODE in
    dev)
        echo "🚀 启动开发服务器..."
        echo ""
        echo "📡 访问地址:"
        echo "   本地访问: http://localhost:5173"
        if [ "$LOCAL_IP" != "未知" ]; then
            echo "   局域网访问: http://$LOCAL_IP:5173"
        fi
        echo ""
        echo "💡 提示: 远程客服工程师可以通过局域网地址访问"
        echo "   （需确保在同一网络，且防火墙开放5173端口）"
        echo ""
        echo "按 Ctrl+C 停止服务"
        echo "========================================"
        echo ""
        npm run dev
        ;;

    build)
        echo "🔨 构建生产版本..."
        npm run build
        echo ""
        echo "✅ 构建完成！输出目录: dist/"
        echo ""
        echo "📝 后续步骤:"
        echo "   1. 使用 Nginx 部署 dist 目录"
        echo "   2. 或运行 ./start.sh preview 预览构建结果"
        ;;

    preview)
        echo "👀 预览生产版本..."
        if [ ! -d "dist" ]; then
            echo "❌ 错误: 未找到 dist 目录"
            echo "请先运行: ./start.sh build"
            exit 1
        fi
        echo ""
        echo "📡 访问地址:"
        echo "   本地访问: http://localhost:4173"
        if [ "$LOCAL_IP" != "未知" ]; then
            echo "   局域网访问: http://$LOCAL_IP:4173"
        fi
        echo ""
        echo "按 Ctrl+C 停止服务"
        echo "========================================"
        echo ""
        npm run preview
        ;;

    *)
        echo "❌ 未知命令: $MODE"
        echo ""
        echo "用法: ./start.sh [命令]"
        echo ""
        echo "可用命令:"
        echo "  dev      - 启动开发服务器（默认）"
        echo "  build    - 构建生产版本"
        echo "  preview  - 预览生产版本"
        echo ""
        echo "示例:"
        echo "  ./start.sh          # 启动开发服务器"
        echo "  ./start.sh dev      # 启动开发服务器"
        echo "  ./start.sh build    # 构建生产版本"
        echo "  ./start.sh preview  # 预览生产版本"
        exit 1
        ;;
esac
