-- 初始化用户数据 - 新角色体系
-- 每个角色创建2个用户，共16个角色，32个用户

-- 清空现有测试用户（可选）
-- DELETE FROM users WHERE wecom_userid LIKE 'test_%';

-- admin (管理员) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_admin_001', '周管理员', '13800001001', 'dept_admin', 'admin', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_admin_002', '吴管理员', '13800001002', 'dept_admin', 'admin', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- sales_engineer (销售工程师) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_sales_engineer_001', '张销售工程师', '13800002001', 'dept_sales', 'sales_engineer', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_sales_engineer_002', '李销售工程师', '13800002002', 'dept_sales', 'sales_engineer', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- sales_manager (销售经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_sales_manager_001', '王销售经理', '13800003001', 'dept_sales', 'sales_manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_sales_manager_002', '赵销售经理', '13800003002', 'dept_sales', 'sales_manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- presales (售前技术) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_presales_001', '陈售前技术', '13800004001', 'dept_sales', 'presales', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_presales_002', '刘售前技术', '13800004002', 'dept_sales', 'presales', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- mechanical (机械工程师) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_mechanical_001', '孙机械工程师', '13800005001', 'dept_mechanical', 'mechanical', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_mechanical_002', '钱机械工程师', '13800005002', 'dept_mechanical', 'mechanical', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- mechanical_manager (机械部经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_mechanical_manager_001', '周机械部经理', '13800006001', 'dept_mechanical', 'mechanical_manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_mechanical_manager_002', '吴机械部经理', '13800006002', 'dept_mechanical', 'mechanical_manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- electrical (电气工程师) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_electrical_001', '郑电气工程师', '13800007001', 'dept_electrical', 'electrical', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_electrical_002', '王电气工程师', '13800007002', 'dept_electrical', 'electrical', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- electrical_manager (电气部经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_electrical_manager_001', '冯电气部经理', '13800008001', 'dept_electrical', 'electrical_manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_electrical_manager_002', '陈电气部经理', '13800008002', 'dept_electrical', 'electrical_manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- test (测试工程师) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_test_001', '褚测试工程师', '13800009001', 'dept_test', 'test', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_test_002', '卫测试工程师', '13800009002', 'dept_test', 'test', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- test_manager (测试部经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_test_manager_001', '蒋测试部经理', '13800010001', 'dept_test', 'test_manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_test_manager_002', '沈测试部经理', '13800010002', 'dept_test', 'test_manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- procurement (采购工程师) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_procurement_001', '韩采购工程师', '13800011001', 'dept_procurement', 'procurement', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_procurement_002', '杨采购工程师', '13800011002', 'dept_procurement', 'procurement', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- customer_service (客服工程师) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_customer_service_001', '朱客服工程师', '13800013001', 'dept_customer_service', 'customer_service', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_customer_service_002', '秦客服工程师', '13800013002', 'dept_customer_service', 'customer_service', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- customer_service_manager (客服部经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_customer_service_manager_001', '尤客服部经理', '13800014001', 'dept_customer_service', 'customer_service_manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_customer_service_manager_002', '许客服部经理', '13800014002', 'dept_customer_service', 'customer_service_manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- procurement_manager (采购部经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_procurement_manager_001', '何采购部经理', '13800012001', 'dept_procurement', 'procurement_manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_procurement_manager_002', '吕采购部经理', '13800012002', 'dept_procurement', 'procurement_manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- manager (项目经理) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_manager_001', '施项目经理', '13800015001', 'dept_project', 'manager', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_manager_002', '张项目经理', '13800015002', 'dept_project', 'manager', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- production_worker (生产工人) - 2人
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_production_worker_001', '孔生产工人', '13800016001', 'dept_production', 'production_worker', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_production_worker_002', '曹生产工人', '13800016002', 'dept_production', 'production_worker', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- 查询验证
SELECT role, COUNT(*) as user_count, array_agg(name ORDER BY name) as user_names
FROM users
WHERE wecom_userid LIKE 'test_%'
GROUP BY role
ORDER BY role;



