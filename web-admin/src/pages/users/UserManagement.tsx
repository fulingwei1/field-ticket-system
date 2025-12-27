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
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined,
  UserOutlined,
} from '@ant-design/icons';

const { Title } = Typography;
const { Search } = Input;
const { Option } = Select;

interface UserDto {
  id: string;
  corpId: string;
  wecomUserId: string;
  name: string;
  mobile?: string;
  deptId?: string;
  deptName?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

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
      // TODO: 调用实际的 API
      // const result = await userService.getUsers({
      //   page,
      //   pageSize,
      //   searchQuery,
      // });
      // setUsers(result.items);
      // setTotal(result.total);

      // 模拟数据
      const mockUsers: UserDto[] = [
        {
          id: '1',
          corpId: 'corp123',
          wecomUserId: 'user001',
          name: '张三',
          mobile: '13800138000',
          deptId: 'dept1',
          deptName: '研发部',
          role: 'SeniorEngineer',
          isActive: true,
          createdAt: '2024-01-01',
          updatedAt: '2024-01-01',
        },
        {
          id: '2',
          corpId: 'corp123',
          wecomUserId: 'user002',
          name: '李四',
          mobile: '13900139000',
          deptId: 'dept2',
          deptName: '客服部',
          role: 'CS',
          isActive: true,
          createdAt: '2024-01-02',
          updatedAt: '2024-01-02',
        },
        {
          id: '3',
          corpId: 'corp123',
          wecomUserId: 'user003',
          name: '王五',
          mobile: '13700137000',
          deptId: 'dept1',
          deptName: '研发部',
          role: 'FieldEngineer',
          isActive: true,
          createdAt: '2024-01-03',
          updatedAt: '2024-01-03',
        },
      ];
      setUsers(mockUsers);
      setTotal(mockUsers.length);
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
      // TODO: 调用实际的 API
      // await userService.deleteUser(userId);
      message.success('删除成功');
      loadUsers();
    } catch (error) {
      console.error('Failed to delete user:', error);
      message.error('删除失败');
    }
  };

  const handleToggleStatus = async (userId: string, isActive: boolean) => {
    try {
      // TODO: 调用实际的 API
      // await userService.updateUserStatus(userId, isActive);
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
        // await userService.updateUser(editingUser.id, values);
        message.success('更新成功');
      }

      setModalVisible(false);
      loadUsers();
    } catch (error) {
      console.error('Failed to submit:', error);
      message.error('更新失败');
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
      width: 150,
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
        scroll={{ x: 1200 }}
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
    </div>
  );
};

export default UserManagement;
