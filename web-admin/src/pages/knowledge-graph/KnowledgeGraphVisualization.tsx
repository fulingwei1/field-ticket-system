"use client";

import { useState, useEffect } from 'react';
import { Card, Button, Space, message, Spin, Input, Select, Table, Tag, Alert } from 'antd';
import { ReloadOutlined, SearchOutlined, BuildOutlined } from '@ant-design/icons';
import { knowledgeGraphService, KnowledgeNodeDto, KnowledgeRelationDto, KnowledgeGraphDto } from '../../services/knowledgeGraphService';
import { SimpleGraph } from '../../components/common/SimpleGraph';

const { Option } = Select;

export default function KnowledgeGraphVisualization() {
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [nodeType, setNodeType] = useState<string | undefined>();
  const [searchResults, setSearchResults] = useState<KnowledgeNodeDto[]>([]);
  const [relations, setRelations] = useState<KnowledgeRelationDto[]>([]);
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  const [graphData, setGraphData] = useState<KnowledgeGraphDto | null>(null);

  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      message.warning('请输入搜索关键词');
      return;
    }
    try {
      setLoading(true);
      const results = await knowledgeGraphService.searchKnowledge(searchQuery, 10, nodeType);
      setSearchResults(results);
    } catch (error: any) {
      message.error(error.message || '搜索失败');
    } finally {
      setLoading(false);
    }
  };

  const handleBuildGraph = async () => {
    try {
      setLoading(true);
      const graph = await knowledgeGraphService.buildKnowledgeGraph();
      setGraphData(graph);
      message.success('知识图谱构建成功');
    } catch (error: any) {
      message.error(error.message || '构建失败');
    } finally {
      setLoading(false);
    }
  };

  const handleLoadRelations = async (nodeId: string) => {
    try {
      setLoading(true);
      const rels = await knowledgeGraphService.getKnowledgeRelations(nodeId);
      setRelations(rels);
      setSelectedNode(nodeId);
    } catch (error: any) {
      message.error(error.message || '加载关系失败');
    } finally {
      setLoading(false);
    }
  };

  const nodeTypeMap: Record<string, string> = {
    symptom: '症状',
    root_cause: '根因',
    solution: '解决方案',
    device: '设备',
    step: '步骤',
  };

  const edgeTypeMap: Record<string, string> = {
    causes: '导致',
    solves: '解决',
    related_to: '相关',
    depends_on: '依赖',
  };

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 头部 */}
        <Card>
          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <h2 style={{ margin: 0 }}>知识图谱可视化</h2>
            <Button icon={<BuildOutlined />} onClick={handleBuildGraph} loading={loading}>
              构建知识图谱
            </Button>
          </Space>
        </Card>

        {/* 搜索 */}
        <Card title="知识检索">
          <Space style={{ width: '100%' }}>
            <Input
              placeholder="输入搜索关键词"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onPressEnter={handleSearch}
              style={{ width: '300px' }}
            />
            <Select
              placeholder="节点类型"
              allowClear
              style={{ width: '150px' }}
              value={nodeType}
              onChange={setNodeType}
            >
              <Option value="symptom">症状</Option>
              <Option value="root_cause">根因</Option>
              <Option value="solution">解决方案</Option>
              <Option value="device">设备</Option>
              <Option value="step">步骤</Option>
            </Select>
            <Button type="primary" icon={<SearchOutlined />} onClick={handleSearch} loading={loading}>
              搜索
            </Button>
          </Space>
        </Card>

        {/* 搜索结果 */}
        {searchResults.length > 0 && (
          <Card title="搜索结果">
            <Table
              columns={[
                {
                  title: '节点名称',
                  dataIndex: 'nodeName',
                  key: 'nodeName',
                },
                {
                  title: '节点类型',
                  dataIndex: 'nodeType',
                  key: 'nodeType',
                  render: (type: string) => <Tag>{nodeTypeMap[type] || type}</Tag>,
                },
                {
                  title: '节点代码',
                  dataIndex: 'nodeCode',
                  key: 'nodeCode',
                },
                {
                  title: '操作',
                  key: 'action',
                  render: (_: any, record: KnowledgeNodeDto) => (
                    <Button
                      type="link"
                      size="small"
                      onClick={() => handleLoadRelations(record.nodeId)}
                    >
                      查看关系
                    </Button>
                  ),
                },
              ]}
              dataSource={searchResults}
              rowKey="nodeId"
              pagination={false}
            />
          </Card>
        )}

        {/* 知识图谱可视化 */}
        {graphData && graphData.nodes.length > 0 && (
          <SimpleGraph
            nodes={graphData.nodes.map((node) => ({
              id: node.nodeId,
              label: node.nodeName,
              type: node.nodeType,
            }))}
            edges={graphData.edges.map((edge) => ({
              id: edge.edgeId,
              source: edge.sourceNodeId,
              target: edge.targetNodeId,
              type: edge.edgeType,
              weight: edge.weight,
            }))}
            width={800}
            height={600}
          />
        )}

        {/* 知识关系 */}
        {relations.length > 0 && (
          <Card
            title={`节点关系${selectedNode ? ` (节点ID: ${selectedNode})` : ''}`}
            extra={
              <Button icon={<ReloadOutlined />} onClick={() => setRelations([])}>
                清除
              </Button>
            }
          >
            <Table
              columns={[
                {
                  title: '源节点',
                  dataIndex: 'sourceNodeName',
                  key: 'sourceNodeName',
                },
                {
                  title: '关系类型',
                  dataIndex: 'edgeType',
                  key: 'edgeType',
                  render: (type: string) => <Tag color="blue">{edgeTypeMap[type] || type}</Tag>,
                },
                {
                  title: '目标节点',
                  dataIndex: 'targetNodeName',
                  key: 'targetNodeName',
                },
                {
                  title: '权重',
                  dataIndex: 'weight',
                  key: 'weight',
                  render: (weight: number) => weight.toFixed(2),
                },
              ]}
              dataSource={relations}
              rowKey="edgeId"
              pagination={false}
            />
          </Card>
        )}

        {searchResults.length === 0 && relations.length === 0 && (
          <Card>
            <Alert
              message="暂无数据"
              description="请使用搜索功能查找知识节点，或点击'构建知识图谱'按钮从工单中提取知识关系"
              type="info"
              showIcon
            />
          </Card>
        )}
      </Space>
    </div>
  );
}

