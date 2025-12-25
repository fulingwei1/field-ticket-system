# GitHub 仓库完整设置指南

> 本指南将帮助你完成 GitHub 仓库的完整设置，包括创建 Milestones、Issues 和关联关系。

---

## 📋 前置条件

1. **已安装 GitHub CLI**
   ```bash
   brew install gh
   ```

2. **已登录 GitHub**
   ```bash
   gh auth login
   ```

3. **已创建 GitHub 仓库**
   ```bash
   # 如果还没有创建仓库
   gh repo create field-ticket-system --public --source=. --remote=origin --push
   ```

---

## 🚀 快速开始

### 方式一：一键执行（推荐）

```bash
cd "/Users/flw/非标自动化客服现场问题反馈系统"
./.github/scripts/setup-complete.sh
```

这个脚本会：
1. ✅ 创建所有 Milestones（Sprint 1/2/3 + v2.1）
2. ✅ 创建所有 Issues（31个）
3. ✅ 关联 Issues 到对应的 Milestones

### 方式二：分步执行

#### Step 1: 创建 Milestones

```bash
./.github/scripts/create-milestones.sh
```

这会创建：
- Sprint 1: MVP闭环
- Sprint 2: 可用性增强 + v2.1质量保障
- Sprint 3: 体验优化
- v2.1: 质量保障（防系统退化）

#### Step 2: 创建 Issues

```bash
./.github/scripts/create-all-issues.sh
```

这会创建所有 31 个 Issues：
- Sprint 1: 9 个 Issues
- Sprint 2: 17 个 Issues
- Sprint 3: 10 个 Issues

#### Step 3: 关联 Issues 到 Milestones

```bash
./.github/scripts/setup-project.sh
```

---

## 📊 创建结果验证

### 查看 Milestones

```bash
gh api repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/milestones
```

### 查看 Issues

```bash
gh issue list --repo $(gh repo view --json nameWithOwner -q .nameWithOwner)
```

### 查看特定 Sprint 的 Issues

```bash
# Sprint 1
gh issue list --repo $(gh repo view --json nameWithOwner -q .nameWithOwner) --milestone "Sprint 1: MVP闭环"

# Sprint 2
gh issue list --repo $(gh repo view --json nameWithOwner -q .nameWithOwner) --milestone "Sprint 2: 可用性增强 + v2.1质量保障"

# Sprint 3
gh issue list --repo $(gh repo view --json nameWithOwner -q .nameWithOwner) --milestone "Sprint 3: 体验优化"
```

---

## 🎯 下一步操作

### 1. 创建 GitHub Project（看板）

1. 在 GitHub 网页上进入仓库
2. 点击 "Projects" 标签
3. 创建新 Project（选择 "Board" 模板）
4. 添加列：
   - Backlog
   - Sprint 1
   - Sprint 2
   - Sprint 3
   - 待审查
   - 已完成

### 2. 将 Issues 添加到 Project

```bash
# 获取 Project 编号（需要在网页上查看）
PROJECT_NUMBER=1  # 替换为实际编号

# 批量添加 Issues 到 Project
gh issue list --json number --jq '.[].number' | while read issue_num; do
    gh api repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/projects/$PROJECT_NUMBER/columns --jq '.[0].id' | while read column_id; do
        gh api repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/projects/columns/$column_id/cards \
            -X POST \
            -f content_id=$issue_num \
            -f content_type=Issue
    done
done
```

### 3. 设置 Labels

```bash
# 导入 Labels
gh label list --repo $(gh repo view --json nameWithOwner -q .nameWithOwner) | while read label; do
    gh label create "$label" --repo $(gh repo view --json nameWithOwner -q .nameWithOwner) --force
done

# 或者使用 GitHub API 批量创建
gh api repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/labels -X POST -f name=bug -f color=d73a4a
# ... 其他 labels
```

---

## ⚠️ 常见问题

### Q1: 脚本执行失败，提示"未登录"

**解决**：
```bash
gh auth login
```

### Q2: 脚本执行失败，提示"不是 GitHub 仓库"

**解决**：
```bash
# 检查是否已初始化 Git
git status

# 如果没有，先初始化
git init
git add .
git commit -m "Initial commit"

# 创建 GitHub 仓库
gh repo create field-ticket-system --public --source=. --remote=origin --push
```

### Q3: Issues 创建失败，提示"API 限流"

**解决**：
- 脚本已内置延迟（每个 Issue 间隔 1 秒）
- 如果仍然失败，可以分批执行：
  ```bash
  # 只创建 Sprint 1
  for file in .github/issues/sprint-1/*.md; do
      ./.github/issues/create-issues.sh "$file"
      sleep 2
  done
  ```

### Q4: Milestone 关联失败

**解决**：
- 确保先创建 Milestones
- 检查 Milestone 名称是否匹配
- 可以手动在 GitHub 网页上关联

---

## 📝 手动操作（如果脚本失败）

如果脚本执行失败，可以手动操作：

### 创建 Milestone

1. 在 GitHub 网页上进入仓库
2. 点击 "Issues" → "Milestones"
3. 点击 "New milestone"
4. 填写信息并创建

### 创建 Issue

1. 在 GitHub 网页上进入仓库
2. 点击 "Issues" → "New issue"
3. 复制 `.github/issues/sprint-X/XXX-xxx.md` 文件内容
4. 粘贴到 Issue 编辑器中
5. 选择对应的 Milestone 和 Labels
6. 提交

---

## 📚 相关文档

- [开发指南](./docs/DEVELOPMENT_GUIDE.md)
- [任务拆分总结](./TASKS_SUMMARY.md)
- [立项评审文档](./docs/PROJECT_PROPOSAL.md)
- [执行摘要](./docs/EXECUTIVE_SUMMARY.md)

---

**最后更新**：2025-12-22

