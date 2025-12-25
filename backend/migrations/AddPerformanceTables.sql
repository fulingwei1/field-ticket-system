-- ============================================
-- 绩效管理系统数据库迁移脚本
-- 创建时间: 2025-12-22
-- 说明: 创建绩效指标表和AI分析结果表
-- ============================================

-- 1. 创建绩效指标表
CREATE TABLE IF NOT EXISTS performance_metrics (
    metric_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 关联信息
    engineer_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    period_type VARCHAR(20) NOT NULL,  -- 'daily', 'weekly', 'monthly', 'quarterly', 'yearly'
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    
    -- 工单相关指标
    total_tickets INT DEFAULT 0,
    tickets_resolved INT DEFAULT 0,
    tickets_pending INT DEFAULT 0,
    average_resolution_time INTERVAL,
    first_time_resolution_rate DECIMAL(5, 2),
    
    -- 响应时间指标
    average_response_time INTERVAL,
    response_time_p95 INTERVAL,
    on_time_response_rate DECIMAL(5, 2),
    
    -- 设备故障率
    devices_serviced INT DEFAULT 0,
    device_failure_rate DECIMAL(5, 2),
    repeat_failure_rate DECIMAL(5, 2),
    
    -- 工作活动完整性
    work_activity_days INT DEFAULT 0,
    work_activity_completeness DECIMAL(5, 2),
    
    -- 工单创建质量指标
    ticket_creation_completeness DECIMAL(5, 2),
    field_feedback_timeliness_rate DECIMAL(5, 2),
    question_reply_timeliness_rate DECIMAL(5, 2),
    
    -- 问题解决能力指标
    verification_pass_rate DECIMAL(5, 2),
    repeat_problem_rate DECIMAL(5, 2),
    
    -- 技术诊断能力指标
    judgement_card_usage_accuracy DECIMAL(5, 2),
    judgement_card_hit_rate DECIMAL(5, 2),
    ai_suggestion_adoption_rate DECIMAL(5, 2),
    low_confidence_upgrade_timeliness DECIMAL(5, 2),
    
    -- 知识贡献指标
    judgement_cards_created INT DEFAULT 0,
    judgement_card_quality_score DECIMAL(5, 2),
    judgement_card_reuse_contribution DECIMAL(5, 2),
    solutions_contributed INT DEFAULT 0,
    
    -- 客户服务能力指标
    customer_communication_timeliness DECIMAL(5, 2),
    customer_communication_quality DECIMAL(5, 2),
    customer_satisfaction_score DECIMAL(3, 2),
    customer_feedback_count INT DEFAULT 0,
    
    -- 协作能力指标
    team_collaboration_activity DECIMAL(5, 2),
    knowledge_sharing_contribution DECIMAL(5, 2),
    
    -- 工作规范性指标
    ticket_information_completeness DECIMAL(5, 2),
    root_cause_attribution_completeness DECIMAL(5, 2),
    
    -- 综合评分
    overall_score DECIMAL(5, 2),
    performance_level VARCHAR(20),  -- 'excellent', 'good', 'average', 'below_average', 'poor'
    
    -- 排名
    rank_in_team INT,
    rank_in_department INT,
    
    -- 审计字段
    calculated_at TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- 唯一约束
    UNIQUE(engineer_id, period_type, period_start)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_performance_engineer ON performance_metrics(engineer_id, period_start DESC);
CREATE INDEX IF NOT EXISTS idx_performance_period ON performance_metrics(period_type, period_start DESC);
CREATE INDEX IF NOT EXISTS idx_performance_score ON performance_metrics(overall_score DESC);

-- 2. 创建AI分析结果表
CREATE TABLE IF NOT EXISTS ai_analysis_results (
    analysis_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- 分析范围
    analysis_type VARCHAR(50) NOT NULL,  -- 'daily_summary', 'weekly_summary', 'team_analysis', 'scheduling_suggestion'
    analysis_date DATE NOT NULL,
    engineer_id UUID REFERENCES users(id) ON DELETE SET NULL,
    department_id UUID,
    
    -- 分析内容
    summary TEXT NOT NULL,
    key_insights JSONB,
    suggestions JSONB,
    performance_analysis JSONB,
    
    -- AI模型信息
    ai_model VARCHAR(100),
    confidence_score DECIMAL(3, 2),
    
    -- 审计字段
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by UUID REFERENCES users(id) ON DELETE SET NULL
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_ai_analysis_type ON ai_analysis_results(analysis_type, analysis_date DESC);
CREATE INDEX IF NOT EXISTS idx_ai_analysis_engineer ON ai_analysis_results(engineer_id, analysis_date DESC);

-- 添加注释
COMMENT ON TABLE performance_metrics IS '绩效指标表（按周期统计）';
COMMENT ON TABLE ai_analysis_results IS 'AI分析结果表';
COMMENT ON COLUMN performance_metrics.period_type IS '周期类型：daily(日度), weekly(周度), monthly(月度), quarterly(季度), yearly(年度)';
COMMENT ON COLUMN performance_metrics.performance_level IS '绩效等级：excellent(优秀), good(良好), average(合格), below_average(待改进), poor(不合格)';
COMMENT ON COLUMN ai_analysis_results.analysis_type IS '分析类型：daily_summary(每日总结), weekly_summary(每周总结), team_analysis(团队分析), scheduling_suggestion(人员安排建议)';


