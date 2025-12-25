# 项目设置指南

> 本文档提供项目设置的完整指南

## 📚 相关文档

- [GitHub设置指南](./GITHUB.md) - GitHub仓库设置
- [GitHub完整设置](./GITHUB_COMPLETE.md) - 一键设置所有GitHub功能
- [推送到GitHub](./PUSH_TO_GITHUB.md) - 如何推送代码到GitHub
- [部署指南](./DEPLOYMENT.md) - 生产环境部署
- [Docker部署](./DOCKER_DEPLOYMENT.md) - Docker部署说明

## 🚀 快速开始

1. **环境准备**
   - 安装 Docker 和 Docker Compose
   - 安装 Node.js 和 npm
   - 安装 .NET 8 SDK

2. **克隆仓库**
   ```bash
   git clone <repository-url>
   cd 非标自动化客服现场问题反馈系统
   ```

3. **配置环境变量**
   ```bash
   cp env.example .env
   # 编辑 .env 文件，填入配置
   ```

4. **启动服务**
   ```bash
   docker-compose up -d
   ```

5. **访问应用**
   - Web管理端：http://localhost:3000
   - API：http://localhost:5000

## 📖 详细文档

请查看各个子文档获取更详细的设置说明。

---

**最后更新**：2025-12-22
