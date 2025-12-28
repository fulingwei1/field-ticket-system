-- ========================================
-- 用户表结构更新脚本
-- 添加用户名密码登录支持
-- ========================================

-- 1. 添加新字段到Users表
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "Username" VARCHAR(100),
ADD COLUMN IF NOT EXISTS "PasswordHash" TEXT,
ADD COLUMN IF NOT EXISTS "Email" VARCHAR(255),
ADD COLUMN IF NOT EXISTS "LoginType" VARCHAR(20) NOT NULL DEFAULT 'WeCom',
ADD COLUMN IF NOT EXISTS "LastPasswordChangeAt" TIMESTAMP,
ADD COLUMN IF NOT EXISTS "MustChangePassword" BOOLEAN NOT NULL DEFAULT false;

-- 2. 更新现有字段为可空（支持密码登录用户）
ALTER TABLE "Users"
ALTER COLUMN "CorpId" DROP NOT NULL,
ALTER COLUMN "WeComUserId" DROP NOT NULL;

-- 3. 添加索引
CREATE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username")
WHERE "Username" IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email")
WHERE "Email" IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_Users_LoginType" ON "Users" ("LoginType");

-- 4. 添加唯一约束
ALTER TABLE "Users"
ADD CONSTRAINT "UQ_Users_Username" UNIQUE ("Username")
WHERE "Username" IS NOT NULL;

-- 注意：执行完此脚本后，请运行初始化用户脚本创建默认管理员账户
