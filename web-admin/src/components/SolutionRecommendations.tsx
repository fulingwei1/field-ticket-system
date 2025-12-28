import React, { useState, useEffect } from 'react';
import {
  Card,
  Button,
  List,
  Tag,
  Progress,
  Space,
  Tooltip,
  Empty,
  Spin,
  message,
  Modal,
  Descriptions,
  Divider,
  Alert,
} from 'antd';
import {
  BulbOutlined,
  CheckCircleOutlined,
  WarningOutlined,
  InfoCircleOutlined,
  ThunderboltOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import solutionRecommendationService, {
  SolutionRecommendationDto,
  RecommendSolutionsResponse,
} from '../services/solutionRecommendationService';

interface SolutionRecommendationsProps {
  ticketId: string;
  onSelectSolution?: (solutionId: string) => void;
}

const SolutionRecommendations: React.FC<SolutionRecommendationsProps> = ({
  ticketId,
  onSelectSolution,
}) => {
  const [loading, setLoading] = useState(false);
  const [recommendations, setRecommendations] = useState<RecommendSolutionsResponse | null>(null);
  const [selectedRecommendation, setSelectedRecommendation] = useState<SolutionRecommendationDto | null>(null);
  const [detailModalVisible, setDetailModalVisible] = useState(false);

  // 加载推荐
  const loadRecommendations = async () => {
    setLoading(true);
    try {
      const response = await solutionRecommendationService.recommendSolutions({
        ticketId,
        topK: 5,
        minSimilarityScore: 0.3,
      });
      setRecommendations(response);

      if (response.recommendations.length === 0) {
        message.info(response.message || '暂无推荐方案');
      } else {
        message.success(`找到 ${response.recommendations.length} 个推荐方案`);
      }
    } catch (error: any) {
      message.error('加载推荐失败: ' + (error.message || '未知错误'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (ticketId) {
      loadRecommendations();
    }
  }, [ticketId]);

  // 查看详情
  const handleViewDetail = (recommendation: SolutionRecommendationDto) => {
    setSelectedRecommendation(recommendation);
    setDetailModalVisible(true);
  };

  // 应用方案
  const handleApply = (recommendation: SolutionRecommendationDto) => {
    Modal.confirm({
      title: '应用推荐方案',
      content: (
        <div>
          <p>确定要基于此方案创建新的解决方案吗？</p>
          <p><strong>方案: </strong>{recommendation.title}</p>
          <p><strong>来源工单: </strong>{recommendation.sourceTicketNo}</p>
          {!recommendation.isVersionCompatible && (
            <Alert
              message="版本不兼容"
              description={recommendation.versionCompatibilityMessage}
              type="warning"
              showIcon
              style={{ marginTop: 8 }}
            />
          )}
        </div>
      ),
      onOk: () => {
        if (onSelectSolution) {
          onSelectSolution(recommendation.solutionId);
        }
        message.success('已选择该方案作为参考');
      },
    });
  };

  // 渲染匹配分数
  const renderMatchScore = (score: number) => {
    const percent = Math.round(score * 100);
    const color = percent >= 70 ? 'success' : percent >= 50 ? 'warning' : 'exception';
    return (
      <Tooltip title={`匹配度: ${percent}%`}>
        <Progress
          type="circle"
          percent={percent}
          width={60}
          status={color as any}
        />
      </Tooltip>
    );
  };

  // 渲染成功率
  const renderSuccessRate = (stats: SolutionRecommendationDto['statistics']) => {
    const percent = Math.round(stats.successRate * 100);
    const color = percent >= 80 ? '#52c41a' : percent >= 60 ? '#faad14' : '#ff4d4f';
    return (
      <Tooltip title={`成功 ${stats.successCount} / 验证 ${stats.verificationCount}`}>
        <Tag color={color} icon={<CheckCircleOutlined />}>
          成功率 {percent}%
        </Tag>
      </Tooltip>
    );
  };

  // 渲染匹配原因标签
  const renderMatchReasons = (reasons: SolutionRecommendationDto['matchReasons']) => {
    const iconMap: Record<string, React.ReactNode> = {
      domain_match: <ThunderboltOutlined />,
      symptom_similarity: <BulbOutlined />,
      high_success_rate: <CheckCircleOutlined />,
      version_compatible: <InfoCircleOutlined />,
    };

    return (
      <Space size={[0, 4]} wrap>
        {reasons.map((reason, index) => (
          <Tooltip key={index} title={`权重: ${(reason.weight * 100).toFixed(0)}%`}>
            <Tag icon={iconMap[reason.reasonType]} color="blue">
              {reason.description}
            </Tag>
          </Tooltip>
        ))}
      </Space>
    );
  };

  // 渲染新鲜度
  const renderRecency = (days: number) => {
    if (days <= 30) {
      return <Tag color="green" icon={<ClockCircleOutlined />}>最近使用</Tag>;
    } else if (days <= 90) {
      return <Tag color="orange" icon={<ClockCircleOutlined />}>{days}天前</Tag>;
    } else {
      return <Tag color="default" icon={<ClockCircleOutlined />}>{days}天前</Tag>;
    }
  };

  if (loading) {
    return (
      <Card title="推荐解决方案">
        <div style={{ textAlign: 'center', padding: '40px 0' }}>
          <Spin size="large" tip="正在分析并推荐解决方案..." />
        </div>
      </Card>
    );
  }

  if (!recommendations || recommendations.recommendations.length === 0) {
    return (
      <Card title="推荐解决方案">
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description={recommendations?.message || '暂无推荐方案'}
        >
          <Button type="primary" onClick={loadRecommendations}>
            重新推荐
          </Button>
        </Empty>
      </Card>
    );
  }

  return (
    <>
      <Card
        title={
          <Space>
            <BulbOutlined />
            推荐解决方案
            <Tag color="blue">{recommendations.recommendations.length} 个推荐</Tag>
            <Tag color="default">来自 {recommendations.totalCandidates} 个候选方案</Tag>
          </Space>
        }
        extra={
          <Button onClick={loadRecommendations}>刷新推荐</Button>
        }
      >
        <List
          dataSource={recommendations.recommendations}
          renderItem={(item, index) => (
            <List.Item
              key={item.solutionId}
              actions={[
                <Button type="link" onClick={() => handleViewDetail(item)}>
                  查看详情
                </Button>,
                <Button type="primary" onClick={() => handleApply(item)}>
                  应用方案
                </Button>,
              ]}
            >
              <List.Item.Meta
                avatar={
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                    <Tag color="gold" style={{ marginBottom: 8 }}>
                      #{index + 1}
                    </Tag>
                    {renderMatchScore(item.matchScore)}
                  </div>
                }
                title={
                  <Space direction="vertical" size={4} style={{ width: '100%' }}>
                    <Space>
                      <span style={{ fontSize: 16, fontWeight: 500 }}>{item.title}</span>
                      <Tag color="blue">{item.solutionCode}</Tag>
                      {!item.isVersionCompatible && (
                        <Tooltip title={item.versionCompatibilityMessage}>
                          <Tag color="warning" icon={<WarningOutlined />}>
                            版本需调整
                          </Tag>
                        </Tooltip>
                      )}
                    </Space>
                    <Space size={[0, 4]} wrap>
                      <Tag>来源: {item.sourceTicketNo}</Tag>
                      <Tag>{item.solutionType}</Tag>
                      <Tag>{item.releaseType}</Tag>
                      {renderSuccessRate(item.statistics)}
                      {renderRecency(item.statistics.daysSinceLastUse)}
                    </Space>
                  </Space>
                }
                description={
                  <Space direction="vertical" size={8} style={{ width: '100%' }}>
                    {item.description && (
                      <div style={{ color: '#666' }}>{item.description}</div>
                    )}
                    <div>
                      <strong>匹配原因: </strong>
                      {renderMatchReasons(item.matchReasons)}
                    </div>
                  </Space>
                }
              />
            </List.Item>
          )}
        />
      </Card>

      {/* 详情弹窗 */}
      <Modal
        title={
          <Space>
            <BulbOutlined />
            解决方案详情
          </Space>
        }
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        width={800}
        footer={[
          <Button key="close" onClick={() => setDetailModalVisible(false)}>
            关闭
          </Button>,
          <Button
            key="apply"
            type="primary"
            onClick={() => {
              if (selectedRecommendation) {
                handleApply(selectedRecommendation);
                setDetailModalVisible(false);
              }
            }}
          >
            应用方案
          </Button>,
        ]}
      >
        {selectedRecommendation && (
          <div>
            <Descriptions bordered column={2} size="small">
              <Descriptions.Item label="方案编号" span={2}>
                {selectedRecommendation.solutionCode}
              </Descriptions.Item>
              <Descriptions.Item label="来源工单" span={2}>
                {selectedRecommendation.sourceTicketNo}
              </Descriptions.Item>
              <Descriptions.Item label="方案标题" span={2}>
                {selectedRecommendation.title}
              </Descriptions.Item>
              <Descriptions.Item label="方案描述" span={2}>
                {selectedRecommendation.description || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="方案类型">
                {selectedRecommendation.solutionType}
              </Descriptions.Item>
              <Descriptions.Item label="发布类型">
                {selectedRecommendation.releaseType}
              </Descriptions.Item>
              <Descriptions.Item label="匹配度">
                {renderMatchScore(selectedRecommendation.matchScore)}
              </Descriptions.Item>
              <Descriptions.Item label="成功率">
                {renderSuccessRate(selectedRecommendation.statistics)}
              </Descriptions.Item>
            </Descriptions>

            <Divider orientation="left">版本信息</Divider>
            <Descriptions bordered column={2} size="small">
              <Descriptions.Item label="前置软件版本">
                {selectedRecommendation.requiredSwVersion || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="新软件版本">
                {selectedRecommendation.newSwVersion || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="前置PLC版本">
                {selectedRecommendation.requiredPlcVersion || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="新PLC版本">
                {selectedRecommendation.newPlcVersion || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="版本兼容性" span={2}>
                {selectedRecommendation.isVersionCompatible ? (
                  <Tag color="success" icon={<CheckCircleOutlined />}>
                    完全兼容
                  </Tag>
                ) : (
                  <Tag color="warning" icon={<WarningOutlined />}>
                    {selectedRecommendation.versionCompatibilityMessage}
                  </Tag>
                )}
              </Descriptions.Item>
            </Descriptions>

            <Divider orientation="left">统计信息</Divider>
            <Descriptions bordered column={2} size="small">
              <Descriptions.Item label="总使用次数">
                {selectedRecommendation.statistics.totalUsageCount}
              </Descriptions.Item>
              <Descriptions.Item label="验证次数">
                {selectedRecommendation.statistics.verificationCount}
              </Descriptions.Item>
              <Descriptions.Item label="成功次数">
                <Tag color="success">{selectedRecommendation.statistics.successCount}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="失败次数">
                <Tag color="error">{selectedRecommendation.statistics.failureCount}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="成功率" span={2}>
                <Progress
                  percent={Math.round(selectedRecommendation.statistics.successRate * 100)}
                  status={
                    selectedRecommendation.statistics.successRate >= 0.8
                      ? 'success'
                      : selectedRecommendation.statistics.successRate >= 0.6
                      ? 'normal'
                      : 'exception'
                  }
                />
              </Descriptions.Item>
              <Descriptions.Item label="距上次使用" span={2}>
                {renderRecency(selectedRecommendation.statistics.daysSinceLastUse)}
              </Descriptions.Item>
            </Descriptions>

            <Divider orientation="left">匹配分析</Divider>
            <div>
              <strong>匹配原因：</strong>
              <div style={{ marginTop: 8 }}>
                {renderMatchReasons(selectedRecommendation.matchReasons)}
              </div>
            </div>
            <div style={{ marginTop: 16 }}>
              <strong>分数详情：</strong>
              <List
                size="small"
                dataSource={Object.entries(selectedRecommendation.scoreBreakdown)}
                renderItem={([key, value]) => (
                  <List.Item>
                    <span>{key}: </span>
                    <Progress
                      percent={Math.round(value * 100)}
                      size="small"
                      style={{ width: 200 }}
                    />
                  </List.Item>
                )}
              />
            </div>
          </div>
        )}
      </Modal>
    </>
  );
};

export default SolutionRecommendations;
