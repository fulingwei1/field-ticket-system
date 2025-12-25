-- ============================================
-- 知识版本管理系统数据库迁移脚本
-- 创建时间: 2025-12-22
-- 说明: 创建知识版本表和版本关联表
-- ============================================

-- 1. 创建知识版本表
CREATE TABLE IF NOT EXISTS knowledge_versions (
    version_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    knowledge_id UUID NOT NULL,
    knowledge_type VARCHAR(50) NOT NULL,  -- 'judgement_card', 'solution', 'faq'
    
    -- 版本信息
    version_number VARCHAR(20) NOT NULL,  -- 'v1.0', 'v1.1', 'v2.0'
    version_description TEXT,
    
    -- 内容
    content JSONB NOT NULL,
    
    -- 版本元数据
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    is_current BOOLEAN DEFAULT false,
    
    -- 变更信息
    change_type VARCHAR(50),  -- 'created', 'updated', 'deleted'
    change_reason TEXT,
    change_summary TEXT
);

-- 2. 创建知识版本关联表
CREATE TABLE IF NOT EXISTS knowledge_version_relations (
    relation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_version_id UUID NOT NULL REFERENCES knowledge_versions(version_id) ON DELETE CASCADE,
    target_version_id UUID NOT NULL REFERENCES knowledge_versions(version_id) ON DELETE CASCADE,
    relation_type VARCHAR(50) NOT NULL,  -- 'evolves_from', 'replaces', 'related_to'
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. 创建索引
CREATE INDEX IF NOT EXISTS idx_kv_knowledge ON knowledge_versions(knowledge_id, knowledge_type);
CREATE INDEX IF NOT EXISTS idx_kv_current ON knowledge_versions(knowledge_id, knowledge_type, is_current);
CREATE INDEX IF NOT EXISTS idx_kv_version_number ON knowledge_versions(version_number);
CREATE INDEX IF NOT EXISTS idx_kvr_source ON knowledge_version_relations(source_version_id);
CREATE INDEX IF NOT EXISTS idx_kvr_target ON knowledge_version_relations(target_version_id);

-- 4. 添加表注释
COMMENT ON TABLE knowledge_versions IS '知识版本表，存储知识的版本历史';
COMMENT ON TABLE knowledge_version_relations IS '知识版本关联表，用于版本对比和关系追踪';
COMMENT ON COLUMN knowledge_versions.knowledge_type IS '知识类型：judgement_card(判断卡), solution(解决方案), faq(常见问题)';
COMMENT ON COLUMN knowledge_versions.change_type IS '变更类型：created(创建), updated(更新), deleted(删除)';

