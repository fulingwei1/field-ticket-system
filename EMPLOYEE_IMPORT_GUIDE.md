# 📊 员工批量导入使用指南

## 📋 目录
1. [功能概述](#功能概述)
2. [导入流程](#导入流程)
3. [Excel模板说明](#excel模板说明)
4. [API接口说明](#api接口说明)
5. [密码生成规则](#密码生成规则)
6. [账户开通流程](#账户开通流程)
7. [常见问题](#常见问题)

---

## 🎯 功能概述

员工批量导入功能允许管理员通过上传Excel文件快速创建多个员工账户，包括：

- ✅ 自动创建登录账户
- ✅ 自动生成初始密码
- ✅ 建立组织架构关系（部门、上级）
- ✅ 批量账户开通管理
- ✅ 强制首次登录修改密码

---

## 🔄 导入流程

### 完整流程图

```
1. 管理员准备Excel文件
   ↓
2. 上传Excel文件到系统
   ↓
3. 系统自动创建账户（状态：未开通）
   ↓
4. 管理员审核并开通账户
   ↓
5. 员工使用初始密码登录
   ↓
6. 首次登录强制修改密码
```

### 步骤详解

#### 第1步：下载Excel模板

**API调用：**
```bash
GET http://localhost:5000/api/employees/import/template
```

**或在浏览器直接访问：**
```
http://localhost:5000/api/employees/import/template
```

系统会自动下载 `员工导入模板.xlsx` 文件。

#### 第2步：填写员工信息

打开下载的模板，填写员工信息：

| 列名 | 是否必填 | 说明 | 示例 |
|------|---------|------|------|
| 姓名 | ✅ 必填 | 将作为登录账号 | 张三 |
| 身份证号 | ✅ 必填 | 至少4位，用于密码生成 | 110101199001011234 |
| 部门名称 | ✅ 必填 | 员工所属部门 | 技术部 |
| 部门号 | ⭕ 可选 | 部门编号 | DEPT001 |
| 上级姓名 | ⭕ 可选 | 直属上级的姓名 | 李四 |
| 手机号 | ⭕ 可选 | 联系电话 | 13800138000 |
| 邮箱 | ⭕ 可选 | 电子邮箱 | zhangsan@example.com |
| 角色 | ⭕ 可选 | 默认为FieldEngineer | FieldEngineer |

**角色可选值：**
- `FieldEngineer` - 现场工程师（默认）
- `CS` - 客服
- `SeniorEngineer` - 高级工程师
- `Admin` - 系统管理员

#### 第3步：上传Excel文件

**使用API：**
```bash
curl -X POST http://localhost:5000/api/employees/import/excel \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -F "file=@/path/to/员工信息.xlsx" \
  -F "defaultRole=FieldEngineer" \
  -F "overwriteExisting=false"
```

**参数说明：**
- `file`: Excel文件（必填）
- `defaultRole`: 默认角色（可选，默认为FieldEngineer）
- `overwriteExisting`: 是否覆盖已存在的用户（可选，默认为false）

**返回结果示例：**
```json
{
  "totalCount": 10,
  "successCount": 8,
  "failureCount": 1,
  "skippedCount": 1,
  "successList": [
    {
      "rowNumber": 2,
      "name": "张三",
      "username": "张三",
      "password": "zhangsan1234",
      "deptName": "技术部"
    }
  ],
  "errorList": [
    {
      "rowNumber": 5,
      "name": "王五",
      "error": "身份证号不能为空且至少4位"
    }
  ]
}
```

#### 第4步：审核并开通账户

导入成功后，所有账户默认处于**未开通**状态，需要管理员手动开通。

**查询未开通账户：**
```bash
GET http://localhost:5000/api/users?isActivated=false
```

**开通单个账户：**
```bash
POST http://localhost:5000/api/employees/{userId}/activate
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**批量开通账户：**
```bash
POST http://localhost:5000/api/employees/batch/activate
Authorization: Bearer YOUR_ADMIN_TOKEN
Content-Type: application/json

[
  "user-id-1",
  "user-id-2",
  "user-id-3"
]
```

#### 第5步：通知员工登录

将生成的用户名和密码告知员工：

```
员工账户开通通知

您好，张三！

您的账户已开通，请使用以下信息登录系统：

登录地址：http://localhost:5173
用户名：张三
初始密码：zhangsan1234

⚠️ 重要提示：
1. 首次登录后系统会要求您修改密码
2. 新密码至少8位，必须包含大小写字母和数字
3. 请妥善保管您的账户信息

如有问题，请联系系统管理员。
```

---

## 📝 Excel模板说明

### 模板结构

```
| A列 | B列 | C列 | D列 | E列 | F列 | G列 | H列 |
|-----|-----|-----|-----|-----|-----|-----|-----|
| 姓名（必填） | 身份证号（必填） | 部门名称（必填） | 部门号（可选） | 上级姓名（可选） | 手机号（可选） | 邮箱（可选） | 角色（可选） |
| 张三 | 110101199001011234 | 技术部 | DEPT001 | 李四 | 13800138000 | zhangsan@example.com | FieldEngineer |
```

### 注意事项

1. **第1行为标题行**，从第2行开始填写数据
2. **姓名、身份证号、部门名称为必填项**
3. **空行会自动跳过**
4. **身份证号至少需要4位**（用于密码生成）
5. **上级姓名必须是同批次导入或已存在的用户**
6. **角色未填写时使用默认角色**（通常为FieldEngineer）

### 数据验证规则

导入时系统会自动验证：

| 验证项 | 规则 |
|--------|------|
| 姓名 | 不能为空 |
| 身份证号 | 不能为空，至少4位 |
| 部门名称 | 不能为空 |
| 手机号 | 可选，格式不做严格校验 |
| 邮箱 | 可选，格式不做严格校验 |
| 角色 | 可选，必须是有效的角色值 |

---

## 🔐 密码生成规则

### 生成规则

**密码 = 姓名拼音（全小写） + 身份证后4位**

### 拼音转换示例

| 姓名 | 拼音 | 身份证后4位 | 生成的密码 |
|------|------|------------|-----------|
| 张三 | zhangsan | 1234 | zhangsan1234 |
| 李明华 | liminghua | 5678 | liminghua5678 |
| 王五 | wangwu | 9012 | wangwu9012 |
| 赵丽娜 | zhaolina | 3456 | zhaolina3456 |

### 特殊情况处理

1. **复杂汉字**：如果姓名包含不常用汉字，系统会使用简化拼音映射
2. **英文字母**：直接转换为小写
3. **数字**：保持不变
4. **特殊字符**：会被过滤或映射为字母

### 支持的姓氏（部分）

系统内置了80+常见姓氏的拼音映射：

```
王、李、张、刘、陈、杨、赵、黄、周、吴、
徐、孙、胡、朱、高、林、何、郭、马、罗、
梁、宋、郑、谢、韩、唐、冯、于、董、萧、
程、曹、袁、邓、许、傅、沈、曾、彭、吕...
```

### 支持的常用字（部分）

系统内置了60+常用字的拼音映射：

```
明、华、强、军、伟、敏、杰、娜、静、丽、
芳、秀、国、文、平、建、云、海、江、波...
```

---

## 🔓 账户开通流程

### 为什么需要开通？

导入的账户默认**未开通**状态，这样设计的目的是：

1. ✅ **安全审核**：管理员可以在开通前审核员工信息
2. ✅ **分批开通**：支持按需分批次开通账户
3. ✅ **防止误操作**：避免批量导入错误数据后立即生效

### 开通状态说明

| 状态 | 说明 | 能否登录 |
|------|------|---------|
| 未开通（IsActivated = false） | 账户已创建但未激活 | ❌ 不能 |
| 已开通（IsActivated = true） | 账户已激活，可以使用 | ✅ 可以 |

### 开通操作

#### 方式1：Web管理端（推荐）

1. 登录Web管理端
2. 进入"用户管理"页面
3. 筛选"未开通"的用户
4. 勾选需要开通的用户
5. 点击"批量开通"按钮

#### 方式2：API接口

**开通单个账户：**
```bash
curl -X POST http://localhost:5000/api/employees/user-id-123/activate \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**批量开通：**
```bash
curl -X POST http://localhost:5000/api/employees/batch/activate \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '[
    "user-id-1",
    "user-id-2",
    "user-id-3"
  ]'
```

### 开通后的效果

开通后：
- ✅ 用户可以使用初始密码登录
- ✅ 首次登录会强制修改密码
- ✅ 修改密码后即可正常使用系统

---

## 🔧 API接口说明

### 1. 下载Excel模板

```http
GET /api/employees/import/template
Authorization: Bearer {token}
```

**返回：** Excel文件下载

---

### 2. 从Excel导入员工

```http
POST /api/employees/import/excel
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: (Excel文件)
defaultRole: FieldEngineer (可选)
overwriteExisting: false (可选)
```

**返回：**
```json
{
  "totalCount": 10,
  "successCount": 8,
  "failureCount": 1,
  "skippedCount": 1,
  "successList": [...],
  "errorList": [...]
}
```

---

### 3. 从JSON导入员工

```http
POST /api/employees/import/json
Authorization: Bearer {token}
Content-Type: application/json

{
  "employees": [
    {
      "name": "张三",
      "idCard": "110101199001011234",
      "deptName": "技术部",
      "deptId": "DEPT001",
      "supervisorName": "李四",
      "mobile": "13800138000",
      "email": "zhangsan@example.com",
      "role": "FieldEngineer"
    }
  ],
  "defaultRole": "FieldEngineer",
  "overwriteExisting": false
}
```

---

### 4. 开通单个账户

```http
POST /api/employees/{id}/activate
Authorization: Bearer {token}
```

**返回：**
```json
{
  "success": true,
  "message": "账户已开通",
  "user": {
    "id": "user-id",
    "username": "张三",
    "name": "张三",
    "deptName": "技术部",
    "isActivated": true
  }
}
```

---

### 5. 批量开通账户

```http
POST /api/employees/batch/activate
Authorization: Bearer {token}
Content-Type: application/json

["user-id-1", "user-id-2", "user-id-3"]
```

**返回：**
```json
{
  "success": true,
  "message": "成功开通 3 个账户",
  "activatedCount": 3,
  "users": [...]
}
```

---

## ❓ 常见问题

### Q1: 导入时提示"姓名不能为空"

**原因：** Excel中的姓名单元格为空

**解决：**
- 检查Excel文件，确保所有必填字段都已填写
- 删除完全空白的行

---

### Q2: 导入时提示"身份证号不能为空且至少4位"

**原因：** 身份证号未填写或少于4位

**解决：**
- 确保身份证号至少4位数字
- 如果是测试数据，可以使用"1234"等简单数字

---

### Q3: 上级关系未正确建立

**原因：** 上级用户不存在或上级在Excel中的位置靠后

**解决：**
- 确保上级用户已存在于系统中
- 或在同一批次导入时，上级用户的行应在员工之前
- 系统会自动处理同批次导入的上级关系

---

### Q4: 员工无法登录，提示"账户未开通"

**原因：** 导入后账户默认未开通状态

**解决：**
- 管理员需要手动开通账户
- 使用Web管理端或API接口开通

---

### Q5: 如何批量修改已导入员工的信息？

**解决方案1：** 使用覆盖模式重新导入
```bash
curl -X POST http://localhost:5000/api/employees/import/excel \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -F "file=@/path/to/员工信息.xlsx" \
  -F "overwriteExisting=true"
```

**解决方案2：** 通过用户管理接口逐个修改

---

### Q6: 拼音转换不准确怎么办？

**临时解决：** 系统会使用简化映射，生成的密码仍然有效

**建议：** 通知员工首次登录后立即修改密码，初始密码的准确性不影响使用

---

### Q7: 如何查看所有未开通的账户？

**API查询：**
```bash
GET http://localhost:5000/api/users?isActivated=false
```

**或在Web管理端：**
- 进入"用户管理"
- 使用筛选器：状态 = "未开通"

---

### Q8: 开通账户后能否撤销？

**当前版本不支持撤销开通，但可以：**

1. **禁用账户：**
```bash
PATCH http://localhost:5000/api/users/{id}
{
  "isActive": false
}
```

2. **删除账户：**
```bash
DELETE http://localhost:5000/api/users/{id}
```

---

## 📞 技术支持

### 数据库迁移

在使用员工导入功能前，需要先运行数据库迁移脚本：

```bash
# 进入脚本目录
cd /home/user/field-ticket-system/backend/scripts

# 执行迁移脚本
psql -U fieldticket -d fieldticket_db -f 003_add_employee_fields.sql
```

### 相关文档

- **Web管理端启动指南**: `/home/user/field-ticket-system/WEB_ADMIN_QUICK_START.md`
- **完整部署指南**: `/home/user/field-ticket-system/DEPLOYMENT_GUIDE.md`
- **登录指南**: `/home/user/field-ticket-system/LOGIN_GUIDE.md`

---

**最后更新：** 2025-12-27
**版本：** 1.0.0
