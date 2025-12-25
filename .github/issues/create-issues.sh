#!/bin/bash

# 批量创建 GitHub Issues 脚本
# 使用 GitHub CLI (gh) 批量创建 Issues

set -e

# 检查 GitHub CLI 是否安装
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) 未安装，请先安装："
    echo "   brew install gh"
    echo "   或访问: https://cli.github.com/"
    exit 1
fi

# 检查是否已登录
if ! gh auth status &> /dev/null; then
    echo "❌ 未登录 GitHub，请先登录："
    echo "   gh auth login"
    exit 1
fi

# 获取仓库信息
REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || echo "")

if [ -z "$REPO" ]; then
    echo "❌ 当前目录不是 GitHub 仓库，请先创建仓库并设置远程："
    echo "   gh repo create field-ticket-system --public --source=. --remote=origin --push"
    exit 1
fi

echo "📦 仓库: $REPO"
echo ""

# 创建 Issues 的函数
create_issue() {
    local file=$1
    local sprint=$(basename $(dirname $file))
    local title=$(grep "^title:" "$file" | sed 's/title: //' | sed "s/'//g")
    local labels=$(grep "^labels:" "$file" | sed 's/labels: //' | sed "s/'//g")
    local body_file=$(mktemp)
    
    # 提取 body 部分（去掉 front matter）
    awk '/^---$/{if(++count==2)exit} count==1{next} {print}' "$file" > "$body_file"
    
    echo "创建 Issue: $title"
    
    # 创建 Issue
    gh issue create \
        --repo "$REPO" \
        --title "$title" \
        --label "$labels" \
        --body-file "$body_file" \
        --assignee "" || echo "⚠️  创建失败: $title"
    
    rm "$body_file"
}

# 创建 Sprint 1 Issues
echo "🚀 创建 Sprint 1 Issues..."
for file in .github/issues/sprint-1/*.md; do
    if [ -f "$file" ]; then
        create_issue "$file"
    fi
done

# 创建 Sprint 2 Issues
echo ""
echo "🚀 创建 Sprint 2 Issues..."
for file in .github/issues/sprint-2/*.md; do
    if [ -f "$file" ]; then
        create_issue "$file"
    fi
done

# 创建 Sprint 3 Issues
echo ""
echo "🚀 创建 Sprint 3 Issues..."
for file in .github/issues/sprint-3/*.md; do
    if [ -f "$file" ]; then
        create_issue "$file"
    fi
done

echo ""
echo "✅ 所有 Issues 创建完成！"
echo ""
echo "查看 Issues:"
echo "   gh issue list --repo $REPO"



