-- 006_add_field_problems.sql
-- 创建现场问题表，用于知识自动沉淀

-- 创建 field_problems 表
CREATE TABLE IF NOT EXISTS field_problems (
    problem_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- 关联信息
    project_id UUID NOT NULL,
    problem_sequence INTEGER NOT NULL,

    -- 问题基本信息
    problem_category VARCHAR(100) NOT NULL DEFAULT '',
    problem_description TEXT NOT NULL DEFAULT '',
    priority VARCHAR(20),

    -- 时间信息
    found_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_date TIMESTAMP WITH TIME ZONE,
    processing_days INTEGER,

    -- 责任信息
    primary_department VARCHAR(200) NOT NULL DEFAULT '',
    primary_responsible VARCHAR(200) NOT NULL DEFAULT '',
    primary_responsible_id UUID,
    collaborating_department VARCHAR(200),
    collaborating_person VARCHAR(200),
    collaborating_person_id UUID,

    -- 处理信息
    status VARCHAR(50) NOT NULL DEFAULT '待分配',
    solution TEXT,
    solution_details TEXT,

    -- 验证信息
    verification_status VARCHAR(50),
    customer_feedback TEXT,
    satisfaction_score INTEGER CHECK (satisfaction_score >= 1 AND satisfaction_score <= 5),
    verified_at TIMESTAMP WITH TIME ZONE,
    verified_by VARCHAR(200),

    -- 关联信息
    related_ticket_id UUID,
    related_ticket_no VARCHAR(50),
    knowledge_base_id VARCHAR(100),
    is_repeat_problem BOOLEAN NOT NULL DEFAULT FALSE,
    related_history_problem_id UUID,

    -- 备注
    notes TEXT,

    -- 审计字段
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID,

    -- 外键约束
    CONSTRAINT fk_field_problems_project FOREIGN KEY (project_id) REFERENCES projects(project_id) ON DELETE CASCADE,
    CONSTRAINT fk_field_problems_primary_responsible FOREIGN KEY (primary_responsible_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_field_problems_collaborating_person FOREIGN KEY (collaborating_person_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_field_problems_related_ticket FOREIGN KEY (related_ticket_id) REFERENCES tickets(ticket_id) ON DELETE SET NULL,
    CONSTRAINT fk_field_problems_related_history FOREIGN KEY (related_history_problem_id) REFERENCES field_problems(problem_id) ON DELETE SET NULL,
    CONSTRAINT fk_field_problems_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_field_problems_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL
);

-- 创建索引以提升查询性能
CREATE INDEX IF NOT EXISTS idx_field_problems_project_sequence
    ON field_problems(project_id, problem_sequence);

CREATE INDEX IF NOT EXISTS idx_field_problems_project_id
    ON field_problems(project_id);

CREATE INDEX IF NOT EXISTS idx_field_problems_status
    ON field_problems(status);

CREATE INDEX IF NOT EXISTS idx_field_problems_category
    ON field_problems(problem_category);

CREATE INDEX IF NOT EXISTS idx_field_problems_primary_responsible_id
    ON field_problems(primary_responsible_id);

CREATE INDEX IF NOT EXISTS idx_field_problems_related_ticket_id
    ON field_problems(related_ticket_id);

CREATE INDEX IF NOT EXISTS idx_field_problems_is_repeat
    ON field_problems(is_repeat_problem);

CREATE INDEX IF NOT EXISTS idx_field_problems_found_date
    ON field_problems(found_date);

CREATE INDEX IF NOT EXISTS idx_field_problems_created_at
    ON field_problems(created_at);

-- 添加唯一约束：同一项目内的问题序号必须唯一
CREATE UNIQUE INDEX IF NOT EXISTS uq_field_problems_project_sequence
    ON field_problems(project_id, problem_sequence);

-- 添加注释
COMMENT ON TABLE field_problems IS '现场问题表 - 用于工单自动知识沉淀';
COMMENT ON COLUMN field_problems.problem_id IS '问题唯一标识';
COMMENT ON COLUMN field_problems.project_id IS '关联项目ID';
COMMENT ON COLUMN field_problems.problem_sequence IS '项目内问题序号';
COMMENT ON COLUMN field_problems.problem_category IS '问题分类';
COMMENT ON COLUMN field_problems.problem_description IS '问题描述';
COMMENT ON COLUMN field_problems.processing_days IS '处理周期天数';
COMMENT ON COLUMN field_problems.is_repeat_problem IS '是否为重复问题';
COMMENT ON COLUMN field_problems.related_history_problem_id IS '关联的历史重复问题ID';
COMMENT ON COLUMN field_problems.related_ticket_id IS '关联工单ID（知识沉淀来源）';
COMMENT ON COLUMN field_problems.satisfaction_score IS '客户满意度评分（1-5分）';

-- 创建更新 updated_at 的触发器
CREATE OR REPLACE FUNCTION update_field_problems_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_field_problems_updated_at
    BEFORE UPDATE ON field_problems
    FOR EACH ROW
    EXECUTE FUNCTION update_field_problems_updated_at();

-- 授予必要的权限
-- GRANT SELECT, INSERT, UPDATE, DELETE ON field_problems TO your_app_user;
