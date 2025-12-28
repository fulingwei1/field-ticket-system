-- 初始化用户数据
-- 每个角色创建2个用户

-- 清空现有测试用户（可选，如果不想清空可以注释掉）
-- DELETE FROM users WHERE wecom_userid LIKE 'test_%';

-- FieldEngineer 角色用户（现场工程师）
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_fieldengineer_001', '张工程师', '13800001001', 'dept_001', 'FieldEngineer', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_fieldengineer_002', '李工程师', '13800001002', 'dept_001', 'FieldEngineer', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- CS 角色用户（客服）
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_cs_001', '王客服', '13800002001', 'dept_002', 'CS', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_cs_002', '赵客服', '13800002002', 'dept_002', 'CS', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- SeniorEngineer 角色用户（高级工程师）
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_seniorengineer_001', '陈高级工程师', '13800003001', 'dept_003', 'SeniorEngineer', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_seniorengineer_002', '刘高级工程师', '13800003002', 'dept_003', 'SeniorEngineer', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- Admin 角色用户（管理员）
INSERT INTO users (id, corp_id, wecom_userid, name, mobile, dept_id, role, is_active, created_at, updated_at)
VALUES 
    (gen_random_uuid(), 'test_corp', 'test_admin_001', '周管理员', '13800004001', 'dept_004', 'Admin', true, NOW(), NOW()),
    (gen_random_uuid(), 'test_corp', 'test_admin_002', '吴管理员', '13800004002', 'dept_004', 'Admin', true, NOW(), NOW())
ON CONFLICT (corp_id, wecom_userid) DO UPDATE SET
    name = EXCLUDED.name,
    mobile = EXCLUDED.mobile,
    role = EXCLUDED.role,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

-- 查询验证
SELECT role, COUNT(*) as user_count, array_agg(name) as user_names
FROM users
WHERE wecom_userid LIKE 'test_%'
GROUP BY role
ORDER BY role;



