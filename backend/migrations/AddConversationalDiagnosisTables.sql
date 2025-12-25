-- ============================================
-- 多轮对话式诊断系统数据库迁移脚本
-- 创建时间: 2025-12-22
-- 说明: 创建诊断对话表和假设验证步骤表
-- ============================================

-- 1. 创建诊断对话表
CREATE TABLE IF NOT EXISTS diagnosis_conversations (
    conversation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    
    -- 对话状态
    current_hypothesis TEXT,
    current_confidence DECIMAL(3,2),
    conversation_round INT DEFAULT 0,
    status VARCHAR(20) DEFAULT 'active',  -- 'active', 'completed', 'abandoned'
    
    -- 诊断路径（JSONB）
    diagnosis_path JSONB,
    /*
    {
      "rounds": [
        {
          "round": 1,
          "hypothesis": "假设1",
          "confidence": 0.7,
          "verification_steps": [...],
          "user_feedback": {...},
          "adjusted_hypothesis": "调整后的假设"
        }
      ],
      "final_hypothesis": "最终假设",
      "final_confidence": 0.9
    }
    */
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. 创建假设验证步骤表
CREATE TABLE IF NOT EXISTS hypothesis_verification_steps (
    step_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES diagnosis_conversations(conversation_id) ON DELETE CASCADE,
    hypothesis_id VARCHAR(50),
    
    -- 验证步骤
    step_description TEXT NOT NULL,
    step_type VARCHAR(50),  -- 'check', 'test', 'observe', 'measure'
    expected_result TEXT,
    actual_result TEXT,
    
    -- 验证结果
    verification_status VARCHAR(20) DEFAULT 'pending',  -- 'pending', 'passed', 'failed', 'inconclusive'
    verification_notes TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. 创建索引
CREATE INDEX IF NOT EXISTS idx_diagnosis_conversations_ticket ON diagnosis_conversations(ticket_id);
CREATE INDEX IF NOT EXISTS idx_diagnosis_conversations_status ON diagnosis_conversations(status);
CREATE INDEX IF NOT EXISTS idx_verification_steps_conversation ON hypothesis_verification_steps(conversation_id);
CREATE INDEX IF NOT EXISTS idx_verification_steps_status ON hypothesis_verification_steps(verification_status);

-- 4. 添加表注释
COMMENT ON TABLE diagnosis_conversations IS '诊断对话表，记录多轮对话式诊断的会话信息';
COMMENT ON TABLE hypothesis_verification_steps IS '假设验证步骤表，记录每个假设的验证步骤和结果';
COMMENT ON COLUMN diagnosis_conversations.status IS '对话状态：active(进行中), completed(已完成), abandoned(已放弃)';
COMMENT ON COLUMN hypothesis_verification_steps.verification_status IS '验证状态：pending(待验证), passed(通过), failed(失败), inconclusive(不确定)';


