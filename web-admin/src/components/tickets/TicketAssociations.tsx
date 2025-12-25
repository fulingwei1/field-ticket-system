"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Tag,
  Space,
  Typography,
  Spin,
  Empty,
  Tabs,
  Progress,
  Button,
  Tooltip,
} from 'antd';
import {
  LinkOutlined,
  ReloadOutlined,
  FileTextOutlined,
  DesktopOutlined,
  UserOutlined,
  ApartmentOutlined,
} from '@ant-design/icons';
import {
  ticketAssociationService,
  TicketAssociationDto,
  AssociatedTicketDto,
} from '../../services/ticketAssociationService';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;
const { TabPane } = Tabs;

interface TicketAssociationsProps {
  ticketId: string;
}

export default function TicketAssociations({ ticketId }: TicketAssociationsProps) {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [associations, setAssociations] = useState<TicketAssociationDto | null>(null);
  const [activeTab, setActiveTab] = useState('all');

  useEffect(() => {
    if (ticketId) {
      loadAssociations();
    }
  }, [ticketId]);

  const loadAssociations = async () => {
    try {
      setLoading(true);
      const data = await ticketAssociationService.getAssociations(ticketId);
      setAssociations(data);
    } catch (error: any) {
      console.error('Failed to load associations:', error);
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

  const getDomainLabel = (domain: string) => {
    const domainMap: Record<string, string> = {
      A: '机械/动作',
      B: '电气/IO',
      C: 'PLC/程序',
      D: '测试/判定',
      E: '系统/环境',
    };
    return domainMap[domain] || domain;
  };

  const getAssociationTypeLabel = (type?: string) => {
    const typeMap: Record<string, { label: string; icon: React.ReactNode; color: string }> = {
      similar: { label: '相似工单', icon: <LinkOutlined />, color: 'blue' },
      device: { label: '设备关联', icon: <DesktopOutlined />, color: 'green' },
      customer: { label: '客户关联', icon: <UserOutlined />, color: 'orange' },
      domain: { label: '问题域关联', icon: <ApartmentOutlined />, color: 'purple' },
    };
    return typeMap[type || ''] || { label: type || '未知', icon: <FileTextOutlined />, color: 'default' };
  };

  const renderTicketTable = (tickets: AssociatedTicketDto[], showSimilarity: boolean = false) => {
    const columns = [
      {
        title: '工单编号',
        dataIndex: 'ticketNo',
        key: 'ticketNo',
        render: (text: string, record: AssociatedTicketDto) => (
          <Button
            type="link"
            onClick={() => navigate(`/tickets/${record.ticketId}`)}
            style={{ padding: 0 }}
          >
            {text}
          </Button>
        ),
      },
      {
        title: '症状标题',
        dataIndex: 'symptomTitle',
        key: 'symptomTitle',
        ellipsis: true,
      },
      {
        title: '问题域',
        dataIndex: 'domain',
        key: 'domain',
        render: (domain: string) => (
          <Tag color="blue">{getDomainLabel(domain)}</Tag>
        ),
      },
      {
        title: '状态',
        dataIndex: 'status',
        key: 'status',
        render: (status: string) => (
          <Tag color={getStatusColor(status)}>{getStatusLabel(status)}</Tag>
        ),
      },
      {
        title: '创建时间',
        dataIndex: 'createdAt',
        key: 'createdAt',
        render: (text: string) => new Date(text).toLocaleString('zh-CN'),
      },
      ...(showSimilarity
        ? [
            {
              title: '相似度',
              dataIndex: 'similarityScore',
              key: 'similarityScore',
              render: (score?: number) => {
                if (score === undefined) return '-';
                const percent = (score * 100).toFixed(1);
                return (
                  <Tooltip title={`相似度: ${percent}%`}>
                    <Progress
                      percent={parseFloat(percent)}
                      size="small"
                      format={() => `${percent}%`}
                      strokeColor={score >= 0.7 ? '#52c41a' : score >= 0.5 ? '#faad14' : '#ff4d4f'}
                    />
                  </Tooltip>
                );
              },
            },
          ]
        : []),
      {
        title: '关联原因',
        dataIndex: 'associationReason',
        key: 'associationReason',
        render: (reason?: string) => (
          <Text type="secondary" style={{ fontSize: '12px' }}>
            {reason || '-'}
          </Text>
        ),
      },
    ];

    return (
      <Table
        columns={columns}
        dataSource={tickets}
        rowKey="ticketId"
        pagination={false}
        size="small"
      />
    );
  };

  if (loading) {
    return (
      <Card>
        <Spin tip="加载关联工单信息..." />
      </Card>
    );
  }

  if (!associations) {
    return (
      <Card>
        <Empty description="暂无关联工单信息" />
      </Card>
    );
  }

  const allTickets = [
    ...associations.similarTickets.map(t => ({ ...t, associationType: 'similar' })),
    ...associations.deviceRelatedTickets.map(t => ({ ...t, associationType: 'device' })),
    ...associations.customerRelatedTickets.map(t => ({ ...t, associationType: 'customer' })),
    ...associations.domainRelatedTickets.map(t => ({ ...t, associationType: 'domain' })),
  ];

  return (
    <Card
      title={
        <Space>
          <span>关联工单分析</span>
          <Tag color="blue">共 {associations.totalCount} 个关联工单</Tag>
          <Button
            type="text"
            icon={<ReloadOutlined />}
            onClick={loadAssociations}
            size="small"
          >
            刷新
          </Button>
        </Space>
      }
    >
      <Tabs activeKey={activeTab} onChange={setActiveTab}>
        <TabPane
          tab={
            <span>
              <FileTextOutlined /> 全部 ({associations.totalCount})
            </span>
          }
          key="all"
        >
          {allTickets.length > 0 ? (
            <Table
              columns={[
                {
                  title: '关联类型',
                  dataIndex: 'associationType',
                  key: 'associationType',
                  render: (type?: string) => {
                    const typeInfo = getAssociationTypeLabel(type);
                    return (
                      <Tag color={typeInfo.color} icon={typeInfo.icon}>
                        {typeInfo.label}
                      </Tag>
                    );
                  },
                },
                {
                  title: '工单编号',
                  dataIndex: 'ticketNo',
                  key: 'ticketNo',
                  render: (text: string, record: AssociatedTicketDto) => (
                    <Button
                      type="link"
                      onClick={() => navigate(`/tickets/${record.ticketId}`)}
                      style={{ padding: 0 }}
                    >
                      {text}
                    </Button>
                  ),
                },
                {
                  title: '症状标题',
                  dataIndex: 'symptomTitle',
                  key: 'symptomTitle',
                  ellipsis: true,
                },
                {
                  title: '状态',
                  dataIndex: 'status',
                  key: 'status',
                  render: (status: string) => (
                    <Tag color={getStatusColor(status)}>{getStatusLabel(status)}</Tag>
                  ),
                },
                {
                  title: '相似度',
                  dataIndex: 'similarityScore',
                  key: 'similarityScore',
                  render: (score?: number) => {
                    if (score === undefined) return '-';
                    const percent = (score * 100).toFixed(1);
                    return (
                      <Progress
                        percent={parseFloat(percent)}
                        size="small"
                        format={() => `${percent}%`}
                        strokeColor={score >= 0.7 ? '#52c41a' : score >= 0.5 ? '#faad14' : '#ff4d4f'}
                      />
                    );
                  },
                },
                {
                  title: '创建时间',
                  dataIndex: 'createdAt',
                  key: 'createdAt',
                  render: (text: string) => new Date(text).toLocaleString('zh-CN'),
                },
              ]}
              dataSource={allTickets}
              rowKey="ticketId"
              pagination={false}
              size="small"
            />
          ) : (
            <Empty description="暂无关联工单" />
          )}
        </TabPane>

        <TabPane
          tab={
            <span>
              <LinkOutlined /> 相似工单 ({associations.similarTickets.length})
            </span>
          }
          key="similar"
        >
          {associations.similarTickets.length > 0 ? (
            renderTicketTable(associations.similarTickets, true)
          ) : (
            <Empty description="暂无相似工单" />
          )}
        </TabPane>

        <TabPane
          tab={
            <span>
              <DesktopOutlined /> 设备关联 ({associations.deviceRelatedTickets.length})
            </span>
          }
          key="device"
        >
          {associations.deviceRelatedTickets.length > 0 ? (
            renderTicketTable(associations.deviceRelatedTickets)
          ) : (
            <Empty description="暂无设备关联工单" />
          )}
        </TabPane>

        <TabPane
          tab={
            <span>
              <UserOutlined /> 客户关联 ({associations.customerRelatedTickets.length})
            </span>
          }
          key="customer"
        >
          {associations.customerRelatedTickets.length > 0 ? (
            renderTicketTable(associations.customerRelatedTickets)
          ) : (
            <Empty description="暂无客户关联工单" />
          )}
        </TabPane>

        <TabPane
          tab={
            <span>
              <ApartmentOutlined /> 问题域关联 ({associations.domainRelatedTickets.length})
            </span>
          }
          key="domain"
        >
          {associations.domainRelatedTickets.length > 0 ? (
            renderTicketTable(associations.domainRelatedTickets)
          ) : (
            <Empty description="暂无问题域关联工单" />
          )}
        </TabPane>
      </Tabs>
    </Card>
  );
}








