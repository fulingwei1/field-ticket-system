#!/bin/bash

# 完整设置脚本：创建 Milestones + Issues + 关联

set -e

echo "🚀 开始完整设置 GitHub 仓库..."
echo ""

# 检查 GitHub CLI
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) 未安装"
    exit 1
fi

if ! gh auth status &> /dev/null; then
    echo "❌ 未登录 GitHub"
    exit 1
fi

REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || echo "")

if [ -z "$REPO" ]; then
    echo "❌ 当前目录不是 GitHub 仓库"
    exit 1
fi

echo "📦 仓库: $REPO"
echo ""

# Step 1: 创建 Milestones
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Step 1: 创建 Milestones"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
./.github/scripts/create-milestones.sh

echo ""
echo ""

# Step 2: 创建 Issues
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Step 2: 创建 Issues"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
./.github/issues/create-issues.sh || ./.github/scripts/create-all-issues.sh

echo ""
echo ""

# Step 3: 关联 Issues 到 Milestones
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Step 3: 关联 Issues 到 Milestones"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
./.github/scripts/setup-project.sh

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ 完整设置完成！"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📊 查看结果："
echo "   gh issue list --repo $REPO"
echo "   gh api repos/$REPO/milestones"
echo ""
echo "💡 下一步："
echo "   1. 在 GitHub 网页上创建 Project（看板视图）"
echo "   2. 添加列：Backlog, Sprint 1, Sprint 2, Sprint 3, 待审查, 已完成"
echo "   3. 将 Issues 拖拽到对应列"



