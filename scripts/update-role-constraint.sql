-- 更新用户表的角色约束，支持新的角色体系
-- 移除旧的约束，扩展角色字段长度

-- 1. 删除旧的 CHECK 约束
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_role_check;

-- 2. 扩展角色字段长度（支持更长的角色代码如 customer_service_manager）
ALTER TABLE users ALTER COLUMN role TYPE VARCHAR(50);

-- 3. 添加新的角色约束（包含所有16个角色）
ALTER TABLE users ADD CONSTRAINT users_role_check 
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
    ));

-- 验证更新
SELECT 
    column_name, 
    data_type, 
    character_maximum_length,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'users' AND column_name = 'role';






