# 现场工单系统 - Web管理端

基于 React 18 + TypeScript + Ant Design 5 + Vite 构建的现代化Web管理平台。

## ⚡ 快速开始

### 1. 安装依赖
```bash
npm install
```

### 2. 启动开发服务器
```bash
npm run dev
```

开发服务器将在 http://localhost:5173 启动

### 3. 构建生产版本
```bash
npm run build
```

### 4. 预览生产版本
```bash
npm run preview
```

## 📁 项目结构

```
web-admin/
├── src/
│   ├── components/          # 公共组件
│   │   ├── AppLayout.tsx    # 主布局
│   │   ├── ErrorBoundary.tsx # 错误边界
│   │   └── GlobalLoading.tsx # 全局加载
│   ├── pages/               # 页面组件
│   │   ├── dashboard/       # 首页Dashboard
│   │   ├── tickets/         # 工单管理
│   │   ├── customers/       # 客户管理
│   │   ├── devices/         # 设备管理
│   │   ├── users/           # 用户管理
│   │   └── judgement-cards/ # 判断卡管理
│   ├── services/            # API服务层
│   │   ├── authService.ts
│   │   ├── customerService.ts
│   │   ├── deviceService.ts
│   │   ├── userManagementService.ts
│   │   └── judgementCardService.ts
│   ├── utils/               # 工具函数
│   │   └── request.ts       # 统一请求封装
│   ├── routes.tsx           # 路由配置
│   ├── App.tsx              # 应用根组件
│   └── main.tsx             # 应用入口
├── public/                  # 静态资源
├── .env.example             # 环境变量示例
├── vite.config.ts           # Vite配置
├── package.json             # 项目配置
├── README.md                # 本文件
└── DEPLOYMENT_GUIDE.md      # 部署指南（详细）
```

## 🚀 核心功能

### 已完成
- ✅ Dashboard 系统概览
- ✅ 工单管理（列表、看板、详情）
- ✅ 客户管理（CRUD）
- ✅ 设备管理（CRUD、配置快照、二维码）
- ✅ 用户管理（CRUD、角色管理）
- ✅ 判断卡管理（知识库）
- ✅ 全局错误处理
- ✅ 统一API服务层
- ✅ 认证授权

### 开发中
- 🔄 性能优化
- 🔄 单元测试
- 🔄 E2E测试

## 🛠 技术栈

- **框架**: React 18
- **语言**: TypeScript
- **UI库**: Ant Design 5
- **路由**: React Router 6
- **构建工具**: Vite 5
- **HTTP客户端**: Fetch API (封装)
- **状态管理**: React Hooks

## 🔧 开发工具

```bash
# 代码检查
npm run lint

# 类型检查
npx tsc --noEmit
```

## 🌐 环境配置

复制 `.env.example` 为 `.env.local` 并修改配置：

```bash
cp .env.example .env.local
```

主要配置项：
- `VITE_API_URL`: 后端API地址

## 📖 文档

- [部署指南](./DEPLOYMENT_GUIDE.md) - 详细的部署和远程访问指南
- [开发总结](./DEVELOPMENT_SUMMARY.md) - 功能开发总结

## 🔐 访问控制

默认登录凭据（开发环境）：
- 用户名：admin
- 密码：请联系管理员

## 🐛 常见问题

### 端口冲突
```bash
# 修改端口
VITE_PORT=3000 npm run dev
```

### API连接失败
1. 检查后端服务是否运行（端口5000）
2. 检查 `vite.config.ts` 中的proxy配置
3. 查看浏览器控制台网络请求

### 构建失败
```bash
# 清除依赖重新安装
rm -rf node_modules package-lock.json
npm install
```

## 📞 支持

遇到问题？请查看：
1. [部署指南](./DEPLOYMENT_GUIDE.md)
2. GitHub Issues
3. 联系开发团队

## 📄 许可证

Private - 仅供内部使用

---

**快速访问：**
- 开发环境：http://localhost:5173
- 生产环境：根据部署配置
