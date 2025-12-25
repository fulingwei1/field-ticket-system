import React, { useEffect, useState } from 'react';
import {
  Card,
  Typography,
  Descriptions,
  Tag,
  Spin,
  message,
  Button,
  Space,
  Collapse,
  Empty,
} from 'antd';
import {
  ArrowLeftOutlined,
  FileTextOutlined,
  BulbOutlined,
  TeamOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import { aiAnalysisService, AiAnalysisResultDto } from '../../services/aiAnalysisService';
import { useNavigate, useParams } from 'react-router-dom';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { Panel } = Collapse;

const AiAnalysisDetail: React.FC = () => {
  const { analysisId } = useParams<{ analysisId: string }>();
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<AiAnalysisResultDto | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (analysisId) {
      loadDetail();
    }
  }, [analysisId]);

  const loadDetail = async () => {
    if (!analysisId) return;

    setLoading(true);
    try {
      const data = await aiAnalysisService.getAnalysisResult(analysisId);
      setResult(data);
    } catch (error: any) {
      message.error(error.message || '加载分析结果失败');
      console.error('Failed to load analysis detail:', error);
    } finally {
      setLoading(false);
    }
  };

  const getAnalysisTypeLabel = (type: string) => {
    const map: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
      daily_summary: { label: '每日总结', color: 'blue', icon: <CalendarOutlined /> },
      weekly_summary: { label: '每周总结', color: 'green', icon: <FileTextOutlined /> },
      team_analysis: { label: '团队分析', color: 'orange', icon: <TeamOutlined /> },
      scheduling_suggestion: { label: '人员安排建议', color: 'purple', icon: <BulbOutlined /> },
    };
    return map[type] || { label: type, color: 'default', icon: <FileTextOutlined /> };
  };

  const renderKeyInsights = (insights: any) => {
    if (!insights) return <Empty description="暂无关键洞察" />;

    // 如果 insights 是字符串（JSON字符串），先解析
    let parsedInsights = insights;
    if (typeof insights === 'string') {
      try {
        parsedInsights = JSON.parse(insights);
      } catch {
        return <Paragraph>{insights}</Paragraph>;
      }
    }

    if (typeof parsedInsights !== 'object' || parsedInsights === null) {
      return <Paragraph>{String(parsedInsights)}</Paragraph>;
    }

    return (
      <Descriptions column={1} bordered>
        {Object.entries(parsedInsights).map(([key, value]) => (
          <Descriptions.Item key={key} label={key}>
            {typeof value === 'object' && value !== null ? (
              <pre style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                {JSON.stringify(value, null, 2)}
              </pre>
            ) : (
              String(value)
            )}
          </Descriptions.Item>
        ))}
      </Descriptions>
    );
  };

  const renderSuggestions = (suggestions: any) => {
    if (!suggestions) return <Empty description="暂无建议" />;

    // 如果 suggestions 是字符串（JSON字符串），先解析
    let parsedSuggestions = suggestions;
    if (typeof suggestions === 'string') {
      try {
        parsedSuggestions = JSON.parse(suggestions);
      } catch {
        return <Paragraph>{suggestions}</Paragraph>;
      }
    }

    if (Array.isArray(parsedSuggestions)) {
      return (
        <ul>
          {parsedSuggestions.map((suggestion: any, index: number) => (
            <li key={index}>
              <Text strong>{suggestion.suggestion || suggestion}</Text>
              {suggestion.reason && (
                <div>
                  <Text type="secondary">原因：{suggestion.reason}</Text>
                </div>
              )}
              {suggestion.priority && (
                <Tag color={suggestion.priority === 'high' ? 'red' : 'orange'}>
                  {suggestion.priority === 'high' ? '高优先级' : '中优先级'}
                </Tag>
              )}
            </li>
          ))}
        </ul>
      );
    }

    return (
      <Collapse>
        {Object.entries(parsedSuggestions).map(([key, value]: [string, any]) => (
          <Panel header={key} key={key}>
            {Array.isArray(value) ? (
              <ul>
                {value.map((item: any, index: number) => (
                  <li key={index}>
                    <Text>{JSON.stringify(item, null, 2)}</Text>
                  </li>
                ))}
              </ul>
            ) : (
              <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(value, null, 2)}</pre>
            )}
          </Panel>
        ))}
      </Collapse>
    );
  };

  const renderPerformanceAnalysis = (analysis: any) => {
    if (!analysis) return <Empty description="暂无绩效分析" />;

    // 如果 analysis 是字符串（JSON字符串），先解析
    let parsedAnalysis = analysis;
    if (typeof analysis === 'string') {
      try {
        parsedAnalysis = JSON.parse(analysis);
      } catch {
        return <Paragraph>{analysis}</Paragraph>;
      }
    }

    if (typeof parsedAnalysis !== 'object' || parsedAnalysis === null) {
      return <Paragraph>{String(parsedAnalysis)}</Paragraph>;
    }

    return (
      <Descriptions column={1} bordered>
        {Object.entries(parsedAnalysis).map(([key, value]) => (
          <Descriptions.Item key={key} label={key}>
            {typeof value === 'object' && value !== null ? (
              <pre style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                {JSON.stringify(value, null, 2)}
              </pre>
            ) : (
              String(value)
            )}
          </Descriptions.Item>
        ))}
      </Descriptions>
    );
  };

  if (!result) {
    return (
      <div style={{ padding: '24px', textAlign: 'center' }}>
        <Spin spinning={loading} />
        {!loading && <Empty description="分析结果不存在" />}
      </div>
    );
  }

  const typeInfo = getAnalysisTypeLabel(result.analysisType);

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px' }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/ai-analysis')}
          style={{ marginBottom: '16px' }}
        >
          返回列表
        </Button>
        <Title level={2}>
          <Space>
            {typeInfo.icon}
            {typeInfo.label}
          </Space>
        </Title>
      </div>

      <Spin spinning={loading}>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          {/* 基本信息 */}
          <Card title="基本信息">
            <Descriptions column={2}>
              <Descriptions.Item label="分析类型">
                <Tag color={typeInfo.color}>{typeInfo.label}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="分析日期">
                {dayjs(result.analysisDate).format('YYYY-MM-DD')}
              </Descriptions.Item>
              {result.engineerName && (
                <Descriptions.Item label="工程师">
                  {result.engineerName}
                </Descriptions.Item>
              )}
              <Descriptions.Item label="AI模型">
                {result.aiModel || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="置信度">
                {result.confidenceScore
                  ? `${(result.confidenceScore * 100).toFixed(0)}%`
                  : '-'}
              </Descriptions.Item>
              <Descriptions.Item label="创建时间">
                {dayjs(result.createdAt).format('YYYY-MM-DD HH:mm:ss')}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          {/* 工作摘要 */}
          <Card title="工作摘要" icon={<FileTextOutlined />}>
            <Paragraph>{result.summary}</Paragraph>
          </Card>

          {/* 关键洞察 */}
          {result.keyInsights && (
            <Card title="关键洞察" icon={<BulbOutlined />}>
              {renderKeyInsights(result.keyInsights)}
            </Card>
          )}

          {/* 建议 */}
          {result.suggestions && (
            <Card title="建议" icon={<BulbOutlined />}>
              {renderSuggestions(result.suggestions)}
            </Card>
          )}

          {/* 绩效分析 */}
          {result.performanceAnalysis && (
            <Card title="绩效分析" icon={<TeamOutlined />}>
              {renderPerformanceAnalysis(result.performanceAnalysis)}
            </Card>
          )}
        </Space>
      </Spin>
    </div>
  );
};

export default AiAnalysisDetail;

