import React, { useEffect, useState } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  Table,
  Tag,
  Space,
  Typography,
  Button,
  List,
  Avatar,
  Progress,
} from 'antd';
import {
  FileTextOutlined,
  UserOutlined,
  CustomerServiceOutlined,
  DatabaseOutlined,
  RiseOutlined,
  FallOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  WarningOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';

const { Title, Text } = Typography;

interface DashboardStats {
  totalTickets: number;
  pendingTickets: number;
  resolvedTickets: number;
  activeUsers: number;
  totalCustomers: number;
  totalDevices: number;
  avgResolutionTime: number;
  ticketGrowth: number;
}

interface RecentTicket {
  ticketId: string;
  ticketNumber: string;
  title: string;
  status: string;
  priority: string;
  createdAt: string;
  customerName: string;
}

interface ActivityLog {
  id: string;
  type: string;
  user: string;
  action: string;
  time: string;
}

/**
 * 首页 Dashboard
 * 展示系统概览、关键指标、最近工单、活动日志等
 */
const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [stats, setStats] = useState<DashboardStats>({
    totalTickets: 0,
    pendingTickets: 0,
    resolvedTickets: 0,
    activeUsers: 0,
    totalCustomers: 0,
    totalDevices: 0,
    avgResolutionTime: 0,
    ticketGrowth: 0,
  });
  const [recentTickets, setRecentTickets] = useState<RecentTicket[]>([]);
  const [activities, setActivities] = useState<ActivityLog[]>([]);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    setLoading(true);
    try {
      // TODO: 调用实际的 API 获取统计数据
      // const statsData = await dashboardService.getStats();
      // const ticketsData = await dashboardService.getRecentTickets();
      // const activitiesData = await dashboardService.getActivities();

      // 模拟数据
      setStats({
        totalTickets: 1248,
        pendingTickets: 87,
        resolvedTickets: 1089,
        activeUsers: 45,
        totalCustomers: 128,
        totalDevices: 456,
        avgResolutionTime: 4.5,
        ticketGrowth: 12.5,
      });

      setRecentTickets([
        {
          ticketId: '1',
          ticketNumber: 'TK-2024-001',
          title: '设备A异常报警',
          status: 'Submitted',
          priority: 'High',
          createdAt: '2024-01-20 10:30',
          customerName: '客户A',
        },
        {
          ticketId: '2',
          ticketNumber: 'TK-2024-002',
          title: 'PLC程序更新需求',
          status: 'Triage',
          priority: 'Medium',
          createdAt: '2024-01-20 09:15',
          customerName: '客户B',
        },
        {
          ticketId: '3',
          ticketNumber: 'TK-2024-003',
          title: '传感器校准',
          status: 'SolutionIssued',
          priority: 'Low',
          createdAt: '2024-01-19 16:45',
          customerName: '客户C',
        },
      ]);

      setActivities([
        {
          id: '1',
          type: 'ticket',
          user: '张三',
          action: '创建了新工单 TK-2024-001',
          time: '5分钟前',
        },
        {
          id: '2',
          type: 'solution',
          user: '李四',
          action: '为工单 TK-2024-002 创建了解决方案',
          time: '15分钟前',
        },
        {
          id: '3',
          type: 'verify',
          user: '王五',
          action: '验证通过工单 TK-2024-003',
          time: '1小时前',
        },
        {
          id: '4',
          type: 'user',
          user: '管理员',
          action: '添加了新用户：赵六',
          time: '2小时前',
        },
      ]);
    } catch (error) {
      console.error('Failed to load dashboard data:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    const colorMap: Record<string, string> = {
      Submitted: 'blue',
      Triage: 'orange',
      SolutionIssued: 'purple',
      Verifying: 'cyan',
      Closed: 'green',
      Cancelled: 'red',
    };
    return colorMap[status] || 'default';
  };

  const getStatusText = (status: string) => {
    const textMap: Record<string, string> = {
      Submitted: '已提交',
      Triage: '分诊中',
      SolutionIssued: '方案已发布',
      Verifying: '验证中',
      Closed: '已关闭',
      Cancelled: '已取消',
    };
    return textMap[status] || status;
  };

  const getPriorityColor = (priority: string) => {
    const colorMap: Record<string, string> = {
      Critical: 'red',
      High: 'orange',
      Medium: 'blue',
      Low: 'green',
    };
    return colorMap[priority] || 'default';
  };

  const getPriorityText = (priority: string) => {
    const textMap: Record<string, string> = {
      Critical: '紧急',
      High: '高',
      Medium: '中',
      Low: '低',
    };
    return textMap[priority] || priority;
  };

  const getActivityIcon = (type: string) => {
    const iconMap: Record<string, React.ReactNode> = {
      ticket: <FileTextOutlined style={{ color: '#1890ff' }} />,
      solution: <CheckCircleOutlined style={{ color: '#52c41a' }} />,
      verify: <WarningOutlined style={{ color: '#faad14' }} />,
      user: <UserOutlined style={{ color: '#722ed1' }} />,
    };
    return iconMap[type] || <ClockCircleOutlined />;
  };

  const recentTicketColumns = [
    {
      title: '工单编号',
      dataIndex: 'ticketNumber',
      key: 'ticketNumber',
      width: 130,
      render: (text: string, record: RecentTicket) => (
        <Button type="link" onClick={() => navigate(`/tickets/${record.ticketId}`)}>
          {text}
        </Button>
      ),
    },
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
    },
    {
      title: '客户',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 120,
    },
    {
      title: '优先级',
      dataIndex: 'priority',
      key: 'priority',
      width: 80,
      render: (priority: string) => (
        <Tag color={getPriorityColor(priority)}>{getPriorityText(priority)}</Tag>
      ),
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => (
        <Tag color={getStatusColor(status)}>{getStatusText(status)}</Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 150,
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Title level={2} style={{ marginBottom: 24 }}>
        系统概览
      </Title>

      {/* 关键指标卡片 */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="总工单数"
              value={stats.totalTickets}
              prefix={<FileTextOutlined />}
              suffix={
                <Text type="secondary" style={{ fontSize: 14 }}>
                  {stats.ticketGrowth > 0 ? (
                    <span style={{ color: '#52c41a' }}>
                      <RiseOutlined /> {stats.ticketGrowth}%
                    </span>
                  ) : (
                    <span style={{ color: '#ff4d4f' }}>
                      <FallOutlined /> {Math.abs(stats.ticketGrowth)}%
                    </span>
                  )}
                </Text>
              }
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="待处理工单"
              value={stats.pendingTickets}
              prefix={<ClockCircleOutlined />}
              valueStyle={{ color: '#faad14' }}
            />
            <Progress
              percent={Math.round((stats.pendingTickets / stats.totalTickets) * 100)}
              size="small"
              showInfo={false}
              strokeColor="#faad14"
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="已解决工单"
              value={stats.resolvedTickets}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
            <Progress
              percent={Math.round((stats.resolvedTickets / stats.totalTickets) * 100)}
              size="small"
              showInfo={false}
              strokeColor="#52c41a"
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="平均解决时间"
              value={stats.avgResolutionTime}
              suffix="小时"
              prefix={<ClockCircleOutlined />}
              precision={1}
            />
          </Card>
        </Col>
      </Row>

      {/* 系统资源卡片 */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={8}>
          <Card>
            <Statistic
              title="活跃用户"
              value={stats.activeUsers}
              prefix={<TeamOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card>
            <Statistic
              title="客户总数"
              value={stats.totalCustomers}
              prefix={<CustomerServiceOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card>
            <Statistic
              title="设备总数"
              value={stats.totalDevices}
              prefix={<DatabaseOutlined />}
            />
          </Card>
        </Col>
      </Row>

      {/* 最近工单和活动日志 */}
      <Row gutter={[16, 16]}>
        <Col xs={24} lg={16}>
          <Card
            title="最近工单"
            extra={
              <Button type="link" onClick={() => navigate('/tickets')}>
                查看全部
              </Button>
            }
          >
            <Table
              columns={recentTicketColumns}
              dataSource={recentTickets}
              rowKey="ticketId"
              loading={loading}
              pagination={false}
              size="small"
            />
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="最近活动" style={{ height: '100%' }}>
            <List
              dataSource={activities}
              loading={loading}
              renderItem={(item) => (
                <List.Item>
                  <List.Item.Meta
                    avatar={<Avatar icon={getActivityIcon(item.type)} />}
                    title={
                      <Space>
                        <Text strong>{item.user}</Text>
                        <Text type="secondary">{item.action}</Text>
                      </Space>
                    }
                    description={<Text type="secondary">{item.time}</Text>}
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Dashboard;
