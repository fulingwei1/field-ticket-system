import React, { useState, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  Table,
  Tag,
  Space,
  DatePicker,
  Select,
  Button,
  message,
  Progress,
  Spin,
  Empty,
} from 'antd';
import {
  FireOutlined,
  WarningOutlined,
  CheckCircleOutlined,
  ReloadOutlined,
  TrendingUpOutlined,
} from '@ant-design/icons';
import { Column } from '@ant-design/charts';
import dayjs from 'dayjs';
import fieldProblemService, {
  ProblemStatisticsResponse,
  ProblemHotspot,
} from '../../services/fieldProblemService';

const { RangePicker } = DatePicker;
const { Option } = Select;

export default function ProblemHotspots() {
  const [loading, setLoading] = useState(false);
  const [statistics, setStatistics] = useState<ProblemStatisticsResponse | null>(null);
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().subtract(30, 'days'),
    dayjs(),
  ]);
  const [topN, setTopN] = useState(10);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const response = await fieldProblemService.getStatistics({
        startDate: dateRange[0].format('YYYY-MM-DD'),
        endDate: dateRange[1].format('YYYY-MM-DD'),
        topN,
      });

      setStatistics(response);
      message.success('数据加载成功');
    } catch (error: any) {
      message.error('加载数据失败: ' + (error.message || '未知错误'));
    } finally {
      setLoading(false);
    }
  };

  const hotspotColumns = [
    {
      title: '排名',
      key: 'rank',
      width: 60,
      render: (_: any, __: any, index: number) => (
        <Tag color={index < 3 ? 'red' : 'default'}>#{index + 1}</Tag>
      ),
    },
    {
      title: '问题分类',
      dataIndex: 'problemCategory',
      key: 'problemCategory',
      render: (category: string) => (
        <Tag color="blue" icon={<FireOutlined />}>
          {category}
        </Tag>
      ),
    },
    {
      title: '问题数量',
      dataIndex: 'count',
      key: 'count',
      sorter: (a: ProblemHotspot, b: ProblemHotspot) => b.count - a.count,
      render: (count: number) => <strong>{count}</strong>,
    },
    {
      title: '占比',
      dataIndex: 'percentage',
      key: 'percentage',
      render: (percentage: number) => (
        <Progress
          percent={Math.round(percentage * 100)}
          size="small"
          status="active"
        />
      ),
    },
    {
      title: '重复问题',
      dataIndex: 'repeatCount',
      key: 'repeatCount',
      render: (count: number, record: ProblemHotspot) => {
        const repeatRate = record.count > 0 ? (count / record.count) * 100 : 0;
        return (
          <Space>
            <Tag color={repeatRate > 50 ? 'red' : repeatRate > 30 ? 'orange' : 'green'}>
              {count} ({repeatRate.toFixed(0)}%)
            </Tag>
          </Space>
        );
      },
    },
    {
      title: '平均处理周期',
      dataIndex: 'averageProcessingDays',
      key: 'averageProcessingDays',
      sorter: (a: ProblemHotspot, b: ProblemHotspot) =>
        a.averageProcessingDays - b.averageProcessingDays,
      render: (days: number) => {
        const color = days <= 3 ? 'green' : days <= 7 ? 'orange' : 'red';
        return <Tag color={color}>{days.toFixed(1)} 天</Tag>;
      },
    },
    {
      title: '典型症状',
      dataIndex: 'topSymptoms',
      key: 'topSymptoms',
      render: (symptoms: string[]) => (
        <div style={{ maxWidth: 300 }}>
          {symptoms.slice(0, 2).map((symptom, index) => (
            <div
              key={index}
              style={{
                fontSize: 12,
                color: '#666',
                marginBottom: 4,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              • {symptom}
            </div>
          ))}
        </div>
      ),
    },
  ];

  const trendChartConfig = {
    data: statistics?.trends || [],
    xField: 'date',
    yField: 'totalCount',
    seriesField: 'type',
    isGroup: true,
    columnStyle: {
      radius: [4, 4, 0, 0],
    },
    label: {
      position: 'top' as const,
      style: {
        fill: '#000000',
        opacity: 0.6,
      },
    },
    xAxis: {
      label: {
        autoRotate: false,
        autoHide: true,
      },
    },
  };

  if (loading && !statistics) {
    return (
      <div style={{ textAlign: 'center', padding: '100px 0' }}>
        <Spin size="large" tip="加载数据中..." />
      </div>
    );
  }

  return (
    <div>
      <Card
        title={
          <Space>
            <FireOutlined />
            问题热点分析
          </Space>
        }
        extra={
          <Space>
            <RangePicker
              value={dateRange}
              onChange={(dates) => {
                if (dates) {
                  setDateRange([dates[0]!, dates[1]!]);
                }
              }}
            />
            <Select
              value={topN}
              onChange={setTopN}
              style={{ width: 120 }}
            >
              <Option value={5}>Top 5</Option>
              <Option value={10}>Top 10</Option>
              <Option value={20}>Top 20</Option>
            </Select>
            <Button
              type="primary"
              icon={<ReloadOutlined />}
              onClick={loadData}
              loading={loading}
            >
              刷新
            </Button>
          </Space>
        }
      >
        {statistics ? (
          <>
            {/* 统计卡片 */}
            <Row gutter={16} style={{ marginBottom: 24 }}>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="总问题数"
                    value={statistics.totalCount}
                    prefix={<WarningOutlined />}
                    valueStyle={{ color: '#1890ff' }}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="已解决"
                    value={statistics.resolvedCount}
                    prefix={<CheckCircleOutlined />}
                    valueStyle={{ color: '#52c41a' }}
                    suffix={`/ ${statistics.totalCount}`}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="重复问题数"
                    value={statistics.repeatProblemCount}
                    prefix={<TrendingUpOutlined />}
                    valueStyle={{
                      color: statistics.repeatRate > 0.3 ? '#ff4d4f' : '#faad14',
                    }}
                  />
                  <div style={{ marginTop: 8, fontSize: 12, color: '#999' }}>
                    重复率: {(statistics.repeatRate * 100).toFixed(1)}%
                  </div>
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="平均处理周期"
                    value={statistics.averageProcessingDays.toFixed(1)}
                    suffix="天"
                    valueStyle={{
                      color:
                        statistics.averageProcessingDays <= 3
                          ? '#52c41a'
                          : statistics.averageProcessingDays <= 7
                          ? '#faad14'
                          : '#ff4d4f',
                    }}
                  />
                </Card>
              </Col>
            </Row>

            {/* 热点问题表格 */}
            <Card
              title={
                <Space>
                  <FireOutlined />
                  高频问题 Top {topN}
                </Space>
              }
              style={{ marginBottom: 24 }}
            >
              <Table
                dataSource={statistics.hotspots}
                columns={hotspotColumns}
                rowKey="problemCategory"
                pagination={false}
                loading={loading}
              />
            </Card>

            {/* 趋势图表 */}
            {statistics.trends.length > 0 && (
              <Card
                title={
                  <Space>
                    <TrendingUpOutlined />
                    问题趋势
                  </Space>
                }
              >
                <Column
                  {...trendChartConfig}
                  height={300}
                />
              </Card>
            )}
          </>
        ) : (
          <Empty description="暂无数据" />
        )}
      </Card>
    </div>
  );
}
