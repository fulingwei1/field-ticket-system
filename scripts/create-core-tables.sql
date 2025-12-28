-- 创建核心数据库表
-- 基于 ApplicationDbContext 配置生成

-- 创建 tickets 表（核心表）
CREATE TABLE IF NOT EXISTS tickets (
    ticket_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_no VARCHAR(20) NOT NULL UNIQUE,
    customer_id UUID NOT NULL,
    project_id UUID NOT NULL,
    device_id UUID NOT NULL,
    station_id UUID,
    created_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    domain CHAR(1) NOT NULL CHECK (domain IN ('A', 'B', 'C', 'D', 'E')),
    step_code VARCHAR(20) NOT NULL,
    step_name VARCHAR(100),
    symptom_title VARCHAR(200) NOT NULL,
    symptom_detail TEXT,
    repro_rate SMALLINT,
    reboot_recovers BOOLEAN,
    env_related BOOLEAN,
    sw_version VARCHAR(50) NOT NULL,
    plc_version VARCHAR(50) NOT NULL,
    param_version VARCHAR(50) NOT NULL,
    hw_version VARCHAR(50),
    facts_json JSONB DEFAULT '{}',
    actions_taken TEXT[] DEFAULT '{}',
    actions_taken_note TEXT,
    confirmed_as_fact BOOLEAN DEFAULT FALSE,
    confirmed_at TIMESTAMPTZ,
    alarm_code VARCHAR(50),
    status VARCHAR(20) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft', 'Submitted', 'Triage', 'SolutionIssued', 'Verifying', 'Closed')),
    priority VARCHAR(5) DEFAULT 'P3' CHECK (priority IN ('P1', 'P2', 'P3', 'P4')),
    current_jc_code VARCHAR(20),
    assigned_to UUID REFERENCES users(id) ON DELETE SET NULL,
    root_cause TEXT,
    root_responsibility VARCHAR(50),
    responsibility_team VARCHAR(50),
    is_preventable BOOLEAN,
    responsibility_notes TEXT,
    attributed_by UUID REFERENCES users(id) ON DELETE SET NULL,
    attributed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    submitted_at TIMESTAMPTZ,
    closed_at TIMESTAMPTZ,
    local_draft_id VARCHAR(64),
    idempotency_key VARCHAR(64) UNIQUE,
    merged_into UUID REFERENCES tickets(ticket_id) ON DELETE SET NULL,
    duplicate_of UUID REFERENCES tickets(ticket_id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_tickets_ticket_no ON tickets(ticket_no);
CREATE INDEX IF NOT EXISTS idx_tickets_status ON tickets(status);
CREATE INDEX IF NOT EXISTS idx_tickets_customer_id ON tickets(customer_id);
CREATE INDEX IF NOT EXISTS idx_tickets_device_id ON tickets(device_id);
CREATE INDEX IF NOT EXISTS idx_tickets_created_by_user_id ON tickets(created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_tickets_domain ON tickets(domain);
CREATE INDEX IF NOT EXISTS idx_tickets_created_at ON tickets(created_at);

-- 创建 attachments 表
CREATE TABLE IF NOT EXISTS attachments (
    attachment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id UUID NOT NULL REFERENCES tickets(ticket_id) ON DELETE CASCADE,
    uploaded_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    file_type VARCHAR(20) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_key VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100),
    sha256 VARCHAR(64),
    upload_status VARCHAR(20) DEFAULT 'completed',
    upload_id VARCHAR(200),
    uploaded_chunks INT DEFAULT 0,
    total_chunks INT,
    tags TEXT,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_attachments_ticket_id ON attachments(ticket_id);



