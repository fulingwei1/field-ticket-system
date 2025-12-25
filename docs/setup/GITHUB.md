# GitHub 仓库设置指南

本文档将指导您完成 GitHub 仓库的创建和初始化。

## 📋 前置要求

- 已安装 Git
- 已配置 GitHub 账号
- （可选）已安装 GitHub CLI (`gh`)

## 🚀 方法一：使用 GitHub CLI（推荐）

如果您已安装 GitHub CLI，这是最简单的方法：

```bash
# 1. 运行初始化脚本
./setup-git.sh

# 2. 使用 GitHub CLI 创建仓库并推送
gh repo create field-ticket-system \
  --public \
  --source=. \
  --remote=origin \
  --push
```

## 🔧 方法二：手动操作

### 步骤 1: 初始化本地 Git 仓库

```bash
# 运行初始化脚本
./setup-git.sh
```

或者手动执行：

```bash
# 初始化仓库
git init

# 添加所有文件
git add .

# 创建初始提交
git commit -m "chore: 初始化项目仓库"
```

### 步骤 2: 在 GitHub 上创建仓库

1. 登录 GitHub
2. 点击右上角的 "+" → "New repository"
3. 填写仓库信息：
   - **Repository name**: `field-ticket-system`（或您喜欢的名称）
   - **Description**: `非标自动化客服现场问题反馈系统`
   - **Visibility**: 选择 Public 或 Private
   - **不要**勾选 "Initialize this repository with a README"（我们已经有了）
4. 点击 "Create repository"

### 步骤 3: 连接本地仓库到 GitHub

```bash
# 添加远程仓库（替换 YOUR_USERNAME 和 YOUR_REPO_NAME）
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git

# 重命名主分支为 main（如果还没有）
git branch -M main

# 推送代码到 GitHub
git push -u origin main
```

## 📝 配置 GitHub 仓库

### 1. 设置仓库描述和主题

在仓库设置中添加：
- **Description**: `现场问题结构化上报系统 - 让初级工程师2分钟内完成问题提交`
- **Topics**: `field-service`, `ticket-system`, `flutter`, `dotnet`, `react`, `postgresql`

### 2. 配置 Labels（标签）

可以使用 GitHub CLI 批量创建标签：

```bash
# 安装 gh-label-sync（如果还没有）
npm install -g github-label-sync

# 同步标签（需要 GitHub Token）
github-label-sync --labels .github/labels.json YOUR_USERNAME/YOUR_REPO_NAME
```

或者手动在 GitHub 网页上创建标签。

### 3. 配置分支保护规则（可选）

在仓库设置 → Branches 中：
- 保护 `main` 分支
- 要求 PR 审查
- 要求 CI 通过

### 4. 配置 GitHub Pages（如果需要文档站点）

在仓库设置 → Pages 中：
- Source: `main` 分支
- Folder: `/docs` 或 `/root`

## ✅ 验证设置

完成上述步骤后，您应该能够：

- [x] 在 GitHub 上看到仓库
- [x] 看到 README.md 正确显示
- [x] 能够创建 Issue（使用模板）
- [x] 能够创建 Pull Request
- [x] CI 工作流能够运行（推送代码后）

## 🔗 有用的链接

- [GitHub 文档](https://docs.github.com/)
- [Git 文档](https://git-scm.com/doc)
- [GitHub CLI 文档](https://cli.github.com/manual/)

## ❓ 遇到问题？

如果遇到问题，可以：

1. 检查 Git 配置：`git config --list`
2. 检查远程仓库：`git remote -v`
3. 查看 Git 日志：`git log --oneline`
4. 创建 Issue 寻求帮助

---

**提示**: 如果仓库名称包含中文字符，建议使用英文名称，避免 URL 编码问题。

