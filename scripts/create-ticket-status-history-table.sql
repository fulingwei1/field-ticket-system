-- 创建 ticket_status_history 表（工单状态历史表）
-- 用于记录工单的状态变更历史

CREATE TABLE IF NOT EXISTS ticket_status_history (
    history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL,
    from_status VARCHAR(20) NOT NULL,
    to_status VARCHAR(20) NOT NULL,
    change_reason TEXT,
    changed_by UUID NOT NULL,
    changed_by_name VARCHAR(100),
    changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    change_type VARCHAR(20) DEFAULT 'manual',
    related_entity_id UUID,
    related_entity_type VARCHAR(50),
    notes TEXT
);

-- 添加外键约束（如果相关表存在）
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'tickets') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_ticket_status_history_ticket_id'
        ) THEN
            ALTER TABLE ticket_status_history 
            ADD CONSTRAINT fk_ticket_status_history_ticket_id 
            FOREIGN KEY (ticket_id) REFERENCES tickets(ticket_id) ON DELETE CASCADE;
        END IF;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_ticket_status_history_changed_by'
        ) THEN
            ALTER TABLE ticket_status_history 
            ADD CONSTRAINT fk_ticket_status_history_changed_by 
            FOREIGN KEY (changed_by) REFERENCES users(id);
        END IF;
    END IF;
END $$;

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_ticket_status_history_ticket ON ticket_status_history(ticket_id);
CREATE INDEX IF NOT EXISTS idx_ticket_status_history_changed_at ON ticket_status_history(changed_at);
CREATE INDEX IF NOT EXISTS idx_ticket_status_history_ticket_time ON ticket_status_history(ticket_id, changed_at);

-- 添加注释
COMMENT ON TABLE ticket_status_history IS '工单状态历史表，记录工单的状态变更历史';
COMMENT ON COLUMN ticket_status_history.history_id IS '历史记录ID';
COMMENT ON COLUMN ticket_status_history.ticket_id IS '关联的工单ID';
COMMENT ON COLUMN ticket_status_history.from_status IS '原状态';
COMMENT ON COLUMN ticket_status_history.to_status IS '新状态';
COMMENT ON COLUMN ticket_status_history.change_reason IS '状态变更原因';
COMMENT ON COLUMN ticket_status_history.changed_by IS '操作人ID';
COMMENT ON COLUMN ticket_status_history.changed_by_name IS '操作人姓名';
COMMENT ON COLUMN ticket_status_history.changed_at IS '变更时间';
COMMENT ON COLUMN ticket_status_history.change_type IS '变更类型（manual/auto/system）';
COMMENT ON COLUMN ticket_status_history.related_entity_id IS '关联实体ID';
COMMENT ON COLUMN ticket_status_history.related_entity_type IS '关联实体类型';
COMMENT ON COLUMN ticket_status_history.notes IS '备注';


