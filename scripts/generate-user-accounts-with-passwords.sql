-- 为所有测试用户生成账户和密码
-- 账户：姓名的拼音
-- 密码：拼音+身份证后四位（虚拟生成）
-- 注意：这里使用明文密码占位符，实际应用中需要使用BCrypt加密存储

-- admin (管理员)
UPDATE users SET username = 'zhou', password_hash = 'zhou1234' WHERE wecom_userid = 'test_admin_001' AND name = '周管理员';
UPDATE users SET username = 'wu', password_hash = 'wu5678' WHERE wecom_userid = 'test_admin_002' AND name = '吴管理员';

-- sales_engineer (销售工程师)
UPDATE users SET username = 'zhang', password_hash = 'zhang1234' WHERE wecom_userid = 'test_sales_engineer_001' AND name = '张销售工程师';
UPDATE users SET username = 'li', password_hash = 'li5678' WHERE wecom_userid = 'test_sales_engineer_002' AND name = '李销售工程师';

-- sales_manager (销售经理)
UPDATE users SET username = 'wang', password_hash = 'wang1234' WHERE wecom_userid = 'test_sales_manager_001' AND name = '王销售经理';
UPDATE users SET username = 'zhao', password_hash = 'zhao5678' WHERE wecom_userid = 'test_sales_manager_002' AND name = '赵销售经理';

-- presales (售前技术)
UPDATE users SET username = 'chen', password_hash = 'chen1234' WHERE wecom_userid = 'test_presales_001' AND name = '陈售前技术';
UPDATE users SET username = 'liu', password_hash = 'liu5678' WHERE wecom_userid = 'test_presales_002' AND name = '刘售前技术';

-- mechanical (机械工程师)
UPDATE users SET username = 'sun', password_hash = 'sun1234' WHERE wecom_userid = 'test_mechanical_001' AND name = '孙机械工程师';
UPDATE users SET username = 'qian', password_hash = 'qian5678' WHERE wecom_userid = 'test_mechanical_002' AND name = '钱机械工程师';

-- mechanical_manager (机械部经理)
UPDATE users SET username = 'zhou2', password_hash = 'zhou21234' WHERE wecom_userid = 'test_mechanical_manager_001' AND name = '周机械部经理';
UPDATE users SET username = 'wu2', password_hash = 'wu25678' WHERE wecom_userid = 'test_mechanical_manager_002' AND name = '吴机械部经理';

-- electrical (电气工程师)
UPDATE users SET username = 'zheng', password_hash = 'zheng1234' WHERE wecom_userid = 'test_electrical_001' AND name = '郑电气工程师';
UPDATE users SET username = 'wang2', password_hash = 'wang21234' WHERE wecom_userid = 'test_electrical_002' AND name = '王电气工程师';

-- electrical_manager (电气部经理)
UPDATE users SET username = 'feng', password_hash = 'feng1234' WHERE wecom_userid = 'test_electrical_manager_001' AND name = '冯电气部经理';
UPDATE users SET username = 'chen2', password_hash = 'chen25678' WHERE wecom_userid = 'test_electrical_manager_002' AND name = '陈电气部经理';

-- test (测试工程师)
UPDATE users SET username = 'chu', password_hash = 'chu1234' WHERE wecom_userid = 'test_test_001' AND name = '褚测试工程师';
UPDATE users SET username = 'wei', password_hash = 'wei5678' WHERE wecom_userid = 'test_test_002' AND name = '卫测试工程师';

-- test_manager (测试部经理)
UPDATE users SET username = 'jiang', password_hash = 'jiang1234' WHERE wecom_userid = 'test_test_manager_001' AND name = '蒋测试部经理';
UPDATE users SET username = 'shen', password_hash = 'shen5678' WHERE wecom_userid = 'test_test_manager_002' AND name = '沈测试部经理';

-- procurement (采购工程师)
UPDATE users SET username = 'han', password_hash = 'han1234' WHERE wecom_userid = 'test_procurement_001' AND name = '韩采购工程师';
UPDATE users SET username = 'yang', password_hash = 'yang5678' WHERE wecom_userid = 'test_procurement_002' AND name = '杨采购工程师';

-- procurement_manager (采购部经理)
UPDATE users SET username = 'he', password_hash = 'he1234' WHERE wecom_userid = 'test_procurement_manager_001' AND name = '何采购部经理';
UPDATE users SET username = 'lv', password_hash = 'lv5678' WHERE wecom_userid = 'test_procurement_manager_002' AND name = '吕采购部经理';

-- customer_service (客服工程师)
UPDATE users SET username = 'zhu', password_hash = 'zhu1234' WHERE wecom_userid = 'test_customer_service_001' AND name = '朱客服工程师';
UPDATE users SET username = 'qin', password_hash = 'qin5678' WHERE wecom_userid = 'test_customer_service_002' AND name = '秦客服工程师';

-- customer_service_manager (客服部经理)
UPDATE users SET username = 'you', password_hash = 'you1234' WHERE wecom_userid = 'test_customer_service_manager_001' AND name = '尤客服部经理';
UPDATE users SET username = 'xu', password_hash = 'xu5678' WHERE wecom_userid = 'test_customer_service_manager_002' AND name = '许客服部经理';

-- manager (项目经理)
UPDATE users SET username = 'shi', password_hash = 'shi1234' WHERE wecom_userid = 'test_manager_001' AND name = '施项目经理';
UPDATE users SET username = 'zhang2', password_hash = 'zhang21234' WHERE wecom_userid = 'test_manager_002' AND name = '张项目经理';

-- production_worker (生产工人)
UPDATE users SET username = 'kong', password_hash = 'kong1234' WHERE wecom_userid = 'test_production_worker_001' AND name = '孔生产工人';
UPDATE users SET username = 'cao', password_hash = 'cao5678' WHERE wecom_userid = 'test_production_worker_002' AND name = '曹生产工人';

-- 验证更新
SELECT username, name, role FROM users WHERE wecom_userid LIKE 'test_%' ORDER BY role, username;



