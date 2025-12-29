# 登录账户密码快速参考

## 登录规则

- **账户**: 姓名的拼音（小写）
- **密码**: 拼音 + 身份证后四位（虚拟生成）

## 所有用户账户密码列表

### admin (管理员)
- **zhou** / **zhou1234** - 周管理员
- **wu** / **wu5678** - 吴管理员

### sales_engineer (销售工程师)
- **zhang** / **zhang1234** - 张销售工程师
- **li** / **li5678** - 李销售工程师

### sales_manager (销售经理)
- **wang** / **wang1234** - 王销售经理
- **zhao** / **zhao5678** - 赵销售经理

### presales (售前技术)
- **chen** / **chen1234** - 陈售前技术
- **liu** / **liu5678** - 刘售前技术

### mechanical (机械工程师)
- **sun** / **sun1234** - 孙机械工程师
- **qian** / **qian5678** - 钱机械工程师

### mechanical_manager (机械部经理)
- **zhou2** / **zhou21234** - 周机械部经理
- **wu2** / **wu25678** - 吴机械部经理

### electrical (电气工程师)
- **zheng** / **zheng1234** - 郑电气工程师
- **wang2** / **wang21234** - 王电气工程师

### electrical_manager (电气部经理)
- **feng** / **feng1234** - 冯电气部经理
- **chen2** / **chen25678** - 陈电气部经理

### test (测试工程师)
- **chu** / **chu1234** - 褚测试工程师
- **wei** / **wei5678** - 卫测试工程师

### test_manager (测试部经理)
- **jiang** / **jiang1234** - 蒋测试部经理
- **shen** / **shen5678** - 沈测试部经理

### procurement (采购工程师)
- **han** / **han1234** - 韩采购工程师
- **yang** / **yang5678** - 杨采购工程师

### procurement_manager (采购部经理)
- **he** / **he1234** - 何采购部经理
- **lv** / **lv5678** - 吕采购部经理

### customer_service (客服工程师)
- **zhu** / **zhu1234** - 朱客服工程师
- **qin** / **qin5678** - 秦客服工程师

### customer_service_manager (客服部经理)
- **you** / **you1234** - 尤客服部经理
- **xu** / **xu5678** - 许客服部经理

### manager (项目经理)
- **shi** / **shi1234** - 施项目经理
- **zhang2** / **zhang21234** - 张项目经理

### production_worker (生产工人)
- **kong** / **kong1234** - 孔生产工人
- **cao** / **cao5678** - 曹生产工人

---

## 格式说明

格式：**账户** / **密码**

例如：**zhou** / **zhou1234** 表示：
- 账户：zhou
- 密码：zhou1234

## 测试账户示例

| 角色 | 账户 | 密码 | 姓名 |
|------|------|------|------|
| admin | zhou | zhou1234 | 周管理员 |
| sales_engineer | zhang | zhang1234 | 张销售工程师 |
| mechanical | sun | sun1234 | 孙机械工程师 |
| electrical | zheng | zheng1234 | 郑电气工程师 |

## 注意事项

⚠️ **当前密码使用明文存储，仅用于测试环境**

生产环境需要：
1. 使用BCrypt等加密算法存储密码
2. 实现密码强度要求
3. 实现密码重置功能
4. 添加登录失败次数限制






