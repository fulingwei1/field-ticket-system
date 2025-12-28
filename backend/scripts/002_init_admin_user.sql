-- ========================================
-- 初始化管理员用户脚本
-- 创建默认管理员账户
-- ========================================

-- 默认管理员账户信息：
-- 用户名: admin
-- 密码: Admin123!
-- 角色: Admin
-- 重要: 首次登录后请立即修改密码！

-- BCrypt Hash of "Admin123!" (使用C#代码生成)
-- 注意: 这个hash需要在实际部署时通过程序生成
-- 下面的hash是示例，实际使用时需要重新生成

DO $$
DECLARE
    admin_user_id UUID;
BEGIN
    -- 检查是否已存在admin用户
    IF NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'admin') THEN

        -- 生成新的UUID
        admin_user_id := gen_random_uuid();

        -- 插入管理员用户
        INSERT INTO "Users" (
            "Id",
            "Username",
            "Name",
            "Email",
            "Role",
            "LoginType",
            "PasswordHash",
            "IsActive",
            "MustChangePassword",
            "CreatedAt",
            "UpdatedAt"
        ) VALUES (
            admin_user_id,
            'admin',
            '系统管理员',
            'admin@example.com',
            'Admin',
            'Password',
            -- 这里需要填入BCrypt哈希后的密码
            -- 临时使用，部署时应通过程序生成
            '$2a$11$abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJK', -- 占位符
            true,
            true, -- 首次登录必须修改密码
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );

        RAISE NOTICE 'Admin user created successfully with ID: %', admin_user_id;
        RAISE NOTICE '默认用户名: admin';
        RAISE NOTICE '默认密码: Admin123!';
        RAISE NOTICE '重要: 首次登录后请立即修改密码！';

    ELSE
        RAISE NOTICE 'Admin user already exists, skipping creation.';
    END IF;
END $$;

-- 说明:
-- 1. 此脚本创建一个默认管理员账户
-- 2. 默认密码为 Admin123!（需要在部署时通过程序生成实际的BCrypt哈希）
-- 3. 用户首次登录后会被要求修改密码
-- 4. 建议在生产环境部署前修改默认密码
