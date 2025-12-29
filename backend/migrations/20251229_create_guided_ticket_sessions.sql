-- Migration: Create guided_ticket_sessions table
-- Date: 2025-12-29
-- Description: 创建AI引导式工单创建会话表

-- 创建表
CREATE TABLE IF NOT EXISTS guided_ticket_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    ticket_id UUID REFERENCES tickets(ticket_id) ON DELETE SET NULL,
    
    -- 会话状态：collecting, analyzing, guiding, completing, completed
    status VARCHAR(20) NOT NULL DEFAULT 'collecting',
    turn_count INTEGER NOT NULL DEFAULT 0,
    max_turns INTEGER NOT NULL DEFAULT 3,
    
    -- 初始信息
    initial_text TEXT,
    image_attachment_ids UUID[],
    
    -- AI分析结果（JSONB）
    text_analysis JSONB,
    image_analyses JSONB[],
    comprehensive_analysis JSONB,
    
    -- 对话历史（JSONB）
    conversation_history JSONB NOT NULL DEFAULT '[]'::jsonb,
    
    -- 时间戳
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_guided_sessions_user_id ON guided_ticket_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_guided_sessions_status ON guided_ticket_sessions(status);
CREATE INDEX IF NOT EXISTS idx_guided_sessions_created_at ON guided_ticket_sessions(created_at);
CREATE INDEX IF NOT EXISTS idx_guided_sessions_ticket_id ON guided_ticket_sessions(ticket_id);

-- 添加注释
COMMENT ON TABLE guided_ticket_sessions IS 'AI引导式工单创建会话表';
COMMENT ON COLUMN guided_ticket_sessions.id IS '会话ID';
COMMENT ON COLUMN guided_ticket_sessions.user_id IS '用户ID';
COMMENT ON COLUMN guided_ticket_sessions.ticket_id IS '关联的工单ID（如果已生成）';
COMMENT ON COLUMN guided_ticket_sessions.status IS '会话状态：collecting-收集信息, analyzing-分析中, guiding-引导中, completing-完成中, completed-已完成';
COMMENT ON COLUMN guided_ticket_sessions.turn_count IS '当前对话轮数';
COMMENT ON COLUMN guided_ticket_sessions.max_turns IS '最大对话轮数';
COMMENT ON COLUMN guided_ticket_sessions.initial_text IS '初始文字描述';
COMMENT ON COLUMN guided_ticket_sessions.image_attachment_ids IS '图片附件ID列表';
COMMENT ON COLUMN guided_ticket_sessions.text_analysis IS '文本分析结果（JSONB）';
COMMENT ON COLUMN guided_ticket_sessions.image_analyses IS '图片分析结果列表（JSONB数组）';
COMMENT ON COLUMN guided_ticket_sessions.comprehensive_analysis IS '综合分析结果（JSONB）';
COMMENT ON COLUMN guided_ticket_sessions.conversation_history IS '对话历史（JSONB数组）';
COMMENT ON COLUMN guided_ticket_sessions.created_at IS '创建时间';
COMMENT ON COLUMN guided_ticket_sessions.updated_at IS '更新时间';
COMMENT ON COLUMN guided_ticket_sessions.completed_at IS '完成时间';

