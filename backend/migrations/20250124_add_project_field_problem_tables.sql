-- Migration: Add Project, FieldProblem, and RootCauseAnalysis tables
-- Created: 2025-01-24
-- Description: 添加项目、现场问题和根本原因分析表

-- 创建项目表
CREATE TABLE IF NOT EXISTS projects (
    project_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_no VARCHAR(50) UNIQUE NOT NULL,
    project_name VARCHAR(200) NOT NULL,
    customer_id UUID NOT NULL,
    customer_name VARCHAR(200),
    device_type VARCHAR(50),
    industry_type VARCHAR(50),
    sales_amount DECIMAL(18, 2),
    quantity INTEGER DEFAULT 1,
    order_date DATE,
    required_delivery_date DATE,
    actual_delivery_date DATE,
    delivery_delay_days INTEGER,
    project_status VARCHAR(50) DEFAULT '进行中',
    project_manager_id UUID,
    project_manager_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    updated_by UUID
);

-- 创建现场问题表
CREATE TABLE IF NOT EXISTS field_problems (
    problem_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NOT NULL REFERENCES projects(project_id) ON DELETE CASCADE,
    problem_sequence INTEGER NOT NULL,
    problem_category VARCHAR(50) NOT NULL,
    problem_description TEXT NOT NULL,
    priority VARCHAR(10),
    found_date DATE NOT NULL,
    completed_date DATE,
    processing_days INTEGER,
    primary_department VARCHAR(100) NOT NULL,
    primary_responsible VARCHAR(100) NOT NULL,
    primary_responsible_id UUID,
    collaborating_department VARCHAR(100),
    collaborating_person VARCHAR(100),
    collaborating_person_id UUID,
    status VARCHAR(50) DEFAULT '待分配',
    solution TEXT,
    solution_details TEXT,
    verification_status VARCHAR(50),
    customer_feedback TEXT,
    satisfaction_score INTEGER CHECK (satisfaction_score >= 1 AND satisfaction_score <= 5),
    verified_at TIMESTAMP,
    verified_by VARCHAR(100),
    related_ticket_id UUID,
    related_ticket_no VARCHAR(50),
    knowledge_base_id VARCHAR(100),
    is_repeat_problem BOOLEAN DEFAULT FALSE,
    related_history_problem_id UUID,
    notes TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by UUID,
    updated_by UUID,
    UNIQUE(project_id, problem_sequence)
);

-- 创建根本原因分析表
CREATE TABLE IF NOT EXISTS root_cause_analyses (
    analysis_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    problem_id UUID NOT NULL UNIQUE REFERENCES field_problems(problem_id) ON DELETE CASCADE,
    why1 TEXT,
    why2 TEXT,
    why3 TEXT,
    why4 TEXT,
    why5 TEXT,
    root_cause TEXT NOT NULL,
    root_cause_category VARCHAR(50),
    preventive_measures TEXT,
    verification_method TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    analyzed_by UUID,
    analyzed_at TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_projects_project_no ON projects(project_no);
CREATE INDEX IF NOT EXISTS idx_projects_customer_id ON projects(customer_id);
CREATE INDEX IF NOT EXISTS idx_projects_project_status ON projects(project_status);

CREATE INDEX IF NOT EXISTS idx_field_problems_project_id ON field_problems(project_id);
CREATE INDEX IF NOT EXISTS idx_field_problems_status ON field_problems(status);
CREATE INDEX IF NOT EXISTS idx_field_problems_category ON field_problems(problem_category);
CREATE INDEX IF NOT EXISTS idx_field_problems_primary_responsible_id ON field_problems(primary_responsible_id);
CREATE INDEX IF NOT EXISTS idx_field_problems_project_sequence ON field_problems(project_id, problem_sequence);

CREATE INDEX IF NOT EXISTS idx_root_cause_analyses_problem_id ON root_cause_analyses(problem_id);

-- 添加注释
COMMENT ON TABLE projects IS '项目表';
COMMENT ON TABLE field_problems IS '现场问题表';
COMMENT ON TABLE root_cause_analyses IS '根本原因分析表';

COMMENT ON COLUMN projects.project_no IS '项目号（唯一）';
COMMENT ON COLUMN projects.delivery_delay_days IS '交货延期天数（自动计算）';
COMMENT ON COLUMN field_problems.problem_sequence IS '问题序号（项目内唯一）';
COMMENT ON COLUMN field_problems.processing_days IS '处理周期天数（自动计算）';
COMMENT ON COLUMN field_problems.satisfaction_score IS '满意度评分（1-5）';







