-- ============================================
-- 知识图谱构建系统数据库迁移脚本
-- 创建时间: 2025-12-22
-- 说明: 创建知识图谱节点表和边表
-- ============================================

-- 1. 创建知识图谱节点表
CREATE TABLE IF NOT EXISTS knowledge_graph_nodes (
    node_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    node_type VARCHAR(50) NOT NULL,  -- 'symptom', 'root_cause', 'solution', 'device', 'step'
    node_code VARCHAR(50) NOT NULL,
    node_name VARCHAR(200) NOT NULL,
    properties JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. 创建知识图谱边表
CREATE TABLE IF NOT EXISTS knowledge_graph_edges (
    edge_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_node_id UUID NOT NULL REFERENCES knowledge_graph_nodes(node_id) ON DELETE CASCADE,
    target_node_id UUID NOT NULL REFERENCES knowledge_graph_nodes(node_id) ON DELETE CASCADE,
    edge_type VARCHAR(50) NOT NULL,  -- 'causes', 'solves', 'related_to', 'depends_on'
    weight DECIMAL(3,2) DEFAULT 1.0,
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. 创建索引
CREATE INDEX IF NOT EXISTS idx_kg_nodes_type ON knowledge_graph_nodes(node_type);
CREATE INDEX IF NOT EXISTS idx_kg_nodes_code ON knowledge_graph_nodes(node_code);
CREATE INDEX IF NOT EXISTS idx_kg_edges_source ON knowledge_graph_edges(source_node_id);
CREATE INDEX IF NOT EXISTS idx_kg_edges_target ON knowledge_graph_edges(target_node_id);
CREATE INDEX IF NOT EXISTS idx_kg_edges_type ON knowledge_graph_edges(edge_type);
CREATE UNIQUE INDEX IF NOT EXISTS idx_kg_edges_unique ON knowledge_graph_edges(source_node_id, target_node_id, edge_type);

-- 4. 添加表注释
COMMENT ON TABLE knowledge_graph_nodes IS '知识图谱节点表，存储知识实体（症状、根因、解决方案等）';
COMMENT ON TABLE knowledge_graph_edges IS '知识图谱边表，存储知识实体之间的关系';
COMMENT ON COLUMN knowledge_graph_nodes.node_type IS '节点类型：symptom(症状), root_cause(根因), solution(解决方案), device(设备), step(步骤)';
COMMENT ON COLUMN knowledge_graph_edges.edge_type IS '边类型：causes(导致), solves(解决), related_to(相关), depends_on(依赖)';

