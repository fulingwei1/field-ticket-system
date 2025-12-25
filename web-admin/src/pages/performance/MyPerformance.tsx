import React, { useEffect, useState } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  Select,
  DatePicker,
  Spin,
  message,
  Tag,
  Table,
  Progress,
  Space,
  Typography,
} from 'antd';
import {
  TrophyOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { performanceService, PerformanceMetricsDto, PerformanceTrendDto } from '../../services/performanceService';
import { authService } from '../../services/authService';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { Option } = Select;

const MyPerformance: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [metrics, setMetrics] = useState<PerformanceMetricsDto | null>(null);
  const [trends, setTrends] = useState<PerformanceTrendDto[]>([]);
  const [periodType, setPeriodType] = useState<string>('monthly');
  const [periodStart, setPeriodStart] = useState<Dayjs>(dayjs().startOf('month'));

  const user = authService.getUser();

  useEffect(() => {
    if (user?.id) {
      loadPerformance();
      loadTrends();
    }
  }, [user, periodType, periodStart]);

  const loadPerformance = async () => {
    if (!user?.id) return;

    setLoading(true);
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const data = await performanceService.getEngineerPerformance(
        user.id,
        periodType,
        periodStartStr
      );
      setMetrics(data);
    } catch (error: any) {
      if (error.message === 'Performance metrics not found') {
        message.warning('该周期暂无绩效数据，请先计算绩效');
      } else {
        message.error('加载绩效数据失败');
        console.error('Failed to load performance:', error);
      }
    } finally {
      setLoading(false);
    }
  };

  const loadTrends = async () => {
    if (!user?.id) return;

    try {
      const fromDate = periodStart
        .subtract(5, periodType === 'monthly' ? 'month' : 'week')
        .format('YYYY-MM-DD');
      const toDate = periodStart.format('YYYY-MM-DD');
      const data = await performanceService.getTrends({
        engineerId: user.id,
        periodType,
        fromDate,
        toDate,
      });
      setTrends(data);
    } catch (error) {
      console.error('Failed to load trends:', error);
    }
  };

  const getPerformanceLevelColor = (level?: string) => {
    switch (level) {
      case 'excellent':
        return 'success';
      case 'good':
        return 'processing';
      case 'average':
        return 'warning';
      case 'below_average':
        return 'error';
      case 'poor':
        return 'error';
      default:
        return 'default';
    }
  };

  const getPerformanceLevelText = (level?: string) => {
    switch (level) {
      case 'excellent':
        return '优秀';
      case 'good':
        return '良好';
      case 'average':
        return '合格';
      case 'below_average':
        return '待改进';
      case 'poor':
        return '不合格';
      default:
        return '未知';
    }
  };

  const formatTimeSpan = (timeStr?: string) => {
    if (!timeStr) return '-';
    // 解析 TimeSpan 格式 (例如: "02:30:00" 表示 2小时30分钟)
    const parts = timeStr.split(':');
    const hours = parseInt(parts[0]);
    const minutes = parseInt(parts[1]);
    if (hours > 0) {
      return `${hours}小时${minutes}分钟`;
    }
    return `${minutes}分钟`;
  };

  const metricColumns = [
    {
      title: '指标名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '数值',
      dataIndex: 'value',
      key: 'value',
      render: (value: any, record: any) => {
        if (record.type === 'percentage') {
          return <Progress percent={value} size="small" />;
        }
        if (record.type === 'time') {
          return formatTimeSpan(value);
        }
        return value;
      },
    },
    {
      title: '目标值',
      dataIndex: 'target',
      key: 'target',
    },
  ];

  const metricData = metrics
    ? [
        {
          key: '1',
          name: '工单创建完整度',
          value: metrics.ticketCreationCompleteness || 0,
          type: 'percentage',
          target: '≥ 95%',
        },
        {
          key: '2',
          name: '现场问题反馈及时率',
          value: metrics.fieldFeedbackTimelinessRate || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '3',
          name: '工单响应及时率',
          value: metrics.onTimeResponseRate || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '4',
          name: '一次解决率',
          value: metrics.firstTimeResolutionRate || 0,
          type: 'percentage',
          target: '≥ 80%',
        },
        {
          key: '5',
          name: '平均解决时间',
          value: metrics.averageResolutionTime,
          type: 'time',
          target: '≤ 4小时',
        },
        {
          key: '6',
          name: '验证通过率',
          value: metrics.verificationPassRate || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '7',
          name: '工作活动完整度',
          value: metrics.workActivityCompleteness || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '8',
          name: '客户满意度',
          value: metrics.customerSatisfactionScore ? (metrics.customerSatisfactionScore * 20).toFixed(1) : 0,
          type: 'score',
          target: '≥ 4.0分',
        },
      ]
    : [];

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2}>我的绩效</Title>
        <Space>
          <Select
            value={periodType}
            onChange={setPeriodType}
            style={{ width: 120 }}
          >
            <Option value="daily">日度</Option>
            <Option value="weekly">周度</Option>
            <Option value="monthly">月度</Option>
            <Option value="quarterly">季度</Option>
            <Option value="yearly">年度</Option>
          </Select>
          <DatePicker
            picker={periodType === 'monthly' ? 'month' : periodType === 'yearly' ? 'year' : 'date'}
            value={periodStart}
            onChange={(date) => date && setPeriodStart(date)}
            format={periodType === 'monthly' ? 'YYYY-MM' : periodType === 'yearly' ? 'YYYY' : 'YYYY-MM-DD'}
          />
        </Space>
      </div>

      <Spin spinning={loading}>
        {metrics ? (
          <>
            {/* 综合评分卡片 */}
            <Row gutter={16} style={{ marginBottom: '24px' }}>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="综合评分"
                    value={metrics.overallScore?.toFixed(1) || 0}
                    suffix="/ 100"
                    prefix={<TrophyOutlined />}
                    valueStyle={{ color: metrics.overallScore && metrics.overallScore >= 90 ? '#3f8600' : '#cf1322' }}
                  />
                  <div style={{ marginTop: '8px' }}>
                    <Tag color={getPerformanceLevelColor(metrics.performanceLevel)}>
                      {getPerformanceLevelText(metrics.performanceLevel)}
                    </Tag>
                  </div>
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="团队排名"
                    value={metrics.rankInTeam || '-'}
                    suffix={metrics.rankInTeam ? '名' : ''}
                    prefix={<UserOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="总工单数"
                    value={metrics.totalTickets}
                    prefix={<CheckCircleOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="已解决工单"
                    value={metrics.ticketsResolved}
                    suffix={`/ ${metrics.totalTickets}`}
                    prefix={<ClockCircleOutlined />}
                  />
                </Card>
              </Col>
            </Row>

            {/* 关键指标 */}
            <Card title="关键指标" style={{ marginBottom: '24px' }}>
              <Table
                columns={metricColumns}
                dataSource={metricData}
                pagination={false}
                size="small"
              />
            </Card>

            {/* 详细指标 */}
            <Row gutter={16} style={{ marginBottom: '24px' }}>
              <Col span={12}>
                <Card title="工单处理效率">
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <div>
                      <Text>工单创建完整度：</Text>
                      <Progress
                        percent={metrics.ticketCreationCompleteness || 0}
                        status={metrics.ticketCreationCompleteness && metrics.ticketCreationCompleteness >= 95 ? 'success' : 'active'}
                      />
                    </div>
                    <div>
                      <Text>现场问题反馈及时率：</Text>
                      <Progress
                        percent={metrics.fieldFeedbackTimelinessRate || 0}
                        status={metrics.fieldFeedbackTimelinessRate && metrics.fieldFeedbackTimelinessRate >= 90 ? 'success' : 'active'}
                      />
                    </div>
                    <div>
                      <Text>工单响应及时率：</Text>
                      <Progress
                        percent={metrics.onTimeResponseRate || 0}
                        status={metrics.onTimeResponseRate && metrics.onTimeResponseRate >= 90 ? 'success' : 'active'}
                      />
                    </div>
                  </Space>
                </Card>
              </Col>
              <Col span={12}>
                <Card title="问题解决能力">
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <div>
                      <Text>一次解决率：</Text>
                      <Progress
                        percent={metrics.firstTimeResolutionRate || 0}
                        status={metrics.firstTimeResolutionRate && metrics.firstTimeResolutionRate >= 80 ? 'success' : 'active'}
                      />
                    </div>
                    <div>
                      <Text>平均解决时间：</Text>
                      <Text strong>{formatTimeSpan(metrics.averageResolutionTime)}</Text>
                    </div>
                    <div>
                      <Text>验证通过率：</Text>
                      <Progress
                        percent={metrics.verificationPassRate || 0}
                        status={metrics.verificationPassRate && metrics.verificationPassRate >= 90 ? 'success' : 'active'}
                      />
                    </div>
                  </Space>
                </Card>
              </Col>
            </Row>

            {/* 其他维度 */}
            <Row gutter={16}>
              <Col span={8}>
                <Card title="知识贡献" size="small">
                  <Statistic
                    title="判断卡创建"
                    value={metrics.judgementCardsCreated}
                  />
                  <Statistic
                    title="解决方案贡献"
                    value={metrics.solutionsContributed}
                    style={{ marginTop: '16px' }}
                  />
                </Card>
              </Col>
              <Col span={8}>
                <Card title="客户服务" size="small">
                  <Statistic
                    title="客户满意度"
                    value={metrics.customerSatisfactionScore?.toFixed(1) || '-'}
                    suffix="/ 5.0"
                  />
                  <Statistic
                    title="客户反馈数"
                    value={metrics.customerFeedbackCount}
                    style={{ marginTop: '16px' }}
                  />
                </Card>
              </Col>
              <Col span={8}>
                <Card title="工作规范性" size="small">
                  <Statistic
                    title="工单信息完整性"
                    value={metrics.ticketInformationCompleteness?.toFixed(1) || 0}
                    suffix="%"
                  />
                  <Statistic
                    title="工作活动完整度"
                    value={metrics.workActivityCompleteness?.toFixed(1) || 0}
                    suffix="%"
                    style={{ marginTop: '16px' }}
                  />
                </Card>
              </Col>
            </Row>
          </>
        ) : (
          <Card>
            <div style={{ textAlign: 'center', padding: '40px' }}>
              <Text type="secondary">暂无绩效数据，请联系管理员计算绩效</Text>
            </div>
          </Card>
        )}
      </Spin>
    </div>
  );
};

export default MyPerformance;

