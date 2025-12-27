-- ========================================
-- 用户表扩展 - 添加员工管理字段
-- 支持Excel批量导入员工信息
-- ========================================

-- 1. 添加部门名称字段
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "DeptName" VARCHAR(200);

-- 2. 添加上级关系字段
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "SupervisorId" UUID;

ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "SupervisorName" VARCHAR(100);

-- 3. 添加身份证后4位字段（用于密码生成）
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "IdCardLastFour" VARCHAR(4);

-- 4. 添加账户开通状态字段
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "IsActivated" BOOLEAN NOT NULL DEFAULT false;

-- 5. 创建外键约束（上级关系）
ALTER TABLE "Users"
ADD CONSTRAINT "FK_Users_Supervisor"
FOREIGN KEY ("SupervisorId")
REFERENCES "Users"("Id")
ON DELETE SET NULL;

-- 6. 创建索引
CREATE INDEX IF NOT EXISTS "IX_Users_SupervisorId"
ON "Users" ("SupervisorId");

CREATE INDEX IF NOT EXISTS "IX_Users_DeptName"
ON "Users" ("DeptName");

CREATE INDEX IF NOT EXISTS "IX_Users_IsActivated"
ON "Users" ("IsActivated");

-- 7. 添加注释
COMMENT ON COLUMN "Users"."DeptName" IS '部门名称';
COMMENT ON COLUMN "Users"."SupervisorId" IS '直接上级用户ID';
COMMENT ON COLUMN "Users"."SupervisorName" IS '直接上级姓名（冗余字段，方便查询）';
COMMENT ON COLUMN "Users"."IdCardLastFour" IS '身份证后4位（用于生成默认密码）';
COMMENT ON COLUMN "Users"."IsActivated" IS '是否已开通账户（导入后需要管理员手动开通）';

-- 执行完成
SELECT 'Migration 003: Employee fields added successfully' AS status;
