# GitHub Issues 管理

本目录包含所有 Sprint 的 Issue 模板文件。

## 📁 目录结构

```
.github/issues/
├── sprint-1/          # Sprint 1 Issues (MVP闭环)
├── sprint-2/          # Sprint 2 Issues (可用性增强)
├── sprint-3/          # Sprint 3 Issues (体验优化)
├── create-issues.sh   # 批量创建 Issues 脚本
└── README.md          # 本文件
```

## 🚀 使用方法

### 方法一：使用脚本批量创建（推荐）

```bash
# 1. 确保已安装 GitHub CLI
brew install gh

# 2. 登录 GitHub
gh auth login

# 3. 确保当前目录是 GitHub 仓库
cd /path/to/field-ticket-system

# 4. 运行脚本
./.github/issues/create-issues.sh
```

### 方法二：手动创建

1. 在 GitHub 网页上创建 Issue
2. 选择对应的模板（Feature / Task / Bug）
3. 复制对应 Issue 文件的内容到 Issue 描述中

## 📋 Issue 列表

### Sprint 1 (MVP闭环，2周)

- [ ] #001 - 企业微信登录认证
- [ ] #002 - 工单创建和提交功能
- [ ] #003 - 附件上传功能（基础版）
- [ ] #004 - 工单分诊和解决方案创建
- [ ] #005 - 企业微信消息通知
- [ ] #006 - 验证结果提交
- [ ] #007 - Docker Compose 一键部署

### Sprint 2 (可用性增强，2周)

- [ ] #008 - 离线草稿和补传队列
- [ ] #009 - 附件断点续传
- [ ] #010 - 判断卡推荐引擎
- [ ] #011 - 基础统计看板

### Sprint 3 (体验优化，2周)

- [ ] #012 - 设备二维码和扫码
- [ ] #013 - 客户沟通模板
- [ ] #014 - 工单全文搜索
- [ ] #015 - 数据导出功能

## 📝 Issue 模板说明

每个 Issue 文件包含：

- **Front Matter** (YAML格式)：
  - `title`: Issue 标题
  - `labels`: 标签列表
  - `assignees`: 分配人（可选）

- **Body** (Markdown格式)：
  - 任务描述
  - 任务目标
  - 技术方案
  - 验收标准
  - 相关文档
  - 技术细节
  - 测试要点
  - 备注

## 🔄 更新 Issue

如果需要更新 Issue 内容：

1. 修改对应的 `.md` 文件
2. 在 GitHub 网页上编辑 Issue，更新内容
3. 或使用 GitHub CLI：
   ```bash
   gh issue edit <issue-number> --body-file .github/issues/sprint-1/001-xxx.md
   ```

## 📊 进度跟踪

使用 GitHub Projects 或 Milestones 来跟踪 Sprint 进度：

```bash
# 创建 Milestone
gh api repos/:owner/:repo/milestones -f title="Sprint 1" -f description="MVP闭环" -f due_on="2025-01-15T00:00:00Z"

# 将 Issue 关联到 Milestone
gh issue edit <issue-number> --milestone "Sprint 1"
```

## 🏷️ 标签说明

- `sprint-1/2/3`: Sprint 标识
- `backend/frontend/mobile`: 技术栈
- `priority:high/medium/low`: 优先级
- `bug/enhancement/task`: Issue 类型

## ❓ 常见问题

**Q: 脚本执行失败？**
A: 检查是否已登录 GitHub CLI (`gh auth status`)

**Q: 如何修改 Issue 标签？**
A: 编辑 Issue 文件的 `labels:` 行，然后重新运行脚本（注意：会创建重复 Issue，建议手动编辑）

**Q: 如何删除已创建的 Issue？**
A: 使用 `gh issue close <issue-number>` 或 `gh issue delete <issue-number>`



