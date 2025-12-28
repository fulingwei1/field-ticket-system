# 初始用户列表

## 用户初始化完成

已成功创建 8 个测试用户，每个角色 2 个用户。

## 用户列表

### FieldEngineer（现场工程师）- 2 人

| 姓名 | 企业微信ID | 手机号 | 状态 |
|------|-----------|--------|------|
| 张工程师 | test_fieldengineer_001 | 13800001001 | 启用 |
| 李工程师 | test_fieldengineer_002 | 13800001002 | 启用 |

### CS（客服）- 2 人

| 姓名 | 企业微信ID | 手机号 | 状态 |
|------|-----------|--------|------|
| 王客服 | test_cs_001 | 13800002001 | 启用 |
| 赵客服 | test_cs_002 | 13800002002 | 启用 |

### SeniorEngineer（高级工程师）- 2 人

| 姓名 | 企业微信ID | 手机号 | 状态 |
|------|-----------|--------|------|
| 陈高级工程师 | test_seniorengineer_001 | 13800003001 | 启用 |
| 刘高级工程师 | test_seniorengineer_002 | 13800003002 | 启用 |

### Admin（管理员）- 2 人

| 姓名 | 企业微信ID | 手机号 | 状态 |
|------|-----------|--------|------|
| 周管理员 | test_admin_001 | 13800004001 | 启用 |
| 吴管理员 | test_admin_002 | 13800004002 | 启用 |

## 用户信息说明

- **企业ID (CorpId)**: `test_corp`
- **所有用户均为启用状态** (`is_active = true`)
- **部门ID**: 
  - FieldEngineer: `dept_001`
  - CS: `dept_002`
  - SeniorEngineer: `dept_003`
  - Admin: `dept_004`

## 查询用户

### 查看所有测试用户

```sql
SELECT role, name, wecom_userid, mobile, is_active 
FROM users 
WHERE wecom_userid LIKE 'test_%' 
ORDER BY role, name;
```

### 按角色查询

```sql
-- 查询所有现场工程师
SELECT * FROM users WHERE role = 'FieldEngineer' AND wecom_userid LIKE 'test_%';

-- 查询所有客服
SELECT * FROM users WHERE role = 'CS' AND wecom_userid LIKE 'test_%';

-- 查询所有高级工程师
SELECT * FROM users WHERE role = 'SeniorEngineer' AND wecom_userid LIKE 'test_%';

-- 查询所有管理员
SELECT * FROM users WHERE role = 'Admin' AND wecom_userid LIKE 'test_%';
```

## 重新初始化用户

如果需要重新初始化用户，可以执行：

```bash
# 删除现有测试用户（可选）
docker exec fieldticket-postgres psql -U app -d fieldticket -c "DELETE FROM users WHERE wecom_userid LIKE 'test_%';"

# 重新执行初始化脚本
docker exec -i fieldticket-postgres psql -U app -d fieldticket < scripts/init-users.sql
```

## 注意事项

1. 这些是测试用户，企业微信ID 以 `test_` 开头
2. 实际使用时，用户会通过企业微信 OAuth 登录自动创建
3. 测试用户的企业微信ID 是虚拟的，不能用于实际的企业微信登录
4. 如需添加更多用户，可以修改 `scripts/init-users.sql` 文件后重新执行

## 创建时间

- 初始化时间: 2025-12-28
- 脚本位置: `scripts/init-users.sql`



