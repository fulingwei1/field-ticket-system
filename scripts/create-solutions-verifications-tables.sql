-- 创建 solutions 表（解决方案表）
CREATE TABLE IF NOT EXISTS solutions (
    solution_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    solution_code VARCHAR(50),
    ticket_id UUID NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    solution_type VARCHAR(50) NOT NULL,
    release_type VARCHAR(20) NOT NULL,
    required_sw_version VARCHAR(50),
    required_plc_version VARCHAR(50),
    required_param_version VARCHAR(50),
    new_sw_version VARCHAR(50),
    new_plc_version VARCHAR(50),
    new_param_version VARCHAR(50),
    change_detail_json JSONB DEFAULT '{}',
    verification_checklist_json JSONB DEFAULT '{}',
    implementation_steps TEXT,
    estimated_implementation_time INTEGER,
    risk_level VARCHAR(20) DEFAULT 'medium',
    risk_description TEXT,
    rollback_possible BOOLEAN DEFAULT TRUE,
    rollback_procedure TEXT,
    status VARCHAR(20) DEFAULT 'Draft',
    created_by UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    published_at TIMESTAMPTZ,
    published_by UUID,
    applicable_sw_versions VARCHAR(50)[],
    applicable_hw_versions VARCHAR(50)[],
    expiry_date DATE,
    is_expired BOOLEAN DEFAULT FALSE,
    verified_by UUID,
    last_verified_at TIMESTAMPTZ,
    verification_count INTEGER DEFAULT 0
);

-- 创建 verifications 表（验证表）
CREATE TABLE IF NOT EXISTS verifications (
    verification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL,
    solution_id UUID,
    verification_type VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL,
    executed_by UUID NOT NULL,
    executed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    verified_at TIMESTAMPTZ,
    verification_result TEXT,
    verification_notes TEXT,
    checklist_results JSONB,
    attachments JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 添加外键约束（如果相关表存在）
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'tickets') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_solutions_ticket_id'
        ) THEN
            ALTER TABLE solutions 
            ADD CONSTRAINT fk_solutions_ticket_id 
            FOREIGN KEY (ticket_id) REFERENCES tickets(ticket_id) ON DELETE RESTRICT;
        END IF;
        
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_verifications_ticket_id'
        ) THEN
            ALTER TABLE verifications 
            ADD CONSTRAINT fk_verifications_ticket_id 
            FOREIGN KEY (ticket_id) REFERENCES tickets(ticket_id) ON DELETE CASCADE;
        END IF;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_solutions_created_by'
        ) THEN
            ALTER TABLE solutions 
            ADD CONSTRAINT fk_solutions_created_by 
            FOREIGN KEY (created_by) REFERENCES users(id);
        END IF;
        
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_solutions_published_by'
        ) THEN
            ALTER TABLE solutions 
            ADD CONSTRAINT fk_solutions_published_by 
            FOREIGN KEY (published_by) REFERENCES users(id);
        END IF;
        
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_solutions_verified_by'
        ) THEN
            ALTER TABLE solutions 
            ADD CONSTRAINT fk_solutions_verified_by 
            FOREIGN KEY (verified_by) REFERENCES users(id);
        END IF;
        
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_verifications_executed_by'
        ) THEN
            ALTER TABLE verifications 
            ADD CONSTRAINT fk_verifications_executed_by 
            FOREIGN KEY (executed_by) REFERENCES users(id);
        END IF;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'solutions') THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints 
            WHERE constraint_name = 'fk_verifications_solution_id'
        ) THEN
            ALTER TABLE verifications 
            ADD CONSTRAINT fk_verifications_solution_id 
            FOREIGN KEY (solution_id) REFERENCES solutions(solution_id) ON DELETE SET NULL;
        END IF;
    END IF;
END $$;

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_solutions_ticket_id ON solutions(ticket_id);
CREATE INDEX IF NOT EXISTS idx_solutions_solution_code ON solutions(solution_code);
CREATE INDEX IF NOT EXISTS idx_solutions_created_by ON solutions(created_by);
CREATE INDEX IF NOT EXISTS idx_solutions_status ON solutions(status);
CREATE INDEX IF NOT EXISTS idx_solutions_created_at ON solutions(created_at);

CREATE INDEX IF NOT EXISTS idx_verifications_ticket_id ON verifications(ticket_id);
CREATE INDEX IF NOT EXISTS idx_verifications_solution_id ON verifications(solution_id);
CREATE INDEX IF NOT EXISTS idx_verifications_executed_by ON verifications(executed_by);
CREATE INDEX IF NOT EXISTS idx_verifications_status ON verifications(status);
CREATE INDEX IF NOT EXISTS idx_verifications_executed_at ON verifications(executed_at);

-- 添加注释
COMMENT ON TABLE solutions IS '解决方案表';
COMMENT ON TABLE verifications IS '验证表';


