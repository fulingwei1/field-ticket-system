# 新角色体系用户列表

## 用户初始化完成

已成功创建 **32 个测试用户**，覆盖 **16 个角色**，每个角色 2 个用户。

## 角色与用户列表

### 1. admin (管理员) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 周管理员 | test_admin_001 | 13800001001 | dept_admin | 启用 |
| 吴管理员 | test_admin_002 | 13800001002 | dept_admin | 启用 |

**权限说明**: 所有权限，可管理用户和所有数据

---

### 2. sales_engineer (销售工程师) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 张销售工程师 | test_sales_engineer_001 | 13800002001 | dept_sales | 启用 |
| 李销售工程师 | test_sales_engineer_002 | 13800002002 | dept_sales | 启用 |

**权限说明**: 创建变更单，上级为销售经理

---

### 3. sales_manager (销售经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 王销售经理 | test_sales_manager_001 | 13800003001 | dept_sales | 启用 |
| 赵销售经理 | test_sales_manager_002 | 13800003002 | dept_sales | 启用 |

**权限说明**: 审批销售工程师的变更单

---

### 4. presales (售前技术) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 陈售前技术 | test_presales_001 | 13800004001 | dept_sales | 启用 |
| 刘售前技术 | test_presales_002 | 13800004002 | dept_sales | 启用 |

**权限说明**: 技术支持

---

### 5. mechanical (机械工程师) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 孙机械工程师 | test_mechanical_001 | 13800005001 | dept_mechanical | 启用 |
| 钱机械工程师 | test_mechanical_002 | 13800005002 | dept_mechanical | 启用 |

**权限说明**: 上级为机械部经理

---

### 6. mechanical_manager (机械部经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 周机械部经理 | test_mechanical_manager_001 | 13800006001 | dept_mechanical | 启用 |
| 吴机械部经理 | test_mechanical_manager_002 | 13800006002 | dept_mechanical | 启用 |

**权限说明**: 审批机械工程师的变更单

---

### 7. electrical (电气工程师) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 郑电气工程师 | test_electrical_001 | 13800007001 | dept_electrical | 启用 |
| 王电气工程师 | test_electrical_002 | 13800007002 | dept_electrical | 启用 |

**权限说明**: 上级为电气部经理

---

### 8. electrical_manager (电气部经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 冯电气部经理 | test_electrical_manager_001 | 13800008001 | dept_electrical | 启用 |
| 陈电气部经理 | test_electrical_manager_002 | 13800008002 | dept_electrical | 启用 |

**权限说明**: 审批电气工程师的变更单

---

### 9. test (测试工程师) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 褚测试工程师 | test_test_001 | 13800009001 | dept_test | 启用 |
| 卫测试工程师 | test_test_002 | 13800009002 | dept_test | 启用 |

**权限说明**: 上级为测试部经理

---

### 10. test_manager (测试部经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 蒋测试部经理 | test_test_manager_001 | 13800010001 | dept_test | 启用 |
| 沈测试部经理 | test_test_manager_002 | 13800010002 | dept_test | 启用 |

**权限说明**: 审批测试工程师的变更单

---

### 11. procurement (采购工程师) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 韩采购工程师 | test_procurement_001 | 13800011001 | dept_procurement | 启用 |
| 杨采购工程师 | test_procurement_002 | 13800011002 | dept_procurement | 启用 |

**权限说明**: 上级为采购部经理

---

### 12. procurement_manager (采购部经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 何采购部经理 | test_procurement_manager_001 | 13800012001 | dept_procurement | 启用 |
| 吕采购部经理 | test_procurement_manager_002 | 13800012002 | dept_procurement | 启用 |

**权限说明**: 审批采购工程师的变更单

---

### 13. customer_service (客服工程师) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 朱客服工程师 | test_customer_service_001 | 13800013001 | dept_customer_service | 启用 |
| 秦客服工程师 | test_customer_service_002 | 13800013002 | dept_customer_service | 启用 |

**权限说明**: 上级为客服部经理

---

### 14. customer_service_manager (客服部经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 尤客服部经理 | test_customer_service_manager_001 | 13800014001 | dept_customer_service | 启用 |
| 许客服部经理 | test_customer_service_manager_002 | 13800014002 | dept_customer_service | 启用 |

**权限说明**: 审批客服工程师的变更单

---

### 15. manager (项目经理) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 施项目经理 | test_manager_001 | 13800015001 | dept_project | 启用 |
| 张项目经理 | test_manager_002 | 13800015002 | dept_project | 启用 |

**权限说明**: 项目管理

---

### 16. production_worker (生产工人) - 2人

| 姓名 | 企业微信ID | 手机号 | 部门 | 状态 |
|------|-----------|--------|------|------|
| 孔生产工人 | test_production_worker_001 | 13800016001 | dept_production | 启用 |
| 曹生产工人 | test_production_worker_002 | 13800016002 | dept_production | 启用 |

**权限说明**: 执行任务

---

## 统计信息

- **总用户数**: 32 人
- **总角色数**: 16 个
- **每个角色用户数**: 2 人
- **企业ID**: `test_corp`
- **所有用户状态**: 启用 (`is_active = true`)

## 数据库变更

### 1. 角色字段扩展

- **原长度**: VARCHAR(20)
- **新长度**: VARCHAR(50)
- **原因**: 支持更长的角色代码（如 `customer_service_manager`）

### 2. 角色约束更新

已更新 CHECK 约束，支持以下 16 个角色：

```sql
CHECK (role IN (
    'admin',
    'sales_engineer',
    'sales_manager',
    'presales',
    'mechanical',
    'mechanical_manager',
    'electrical',
    'electrical_manager',
    'test',
    'test_manager',
    'procurement',
    'procurement_manager',
    'customer_service',
    'customer_service_manager',
    'manager',
    'production_worker'
))
```

## 查询命令

### 查看所有用户

```sql
SELECT role, name, wecom_userid, mobile, is_active 
FROM users 
WHERE wecom_userid LIKE 'test_%' 
ORDER BY role, name;
```

### 按角色统计

```sql
SELECT role, COUNT(*) as user_count 
FROM users 
WHERE wecom_userid LIKE 'test_%' 
GROUP BY role 
ORDER BY role;
```

### 按部门查询

```sql
SELECT dept_id, role, COUNT(*) as count
FROM users 
WHERE wecom_userid LIKE 'test_%'
GROUP BY dept_id, role
ORDER BY dept_id, role;
```

## 重新初始化

如果需要重新初始化用户：

```bash
# 删除现有测试用户
docker exec fieldticket-postgres psql -U app -d fieldticket -c "DELETE FROM users WHERE wecom_userid LIKE 'test_%';"

# 重新执行初始化脚本
docker exec -i fieldticket-postgres psql -U app -d fieldticket < scripts/init-users-new-roles.sql
```

## 相关文件

- **数据库更新脚本**: `scripts/update-role-constraint.sql`
- **用户初始化脚本**: `scripts/init-users-new-roles.sql`
- **文档**: `docs/setup/NEW_ROLE_SYSTEM_USERS.md`

## 创建时间

- 初始化时间: 2025-12-28
- 角色体系: 基于图片中的角色定义






