#!/bin/bash

# 设置 GitHub Project 和关联 Issues 到 Milestones 的脚本

set -e

# 检查 GitHub CLI 是否安装
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) 未安装"
    exit 1
fi

# 检查是否已登录
if ! gh auth status &> /dev/null; then
    echo "❌ 未登录 GitHub"
    exit 1
fi

# 获取仓库信息
REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || echo "")

if [ -z "$REPO" ]; then
    echo "❌ 当前目录不是 GitHub 仓库"
    exit 1
fi

echo "📦 仓库: $REPO"
echo ""

# 获取 Milestone 编号
echo "🔍 查找 Milestones..."
SPRINT1_MILESTONE=$(gh api repos/$REPO/milestones --jq '.[] | select(.title == "Sprint 1: MVP闭环") | .number' | head -1)
SPRINT2_MILESTONE=$(gh api repos/$REPO/milestones --jq '.[] | select(.title == "Sprint 2: 可用性增强 + v2.1质量保障") | .number' | head -1)
SPRINT3_MILESTONE=$(gh api repos/$REPO/milestones --jq '.[] | select(.title == "Sprint 3: 体验优化") | .number' | head -1)
V21_MILESTONE=$(gh api repos/$REPO/milestones --jq '.[] | select(.title == "v2.1: 质量保障（防系统退化）") | .number' | head -1)

if [ -z "$SPRINT1_MILESTONE" ] || [ -z "$SPRINT2_MILESTONE" ] || [ -z "$SPRINT3_MILESTONE" ]; then
    echo "⚠️  请先运行 create-milestones.sh 创建 Milestones"
    exit 1
fi

echo "找到 Milestones:"
echo "  Sprint 1: #$SPRINT1_MILESTONE"
echo "  Sprint 2: #$SPRINT2_MILESTONE"
echo "  Sprint 3: #$SPRINT3_MILESTONE"
if [ -n "$V21_MILESTONE" ]; then
    echo "  v2.1: #$V21_MILESTONE"
fi
echo ""

# 关联 Issues 到 Milestones
echo "🔗 关联 Issues 到 Milestones..."

# Sprint 1 Issues
for issue_num in {1..9}; do
    issue_file=$(find .github/issues/sprint-1 -name "${issue_num}*.md" | head -1)
    if [ -f "$issue_file" ]; then
        title=$(grep "^title:" "$issue_file" | sed 's/title: //' | sed "s/'//g" | sed 's/\[Sprint 1\] //')
        issue_number=$(gh issue list --repo "$REPO" --search "$title" --json number --jq '.[0].number' 2>/dev/null || echo "")
        if [ -n "$issue_number" ]; then
            echo "  关联 Issue #$issue_number ($title) 到 Sprint 1"
            gh issue edit "$issue_number" --repo "$REPO" --milestone "$SPRINT1_MILESTONE" || echo "    ⚠️  关联失败"
        fi
    fi
done

# Sprint 2 Issues
for issue_num in {10..26}; do
    issue_file=$(find .github/issues/sprint-2 -name "${issue_num}*.md" | head -1)
    if [ -f "$issue_file" ]; then
        title=$(grep "^title:" "$issue_file" | sed 's/title: //' | sed "s/'//g" | sed 's/\[Sprint 2\] //')
        issue_number=$(gh issue list --repo "$REPO" --search "$title" --json number --jq '.[0].number' 2>/dev/null || echo "")
        if [ -n "$issue_number" ]; then
            echo "  关联 Issue #$issue_number ($title) 到 Sprint 2"
            gh issue edit "$issue_number" --repo "$REPO" --milestone "$SPRINT2_MILESTONE" || echo "    ⚠️  关联失败"
        fi
    fi
done

# Sprint 3 Issues
for issue_num in {12..31}; do
    issue_file=$(find .github/issues/sprint-3 -name "${issue_num}*.md" | head -1)
    if [ -f "$issue_file" ]; then
        title=$(grep "^title:" "$issue_file" | sed 's/title: //' | sed "s/'//g" | sed 's/\[Sprint 3\] //')
        issue_number=$(gh issue list --repo "$REPO" --search "$title" --json number --jq '.[0].number' 2>/dev/null || echo "")
        if [ -n "$issue_number" ]; then
            echo "  关联 Issue #$issue_number ($title) 到 Sprint 3"
            gh issue edit "$issue_number" --repo "$REPO" --milestone "$SPRINT3_MILESTONE" || echo "    ⚠️  关联失败"
        fi
    fi
done

echo ""
echo "✅ Issues 关联完成！"
echo ""
echo "💡 提示："
echo "   1. 在 GitHub 网页上创建 Project（使用看板视图）"
echo "   2. 添加列：Backlog, Sprint 1, Sprint 2, Sprint 3, 待审查, 已完成"
echo "   3. 将 Issues 拖拽到对应的列"

