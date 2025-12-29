"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Input,
  Select,
  Tag,
  message,
  Modal,
  Form,
  Popconfirm,
  Switch,
} from 'antd';
import {
  SearchOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  ReloadOutlined,
  UserOutlined,
  LockOutlined,
} from '@ant-design/icons';
import {
  userManagementService,
  UserListItemDto,
  UserDetailDto,
  CreateUserRequest,
  UpdateUserRequest,
} from '../../services/userManagementService';
import dayjs from 'dayjs';

const { Option } = Select;

const ROLE_OPTIONS = [
  { value: 'FieldEngineer', label: '现场工程师' },
  { value: 'mechanical_manager', label: '机械部经理' },
  { value: 'electrical_manager', label: '电气部经理' },
  { value: 'software_manager', label: '软件部经理' },
  { value: 'Manager', label: '经理' },
  { value: 'Admin', label: '管理员' },
];

export default function UserManagement() {
  const [loading, setLoading] = useState(false);
  const [users, setUsers] = useState<UserListItemDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // 筛选条件
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string | undefined>();
  const [isActiveFilter, setIsActiveFilter] = useState<boolean | undefined>();

  // 模态框状态
  const [createModalVisible, setCreateModalVisible] = useState(false);
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [resetPasswordModalVisible, setResetPasswordModalVisible] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserDetailDto | null>(null);
  const [createForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const [passwordForm] = Form.useForm();

  useEffect(() => {
    loadUsers();
  }, [page, pageSize, search, roleFilter, isActiveFilter]);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const result = await userManagementService.getUsers({
        search: search || undefined,
        role: roleFilter,
        isActive: isActiveFilter,
        page,
        pageSize,
      });
      setUsers(result.items);
      setTotal(result.total);
    } catch (error: any) {
      message.error('加载用户列表失败：' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (values: CreateUserRequest) => {
    try {
      await userManagementService.createUser(values);
      message.success('用户创建成功');
      setCreateModalVisible(false);
      createForm.resetFields();
      loadUsers();
    } catch (error: any) {
      message.error('创建用户失败：' + error.message);
    }
  };

  const handleEdit = async (userId: string, values: UpdateUserRequest) => {
    try {
      await userManagementService.updateUser(userId, values);
      message.success('用户更新成功');
      setEditModalVisible(false);
      editForm.resetFields();
      setSelectedUser(null);
      loadUsers();
    } catch (error: any) {
      message.error('更新用户失败：' + error.message);
    }
  };

  const handleDelete = async (userId: string) => {
    try {
      await userManagementService.deleteUser(userId);
      message.success('用户已删除');
      loadUsers();
    } catch (error: any) {
      message.error('删除用户失败：' + error.message);
    }
  };

  const handleResetPassword = async (values: { newPassword: string }) => {
    if (!selectedUser) return;
    try {
      await userManagementService.resetPassword(selectedUser.id, values.newPassword);
      message.success('密码重置成功');
      setResetPasswordModalVisible(false);
      passwordForm.resetFields();
      setSelectedUser(null);
    } catch (error: any) {
      message.error('重置密码失败：' + error.message);
    }
  };

  const openEditModal = async (userId: string) => {
    try {
      const user = await userManagementService.getUser(userId);
      setSelectedUser(user);
      editForm.setFieldsValue({
        name: user.name,
        username: user.username,
        mobile: user.mobile,
        deptId: user.deptId,
        role: user.role,
        isActive: user.isActive,
        weComUserId: user.weComUserId,
      });
      setEditModalVisible(true);
    } catch (error: any) {
      message.error('加载用户信息失败：' + error.message);
    }
  };

  const openResetPasswordModal = (userId: string) => {
    setSelectedUser({ id: userId } as UserDetailDto);
    setResetPasswordModalVisible(true);
  };

  const getRoleColor = (role: string) => {
    const roleMap: Record<string, string> = {
      Admin: 'red',
      Manager: 'orange',
      mechanical_manager: 'blue',
      electrical_manager: 'green',
      software_manager: 'purple',
      FieldEngineer: 'default',
    };
    return roleMap[role] || 'default';
  };

  const getRoleLabel = (role: string) => {
    const roleOption = ROLE_OPTIONS.find((r) => r.value === role);
    return roleOption?.label || role;
  };

  const columns = [
    {
      title: '姓名',
      dataIndex: 'name',
      key: 'name',
      width: 120,
    },
    {
      title: '用户名',
      dataIndex: 'username',
      key: 'username',
      width: 120,
    },
    {
      title: '手机号',
      dataIndex: 'mobile',
      key: 'mobile',
      width: 120,
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
      width: 120,
      render: (role: string) => (
        <Tag color={getRoleColor(role)}>{getRoleLabel(role)}</Tag>
      ),
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 80,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'success' : 'error'}>
          {isActive ? '活跃' : '禁用'}
        </Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      fixed: 'right' as const,
      render: (_: any, record: UserListItemDto) => (
        <Space size="small">
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => openEditModal(record.id)}
          >
            编辑
          </Button>
          <Button
            type="link"
            size="small"
            icon={<LockOutlined />}
            onClick={() => openResetPasswordModal(record.id)}
          >
            重置密码
          </Button>
          <Popconfirm
            title="确定要删除此用户吗？"
            description="删除后用户将被禁用，无法登录系统。"
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
    <div style={{ padding: '24px' }}>
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          {/* 搜索和筛选栏 */}
          <Space wrap>
            <Input
              placeholder="搜索用户（姓名、用户名、手机号）"
              prefix={<SearchOutlined />}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ width: 300 }}
              allowClear
            />
            <Select
              placeholder="选择角色"
              value={roleFilter}
              onChange={setRoleFilter}
              style={{ width: 150 }}
              allowClear
            >
              {ROLE_OPTIONS.map((role) => (
                <Option key={role.value} value={role.value}>
                  {role.label}
                </Option>
              ))}
            </Select>
            <Select
              placeholder="选择状态"
              value={isActiveFilter}
              onChange={setIsActiveFilter}
              style={{ width: 120 }}
              allowClear
            >
              <Option value={true}>活跃</Option>
              <Option value={false}>禁用</Option>
            </Select>
            <Button
              icon={<ReloadOutlined />}
              onClick={loadUsers}
            >
              刷新
            </Button>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setCreateModalVisible(true)}
            >
              创建用户
            </Button>
          </Space>

          {/* 用户列表 */}
          <Table
            columns={columns}
            dataSource={users}
            rowKey="id"
            loading={loading}
            pagination={{
              current: page,
              pageSize: pageSize,
              total: total,
              showSizeChanger: true,
              showTotal: (total) => `共 ${total} 条`,
              onChange: (page, pageSize) => {
                setPage(page);
                setPageSize(pageSize);
              },
            }}
            scroll={{ x: 1200 }}
          />
        </Space>
      </Card>

      {/* 创建用户模态框 */}
      <Modal
        title="创建用户"
        open={createModalVisible}
        onCancel={() => {
          setCreateModalVisible(false);
          createForm.resetFields();
        }}
        footer={null}
        width={600}
      >
        <Form
          form={createForm}
          layout="vertical"
          onFinish={handleCreate}
          initialValues={{
            role: 'FieldEngineer',
            isActive: true,
          }}
        >
          <Form.Item
            name="name"
            label="姓名"
            rules={[{ required: true, message: '请输入姓名' }]}
          >
            <Input placeholder="请输入姓名" />
          </Form.Item>
          <Form.Item
            name="username"
            label="用户名"
            rules={[{ required: true, message: '请输入用户名' }]}
          >
            <Input placeholder="请输入用户名" />
          </Form.Item>
          <Form.Item
            name="password"
            label="密码"
            rules={[{ required: true, message: '请输入密码' }]}
          >
            <Input.Password placeholder="请输入密码" />
          </Form.Item>
          <Form.Item name="mobile" label="手机号">
            <Input placeholder="请输入手机号" />
          </Form.Item>
          <Form.Item name="deptId" label="部门ID">
            <Input placeholder="请输入部门ID" />
          </Form.Item>
          <Form.Item
            name="role"
            label="角色"
            rules={[{ required: true, message: '请选择角色' }]}
          >
            <Select placeholder="请选择角色">
              {ROLE_OPTIONS.map((role) => (
                <Option key={role.value} value={role.value}>
                  {role.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="weComUserId" label="企业微信用户ID">
            <Input placeholder="请输入企业微信用户ID" />
          </Form.Item>
          <Form.Item name="isActive" label="状态" valuePropName="checked">
            <Switch checkedChildren="活跃" unCheckedChildren="禁用" />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                创建
              </Button>
              <Button
                onClick={() => {
                  setCreateModalVisible(false);
                  createForm.resetFields();
                }}
              >
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* 编辑用户模态框 */}
      <Modal
        title="编辑用户"
        open={editModalVisible}
        onCancel={() => {
          setEditModalVisible(false);
          editForm.resetFields();
          setSelectedUser(null);
        }}
        footer={null}
        width={600}
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={(values) => {
            if (selectedUser) {
              handleEdit(selectedUser.id, values);
            }
          }}
        >
          <Form.Item name="name" label="姓名">
            <Input placeholder="请输入姓名" />
          </Form.Item>
          <Form.Item name="username" label="用户名">
            <Input placeholder="请输入用户名" />
          </Form.Item>
          <Form.Item name="password" label="密码（留空则不修改）">
            <Input.Password placeholder="如需修改密码，请输入新密码" />
          </Form.Item>
          <Form.Item name="mobile" label="手机号">
            <Input placeholder="请输入手机号" />
          </Form.Item>
          <Form.Item name="deptId" label="部门ID">
            <Input placeholder="请输入部门ID" />
          </Form.Item>
          <Form.Item name="role" label="角色">
            <Select placeholder="请选择角色">
              {ROLE_OPTIONS.map((role) => (
                <Option key={role.value} value={role.value}>
                  {role.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="weComUserId" label="企业微信用户ID">
            <Input placeholder="请输入企业微信用户ID" />
          </Form.Item>
          <Form.Item name="isActive" label="状态" valuePropName="checked">
            <Switch checkedChildren="活跃" unCheckedChildren="禁用" />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                保存
              </Button>
              <Button
                onClick={() => {
                  setEditModalVisible(false);
                  editForm.resetFields();
                  setSelectedUser(null);
                }}
              >
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* 重置密码模态框 */}
      <Modal
        title="重置密码"
        open={resetPasswordModalVisible}
        onCancel={() => {
          setResetPasswordModalVisible(false);
          passwordForm.resetFields();
          setSelectedUser(null);
        }}
        footer={null}
        width={400}
      >
        <Form
          form={passwordForm}
          layout="vertical"
          onFinish={handleResetPassword}
        >
          <Form.Item
            name="newPassword"
            label="新密码"
            rules={[
              { required: true, message: '请输入新密码' },
              { min: 6, message: '密码长度至少6位' },
            ]}
          >
            <Input.Password placeholder="请输入新密码" />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                确定
              </Button>
              <Button
                onClick={() => {
                  setResetPasswordModalVisible(false);
                  passwordForm.resetFields();
                  setSelectedUser(null);
                }}
              >
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}





