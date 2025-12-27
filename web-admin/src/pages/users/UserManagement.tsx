import React, { useEffect, useState } from 'react';
import {
  Table,
  Button,
  Space,
  message,
  Modal,
  Form,
  Input,
  Select,
  Popconfirm,
  Tag,
  Typography,
  Switch,
  Checkbox,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined,
  UserOutlined,
  KeyOutlined,
} from '@ant-design/icons';
import { userManagementService, UserDto, UserRole } from '../../services/userManagementService';

const { Title } = Typography;
const { Search } = Input;
const { Option } = Select;

/**
 * 用户管理页面
 * 提供用户列表、角色分配、启用/禁用等功能
 */
const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [searchQuery, setSearchQuery] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [form] = Form.useForm();
  const [resetPasswordModalVisible, setResetPasswordModalVisible] = useState(false);
  const [resetPasswordUser, setResetPasswordUser] = useState<UserDto | null>(null);
  const [resetPasswordForm] = Form.useForm();

  const roleMap: Record<string, { label: string; color: string; description: string }> = {
    FieldEngineer: { label: '现场工程师', color: 'blue', description: '创建工单、上传证据、验证' },
    CS: { label: '客服', color: 'green', description: '创建工单、客户沟通' },
    SeniorEngineer: { label: '高级工程师', color: 'orange', description: '分诊、创建解决方案' },
    Admin: { label: '管理员', color: 'red', description: '系统管理、所有权限' },
  };

  useEffect(() => {
    loadUsers();
  }, [page, pageSize, searchQuery]);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const result = await userManagementService.getUsers({
        page,
        pageSize,
        searchQuery,
      });
      setUsers(result.items);
      setTotal(result.total);
    } catch (error) {
      console.error('Failed to load users:', error);
      message.error('加载用户列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = (user: UserDto) => {
    setEditingUser(user);
    form.setFieldsValue(user);
    setModalVisible(true);
  };

  const handleDelete = async (userId: string) => {
    try {
      await userManagementService.deleteUser(userId);
      message.success('删除成功');
      loadUsers();
    } catch (error) {
      console.error('Failed to delete user:', error);
      message.error('删除失败');
    }
  };

  const handleToggleStatus = async (userId: string, isActive: boolean) => {
    try {
      await userManagementService.updateUserStatus(userId, isActive);
      message.success(isActive ? '已启用' : '已禁用');
      loadUsers();
    } catch (error) {
      console.error('Failed to toggle status:', error);
      message.error('操作失败');
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      if (editingUser) {
        // 更新
        await userManagementService.updateUser(editingUser.id, values);
        message.success('更新成功');
      }

      setModalVisible(false);
      loadUsers();
    } catch (error) {
      console.error('Failed to submit:', error);
      message.error('更新失败');
    }
  };

  const handleResetPassword = (user: UserDto) => {
    setResetPasswordUser(user);
    resetPasswordForm.resetFields();
    resetPasswordForm.setFieldsValue({ mustChangePassword: true });
    setResetPasswordModalVisible(true);
  };

  const handleResetPasswordSubmit = async () => {
    try {
      const values = await resetPasswordForm.validateFields();

      if (!resetPasswordUser) return;

      await userManagementService.resetPassword(
        resetPasswordUser.id,
        values.newPassword,
        values.mustChangePassword ?? true
      );

      message.success('密码重置成功');
      setResetPasswordModalVisible(false);
      resetPasswordForm.resetFields();
      loadUsers();
    } catch (error) {
      console.error('Failed to reset password:', error);
      message.error('密码重置失败');
    }
  };

  const columns = [
    {
      title: '用户名',
      dataIndex: 'name',
      key: 'name',
      width: 120,
      fixed: 'left' as const,
      render: (name: string) => (
        <Space>
          <UserOutlined />
          <span>{name}</span>
        </Space>
      ),
    },
    {
      title: '企业微信ID',
      dataIndex: 'wecomUserId',
      key: 'wecomUserId',
      width: 150,
    },
    {
      title: '手机号',
      dataIndex: 'mobile',
      key: 'mobile',
      width: 130,
    },
    {
      title: '部门',
      dataIndex: 'deptName',
      key: 'deptName',
      width: 120,
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
      width: 150,
      render: (role: string) => {
        const roleInfo = roleMap[role] || { label: role, color: 'default', description: '' };
        return (
          <Tag color={roleInfo.color} title={roleInfo.description}>
            {roleInfo.label}
          </Tag>
        );
      },
    },
    {
      title: '登录方式',
      dataIndex: 'loginType',
      key: 'loginType',
      width: 120,
      render: (loginType: string) => {
        if (loginType === 'Password') {
          return <Tag color="blue">密码登录</Tag>;
        }
        return <Tag color="green">企业微信</Tag>;
      },
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 100,
      render: (isActive: boolean, record: UserDto) => (
        <Switch
          checked={isActive}
          checkedChildren="启用"
          unCheckedChildren="禁用"
          onChange={(checked) => handleToggleStatus(record.id, checked)}
        />
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      width: 220,
      fixed: 'right' as const,
      render: (_: any, record: UserDto) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
          {record.loginType === 'Password' && (
            <Button
              type="link"
              size="small"
              icon={<KeyOutlined />}
              onClick={() => handleResetPassword(record)}
            >
              重置密码
            </Button>
          )}
          <Popconfirm
            title="确认删除?"
            description="删除后将无法恢复，确定要删除吗？"
            onConfirm={() => handleDelete(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
            >
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      {/* 页面头部 */}
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2} style={{ margin: 0 }}>
          用户管理
        </Title>
        <Space>
          <Search
            placeholder="搜索用户名或手机号"
            allowClear
            style={{ width: 300 }}
            onSearch={setSearchQuery}
            enterButton={<SearchOutlined />}
          />
        </Space>
      </div>

      {/* 角色说明卡片 */}
      <div style={{ marginBottom: 16, padding: 12, background: '#f0f2f5', borderRadius: 4 }}>
        <Space size="large">
          {Object.entries(roleMap).map(([key, value]) => (
            <div key={key}>
              <Tag color={value.color}>{value.label}</Tag>
              <span style={{ fontSize: 12, color: '#666' }}>{value.description}</span>
            </div>
          ))}
        </Space>
      </div>

      {/* 用户列表表格 */}
      <Table
        columns={columns}
        dataSource={users}
        rowKey="id"
        loading={loading}
        scroll={{ x: 1400 }}
        pagination={{
          current: page,
          pageSize: pageSize,
          total: total,
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 个用户`,
          onChange: (page, pageSize) => {
            setPage(page);
            setPageSize(pageSize);
          },
        }}
      />

      {/* 编辑用户对话框 */}
      <Modal
        title="编辑用户"
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={600}
        okText="保存"
        cancelText="取消"
      >
        <Form
          form={form}
          layout="vertical"
        >
          <Form.Item label="用户名">
            <Input disabled value={editingUser?.name} />
          </Form.Item>

          <Form.Item label="企业微信ID">
            <Input disabled value={editingUser?.wecomUserId} />
          </Form.Item>

          <Form.Item
            label="角色"
            name="role"
            rules={[{ required: true, message: '请选择角色' }]}
          >
            <Select placeholder="请选择角色">
              {Object.entries(roleMap).map(([key, value]) => (
                <Option key={key} value={key}>
                  <Space>
                    <Tag color={value.color}>{value.label}</Tag>
                    <span style={{ fontSize: 12, color: '#666' }}>{value.description}</span>
                  </Space>
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item label="状态" name="isActive" valuePropName="checked">
            <Switch checkedChildren="启用" unCheckedChildren="禁用" />
          </Form.Item>
        </Form>
      </Modal>

      {/* 重置密码对话框 */}
      <Modal
        title="重置用户密码"
        open={resetPasswordModalVisible}
        onOk={handleResetPasswordSubmit}
        onCancel={() => {
          setResetPasswordModalVisible(false);
          resetPasswordForm.resetFields();
        }}
        width={500}
        okText="确认重置"
        cancelText="取消"
        okButtonProps={{ danger: true }}
      >
        <div style={{ marginBottom: 16, padding: 12, background: '#fff7e6', border: '1px solid #ffd591', borderRadius: 4 }}>
          <Space direction="vertical" size={4}>
            <div style={{ fontWeight: 500 }}>
              <UserOutlined /> 重置用户: {resetPasswordUser?.name} ({resetPasswordUser?.username})
            </div>
            <div style={{ fontSize: 12, color: '#666' }}>
              请为该用户设置新密码。建议启用"首次登录强制修改"以提升安全性。
            </div>
          </Space>
        </div>

        <Form
          form={resetPasswordForm}
          layout="vertical"
        >
          <Form.Item
            label="新密码"
            name="newPassword"
            rules={[
              { required: true, message: '请输入新密码' },
              { min: 8, message: '密码至少8个字符' },
              {
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{8,}$/,
                message: '密码必须包含大写字母、小写字母和数字，至少8位',
              },
            ]}
            extra="密码要求：至少8位，包含大写字母、小写字母和数字"
          >
            <Input.Password
              placeholder="请输入新密码"
              prefix={<KeyOutlined />}
              autoComplete="new-password"
            />
          </Form.Item>

          <Form.Item
            label="确认密码"
            name="confirmPassword"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: '请再次输入新密码' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('两次输入的密码不一致'));
                },
              }),
            ]}
          >
            <Input.Password
              placeholder="请再次输入新密码"
              prefix={<KeyOutlined />}
              autoComplete="new-password"
            />
          </Form.Item>

          <Form.Item
            name="mustChangePassword"
            valuePropName="checked"
            initialValue={true}
          >
            <Checkbox>首次登录强制修改密码（推荐）</Checkbox>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default UserManagement;
