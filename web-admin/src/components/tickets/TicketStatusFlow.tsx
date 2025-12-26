"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Timeline,
  Tag,
  Space,
  Typography,
  Spin,
  Empty,
  Tooltip,
  Divider,
} from 'antd';
import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  UserOutlined,
  ArrowRightOutlined,
} from '@ant-design/icons';
import {
  ticketStatusHistoryService,
  TicketStatusFlowDto,
  StatusFlowNodeDto,
  StatusFlowEdgeDto,
} from '../../services/ticketStatusHistoryService';

const { Title, Text } = Typography;

interface TicketStatusFlowProps {
  ticketId: string;
}

export default function TicketStatusFlow({ ticketId }: TicketStatusFlowProps) {
  const [loading, setLoading] = useState(false);
  const [flow, setFlow] = useState<TicketStatusFlowDto | null>(null);

  useEffect(() => {
    if (ticketId) {
      loadStatusFlow();
    }
  }, [ticketId]);

  const loadStatusFlow = async () => {
    try {
      setLoading(true);
      const flowData = await ticketStatusHistoryService.getStatusFlow(ticketId);
      setFlow(flowData);
    } catch (error: any) {
      console.error('Failed to load status flow:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Draft':
        return 'default';
      case 'Submitted':
        return 'processing';
      case 'Triage':
        return 'warning';
      case 'SolutionIssued':
        return 'success';
      case 'Verifying':
        return 'processing';
      case 'Closed':
        return 'default';
      case 'Reopened':
        return 'warning';
      default:
        return 'default';
    }
  };

  const getStatusLabel = (status: string) => {
    const statusMap: Record<string, string> = {
      Draft: '草稿',
      Submitted: '已提交',
      Triage: '分诊中',
      SolutionIssued: '方案已发布',
      Verifying: '验证中',
      Closed: '已关闭',
      Reopened: '已重开',
    };
    return statusMap[status] || status;
  };

  const formatDuration = (duration?: string) => {
    if (!duration) return '-';
    
    // Parse ISO 8601 duration (e.g., "PT2H30M" = 2 hours 30 minutes)
    // Simple parser for common formats
    const match = duration.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);
    if (!match) return duration;

    const hours = parseInt(match[1] || '0');
    const minutes = parseInt(match[2] || '0');
    const seconds = parseInt(match[3] || '0');

    const parts: string[] = [];
    if (hours > 0) parts.push(`${hours}小时`);
    if (minutes > 0) parts.push(`${minutes}分钟`);
    if (seconds > 0 && hours === 0 && minutes === 0) parts.push(`${seconds}秒`);

    return parts.length > 0 ? parts.join('') : '0分钟';
  };

  const formatDateTime = (dateStr?: string) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleString('zh-CN');
  };

  if (loading) {
    return (
      <Card>
        <Spin tip="加载状态流转信息..." />
      </Card>
    );
  }

  if (!flow || flow.nodes.length === 0) {
    return (
      <Card>
        <Empty description="暂无状态流转信息" />
      </Card>
    );
  }

  return (
    <Card
      title={
        <Space>
          <span>工单状态流转</span>
          <Tag color={getStatusColor(flow.currentStatus)}>
            {getStatusLabel(flow.currentStatus)}
          </Tag>
          {flow.totalDuration && (
            <Text type="secondary">
              总时长: {formatDuration(flow.totalDuration)}
            </Text>
          )}
        </Space>
      }
    >
      <Timeline
        mode="left"
        items={flow.nodes.map((node, index) => {
          const edge = flow.edges.find(e => e.toStatus === node.status);
          const isLast = index === flow.nodes.length - 1;

          return {
            dot: node.isCurrent ? (
              <CheckCircleOutlined style={{ fontSize: '16px', color: '#52c41a' }} />
            ) : (
              <ClockCircleOutlined style={{ fontSize: '16px' }} />
            ),
            color: node.isCurrent ? 'green' : 'blue',
            children: (
              <div style={{ paddingLeft: '16px' }}>
                <Space direction="vertical" size="small" style={{ width: '100%' }}>
                  <div>
                    <Tag color={getStatusColor(node.status)} style={{ fontSize: '14px' }}>
                      {node.label}
                    </Tag>
                    {node.isCurrent && (
                      <Tag color="success" style={{ marginLeft: '8px' }}>
                        当前状态
                      </Tag>
                    )}
                  </div>

                  <div>
                    <Text type="secondary" style={{ fontSize: '12px' }}>
                      <ClockCircleOutlined /> 进入时间: {formatDateTime(node.enteredAt)}
                    </Text>
                    {node.exitedAt && (
                      <>
                        <br />
                        <Text type="secondary" style={{ fontSize: '12px' }}>
                          <ArrowRightOutlined /> 退出时间: {formatDateTime(node.exitedAt)}
                        </Text>
                      </>
                    )}
                  </div>

                  {node.duration && (
                    <div>
                      <Text type="secondary" style={{ fontSize: '12px' }}>
                        停留时长: {formatDuration(node.duration)}
                      </Text>
                    </div>
                  )}

                  {edge && (
                    <div style={{ marginTop: '8px' }}>
                      <Divider style={{ margin: '8px 0' }} />
                      <Space direction="vertical" size="small">
                        <div>
                          <Text type="secondary" style={{ fontSize: '12px' }}>
                            <UserOutlined /> 操作人: {edge.changedByName || edge.changedBy}
                          </Text>
                        </div>
                        {edge.changeReason && (
                          <div>
                            <Text type="secondary" style={{ fontSize: '12px' }}>
                              变更原因: {edge.changeReason}
                            </Text>
                          </div>
                        )}
                        <div>
                          <Tag size="small" color={edge.changeType === 'auto' ? 'blue' : 'default'}>
                            {edge.changeType === 'auto' ? '自动' : edge.changeType === 'system' ? '系统' : '手动'}
                          </Tag>
                        </div>
                      </Space>
                    </div>
                  )}

                  {node.changedByName && !edge && (
                    <div>
                      <Text type="secondary" style={{ fontSize: '12px' }}>
                        <UserOutlined /> 操作人: {node.changedByName}
                      </Text>
                    </div>
                  )}
                </Space>
              </div>
            ),
          };
        })}
      />
    </Card>
  );
}











