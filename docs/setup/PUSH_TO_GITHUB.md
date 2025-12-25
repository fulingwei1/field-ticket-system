# 推送到 GitHub 指南

## 🚀 快速开始

### 方式一：使用脚本（推荐）

```bash
cd "/Users/flw/非标自动化客服现场问题反馈系统"
./push-to-github.sh
```

这个脚本会自动：
1. ✅ 初始化 Git 仓库（如果还没有）
2. ✅ 添加所有文件
3. ✅ 创建初始提交
4. ✅ 创建 GitHub 仓库并推送（如果使用 GitHub CLI）

### 方式二：手动执行

#### Step 1: 初始化 Git 仓库

```bash
cd "/Users/flw/非标自动化客服现场问题反馈系统"
git init
```

#### Step 2: 添加文件并提交

```bash
git add .
git commit -m "Initial commit: 非标自动化客服现场问题反馈系统 v2.0

- 完整的项目文档和架构设计
- 31个开发任务（Sprint 1/2/3 + v2.1）
- GitHub Issues 和 Milestones 脚本
- 立项评审文档和执行摘要"
```

#### Step 3: 创建 GitHub 仓库并推送

**选项 A：使用 GitHub CLI（推荐）**

```bash
# 安装 GitHub CLI（如果还没有）
brew install gh

# 登录 GitHub
gh auth login

# 创建仓库并推送
gh repo create field-ticket-system --public --source=. --remote=origin --push
```

**选项 B：手动创建仓库**

1. 访问 https://github.com/new
2. 创建名为 `field-ticket-system` 的仓库（选择 Public）
3. **不要**初始化 README、.gitignore 或 license（因为我们已经有了）
4. 运行以下命令：

```bash
git remote add origin https://github.com/YOUR_USERNAME/field-ticket-system.git
git branch -M main
git push -u origin main
```

（将 `YOUR_USERNAME` 替换为你的 GitHub 用户名）

---

## 📋 前置条件检查

### 1. 检查 Git 是否安装

```bash
git --version
```

如果没有安装：
```bash
# macOS
brew install git

# 或访问 https://git-scm.com/downloads
```

### 2. 检查 GitHub CLI 是否安装（可选，但推荐）

```bash
gh --version
```

如果没有安装：
```bash
brew install gh
```

### 3. 登录 GitHub CLI（如果使用）

```bash
gh auth login
```

---

## ✅ 验证推送结果

### 查看远程仓库

```bash
git remote -v
```

应该看到：
```
origin  https://github.com/YOUR_USERNAME/field-ticket-system.git (fetch)
origin  https://github.com/YOUR_USERNAME/field-ticket-system.git (push)
```

### 在浏览器中打开仓库

```bash
gh repo view --web
```

或直接访问：
```
https://github.com/YOUR_USERNAME/field-ticket-system
```

---

## 🎯 下一步操作

推送成功后，运行以下脚本创建 Milestones 和 Issues：

```bash
./.github/scripts/setup-complete.sh
```

这会：
1. ✅ 创建 4 个 Milestones（Sprint 1/2/3 + v2.1）
2. ✅ 创建 31 个 Issues
3. ✅ 关联 Issues 到对应的 Milestones

---

## ⚠️ 常见问题

### Q1: 提示 "fatal: not a git repository"

**解决**：先运行 `git init` 初始化仓库

### Q2: 提示 "GitHub CLI (gh) 未安装"

**解决**：
- 安装：`brew install gh`
- 或使用手动方式创建仓库（见上方"选项 B"）

### Q3: 提示 "未登录 GitHub"

**解决**：运行 `gh auth login` 登录

### Q4: 提示 "仓库已存在"

**解决**：
- 如果仓库已存在，直接推送：
  ```bash
  git remote add origin https://github.com/YOUR_USERNAME/field-ticket-system.git
  git push -u origin main
  ```
- 或使用不同的仓库名：
  ```bash
  gh repo create field-ticket-system-v2 --public --source=. --remote=origin --push
  ```

### Q5: 推送时提示权限错误

**解决**：
- 检查是否已登录：`gh auth status`
- 检查 SSH 密钥是否配置（如果使用 SSH）
- 或使用 HTTPS 方式推送

---

## 📝 文件清单

推送后，仓库应包含以下内容：

### 文档目录
- `docs/` - 所有项目文档
- `.github/` - GitHub 配置和脚本
- `README.md` - 项目说明
- `TASKS_SUMMARY.md` - 任务总结

### 脚本文件
- `push-to-github.sh` - 推送脚本
- `.github/scripts/` - GitHub 设置脚本

### Issue 文件
- `.github/issues/sprint-1/` - 9 个 Issues
- `.github/issues/sprint-2/` - 17 个 Issues
- `.github/issues/sprint-3/` - 10 个 Issues

---

**最后更新**：2025-12-22

