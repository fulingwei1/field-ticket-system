# 用户账户密码列表

## 登录说明

- **登录方式**: 账户密码登录
- **账户格式**: 姓名的拼音（小写）
- **密码格式**: 拼音 + 身份证后四位（虚拟生成）

## 用户账户密码列表

### admin (管理员) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 周管理员 | zhou | zhou1234 | admin |
| 吴管理员 | wu | wu5678 | admin |

---

### sales_engineer (销售工程师) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 张销售工程师 | zhang | zhang1234 | sales_engineer |
| 李销售工程师 | li | li5678 | sales_engineer |

---

### sales_manager (销售经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 王销售经理 | wang | wang1234 | sales_manager |
| 赵销售经理 | zhao | zhao5678 | sales_manager |

---

### presales (售前技术) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 陈售前技术 | chen | chen1234 | presales |
| 刘售前技术 | liu | liu5678 | presales |

---

### mechanical (机械工程师) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 孙机械工程师 | sun | sun1234 | mechanical |
| 钱机械工程师 | qian | qian5678 | mechanical |

---

### mechanical_manager (机械部经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 周机械部经理 | zhou2 | zhou21234 | mechanical_manager |
| 吴机械部经理 | wu2 | wu25678 | mechanical_manager |

---

### electrical (电气工程师) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 郑电气工程师 | zheng | zheng1234 | electrical |
| 王电气工程师 | wang2 | wang21234 | electrical |

---

### electrical_manager (电气部经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 冯电气部经理 | feng | feng1234 | electrical_manager |
| 陈电气部经理 | chen2 | chen25678 | electrical_manager |

---

### test (测试工程师) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 褚测试工程师 | chu | chu1234 | test |
| 卫测试工程师 | wei | wei5678 | test |

---

### test_manager (测试部经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 蒋测试部经理 | jiang | jiang1234 | test_manager |
| 沈测试部经理 | shen | shen5678 | test_manager |

---

### procurement (采购工程师) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 韩采购工程师 | han | han1234 | procurement |
| 杨采购工程师 | yang | yang5678 | procurement |

---

### procurement_manager (采购部经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 何采购部经理 | he | he1234 | procurement_manager |
| 吕采购部经理 | lv | lv5678 | procurement_manager |

---

### customer_service (客服工程师) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 朱客服工程师 | zhu | zhu1234 | customer_service |
| 秦客服工程师 | qin | qin5678 | customer_service |

---

### customer_service_manager (客服部经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 尤客服部经理 | you | you1234 | customer_service_manager |
| 许客服部经理 | xu | xu5678 | customer_service_manager |

---

### manager (项目经理) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 施项目经理 | shi | shi1234 | manager |
| 张项目经理 | zhang2 | zhang21234 | manager |

---

### production_worker (生产工人) - 2人

| 姓名 | 账户 | 密码 | 角色 |
|------|------|------|------|
| 孔生产工人 | kong | kong1234 | production_worker |
| 曹生产工人 | cao | cao5678 | production_worker |

---

## 快速参考表

| 角色 | 账户示例 | 密码示例 |
|------|---------|----------|
| admin | zhou | zhou1234 |
| sales_engineer | zhang | zhang1234 |
| sales_manager | wang | wang1234 |
| presales | chen | chen1234 |
| mechanical | sun | sun1234 |
| mechanical_manager | zhou2 | zhou21234 |
| electrical | zheng | zheng1234 |
| electrical_manager | feng | feng1234 |
| test | chu | chu1234 |
| test_manager | jiang | jiang1234 |
| procurement | han | han1234 |
| procurement_manager | he | he1234 |
| customer_service | zhu | zhu1234 |
| customer_service_manager | you | you1234 |
| manager | shi | shi1234 |
| production_worker | kong | kong1234 |

## 注意事项

1. **密码规则**: 密码 = 账户名（拼音）+ 身份证后四位（虚拟生成）
2. **账户唯一性**: 每个账户名都是唯一的
3. **密码存储**: 当前使用明文存储，实际生产环境应使用BCrypt加密
4. **测试用途**: 这些账户仅用于测试，生产环境需要修改密码策略

## 数据库查询

```sql
-- 查看所有用户账户
SELECT username, name, role, password_hash 
FROM users 
WHERE wecom_userid LIKE 'test_%' 
ORDER BY role, username;

-- 按角色查看账户
SELECT role, username, name 
FROM users 
WHERE wecom_userid LIKE 'test_%' 
ORDER BY role, username;
```

## 更新记录

- 创建时间: 2025-12-28
- 账户生成规则: 姓名的拼音（小写）
- 密码生成规则: 拼音 + 身份证后四位（虚拟生成）



