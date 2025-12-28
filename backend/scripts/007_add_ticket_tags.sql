-- 007_add_ticket_tags.sql
-- 为工单表添加标签字段，支持批量标记功能

-- 添加 tags 字段（JSONB 类型）
ALTER TABLE tickets
ADD COLUMN IF NOT EXISTS tags JSONB DEFAULT '[]'::jsonb;

-- 添加注释
COMMENT ON COLUMN tickets.tags IS '工单标签列表（用于批量标记和分类）';

-- 创建 GIN 索引以提升标签查询性能
CREATE INDEX IF NOT EXISTS idx_tickets_tags
ON tickets USING GIN (tags);

-- 添加示例标签说明
COMMENT ON INDEX idx_tickets_tags IS '工单标签 GIN 索引，支持高效的标签查询';

-- 注意：标签示例
-- '["紧急", "客户投诉", "重复问题"]'::jsonb
-- '["待审核", "需要现场确认"]'::jsonb
