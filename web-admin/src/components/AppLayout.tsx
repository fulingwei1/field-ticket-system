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
  DashboardOutlined,
  CustomerServiceOutlined,
  DatabaseOutlined,
  UploadOutlined,
  CheckCircleOutlined,
  UserAddOutlined,
  EditOutlined,
  HistoryOutlined,
  FireOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService, UserInfo } from '../services/authService';
import type { MenuProps } from 'antd';
import { usePermission } from '../hooks/usePermission';

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
  const { canAccessMenu } = usePermission();

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

  // 定义所有菜单项
  const allMenuItems: MenuProps['items'] = [
    {
      key: '/',
      icon: <DashboardOutlined />,
      label: '首页概览',
    },
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
          key: '/tickets/kanban',
          label: '工单看板',
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
      children: [
        {
          key: '/statistics',
          label: '统计面板',
        },
        {
          key: '/analytics/problem-hotspots',
          icon: <FireOutlined />,
          label: '问题热点分析',
        },
      ],
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
          key: '/judgement-cards',
          label: '判断卡列表',
        },
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
      key: '/customers',
      icon: <CustomerServiceOutlined />,
      label: '客户管理',
    },
    {
      key: '/devices',
      icon: <SettingOutlined />,
      label: '设备管理',
      children: [
        {
          key: '/devices',
          label: '设备列表',
        },
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
          key: '/projects',
          label: '项目列表',
        },
        {
          key: '/projects/excel-import',
          label: 'Excel导入',
          icon: <PlusOutlined />,
        },
      ],
    },
    {
      key: '/knowledge-graph',
      icon: <DatabaseOutlined />,
      label: '知识图谱',
    },
    {
      key: '/users',
      icon: <TeamOutlined />,
      label: '用户管理',
      children: [
        {
          key: '/users',
          icon: <UserOutlined />,
          label: '用户列表',
        },
        {
          key: '/users/import',
          icon: <UploadOutlined />,
          label: '员工批量导入',
        },
        {
          key: '/users/update',
          icon: <EditOutlined />,
          label: '员工批量更新',
        },
        {
          key: '/users/activate',
          icon: <CheckCircleOutlined />,
          label: '账户开通审核',
        },
        {
          key: '/users/logs',
          icon: <HistoryOutlined />,
          label: '操作日志',
        },
      ],
    },
  ];

  // 根据权限过滤菜单项
  const filterMenuItems = (items: MenuProps['items']): MenuProps['items'] => {
    if (!items) return [];

    return items
      .map((item: any) => {
        if (!item) return null;

        // 检查当前项是否有权限访问
        const hasAccess = canAccessMenu(item.key);
        if (!hasAccess) return null;

        // 如果有子菜单，递归过滤
        if (item.children && item.children.length > 0) {
          const filteredChildren = filterMenuItems(item.children);
          // 如果所有子菜单都被过滤掉了，则不显示父菜单
          if (filteredChildren.length === 0) return null;

          return {
            ...item,
            children: filteredChildren,
          };
        }

        return item;
      })
      .filter(Boolean);
  };

  const menuItems = filterMenuItems(allMenuItems);

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

    // 对于各个模块，返回当前路径
    if (path.startsWith('/tickets')) return [path];
    if (path.startsWith('/performance')) return [path];
    if (path.startsWith('/statistics')) return ['/statistics'];
    if (path.startsWith('/corrective-actions')) return [path];
    if (path.startsWith('/ai-analysis')) return ['/ai-analysis'];
    if (path.startsWith('/judgement-cards')) return [path];
    if (path.startsWith('/customers')) return ['/customers'];
    if (path.startsWith('/devices')) return [path];
    if (path.startsWith('/projects')) return [path];
    if (path.startsWith('/knowledge-graph')) return ['/knowledge-graph'];
    if (path.startsWith('/users')) return [path]; // 返回完整路径，支持子菜单

    return [];
  };

  // 获取默认展开的菜单项
  const getOpenKeys = () => {
    const path = location.pathname;
    const openKeys: string[] = [];

    // 根据当前路径确定需要展开的菜单
    if (path.startsWith('/tickets')) openKeys.push('/tickets');
    if (path.startsWith('/performance')) openKeys.push('/performance');
    if (path.startsWith('/ai-analysis')) openKeys.push('/ai-analysis');
    if (path.startsWith('/judgement-cards')) openKeys.push('/judgement-cards');
    if (path.startsWith('/devices')) openKeys.push('/devices');
    if (path.startsWith('/projects')) openKeys.push('/projects');
    if (path.startsWith('/users')) openKeys.push('/users'); // 展开用户管理子菜单

    return openKeys;
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
          defaultOpenKeys={getOpenKeys()}
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

