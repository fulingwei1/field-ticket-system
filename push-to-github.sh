#!/bin/bash

# 推送到 GitHub 脚本
# 使用方法：bash push-to-github.sh

set -e

REPO_NAME="field-ticket-system"

echo "🚀 准备推送到 GitHub..."
echo ""

# 检查是否已配置远程仓库
if git remote | grep -q "^origin$"; then
    echo "✅ 远程仓库 'origin' 已存在"
    echo "当前远程仓库："
    git remote -v
    echo ""
    read -p "是否要使用现有远程仓库并推送？(y/n) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "📤 正在推送到 GitHub..."
        git push -u origin main
        echo ""
        echo "✅ 推送成功！"
        exit 0
    fi
fi

# 获取 GitHub 用户名
echo "请输入您的 GitHub 用户名："
read -r GITHUB_USERNAME

if [ -z "$GITHUB_USERNAME" ]; then
    echo "❌ 用户名不能为空"
    exit 1
fi

echo ""
echo "📦 配置远程仓库..."
git remote add origin "https://github.com/$GITHUB_USERNAME/$REPO_NAME.git" 2>/dev/null || \
git remote set-url origin "https://github.com/$GITHUB_USERNAME/$REPO_NAME.git"

echo "📤 正在推送到 GitHub..."
git push -u origin main

echo ""
echo "✅ 推送成功！"
echo "📍 仓库地址：https://github.com/$GITHUB_USERNAME/$REPO_NAME"





