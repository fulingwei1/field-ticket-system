-- ============================================
-- 置信度校准系统数据库迁移脚本
-- 创建时间: 2025-12-22
-- 说明: 创建置信度校准记录表和校准模型表
-- ============================================

-- 1. 创建置信度校准记录表
CREATE TABLE IF NOT EXISTS confidence_calibration_records (
    record_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    hypothesis_id VARCHAR(50),
    
    -- 原始置信度
    original_confidence DECIMAL(3,2) NOT NULL,
    
    -- 校准后置信度
    calibrated_confidence DECIMAL(3,2),
    
    -- 实际结果
    actual_result VARCHAR(20),  -- 'correct', 'incorrect', 'partial'
    
    -- 校准信息
    calibration_method VARCHAR(50),  -- 'historical', 'context', 'user_feedback'
    calibration_factors JSONB,
    /*
    {
      "historical_accuracy": 0.85,
      "context_match": 0.9,
      "user_feedback": 0.8,
      "evidence_strength": 0.75
    }
    */
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. 创建置信度校准模型表
CREATE TABLE IF NOT EXISTS confidence_calibration_models (
    model_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    model_version VARCHAR(20) NOT NULL,
    model_type VARCHAR(50) NOT NULL,  -- 'linear', 'ml', 'ensemble'
    
    -- 模型参数
    model_parameters JSONB NOT NULL,
    
    -- 模型性能
    accuracy DECIMAL(5,4),
    precision_score DECIMAL(5,4),
    recall_score DECIMAL(5,4),
    f1_score DECIMAL(5,4),
    
    -- 训练信息
    training_data_count INT,
    trained_at TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT false,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. 创建索引
CREATE INDEX IF NOT EXISTS idx_calibration_records_ticket ON confidence_calibration_records(ticket_id);
CREATE INDEX IF NOT EXISTS idx_calibration_records_hypothesis ON confidence_calibration_records(hypothesis_id);
CREATE INDEX IF NOT EXISTS idx_calibration_records_created ON confidence_calibration_records(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_calibration_models_active ON confidence_calibration_models(is_active);
CREATE INDEX IF NOT EXISTS idx_calibration_models_version ON confidence_calibration_models(model_version);

-- 4. 添加表注释
COMMENT ON TABLE confidence_calibration_records IS '置信度校准记录表，记录每次校准的原始置信度、校准后置信度和实际结果';
COMMENT ON TABLE confidence_calibration_models IS '置信度校准模型表，存储训练好的校准模型';
COMMENT ON COLUMN confidence_calibration_records.actual_result IS '实际结果：correct(正确), incorrect(错误), partial(部分正确)';
COMMENT ON COLUMN confidence_calibration_records.calibration_method IS '校准方法：historical(历史数据), context(上下文), user_feedback(用户反馈)';
COMMENT ON COLUMN confidence_calibration_models.model_type IS '模型类型：linear(线性), ml(机器学习), ensemble(集成)';

