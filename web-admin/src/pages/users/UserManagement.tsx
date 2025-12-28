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
  CheckCircleOutlined,
  DownloadOutlined,
} from '@ant-design/icons';
import { userManagementService, UserDto, UserRole, CreateUserRequest } from '../../services/userManagementService';
import { employeeImportService } from '../../services/employeeImportService';
import employeeExportService, { EmployeeExportRequest, ExportPreviewResult } from '../../services/employeeExportService';

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
  const [createModalVisible, setCreateModalVisible] = useState(false);
  const [createForm] = Form.useForm<CreateUserRequest>();
  const [exportModalVisible, setExportModalVisible] = useState(false);
  const [exportForm] = Form.useForm<EmployeeExportRequest>();
  const [exportLoading, setExportLoading] = useState(false);
  const [exportPreview, setExportPreview] = useState<ExportPreviewResult | null>(null);

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

  const handleCreate = () => {
    createForm.resetFields();
    createForm.setFieldsValue({ loginType: 'Password' });
    setCreateModalVisible(true);
  };

  const handleCreateSubmit = async () => {
    try {
      const values = await createForm.validateFields();

      await userManagementService.createUser({
        ...values,
        loginType: 'Password',
      });

      message.success('用户创建成功');
      setCreateModalVisible(false);
      createForm.resetFields();
      loadUsers();
    } catch (error) {
      console.error('Failed to create user:', error);
      message.error('用户创建失败');
    }
  };


  const handleExport = () => {
    exportForm.resetFields();
    exportForm.setFieldsValue({ format: 'Excel', includeInactive: false });
    setExportModalVisible(true);
    setExportPreview(null);
  };

  const handleExportPreview = async () => {
    try {
      const values = await exportForm.validateFields();
      setExportLoading(true);
      const preview = await employeeExportService.previewExportStats(values);
      setExportPreview(preview);
    } catch (error: any) {
      console.error('Failed to preview export:', error);
      message.error(error.message || '预览失败');
    } finally {
      setExportLoading(false);
    }
  };

  const handleExportSubmit = async () => {
    try {
      const values = await exportForm.validateFields();
      setExportLoading(true);

      const blob = await employeeExportService.exportEmployees(values);
      const fileName = employeeExportService.generateFileName(values);

      employeeExportService.downloadFile(blob, fileName);

      message.success(`已导出 ${exportPreview?.totalCount || 0} 条记录`);
      setExportModalVisible(false);
      setExportPreview(null);
    } catch (error: any) {
      console.error('Failed to export:', error);
      message.error(error.message || '导出失败');
    } finally {
      setExportLoading(false);
    }
  };
  const columns = [
    {
      title: '姓名',
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
      title: '登录账号',
      dataIndex: 'username',
      key: 'username',
      width: 140,
      render: (username: string | undefined, record: UserDto) => {
        if (record.loginType === 'Password' && username) {
          return <span style={{ fontFamily: 'monospace', color: '#1890ff' }}>{username}</span>;
        }
        return <span style={{ color: '#999' }}>-</span>;
      },
    },
    {
      title: '企业微信ID',
      dataIndex: 'wecomUserId',
      key: 'wecomUserId',
      width: 150,
      render: (wecomUserId: string | undefined) => wecomUserId || <span style={{ color: '#999' }}>-</span>,
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
      title: '开通状态',
      dataIndex: 'isActivated',
      key: 'isActivated',
      width: 120,
      render: (isActivated: boolean | undefined, record: UserDto) => {
        // 企业微信用户默认已开通
        if (record.loginType === 'WeCom') {
          return <Tag color="green">已开通</Tag>;
        }
        // 密码登录用户检查isActivated字段
        if (isActivated === false) {
          return <Tag color="orange">未开通</Tag>;
        }
        return <Tag color="green">已开通</Tag>;
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
      width: 280,
      fixed: 'right' as const,
      render: (_: any, record: UserDto) => (
        <Space size="small">
          {/* 未开通账户显示开通按钮 */}
          {record.loginType === 'Password' && record.isActivated === false && (
            <Popconfirm
              title="确认开通账户？"
              description={`开通后，${record.name} 将可以使用其登录账号和初始密码登录系统`}
              onConfirm={async () => {
                try {
                  const result = await employeeImportService.activateAccount(record.id);
                  if (result.success) {
                    message.success(`已开通 ${record.name} 的账户`);
                    loadUsers();
                  } else {
                    message.error(result.message);
                  }
                } catch (error: any) {
                  message.error(error.message || '开通失败');
                }
              }}
              okText="确认开通"
              cancelText="取消"
            >
              <Button
                type="primary"
                size="small"
                icon={<CheckCircleOutlined />}
              >
                开通
              </Button>
            </Popconfirm>
          )}
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
          <Button
            icon={<DownloadOutlined />}
            onClick={handleExport}
          >
            导出数据
          </Button>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleCreate}
          >
            创建用户
          </Button>
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
        scroll={{ x: 1500 }}
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

      {/* 创建用户对话框 */}
      <Modal
        title="创建新用户"
        open={createModalVisible}
        onOk={handleCreateSubmit}
        onCancel={() => {
          setCreateModalVisible(false);
          createForm.resetFields();
        }}
        width={600}
        okText="创建"
        cancelText="取消"
      >
        <div style={{ marginBottom: 16, padding: 12, background: '#e6f7ff', border: '1px solid #91d5ff', borderRadius: 4 }}>
          <div style={{ fontSize: 12, color: '#666' }}>
            创建使用密码登录的新用户。用户首次登录时需要修改密码。
          </div>
        </div>

        <Form
          form={createForm}
          layout="vertical"
        >
          <Form.Item
            label="用户名"
            name="username"
            rules={[
              { required: true, message: '请输入用户名' },
              { min: 3, message: '用户名至少3个字符' },
              { max: 50, message: '用户名最多50个字符' },
              { pattern: /^[a-zA-Z0-9_]+$/, message: '用户名只能包含字母、数字和下划线' },
            ]}
            extra="用于登录系统，只能包含字母、数字和下划线"
          >
            <Input
              placeholder="请输入用户名"
              prefix={<UserOutlined />}
              autoComplete="off"
            />
          </Form.Item>

          <Form.Item
            label="姓名"
            name="name"
            rules={[
              { required: true, message: '请输入姓名' },
              { max: 100, message: '姓名最多100个字符' },
            ]}
          >
            <Input
              placeholder="请输入姓名"
              prefix={<UserOutlined />}
            />
          </Form.Item>

          <Form.Item
            label="初始密码"
            name="password"
            rules={[
              { required: true, message: '请输入初始密码' },
              { min: 8, message: '密码至少8个字符' },
              {
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{8,}$/,
                message: '密码必须包含大写字母、小写字母和数字，至少8位',
              },
            ]}
            extra="用户首次登录时必须修改此密码"
          >
            <Input.Password
              placeholder="请输入初始密码"
              prefix={<KeyOutlined />}
              autoComplete="new-password"
            />
          </Form.Item>

          <Form.Item
            label="确认密码"
            name="confirmPassword"
            dependencies={['password']}
            rules={[
              { required: true, message: '请再次输入密码' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('两次输入的密码不一致'));
                },
              }),
            ]}
          >
            <Input.Password
              placeholder="请再次输入密码"
              prefix={<KeyOutlined />}
              autoComplete="new-password"
            />
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

          <Form.Item
            label="邮箱"
            name="email"
            rules={[
              { type: 'email', message: '请输入有效的邮箱地址' },
            ]}
          >
            <Input
              placeholder="请输入邮箱（可选）"
              type="email"
            />
          </Form.Item>

          <Form.Item
            label="手机号"
            name="mobile"
            rules={[
              { pattern: /^1[3-9]\d{9}$/, message: '请输入有效的手机号' },
            ]}
          >
            <Input
              placeholder="请输入手机号（可选）"
              maxLength={11}
            />
          </Form.Item>
        </Form>
      </Modal>

      {/* 导出数据对话框 */}
      <Modal
        title="导出员工数据"
        open={exportModalVisible}
        onOk={handleExportSubmit}
        onCancel={() => {
          setExportModalVisible(false);
          setExportPreview(null);
        }}
        width={700}
        okText="确认导出"
        cancelText="取消"
        confirmLoading={exportLoading}
        okButtonProps={{ disabled: !exportPreview }}
      >
        <div style={{ marginBottom: 16, padding: 12, background: '#e6f7ff', border: '1px solid #91d5ff', borderRadius: 4 }}>
          <div style={{ fontSize: 12, color: '#666' }}>
            导出员工数据为Excel或CSV格式。可以选择筛选条件导出部分数据。
          </div>
        </div>

        <Form
          form={exportForm}
          layout="vertical"
          onValuesChange={handleExportPreview}
        >
          <Form.Item
            label="导出格式"
            name="format"
            rules={[{ required: true, message: '请选择导出格式' }]}
            initialValue="Excel"
          >
            <Select>
              <Option value="Excel">Excel格式 (.xlsx)</Option>
              <Option value="CSV">CSV格式 (.csv)</Option>
            </Select>
          </Form.Item>

          <Form.Item label="部门筛选" name="deptName">
            <Input placeholder="留空表示不限制部门" allowClear />
          </Form.Item>

          <Form.Item label="角色筛选" name="role">
            <Select placeholder="留空表示不限制角色" allowClear>
              {Object.entries(roleMap).map(([key, value]) => (
                <Option key={key} value={key}>
                  <Tag color={value.color}>{value.label}</Tag>
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item label="开通状态" name="isActivated">
            <Select placeholder="留空表示不限制开通状态" allowClear>
              <Option value={true}>已开通</Option>
              <Option value={false}>未开通</Option>
            </Select>
          </Form.Item>

          <Form.Item label="登录方式" name="loginType">
            <Select placeholder="留空表示不限制登录方式" allowClear>
              <Option value="Password">密码登录</Option>
              <Option value="WeCom">企业微信</Option>
            </Select>
          </Form.Item>

          <Form.Item name="includeInactive" valuePropName="checked" initialValue={false}>
            <Checkbox>包含已停用账户</Checkbox>
          </Form.Item>
        </Form>

        {/* 导出预览统计 */}
        {exportPreview && (
          <div style={{ marginTop: 16, padding: 16, background: '#f6ffed', border: '1px solid #b7eb8f', borderRadius: 4 }}>
            <div style={{ fontWeight: 500, marginBottom: 12, fontSize: 14 }}>导出预览</div>
            <Space direction="vertical" size={8} style={{ width: '100%' }}>
              <div>
                <span style={{ color: '#666' }}>总记录数：</span>
                <span style={{ fontWeight: 500, fontSize: 16, color: '#1890ff' }}>{exportPreview.totalCount}</span> 条
              </div>
              <div>
                <span style={{ color: '#666' }}>已开通：</span>
                <Tag color="green">{exportPreview.activatedCount}</Tag>
                <span style={{ color: '#666', marginLeft: 16 }}>未开通：</span>
                <Tag color="orange">{exportPreview.inactivatedCount}</Tag>
              </div>

              {exportPreview.departmentStats.length > 0 && (
                <div>
                  <div style={{ color: '#666', marginBottom: 4 }}>部门分布：</div>
                  <Space wrap>
                    {exportPreview.departmentStats.slice(0, 5).map(dept => (
                      <Tag key={dept.deptName}>{dept.deptName} ({dept.count})</Tag>
                    ))}
                    {exportPreview.departmentStats.length > 5 && (
                      <span style={{ color: '#999', fontSize: 12 }}>等{exportPreview.departmentStats.length}个部门</span>
                    )}
                  </Space>
                </div>
              )}

              {exportPreview.roleStats.length > 0 && (
                <div>
                  <div style={{ color: '#666', marginBottom: 4 }}>角色分布：</div>
                  <Space wrap>
                    {exportPreview.roleStats.map(role => {
                      const roleInfo = roleMap[role.role] || { label: role.role, color: 'default' };
                      return (
                        <Tag key={role.role} color={roleInfo.color}>
                          {roleInfo.label} ({role.count})
                        </Tag>
                      );
                    })}
                  </Space>
                </div>
              )}
            </Space>
          </div>
        )}

        {exportLoading && !exportPreview && (
          <div style={{ textAlign: 'center', padding: 20, color: '#999' }}>
            加载预览数据中...
          </div>
        )}
      </Modal>
    </div>
  );
};

export default UserManagement;
