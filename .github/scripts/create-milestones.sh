#!/bin/bash

# 创建 GitHub Milestones 脚本

set -e

# 检查 GitHub CLI 是否安装
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) 未安装，请先安装："
    echo "   brew install gh"
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
    echo "❌ 当前目录不是 GitHub 仓库，请先创建仓库"
    exit 1
fi

echo "📦 仓库: $REPO"
echo ""

# 计算日期（Sprint 1 从今天开始，每个 Sprint 2周）
TODAY=$(date +%Y-%m-%d)
SPRINT1_START="$TODAY"
SPRINT1_END=$(date -v+14d +%Y-%m-%d 2>/dev/null || date -d "+14 days" +%Y-%m-%d)
SPRINT2_START="$SPRINT1_END"
SPRINT2_END=$(date -v+14d -j -f "%Y-%m-%d" "$SPRINT2_START" +%Y-%m-%d 2>/dev/null || date -d "$SPRINT2_START +14 days" +%Y-%m-%d)
SPRINT3_START="$SPRINT2_END"
SPRINT3_END=$(date -v+14d -j -f "%Y-%m-%d" "$SPRINT3_START" +%Y-%m-%d 2>/dev/null || date -d "$SPRINT3_START +14 days" +%Y-%m-%d)

echo "🚀 创建 Milestones..."

# 创建 Sprint 1 Milestone
echo "创建 Sprint 1: MVP闭环 ($SPRINT1_START - $SPRINT1_END)"
gh api repos/$REPO/milestones \
  -X POST \
  -f title="Sprint 1: MVP闭环" \
  -f description="跑通核心流程，可Demo

**目标**：可运行的系统，8个核心用例通过

**任务**：
- 企业微信登录认证
- 工单创建和提交
- 附件上传功能
- 工单分诊和解决方案创建
- 企业微信通知
- 验证结果提交
- Docker Compose 一键部署" \
  -f due_on="${SPRINT1_END}T23:59:59Z" || echo "⚠️  Sprint 1 可能已存在"

# 创建 Sprint 2 Milestone
echo "创建 Sprint 2: 可用性增强 ($SPRINT2_START - $SPRINT2_END)"
gh api repos/$REPO/milestones \
  -X POST \
  -f title="Sprint 2: 可用性增强 + v2.1质量保障" \
  -f description="提升用户体验和系统可用性 + v2.1质量保障需求

**目标**：完善离线支持、断点续传、智能推荐、质量保障

**基础功能**：
- 离线草稿和补传队列
- 附件断点续传
- 判断卡推荐引擎
- 基础统计看板
- AI辅助判断系统
- 口径输出层
- 工单去重和合并

**v2.1 P0需求（必须）**：
- 判断卡质量评分系统
- 判断轨迹版本化
- 设备配置快照和差异提示
- 最近变更自动关联

**v2.1 P1需求（强烈建议）**：
- 不可复现状态管理
- 潜在风险挂起功能" \
  -f due_on="${SPRINT2_END}T23:59:59Z" || echo "⚠️  Sprint 2 可能已存在"

# 创建 Sprint 3 Milestone
echo "创建 Sprint 3: 体验优化 ($SPRINT3_START - $SPRINT3_END)"
gh api repos/$REPO/milestones \
  -X POST \
  -f title="Sprint 3: 体验优化" \
  -f description="完善功能和提升用户体验

**目标**：完善辅助功能，提升使用体验

**任务**：
- 设备二维码和扫码
- 客户沟通模板
- 工单全文搜索
- 数据导出功能
- 责任归因系统
- 整改任务系统
- 工程师负载统计（v2.1 P2）
- 新人成长曲线可视化（v2.1 P2）
- 知识有效期和版本绑定（v2.1 P3）
- 知识来源追溯（v2.1 P3）
- KPI反作弊预警（v2.1 P3）" \
  -f due_on="${SPRINT3_END}T23:59:59Z" || echo "⚠️  Sprint 3 可能已存在"

# 创建 v2.1 Milestone
SPRINT2_1_START="$SPRINT2_START"
SPRINT2_1_END=$(date -v+7d -j -f "%Y-%m-%d" "$SPRINT2_1_START" +%Y-%m-%d 2>/dev/null || date -d "$SPRINT2_1_START +7 days" +%Y-%m-%d)

echo "创建 v2.1: 质量保障 ($SPRINT2_1_START - $SPRINT2_1_END)"
gh api repos/$REPO/milestones \
  -X POST \
  -f title="v2.1: 质量保障（防系统退化）" \
  -f description="v2.1 不是'加功能'，而是'防系统退化、防经验流失、防形式主义'

**P0需求（必须）**：
- 判断卡质量评分系统
- 判断轨迹版本化
- 设备配置快照和差异提示
- 最近变更自动关联

**P1需求（强烈建议）**：
- 不可复现状态管理
- 潜在风险挂起功能
- 口径结构完整性校验

**核心价值**：让系统'不会坏'" \
  -f due_on="${SPRINT2_1_END}T23:59:59Z" || echo "⚠️  v2.1 可能已存在"

echo ""
echo "✅ Milestones 创建完成！"
echo ""
echo "查看 Milestones:"
echo "   gh api repos/$REPO/milestones"

