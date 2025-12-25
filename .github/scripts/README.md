# GitHub 脚本工具

本目录包含用于管理 GitHub 仓库的自动化脚本。

## 📋 脚本列表

### 1. `create-milestones.sh`

创建 Sprint Milestones（里程碑）。

**使用方法**：
```bash
./.github/scripts/create-milestones.sh
```

**功能**：
- 创建 Sprint 1/2/3 的 Milestones
- 自动计算截止日期（每个 Sprint 2周）
- 添加 Milestone 描述

### 2. `setup-project.sh`

关联 Issues 到 Milestones。

**使用方法**：
```bash
./.github/scripts/setup-project.sh
```

**前置条件**：
- 已运行 `create-milestones.sh`
- 已创建所有 Issues

**功能**：
- 自动关联 Sprint 1 Issues 到 Sprint 1 Milestone
- 自动关联 Sprint 2 Issues 到 Sprint 2 Milestone
- 自动关联 Sprint 3 Issues 到 Sprint 3 Milestone

### 3. `create-issues.sh`

批量创建 GitHub Issues（在 `.github/issues/` 目录）。

**使用方法**：
```bash
./.github/issues/create-issues.sh
```

## 🚀 完整设置流程

```bash
# 1. 创建 Milestones
./.github/scripts/create-milestones.sh

# 2. 创建 Issues
./.github/issues/create-issues.sh

# 3. 关联 Issues 到 Milestones
./.github/scripts/setup-project.sh
```

## 📝 注意事项

- 所有脚本都需要 GitHub CLI (`gh`) 已安装并登录
- 脚本会自动检测当前仓库
- 如果资源已存在，脚本会跳过或提示

## ❓ 常见问题

**Q: 脚本执行失败？**
A: 检查是否已登录：`gh auth status`

**Q: 如何修改 Milestone 日期？**
A: 编辑脚本中的日期计算逻辑，或手动在 GitHub 网页上修改

**Q: 如何重新运行脚本？**
A: 脚本支持幂等操作，可以安全地重复运行



