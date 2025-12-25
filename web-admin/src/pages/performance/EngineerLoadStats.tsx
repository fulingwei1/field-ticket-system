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
  Alert,
} from 'antd';
import {
  UserOutlined,
  TeamOutlined,
  ReloadOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
} from '@ant-design/icons';
import {
  engineerLoadStatService,
  EngineerLoadReportDto,
  TeamLoadDistributionDto,
  LoadTrendDto,
} from '../../services/engineerLoadStatService';
import { authService } from '../../services/authService';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Option } = Select;

const EngineerLoadStats: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState<EngineerLoadReportDto | null>(null);
  const [teamDistribution, setTeamDistribution] = useState<TeamLoadDistributionDto | null>(null);
  const [trends, setTrends] = useState<LoadTrendDto[]>([]);
  const [viewMode, setViewMode] = useState<'personal' | 'team'>('personal');
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs]>([
    dayjs().subtract(30, 'day'),
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
        const [reportData, trendData] = await Promise.all([
          engineerLoadStatService.getPersonalLoadReport(startDate, endDate),
          engineerLoadStatService.getLoadTrend(undefined, 'daily', 30),
        ]);
        setReport(reportData);
        setTrends(trendData);
      } else {
        const distributionData = await engineerLoadStatService.getTeamLoadDistribution(
          undefined,
          startDate,
          endDate
        );
        setTeamDistribution(distributionData);
      }
    } catch (error: any) {
      message.error(error.message || '加载数据失败');
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  };

  const getLoadLevelColor = (level: string) => {
    switch (level) {
      case 'low':
        return 'success';
      case 'normal':
        return 'processing';
      case 'high':
        return 'warning';
      case 'very_high':
        return 'error';
      default:
        return 'default';
    }
  };

  const getLoadLevelText = (level: string) => {
    switch (level) {
      case 'low':
        return '低';
      case 'normal':
        return '正常';
      case 'high':
        return '高';
      case 'very_high':
        return '非常高';
      default:
        return level;
    }
  };

  const dailyStatsColumns = [
    {
      title: '日期',
      dataIndex: 'statDate',
      key: 'statDate',
      width: 120,
    },
    {
      title: '被@次数',
      dataIndex: 'mentionedCount',
      key: 'mentionedCount',
      width: 100,
    },
    {
      title: '升级接手',
      dataIndex: 'escalationTakenCount',
      key: 'escalationTakenCount',
      width: 100,
    },
    {
      title: '判断复用',
      dataIndex: 'judgementReusedCount',
      key: 'judgementReusedCount',
      width: 100,
    },
    {
      title: '低置信度接手',
      dataIndex: 'lowConfidenceTakenCount',
      key: 'lowConfidenceTakenCount',
      width: 120,
    },
    {
      title: '分配工单',
      dataIndex: 'ticketsAssigned',
      key: 'ticketsAssigned',
      width: 100,
    },
    {
      title: '关闭工单',
      dataIndex: 'ticketsClosed',
      key: 'ticketsClosed',
      width: 100,
    },
    {
      title: '负载分数',
      dataIndex: 'totalLoadScore',
      key: 'totalLoadScore',
      width: 100,
      render: (score: number) => <Text strong>{score.toFixed(2)}</Text>,
    },
  ];

  const teamStatsColumns = [
    {
      title: '工程师',
      dataIndex: 'engineerName',
      key: 'engineerName',
      width: 150,
    },
    {
      title: '被@次数',
      dataIndex: 'mentionedCount',
      key: 'mentionedCount',
      width: 100,
    },
    {
      title: '升级接手',
      dataIndex: 'escalationTakenCount',
      key: 'escalationTakenCount',
      width: 100,
    },
    {
      title: '判断复用',
      dataIndex: 'judgementReusedCount',
      key: 'judgementReusedCount',
      width: 100,
    },
    {
      title: '低置信度接手',
      dataIndex: 'lowConfidenceTakenCount',
      key: 'lowConfidenceTakenCount',
      width: 120,
    },
    {
      title: '分配工单',
      dataIndex: 'ticketsAssigned',
      key: 'ticketsAssigned',
      width: 100,
    },
    {
      title: '关闭工单',
      dataIndex: 'ticketsClosed',
      key: 'ticketsClosed',
      width: 100,
    },
    {
      title: '负载分数',
      dataIndex: 'totalLoadScore',
      key: 'totalLoadScore',
      width: 100,
      render: (score: number) => <Text strong>{score.toFixed(2)}</Text>,
      sorter: (a: any, b: any) => a.totalLoadScore - b.totalLoadScore,
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
                {viewMode === 'personal' ? '个人负载统计' : '团队负载分布'}
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
          {viewMode === 'personal' && report && (
            <>
              {/* 负载摘要 */}
              <Row gutter={16}>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="被@次数"
                      value={report.totalStats.mentionedCount}
                      prefix={<UserOutlined />}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="升级接手次数"
                      value={report.totalStats.escalationTakenCount}
                      prefix={<ArrowUpOutlined />}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="判断复用次数"
                      value={report.totalStats.judgementReusedCount}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="综合负载分数"
                      value={report.totalStats.totalLoadScore}
                      precision={2}
                      prefix={<ArrowDownOutlined />}
                    />
                  </Card>
                </Col>
              </Row>

              {/* 负载分析 */}
              {report.analysis && (
                <Card title="负载分析" style={{ marginTop: 16 }}>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <Alert
                      message={`负载水平：${getLoadLevelText(report.analysis.loadLevel)}`}
                      description={report.analysis.assessment}
                      type={
                        report.analysis.loadLevel === 'very_high' || report.analysis.loadLevel === 'high'
                          ? 'warning'
                          : 'info'
                      }
                      showIcon
                    />
                    {report.analysis.suggestions.length > 0 && (
                      <div>
                        <Text strong>建议：</Text>
                        <ul style={{ marginTop: 8, marginBottom: 0 }}>
                          {report.analysis.suggestions.map((suggestion, index) => (
                            <li key={index}>{suggestion}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </Space>
                </Card>
              )}

              {/* 每日负载统计 */}
              <Card title="每日负载统计" style={{ marginTop: 16 }}>
                <Table
                  columns={dailyStatsColumns}
                  dataSource={report.dailyStats}
                  rowKey="statDate"
                  pagination={{ pageSize: 10 }}
                  size="small"
                />
              </Card>

              {/* 负载趋势 */}
              {trends.length > 0 && (
                <Card title="负载趋势" style={{ marginTop: 16 }}>
                  <div style={{ padding: '20px' }}>
                    {trends.map((trend, index) => (
                      <div
                        key={index}
                        style={{
                          marginBottom: '8px',
                          display: 'flex',
                          alignItems: 'center',
                        }}
                      >
                        <span style={{ width: '120px', fontSize: '12px' }}>{trend.period}</span>
                        <div
                          style={{
                            flex: 1,
                            height: '20px',
                            backgroundColor: '#f0f0f0',
                            borderRadius: '4px',
                            overflow: 'hidden',
                            marginLeft: '12px',
                          }}
                        >
                          <div
                            style={{
                              height: '100%',
                              width: `${
                                (trend.totalLoadScore /
                                  Math.max(...trends.map((d) => d.totalLoadScore))) *
                                100
                              }%`,
                              backgroundColor: '#1890ff',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'flex-end',
                              paddingRight: '4px',
                              color: '#fff',
                              fontSize: '11px',
                            }}
                          >
                            {trend.totalLoadScore.toFixed(2)}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </Card>
              )}
            </>
          )}

          {viewMode === 'team' && teamDistribution && (
            <>
              {/* 团队负载摘要 */}
              <Row gutter={16}>
                <Col xs={24} sm={12} md={8}>
                  <Card>
                    <Statistic
                      title="平均负载分数"
                      value={teamDistribution.distributionAnalysis.averageLoadScore}
                      precision={2}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Card>
                    <Statistic
                      title="负载均衡分数"
                      value={teamDistribution.distributionAnalysis.loadBalanceScore}
                      precision={2}
                      suffix="%"
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={8}>
                  <Card>
                    <Statistic
                      title="工程师数量"
                      value={teamDistribution.engineerStats.length}
                    />
                  </Card>
                </Col>
              </Row>

              {/* 负载分布分析 */}
              {teamDistribution.distributionAnalysis.recommendations.length > 0 && (
                <Card title="负载分布分析" style={{ marginTop: 16 }}>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    {teamDistribution.distributionAnalysis.recommendations.map(
                      (recommendation, index) => (
                        <Alert
                          key={index}
                          message={recommendation}
                          type="info"
                          showIcon
                        />
                      )
                    )}
                  </Space>
                </Card>
              )}

              {/* 团队负载统计表 */}
              <Card title="团队负载统计" style={{ marginTop: 16 }}>
                <Table
                  columns={teamStatsColumns}
                  dataSource={teamDistribution.engineerStats}
                  rowKey="engineerId"
                  pagination={{ pageSize: 20 }}
                  size="small"
                />
              </Card>
            </>
          )}
        </Spin>
      </Space>
    </div>
  );
};

export default EngineerLoadStats;






