import React, { useEffect, useState } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  DatePicker,
  Select,
  Spin,
  message,
  Table,
  Tag,
  Space,
  Typography,
  Button,
  Timeline,
  Alert,
} from 'antd';
import {
  UserOutlined,
  TeamOutlined,
  ReloadOutlined,
  TrophyOutlined,
  RiseOutlined,
  FallOutlined,
} from '@ant-design/icons';
import {
  newcomerGrowthService,
  NewcomerGrowthCurveDto,
  TeamAverageGrowthCurveDto,
  GrowthMilestoneDto,
  GrowthMetricsDto,
} from '../../services/newcomerGrowthService';
import { authService } from '../../services/authService';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Option } = Select;

const NewcomerGrowthCurve: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [curve, setCurve] = useState<NewcomerGrowthCurveDto | null>(null);
  const [teamCurve, setTeamCurve] = useState<TeamAverageGrowthCurveDto | null>(null);
  const [milestones, setMilestones] = useState<GrowthMilestoneDto[]>([]);
  const [metrics, setMetrics] = useState<GrowthMetricsDto | null>(null);
  const [viewMode, setViewMode] = useState<'personal' | 'team'>('personal');
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs]>([
    dayjs().subtract(6, 'month'),
    dayjs(),
  ]);

  const user = authService.getUser();

  useEffect(() => {
    if (user?.id) {
      loadData();
    }
  }, [user, viewMode, dateRange]);

  const loadData = async () => {
    setLoading(true);
    try {
      const startDate = dateRange[0].format('YYYY-MM-DD');
      const endDate = dateRange[1].format('YYYY-MM-DD');

      if (viewMode === 'personal') {
        const [curveData, milestonesData, metricsData, teamData] = await Promise.all([
          newcomerGrowthService.getPersonalGrowthCurve(undefined, startDate, endDate),
          newcomerGrowthService.getGrowthMilestones(),
          newcomerGrowthService.calculateGrowthMetrics(undefined, startDate, endDate),
          newcomerGrowthService.getTeamAverageGrowthCurve(undefined, startDate, endDate),
        ]);
        setCurve(curveData);
        setMilestones(milestonesData);
        setMetrics(metricsData);
        setTeamCurve(teamData);
      } else {
        const teamData = await newcomerGrowthService.getTeamAverageGrowthCurve(
          undefined,
          startDate,
          endDate
        );
        setTeamCurve(teamData);
      }
    } catch (error: any) {
      message.error(error.message || '加载数据失败');
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  };

  const getTrendColor = (trend: number) => {
    if (trend > 0.1) return '#52c41a';
    if (trend < -0.1) return '#ff4d4f';
    return '#faad14';
  };

  const getTrendIcon = (trend: number) => {
    if (trend > 0.1) return <RiseOutlined style={{ color: '#52c41a' }} />;
    if (trend < -0.1) return <FallOutlined style={{ color: '#ff4d4f' }} />;
    return null;
  };

  const getGrowthTrendText = (trend: string) => {
    switch (trend) {
      case 'improving':
        return '上升';
      case 'declining':
        return '下降';
      case 'stable':
        return '稳定';
      default:
        return trend;
    }
  };

  const getGrowthTrendColor = (trend: string) => {
    switch (trend) {
      case 'improving':
        return 'success';
      case 'declining':
        return 'error';
      case 'stable':
        return 'default';
      default:
        return 'default';
    }
  };

  const growthDataColumns = [
    {
      title: '日期',
      dataIndex: 'date',
      key: 'date',
      width: 120,
    },
    {
      title: '判断卡质量',
      dataIndex: 'judgementCardQualityScore',
      key: 'judgementCardQualityScore',
      width: 120,
      render: (score: number) => <Text>{score.toFixed(2)}</Text>,
    },
    {
      title: '置信度准确性',
      dataIndex: 'confidenceAccuracy',
      key: 'confidenceAccuracy',
      width: 120,
      render: (score: number) => <Text>{score.toFixed(2)}%</Text>,
    },
    {
      title: 'AI采纳率',
      dataIndex: 'aiAdoptionRate',
      key: 'aiAdoptionRate',
      width: 100,
      render: (rate: number) => <Text>{rate.toFixed(2)}%</Text>,
    },
    {
      title: '一次解决率',
      dataIndex: 'firstTimeResolutionRate',
      key: 'firstTimeResolutionRate',
      width: 120,
      render: (rate: number) => <Text>{rate.toFixed(2)}%</Text>,
    },
    {
      title: '处理工单',
      dataIndex: 'ticketsHandled',
      key: 'ticketsHandled',
      width: 100,
    },
    {
      title: '创建判断卡',
      dataIndex: 'judgementCardsCreated',
      key: 'judgementCardsCreated',
      width: 120,
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 页面标题和筛选 */}
        <Card>
          <Row justify="space-between" align="middle">
            <Col>
              <Title level={2} style={{ margin: 0 }}>
                新人成长曲线
              </Title>
            </Col>
            <Col>
              <Space>
                <Select
                  value={viewMode}
                  onChange={setViewMode}
                  style={{ width: 120 }}
                >
                  <Option value="personal">
                    <UserOutlined /> 个人
                  </Option>
                  <Option value="team">
                    <TeamOutlined /> 团队
                  </Option>
                </Select>
                <RangePicker
                  value={dateRange}
                  onChange={(dates) => {
                    if (dates) {
                      setDateRange([dates[0]!, dates[1]!]);
                    }
                  }}
                />
                <Button icon={<ReloadOutlined />} onClick={loadData}>
                  刷新
                </Button>
              </Space>
            </Col>
          </Row>
        </Card>

        <Spin spinning={loading}>
          {viewMode === 'personal' && curve && (
            <>
              {/* 成长摘要 */}
              <Row gutter={16}>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="平均判断卡质量"
                      value={curve.summary.averageJudgementCardQuality}
                      precision={2}
                      suffix="分"
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="平均置信度准确性"
                      value={curve.summary.averageConfidenceAccuracy}
                      precision={2}
                      suffix="%"
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="平均AI采纳率"
                      value={curve.summary.averageAiAdoptionRate}
                      precision={2}
                      suffix="%"
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="平均一次解决率"
                      value={curve.summary.averageFirstTimeResolutionRate}
                      precision={2}
                      suffix="%"
                    />
                  </Card>
                </Col>
              </Row>

              {/* 成长趋势 */}
              {curve.summary && (
                <Card title="成长趋势" style={{ marginTop: 16 }}>
                  <Space>
                    <Tag color={getGrowthTrendColor(curve.summary.growthTrend)}>
                      {getGrowthTrendText(curve.summary.growthTrend)}
                    </Tag>
                    <Text>
                      总处理工单：{curve.summary.totalTicketsHandled} 个
                    </Text>
                    <Text>
                      总创建判断卡：{curve.summary.totalJudgementCardsCreated} 个
                    </Text>
                  </Space>
                </Card>
              )}

              {/* 成长指标 */}
              {metrics && (
                <Card title="成长指标分析" style={{ marginTop: 16 }}>
                  <Row gutter={16}>
                    <Col xs={24} sm={12} md={6}>
                      <Card size="small">
                        <Statistic
                          title="判断卡质量趋势"
                          value={metrics.judgementCardQualityTrend}
                          precision={3}
                          prefix={getTrendIcon(metrics.judgementCardQualityTrend)}
                          valueStyle={{ color: getTrendColor(metrics.judgementCardQualityTrend) }}
                        />
                      </Card>
                    </Col>
                    <Col xs={24} sm={12} md={6}>
                      <Card size="small">
                        <Statistic
                          title="置信度准确性趋势"
                          value={metrics.confidenceAccuracyTrend}
                          precision={3}
                          prefix={getTrendIcon(metrics.confidenceAccuracyTrend)}
                          valueStyle={{ color: getTrendColor(metrics.confidenceAccuracyTrend) }}
                        />
                      </Card>
                    </Col>
                    <Col xs={24} sm={12} md={6}>
                      <Card size="small">
                        <Statistic
                          title="AI采纳趋势"
                          value={metrics.aiAdoptionTrend}
                          precision={3}
                          prefix={getTrendIcon(metrics.aiAdoptionTrend)}
                          valueStyle={{ color: getTrendColor(metrics.aiAdoptionTrend) }}
                        />
                      </Card>
                    </Col>
                    <Col xs={24} sm={12} md={6}>
                      <Card size="small">
                        <Statistic
                          title="一次解决率趋势"
                          value={metrics.firstTimeResolutionTrend}
                          precision={3}
                          prefix={getTrendIcon(metrics.firstTimeResolutionTrend)}
                          valueStyle={{ color: getTrendColor(metrics.firstTimeResolutionTrend) }}
                        />
                      </Card>
                    </Col>
                  </Row>
                  {metrics.recommendations.length > 0 && (
                    <Alert
                      message="成长建议"
                      description={
                        <ul style={{ marginTop: 8, marginBottom: 0 }}>
                          {metrics.recommendations.map((rec, index) => (
                            <li key={index}>{rec}</li>
                          ))}
                        </ul>
                      }
                      type="info"
                      showIcon
                      style={{ marginTop: 16 }}
                    />
                  )}
                </Card>
              )}

              {/* 成长里程碑 */}
              {milestones.length > 0 && (
                <Card title="成长里程碑" style={{ marginTop: 16 }}>
                  <Timeline>
                    {milestones.map((milestone, index) => (
                      <Timeline.Item
                        key={index}
                        color="green"
                        dot={<TrophyOutlined />}
                      >
                        <Space direction="vertical" size="small">
                          <Text strong>{milestone.title}</Text>
                          <Text type="secondary">{milestone.description}</Text>
                          <Text type="secondary" style={{ fontSize: '12px' }}>
                            {milestone.achievedDate}
                            {milestone.score !== undefined && ` - 得分: ${milestone.score}`}
                          </Text>
                        </Space>
                      </Timeline.Item>
                    ))}
                  </Timeline>
                </Card>
              )}

              {/* 成长数据点 */}
              <Card title="成长数据详情" style={{ marginTop: 16 }}>
                <Table
                  columns={growthDataColumns}
                  dataSource={curve.dataPoints}
                  rowKey="date"
                  pagination={{ pageSize: 10 }}
                  size="small"
                />
              </Card>

              {/* 成长曲线图（与团队平均对比） */}
              {teamCurve && teamCurve.averageDataPoints.length > 0 && (
                <Card title="成长曲线对比（个人 vs 团队平均）" style={{ marginTop: 16 }}>
                  <div style={{ padding: '20px' }}>
                    {curve.dataPoints.map((point, index) => {
                      const teamPoint = teamCurve.averageDataPoints[index];
                      if (!teamPoint) return null;

                      const maxScore = Math.max(
                        ...curve.dataPoints.map((p) => p.judgementCardQualityScore),
                        ...teamCurve.averageDataPoints.map((p) => p.judgementCardQualityScore)
                      );

                      return (
                        <div
                          key={index}
                          style={{
                            marginBottom: '12px',
                            display: 'flex',
                            alignItems: 'center',
                          }}
                        >
                          <span style={{ width: '120px', fontSize: '12px' }}>{point.date}</span>
                          <div style={{ flex: 1, display: 'flex', gap: '8px' }}>
                            <div
                              style={{
                                flex: 1,
                                height: '24px',
                                backgroundColor: '#f0f0f0',
                                borderRadius: '4px',
                                overflow: 'hidden',
                                position: 'relative',
                              }}
                            >
                              <div
                                style={{
                                  height: '100%',
                                  width: `${(point.judgementCardQualityScore / maxScore) * 100}%`,
                                  backgroundColor: '#1890ff',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'flex-end',
                                  paddingRight: '4px',
                                  color: '#fff',
                                  fontSize: '11px',
                                }}
                              >
                                个人: {point.judgementCardQualityScore.toFixed(2)}
                              </div>
                            </div>
                            <div
                              style={{
                                flex: 1,
                                height: '24px',
                                backgroundColor: '#f0f0f0',
                                borderRadius: '4px',
                                overflow: 'hidden',
                                position: 'relative',
                              }}
                            >
                              <div
                                style={{
                                  height: '100%',
                                  width: `${(teamPoint.judgementCardQualityScore / maxScore) * 100}%`,
                                  backgroundColor: '#52c41a',
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'flex-end',
                                  paddingRight: '4px',
                                  color: '#fff',
                                  fontSize: '11px',
                                }}
                              >
                                团队: {teamPoint.judgementCardQualityScore.toFixed(2)}
                              </div>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </Card>
              )}
            </>
          )}

          {viewMode === 'team' && teamCurve && (
            <Card title="团队平均成长曲线">
              <Table
                columns={growthDataColumns}
                dataSource={teamCurve.averageDataPoints}
                rowKey="date"
                pagination={{ pageSize: 10 }}
                size="small"
              />
            </Card>
          )}
        </Spin>
      </Space>
    </div>
  );
};

export default NewcomerGrowthCurve;






