# 现场问题反馈系统 - Flutter 移动端

## 📱 项目简介

这是现场问题反馈系统的移动端应用，使用 Flutter 开发，支持 Android 和 iOS 平台。

## 🚀 快速开始

### 前置要求

- Flutter SDK 3.0.0 或更高版本
- Dart SDK 3.0.0 或更高版本
- Android Studio / Xcode（用于运行模拟器）

### 安装依赖

```bash
cd mobile-app
flutter pub get
```

### 运行应用

```bash
# 在 Chrome 浏览器中运行（开发测试）
flutter run -d chrome

# 在 Android 模拟器中运行
flutter run -d android

# 在 iOS 模拟器中运行（需要 macOS）
flutter run -d ios

# 查看可用设备
flutter devices
```

### 配置后端 API 地址

编辑 `lib/services/auth_service.dart` 和 `lib/services/ticket_service.dart`，修改 `_apiBaseUrl`：

```dart
static const String _apiBaseUrl = 'http://your-server-ip:5000';
```

## 📁 项目结构

```
lib/
├── main.dart                    # 应用入口
├── models/                      # 数据模型
│   └── missing_info_models.dart
├── pages/                       # 页面
│   ├── home_page.dart          # 首页/工作台
│   ├── login_page.dart         # 登录页
│   └── ticket/                 # 工单相关页面
│       ├── create_ticket_page.dart
│       ├── ticket_detail_page.dart
│       ├── ticket_list_page.dart
│       └── missing_info_questionnaire_page.dart
├── providers/                   # 状态管理
│   ├── auth_provider.dart      # 认证状态
│   └── ticket_provider.dart    # 工单状态
└── services/                    # 服务层
    ├── auth_service.dart       # 认证服务
    └── ticket_service.dart     # 工单服务
```

## 🎯 主要功能

### 已完成 ✅

#### 用户认证
- ✅ 企业微信 OAuth2 登录
- ✅ Token 自动管理和刷新
- ✅ 用户信息展示
- ✅ 退出登录

#### 工单管理
- ✅ 工单列表展示（分页、下拉刷新、加载更多）
- ✅ 工单状态和优先级标签
- ✅ **完整的创建工单功能**
  - 客户和设备选择
  - 问题域选择（A/B/C/D/E）
  - 步骤代码和问题描述
  - 版本信息（自动填充）
  - 复现率、重启恢复等特征
  - 保存草稿功能
  - 表单验证
- ✅ **完整的工单详情展示**
  - 工单基本信息
  - 问题描述和措施
  - 版本信息
  - 事实项展示
  - 时间轴
  - 状态流转
  - 根据状态显示操作按钮
- ✅ 问诊式补全页面

#### 客户和设备
- ✅ 客户列表查询
- ✅ 设备列表查询（按客户筛选）
- ✅ 设备版本信息自动填充

#### 导航和交互
- ✅ 底部导航（工单、工作台、个人中心）
- ✅ 页面间导航
- ✅ 下拉刷新
- ✅ 错误处理和提示

#### 附件管理
- ✅ 附件上传功能（拍照、相册、视频、文档）
- ✅ 附件查看功能（网格展示、图片缩放）
- ✅ 附件删除功能
- ✅ 上传进度显示
- ✅ 文件大小验证和格式化显示
- ✅ 视频播放功能（完整控制器、进度条、播放/暂停）
- ✅ 附件下载功能（保存到本地、下载进度提示）

### 开发中 🔄

- 🔄 离线草稿支持
- 🔄 推送通知

### 计划中 📝

- 📝 二维码扫描（自动填充设备）
- 📝 工单筛选和搜索
- 📝 数据统计和报表
- 📝 暗黑模式
- 📝 消息通知中心

## 🔧 开发指南

### 添加新页面

1. 在 `lib/pages/` 创建新页面文件
2. 在 `lib/main.dart` 中添加路由配置
3. 使用 Provider 管理状态

### 添加新服务

1. 在 `lib/services/` 创建服务文件
2. 定义相关的 DTO 模型
3. 在对应的 Provider 中调用服务

### 状态管理

本项目使用 Provider 进行状态管理：

- `AuthProvider`: 管理用户登录状态
- `TicketProvider`: 管理工单数据

## 📦 打包发布

### Android APK

```bash
# Debug 版本
flutter build apk --debug

# Release 版本
flutter build apk --release

# 输出路径：build/app/outputs/flutter-apk/app-release.apk
```

### iOS IPA

```bash
# 需要在 macOS 上运行
flutter build ios --release

# 使用 Xcode 打开项目进行签名和发布
open ios/Runner.xcworkspace
```

## 🐛 常见问题

### 1. API 连接失败

- 检查后端服务是否启动
- 确认 API 地址配置正确
- 确认网络权限已配置

### 2. 企业微信登录失败

- 检查企业微信配置是否正确
- 确认回调地址已配置
- 查看控制台错误信息

### 3. 依赖安装失败

```bash
# 清理缓存
flutter clean
flutter pub cache repair
flutter pub get
```

## 📝 待办事项

- [x] 完善创建工单功能
- [x] 实现附件上传和查看
- [x] 实现视频播放
- [x] 实现附件下载
- [ ] 添加离线支持
- [ ] 优化 UI/UX
- [ ] 添加单元测试
- [ ] 添加集成测试
- [ ] 性能优化
- [ ] 错误处理完善

## 📄 License

[待定]

## 👥 贡献者

[待补充]

---

**当前状态**: 🔄 开发中
**最后更新**: 2025-12-27
