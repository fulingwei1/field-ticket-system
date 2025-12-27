import React, { useEffect, useState } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  message,
  Tag,
  Typography,
  Input,
  Modal,
  Descriptions,
  Popconfirm,
  Row,
  Col,
  Statistic,
  Alert,
} from 'antd';
import {
  CheckCircleOutlined,
  UserAddOutlined,
  SearchOutlined,
  EyeOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import {
  employeeImportService,
  type InactivatedUserDto,
} from '../../services/employeeImportService';

const { Title, Text } = Typography;
const { Search } = Input;

/**
 * 账户开通页面
 */
export default function AccountActivation() {
  const [users, setUsers] = useState<InactivatedUserDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRowKeys, setSelectedRowKeys] = useState<string[]>([]);
  const [activating, setActivating] = useState(false);
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [selectedUser, setSelectedUser] = useState<InactivatedUserDto | null>(null);

  useEffect(() => {
    loadInactivatedUsers();
  }, [page, pageSize, searchQuery]);

  const loadInactivatedUsers = async () => {
    setLoading(true);
    try {
      const result = await employeeImportService.getInactivatedUsers({
        page,
        pageSize,
        searchQuery,
      });
      setUsers(result.items);
      setTotal(result.total);
    } catch (error) {
      console.error('Failed to load inactivated users:', error);
      message.error('加载未开通账户列表失败');
    } finally {
      setLoading(false);
    }
  };

  // 开通单个账户
  const handleActivateSingle = async (userId: string, userName: string) => {
    try {
      const result = await employeeImportService.activateAccount(userId);
      if (result.success) {
        message.success(`已开通 ${userName} 的账户`);
        loadInactivatedUsers();
      } else {
        message.error(result.message);
      }
    } catch (error: any) {
      console.error('Failed to activate account:', error);
      message.error(error.message || '开通失败');
    }
  };

  // 批量开通账户
  const handleBatchActivate = async () => {
    if (selectedRowKeys.length === 0) {
      message.warning('请先选择要开通的账户');
      return;
    }

    setActivating(true);
    try {
      const result = await employeeImportService.batchActivateAccounts(selectedRowKeys);
      if (result.success) {
        message.success(`成功开通 ${result.activatedCount} 个账户`);
        setSelectedRowKeys([]);
        loadInactivatedUsers();
      } else {
        message.error(result.message);
      }
    } catch (error: any) {
      console.error('Failed to batch activate:', error);
      message.error(error.message || '批量开通失败');
    } finally {
      setActivating(false);
    }
  };

  // 全选/取消全选
  const handleSelectAll = () => {
    if (selectedRowKeys.length === users.length) {
      setSelectedRowKeys([]);
    } else {
      setSelectedRowKeys(users.map(user => user.id));
    }
  };

  // 查看详情
  const handleViewDetail = (user: InactivatedUserDto) => {
    setSelectedUser(user);
    setDetailModalVisible(true);
  };

  const roleMap: Record<string, { label: string; color: string }> = {
    FieldEngineer: { label: '现场工程师', color: 'blue' },
    CS: { label: '客服', color: 'green' },
    SeniorEngineer: { label: '高级工程师', color: 'orange' },
    Admin: { label: '管理员', color: 'red' },
  };

  const columns = [
    {
      title: '姓名',
      dataIndex: 'name',
      key: 'name',
      width: 120,
      fixed: 'left' as const,
    },
    {
      title: '登录账号',
      dataIndex: 'username',
      key: 'username',
      width: 140,
      render: (username: string) => (
        <Text style={{ fontFamily: 'monospace', color: '#1890ff' }}>{username}</Text>
      ),
    },
    {
      title: '部门',
      dataIndex: 'deptName',
      key: 'deptName',
      width: 150,
      render: (deptName?: string) => deptName || <Text type="secondary">-</Text>,
    },
    {
      title: '上级',
      dataIndex: 'supervisorName',
      key: 'supervisorName',
      width: 120,
      render: (supervisorName?: string) => supervisorName || <Text type="secondary">-</Text>,
    },
    {
      title: '手机号',
      dataIndex: 'mobile',
      key: 'mobile',
      width: 130,
      render: (mobile?: string) => mobile || <Text type="secondary">-</Text>,
    },
    {
      title: '邮箱',
      dataIndex: 'email',
      key: 'email',
      width: 180,
      render: (email?: string) => email || <Text type="secondary">-</Text>,
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
      width: 130,
      render: (role: string) => {
        const roleInfo = roleMap[role] || { label: role, color: 'default' };
        return <Tag color={roleInfo.color}>{roleInfo.label}</Tag>;
      },
    },
    {
      title: '身份证后4位',
      dataIndex: 'idCardLastFour',
      key: 'idCardLastFour',
      width: 120,
      render: (idCardLastFour?: string) => (
        idCardLastFour ? (
          <Text style={{ fontFamily: 'monospace' }}>****{idCardLastFour}</Text>
        ) : (
          <Text type="secondary">-</Text>
        )
      ),
    },
    {
      title: '导入时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (createdAt: string) => new Date(createdAt).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      width: 180,
      fixed: 'right' as const,
      render: (_: any, record: InactivatedUserDto) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => handleViewDetail(record)}
          >
            详情
          </Button>
          <Popconfirm
            title="确认开通此账户？"
            description={`开通后，${record.name} 将可以使用其登录账号和初始密码登录系统`}
            onConfirm={() => handleActivateSingle(record.id, record.name)}
            okText="确认开通"
            cancelText="取消"
          >
            <Button type="primary" size="small" icon={<CheckCircleOutlined />}>
              开通
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const rowSelection = {
    selectedRowKeys,
    onChange: (selectedKeys: React.Key[]) => {
      setSelectedRowKeys(selectedKeys as string[]);
    },
  };

  return (
    <div style={{ padding: 24 }}>
      <Card>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          {/* 页面标题 */}
          <div>
            <Title level={2}>
              <UserAddOutlined /> 账户开通审核
            </Title>
            <Text type="secondary">
              审核并开通通过Excel导入的员工账户，开通后员工即可使用初始密码登录系统
            </Text>
          </div>

          {/* 统计信息 */}
          <Row gutter={16}>
            <Col span={8}>
              <Card>
                <Statistic
                  title="待开通账户"
                  value={total}
                  prefix={<TeamOutlined />}
                  valueStyle={{ color: '#faad14' }}
                />
              </Card>
            </Col>
            <Col span={8}>
              <Card>
                <Statistic
                  title="已选择"
                  value={selectedRowKeys.length}
                  prefix={<CheckCircleOutlined />}
                  valueStyle={{ color: '#1890ff' }}
                />
              </Card>
            </Col>
          </Row>

          {/* 提示信息 */}
          {total > 0 && (
            <Alert
              message="操作提示"
              description={
                <div>
                  <p>📋 以下账户已通过Excel导入创建，但尚未开通</p>
                  <p>✅ 请审核员工信息后，点击"开通"按钮激活账户</p>
                  <p>🔐 开通后，员工可使用<Text strong>登录账号</Text>和<Text strong>初始密码</Text>登录</p>
                  <p>⚠️ 初始密码规则：<Text code>姓名拼音 + 身份证后4位</Text></p>
                </div>
              }
              type="info"
              showIcon
              closable
            />
          )}

          {/* 搜索和批量操作 */}
          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <Search
              placeholder="搜索姓名、账号、部门..."
              allowClear
              onSearch={setSearchQuery}
              style={{ width: 300 }}
              prefix={<SearchOutlined />}
            />
            <Space>
              <Button onClick={handleSelectAll}>
                {selectedRowKeys.length === users.length ? '取消全选' : '全选'}
              </Button>
              <Popconfirm
                title="确认批量开通账户？"
                description={`将开通 ${selectedRowKeys.length} 个账户，这些员工将可以登录系统`}
                onConfirm={handleBatchActivate}
                okText="确认开通"
                cancelText="取消"
                disabled={selectedRowKeys.length === 0}
              >
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  loading={activating}
                  disabled={selectedRowKeys.length === 0}
                >
                  批量开通 ({selectedRowKeys.length})
                </Button>
              </Popconfirm>
            </Space>
          </Space>

          {/* 用户列表 */}
          <Table
            rowSelection={rowSelection}
            columns={columns}
            dataSource={users}
            rowKey="id"
            loading={loading}
            pagination={{
              current: page,
              pageSize,
              total,
              showSizeChanger: true,
              showQuickJumper: true,
              showTotal: (total) => `共 ${total} 条`,
              onChange: (page, pageSize) => {
                setPage(page);
                setPageSize(pageSize);
              },
            }}
            scroll={{ x: 1400 }}
          />
        </Space>
      </Card>

      {/* 用户详情Modal */}
      <Modal
        title="账户详细信息"
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setDetailModalVisible(false)}>
            关闭
          </Button>,
          selectedUser && (
            <Popconfirm
              key="activate"
              title="确认开通此账户？"
              description={`开通后，${selectedUser.name} 将可以使用其登录账号和初始密码登录系统`}
              onConfirm={() => {
                handleActivateSingle(selectedUser.id, selectedUser.name);
                setDetailModalVisible(false);
              }}
              okText="确认开通"
              cancelText="取消"
            >
              <Button type="primary" icon={<CheckCircleOutlined />}>
                开通账户
              </Button>
            </Popconfirm>
          ),
        ]}
        width={700}
      >
        {selectedUser && (
          <Descriptions column={2} bordered>
            <Descriptions.Item label="用户ID" span={2}>
              <Text copyable>{selectedUser.id}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="姓名">
              {selectedUser.name}
            </Descriptions.Item>
            <Descriptions.Item label="登录账号">
              <Text copyable style={{ fontFamily: 'monospace', color: '#1890ff' }}>
                {selectedUser.username}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="初始密码规则" span={2}>
              <Text code>姓名拼音 + 身份证后4位</Text>
              <Text type="secondary"> (例如：zhangsan1234)</Text>
            </Descriptions.Item>
            <Descriptions.Item label="部门">
              {selectedUser.deptName || <Text type="secondary">-</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="上级">
              {selectedUser.supervisorName || <Text type="secondary">-</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="手机号">
              {selectedUser.mobile || <Text type="secondary">-</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="邮箱">
              {selectedUser.email || <Text type="secondary">-</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="角色">
              <Tag color={roleMap[selectedUser.role]?.color || 'default'}>
                {roleMap[selectedUser.role]?.label || selectedUser.role}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="身份证后4位">
              {selectedUser.idCardLastFour ? (
                <Text style={{ fontFamily: 'monospace' }}>****{selectedUser.idCardLastFour}</Text>
              ) : (
                <Text type="secondary">-</Text>
              )}
            </Descriptions.Item>
            <Descriptions.Item label="导入时间" span={2}>
              {new Date(selectedUser.createdAt).toLocaleString('zh-CN')}
            </Descriptions.Item>
            <Descriptions.Item label="账户状态" span={2}>
              <Tag color="orange">未开通（需要管理员开通）</Tag>
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
}
