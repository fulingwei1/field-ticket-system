# 🔐 登录和账户管理完整指南

本指南介绍如何使用双登录系统（用户名密码 + 企业微信）以及账户管理功能。

---

## 📋 目录

1. [快速开始](#快速开始)
2. [初始化管理员账户](#初始化管理员账户)
3. [用户登录方式](#用户登录方式)
4. [首次登录修改密码](#首次登录修改密码)
5. [管理员创建新用户](#管理员创建新用户)
6. [企业微信登录配置](#企业微信登录配置)
7. [常见问题](#常见问题)

---

## 🚀 快速开始

### 最快上手流程（5分钟）

```bash
# 1. 启动后端API（端口5000）
cd backend
dotnet run

# 2. 初始化管理员账户
cd backend/scripts
dotnet script init-admin.csx

# 3. 启动Web管理端（端口5173）
cd ../../web-admin
./start.sh

# 4. 访问系统
# 浏览器打开: http://localhost:5173
# 用户名: admin
# 密码: Admin123!
```

---

## 👤 初始化管理员账户

### 方式1：使用C#脚本（推荐）

#### 前置条件
```bash
# 安装.NET SDK 7.0+
# 安装dotnet-script
dotnet tool install -g dotnet-script
```

#### 执行步骤

**1. 配置数据库连接（可选）**
```bash
# Linux/Mac
export DATABASE_URL="Host=localhost;Database=field_ticket;Username=postgres;Password=yourpassword"

# Windows PowerShell
$env:DATABASE_URL="Host=localhost;Database=field_ticket;Username=postgres;Password=yourpassword"
```

**2. 运行初始化脚本**
```bash
cd /home/user/field-ticket-system/backend/scripts

# 使用默认凭据（admin / Admin123!）
dotnet script init-admin.csx

# 或指定自定义凭据
dotnet script init-admin.csx myusername MySecureP@ss123
```

**3. 查看输出**
```
========================================
✅ 管理员账户创建成功！
========================================

用户ID: 12345678-1234-1234-1234-123456789abc
用户名: admin
密码:   Admin123!
姓名:   系统管理员
邮箱:   admin@example.com
角色:   Admin

⚠️  重要提示:
  1. 请立即使用上述凭据登录系统
  2. 首次登录后会要求修改密码
  3. 请妥善保管新密码
```

### 方式2：手动SQL（备用）

```sql
-- 1. 先执行表结构更新
psql -U postgres -d field_ticket -f 001_add_password_login_support.sql

-- 2. 使用程序生成密码哈希后手动插入
-- 详见 002_init_admin_user.sql
```

---

## 🔑 用户登录方式

系统支持**两种登录方式**，可以在登录页面Tab切换。

### 方式1：用户名密码登录（默认）

#### 登录步骤

1. **访问登录页**
   ```
   http://localhost:5173/login
   或
   http://服务器IP:5173/login
   ```

2. **选择"账号登录"Tab**（默认已选中）

3. **输入凭据**
   - 用户名：admin（或其他用户名）
   - 密码：Admin123!（或你的密码）
   - ☑️ 记住我（30天）- 可选

4. **点击"登录"按钮**

5. **首次登录**
   - 系统检测到首次登录
   - 自动跳转到修改密码页面
   - 必须修改密码后才能继续

6. **登录成功**
   - 跳转到系统首页
   - 显示Dashboard概览

#### 默认管理员账户

| 字段 | 值 |
|------|------|
| 用户名 | admin |
| 密码 | Admin123! |
| 角色 | Admin（系统管理员） |
| 首次登录 | 必须修改密码 |

---

### 方式2：企业微信登录

#### 前置条件

- 企业已开通企业微信
- 有企业微信管理员权限
- 已配置企业微信自建应用

#### 登录步骤

1. **访问登录页**

2. **选择"企业微信"Tab**

3. **点击"使用企业微信登录"**

4. **扫码授权**
   - 使用企业微信扫描二维码
   - 或自动跳转到企业微信授权页

5. **确认授权**

6. **自动登录**
   - 首次登录会自动创建账户
   - 同步企业微信用户信息
   - 跳转到系统首页

#### 配置企业微信

详见：[企业微信配置指南](#企业微信登录配置)

---

## 🔐 首次登录修改密码

### 自动检测

系统会自动检测首次登录用户，强制要求修改密码。

### 修改流程

1. **首次登录后自动跳转**
   ```
   http://localhost:5173/change-password
   ```

2. **填写表单**
   - **当前密码**：Admin123!（或初始密码）
   - **新密码**：输入新密码
   - **确认新密码**：再次输入新密码

3. **密码要求**
   - ✅ 至少8个字符
   - ✅ 必须包含大写字母
   - ✅ 必须包含小写字母
   - ✅ 必须包含数字
   - 示例：`MyNewP@ss123`

4. **提交修改**
   - 点击"确认修改"
   - 显示"密码修改成功"
   - 自动清除登录状态
   - 跳转回登录页

5. **使用新密码登录**

---

## 👥 管理员创建新用户

### 通过Web界面（开发中）

目前可以通过API或数据库直接创建。

### 通过API创建

```bash
# 获取管理员Token（先登录）
TOKEN="your_admin_token"

# 创建新用户
curl -X POST http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "engineer1",
    "name": "现场工程师1",
    "password": "TempP@ss123",
    "role": "FieldEngineer",
    "email": "engineer1@example.com",
    "mobile": "13800138000",
    "loginType": "Password"
  }'
```

### 通过SQL创建

```sql
-- 注意：密码需要先通过BCrypt加密
INSERT INTO "Users" (
    "Id",
    "Username",
    "Name",
    "PasswordHash",
    "Role",
    "LoginType",
    "Email",
    "IsActive",
    "MustChangePassword",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'engineer1',
    '现场工程师1',
    -- 需要使用BCrypt加密的密码
    '$2a$11$...',
    'FieldEngineer',
    'Password',
    'engineer1@example.com',
    true,
    true,  -- 首次登录强制修改密码
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);
```

### 用户角色说明

| 角色 | 说明 | 权限 |
|------|------|------|
| Admin | 系统管理员 | 所有权限 |
| SeniorEngineer | 高级工程师 | 分诊、解决方案 |
| FieldEngineer | 现场工程师 | 创建工单、验证 |
| CS | 客服 | 查看、跟进工单 |

---

## 🏢 企业微信登录配置

### 1. 创建企业微信自建应用

#### 步骤

1. **登录企业微信管理后台**
   ```
   https://work.weixin.qq.com/
   ```

2. **创建应用**
   - 应用管理 → 应用 → 创建应用
   - 应用名称：现场工单管理系统
   - 应用logo：上传logo
   - 应用介绍：描述系统功能

3. **记录配置信息**
   - AgentId：应用ID
   - CorpId：企业ID
   - Secret：应用Secret

4. **配置网页授权回调域名**
   - 在应用详情页
   - 网页授权及JS-SDK → 设置可信域名
   - 添加：`yourdomain.com`（你的服务器域名）

### 2. 配置后端环境变量

编辑 `.env` 或环境变量：

```bash
# 企业微信配置
WECOM_CORP_ID=ww1234567890abcdef
WECOM_AGENT_ID=1000002
WECOM_SECRET=your_secret_here
WECOM_REDIRECT_URI=https://yourdomain.com/login

# 或本地测试
WECOM_REDIRECT_URI=http://localhost:5173/login
```

### 3. 更新配置文件

编辑 `backend/src/FieldTicket.Api/appsettings.json`：

```json
{
  "WeCom": {
    "CorpId": "ww1234567890abcdef",
    "AgentId": "1000002",
    "Secret": "your_secret_here",
    "RedirectUri": "https://yourdomain.com/login"
  }
}
```

### 4. 重启后端服务

```bash
cd backend
dotnet run
```

### 5. 测试企业微信登录

1. 访问登录页
2. 切换到"企业微信"Tab
3. 点击"使用企业微信登录"
4. 扫码或授权
5. 验证是否成功登录

---

## ❓ 常见问题

### Q1: 忘记管理员密码怎么办？

**解决方案：**

**方式1：重新运行初始化脚本**
```bash
cd backend/scripts
dotnet script init-admin.csx

# 脚本会询问是否覆盖现有用户
# 输入 y 确认
```

**方式2：通过SQL重置**
```sql
-- 生成新密码哈希（使用在线BCrypt工具或程序）
-- 假设新密码为 NewP@ss123

UPDATE "Users"
SET "PasswordHash" = '$2a$11$新的BCrypt哈希...',
    "MustChangePassword" = true
WHERE "Username" = 'admin';
```

---

### Q2: 登录提示"用户名或密码错误"

**检查清单：**

1. ✅ 确认用户名拼写正确
2. ✅ 密码区分大小写
3. ✅ 检查数据库中是否存在该用户
4. ✅ 确认后端API正在运行（端口5000）
5. ✅ 查看后端日志

**验证用户是否存在：**
```bash
psql -U postgres -d field_ticket -c \
  "SELECT \"Username\", \"Name\", \"IsActive\" FROM \"Users\" WHERE \"Username\" = 'admin';"
```

---

### Q3: 首次登录没有跳转到修改密码页面

**可能原因：**

1. 数据库中`MustChangePassword`字段为`false`
2. 前端代码版本过旧

**解决方案：**
```sql
UPDATE "Users"
SET "MustChangePassword" = true
WHERE "Username" = 'admin';
```

---

### Q4: 修改密码提示"密码强度不够"

**密码要求：**

- ✅ 至少8个字符
- ✅ 包含大写字母（A-Z）
- ✅ 包含小写字母（a-z）
- ✅ 包含数字（0-9）

**合格示例：**
- `MyNewP@ss123` ✅
- `SecurePass2024` ✅
- `Admin@123` ❌（少于8位）
- `password123` ❌（缺少大写字母）
- `PASSWORD123` ❌（缺少小写字母）

---

### Q5: 企业微信登录失败

**检查清单：**

1. ✅ 企业微信应用已创建
2. ✅ CorpId、AgentId、Secret配置正确
3. ✅ 回调域名已添加到可信域名
4. ✅ 域名可以访问（非localhost）
5. ✅ 用户已加入企业微信

**查看后端日志：**
```bash
# 后端日志会显示OAuth错误详情
tail -f backend/logs/app.log
```

---

### Q6: 如何切换登录方式？

**用户名密码登录 → 企业微信登录：**

用户可以同时拥有两种登录方式。管理员需要：

1. 绑定企业微信ID到用户账户
2. 修改用户的`LoginType`为`WeCom`或保持`Password`

**暂不支持自助切换**，需要管理员操作。

---

### Q7: "记住我"功能如何工作？

| 选项 | Token有效期 |
|------|-------------|
| 不勾选 | 7天 |
| 勾选 | 30天 |

**注意：**
- Token过期后需要重新登录
- 修改密码后Token立即失效
- 退出登录会清除Token

---

### Q8: 如何批量创建用户？

**方式1：准备CSV文件**

```csv
username,name,password,role,email,mobile
engineer1,张三,TempP@ss123,FieldEngineer,zhang@example.com,13800138001
engineer2,李四,TempP@ss123,FieldEngineer,li@example.com,13800138002
cs1,王五,TempP@ss123,CS,wang@example.com,13800138003
```

**方式2：编写导入脚本**

```bash
# 使用init-admin.csx作为模板
# 修改为循环读取CSV并创建用户
```

**方式3：通过API批量创建**

```bash
# 编写Shell脚本调用API
for user in users.csv; do
  curl -X POST http://localhost:5000/api/users \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d @user.json
done
```

---

## 📞 技术支持

### 查看日志

**后端日志：**
```bash
tail -f backend/logs/app.log
```

**前端控制台：**
```
浏览器F12 → Console标签
```

### 联系方式

- **文档**：查看 `/README.md`、`/DEPLOYMENT_GUIDE.md`
- **Issues**：提交问题到GitHub
- **技术支持**：联系开发团队

---

## 🎯 最佳实践

### 安全建议

1. **立即修改默认密码**
   - 不要使用`Admin123!`
   - 使用强密码管理器

2. **定期更换密码**
   - 建议3-6个月更换一次
   - 使用不同的密码

3. **启用HTTPS**
   - 生产环境必须使用HTTPS
   - 使用Let's Encrypt免费证书

4. **限制登录尝试**
   - 配置防火墙
   - 使用fail2ban

5. **审计日志**
   - 定期检查登录日志
   - 发现异常及时处理

### 用户管理建议

1. **使用最小权限原则**
   - 只授予必要的权限
   - 定期审查用户权限

2. **及时禁用离职用户**
   ```sql
   UPDATE "Users"
   SET "IsActive" = false
   WHERE "Username" = 'former_employee';
   ```

3. **为不同角色创建专用账户**
   - 不要所有人都用管理员账户
   - 根据职责分配角色

---

**最后更新：** 2025-12-27
**版本：** 1.0.0
