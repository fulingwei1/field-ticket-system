#!/bin/bash

# GitHub 仓库设置脚本
# 使用方法：bash setup-github.sh

set -e

REPO_NAME="field-ticket-system"
REPO_DESC="非标自动化客服现场管理系统 - Field Ticket Management System"

echo "🚀 开始设置 GitHub 仓库..."
echo ""

# 检查是否已配置远程仓库
if git remote | grep -q "^origin$"; then
    echo "⚠️  远程仓库 'origin' 已存在"
    echo "当前远程仓库："
    git remote -v
    echo ""
    read -p "是否要更新远程仓库 URL？(y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "❌ 取消操作"
        exit 1
    fi
fi

# 检查 GitHub CLI
if command -v gh &> /dev/null; then
    echo "✅ 检测到 GitHub CLI"
    
    # 检查登录状态
    if gh auth status &> /dev/null; then
        echo "✅ GitHub CLI 已登录"
        echo ""
        echo "📦 正在创建 GitHub 仓库并推送代码..."
        
        # 创建仓库并推送
        gh repo create "$REPO_NAME" \
            --public \
            --source=. \
            --remote=origin \
            --description "$REPO_DESC" \
            --push
        
        echo ""
        echo "✅ 成功！仓库已创建并推送"
        echo "📍 仓库地址：https://github.com/$(gh api user --jq .login)/$REPO_NAME"
    else
        echo "❌ GitHub CLI 未登录"
        echo ""
        echo "请先运行以下命令登录："
        echo "  gh auth login"
        echo ""
        echo "登录后，再次运行此脚本"
        exit 1
    fi
else
    echo "⚠️  未检测到 GitHub CLI"
    echo ""
    echo "请选择以下方式之一："
    echo ""
    echo "方法 1：安装 GitHub CLI"
    echo "  brew install gh"
    echo "  gh auth login"
    echo "  然后再次运行此脚本"
    echo ""
    echo "方法 2：手动创建仓库"
    echo "  1. 访问 https://github.com/new"
    echo "  2. 仓库名称：$REPO_NAME"
    echo "  3. 描述：$REPO_DESC"
    echo "  4. 选择 Public 或 Private"
    echo "  5. 不要初始化任何文件"
    echo "  6. 点击 'Create repository'"
    echo ""
    echo "然后运行以下命令（替换 YOUR_USERNAME）："
    echo "  git remote add origin https://github.com/YOUR_USERNAME/$REPO_NAME.git"
    echo "  git push -u origin main"
    exit 1
fi


