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
  Button,
} from 'antd';
import {
  TrophyOutlined,
  UserOutlined,
  TeamOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import { performanceService, PerformanceMetricsDto, PerformanceRankingDto } from '../../services/performanceService';
import { authService } from '../../services/authService';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { Option } = Select;

const TeamPerformance: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [teamMetrics, setTeamMetrics] = useState<PerformanceMetricsDto[]>([]);
  const [ranking, setRanking] = useState<PerformanceRankingDto[]>([]);
  const [periodType, setPeriodType] = useState<string>('monthly');
  const [periodStart, setPeriodStart] = useState<Dayjs>(dayjs().startOf('month'));
  const [departmentId, setDepartmentId] = useState<string | undefined>();

  useEffect(() => {
    loadTeamPerformance();
    loadRanking();
  }, [periodType, periodStart, departmentId]);

  const loadTeamPerformance = async () => {
    setLoading(true);
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const data = await performanceService.getTeamPerformance({
        departmentId,
        periodType,
        periodStart: periodStartStr,
      });
      setTeamMetrics(data);
    } catch (error) {
      message.error('加载团队绩效失败');
      console.error('Failed to load team performance:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadRanking = async () => {
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const data = await performanceService.getRanking({
        periodType,
        periodStart: periodStartStr,
        departmentId,
      });
      setRanking(data);
    } catch (error) {
      console.error('Failed to load ranking:', error);
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
    const parts = timeStr.split(':');
    const hours = parseInt(parts[0]);
    const minutes = parseInt(parts[1]);
    if (hours > 0) {
      return `${hours}小时${minutes}分钟`;
    }
    return `${minutes}分钟`;
  };

  // 计算团队平均值
  const calculateTeamAverage = () => {
    if (teamMetrics.length === 0) return null;

    const validScores = teamMetrics
      .map((m) => m.overallScore)
      .filter((s): s is number => s !== undefined && s !== null);

    if (validScores.length === 0) return null;

    const avg = validScores.reduce((sum, score) => sum + score, 0) / validScores.length;
    return avg;
  };

  const teamAverage = calculateTeamAverage();

  const rankingColumns = [
    {
      title: '排名',
      dataIndex: 'rank',
      key: 'rank',
      width: 80,
      render: (rank: number) => {
        if (rank === 1) return <Tag color="gold">🥇 {rank}</Tag>;
        if (rank === 2) return <Tag color="default">🥈 {rank}</Tag>;
        if (rank === 3) return <Tag color="orange">🥉 {rank}</Tag>;
        return <Tag>{rank}</Tag>;
      },
    },
    {
      title: '工程师',
      dataIndex: 'engineerName',
      key: 'engineerName',
    },
    {
      title: '综合评分',
      dataIndex: 'overallScore',
      key: 'overallScore',
      render: (score: number | undefined) => (
        <Text strong>{score?.toFixed(1) || '-'}</Text>
      ),
      sorter: (a: PerformanceRankingDto, b: PerformanceRankingDto) =>
        (a.overallScore || 0) - (b.overallScore || 0),
    },
    {
      title: '绩效等级',
      dataIndex: 'performanceLevel',
      key: 'performanceLevel',
      render: (level: string | undefined) => (
        <Tag color={getPerformanceLevelColor(level)}>
          {getPerformanceLevelText(level)}
        </Tag>
      ),
    },
    {
      title: '总工单数',
      dataIndex: 'totalTickets',
      key: 'totalTickets',
      sorter: (a: PerformanceRankingDto, b: PerformanceRankingDto) =>
        a.totalTickets - b.totalTickets,
    },
    {
      title: '已解决',
      dataIndex: 'ticketsResolved',
      key: 'ticketsResolved',
      sorter: (a: PerformanceRankingDto, b: PerformanceRankingDto) =>
        a.ticketsResolved - b.ticketsResolved,
    },
    {
      title: '一次解决率',
      dataIndex: 'firstTimeResolutionRate',
      key: 'firstTimeResolutionRate',
      render: (rate: number | undefined) => (
        <Progress percent={rate || 0} size="small" />
      ),
      sorter: (a: PerformanceRankingDto, b: PerformanceRankingDto) =>
        (a.firstTimeResolutionRate || 0) - (b.firstTimeResolutionRate || 0),
    },
    {
      title: '平均解决时间',
      dataIndex: 'averageResolutionTime',
      key: 'averageResolutionTime',
      render: (time: string | undefined) => formatTimeSpan(time),
    },
  ];

  const teamMetricsColumns = [
    {
      title: '工程师',
      dataIndex: 'engineerName',
      key: 'engineerName',
    },
    {
      title: '综合评分',
      dataIndex: 'overallScore',
      key: 'overallScore',
      render: (score: number | undefined) => (
        <Text strong style={{ color: score && score >= 90 ? '#3f8600' : '#cf1322' }}>
          {score?.toFixed(1) || '-'}
        </Text>
      ),
      sorter: (a: PerformanceMetricsDto, b: PerformanceMetricsDto) =>
        (a.overallScore || 0) - (b.overallScore || 0),
    },
    {
      title: '绩效等级',
      dataIndex: 'performanceLevel',
      key: 'performanceLevel',
      render: (level: string | undefined) => (
        <Tag color={getPerformanceLevelColor(level)}>
          {getPerformanceLevelText(level)}
        </Tag>
      ),
    },
    {
      title: '总工单数',
      dataIndex: 'totalTickets',
      key: 'totalTickets',
      sorter: (a: PerformanceMetricsDto, b: PerformanceMetricsDto) =>
        a.totalTickets - b.totalTickets,
    },
    {
      title: '已解决',
      dataIndex: 'ticketsResolved',
      key: 'ticketsResolved',
    },
    {
      title: '一次解决率',
      dataIndex: 'firstTimeResolutionRate',
      key: 'firstTimeResolutionRate',
      render: (rate: number | undefined) => (
        <Progress percent={rate || 0} size="small" />
      ),
    },
    {
      title: '平均解决时间',
      dataIndex: 'averageResolutionTime',
      key: 'averageResolutionTime',
      render: (time: string | undefined) => formatTimeSpan(time),
    },
    {
      title: '响应及时率',
      dataIndex: 'onTimeResponseRate',
      key: 'onTimeResponseRate',
      render: (rate: number | undefined) => (
        <Progress percent={rate || 0} size="small" />
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2}>团队绩效</Title>
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
          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              loadTeamPerformance();
              loadRanking();
            }}
          >
            刷新
          </Button>
        </Space>
      </div>

      <Spin spinning={loading}>
        {/* 团队概览 */}
        <Row gutter={16} style={{ marginBottom: '24px' }}>
          <Col span={6}>
            <Card>
              <Statistic
                title="团队成员数"
                value={teamMetrics.length}
                prefix={<TeamOutlined />}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="团队平均分"
                value={teamAverage?.toFixed(1) || 0}
                suffix="/ 100"
                prefix={<TrophyOutlined />}
                valueStyle={{ color: teamAverage && teamAverage >= 90 ? '#3f8600' : '#cf1322' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="总工单数"
                value={teamMetrics.reduce((sum, m) => sum + m.totalTickets, 0)}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="已解决工单"
                value={teamMetrics.reduce((sum, m) => sum + m.ticketsResolved, 0)}
              />
            </Card>
          </Col>
        </Row>

        {/* 绩效排名 */}
        <Card title="绩效排名" style={{ marginBottom: '24px' }}>
          <Table
            columns={rankingColumns}
            dataSource={ranking}
            rowKey="engineerId"
            pagination={false}
          />
        </Card>

        {/* 团队绩效详情 */}
        <Card title="团队绩效详情">
          <Table
            columns={teamMetricsColumns}
            dataSource={teamMetrics}
            rowKey="metricId"
            pagination={false}
          />
        </Card>
      </Spin>
    </div>
  );
};

export default TeamPerformance;


