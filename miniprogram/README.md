# 企业微信小程序 - 现场问题反馈系统

## 📋 项目说明

这是现场问题反馈系统的企业微信小程序版本，实现与Web端和移动端App的数据录入功能完全打通。

## 🏗️ 项目结构

```
miniprogram/
├── app.json              # 小程序配置
├── app.ts                # 小程序入口
├── app.wxss              # 全局样式
├── project.config.json   # 项目配置
├── sitemap.json          # 站点地图配置
├── pages/                # 页面目录
│   ├── login/            # 登录页
│   ├── index/            # 首页
│   └── ticket/           # 工单相关页面
│       ├── list/         # 工单列表
│       └── create/       # 工单创建（5步流程）
├── components/           # 组件目录
├── services/             # 服务层
│   ├── api.ts           # API接口
│   ├── auth.ts          # 认证服务
│   └── storage.ts       # 本地存储
├── utils/                # 工具函数
│   ├── request.ts        # 请求封装
│   └── constants.ts      # 常量定义
└── types/                # 类型定义
    └── index.ts
```

## 🚀 快速开始

### 1. 配置项目

1. 修改 `project.config.json` 中的 `appid` 为你的企业微信小程序 AppID
2. 修改 `utils/constants.ts` 中的 `API_BASE_URL` 为你的后端API地址

### 2. 配置企业微信

1. 在企业微信管理后台创建小程序应用
2. 配置服务器域名白名单（API域名）
3. 配置小程序权限（相机、位置等）

### 3. 开发调试

1. 使用企业微信开发者工具打开项目
2. 配置企业微信开发者账号
3. 开始开发调试

## 📝 功能清单

### 已完成 ✅

- [x] 项目基础框架
- [x] 企业微信登录集成
- [x] API请求封装
- [x] 统一数据模型定义
- [x] 登录页面
- [x] 首页

### 待实现 ⏳

- [ ] 工单创建5步流程（Issue #042）
- [ ] 工单列表页面
- [ ] 验证结果提交（Issue #043）
- [ ] 客户沟通记录（Issue #043）

## 🔗 相关文档

- [企业微信小程序规划](../docs/WECHAT_MINIPROGRAM_PLAN.md)
- [Issue #041: 企业微信小程序基础框架](../.github/issues/sprint-2/041-企业微信小程序基础框架.md)
- [Issue #042: 小程序工单创建功能](../.github/issues/sprint-2/042-小程序工单创建功能.md)
- [Issue #043: 小程序验证和沟通功能](../.github/issues/sprint-3/043-小程序验证和沟通功能.md)

## 📝 注意事项

1. **平台标识**：所有API请求都会自动携带 `X-Platform: miniprogram` 头部
2. **Token管理**：Token自动存储在本地，过期自动刷新
3. **错误处理**：统一的错误处理和提示机制
4. **数据同步**：与Web/App端共用后端API，数据实时同步

## 🛠️ 技术栈

- 原生小程序框架
- TypeScript
- 企业微信小程序API

