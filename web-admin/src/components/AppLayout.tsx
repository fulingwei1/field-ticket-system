import React, { useState, useEffect } from 'react';
import { Layout, Menu, Avatar, Dropdown, Space, Typography, Button } from 'antd';
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  FileTextOutlined,
  PlusOutlined,
  BarChartOutlined,
  TeamOutlined,
  UserOutlined,
  LogoutOutlined,
  BulbOutlined,
  SafetyOutlined,
  SettingOutlined,
  ToolOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService, UserInfo } from '../services/authService';
import type { MenuProps } from 'antd';

const { Header, Sider, Content } = Layout;
const { Text } = Typography;

interface AppLayoutProps {
  children: React.ReactNode;
}

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const [collapsed, setCollapsed] = useState(false);
  const [user, setUser] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    // 检查是否已登录
    if (!authService.isAuthenticated()) {
      // 如果当前不在登录页，跳转到登录页
      if (location.pathname !== '/login') {
        navigate('/login', { replace: true });
      }
      setLoading(false);
      return;
    }

    // 加载用户信息
    const currentUser = authService.getUser();
    setUser(currentUser);
    setLoading(false);

    // 尝试从服务器获取最新用户信息
    authService.getCurrentUser().then((latestUser) => {
      if (latestUser) {
        setUser(latestUser);
      }
    }).catch(() => {
      // 如果获取用户信息失败，不影响页面显示
      setLoading(false);
    });
  }, [navigate, location.pathname]);

  const handleLogout = () => {
    authService.clearAuth();
    navigate('/login');
  };

  const menuItems: MenuProps['items'] = [
    {
      key: '/tickets',
      icon: <FileTextOutlined />,
      label: '工单管理',
      children: [
        {
          key: '/tickets',
          label: '工单列表',
        },
        {
          key: '/tickets/new',
          label: '创建工单',
          icon: <PlusOutlined />,
        },
        {
          key: '/tickets/templates',
          label: '工单模板',
        },
      ],
    },
    {
      key: '/performance',
      icon: <BarChartOutlined />,
      label: '绩效管理',
      children: [
        {
          key: '/performance/my',
          label: '我的绩效',
          icon: <UserOutlined />,
        },
        {
          key: '/performance/team',
          label: '团队绩效',
          icon: <TeamOutlined />,
        },
        {
          key: '/performance/load-stats',
          label: '负载统计',
        },
        {
          key: '/performance/growth-curve',
          label: '成长曲线',
        },
      ],
    },
    {
      key: '/statistics',
      icon: <BarChartOutlined />,
      label: '统计分析',
    },
    {
      key: '/corrective-actions',
      icon: <ToolOutlined />,
      label: '整改任务',
    },
    {
      key: '/ai-analysis',
      icon: <BulbOutlined />,
      label: 'AI分析',
    },
    {
      key: '/judgement-cards',
      icon: <SafetyOutlined />,
      label: '判断卡管理',
      children: [
        {
          key: '/judgement-cards/quality',
          label: '质量评分',
        },
        {
          key: '/judgement-cards/version',
          label: '版本管理',
        },
      ],
    },
    {
      key: '/devices',
      icon: <SettingOutlined />,
      label: '设备管理',
      children: [
        {
          key: '/devices/config-snapshot',
          label: '配置快照',
        },
        {
          key: '/devices/qrcode-generator',
          label: '二维码生成',
        },
      ],
    },
    {
      key: '/projects',
      icon: <FileTextOutlined />,
      label: '项目管理',
      children: [
        {
          key: '/projects/excel-import',
          label: 'Excel导入',
          icon: <PlusOutlined />,
        },
      ],
    },
  ];

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'profile',
      label: '个人信息',
      icon: <UserOutlined />,
      disabled: true, // 待实现
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      label: '退出登录',
      icon: <LogoutOutlined />,
      danger: true,
      onClick: handleLogout,
    },
  ];

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key);
  };

  // 获取当前选中的菜单项
  const getSelectedKeys = () => {
    const path = location.pathname;
    if (path.startsWith('/performance')) {
      return [path];
    }
    if (path.startsWith('/tickets')) {
      return [path];
    }
    if (path.startsWith('/corrective-actions')) {
      return [path];
    }
    return [];
  };

  // 如果未登录，不渲染布局（让 React Router 处理跳转）
  if (!authService.isAuthenticated()) {
    return null;
  }

  // 如果正在加载，显示加载状态
  if (loading) {
    return (
      <Layout style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div>加载中...</div>
      </Layout>
    );
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider trigger={null} collapsible collapsed={collapsed} theme="light">
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            borderBottom: '1px solid #f0f0f0',
          }}
        >
          {collapsed ? (
            <Text strong style={{ fontSize: 20 }}>
              工单
            </Text>
          ) : (
            <Text strong style={{ fontSize: 16 }}>
              现场问题反馈系统
            </Text>
          )}
        </div>
        <Menu
          theme="light"
          mode="inline"
          selectedKeys={getSelectedKeys()}
          items={menuItems}
          onClick={handleMenuClick}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            padding: '0 24px',
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            borderBottom: '1px solid #f0f0f0',
          }}
        >
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            style={{ fontSize: 16, width: 64, height: 64 }}
          />
          <Space>
            <Text>{user?.name || '用户'}</Text>
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
              <Avatar
                style={{ backgroundColor: '#1890ff', cursor: 'pointer' }}
                icon={<UserOutlined />}
              />
            </Dropdown>
          </Space>
        </Header>
        <Content
          style={{
            margin: '0',
            padding: 0,
            minHeight: 280,
            background: '#f0f2f5',
          }}
        >
          {children}
        </Content>
      </Layout>
    </Layout>
  );
};

export default AppLayout;

