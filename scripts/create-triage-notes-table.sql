-- 创建 triage_notes 表（分诊记录表）
-- 用于记录工单的分诊信息

-- 先创建表，不添加外键约束
CREATE TABLE IF NOT EXISTS triage_notes (
    triage_note_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL,
    jc_code VARCHAR(20),
    current_hypothesis TEXT,
    next_action TEXT,
    confidence INTEGER NOT NULL CHECK (confidence >= 1 AND confidence <= 5),
    escalation_required BOOLEAN DEFAULT FALSE,
    escalated_to UUID,
    note TEXT,
    created_by UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 添加外键约束（如果相关表存在）
DO $$
BEGIN
    -- 检查 tickets 表是否存在，如果存在则添加外键
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'tickets') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_triage_notes_ticket_id'
        ) THEN
            ALTER TABLE triage_notes 
            ADD CONSTRAINT fk_triage_notes_ticket_id 
            FOREIGN KEY (ticket_id) REFERENCES tickets(ticket_id) ON DELETE CASCADE;
        END IF;
    END IF;
    
    -- 检查 users 表是否存在，如果存在则添加外键
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_triage_notes_created_by'
        ) THEN
            ALTER TABLE triage_notes 
            ADD CONSTRAINT fk_triage_notes_created_by 
            FOREIGN KEY (created_by) REFERENCES users(id);
        END IF;
        
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_triage_notes_escalated_to'
        ) THEN
            ALTER TABLE triage_notes 
            ADD CONSTRAINT fk_triage_notes_escalated_to 
            FOREIGN KEY (escalated_to) REFERENCES users(id);
        END IF;
    END IF;
    
    -- 检查 judgement_cards 表是否存在，如果存在则添加外键
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'judgement_cards') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_triage_notes_jc_code'
        ) THEN
            ALTER TABLE triage_notes 
            ADD CONSTRAINT fk_triage_notes_jc_code 
            FOREIGN KEY (jc_code) REFERENCES judgement_cards(jc_code);
        END IF;
    END IF;
END $$;

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_triage_notes_ticket_id ON triage_notes(ticket_id);
CREATE INDEX IF NOT EXISTS idx_triage_notes_jc_code ON triage_notes(jc_code);
CREATE INDEX IF NOT EXISTS idx_triage_notes_created_by ON triage_notes(created_by);
CREATE INDEX IF NOT EXISTS idx_triage_notes_created_at ON triage_notes(created_at);

-- 添加注释
COMMENT ON TABLE triage_notes IS '分诊记录表，记录工单的分诊信息';
COMMENT ON COLUMN triage_notes.triage_note_id IS '分诊记录ID';
COMMENT ON COLUMN triage_notes.ticket_id IS '关联的工单ID';
COMMENT ON COLUMN triage_notes.jc_code IS '关联的判断卡编号';
COMMENT ON COLUMN triage_notes.current_hypothesis IS '当前判断结论';
COMMENT ON COLUMN triage_notes.next_action IS '下一步动作';
COMMENT ON COLUMN triage_notes.confidence IS '置信度（1-5）';
COMMENT ON COLUMN triage_notes.escalation_required IS '是否需要升级';
COMMENT ON COLUMN triage_notes.escalated_to IS '升级到的用户ID';
COMMENT ON COLUMN triage_notes.note IS '分诊备注';
COMMENT ON COLUMN triage_notes.created_by IS '创建人ID';
COMMENT ON COLUMN triage_notes.created_at IS '创建时间';
