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
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined,
  QrcodeOutlined,
  HistoryOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { deviceService, DeviceDto } from '../../services/deviceService';

const { Title } = Typography;
const { Search } = Input;
const { Option } = Select;

/**
 * 设备管理页面
 * 提供设备档案的 CRUD 功能、二维码生成、配置快照查看等
 */
const DeviceList: React.FC = () => {
  const [devices, setDevices] = useState<DeviceDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [searchQuery, setSearchQuery] = useState('');
  const [modalVisible, setModalVisible] = useState(false);
  const [editingDevice, setEditingDevice] = useState<DeviceDto | null>(null);
  const [form] = Form.useForm();
  const navigate = useNavigate();

  useEffect(() => {
    loadDevices();
  }, [page, pageSize, searchQuery]);

  const loadDevices = async () => {
    setLoading(true);
    try {
      const result = await deviceService.getDevices({
        page,
        pageSize,
        searchQuery,
      });
      setDevices(result.items);
      setTotal(result.total);
    } catch (error) {
      console.error('Failed to load devices:', error);
      message.error('加载设备列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingDevice(null);
    form.resetFields();
    setModalVisible(true);
  };

  const handleEdit = (device: DeviceDto) => {
    setEditingDevice(device);
    form.setFieldsValue(device);
    setModalVisible(true);
  };

  const handleDelete = async (deviceId: string) => {
    try {
      await deviceService.deleteDevice(deviceId);
      message.success('删除成功');
      loadDevices();
    } catch (error) {
      console.error('Failed to delete device:', error);
      message.error('删除失败');
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      if (editingDevice) {
        // 更新
        await deviceService.updateDevice(editingDevice.deviceId, values);
        message.success('更新成功');
      } else {
        // 创建
        await deviceService.createDevice(values);
        message.success('创建成功');
      }

      setModalVisible(false);
      loadDevices();
    } catch (error) {
      console.error('Failed to submit:', error);
      message.error(editingDevice ? '更新失败' : '创建失败');
    }
  };

  const handleGenerateQRCode = (deviceId: string) => {
    navigate(`/devices/qrcode-generator?deviceId=${deviceId}`);
  };

  const handleViewSnapshot = (deviceId: string) => {
    navigate(`/devices/config-snapshot?deviceId=${deviceId}`);
  };

  const columns = [
    {
      title: '设备序列号',
      dataIndex: 'deviceSn',
      key: 'deviceSn',
      width: 150,
      fixed: 'left' as const,
    },
    {
      title: '设备名称',
      dataIndex: 'deviceName',
      key: 'deviceName',
      width: 180,
    },
    {
      title: '设备型号',
      dataIndex: 'deviceModel',
      key: 'deviceModel',
      width: 120,
    },
    {
      title: '所属客户',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 150,
    },
    {
      title: '所属项目',
      dataIndex: 'projectName',
      key: 'projectName',
      width: 150,
    },
    {
      title: 'PLC版本',
      dataIndex: 'plcVersion',
      key: 'plcVersion',
      width: 100,
      render: (version: string) => (
        <Tag color="blue">{version || '-'}</Tag>
      ),
    },
    {
      title: 'UI版本',
      dataIndex: 'uiVersion',
      key: 'uiVersion',
      width: 100,
      render: (version: string) => (
        <Tag color="green">{version || '-'}</Tag>
      ),
    },
    {
      title: 'HW版本',
      dataIndex: 'hwVersion',
      key: 'hwVersion',
      width: 100,
      render: (version: string) => (
        <Tag color="orange">{version || '-'}</Tag>
      ),
    },
    {
      title: '安装位置',
      dataIndex: 'location',
      key: 'location',
      width: 120,
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 80,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'success' : 'default'}>
          {isActive ? '在用' : '停用'}
        </Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 240,
      fixed: 'right' as const,
      render: (_: any, record: DeviceDto) => (
        <Space size="small">
          <Tooltip title="生成二维码">
            <Button
              type="link"
              size="small"
              icon={<QrcodeOutlined />}
              onClick={() => handleGenerateQRCode(record.deviceId)}
            />
          </Tooltip>
          <Tooltip title="配置快照">
            <Button
              type="link"
              size="small"
              icon={<HistoryOutlined />}
              onClick={() => handleViewSnapshot(record.deviceId)}
            />
          </Tooltip>
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
            onConfirm={() => handleDelete(record.deviceId)}
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
          设备管理
        </Title>
        <Space>
          <Search
            placeholder="搜索设备名称或序列号"
            allowClear
            style={{ width: 300 }}
            onSearch={setSearchQuery}
            enterButton={<SearchOutlined />}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            新增设备
          </Button>
        </Space>
      </div>

      {/* 设备列表表格 */}
      <Table
        columns={columns}
        dataSource={devices}
        rowKey="deviceId"
        loading={loading}
        scroll={{ x: 1800 }}
        pagination={{
          current: page,
          pageSize: pageSize,
          total: total,
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 台设备`,
          onChange: (page, pageSize) => {
            setPage(page);
            setPageSize(pageSize);
          },
        }}
      />

      {/* 新增/编辑设备对话框 */}
      <Modal
        title={editingDevice ? '编辑设备' : '新增设备'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={800}
        okText="保存"
        cancelText="取消"
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ isActive: true }}
        >
          <Form.Item
            label="设备序列号"
            name="deviceSn"
            rules={[{ required: true, message: '请输入设备序列号' }]}
          >
            <Input placeholder="如: DEV-2024-001" />
          </Form.Item>

          <Form.Item
            label="设备名称"
            name="deviceName"
            rules={[{ required: true, message: '请输入设备名称' }]}
          >
            <Input placeholder="如: 自动化产线A-1" />
          </Form.Item>

          <Form.Item label="设备型号" name="deviceModel">
            <Input placeholder="如: Model-X100" />
          </Form.Item>

          <Form.Item
            label="所属客户"
            name="customerId"
            rules={[{ required: true, message: '请选择所属客户' }]}
          >
            <Select placeholder="请选择客户">
              <Option value="1">示例客户A</Option>
              {/* TODO: 从 API 加载客户列表 */}
            </Select>
          </Form.Item>

          <Form.Item label="所属项目" name="projectId">
            <Select placeholder="请选择项目" allowClear>
              <Option value="1">项目A</Option>
              {/* TODO: 从 API 加载项目列表 */}
            </Select>
          </Form.Item>

          <Form.Item label="PLC版本" name="plcVersion">
            <Input placeholder="如: v2.1.0" />
          </Form.Item>

          <Form.Item label="UI版本" name="uiVersion">
            <Input placeholder="如: v1.5.2" />
          </Form.Item>

          <Form.Item label="硬件版本" name="hwVersion">
            <Input placeholder="如: v3.0.0" />
          </Form.Item>

          <Form.Item label="安装位置" name="location">
            <Input placeholder="如: 车间1-A区" />
          </Form.Item>

          <Form.Item label="安装日期" name="installDate">
            <Input type="date" />
          </Form.Item>

          <Form.Item label="备注" name="description">
            <Input.TextArea placeholder="请输入设备备注信息" rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default DeviceList;
