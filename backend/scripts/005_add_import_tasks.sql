-- =====================================================
-- 数据库迁移脚本：添加导入任务表
-- 版本: 005
-- 描述: 创建ImportTasks表用于异步导入任务管理和进度跟踪
-- 作者: Claude
-- 日期: 2025-12-28
-- =====================================================

-- 1. 创建导入任务表
CREATE TABLE IF NOT EXISTS "ImportTasks" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TaskType" VARCHAR(50) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "StartedAt" TIMESTAMP WITH TIME ZONE,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "CreatedById" UUID NOT NULL,
    "CreatedByName" VARCHAR(100) NOT NULL,
    "FileName" VARCHAR(255) NOT NULL,
    "FilePath" TEXT,
    "TotalCount" INTEGER NOT NULL DEFAULT 0,
    "ProcessedCount" INTEGER NOT NULL DEFAULT 0,
    "SuccessCount" INTEGER NOT NULL DEFAULT 0,
    "FailedCount" INTEGER NOT NULL DEFAULT 0,
    "SkippedCount" INTEGER NOT NULL DEFAULT 0,
    "ProgressPercentage" INTEGER NOT NULL DEFAULT 0,
    "CurrentMessage" TEXT,
    "ResultJson" TEXT,
    "ErrorMessage" TEXT
);

-- 2. 创建外键约束（创建人）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_ImportTasks_CreatedBy'
    ) THEN
        ALTER TABLE "ImportTasks"
        ADD CONSTRAINT "FK_ImportTasks_CreatedBy"
        FOREIGN KEY ("CreatedById")
        REFERENCES "Users"("Id")
        ON DELETE CASCADE;
    END IF;
END $$;

-- 3. 创建索引（提高查询性能）
CREATE INDEX IF NOT EXISTS "IX_ImportTasks_TaskType"
    ON "ImportTasks"("TaskType");

CREATE INDEX IF NOT EXISTS "IX_ImportTasks_Status"
    ON "ImportTasks"("Status");

CREATE INDEX IF NOT EXISTS "IX_ImportTasks_CreatedById"
    ON "ImportTasks"("CreatedById");

CREATE INDEX IF NOT EXISTS "IX_ImportTasks_CreatedAt"
    ON "ImportTasks"("CreatedAt" DESC);

-- 4. 创建复合索引（常用查询组合）
CREATE INDEX IF NOT EXISTS "IX_ImportTasks_Status_CreatedAt"
    ON "ImportTasks"("Status", "CreatedAt" DESC);

-- 5. 添加注释
COMMENT ON TABLE "ImportTasks" IS '导入任务表，用于异步导入处理和进度跟踪';
COMMENT ON COLUMN "ImportTasks"."TaskType" IS '任务类型：EmployeeImport, EmployeeUpdate';
COMMENT ON COLUMN "ImportTasks"."Status" IS '任务状态：Pending, Processing, Completed, Failed';
COMMENT ON COLUMN "ImportTasks"."ProgressPercentage" IS '进度百分比 (0-100)';
COMMENT ON COLUMN "ImportTasks"."ResultJson" IS '任务结果（JSON格式）';

-- 完成提示
SELECT 'Migration 005 completed: ImportTasks table created successfully' AS status;
