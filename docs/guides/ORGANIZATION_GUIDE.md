# 项目文件组织指南

> **日期**：2025-12-22  
> **说明**：本文档说明项目文件的组织结构和查找方式

---

## 📁 目录结构说明

### 根目录

**保留在根目录的文件**：
- `README.md` - 项目主文档
- `TASKS_SUMMARY.md` - 任务拆分总结
- `field-ticket-system-spec-final.md` - 完整开发规格书
- `docker-compose.yml` / `docker-compose.dev.yml` - Docker配置
- `env.example` - 环境变量示例
- `.gitignore` / `.dockerignore` - 忽略文件配置

### 文档目录 (`docs/`)

#### `docs/design/` - 设计文档
- 系统总体规划
- 模块详细设计
- 使用指南
- 建设方案PPT

#### `docs/setup/` - 设置和部署文档
- 项目设置指南
- GitHub设置指南
- Docker部署指南
- 推送代码指南

#### `docs/issues/` - Issue总结
- 各个Issue的总结文档

#### `docs/testing/` - 测试文档
- 测试指南
- 测试总结

#### `docs/contributing/` - 贡献指南
- 贡献者指南

#### `docs/` - 其他技术文档
- API文档
- 架构文档
- 开发指南
- 功能改进文档
- 平台覆盖说明
- 等等

### 脚本目录 (`scripts/`)

#### `scripts/github/` - GitHub相关脚本
- `push-to-github.sh` - 推送到GitHub
- `setup-git.sh` - Git初始化

#### `scripts/dev/` - 开发脚本
- `start.sh` - 启动脚本

#### `scripts/` - 其他脚本
- 模拟数据脚本
- 测试脚本
- 数据库初始化脚本
- 等等

### 文件目录 (`files/`)

#### `files/archives/` - 归档文件
- 压缩包、备份文件等

#### `files/demos/` - 演示文件
- 演示代码、示例文件等

---

## 🔍 快速查找

### 我想找...

**设计文档** → `docs/design/`
**设置指南** → `docs/setup/`
**测试文档** → `docs/testing/`
**Issue总结** → `docs/issues/`
**API文档** → `docs/API_DOCUMENTATION.md`
**架构文档** → `docs/ARCHITECTURE_V2.md`
**开发指南** → `docs/DEVELOPMENT_GUIDE.md`
**GitHub脚本** → `scripts/github/`
**开发脚本** → `scripts/dev/`

---

## 📝 文件命名规范

### 文档文件
- 使用中文或英文命名
- Markdown文件使用 `.md` 扩展名
- 保持命名清晰、描述性

### 脚本文件
- 使用英文命名
- Shell脚本使用 `.sh` 扩展名
- 功能描述清晰

---

## 🔄 文件移动记录

如果文件被移动，可以在以下文档中查找：
- `docs/FILE_ORGANIZATION_PLAN.md` - 文件分类整理方案
- `docs/FILE_ORGANIZATION_SUMMARY.md` - 文件分类整理总结

---

**最后更新**：2025-12-22



