-- 用户画像和智能阈值系统数据库迁移脚本
-- 创建日期: 2025-12-23
-- 说明: 添加用户画像、用户填写历史和智能阈值配置相关表

-- ============================================
-- 1. 用户画像表
-- ============================================
CREATE TABLE IF NOT EXISTS user_profiles (
    profile_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- 填写习惯（JSONB）
    common_fields JSONB NOT NULL DEFAULT '{}',
    
    -- 专业度评估
    expertise_level VARCHAR(20),  -- 'beginner', 'intermediate', 'expert'
    expertise_score DECIMAL(3,2),
    
    -- 学习数据
    total_tickets INT NOT NULL DEFAULT 0,
    average_completion_time INT,  -- 秒
    common_mistakes JSONB,
    
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 用户画像表索引
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_profiles_user_id ON user_profiles(user_id);

-- ============================================
-- 2. 用户填写历史表
-- ============================================
CREATE TABLE IF NOT EXISTS user_filling_history (
    history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    
    -- 填写数据（JSONB）
    filled_fields JSONB NOT NULL DEFAULT '{}',
    filling_time INT,  -- 秒
    skipped_fields JSONB,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 用户填写历史表索引
CREATE INDEX IF NOT EXISTS idx_user_filling_history_user_id ON user_filling_history(user_id);
CREATE INDEX IF NOT EXISTS idx_user_filling_history_ticket_id ON user_filling_history(ticket_id);
CREATE INDEX IF NOT EXISTS idx_user_filling_history_created_at ON user_filling_history(created_at);

-- ============================================
-- 3. 阈值配置表
-- ============================================
CREATE TABLE IF NOT EXISTS threshold_configs (
    config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_name VARCHAR(100) NOT NULL,
    
    -- 场景配置
    scenario_type VARCHAR(50) NOT NULL,  -- 'device_type', 'problem_type', 'severity', 'default'
    scenario_value VARCHAR(100),
    
    -- 阈值参数
    time_window_days INT NOT NULL DEFAULT 30,
    trigger_count INT NOT NULL DEFAULT 3,
    match_criteria JSONB NOT NULL DEFAULT '{}',
    /*
    示例:
    {
      "same_device": true,
      "same_symptom": true,
      "same_root_responsibility": true
    }
    */
    
    -- 效果评估
    trigger_rate DECIMAL(5,4),
    accuracy_rate DECIMAL(5,4),
    false_positive_rate DECIMAL(5,4),
    
    -- 状态
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_auto_optimized BOOLEAN NOT NULL DEFAULT false,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 阈值配置表索引
CREATE INDEX IF NOT EXISTS idx_threshold_configs_scenario ON threshold_configs(scenario_type, scenario_value);
CREATE INDEX IF NOT EXISTS idx_threshold_configs_active ON threshold_configs(is_active);

-- ============================================
-- 4. 阈值触发历史表
-- ============================================
CREATE TABLE IF NOT EXISTS threshold_trigger_history (
    history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id UUID NOT NULL REFERENCES threshold_configs(config_id) ON DELETE CASCADE,
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    
    -- 触发信息
    trigger_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    matched_tickets UUID[] NOT NULL DEFAULT '{}',
    actual_result VARCHAR(20),  -- 'correct', 'false_positive', 'false_negative'
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 阈值触发历史表索引
CREATE INDEX IF NOT EXISTS idx_threshold_trigger_history_config_id ON threshold_trigger_history(config_id);
CREATE INDEX IF NOT EXISTS idx_threshold_trigger_history_ticket_id ON threshold_trigger_history(ticket_id);
CREATE INDEX IF NOT EXISTS idx_threshold_trigger_history_trigger_time ON threshold_trigger_history(trigger_time);

-- ============================================
-- 5. 添加注释
-- ============================================
COMMENT ON TABLE user_profiles IS '用户画像表，存储用户填写习惯和专业度评估';
COMMENT ON TABLE user_filling_history IS '用户填写历史表，记录用户填写工单的详细信息';
COMMENT ON TABLE threshold_configs IS '阈值配置表，存储智能阈值调整的配置信息';
COMMENT ON TABLE threshold_trigger_history IS '阈值触发历史表，记录阈值触发的历史记录';

COMMENT ON COLUMN user_profiles.common_fields IS '常用字段JSON，包含常用设备型号、问题域、症状等';
COMMENT ON COLUMN user_profiles.expertise_level IS '专业度等级：beginner(初级), intermediate(中级), expert(专家)';
COMMENT ON COLUMN user_profiles.expertise_score IS '专业度评分，0-1之间';
COMMENT ON COLUMN threshold_configs.scenario_type IS '场景类型：device_type(设备类型), problem_type(问题类型), severity(严重程度), default(默认)';
COMMENT ON COLUMN threshold_configs.match_criteria IS '匹配条件JSON，定义触发整改任务的条件';
COMMENT ON COLUMN threshold_trigger_history.actual_result IS '实际结果：correct(正确触发), false_positive(误触发), false_negative(漏触发)';

