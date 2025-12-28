# ✅ 员工批量导入功能 - 启动和测试检查清单

本文档提供完整的启动步骤和测试检查清单，确保所有功能正常运行。

---

## 📋 目录

1. [环境准备检查清单](#环境准备检查清单)
2. [数据库初始化清单](#数据库初始化清单)
3. [后端启动清单](#后端启动清单)
4. [前端启动清单](#前端启动清单)
5. [功能验证清单](#功能验证清单)
6. [故障排查指南](#故障排查指南)

---

## 🔧 环境准备检查清单

### 必要软件安装

- [ ] **PostgreSQL** (版本 16+)
  ```bash
  psql --version
  # 预期输出: psql (PostgreSQL) 16.x
  ```

- [ ] **.NET SDK** (版本 8.0+)
  ```bash
  dotnet --version
  # 预期输出: 8.0.x
  ```

- [ ] **Node.js** (版本 18+)
  ```bash
  node --version
  # 预期输出: v18.x.x 或更高
  ```

- [ ] **npm** (版本 9+)
  ```bash
  npm --version
  # 预期输出: 9.x.x 或更高
  ```

- [ ] **Git**
  ```bash
  git --version
  # 预期输出: git version 2.x.x
  ```

---

## 🗄️ 数据库初始化清单

### 步骤 1: 启动数据库服务

**选项 A: 使用Docker (推荐)**
```bash
cd /home/user/field-ticket-system
docker compose up -d postgres

# 验证状态
docker ps | grep postgres
# 预期输出: 显示postgres容器正在运行
```

**选项 B: 使用本地PostgreSQL**
```bash
# Linux
sudo systemctl start postgresql
sudo systemctl status postgresql

# macOS
brew services start postgresql

# Windows
# 通过服务管理器启动PostgreSQL服务
```

- [ ] 数据库服务已启动
- [ ] 数据库端口可访问 (默认5432)

---

### 步骤 2: 创建数据库 (如果不存在)

```bash
# 连接PostgreSQL
psql -U postgres

# 创建数据库
CREATE DATABASE fieldticket;

# 创建应用用户
CREATE USER app WITH PASSWORD 'your_password_here';

# 授予权限
GRANT ALL PRIVILEGES ON DATABASE fieldticket TO app;

# 退出
\q
```

- [ ] 数据库 `fieldticket` 已创建
- [ ] 应用用户 `app` 已创建并授权

---

### 步骤 3: 运行迁移脚本

**方法 1: 使用迁移脚本 (推荐)**

```bash
cd /home/user/field-ticket-system/backend/scripts

# Linux/Mac
./run-migration.sh

# Windows
run-migration.bat

# Docker环境
./run-migration-docker.sh
```

**方法 2: 手动执行**

```bash
cd /home/user/field-ticket-system/backend/scripts

# 连接数据库
psql -h localhost -U app -d fieldticket

# 执行迁移
\i 003_add_employee_fields.sql

# 检查结果
\d "Users"

# 退出
\q
```

- [ ] 迁移脚本执行成功
- [ ] Users表包含新字段 (DeptName, SupervisorId, SupervisorName, IdCardLastFour, IsActivated)
- [ ] 索引和约束已创建

**验证命令：**
```bash
psql -h localhost -U app -d fieldticket -c "
  SELECT column_name, data_type, is_nullable
  FROM information_schema.columns
  WHERE table_name = 'Users'
  AND column_name IN ('DeptName', 'SupervisorId', 'SupervisorName', 'IdCardLastFour', 'IsActivated')
  ORDER BY column_name;"
```

**预期输出：**
```
  column_name   |     data_type      | is_nullable
----------------+-------------------+-------------
 DeptName       | character varying | YES
 IdCardLastFour | character varying | YES
 IsActivated    | boolean           | NO
 SupervisorId   | uuid              | YES
 SupervisorName | character varying | YES
```

---

### 步骤 4: 创建管理员账户

```bash
cd /home/user/field-ticket-system/backend/scripts

# 运行初始化脚本
dotnet script init-admin.csx

# 或指定自定义用户名和密码
dotnet script init-admin.csx myadmin MyPassword123!
```

- [ ] 管理员账户创建成功
- [ ] 记录了用户名和密码
- [ ] `IsActivated` 设置为 true

**验证命令：**
```bash
psql -h localhost -U app -d fieldticket -c "
  SELECT \"Username\", \"Role\", \"IsActive\", \"IsActivated\", \"MustChangePassword\"
  FROM \"Users\"
  WHERE \"Role\" = 'Admin';"
```

**预期输出：**
```
 Username | Role  | IsActive | IsActivated | MustChangePassword
----------+-------+----------+-------------+-------------------
 admin    | Admin | t        | t           | t
```

---

## 🚀 后端启动清单

### 步骤 1: 安装依赖包

```bash
cd /home/user/field-ticket-system/backend

# 恢复NuGet包
dotnet restore

# 构建项目
dotnet build
```

- [ ] 所有包恢复成功
- [ ] 项目构建无错误
- [ ] EPPlus包已安装 (7.0.0)

---

### 步骤 2: 配置环境变量

**检查 appsettings.json 配置：**

```bash
cd /home/user/field-ticket-system/backend/src/FieldTicket.Api
cat appsettings.json
```

**关键配置项：**
- [ ] `ConnectionStrings:DefaultConnection` - 数据库连接字符串正确
- [ ] `Jwt:Secret` - JWT密钥已设置
- [ ] `Cors:AllowedOrigins` - 包含前端URL

---

### 步骤 3: 启动后端服务

```bash
cd /home/user/field-ticket-system/backend

# 开发模式启动
dotnet run --project src/FieldTicket.Api/FieldTicket.Api.csproj

# 或使用watch模式（自动重载）
dotnet watch --project src/FieldTicket.Api/FieldTicket.Api.csproj
```

- [ ] 服务启动成功，无错误
- [ ] 监听端口 http://localhost:5000
- [ ] Swagger UI 可访问: http://localhost:5000/swagger

**验证命令：**
```bash
# 检查健康端点
curl http://localhost:5000/health

# 预期输出: Healthy 或 200 OK
```

---

### 步骤 4: 验证API端点

**测试关键端点：**

```bash
# 1. 登录端点
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!","rememberMe":false}'

# 2. 获取模板端点 (需要token)
curl -X GET http://localhost:5000/api/employees/import/template \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -o template.xlsx
```

- [ ] 登录接口返回token
- [ ] 员工导入端点已注册
- [ ] 模板下载接口可用
- [ ] 账户开通接口已注册

---

## 🎨 前端启动清单

### 步骤 1: 安装依赖

```bash
cd /home/user/field-ticket-system/web-admin

# 安装依赖
npm install

# 或使用yarn
yarn install
```

- [ ] 所有依赖安装成功
- [ ] node_modules 目录已创建

---

### 步骤 2: 配置环境变量

**检查 .env 文件：**

```bash
cd /home/user/field-ticket-system/web-admin
cat .env
```

**必要配置：**
```
VITE_API_URL=http://localhost:5000
VITE_APP_TITLE=现场工单管理系统
VITE_APP_VERSION=1.0.0
```

- [ ] `.env` 文件存在
- [ ] `VITE_API_URL` 指向后端地址

---

### 步骤 3: 启动开发服务器

```bash
cd /home/user/field-ticket-system/web-admin

# 启动开发服务器
npm run dev

# 或
yarn dev
```

- [ ] 开发服务器启动成功
- [ ] 监听端口 http://localhost:5173
- [ ] 浏览器自动打开

**验证命令：**
```bash
curl http://localhost:5173
# 预期输出: HTML内容或200 OK
```

---

### 步骤 4: 验证页面可访问

在浏览器中访问以下页面：

- [ ] **登录页**: http://localhost:5173/login
- [ ] **首页**: http://localhost:5173/
- [ ] **用户管理**: http://localhost:5173/users
- [ ] **员工导入**: http://localhost:5173/users/import
- [ ] **账户开通**: http://localhost:5173/users/activate

---

## ✅ 功能验证清单

### 1. 管理员登录验证

**步骤：**
1. 访问 http://localhost:5173/login
2. 输入用户名: `admin`
3. 输入密码: `Admin123!`
4. 点击"登录"
5. 如果是首次登录，修改密码

**验证点：**
- [ ] 登录成功
- [ ] 跳转到首页
- [ ] 导航菜单显示正常
- [ ] 用户信息显示在右上角

---

### 2. 员工导入功能验证

**步骤：**
1. 登录后访问 http://localhost:5173/users/import
2. 点击"下载员工导入模板"
3. 填写3-5个员工信息
4. 上传Excel文件
5. 检查导入结果

**验证点：**
- [ ] 导入页面正常显示
- [ ] 模板下载成功
- [ ] 文件上传成功
- [ ] 显示导入结果（成功/失败统计）
- [ ] 密码生成符合规则（拼音+身份证后4位）
- [ ] 可以复制密码

**测试数据：**
```
姓名      | 身份证号         | 部门   | 预期密码
----------|-----------------|--------|------------------
张三      | 110101199001011234 | 技术部 | zhangsan1234
李四      | 110101199002022345 | 销售部 | lisi2345
```

---

### 3. 账户开通功能验证

**步骤：**
1. 访问 http://localhost:5173/users/activate
2. 查看未开通账户列表
3. 勾选1-2个账户
4. 点击"批量开通"
5. 确认开通操作

**验证点：**
- [ ] 未开通列表正确显示导入的员工
- [ ] 开通状态为"未开通"（橙色标签）
- [ ] 批量开通成功
- [ ] 开通后账户消失或状态变更
- [ ] 显示成功提示

**SQL验证：**
```bash
psql -h localhost -U app -d fieldticket -c "
  SELECT \"Username\", \"Name\", \"IsActivated\"
  FROM \"Users\"
  WHERE \"LoginType\" = 'Password'
  ORDER BY \"CreatedAt\" DESC
  LIMIT 5;"
```

---

### 4. 员工登录验证

**步骤：**
1. 退出管理员账户
2. 使用导入的员工账户登录
3. 使用生成的初始密码
4. 修改密码（首次登录要求）
5. 使用新密码重新登录

**验证点：**
- [ ] 使用初始密码登录成功
- [ ] 强制跳转到密码修改页面
- [ ] 密码修改成功
- [ ] 使用新密码登录成功
- [ ] 正常访问系统功能

---

### 5. 权限控制验证

**步骤：**
1. 使用非管理员账户登录
2. 尝试访问 `/users/import`
3. 尝试访问 `/users/activate`

**验证点：**
- [ ] 显示403无权限页面
- [ ] 自动重定向到首页
- [ ] 显示错误提示消息
- [ ] "返回首页"按钮可用

---

### 6. 导航菜单验证

**步骤：**
1. 使用管理员登录
2. 检查左侧导航菜单
3. 点击"用户管理"菜单

**验证点：**
- [ ] "用户管理"子菜单自动展开
- [ ] 包含三个子项：
  - [ ] 用户列表
  - [ ] 员工批量导入
  - [ ] 账户开通审核
- [ ] 所有菜单项可点击
- [ ] 路由跳转正确

---

### 7. 用户管理页面验证

**步骤：**
1. 访问 http://localhost:5173/users
2. 检查用户列表
3. 查找导入的员工

**验证点：**
- [ ] 用户列表正常显示
- [ ] "开通状态"列存在
- [ ] 未开通用户显示"未开通"（橙色）
- [ ] 已开通用户显示"已开通"（绿色）
- [ ] 未开通用户有"开通"按钮
- [ ] 点击"开通"按钮可以直接开通

---

### 8. 上下级关系验证

**步骤：**
1. 准备包含上下级关系的Excel文件
2. 导入数据
3. 检查数据库中的关系

**验证点：**
- [ ] 上级用户先导入
- [ ] 下级用户的SupervisorId正确指向上级
- [ ] SupervisorName正确显示

**SQL验证：**
```bash
psql -h localhost -U app -d fieldticket -c "
  SELECT
    u.\"Name\" as \"员工\",
    u.\"SupervisorName\" as \"上级姓名\",
    s.\"Name\" as \"上级（验证）\"
  FROM \"Users\" u
  LEFT JOIN \"Users\" s ON u.\"SupervisorId\" = s.\"Id\"
  WHERE u.\"LoginType\" = 'Password'
  ORDER BY u.\"CreatedAt\" DESC;"
```

---

## 🔧 故障排查指南

### 问题 1: 数据库连接失败

**症状：**
```
Npgsql.NpgsqlException: Connection refused
```

**排查步骤：**
1. 检查PostgreSQL是否运行
   ```bash
   # Linux
   sudo systemctl status postgresql

   # Docker
   docker ps | grep postgres
   ```

2. 检查连接字符串配置
   ```bash
   cat backend/src/FieldTicket.Api/appsettings.json | grep ConnectionStrings
   ```

3. 测试连接
   ```bash
   psql -h localhost -U app -d fieldticket -c "SELECT 1;"
   ```

**解决方案：**
- 启动PostgreSQL服务
- 修正连接字符串
- 检查防火墙设置

---

### 问题 2: 迁移脚本执行失败

**症状：**
```
ERROR: relation "FK_Users_Supervisor" already exists
```

**原因：** 外键约束已存在（重复执行）

**解决方案：**
这是正常的，脚本已更新为支持重复执行。如果仍有问题，手动删除约束：
```sql
ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "FK_Users_Supervisor";
```

---

### 问题 3: 管理员账户无法登录

**症状：**
```
账户未开通，请联系管理员
```

**原因：** 管理员账户的`IsActivated`字段为false

**解决方案：**
```bash
psql -h localhost -U app -d fieldticket -c "
  UPDATE \"Users\"
  SET \"IsActivated\" = true
  WHERE \"Username\" = 'admin';"
```

---

### 问题 4: 前端无法连接后端

**症状：**
```
Network Error
Failed to fetch
```

**排查步骤：**
1. 检查后端是否运行
   ```bash
   curl http://localhost:5000/health
   ```

2. 检查CORS配置
   ```bash
   cat backend/src/FieldTicket.Api/appsettings.json | grep AllowedOrigins
   ```

3. 检查前端配置
   ```bash
   cat web-admin/.env | grep VITE_API_URL
   ```

**解决方案：**
- 确保后端运行在 http://localhost:5000
- 确保前端运行在 http://localhost:5173
- CORS允许的源包含前端地址

---

### 问题 5: 上传Excel文件失败

**症状：**
```
只支持Excel文件格式（.xlsx, .xls）
```

**解决方案：**
- 确保文件是.xlsx或.xls格式
- 不要使用.csv文件（需要在Excel中另存为.xlsx）
- 检查文件是否损坏

---

### 问题 6: 密码生成不符合预期

**症状：** 密码不是"拼音+身份证后4位"格式

**排查步骤：**
1. 检查`PinyinHelper.cs`是否正确编译
2. 查看导入日志
3. 验证拼音映射表

**常见问题：**
- 不常见汉字可能映射不准确
- 这不影响功能，员工首次登录会修改密码

---

## 📞 获取帮助

如果以上清单和故障排查都无法解决问题，请：

1. 查看后端日志：
   ```bash
   # 后端控制台输出
   ```

2. 查看浏览器控制台：
   ```
   F12 > Console
   ```

3. 检查数据库日志：
   ```bash
   # PostgreSQL日志位置视系统而定
   ```

4. 查阅文档：
   - EMPLOYEE_IMPORT_GUIDE.md - 完整使用指南
   - EMPLOYEE_IMPORT_QUICKSTART.md - 快速启动
   - TESTING_GUIDE.md - 测试指南

---

## ✅ 完成检查

完成上述所有步骤后，您应该能够：

- [x] 数据库运行并包含所有必要的表和字段
- [x] 管理员账户可以登录
- [x] 后端API服务正常运行
- [x] Web管理端可以访问
- [x] 员工导入功能正常工作
- [x] 账户开通功能正常工作
- [x] 员工可以使用生成的密码登录
- [x] 权限控制正常工作

**🎉 恭喜！系统已准备就绪！**

---

**最后更新：** 2025-12-28
