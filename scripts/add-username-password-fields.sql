-- 添加账户密码字段到用户表
-- 账户：姓名的拼音
-- 密码：拼音+身份证后四位（需要加密存储）

-- 1. 添加 username 字段（账户）
ALTER TABLE users ADD COLUMN IF NOT EXISTS username VARCHAR(50);
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash VARCHAR(255);

-- 2. 创建索引
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- 3. 添加唯一约束（用户名唯一）
ALTER TABLE users ADD CONSTRAINT users_username_unique UNIQUE (username);

-- 4. 添加注释
COMMENT ON COLUMN users.username IS '登录账户（姓名的拼音）';
COMMENT ON COLUMN users.password_hash IS '密码哈希值（BCrypt加密）';






