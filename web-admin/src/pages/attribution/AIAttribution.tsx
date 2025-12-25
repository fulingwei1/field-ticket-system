"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Button,
  Space,
  message,
  Spin,
  Descriptions,
  Tag,
  Alert,
  Table,
  DatePicker,
  Row,
  Col,
  Statistic,
  List,
} from 'antd';
import {
  ArrowLeftOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import {
  aiAttributionService,
  AttributionSuggestionDto,
  ConsistencyCheckResultDto,
  AttributionStatisticsDto,
} from '../../services/aiAttributionService';
import { ticketService } from '../../services/ticketService';
import { DistributionChart } from '../../components/common/DistributionChart';
import { ConfidenceBar } from '../../components/common/ConfidenceBar';
import dayjs, { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;

export default function AIAttribution() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [suggestion, setSuggestion] = useState<AttributionSuggestionDto | null>(null);
  const [consistencyCheck, setConsistencyCheck] = useState<ConsistencyCheckResultDto | null>(null);
  const [statistics, setStatistics] = useState<AttributionStatisticsDto | null>(null);
  const [ticketInfo, setTicketInfo] = useState<any>(null);
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null);

  useEffect(() => {
    if (ticketId) {
      loadTicketInfo();
      loadSuggestion();
    }
    loadStatistics();
  }, [ticketId]);

  const loadTicketInfo = async () => {
    if (!ticketId) return;
    try {
      const ticket = await ticketService.getTicket(ticketId);
      setTicketInfo(ticket);
    } catch (error) {
      console.error('Failed to load ticket:', error);
    }
  };

  const loadSuggestion = async () => {
    if (!ticketId) return;
    try {
      setLoading(true);
      const sugg = await aiAttributionService.suggestAttribution(ticketId);
      setSuggestion(sugg);
    } catch (error: any) {
      message.error(error.message || '生成归因建议失败');
    } finally {
      setLoading(false);
    }
  };

  const loadConsistencyCheck = async (rootResponsibility: string, isPreventable?: boolean) => {
    if (!ticketId) return;
    try {
      setLoading(true);
      const result = await aiAttributionService.checkConsistency(ticketId, {
        rootResponsibility,
        isPreventable,
      });
      setConsistencyCheck(result);
    } catch (error: any) {
      message.error(error.message || '检查一致性失败');
    } finally {
      setLoading(false);
    }
  };

  const loadStatistics = async (fromDate?: string, toDate?: string) => {
    try {
      const stats = await aiAttributionService.getAttributionStatistics(fromDate, toDate);
      setStatistics(stats);
    } catch (error: any) {
      console.error('Failed to load statistics:', error);
    }
  };

  const handleDateRangeChange = (dates: [Dayjs, Dayjs] | null) => {
    setDateRange(dates);
    if (dates) {
      loadStatistics(dates[0].format('YYYY-MM-DD'), dates[1].format('YYYY-MM-DD'));
    } else {
      loadStatistics();
    }
  };

  const responsibilityMap: Record<string, string> = {
    design: '设计',
    software: '软件',
    parameter: '参数',
    assembly: '装配',
    documentation: '文档',
    other: '其他',
    unknown: '未知',
  };

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 头部 */}
        <Card>
          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <Space>
              <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
                返回
              </Button>
              <h2 style={{ margin: 0 }}>AI辅助归因</h2>
            </Space>
            {ticketId && (
              <Button icon={<ReloadOutlined />} onClick={loadSuggestion} loading={loading}>
                重新生成建议
              </Button>
            )}
          </Space>
        </Card>

        {/* 工单信息 */}
        {ticketInfo && (
          <Card title="工单信息" size="small">
            <Descriptions column={3} size="small">
              <Descriptions.Item label="工单编号">{ticketInfo.ticketNo}</Descriptions.Item>
              <Descriptions.Item label="症状">{ticketInfo.symptomTitle}</Descriptions.Item>
              <Descriptions.Item label="状态">{ticketInfo.status}</Descriptions.Item>
            </Descriptions>
          </Card>
        )}

        {/* 归因建议 */}
        {suggestion && (
          <Card
            title="AI归因建议"
            extra={<ConfidenceBar confidence={suggestion.confidence} size="small" />}
          >
            <Descriptions column={2}>
              <Descriptions.Item label="根因分类">
                <Tag color="blue">{responsibilityMap[suggestion.rootResponsibility] || suggestion.rootResponsibility}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="是否可预防">
                {suggestion.isPreventable !== undefined ? (
                  <Tag color={suggestion.isPreventable ? 'green' : 'orange'}>
                    {suggestion.isPreventable ? '可预防' : '不可预防'}
                  </Tag>
                ) : (
                  <span>未确定</span>
                )}
              </Descriptions.Item>
            </Descriptions>

            {suggestion.reasons.length > 0 && (
              <div style={{ marginTop: '16px' }}>
                <h4>建议理由：</h4>
                <List
                  size="small"
                  dataSource={suggestion.reasons}
                  renderItem={(reason) => <List.Item>{reason}</List.Item>}
                />
              </div>
            )}

            {suggestion.similarTickets.length > 0 && (
              <div style={{ marginTop: '16px' }}>
                <h4>相似工单：</h4>
                <Table
                  size="small"
                  columns={[
                    { title: '工单编号', dataIndex: 'ticketNo', key: 'ticketNo' },
                    {
                      title: '根因分类',
                      dataIndex: 'rootResponsibility',
                      key: 'rootResponsibility',
                      render: (text: string) => responsibilityMap[text] || text,
                    },
                    {
                      title: '可预防',
                      dataIndex: 'isPreventable',
                      key: 'isPreventable',
                      render: (val?: boolean) =>
                        val !== undefined ? (val ? '是' : '否') : '未知',
                    },
                    {
                      title: '相似度',
                      dataIndex: 'similarityScore',
                      key: 'similarityScore',
                      render: (score: number) => `${(score * 100).toFixed(1)}%`,
                    },
                  ]}
                  dataSource={suggestion.similarTickets}
                  pagination={false}
                />
              </div>
            )}

            <div style={{ marginTop: '16px' }}>
              <Button
                type="primary"
                onClick={() =>
                  loadConsistencyCheck(suggestion.rootResponsibility, suggestion.isPreventable)
                }
                loading={loading}
              >
                检查一致性
              </Button>
            </div>
          </Card>
        )}

        {/* 一致性检查结果 */}
        {consistencyCheck && (
          <Card
            title="一致性检查结果"
            extra={
              <Tag color={consistencyCheck.isConsistent ? 'success' : 'warning'}>
                {consistencyCheck.isConsistent ? '一致' : '不一致'}
              </Tag>
            }
          >
            <Row gutter={16} style={{ marginBottom: '16px' }}>
              <Col span={12}>
                <div>
                  <div style={{ marginBottom: '8px' }}>一致性分数</div>
                  <ConfidenceBar confidence={consistencyCheck.consistencyScore} />
                </div>
              </Col>
              <Col span={12}>
                <Statistic
                  title="问题数量"
                  value={consistencyCheck.issues.length}
                />
              </Col>
            </Row>

            {consistencyCheck.issues.length > 0 && (
              <Alert
                message="发现不一致问题"
                description={
                  <ul style={{ margin: '8px 0', paddingLeft: '20px' }}>
                    {consistencyCheck.issues.map((issue, idx) => (
                      <li key={idx}>
                        <strong>{issue.field}</strong>: 当前值 "{issue.currentValue}" 与预期值 "
                        {issue.expectedValue}" 不一致。{issue.reason}
                      </li>
                    ))}
                  </ul>
                }
                type="warning"
                showIcon
                style={{ marginBottom: '16px' }}
              />
            )}

            {consistencyCheck.suggestions.length > 0 && (
              <div>
                <h4>建议：</h4>
                <List
                  size="small"
                  dataSource={consistencyCheck.suggestions}
                  renderItem={(suggestion) => (
                    <List.Item>
                      <Space>
                        <CheckCircleOutlined style={{ color: '#52c41a' }} />
                        <span>{suggestion}</span>
                      </Space>
                    </List.Item>
                  )}
                />
              </div>
            )}

            {consistencyCheck.isConsistent && (
              <Alert message="归因与历史数据一致" type="success" showIcon />
            )}
          </Card>
        )}

        {/* 归因统计 */}
        {statistics && (
          <Card
            title="归因统计"
            extra={
              <RangePicker
                value={dateRange}
                onChange={handleDateRangeChange}
                format="YYYY-MM-DD"
              />
            }
          >
            <Row gutter={16} style={{ marginBottom: '16px' }}>
              <Col span={6}>
                <Statistic title="总工单数" value={statistics.totalTickets} />
              </Col>
              <Col span={6}>
                <Statistic title="已归因工单" value={statistics.attributedTickets} />
              </Col>
              <Col span={6}>
                <Statistic
                  title="归因率"
                  value={(statistics.attributionRate * 100).toFixed(1)}
                  suffix="%"
                />
              </Col>
              <Col span={6}>
                <Statistic
                  title="可预防率"
                  value={(statistics.preventabilityRate * 100).toFixed(1)}
                  suffix="%"
                />
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={12}>
                <DistributionChart
                  title="责任分布"
                  distribution={Object.fromEntries(
                    Object.entries(statistics.responsibilityDistribution).map(([key, value]) => [
                      responsibilityMap[key] || key,
                      value,
                    ])
                  )}
                  total={statistics.attributedTickets}
                />
              </Col>
              <Col span={12}>
                <Card title="详细数据" size="small">
                  <Table
                    size="small"
                    columns={[
                      {
                        title: '根因分类',
                        dataIndex: 'responsibility',
                        key: 'responsibility',
                        render: (text: string) => responsibilityMap[text] || text,
                      },
                      { title: '数量', dataIndex: 'count', key: 'count' },
                      {
                        title: '占比',
                        dataIndex: 'percentage',
                        key: 'percentage',
                        render: (text: number) => `${text.toFixed(1)}%`,
                      },
                    ]}
                    dataSource={Object.entries(statistics.responsibilityDistribution).map(
                      ([responsibility, count]) => ({
                        responsibility,
                        count,
                        percentage: statistics.responsibilityPercentage[responsibility] || 0,
                      })
                    )}
                    pagination={false}
                  />
                </Card>
              </Col>
            </Row>
          </Card>
        )}
      </Space>
    </div>
  );
}

