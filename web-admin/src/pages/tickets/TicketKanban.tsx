import React, { useEffect, useState } from 'react';
import {
  Card,
  Col,
  Row,
  Tag,
  Space,
  Avatar,
  Typography,
  Spin,
  message,
  Button,
  Tooltip,
} from 'antd';
import {
  ClockCircleOutlined,
  UserOutlined,
  WarningOutlined,
  CheckCircleOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import { ticketService, TicketListItemDto } from '../../services/ticketService';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import 'dayjs/locale/zh-cn';

dayjs.extend(relativeTime);
dayjs.locale('zh-cn');

const { Text, Title } = Typography;

/**
 * 工单看板页面
 * 按状态分四列展示：已提交、分诊中、方案已发布、验证中
 * 适合高级工程师快速查看和处理工单
 */
const TicketKanban: React.FC = () => {
  const [tickets, setTickets] = useState<TicketListItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const columns = [
    { key: 'Submitted', title: '已提交', color: '#1890ff', icon: <ClockCircleOutlined /> },
    { key: 'Triage', title: '分诊中', color: '#faad14', icon: <WarningOutlined /> },
    { key: 'SolutionIssued', title: '方案已发布', color: '#52c41a', icon: <CheckCircleOutlined /> },
    { key: 'Verifying', title: '验证中', color: '#722ed1', icon: <ClockCircleOutlined /> },
  ];

  const priorityMap: Record<string, { label: string; color: string }> = {
    Critical: { label: '紧急', color: 'red' },
    High: { label: '高', color: 'orange' },
    Medium: { label: '中', color: 'blue' },
    Low: { label: '低', color: 'default' },
  };

  const domainMap: Record<string, { label: string; color: string }> = {
    A: { label: '机械', color: 'blue' },
    B: { label: '电气', color: 'green' },
    C: { label: 'PLC', color: 'orange' },
    D: { label: '测试', color: 'purple' },
    E: { label: '系统', color: 'red' },
  };

  useEffect(() => {
    loadTickets();
  }, []);

  const loadTickets = async () => {
    setLoading(true);
    try {
      // 只加载四个目标状态的工单
      const result = await ticketService.getTickets({
        page: 1,
        pageSize: 100,
        statuses: ['Submitted', 'Triage', 'SolutionIssued', 'Verifying'],
      });
      setTickets(result.items);
    } catch (error) {
      console.error('Failed to load tickets:', error);
      message.error('加载工单失败');
    } finally {
      setLoading(false);
    }
  };

  const getTicketsByStatus = (status: string) => {
    return tickets.filter(t => t.status === status);
  };

  const handleCardClick = (ticketId: string) => {
    navigate(`/tickets/${ticketId}`);
  };

  const renderTicketCard = (ticket: TicketListItemDto) => {
    const createdAt = dayjs(ticket.createdAt);
    const timeAgo = createdAt.fromNow();
    const priority = priorityMap[ticket.priority] || { label: ticket.priority, color: 'default' };
    const domain = domainMap[ticket.domain] || { label: ticket.domain, color: 'default' };

    return (
      <Card
        key={ticket.ticketId}
        hoverable
        size="small"
        className="ticket-card"
        style={{ marginBottom: 12, cursor: 'pointer' }}
        onClick={() => handleCardClick(ticket.ticketId)}
      >
        {/* 工单号和优先级 */}
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
          <Text strong style={{ fontSize: 13 }}>
            {ticket.ticketNumber}
          </Text>
          <Tag color={priority.color} style={{ margin: 0 }}>
            {priority.label}
          </Tag>
        </div>

        {/* 标题 */}
        <div style={{ marginBottom: 8 }}>
          <Text
            ellipsis={{ tooltip: ticket.title }}
            style={{ fontSize: 14, fontWeight: 500 }}
          >
            {ticket.title || '无标题'}
          </Text>
        </div>

        {/* 客户和设备 */}
        <div style={{ marginBottom: 8 }}>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {ticket.customerName} / {ticket.deviceName}
          </Text>
        </div>

        {/* 问题域和步骤 */}
        <Space size={4} style={{ marginBottom: 8 }}>
          <Tag color={domain.color} style={{ fontSize: 11 }}>
            {domain.label}
          </Tag>
          {ticket.procedureStep && (
            <Tag color="default" style={{ fontSize: 11 }}>
              {ticket.procedureStep}
            </Tag>
          )}
        </Space>

        {/* 创建人和时间 */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Space size={4}>
            <Avatar size={20} icon={<UserOutlined />} />
            <Text type="secondary" style={{ fontSize: 12 }}>
              {ticket.createdByName || '未知'}
            </Text>
          </Space>
          <Tooltip title={createdAt.format('YYYY-MM-DD HH:mm')}>
            <Text type="secondary" style={{ fontSize: 11 }}>
              {timeAgo}
            </Text>
          </Tooltip>
        </div>
      </Card>
    );
  };

  const renderColumn = (column: typeof columns[0]) => {
    const columnTickets = getTicketsByStatus(column.key);

    return (
      <Col key={column.key} span={6}>
        <Card
          title={
            <Space>
              <span style={{ color: column.color }}>{column.icon}</span>
              <span>{column.title}</span>
              <Tag color={column.color}>{columnTickets.length}</Tag>
            </Space>
          }
          bordered={false}
          style={{ height: 'calc(100vh - 180px)', overflow: 'auto' }}
        >
          {columnTickets.map(ticket => renderTicketCard(ticket))}
          {columnTickets.length === 0 && (
            <div style={{ textAlign: 'center', padding: '40px 0', color: '#999' }}>
              暂无工单
            </div>
          )}
        </Card>
      </Col>
    );
  };

  return (
    <div style={{ padding: 24 }}>
      {/* 页面头部 */}
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2} style={{ margin: 0 }}>
          工单看板
        </Title>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={loadTickets} loading={loading}>
            刷新
          </Button>
          <Button type="primary" onClick={() => navigate('/tickets')}>
            列表视图
          </Button>
        </Space>
      </div>

      {/* 看板列 */}
      <Spin spinning={loading}>
        <Row gutter={16}>
          {columns.map(column => renderColumn(column))}
        </Row>
      </Spin>

      <style>{`
        .ticket-card {
          transition: all 0.3s ease;
        }
        .ticket-card:hover {
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
          transform: translateY(-2px);
        }
      `}</style>
    </div>
  );
};

export default TicketKanban;
