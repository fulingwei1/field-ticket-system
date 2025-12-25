-- ============================================
-- 工单归因字段数据库迁移脚本
-- 创建时间: 2025-12-22
-- 说明: 为tickets表添加责任归因相关字段
-- ============================================

-- 添加责任归因字段
ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS root_responsibility VARCHAR(50)
        CHECK (root_responsibility IN ('design', 'software', 'parameter', 'assembly', 'documentation', 'other', 'unknown'));

ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS is_preventable BOOLEAN;

ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS responsibility_notes TEXT;

ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS attributed_by UUID REFERENCES users(id);

ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS attributed_at TIMESTAMPTZ;

-- 添加根因字段（如果不存在）
ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS root_cause TEXT;

-- 添加责任团队字段（如果不存在）
ALTER TABLE tickets 
    ADD COLUMN IF NOT EXISTS responsibility_team VARCHAR(50);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_tickets_root_responsibility ON tickets(root_responsibility);
CREATE INDEX IF NOT EXISTS idx_tickets_is_preventable ON tickets(is_preventable);
CREATE INDEX IF NOT EXISTS idx_tickets_attributed_by ON tickets(attributed_by);
CREATE INDEX IF NOT EXISTS idx_tickets_attributed_at ON tickets(attributed_at);

-- 添加表注释
COMMENT ON COLUMN tickets.root_responsibility IS '根因分类：design(设计), software(软件), parameter(参数), assembly(装配), documentation(文档), other(其他), unknown(未知)';
COMMENT ON COLUMN tickets.is_preventable IS '是否可预防：true(可预防), false(不可预防)';
COMMENT ON COLUMN tickets.responsibility_notes IS '责任归因备注';
COMMENT ON COLUMN tickets.attributed_by IS '归因操作人';
COMMENT ON COLUMN tickets.attributed_at IS '归因操作时间';

