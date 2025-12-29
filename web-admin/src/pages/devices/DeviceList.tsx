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
  Descriptions,
  Typography,
} from 'antd';
import {
  SearchOutlined,
  EyeOutlined,
  ReloadOutlined,
  SettingOutlined,
} from '@ant-design/icons';
import { deviceService, DeviceDto } from '../../services/deviceService';
import { customerService } from '../../services/customerService';
import { projectService } from '../../services/projectService';

const { Title, Text } = Typography;
const { Option } = Select;

export default function DeviceList() {
  const [loading, setLoading] = useState(false);
  const [devices, setDevices] = useState<DeviceDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [selectedDevice, setSelectedDevice] = useState<DeviceDto | null>(null);
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  
  // 筛选条件
  const [searchKeyword, setSearchKeyword] = useState('');
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | undefined>();
  const [selectedProjectId, setSelectedProjectId] = useState<string | undefined>();
  const [customers, setCustomers] = useState<Array<{ customerId: string; customerName: string }>>([]);
  const [projects, setProjects] = useState<Array<{ projectId: string; projectName: string }>>([]);

  useEffect(() => {
    loadDevices();
    loadCustomers();
  }, [page, pageSize, selectedProjectId]);

  useEffect(() => {
    if (selectedCustomerId) {
      loadProjects(selectedCustomerId);
    } else {
      setProjects([]);
      setSelectedProjectId(undefined);
    }
  }, [selectedCustomerId]);

  const loadCustomers = async () => {
    try {
      const data = await customerService.getCustomers();
      setCustomers(data.map(c => ({ customerId: c.customerId, customerName: c.customerName })));
    } catch (error: any) {
      console.error('Failed to load customers:', error);
    }
  };

  const loadProjects = async (customerId: string) => {
    try {
      const data = await projectService.getProjects({ customerId });
      setProjects(data.items.map(p => ({ projectId: p.projectId, projectName: p.projectName })));
    } catch (error: any) {
      console.error('Failed to load projects:', error);
    }
  };

  const loadDevices = async () => {
    setLoading(true);
    try {
      const data = await deviceService.getDevices(page, pageSize, selectedProjectId);
      setDevices(data);
      setTotal(data.length); // 注意：这里应该从API返回total，目前简化处理
    } catch (error: any) {
      message.error('加载设备列表失败: ' + (error.message || '未知错误'));
      console.error('Failed to load devices:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async () => {
    if (!searchKeyword.trim()) {
      loadDevices();
      return;
    }

    setLoading(true);
    try {
      const data = await deviceService.searchDevices(searchKeyword, 1, pageSize, selectedProjectId);
      setDevices(data);
      setTotal(data.length);
      setPage(1);
    } catch (error: any) {
      message.error('搜索设备失败: ' + (error.message || '未知错误'));
      console.error('Failed to search devices:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleViewDetail = async (device: DeviceDto) => {
    try {
      const detail = await deviceService.getDevice(device.deviceId);
      setSelectedDevice(detail);
      setDetailModalVisible(true);
    } catch (error: any) {
      message.error('获取设备详情失败: ' + (error.message || '未知错误'));
      console.error('Failed to get device detail:', error);
    }
  };

  const columns = [
    {
      title: '设备SN',
      dataIndex: 'deviceSn',
      key: 'deviceSn',
      width: 150,
    },
    {
      title: '设备名称',
      dataIndex: 'deviceName',
      key: 'deviceName',
      width: 200,
    },
    {
      title: '客户',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 150,
      render: (text: string) => text || <Text type="secondary">未关联</Text>,
    },
    {
      title: '项目',
      dataIndex: 'projectName',
      key: 'projectName',
      width: 150,
      render: (text: string) => text || <Text type="secondary">未关联</Text>,
    },
    {
      title: '工单数',
      dataIndex: 'ticketCount',
      key: 'ticketCount',
      width: 100,
      align: 'center' as const,
    },
    {
      title: '最后工单时间',
      dataIndex: 'lastTicketAt',
      key: 'lastTicketAt',
      width: 180,
      render: (text: string) => text ? new Date(text).toLocaleString('zh-CN') : '-',
    },
    {
      title: '操作',
      key: 'action',
      width: 120,
      fixed: 'right' as const,
      render: (_: any, record: DeviceDto) => (
        <Space>
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => handleViewDetail(record)}
          >
            查看
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Card>
        <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
          <Title level={4} style={{ margin: 0 }}>
            <SettingOutlined /> 设备管理
          </Title>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadDevices}>
              刷新
            </Button>
          </Space>
        </Space>

        {/* 筛选条件 */}
        <Space wrap style={{ marginBottom: 16 }}>
          <Input
            placeholder="搜索设备SN或名称"
            value={searchKeyword}
            onChange={(e) => setSearchKeyword(e.target.value)}
            style={{ width: 250 }}
            allowClear
            prefix={<SearchOutlined />}
            onPressEnter={handleSearch}
          />
          <Button type="primary" icon={<SearchOutlined />} onClick={handleSearch}>
            搜索
          </Button>
          <Select
            placeholder="选择客户"
            allowClear
            style={{ width: 200 }}
            value={selectedCustomerId}
            onChange={(value) => {
              setSelectedCustomerId(value);
              setSelectedProjectId(undefined);
            }}
          >
            {customers.map(c => (
              <Option key={c.customerId} value={c.customerId}>
                {c.customerName}
              </Option>
            ))}
          </Select>
          <Select
            placeholder="选择项目"
            allowClear
            style={{ width: 200 }}
            value={selectedProjectId}
            onChange={(value) => {
              setSelectedProjectId(value);
              setPage(1);
            }}
            disabled={!selectedCustomerId}
          >
            {projects.map(p => (
              <Option key={p.projectId} value={p.projectId}>
                {p.projectName}
              </Option>
            ))}
          </Select>
        </Space>

        {/* 设备列表 */}
        <Table
          columns={columns}
          dataSource={devices}
          rowKey="deviceId"
          loading={loading}
          pagination={{
            current: page,
            pageSize: pageSize,
            total: total,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条记录`,
            onChange: (page, pageSize) => {
              setPage(page);
              setPageSize(pageSize);
            },
          }}
        />
      </Card>

      {/* 设备详情模态框 */}
      <Modal
        title="设备详情"
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={null}
        width={800}
      >
        {selectedDevice && (
          <Descriptions column={2} bordered>
            <Descriptions.Item label="设备SN">{selectedDevice.deviceSn}</Descriptions.Item>
            <Descriptions.Item label="设备名称">{selectedDevice.deviceName || '-'}</Descriptions.Item>
            <Descriptions.Item label="客户">
              {selectedDevice.customerName || <Text type="secondary">未关联</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="项目">
              {selectedDevice.projectName || <Text type="secondary">未关联</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="工单数">{selectedDevice.ticketCount}</Descriptions.Item>
            <Descriptions.Item label="最后工单时间">
              {selectedDevice.lastTicketAt ? new Date(selectedDevice.lastTicketAt).toLocaleString('zh-CN') : '-'}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
}


