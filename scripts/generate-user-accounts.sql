-- 为所有测试用户生成账户和密码
-- 账户：姓名的拼音
-- 密码：拼音+身份证后四位（虚拟生成）

-- 注意：这里使用明文密码，实际应用中应该使用BCrypt加密
-- 密码格式：拼音+身份证后四位（例如：zhang1234）

-- admin (管理员)
UPDATE users SET username = 'zhou', password_hash = '$2a$11$placeholder_zhou1234' WHERE wecom_userid = 'test_admin_001' AND name = '周管理员';
UPDATE users SET username = 'wu', password_hash = '$2a$11$placeholder_wu5678' WHERE wecom_userid = 'test_admin_002' AND name = '吴管理员';

-- sales_engineer (销售工程师)
UPDATE users SET username = 'zhang', password_hash = '$2a$11$placeholder_zhang1234' WHERE wecom_userid = 'test_sales_engineer_001' AND name = '张销售工程师';
UPDATE users SET username = 'li', password_hash = '$2a$11$placeholder_li5678' WHERE wecom_userid = 'test_sales_engineer_002' AND name = '李销售工程师';

-- sales_manager (销售经理)
UPDATE users SET username = 'wang', password_hash = '$2a$11$placeholder_wang1234' WHERE wecom_userid = 'test_sales_manager_001' AND name = '王销售经理';
UPDATE users SET username = 'zhao', password_hash = '$2a$11$placeholder_zhao5678' WHERE wecom_userid = 'test_sales_manager_002' AND name = '赵销售经理';

-- presales (售前技术)
UPDATE users SET username = 'chen', password_hash = '$2a$11$placeholder_chen1234' WHERE wecom_userid = 'test_presales_001' AND name = '陈售前技术';
UPDATE users SET username = 'liu', password_hash = '$2a$11$placeholder_liu5678' WHERE wecom_userid = 'test_presales_002' AND name = '刘售前技术';

-- mechanical (机械工程师)
UPDATE users SET username = 'sun', password_hash = '$2a$11$placeholder_sun1234' WHERE wecom_userid = 'test_mechanical_001' AND name = '孙机械工程师';
UPDATE users SET username = 'qian', password_hash = '$2a$11$placeholder_qian5678' WHERE wecom_userid = 'test_mechanical_002' AND name = '钱机械工程师';

-- mechanical_manager (机械部经理)
UPDATE users SET username = 'zhou2', password_hash = '$2a$11$placeholder_zhou21234' WHERE wecom_userid = 'test_mechanical_manager_001' AND name = '周机械部经理';
UPDATE users SET username = 'wu2', password_hash = '$2a$11$placeholder_wu25678' WHERE wecom_userid = 'test_mechanical_manager_002' AND name = '吴机械部经理';

-- electrical (电气工程师)
UPDATE users SET username = 'zheng', password_hash = '$2a$11$placeholder_zheng1234' WHERE wecom_userid = 'test_electrical_001' AND name = '郑电气工程师';
UPDATE users SET username = 'wang2', password_hash = '$2a$11$placeholder_wang21234' WHERE wecom_userid = 'test_electrical_002' AND name = '王电气工程师';

-- electrical_manager (电气部经理)
UPDATE users SET username = 'feng', password_hash = '$2a$11$placeholder_feng1234' WHERE wecom_userid = 'test_electrical_manager_001' AND name = '冯电气部经理';
UPDATE users SET username = 'chen2', password_hash = '$2a$11$placeholder_chen25678' WHERE wecom_userid = 'test_electrical_manager_002' AND name = '陈电气部经理';

-- test (测试工程师)
UPDATE users SET username = 'chu', password_hash = '$2a$11$placeholder_chu1234' WHERE wecom_userid = 'test_test_001' AND name = '褚测试工程师';
UPDATE users SET username = 'wei', password_hash = '$2a$11$placeholder_wei5678' WHERE wecom_userid = 'test_test_002' AND name = '卫测试工程师';

-- test_manager (测试部经理)
UPDATE users SET username = 'jiang', password_hash = '$2a$11$placeholder_jiang1234' WHERE wecom_userid = 'test_test_manager_001' AND name = '蒋测试部经理';
UPDATE users SET username = 'shen', password_hash = '$2a$11$placeholder_shen5678' WHERE wecom_userid = 'test_test_manager_002' AND name = '沈测试部经理';

-- procurement (采购工程师)
UPDATE users SET username = 'han', password_hash = '$2a$11$placeholder_han1234' WHERE wecom_userid = 'test_procurement_001' AND name = '韩采购工程师';
UPDATE users SET username = 'yang', password_hash = '$2a$11$placeholder_yang5678' WHERE wecom_userid = 'test_procurement_002' AND name = '杨采购工程师';

-- procurement_manager (采购部经理)
UPDATE users SET username = 'he', password_hash = '$2a$11$placeholder_he1234' WHERE wecom_userid = 'test_procurement_manager_001' AND name = '何采购部经理';
UPDATE users SET username = 'lv', password_hash = '$2a$11$placeholder_lv5678' WHERE wecom_userid = 'test_procurement_manager_002' AND name = '吕采购部经理';

-- customer_service (客服工程师)
UPDATE users SET username = 'zhu', password_hash = '$2a$11$placeholder_zhu1234' WHERE wecom_userid = 'test_customer_service_001' AND name = '朱客服工程师';
UPDATE users SET username = 'qin', password_hash = '$2a$11$placeholder_qin5678' WHERE wecom_userid = 'test_customer_service_002' AND name = '秦客服工程师';

-- customer_service_manager (客服部经理)
UPDATE users SET username = 'you', password_hash = '$2a$11$placeholder_you1234' WHERE wecom_userid = 'test_customer_service_manager_001' AND name = '尤客服部经理';
UPDATE users SET username = 'xu', password_hash = '$2a$11$placeholder_xu5678' WHERE wecom_userid = 'test_customer_service_manager_002' AND name = '许客服部经理';

-- manager (项目经理)
UPDATE users SET username = 'shi', password_hash = '$2a$11$placeholder_shi1234' WHERE wecom_userid = 'test_manager_001' AND name = '施项目经理';
UPDATE users SET username = 'zhang2', password_hash = '$2a$11$placeholder_zhang21234' WHERE wecom_userid = 'test_manager_002' AND name = '张项目经理';

-- production_worker (生产工人)
UPDATE users SET username = 'kong', password_hash = '$2a$11$placeholder_kong1234' WHERE wecom_userid = 'test_production_worker_001' AND name = '孔生产工人';
UPDATE users SET username = 'cao', password_hash = '$2a$11$placeholder_cao5678' WHERE wecom_userid = 'test_production_worker_002' AND name = '曹生产工人';

-- 验证更新
SELECT username, name, role FROM users WHERE wecom_userid LIKE 'test_%' ORDER BY role, username;






