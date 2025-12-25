"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  DatePicker,
  Space,
  Button,
  Spin,
  message,
  Empty,
} from 'antd';
import {
  ReloadOutlined,
  FileTextOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  PieChartOutlined,
  DownloadOutlined,
} from '@ant-design/icons';
import {
  statisticsService,
  StatisticsOverviewDto,
  DomainStatisticsDto,
  ClosureTimeDistributionDto,
  StatusStatisticsDto,
  TrendDataPointDto,
} from '../../services/statisticsService';
import { statisticsExportService } from '../../services/statisticsExportService';
// 使用简单的图表实现，如果项目中没有 @ant-design/plots，可以使用其他图表库
// import { Column, Pie } from '@ant-design/plots';
import dayjs, { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;

export default function StatisticsDashboard() {
  const [loading, setLoading] = useState(false);
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [overview, setOverview] = useState<StatisticsOverviewDto | null>(null);
  const [topDomains, setTopDomains] = useState<DomainStatisticsDto[]>([]);
  const [closureTimeDistribution, setClosureTimeDistribution] = useState<ClosureTimeDistributionDto | null>(null);
  const [statusStatistics, setStatusStatistics] = useState<StatusStatisticsDto[]>([]);
  const [trendData, setTrendData] = useState<TrendDataPointDto[]>([]);

  useEffect(() => {
    loadStatistics();
  }, [dateRange]);

  const loadStatistics = async () => {
    try {
      setLoading(true);
      const fromDate = dateRange?.[0]?.format('YYYY-MM-DD');
      const toDate = dateRange?.[1]?.format('YYYY-MM-DD');

      const [overviewData, domainsData, closureData, statusData, trendDataResult] = await Promise.all([
        statisticsService.getOverview(fromDate, toDate),
        statisticsService.getTopDomains(5, fromDate, toDate),
        statisticsService.getClosureTimeDistribution(fromDate, toDate),
        statisticsService.getStatusStatistics(fromDate, toDate),
        statisticsService.getTrendData(
          'tickets',
          fromDate || dayjs().subtract(30, 'day').format('YYYY-MM-DD'),
          toDate || dayjs().format('YYYY-MM-DD'),
          'day'
        ),
      ]);

      setOverview(overviewData);
      setTopDomains(domainsData);
      setClosureTimeDistribution(closureData);
      setStatusStatistics(statusData);
      setTrendData(trendDataResult);
    } catch (error: any) {
      console.error('Failed to load statistics:', error);
      message.error(error.message || '加载统计数据失败');
    } finally {
      setLoading(false);
    }
  };

  const handleExportFullReport = async () => {
    try {
      const fromDate = dateRange?.[0]?.format('YYYY-MM-DD');
      const toDate = dateRange?.[1]?.format('YYYY-MM-DD');
      await statisticsExportService.exportFullReport(fromDate, toDate);
      message.success('导出成功');
    } catch (error: any) {
      message.error(error.message || '导出失败');
    }
  };

  const formatDuration = (duration?: string): string => {
    if (!duration) return '-';
    // Parse ISO 8601 duration (e.g., "PT2H30M" = 2 hours 30 minutes)
    const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);
    if (!match) return duration;

    const hours = parseInt(match[1] || '0', 10);
    const minutes = parseInt(match[2] || '0', 10);
    const days = Math.floor(hours / 24);
    const remainingHours = hours % 24;

    if (days > 0) {
      return `${days}天${remainingHours}小时`;
    } else if (hours > 0) {
      return `${hours}小时${minutes}分钟`;
    } else {
      return `${minutes}分钟`;
    }
  };

  // Top问题域图表配置
  const domainChartConfig = {
    data: topDomains.map((d) => ({
      type: d.domainName,
      value: d.ticketCount,
      percentage: d.percentage,
    })),
    angleField: 'value',
    colorField: 'type',
    radius: 0.8,
    label: {
      type: 'outer',
      content: '{name}\n{value} ({percentage}%)',
    },
    interactions: [{ type: 'element-active' }],
  };

  // 闭环时间分布图表配置
  const closureTimeChartConfig = {
    data: closureTimeDistribution?.ranges || [],
    xField: 'range',
    yField: 'count',
    columnStyle: {
      fill: '#1890ff',
    },
    label: {
      position: 'top' as const,
      formatter: (datum: any) => `${datum.count} (${datum.percentage}%)`,
    },
  };

  // 工单状态统计图表配置
  const statusChartConfig = {
    data: statusStatistics.map((s) => ({
      type: s.statusLabel,
      value: s.count,
      percentage: s.percentage,
    })),
    angleField: 'value',
    colorField: 'type',
    radius: 0.8,
    label: {
      type: 'outer',
      content: '{name}\n{value} ({percentage}%)',
    },
    interactions: [{ type: 'element-active' }],
  };

  // 趋势数据图表配置
  const trendChartConfig = {
    data: trendData,
    xField: 'date',
    yField: 'value',
    point: {
      size: 5,
      shape: 'circle',
    },
    label: {
      style: {
        fill: '#aaa',
      },
    },
  };

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="工单统计分析"
        extra={
          <Space>
            <RangePicker
              value={dateRange}
              onChange={(dates) => setDateRange(dates as [Dayjs, Dayjs] | null)}
              format="YYYY-MM-DD"
            />
            <Button icon={<ReloadOutlined />} onClick={loadStatistics}>
              刷新
            </Button>
            <Button
              icon={<DownloadOutlined />}
              onClick={handleExportFullReport}
            >
              导出完整报告
            </Button>
          </Space>
        }
      >
        {loading ? (
          <div style={{ textAlign: 'center', padding: '50px' }}>
            <Spin size="large" />
          </div>
        ) : (
          <>
            {/* 统计概览 */}
            {overview && (
              <Row gutter={[16, 16]} style={{ marginBottom: '24px' }}>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="工单总数"
                      value={overview.totalTickets}
                      prefix={<FileTextOutlined />}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="开放工单"
                      value={overview.openTickets}
                      prefix={<ClockCircleOutlined />}
                      valueStyle={{ color: '#1890ff' }}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="已关闭工单"
                      value={overview.closedTickets}
                      prefix={<CheckCircleOutlined />}
                      valueStyle={{ color: '#52c41a' }}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="平均闭环时长"
                      value={formatDuration(overview.averageClosureTime)}
                      prefix={<PieChartOutlined />}
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="闭环率"
                      value={overview.closureRate || 0}
                      precision={2}
                      suffix="%"
                    />
                  </Card>
                </Col>
                <Col xs={24} sm={12} md={6}>
                  <Card>
                    <Statistic
                      title="重开工单率"
                      value={overview.reopenRate || 0}
                      precision={2}
                      suffix="%"
                    />
                  </Card>
                </Col>
              </Row>
            )}

            {/* 图表区域 */}
            <Row gutter={[16, 16]}>
              {/* Top问题域统计 */}
              <Col xs={24} md={12}>
                <Card title="Top问题域统计">
                  {topDomains.length > 0 ? (
                    <div style={{ padding: '20px' }}>
                      {topDomains.map((domain, index) => (
                        <div key={index} style={{ marginBottom: '12px' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                            <span>{domain.domainName}</span>
                            <span>{domain.ticketCount} ({domain.percentage.toFixed(1)}%)</span>
                          </div>
                          <div
                            style={{
                              height: '8px',
                              backgroundColor: '#f0f0f0',
                              borderRadius: '4px',
                              overflow: 'hidden',
                            }}
                          >
                            <div
                              style={{
                                height: '100%',
                                width: `${domain.percentage}%`,
                                backgroundColor: '#1890ff',
                              }}
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <Empty description="暂无数据" />
                  )}
                </Card>
              </Col>

              {/* 工单状态统计 */}
              <Col xs={24} md={12}>
                <Card title="工单状态统计">
                  {statusStatistics.length > 0 ? (
                    <div style={{ padding: '20px' }}>
                      {statusStatistics.map((status, index) => (
                        <div key={index} style={{ marginBottom: '12px' }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                            <span>{status.statusLabel}</span>
                            <span>{status.count} ({status.percentage.toFixed(1)}%)</span>
                          </div>
                          <div
                            style={{
                              height: '8px',
                              backgroundColor: '#f0f0f0',
                              borderRadius: '4px',
                              overflow: 'hidden',
                            }}
                          >
                            <div
                              style={{
                                height: '100%',
                                width: `${status.percentage}%`,
                                backgroundColor: '#52c41a',
                              }}
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <Empty description="暂无数据" />
                  )}
                </Card>
              </Col>

              {/* 闭环时间分布 */}
              <Col xs={24} md={12}>
                <Card title="闭环时间分布">
                  {closureTimeDistribution && closureTimeDistribution.ranges.length > 0 ? (
                    <>
                      <div style={{ padding: '20px' }}>
                        {closureTimeDistribution.ranges.map((range, index) => (
                          <div key={index} style={{ marginBottom: '12px' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                              <span>{range.range}</span>
                              <span>{range.count} ({range.percentage.toFixed(1)}%)</span>
                            </div>
                            <div
                              style={{
                                height: '8px',
                                backgroundColor: '#f0f0f0',
                                borderRadius: '4px',
                                overflow: 'hidden',
                              }}
                            >
                              <div
                                style={{
                                  height: '100%',
                                  width: `${range.percentage}%`,
                                  backgroundColor: '#1890ff',
                                }}
                              />
                            </div>
                          </div>
                        ))}
                      </div>
                      <div style={{ marginTop: '16px', padding: '0 20px', fontSize: '12px', color: '#666' }}>
                        <div>平均闭环时间: {formatDuration(closureTimeDistribution.averageClosureTime)}</div>
                        <div>中位数闭环时间: {formatDuration(closureTimeDistribution.medianClosureTime)}</div>
                        <div>P95闭环时间: {formatDuration(closureTimeDistribution.p95ClosureTime)}</div>
                      </div>
                    </>
                  ) : (
                    <Empty description="暂无数据" />
                  )}
                </Card>
              </Col>

              {/* 趋势数据 */}
              <Col xs={24} md={12}>
                <Card title="工单数趋势">
                  {trendData.length > 0 ? (
                    <div style={{ padding: '20px' }}>
                      {trendData.map((point, index) => (
                        <div key={index} style={{ marginBottom: '8px', display: 'flex', alignItems: 'center' }}>
                          <span style={{ width: '100px', fontSize: '12px' }}>{point.date}</span>
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
                                width: `${(point.value / Math.max(...trendData.map((d) => d.value))) * 100}%`,
                                backgroundColor: '#52c41a',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'flex-end',
                                paddingRight: '4px',
                                color: '#fff',
                                fontSize: '11px',
                              }}
                            >
                              {point.value}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <Empty description="暂无数据" />
                  )}
                </Card>
              </Col>
            </Row>
          </>
        )}
      </Card>
    </div>
  );
}

