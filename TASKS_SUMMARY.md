# 任务拆分总结

本文档总结了根据设计文档拆分的所有开发任务。

## 📊 任务概览

| Sprint | 任务数 | 优先级 | 预计时间 |
|--------|--------|--------|----------|
| Sprint 1 | 9 | P0 (高) | 2周 |
| Sprint 2 | 15 | P1 (中) | 2周 |
| Sprint 3 | 13 | P2/P3 (低) | 2周 |
| Sprint 4 | 1 | P1 (中) | 2周 |
| **总计** | **38** | - | **8周** |

## 🎯 Sprint 1: MVP闭环（2周）

**目标**：跑通核心流程，可Demo

| # | 任务 | 涉及模块 | 优先级 |
|---|------|---------|--------|
| 001 | 企业微信登录认证 | Backend, Frontend, Mobile | P0 |
| 002 | 工单创建和提交功能 | Backend, Frontend, Mobile | P0 |
| 003 | 附件上传功能（基础版） | Backend, Frontend, Mobile | P0 |
| 004 | 工单分诊和解决方案创建 | Backend, Frontend | P0 |
| 005 | 企业微信消息通知 | Backend | P0 |
| 006 | 验证结果提交 | Backend, Frontend, Mobile | P0 |
| 007 | Docker Compose 一键部署 | DevOps | P0 |
| 008 | 工单通知规则配置系统 | Backend, Frontend | P0 |
| 009 | 问诊式补全缺失信息清单 | Backend, Frontend, Mobile | P0 |

**交付物**：可运行的系统，8个核心用例通过

## 🚀 Sprint 2: 可用性增强（2周）

**目标**：提升用户体验和系统可用性

| # | 任务 | 涉及模块 | 优先级 |
|---|------|---------|--------|
| 008 | 离线草稿和补传队列 | Mobile | P1 |
| 009 | 附件断点续传 | Backend, Mobile | P1 |
| 010 | 判断卡推荐引擎 | Backend, Frontend | P1 |
| 011 | 基础统计看板 | Backend, Frontend | P1 |
| 016 | AI辅助判断系统 | Backend, Frontend | P1 |
| 017 | 口径输出层 | Backend, Frontend | P1 |
| 020 | 工单去重和合并 | Backend | P1 |
| 021 | 判断卡质量评分系统 | Backend | P0 |
| 022 | 判断轨迹版本化 | Backend, Frontend | P0 |
| 023 | 设备配置快照和差异提示 | Backend, Frontend | P0 |
| 024 | 最近变更自动关联 | Backend, Frontend | P0 |
| 025 | 不可复现状态管理 | Backend, Frontend | P1 |
| 026 | 潜在风险挂起功能 | Backend, Frontend | P1 |
| 041 | 企业微信小程序基础框架 | Frontend, Miniprogram | P0 |
| 042 | 小程序工单创建功能 | Frontend, Miniprogram | P0 |
| 043 | 小程序验证和沟通功能 | Frontend, Miniprogram | P0 |

## ✨ Sprint 3: 体验优化（2周）

**目标**：完善功能和提升用户体验

| # | 任务 | 涉及模块 | 优先级 |
|---|------|---------|--------|
| 012 | 设备二维码和扫码 | Backend, Frontend, Mobile | P2 |
| 013 | 客户沟通模板 | Backend, Frontend, Mobile | P2 |
| 014 | 工单全文搜索 | Backend, Frontend | P2 |
| 015 | 数据导出功能 | Backend, Frontend | P2 |
| 018 | 责任归因系统 | Backend, Frontend | P2 |
| 019 | 整改任务系统 | Backend, Frontend | P2 |
| 027 | 工程师负载统计 | Backend, Frontend | P2 |
| 028 | 新人成长曲线可视化 | Backend, Frontend | P2 |
| 029 | 知识有效期和版本绑定 | Backend, Frontend | P3 |
| 030 | 知识来源追溯 | Backend, Frontend | P3 |
| 031 | KPI反作弊预警 | Backend | P3 |
| 032 | 判断卡知识图谱构建 | Backend, Frontend | P1 |
| 033 | AI深度分析缺失信息 | Backend, Frontend, Mobile | P1 |
| 044 | 需求变更管理系统 | Backend, Frontend | P1 |
| 045 | 设计变更系统集成 | Backend, Frontend, Mobile, Integration | P1 |

## 📊 Sprint 4: 绩效管理（2周）

**目标**：建立工作日志和绩效管理系统

| # | 任务 | 涉及模块 | 优先级 |
|---|------|---------|--------|
| 046 | 工作日志与绩效管理系统 | Backend, Frontend, Mobile, AI | P1 |

