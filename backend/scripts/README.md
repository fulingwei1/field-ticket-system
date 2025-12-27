# 数据库脚本和初始化工具

本目录包含数据库迁移脚本和初始化工具。

## 📋 文件说明

### SQL脚本

#### 1. `001_add_password_login_support.sql`
**用途：** 更新User表结构，添加用户名密码登录支持

**执行时机：** 首次部署或从纯企业微信登录升级时

**执行方法：**
```bash
# PostgreSQL
psql -U postgres -d field_ticket -f 001_add_password_login_support.sql

# 或使用数据库管理工具执行
```

**包含内容：**
- 添加Username、PasswordHash、Email字段
- 添加LoginType、LastPasswordChangeAt、MustChangePassword字段
- 更新CorpId和WeComUserId为可空
- 创建必要的索引和约束

#### 2. `002_init_admin_user.sql`
**用途：** 初始化管理员用户（SQL版本）

**注意：** 此脚本中的密码哈希是占位符，需要使用C#脚本或程序生成实际哈希。

---

### C#脚本

#### 3. `init-admin.csx`
**用途：** 创建管理员用户（推荐方式）

**依赖：**
- .NET SDK 7.0+
- dotnet-script工具

**安装dotnet-script：**
```bash
dotnet tool install -g dotnet-script
```

**使用方法：**

**方式1：使用默认凭据**
```bash
cd /home/user/field-ticket-system/backend/scripts
dotnet script init-admin.csx
```

默认凭据：
- 用户名：admin
- 密码：Admin123!

**方式2：自定义凭据**
```bash
dotnet script init-admin.csx myusername MySecureP@ss123
```

**配置数据库连接：**

脚本会按以下顺序查找数据库连接：
1. 环境变量 `DATABASE_URL`
2. 默认连接：`Host=localhost;Database=field_ticket;Username=postgres;Password=postgres`

设置环境变量：
```bash
export DATABASE_URL="Host=localhost;Database=field_ticket;Username=postgres;Password=yourpassword"
dotnet script init-admin.csx
```

**输出示例：**
```
========================================
  现场工单系统 - 管理员初始化工具
========================================

✅ 已连接到数据库
🔐 正在生成密码哈希...
✅ 密码哈希已生成

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
  4. 建议删除或加密此脚本
```

---

## 🚀 快速开始

### 首次部署完整流程

**1. 执行数据库迁移**
```bash
cd /home/user/field-ticket-system/backend/scripts

# 更新表结构
psql -U postgres -d field_ticket -f 001_add_password_login_support.sql
```

**2. 创建管理员用户**
```bash
# 安装dotnet-script（如果未安装）
dotnet tool install -g dotnet-script

# 设置数据库连接（可选）
export DATABASE_URL="Host=localhost;Database=field_ticket;Username=postgres;Password=postgres"

# 运行初始化脚本
dotnet script init-admin.csx
```

**3. 验证**
```bash
# 检查用户是否创建成功
psql -U postgres -d field_ticket -c "SELECT \"Username\", \"Name\", \"Role\", \"LoginType\" FROM \"Users\" WHERE \"Username\" = 'admin';"
```

**4. 登录系统**
- 访问Web管理端
- 使用创建的用户名和密码登录
- 首次登录后修改密码

---

## 🔒 安全建议

### 生产环境部署

1. **修改默认密码**
   - 不要使用默认的 `Admin123!`
   - 使用强密码（至少12位，包含大小写字母、数字、特殊字符）

2. **保护脚本文件**
   ```bash
   # 删除初始化脚本（生产环境）
   rm init-admin.csx

   # 或设置严格权限
   chmod 600 init-admin.csx
   ```

3. **使用环境变量**
   - 不要在脚本中硬编码数据库密码
   - 使用环境变量或配置管理工具

4. **日志审计**
   - 记录管理员账户创建操作
   - 定期审计用户账户

---

## 🛠 故障排除

### 问题1：脚本执行失败
```
❌ 错误: 42P01: relation "Users" does not exist
```

**解决方案：**
1. 确保先执行了表结构更新脚本
2. 检查数据库连接是否正确

### 问题2：用户已存在
```
⚠️  用户名 'admin' 已存在！
```

**解决方案：**
- 脚本会询问是否覆盖
- 输入 `y` 覆盖现有用户
- 输入 `n` 取消操作

### 问题3：dotnet-script未安装
```
bash: dotnet script: command not found
```

**解决方案：**
```bash
dotnet tool install -g dotnet-script

# 确保工具路径在PATH中
export PATH="$PATH:$HOME/.dotnet/tools"
```

### 问题4：数据库连接失败
```
❌ 错误: could not connect to server
```

**解决方案：**
1. 检查PostgreSQL服务是否运行
2. 验证连接字符串是否正确
3. 检查防火墙设置

---

## 📞 需要帮助？

- 查看主文档：`/home/user/field-ticket-system/README.md`
- 查看部署指南：`/home/user/field-ticket-system/web-admin/DEPLOYMENT_GUIDE.md`
- 联系开发团队

---

**最后更新：** 2025-12-27
