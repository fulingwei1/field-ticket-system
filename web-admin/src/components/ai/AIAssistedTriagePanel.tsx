"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Button,
  Space,
  Tag,
  message,
  Spin,
  Alert,
  List,
  Typography,
  Divider,
  Timeline,
  Rate,
  Collapse,
  Empty,
} from 'antd';
import {
  RobotOutlined,
  BulbOutlined,
  CheckCircleOutlined,
  WarningOutlined,
  ArrowRightOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import {
  aiAssistedTriageService,
  AIAssistedTriageResult,
  HypothesisDto,
  ActionSuggestionDto,
  MissingInfoQuestionDto,
} from '../../services/aiAssistedTriageService';

const { Title, Text, Paragraph } = Typography;
const { Panel } = Collapse;

interface AIAssistedTriagePanelProps {
  ticketId: string;
  jcCode?: string;
  onAcceptRecommendation?: (result: AIAssistedTriageResult) => void;
}

export default function AIAssistedTriagePanel({
  ticketId,
  jcCode,
  onAcceptRecommendation,
}: AIAssistedTriagePanelProps) {
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<AIAssistedTriageResult | null>(null);
  const [expandedPanels, setExpandedPanels] = useState<string[]>(['hypotheses']);

  useEffect(() => {
    if (ticketId) {
      loadAIAssistance();
    }
  }, [ticketId, jcCode]);

  const loadAIAssistance = async () => {
    try {
      setLoading(true);
      const aiResult = await aiAssistedTriageService.assistTriage(ticketId);
      setResult(aiResult);
    } catch (error: any) {
      message.error(`加载AI辅助建议失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleAcceptRecommendation = () => {
    if (result && onAcceptRecommendation) {
      onAcceptRecommendation(result);
      message.success('已采纳AI推荐');
    }
  };

  const getConfidenceColor = (level: string) => {
    switch (level) {
      case 'high':
        return 'success';
      case 'medium':
        return 'warning';
      case 'low':
        return 'error';
      default:
        return 'default';
    }
  };

  const getConfidenceText = (level: string) => {
    switch (level) {
      case 'high':
        return '高置信度';
      case 'medium':
        return '中置信度';
      case 'low':
        return '低置信度';
      default:
        return level;
    }
  };

  const getHypothesisConfidenceColor = (confidence: string) => {
    switch (confidence) {
      case 'high':
        return 'success';
      case 'medium':
        return 'warning';
      case 'low':
        return 'error';
      default:
        return 'default';
    }
  };

  if (loading && !result) {
    return (
      <Card>
        <Spin tip="AI正在分析工单..." />
      </Card>
    );
  }

  if (!result) {
    return (
      <Card>
        <Empty description="暂无AI辅助建议" />
        <Button
          type="primary"
          icon={<RobotOutlined />}
          onClick={loadAIAssistance}
          style={{ marginTop: '16px' }}
        >
          获取AI辅助建议
        </Button>
      </Card>
    );
  }

  return (
    <Card
      title={
        <Space>
          <RobotOutlined />
          <span>AI辅助分诊建议</span>
          <Tag color={getConfidenceColor(result.confidenceLevel)}>
            {getConfidenceText(result.confidenceLevel)}
          </Tag>
          {result.escalationRequired && (
            <Tag color="error">需要升级</Tag>
          )}
        </Space>
      }
      extra={
        <Button
          icon={<ReloadOutlined />}
          onClick={loadAIAssistance}
          loading={loading}
        >
          刷新
        </Button>
      }
    >
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        {/* 置信度提示 */}
        {result.escalationRequired && (
          <Alert
            message="置信度较低，建议升级处理"
            description={`当前置信度：${result.confidence}/5，建议由经验丰富的工程师处理。`}
            type="warning"
            showIcon
          />
        )}

        {/* 推荐判断卡 */}
        {result.recommendedJcCode && (
          <Card size="small" title="推荐判断卡">
            <Space direction="vertical" size="small">
              <div>
                <Text strong>{result.recommendedJcCode}</Text>
                {result.recommendedJcTitle && (
                  <Text type="secondary"> - {result.recommendedJcTitle}</Text>
                )}
              </div>
              <div>
                <Rate disabled value={result.confidence} count={5} />
                <Text type="secondary" style={{ marginLeft: '8px' }}>
                  置信度: {result.confidence}/5
                </Text>
              </div>
              {result.reasoning && (
                <Paragraph type="secondary" style={{ marginBottom: 0 }}>
                  {result.reasoning}
                </Paragraph>
              )}
              <Button
                type="primary"
                size="small"
                onClick={handleAcceptRecommendation}
              >
                采纳推荐
              </Button>
            </Space>
          </Card>
        )}

        {/* Top-3假设 */}
        {result.hypotheses.length > 0 && (
          <Card size="small" title={<><BulbOutlined /> Top-3假设</>}>
            <Timeline>
              {result.hypotheses.map((hypothesis) => (
                <Timeline.Item
                  key={hypothesis.id}
                  color={
                    hypothesis.confidence === 'high' ? 'green' :
                    hypothesis.confidence === 'medium' ? 'orange' : 'red'
                  }
                >
                  <Space direction="vertical" size="small">
                    <div>
                      <Tag color={getHypothesisConfidenceColor(hypothesis.confidence)}>
                        #{hypothesis.rank} - {hypothesis.confidence}
                      </Tag>
                      <Text strong>{hypothesis.description}</Text>
                    </div>
                    {hypothesis.evidence.length > 0 && (
                      <div>
                        <Text type="secondary">证据：</Text>
                        <ul style={{ marginTop: '4px', marginBottom: 0 }}>
                          {hypothesis.evidence.map((evidence, idx) => (
                            <li key={idx}>
                              <Text type="secondary">{evidence}</Text>
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </Space>
                </Timeline.Item>
              ))}
            </Timeline>
          </Card>
        )}

        {/* 动作建议 */}
        {result.actionSuggestions.length > 0 && (
          <Card size="small" title={<><ArrowRightOutlined /> 下一步动作建议</>}>
            <List
              dataSource={result.actionSuggestions}
              renderItem={(suggestion) => (
                <List.Item>
                  <List.Item.Meta
                    avatar={
                      suggestion.isVerifiable ? (
                        <CheckCircleOutlined style={{ color: '#52c41a' }} />
                      ) : (
                        <WarningOutlined style={{ color: '#faad14' }} />
                      )
                    }
                    title={
                      <Space>
                        <Text strong>{suggestion.description}</Text>
                        <Tag color={suggestion.priority >= 4 ? 'red' : 'blue'}>
                          优先级: {suggestion.priority}
                        </Tag>
                        {suggestion.isVerifiable && (
                          <Tag color="success">可验证</Tag>
                        )}
                      </Space>
                    }
                    description={
                      suggestion.verificationMethod && (
                        <Text type="secondary">
                          验证方法: {suggestion.verificationMethod}
                        </Text>
                      )
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        )}

        {/* 缺失信息问题 */}
        {result.missingInfoQuestions.length > 0 && (
          <Card size="small" title={<><WarningOutlined /> 缺失信息问题</>}>
            <List
              dataSource={result.missingInfoQuestions}
              renderItem={(question) => (
                <List.Item>
                  <List.Item.Meta
                    title={
                      <Space>
                        <Text>{question.question}</Text>
                        {question.isRequired && (
                          <Tag color="error">必填</Tag>
                        )}
                        <Tag>{question.questionType}</Tag>
                      </Space>
                    }
                    description={
                      question.explanation && (
                        <Text type="secondary">{question.explanation}</Text>
                      )
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        )}

        {/* 推荐假设和动作 */}
        {(result.recommendedHypothesis || result.recommendedNextAction) && (
          <Card size="small" title="AI推荐摘要">
            <Space direction="vertical" size="small">
              {result.recommendedHypothesis && (
                <div>
                  <Text strong>推荐假设：</Text>
                  <Text>{result.recommendedHypothesis}</Text>
                </div>
              )}
              {result.recommendedNextAction && (
                <div>
                  <Text strong>推荐动作：</Text>
                  <Text>{result.recommendedNextAction}</Text>
                </div>
              )}
            </Space>
          </Card>
        )}
      </Space>
    </Card>
  );
}





















