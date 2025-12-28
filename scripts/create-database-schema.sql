-- 创建数据库表结构
-- 基于 ApplicationDbContext 的配置生成

-- 创建 users 表
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    corp_id VARCHAR(64) NOT NULL,
    wecom_userid VARCHAR(64) NOT NULL,
    name VARCHAR(100) NOT NULL,
    mobile VARCHAR(20),
    dept_id VARCHAR(64),
    role VARCHAR(20) NOT NULL CHECK (role IN ('FieldEngineer', 'CS', 'SeniorEngineer', 'Admin')),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(corp_id, wecom_userid)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_users_corp_wecom ON users(corp_id, wecom_userid);
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);

-- 添加表注释
COMMENT ON TABLE users IS '用户表';
COMMENT ON COLUMN users.corp_id IS '企业微信企业ID';
COMMENT ON COLUMN users.wecom_userid IS '企业微信用户ID';
COMMENT ON COLUMN users.role IS '用户角色：FieldEngineer(现场工程师), CS(客服), SeniorEngineer(高级工程师), Admin(管理员)';



