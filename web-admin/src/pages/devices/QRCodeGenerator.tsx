import React, { useEffect, useState } from 'react';
import {
  Card,
  Row,
  Col,
  Button,
  Table,
  Space,
  Input,
  message,
  Spin,
  Image,
  Modal,
  Typography,
  Tag,
  Select,
  Checkbox,
} from 'antd';
import {
  QrcodeOutlined,
  DownloadOutlined,
  ReloadOutlined,
  SearchOutlined,
  CheckSquareOutlined,
  CloseSquareOutlined,
} from '@ant-design/icons';
import { qrCodeService, GenerateQRCodeResponse } from '../../services/qrCodeService';
import { deviceService, DeviceDto } from '../../services/deviceService';

const { Title, Text } = Typography;
const { Search } = Input;

const QRCodeGenerator: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [devices, setDevices] = useState<DeviceDto[]>([]);
  const [selectedDevices, setSelectedDevices] = useState<string[]>([]);
  const [qrCodes, setQrCodes] = useState<Record<string, string>>({});
  const [previewVisible, setPreviewVisible] = useState(false);
  const [previewDevice, setPreviewDevice] = useState<DeviceDto | null>(null);
  const [previewQRCode, setPreviewQRCode] = useState<string>('');
  const [searchKeyword, setSearchKeyword] = useState('');

  useEffect(() => {
    loadDevices();
  }, []);

  const loadDevices = async () => {
    setLoading(true);
    try {
      const data = await deviceService.getDevices(1, 100);
      setDevices(data);
    } catch (error: any) {
      message.error('加载设备列表失败');
      console.error('Failed to load devices:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (keyword: string) => {
    setSearchKeyword(keyword);
    setLoading(true);
    try {
      const data = await deviceService.searchDevices(keyword, 1, 100);
      setDevices(data);
    } catch (error: any) {
      message.error('搜索设备失败');
      console.error('Failed to search devices:', error);
    } finally {
      setLoading(false);
    }
  };

  const generateQRCode = async (deviceId: string) => {
    try {
      const response = await qrCodeService.generateDeviceQRCode(deviceId);
      setQrCodes((prev) => ({
        ...prev,
        [deviceId]: response.qrCode,
      }));
      message.success('二维码生成成功');
    } catch (error: any) {
      message.error('生成二维码失败');
      console.error('Failed to generate QR code:', error);
    }
  };

  const batchGenerateQRCodes = async () => {
    if (selectedDevices.length === 0) {
      message.warning('请先选择要生成二维码的设备');
      return;
    }

    try {
      setLoading(true);
      await qrCodeService.batchGenerateDeviceQRCodes(selectedDevices);
      message.success('批量生成二维码成功，文件已下载');
    } catch (error: any) {
      message.error('批量生成二维码失败');
      console.error('Failed to batch generate QR codes:', error);
    } finally {
      setLoading(false);
    }
  };

  const handlePreviewQRCode = (device: DeviceDto) => {
    const qrCode = qrCodes[device.deviceId];
    if (!qrCode) {
      message.warning('请先生成二维码');
      return;
    }
    setPreviewDevice(device);
    setPreviewQRCode(qrCode);
    setPreviewVisible(true);
  };

  const downloadQRCode = (device: DeviceDto) => {
    const qrCode = qrCodes[device.deviceId];
    if (!qrCode) {
      message.warning('请先生成二维码');
      return;
    }

    // 将 Base64 转换为 Blob 并下载
    const byteCharacters = atob(qrCode);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: 'image/png' });

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `device_${device.deviceSn || device.deviceId}.png`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
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
      title: '工单数',
      dataIndex: 'ticketCount',
      key: 'ticketCount',
      width: 100,
    },
    {
      title: '最后工单时间',
      dataIndex: 'lastTicketAt',
      key: 'lastTicketAt',
      width: 180,
      render: (date: string) => (date ? new Date(date).toLocaleString() : '-'),
    },
    {
      title: '操作',
      key: 'action',
      width: 250,
      render: (_: any, record: DeviceDto) => (
        <Space>
          <Button
            size="small"
            icon={<QrcodeOutlined />}
            onClick={() => generateQRCode(record.deviceId)}
          >
            生成二维码
          </Button>
          {qrCodes[record.deviceId] && (
            <>
              <Button
                size="small"
                onClick={() => handlePreviewQRCode(record)}
              >
                预览
              </Button>
              <Button
                size="small"
                icon={<DownloadOutlined />}
                onClick={() => downloadQRCode(record)}
              >
                下载
              </Button>
            </>
          )}
        </Space>
      ),
    },
  ];

  const rowSelection = {
    selectedRowKeys: selectedDevices,
    onChange: (selectedRowKeys: React.Key[]) => {
      setSelectedDevices(selectedRowKeys as string[]);
    },
  };

  return (
    <div style={{ padding: '24px' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 页面标题和操作 */}
        <Card>
          <Row justify="space-between" align="middle">
            <Col>
              <Title level={2} style={{ margin: 0 }}>
                设备二维码生成
              </Title>
            </Col>
            <Col>
              <Space>
                <Search
                  placeholder="搜索设备SN或名称"
                  allowClear
                  enterButton={<SearchOutlined />}
                  style={{ width: 300 }}
                  onSearch={handleSearch}
                />
                {selectedDevices.length > 0 && (
                  <Button
                    type="primary"
                    icon={<DownloadOutlined />}
                    onClick={batchGenerateQRCodes}
                    loading={loading}
                  >
                    批量生成并下载 ({selectedDevices.length})
                  </Button>
                )}
                <Button icon={<ReloadOutlined />} onClick={loadDevices}>
                  刷新
                </Button>
              </Space>
            </Col>
          </Row>
        </Card>

        {/* 设备列表 */}
        <Card>
          <Spin spinning={loading}>
            <Table
              rowSelection={rowSelection}
              columns={columns}
              dataSource={devices}
              rowKey="deviceId"
              pagination={{ pageSize: 20 }}
            />
          </Spin>
        </Card>

        {/* 二维码预览模态框 */}
        <Modal
          title={`设备二维码 - ${previewDevice?.deviceSn || previewDevice?.deviceId}`}
          open={previewVisible}
          onCancel={() => setPreviewVisible(false)}
          footer={[
            <Button key="download" icon={<DownloadOutlined />} onClick={() => previewDevice && downloadQRCode(previewDevice)}>
              下载
            </Button>,
            <Button key="close" onClick={() => setPreviewVisible(false)}>
              关闭
            </Button>,
          ]}
          width={400}
        >
          {previewQRCode && (
            <div style={{ textAlign: 'center', padding: '20px' }}>
              <Image
                src={`data:image/png;base64,${previewQRCode}`}
                alt="设备二维码"
                style={{ maxWidth: '100%' }}
              />
              {previewDevice && (
                <div style={{ marginTop: '16px' }}>
                  <Text type="secondary">设备SN: {previewDevice.deviceSn}</Text>
                </div>
              )}
            </div>
          )}
        </Modal>
      </Space>
    </div>
  );
};

export default QRCodeGenerator;