## 📁 文件结构

所有 Issue 文件已创建在 `.github/issues/` 目录下：

```
.github/issues/
├── sprint-1/
│   ├── 001-企业微信登录认证.md
│   ├── 002-工单创建和提交.md
│   ├── 003-附件上传功能.md
│   ├── 004-工单分诊和解决方案创建.md
│   ├── 005-企业微信通知.md
│   ├── 006-验证结果提交.md
│   └── 007-Docker-Compose部署.md
├── sprint-2/
│   ├── 008-离线草稿和补传队列.md
│   ├── 009-附件断点续传.md
│   ├── 010-判断卡推荐引擎.md
│   └── 011-统计看板.md
├── sprint-3/
│   ├── 012-设备二维码和扫码.md
│   ├── 013-客户沟通模板.md
│   ├── 014-工单全文搜索.md
│   └── 015-数据导出功能.md
├── create-issues.sh    # 批量创建脚本
└── README.md           # 使用说明
```

## 🚀 下一步操作

### 1. 创建 GitHub 仓库

如果还没有创建仓库，请参考 `SETUP_GITHUB.md`：

```bash
# 使用 GitHub CLI
gh repo create field-ticket-system \
  --public \
  --source=. \
  --remote=origin \
  --push
```

### 2. 批量创建 Issues

```bash
# 确保已安装 GitHub CLI 并登录
gh auth login

# 运行批量创建脚本
./.github/issues/create-issues.sh
```

### 3. 设置项目看板（可选）

在 GitHub 上创建 Project，按 Sprint 组织 Issues：

```bash
# 创建 Milestones
gh api repos/:owner/:repo/milestones -f title="Sprint 1" -f description="MVP闭环"
gh api repos/:owner/:repo/milestones -f title="Sprint 2" -f description="可用性增强"
gh api repos/:owner/:repo/milestones -f title="Sprint 3" -f description="体验优化"
```

### 4. 开始开发

按照 Sprint 1 的任务顺序开始开发：

1. 先完成基础设施（#007 Docker部署）
2. 然后实现认证（#001）
3. 接着实现核心业务（#002, #003, #004）
4. 最后完善通知和验证（#005, #006）

## 📝 Issue 内容说明

每个 Issue 文件包含：

1. **Front Matter** (YAML)
   - 标题、标签、分配人

2. **任务描述**
   - 清晰描述要做什么

3. **任务目标**
   - 完成后的预期结果

4. **技术方案**
   - 详细的实现方案
   - 涉及的文件和模块
   - API 设计

5. **验收标准**
   - 可检查的完成标准

6. **相关文档**
   - 链接到设计文档

7. **技术细节**
   - 数据结构示例
   - 关键实现点

8. **测试要点**
   - 需要测试的场景

## 🔄 任务依赖关系

```
Sprint 1:
  007 (部署) → 001 (认证) → 002 (工单) → 003 (附件)
                                    ↓
  004 (分诊) ← 005 (通知) ← 006 (验证)

Sprint 2:
  008 (离线) → 009 (断点续传)
  010 (推荐) → 011 (统计)

Sprint 3:
  012 (扫码) → 013 (模板) → 014 (搜索) → 015 (导出)
```

## 📊 工作量估算

| 模块 | Sprint 1 | Sprint 2 | Sprint 3 | 总计 |
|------|----------|----------|----------|------|
| Backend | 5人天 | 3人天 | 2人天 | 10人天 |
| Frontend | 3人天 | 2人天 | 2人天 | 7人天 |
| Mobile | 4人天 | 2人天 | 1人天 | 7人天 |
| DevOps | 2人天 | - | - | 2人天 |
| **总计** | **14人天** | **7人天** | **5人天** | **26人天** |

*注：以上为粗略估算，实际工作量可能因团队经验和技术栈熟悉程度而有所不同*

## ✅ 检查清单

在开始开发前，请确认：

- [ ] GitHub 仓库已创建
- [ ] 所有 Issues 已创建
- [ ] Milestones 已设置
- [ ] 开发环境已准备（.NET 8, Node.js, Flutter）
- [ ] Docker 环境已准备
- [ ] 企业微信配置已获取
- [ ] 数据库设计已确认
- [ ] API 契约已确认

## 📚 相关文档

- [完整开发规格书](./field-ticket-system-spec-final.md)
- [系统总体规划](./客服系统总体规划.md)
- [客户和设备模块设计](./客户和设备模块详细设计.md)
- [问题管理模块设计](./问题管理模块详细设计.md)
- [GitHub Issues 使用说明](./.github/issues/README.md)

---

**创建时间**：2025-12-22  
**最后更新**：2025-12-22

