# 贡献指南

感谢您对本项目的关注！本文档将帮助您了解如何为项目做出贡献。

## 📋 开发流程

### 1. Fork 和 Clone

```bash
# Fork 仓库到您的 GitHub 账号
# 然后 clone 您的 fork
git clone https://github.com/YOUR_USERNAME/field-ticket-system.git
cd field-ticket-system
```

### 2. 创建分支

```bash
# 从 main 分支创建新分支
git checkout -b feature/your-feature-name
# 或
git checkout -b fix/your-bug-fix
```

### 3. 开发

- 遵循项目代码规范
- 编写单元测试
- 更新相关文档

### 4. 提交

```bash
git add .
git commit -m "feat: 添加新功能描述"
```

**提交信息规范**：
- `feat:` 新功能
- `fix:` Bug 修复
- `docs:` 文档更新
- `style:` 代码格式调整
- `refactor:` 代码重构
- `test:` 测试相关
- `chore:` 构建/工具相关

### 5. 推送和创建 PR

```bash
git push origin feature/your-feature-name
```

然后在 GitHub 上创建 Pull Request。

## 🎯 代码规范

### 后端 (.NET)

- 遵循 C# 编码规范
- 使用 `PascalCase` 命名类和方法
- 使用 `camelCase` 命名局部变量和参数
- 所有公共 API 必须有 XML 注释

### 前端 (React)

- 使用 TypeScript
- 组件使用 `PascalCase` 命名
- 函数和变量使用 `camelCase`
- 使用 ESLint 和 Prettier

### 移动端 (Flutter)

- 遵循 Dart 风格指南
- 使用 `PascalCase` 命名类
- 使用 `camelCase` 命名变量和函数

## 🧪 测试

在提交 PR 之前，请确保：

- [ ] 所有现有测试通过
- [ ] 新功能有对应的测试
- [ ] 代码覆盖率不降低

## 📝 Issue 和 PR

### 创建 Issue

- 使用合适的模板（Bug / Feature / Task）
- 提供清晰的问题描述
- 如果是 Bug，提供复现步骤

### 创建 PR

- 关联相关 Issue
- 提供清晰的变更描述
- 确保 CI 通过
- 请求代码审查

## 🤝 行为准则

- 尊重所有贡献者
- 接受建设性批评
- 专注于对项目最有利的事情
- 对其他社区成员表示同理心

## ❓ 需要帮助？

如果您有任何问题，可以：

- 创建 Issue 询问
- 查看项目文档
- 联系项目维护者

感谢您的贡献！🎉

