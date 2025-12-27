import React, { useEffect, useState } from 'react';
import {
  Table,
  Button,
  Space,
  message,
  Modal,
  Form,
  Input,
  Popconfirm,
  Tag,
  Typography,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import { customerService, CustomerDto } from '../../services/customerService';

const { Title } = Typography;
const { Search } = Input;

/**
 * 客户管理页面
 * 提供客户档案的 CRUD 功能
 */
const CustomerList: React.FC = () => {
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [searchQuery, setSearchQuery] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<CustomerDto | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadCustomers();
  }, [page, pageSize, searchQuery]);

  const loadCustomers = async () => {
    setLoading(true);
    try {
      const result = await customerService.getCustomers({
        page,
        pageSize,
        searchQuery,
      });
      setCustomers(result.items);
      setTotal(result.total);
    } catch (error) {
      console.error('Failed to load customers:', error);
      message.error('加载客户列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingCustomer(null);
    form.resetFields();
    setModalVisible(true);
  };

  const handleEdit = (customer: CustomerDto) => {
    setEditingCustomer(customer);
    form.setFieldsValue(customer);
    setModalVisible(true);
  };

  const handleDelete = async (customerId: string) => {
    try {
      await customerService.deleteCustomer(customerId);
      message.success('删除成功');
      loadCustomers();
    } catch (error) {
      console.error('Failed to delete customer:', error);
      message.error('删除失败');
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      if (editingCustomer) {
        // 更新
        await customerService.updateCustomer(editingCustomer.customerId, values);
        message.success('更新成功');
      } else {
        // 创建
        await customerService.createCustomer(values);
        message.success('创建成功');
      }

      setModalVisible(false);
      loadCustomers();
    } catch (error) {
      console.error('Failed to submit:', error);
      message.error(editingCustomer ? '更新失败' : '创建失败');
    }
  };

  const columns = [
    {
      title: '客户编号',
      dataIndex: 'customerCode',
      key: 'customerCode',
      width: 120,
    },
    {
      title: '客户名称',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 200,
    },
    {
      title: '行业',
      dataIndex: 'industry',
      key: 'industry',
      width: 120,
    },
    {
      title: '联系人',
      dataIndex: 'contactPerson',
      key: 'contactPerson',
      width: 100,
    },
    {
      title: '联系电话',
      dataIndex: 'contactPhone',
      key: 'contactPhone',
      width: 130,
    },
    {
      title: '联系邮箱',
      dataIndex: 'contactEmail',
      key: 'contactEmail',
      width: 180,
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 80,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'success' : 'default'}>
          {isActive ? '启用' : '禁用'}
        </Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 150,
      fixed: 'right' as const,
      render: (_: any, record: CustomerDto) => (
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
            onConfirm={() => handleDelete(record.customerId)}
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
          客户管理
        </Title>
        <Space>
          <Search
            placeholder="搜索客户名称或编号"
            allowClear
            style={{ width: 300 }}
            onSearch={setSearchQuery}
            enterButton={<SearchOutlined />}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            新增客户
          </Button>
        </Space>
      </div>

      {/* 客户列表表格 */}
      <Table
        columns={columns}
        dataSource={customers}
        rowKey="customerId"
        loading={loading}
        scroll={{ x: 1300 }}
        pagination={{
          current: page,
          pageSize: pageSize,
          total: total,
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 条`,
          onChange: (page, pageSize) => {
            setPage(page);
            setPageSize(pageSize);
          },
        }}
      />

      {/* 新增/编辑客户对话框 */}
      <Modal
        title={editingCustomer ? '编辑客户' : '新增客户'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={700}
        okText="保存"
        cancelText="取消"
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ isActive: true }}
        >
          <Form.Item
            label="客户编号"
            name="customerCode"
            rules={[{ required: true, message: '请输入客户编号' }]}
          >
            <Input placeholder="如: C001" />
          </Form.Item>

          <Form.Item
            label="客户名称"
            name="customerName"
            rules={[{ required: true, message: '请输入客户名称' }]}
          >
            <Input placeholder="请输入客户名称" />
          </Form.Item>

          <Form.Item label="行业" name="industry">
            <Input placeholder="如: 汽车制造" />
          </Form.Item>

          <Form.Item label="联系人" name="contactPerson">
            <Input placeholder="请输入联系人姓名" />
          </Form.Item>

          <Form.Item
            label="联系电话"
            name="contactPhone"
            rules={[
              { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号' },
            ]}
          >
            <Input placeholder="请输入联系电话" />
          </Form.Item>

          <Form.Item
            label="联系邮箱"
            name="contactEmail"
            rules={[{ type: 'email', message: '请输入正确的邮箱地址' }]}
          >
            <Input placeholder="请输入联系邮箱" />
          </Form.Item>

          <Form.Item label="地址" name="address">
            <Input.TextArea placeholder="请输入客户地址" rows={2} />
          </Form.Item>

          <Form.Item label="备注" name="description">
            <Input.TextArea placeholder="请输入备注信息" rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default CustomerList;
