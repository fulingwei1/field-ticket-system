-- =====================================================
-- 数据库迁移脚本：添加操作日志表
-- 版本: 004
-- 描述: 创建OperationLogs表用于记录员工导入、更新、开通等操作
-- 作者: Claude
-- 日期: 2025-12-28
-- =====================================================

-- 1. 创建操作日志表
CREATE TABLE IF NOT EXISTS "OperationLogs" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "OperationType" VARCHAR(50) NOT NULL,
    "OperatorId" UUID NOT NULL,
    "OperatorName" VARCHAR(100) NOT NULL,
    "OperatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Description" TEXT NOT NULL,
    "Result" VARCHAR(20) NOT NULL,
    "TotalCount" INTEGER NOT NULL DEFAULT 0,
    "SuccessCount" INTEGER NOT NULL DEFAULT 0,
    "FailedCount" INTEGER NOT NULL DEFAULT 0,
    "SkippedCount" INTEGER NOT NULL DEFAULT 0,
    "DetailsJson" TEXT,
    "ErrorMessagesJson" TEXT,
    "SourceFileName" VARCHAR(255),
    "IpAddress" VARCHAR(50),
    "UserAgent" TEXT
);

-- 2. 创建外键约束（操作人）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_OperationLogs_Operator'
    ) THEN
        ALTER TABLE "OperationLogs"
        ADD CONSTRAINT "FK_OperationLogs_Operator"
        FOREIGN KEY ("OperatorId")
        REFERENCES "Users"("Id")
        ON DELETE CASCADE;
    END IF;
END $$;

-- 3. 创建索引（提高查询性能）
CREATE INDEX IF NOT EXISTS "IX_OperationLogs_OperationType"
    ON "OperationLogs"("OperationType");

CREATE INDEX IF NOT EXISTS "IX_OperationLogs_OperatorId"
    ON "OperationLogs"("OperatorId");

CREATE INDEX IF NOT EXISTS "IX_OperationLogs_OperatedAt"
    ON "OperationLogs"("OperatedAt" DESC);

CREATE INDEX IF NOT EXISTS "IX_OperationLogs_Result"
    ON "OperationLogs"("Result");

-- 4. 创建复合索引（常用查询组合）
CREATE INDEX IF NOT EXISTS "IX_OperationLogs_Type_Time"
    ON "OperationLogs"("OperationType", "OperatedAt" DESC);

-- 5. 添加注释
COMMENT ON TABLE "OperationLogs" IS '操作日志表，记录员工导入、更新、账户开通等操作';
COMMENT ON COLUMN "OperationLogs"."OperationType" IS '操作类型：EmployeeImport, EmployeeUpdate, AccountActivation, AccountBatchActivation';
COMMENT ON COLUMN "OperationLogs"."Result" IS '操作结果：Success, Partial, Failed';
COMMENT ON COLUMN "OperationLogs"."DetailsJson" IS '详细数据（JSON格式），存储导入的员工列表、更新详情等';
COMMENT ON COLUMN "OperationLogs"."ErrorMessagesJson" IS '错误消息（JSON数组）';

-- 完成提示
SELECT 'Migration 004 completed: OperationLogs table created successfully' AS status;
