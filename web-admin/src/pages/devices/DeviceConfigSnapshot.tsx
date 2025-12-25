"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Input,
  Space,
  Tag,
  message,
  Spin,
  Typography,
  Descriptions,
  Modal,
  Form,
  Select,
  Timeline,
  Divider,
  Alert,
  Tabs,
  Empty,
  Badge,
} from 'antd';
import {
  SearchOutlined,
  ReloadOutlined,
  HistoryOutlined,
  DiffOutlined,
  CameraOutlined,
  WarningOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import {
  deviceConfigSnapshotService,
  ConfigSnapshotDto,
  ConfigDifference,
  CreateConfigSnapshotRequest,
} from '../../services/deviceConfigSnapshotService';
import { deviceService, DeviceDto } from '../../services/deviceService';

const { Title, Text } = Typography;
const { Search } = Input;
const { TextArea } = Input;
const { Option } = Select;
const { TabPane } = Tabs;


export default function DeviceConfigSnapshot() {
  const [loading, setLoading] = useState(false);
  const [devices, setDevices] = useState<DeviceDto[]>([]);
  const [selectedDevice, setSelectedDevice] = useState<DeviceDto | null>(null);
  const [snapshots, setSnapshots] = useState<ConfigSnapshotDto[]>([]);
  const [differences, setDifferences] = useState<ConfigDifference[]>([]);
  const [standardConfig, setStandardConfig] = useState<Record<string, any> | null>(null);
  const [currentConfig, setCurrentConfig] = useState<Record<string, any> | null>(null);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [snapshotModalVisible, setSnapshotModalVisible] = useState(false);
  const [diffModalVisible, setDiffModalVisible] = useState(false);
  const [historyModalVisible, setHistoryModalVisible] = useState(false);
  const [form] = Form.useForm();

  useEffect(() => {
    loadDevices();
  }, []);

  const loadDevices = async () => {
    try {
      setLoading(true);
      const deviceList = await deviceService.getDevices(1, 100);
      setDevices(deviceList);
    } catch (error: any) {
      message.error(error.message || '加载设备列表失败');
    } finally {
      setLoading(false);
    }
  };

  const loadSnapshotHistory = async (deviceId: string, snapshotType?: string) => {
    try {
      setLoading(true);
      const history = await deviceConfigSnapshotService.getSnapshotHistory(
        deviceId,
        snapshotType
      );
      setSnapshots(history);
    } catch (error: any) {
      message.error(`加载快照历史失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const loadDifferences = async (deviceId: string) => {
    try {
      setLoading(true);
      const diff = await deviceConfigSnapshotService.checkDifferences(deviceId);
      setDifferences(diff);
      
      // 同时加载标准配置和当前配置
      const standard = await deviceConfigSnapshotService.getStandardConfig(deviceId);
      const current = await deviceConfigSnapshotService.getCurrentConfig(deviceId);
      setStandardConfig(standard);
      setCurrentConfig(current);
    } catch (error: any) {
      message.error(`检查配置差异失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleViewHistory = async (record: DeviceDto) => {
    setSelectedDevice(record);
    setHistoryModalVisible(true);
    await loadSnapshotHistory(record.deviceId);
  };

  const handleCheckDifferences = async (record: DeviceDto) => {
    setSelectedDevice(record);
    setDiffModalVisible(true);
    await loadDifferences(record.deviceId);
  };

  const handleCreateSnapshot = async (values: any) => {
    if (!selectedDevice) return;

    try {
      setLoading(true);
      const request: CreateConfigSnapshotRequest = {
        deviceId: selectedDevice.deviceId,
        snapshotType: values.snapshotType,
        snapshotAt: values.snapshotAt || new Date().toISOString(),
        configJson: JSON.parse(values.configJson || '{}'),
        standardConfigJson: values.standardConfigJson ? JSON.parse(values.standardConfigJson) : undefined,
      };

      await deviceConfigSnapshotService.createSnapshot(request);
      message.success('配置快照创建成功');
      setSnapshotModalVisible(false);
      form.resetFields();
      await loadSnapshotHistory(selectedDevice.deviceId);
    } catch (error: any) {
      message.error(`创建快照失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const getSnapshotTypeColor = (type: string) => {
    switch (type) {
      case 'delivery':
        return 'success';
      case 'change':
        return 'processing';
      case 'problem':
        return 'error';
      default:
        return 'default';
    }
  };

  const getSnapshotTypeText = (type: string) => {
    switch (type) {
      case 'delivery':
        return '交付';
      case 'change':
        return '变更';
      case 'problem':
        return '问题';
      default:
        return type;
    }
  };

  const getDifferenceTypeIcon = (type: string) => {
    switch (type) {
      case 'added':
        return <CheckCircleOutlined style={{ color: '#52c41a' }} />;
      case 'removed':
        return <WarningOutlined style={{ color: '#ff4d4f' }} />;
      case 'changed':
        return <DiffOutlined style={{ color: '#1890ff' }} />;
      default:
        return null;
    }
  };

  const getDifferenceTypeColor = (type: string) => {
    switch (type) {
      case 'added':
        return 'success';
      case 'removed':
        return 'error';
      case 'changed':
        return 'processing';
      default:
        return 'default';
    }
  };

  const columns = [
    {
      title: '设备编号',
      dataIndex: 'deviceSn',
      key: 'deviceSn',
      width: 120,
    },
    {
      title: '设备名称',
      dataIndex: 'deviceName',
      key: 'deviceName',
      ellipsis: true,
      render: (text: string, record: DeviceDto) => text || record.deviceSn,
    },
    {
      title: '客户',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 150,
    },
    {
      title: '项目',
      dataIndex: 'projectName',
      key: 'projectName',
      width: 150,
    },
    {
      title: '操作',
      key: 'action',
      width: 300,
      render: (_: any, record: DeviceDto) => (
        <Space>
          <Button
            size="small"
            icon={<CameraOutlined />}
            onClick={() => {
              setSelectedDevice(record);
              form.resetFields();
              form.setFieldsValue({
                snapshotType: 'change',
                snapshotAt: new Date().toISOString(),
              });
              setSnapshotModalVisible(true);
            }}
          >
            创建快照
          </Button>
          <Button
            size="small"
            icon={<DiffOutlined />}
            onClick={() => handleCheckDifferences(record)}
          >
            检查差异
          </Button>
          <Button
            size="small"
            icon={<HistoryOutlined />}
            onClick={() => handleViewHistory(record)}
          >
            快照历史
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Title level={2}>设备配置快照管理</Title>
            <Space>
              <Search
                placeholder="搜索设备编号或名称"
                allowClear
                style={{ width: 300 }}
                onSearch={setSearchKeyword}
                onChange={(e) => setSearchKeyword(e.target.value)}
              />
              <Button
                icon={<ReloadOutlined />}
                onClick={loadDevices}
              >
                刷新
              </Button>
            </Space>
          </div>

          <Alert
            message="配置快照说明"
            description="系统会自动记录设备配置快照，包括交付时的标准配置、变更时的配置和问题发生时的配置。可以对比当前配置与标准配置的差异，及时发现配置变更。"
            type="info"
            showIcon
            style={{ marginBottom: '16px' }}
          />

          <Table
            columns={columns}
            dataSource={devices.filter(d => 
              !searchKeyword || 
              d.deviceSn.toLowerCase().includes(searchKeyword.toLowerCase()) ||
              (d.deviceName && d.deviceName.toLowerCase().includes(searchKeyword.toLowerCase()))
            )}
            rowKey="deviceId"
            loading={loading}
            pagination={{
              pageSize: 20,
              showSizeChanger: true,
              showTotal: (total) => `共 ${total} 台设备`,
            }}
          />
        </Space>
      </Card>

      {/* 创建快照弹窗 */}
      <Modal
        title={`创建配置快照 - ${selectedDevice?.deviceSn}`}
        open={snapshotModalVisible}
        onCancel={() => {
          setSnapshotModalVisible(false);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        confirmLoading={loading}
        width={700}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleCreateSnapshot}
        >
          <Form.Item
            name="snapshotType"
            label="快照类型"
            rules={[{ required: true, message: '请选择快照类型' }]}
          >
            <Select>
              <Option value="delivery">交付（标准配置）</Option>
              <Option value="change">变更</Option>
              <Option value="problem">问题</Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="snapshotAt"
            label="快照时间"
            rules={[{ required: true, message: '请选择快照时间' }]}
          >
            <Input type="datetime-local" />
          </Form.Item>
          <Form.Item
            name="configJson"
            label="配置内容（JSON）"
            rules={[
              { required: true, message: '请输入配置内容' },
              {
                validator: (_, value) => {
                  if (!value) return Promise.resolve();
                  try {
                    JSON.parse(value);
                    return Promise.resolve();
                  } catch {
                    return Promise.reject(new Error('请输入有效的JSON格式'));
                  }
                },
              },
            ]}
          >
            <TextArea rows={8} placeholder='{"sw_version": "v2.1.3", "plc_version": "v1.2.6", ...}' />
          </Form.Item>
          <Form.Item
            name="standardConfigJson"
            label="标准配置（JSON，可选）"
          >
            <TextArea rows={6} placeholder='标准配置JSON，用于对比' />
          </Form.Item>
        </Form>
      </Modal>

      {/* 配置差异弹窗 */}
      <Modal
        title={`配置差异 - ${selectedDevice?.deviceSn}`}
        open={diffModalVisible}
        onCancel={() => {
          setDiffModalVisible(false);
          setDifferences([]);
          setStandardConfig(null);
          setCurrentConfig(null);
        }}
        footer={null}
        width={900}
      >
        <Spin spinning={loading}>
          {differences.length > 0 ? (
            <>
              <Alert
                message="发现配置差异"
                description={`检测到 ${differences.length} 处配置差异，请仔细检查。`}
                type="warning"
                showIcon
                style={{ marginBottom: '16px' }}
              />

              <Tabs defaultActiveKey="differences">
                <TabPane tab="差异列表" key="differences">
                  <Timeline>
                    {differences.map((diff, index) => (
                      <Timeline.Item
                        key={index}
                        dot={getDifferenceTypeIcon(diff.differenceType)}
                        color={
                          diff.differenceType === 'added' ? 'green' :
                          diff.differenceType === 'removed' ? 'red' : 'blue'
                        }
                      >
                        <Space direction="vertical" size="small">
                          <div>
                            <Tag color={getDifferenceTypeColor(diff.differenceType)}>
                              {diff.differenceType === 'added' ? '新增' :
                               diff.differenceType === 'removed' ? '删除' : '修改'}
                            </Tag>
                            <Text strong>{diff.field}</Text>
                          </div>
                          {diff.standard && (
                            <Text type="secondary" delete>
                              标准值: {diff.standard}
                            </Text>
                          )}
                          {diff.actual && (
                            <Text>
                              实际值: {diff.actual}
                            </Text>
                          )}
                          {diff.impact && (
                            <Text type="warning">
                              影响: {diff.impact}
                            </Text>
                          )}
                        </Space>
                      </Timeline.Item>
                    ))}
                  </Timeline>
                </TabPane>
                <TabPane tab="标准配置" key="standard">
                  {standardConfig ? (
                    <pre style={{ background: '#f5f5f5', padding: '16px', borderRadius: '4px' }}>
                      {JSON.stringify(standardConfig, null, 2)}
                    </pre>
                  ) : (
                    <Empty description="未找到标准配置" />
                  )}
                </TabPane>
                <TabPane tab="当前配置" key="current">
                  {currentConfig ? (
                    <pre style={{ background: '#f5f5f5', padding: '16px', borderRadius: '4px' }}>
                      {JSON.stringify(currentConfig, null, 2)}
                    </pre>
                  ) : (
                    <Empty description="未找到当前配置" />
                  )}
                </TabPane>
              </Tabs>
            </>
          ) : (
            <Alert
              message="未发现配置差异"
              description="当前配置与标准配置一致，无差异。"
              type="success"
              showIcon
            />
          )}
        </Spin>
      </Modal>

      {/* 快照历史弹窗 */}
      <Modal
        title={`快照历史 - ${selectedDevice?.deviceSn}`}
        open={historyModalVisible}
        onCancel={() => {
          setHistoryModalVisible(false);
          setSnapshots([]);
        }}
        footer={null}
        width={1000}
      >
        {snapshots.length > 0 ? (
          <Table
            columns={[
              {
                title: '快照类型',
                dataIndex: 'snapshotType',
                key: 'snapshotType',
                width: 120,
                render: (type: string) => (
                  <Tag color={getSnapshotTypeColor(type)}>
                    {getSnapshotTypeText(type)}
                  </Tag>
                ),
              },
              {
                title: '快照时间',
                dataIndex: 'snapshotAt',
                key: 'snapshotAt',
                width: 180,
                render: (date: string) => new Date(date).toLocaleString('zh-CN'),
              },
              {
                title: '配置内容',
                dataIndex: 'configJson',
                key: 'configJson',
                ellipsis: true,
                render: (config: Record<string, any>) => (
                  <Text code>{JSON.stringify(config).substring(0, 50)}...</Text>
                ),
              },
              {
                title: '差异数',
                dataIndex: 'differences',
                key: 'differences',
                width: 100,
                render: (diffs?: ConfigDifference[]) => (
                  diffs && diffs.length > 0 ? (
                    <Badge count={diffs.length} showZero />
                  ) : (
                    <Text type="secondary">-</Text>
                  )
                ),
              },
              {
                title: '操作',
                key: 'action',
                width: 120,
                render: (_: any, record: ConfigSnapshotDto) => (
                  <Button
                    size="small"
                    onClick={() => {
                      setDifferences(record.differences || []);
                      setDiffModalVisible(true);
                    }}
                    disabled={!record.differences || record.differences.length === 0}
                  >
                    查看差异
                  </Button>
                ),
              },
            ]}
            dataSource={snapshots}
            rowKey="snapshotId"
            pagination={{
              pageSize: 10,
            }}
            size="small"
          />
        ) : (
          <Empty description="暂无快照历史" />
        )}
      </Modal>
    </div>
  );
}

