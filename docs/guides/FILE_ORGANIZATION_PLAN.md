# 文件分类整理方案

> **日期**：2025-12-22  
> **目标**：整理项目根目录，将文件按类型和用途分类

---

## 📋 当前问题

项目根目录下文件较多，包括：
- 各种README文档
- Issue总结文件
- 设计文档
- 脚本文件
- Docker配置文件
- 其他临时文件

---

## 🗂️ 分类方案

### 1. 根目录保留文件

**必须保留在根目录**：
- `README.md` - 项目主README
- `docker-compose.yml` - Docker Compose配置（生产环境）
- `docker-compose.dev.yml` - Docker Compose配置（开发环境）
- `.gitignore` - Git忽略文件
- `.dockerignore` - Docker忽略文件
- `env.example` - 环境变量示例

### 2. 文档分类

#### `docs/` - 已存在，继续使用
- 所有技术文档、架构文档、API文档等

#### `docs/design/` - 新增：设计文档
- `客服系统总体规划.md`
- `客户和设备模块详细设计.md`
- `问题管理模块详细设计.md`
- `完整文档包使用指南.md`
- `快速启动.md`
- `非标自动化设备客服系统建设方案.pptx`

#### `docs/setup/` - 新增：设置和部署文档
- `README_SETUP.md` → `docs/setup/README.md`
- `README_DEPLOYMENT.md` → `docs/setup/DEPLOYMENT.md`
- `SETUP_GITHUB.md` → `docs/setup/GITHUB.md`
- `SETUP_GITHUB_COMPLETE.md` → `docs/setup/GITHUB_COMPLETE.md`
- `PUSH_TO_GITHUB.md` → `docs/setup/PUSH_TO_GITHUB.md`
- `DOCKER_DEPLOYMENT.md` → `docs/setup/DOCKER_DEPLOYMENT.md`

#### `docs/issues/` - 新增：Issue总结文档
- `ISSUE_001_SUMMARY.md`
- `ISSUE_002_SUMMARY.md`
- `ISSUE_003_SUMMARY.md`

#### `docs/testing/` - 新增：测试文档
- `TESTING.md`
- `TEST_SUMMARY.md`

### 3. 脚本分类

#### `scripts/` - 已存在，继续使用
- 所有脚本文件移动到 `scripts/` 目录

**需要移动的脚本**：
- `push-to-github.sh` → `scripts/github/push-to-github.sh`
- `setup-git.sh` → `scripts/github/setup-git.sh`
- `start.sh` → `scripts/dev/start.sh`

### 4. 其他文件

#### `files/` - 已存在，继续使用
- `files.zip` → `files/archives/files.zip`
- `field-ticket-demo.jsx` → `files/demos/field-ticket-demo.jsx`

#### `docs/contributing/` - 新增：贡献指南
- `CONTRIBUTING.md` → `docs/contributing/CONTRIBUTING.md`

---

## 📁 最终目录结构

```
项目根目录/
├── README.md                    # 项目主README（保留）
├── docker-compose.yml           # Docker配置（保留）
├── docker-compose.dev.yml       # Docker开发配置（保留）
├── env.example                  # 环境变量示例（保留）
├── .gitignore                   # Git忽略（保留）
├── .dockerignore               # Docker忽略（保留）
│
├── docs/                        # 文档目录
│   ├── design/                 # 设计文档
│   │   ├── 客服系统总体规划.md
│   │   ├── 客户和设备模块详细设计.md
│   │   ├── 问题管理模块详细设计.md
│   │   ├── 完整文档包使用指南.md
│   │   ├── 快速启动.md
│   │   └── 非标自动化设备客服系统建设方案.pptx
│   │
│   ├── setup/                  # 设置和部署文档
│   │   ├── README.md
│   │   ├── DEPLOYMENT.md
│   │   ├── GITHUB.md
│   │   ├── GITHUB_COMPLETE.md
│   │   ├── PUSH_TO_GITHUB.md
│   │   └── DOCKER_DEPLOYMENT.md
│   │
│   ├── issues/                 # Issue总结
│   │   ├── ISSUE_001_SUMMARY.md
│   │   ├── ISSUE_002_SUMMARY.md
│   │   └── ISSUE_003_SUMMARY.md
│   │
│   ├── testing/                # 测试文档
│   │   ├── TESTING.md
│   │   └── TEST_SUMMARY.md
│   │
│   ├── contributing/           # 贡献指南
│   │   └── CONTRIBUTING.md
│   │
│   └── [其他现有文档]          # API文档、架构文档等
│
├── scripts/                     # 脚本目录
│   ├── github/                 # GitHub相关脚本
│   │   ├── push-to-github.sh
│   │   └── setup-git.sh
│   │
│   ├── dev/                    # 开发脚本
│   │   └── start.sh
│   │
│   └── [其他现有脚本]          # 模拟数据、测试脚本等
│
├── files/                       # 文件目录
│   ├── archives/               # 归档文件
│   │   └── files.zip
│   │
│   └── demos/                  # 演示文件
│       └── field-ticket-demo.jsx
│
├── backend/                     # 后端代码
├── web-admin/                   # Web前端代码
├── mobile-app/                  # 移动端代码
└── .github/                     # GitHub配置
```

---

## 🔄 执行步骤

1. 创建新目录结构
2. 移动文件到对应目录
3. 更新README.md中的链接（如果有）
4. 更新.gitignore（如果需要）

---

**最后更新**：2025-12-22



