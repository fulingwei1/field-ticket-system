# 🚀 Web管理端快速启动指南

## 📋 目录
1. [环境要求](#环境要求)
2. [快速启动（3步）](#快速启动3步)
3. [账户访问说明](#账户访问说明)
4. [常见问题](#常见问题)

---

## 🔧 环境要求

### 必需软件
- **Node.js**: 16.x 或更高版本
- **.NET SDK**: 7.0 或更高版本
- **PostgreSQL**: 13.x 或更高版本

### 检查是否已安装
```bash
# 检查Node.js版本
node --version
# 应该显示: v16.x.x 或更高

# 检查npm版本
npm --version

# 检查.NET SDK版本
dotnet --version
# 应该显示: 7.0.x 或更高

# 检查PostgreSQL
psql --version
```

---

## ⚡ 快速启动（3步）

### 第1步：启动后端API服务器

```bash
# 进入后端目录
cd /home/user/field-ticket-system/backend

# 启动后端（默认端口5000）
dotnet run --project src/FieldTicket.Api/FieldTicket.Api.csproj
```

**成功标志：**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 第2步：初始化管理员账户

**打开新终端**，运行：

```bash
# 进入脚本目录
cd /home/user/field-ticket-system/backend/scripts

# 执行初始化脚本（使用默认账户）
dotnet script init-admin.csx
```

**输出示例：**
```
========================================
✅ 管理员账户创建成功！
========================================

用户名: admin
密码:   Admin123!
角色:   Admin

⚠️  重要提示:
  1. 请立即使用上述凭据登录系统
  2. 首次登录后会要求修改密码
  3. 请妥善保管新密码
```

**注意：** 如果提示管理员已存在，输入 `y` 覆盖即可。

### 第3步：启动Web管理端

**再打开一个新终端**，运行：

```bash
# 进入Web管理端目录
cd /home/user/field-ticket-system/web-admin

# 使用启动脚本（自动安装依赖 + 启动）
./start.sh
```

**或手动启动：**
```bash
# 首次运行需要安装依赖
npm install

# 启动开发服务器
npm run dev
```

**成功标志：**
```
  VITE v5.x.x  ready in xxx ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
  ➜  press h + enter to show help
```

### 第4步：访问系统

打开浏览器访问：
```
http://localhost:5173
```

---

## 🔐 账户访问说明

### 🎯 重要：不需要注册！

系统采用**管理员创建用户**的方式，不支持自助注册。

### 默认管理员账户

| 项目 | 值 |
|------|------|
| **用户名** | `admin` |
| **密码** | `Admin123!` |
| **角色** | 系统管理员 |
| **首次登录** | 必须修改密码 |

### 登录流程

1. **访问登录页**
   ```
   http://localhost:5173/login
   ```

2. **输入凭据**
   - 用户名：`admin`
   - 密码：`Admin123!`
   - 可选：勾选"记住我（30天）"

3. **首次登录**
   - 系统自动检测到首次登录
   - 跳转到修改密码页面
   - 填写新密码（至少8位，包含大小写字母和数字）
   - 示例：`MyNewPass123`

4. **重新登录**
   - 使用新密码登录
   - 进入系统首页

---

## 👥 其他用户如何登录？

### 方式1：管理员通过Web界面创建

1. 管理员登录系统
2. 点击左侧菜单 **"用户管理"**
3. 点击右上角 **"创建用户"** 按钮
4. 填写表单：
   - 用户名（登录账号）
   - 姓名
   - 初始密码
   - 角色（现场工程师/客服/高级工程师）
   - 邮箱、手机号（可选）
5. 点击"创建"
6. 将用户名和初始密码告知该用户
7. 用户首次登录后必须修改密码

### 方式2：通过API创建（适合批量）

```bash
# 获取管理员Token（先登录获取）
TOKEN="your_admin_token"

# 创建新用户
curl -X POST http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "engineer1",
    "name": "张三",
    "password": "TempPass123",
    "role": "FieldEngineer",
    "email": "zhang@example.com",
    "mobile": "13800138001"
  }'
```

### 用户角色说明

| 角色 | 英文名 | 权限 |
|------|--------|------|
| 系统管理员 | Admin | 所有权限，用户管理 |
| 现场工程师 | FieldEngineer | 创建工单、上传证据、验证方案 |
| 高级工程师 | SeniorEngineer | 分诊、创建解决方案 |
| 客服 | CS | 创建工单、客户沟通 |

---

## ❓ 常见问题

### Q1: 启动后端时提示端口5000已被占用

**解决方案：**
```bash
# 查找占用端口的进程
lsof -i :5000

# 杀死进程（替换PID为实际进程ID）
kill -9 <PID>

# 或者修改端口
# 编辑 backend/src/FieldTicket.Api/appsettings.json
# 修改 "Urls": "http://localhost:5001"
```

### Q2: Web端启动失败，提示依赖错误

**解决方案：**
```bash
cd /home/user/field-ticket-system/web-admin

# 删除node_modules和lock文件
rm -rf node_modules package-lock.json

# 重新安装
npm install

# 启动
npm run dev
```

### Q3: 浏览器访问localhost:5173显示空白

**解决方案：**

1. **检查控制台错误**
   - 按F12打开开发者工具
   - 查看Console标签的错误信息

2. **检查API连接**
   - 确保后端已启动（localhost:5000）
   - 检查.env文件中的API地址

3. **清除浏览器缓存**
   - Ctrl+Shift+Delete
   - 清除缓存和Cookie

### Q4: 登录后显示"未登录或登录已过期"

**解决方案：**

1. **检查后端是否运行**
   ```bash
   curl http://localhost:5000/api/health
   ```

2. **检查API地址配置**
   ```bash
   cat /home/user/field-ticket-system/web-admin/.env
   # 应该显示: VITE_API_URL=http://localhost:5000
   ```

3. **查看浏览器Network标签**
   - F12 → Network → 查看失败的请求

### Q5: 忘记管理员密码

**解决方案：**
```bash
cd /home/user/field-ticket-system/backend/scripts

# 重新运行初始化脚本
dotnet script init-admin.csx

# 提示覆盖时输入 y
# 密码会重置为 Admin123!
```

---

## 🌐 远程访问（局域网内其他人访问）

### 方式1：使用本机IP

```bash
# 1. 获取本机IP
ip addr show | grep "inet " | grep -v 127.0.0.1

# 假设输出: inet 192.168.1.100/24

# 2. 启动Web端时允许外部访问
cd /home/user/field-ticket-system/web-admin
npm run dev -- --host

# 3. 其他人访问
# 浏览器打开: http://192.168.1.100:5173
```

### 方式2：使用ngrok（临时公网访问）

```bash
# 1. 安装ngrok
# 访问 https://ngrok.com 下载安装

# 2. 启动Web端
cd /home/user/field-ticket-system/web-admin
npm run dev

# 3. 在另一个终端启动ngrok
ngrok http 5173

# 4. 使用ngrok提供的URL访问
# 例如: https://abc123.ngrok.io
```

**注意：** 如果使用远程访问，需要同时配置后端允许跨域访问。

---

## 📝 完整启动流程总结

```bash
# === 终端1：启动后端 ===
cd /home/user/field-ticket-system/backend
dotnet run --project src/FieldTicket.Api/FieldTicket.Api.csproj

# === 终端2：初始化管理员（仅首次） ===
cd /home/user/field-ticket-system/backend/scripts
dotnet script init-admin.csx

# === 终端3：启动Web管理端 ===
cd /home/user/field-ticket-system/web-admin
./start.sh

# === 浏览器 ===
# 访问: http://localhost:5173
# 用户名: admin
# 密码: Admin123!
# 首次登录后修改密码
```

---

## 🎯 快速检查清单

启动前检查：
- [ ] PostgreSQL已启动
- [ ] 后端端口5000未被占用
- [ ] Web端口5173未被占用
- [ ] .env文件已创建
- [ ] 管理员账户已初始化

运行中检查：
- [ ] 后端正常运行（终端显示"Now listening on"）
- [ ] Web端正常运行（终端显示"Local: http://localhost:5173"）
- [ ] 浏览器能访问登录页
- [ ] 能用admin/Admin123!登录
- [ ] 首次登录能修改密码

---

## 📞 需要帮助？

- 查看详细文档：`/home/user/field-ticket-system/LOGIN_GUIDE.md`
- 查看部署指南：`/home/user/field-ticket-system/DEPLOYMENT_GUIDE.md`
- 后端API文档：访问 http://localhost:5000/swagger

---

**最后更新：** 2025-12-27
**版本：** 1.0.0
