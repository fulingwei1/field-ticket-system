import React, { useRef } from 'react';
import { Modal, Typography, Space, Button, Card, Divider } from 'antd';
import { QRCodeSVG } from 'qrcode.react';
import { DownloadOutlined, PrinterOutlined } from '@ant-design/icons';
import { DeviceDto } from '../services/deviceService';

const { Title, Text } = Typography;

interface DeviceQRCodeModalProps {
    visible: boolean;
    onCancel: () => void;
    device: DeviceDto | null;
}

const DeviceQRCodeModal: React.FC<DeviceQRCodeModalProps> = ({
    visible,
    onCancel,
    device,
}) => {
    const printRef = useRef<HTMLDivElement>(null);

    if (!device) return null;

    // URL pointing to the mobile view of the device
    // Assuming a route structure like /mobile/devices/:id or just a public view
    const qrCodeValue = `${window.location.origin}/devices/${device.deviceId}`;

    const handlePrint = () => {
        const content = printRef.current;
        if (!content) return;

        const printWindow = window.open('', '_blank');
        if (!printWindow) return;

        printWindow.document.write(`
      <html>
        <head>
          <title>${device.deviceName || 'Device'} - QR Code</title>
          <style>
            body {
              font-family: Arial, sans-serif;
              display: flex;
              justify-content: center;
              align-items: center;
              height: 100vh;
              margin: 0;
            }
            .card {
              border: 2px solid #000;
              padding: 20px;
              text-align: center;
              border-radius: 8px;
              width: 300px;
            }
            .title {
              font-size: 18px;
              font-weight: bold;
              margin-bottom: 10px;
            }
            .qr-code {
              margin: 20px 0;
            }
            .info {
              font-size: 14px;
              margin: 5px 0;
              color: #333;
            }
            .sn {
              font-family: monospace;
              font-size: 16px;
              font-weight: bold;
              margin-top: 10px;
            }
          </style>
        </head>
        <body>
          <div class="card">
            <div class="title">设备身份证</div>
            <div class="title">${device.deviceName || '未知设备'}</div>
            <div class="qr-code">
              ${content.querySelector('svg')?.outerHTML || ''}
            </div>
            <div class="sn">SN: ${device.deviceSn}</div>
            <div class="info">扫码查看设备详情</div>
          </div>
          <script>
            window.onload = function() {
              window.print();
              window.close();
            }
          </script>
        </body>
      </html>
    `);
        printWindow.document.close();
    };

    const handleDownload = () => {
        const svg = document.getElementById('device-qrcode-svg');
        if (!svg) return;

        const svgData = new XMLSerializer().serializeToString(svg);
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        const img = new Image();

        img.onload = () => {
            canvas.width = img.width;
            canvas.height = img.height;
            ctx?.drawImage(img, 0, 0);
            const pngFile = canvas.toDataURL('image/png');
            const downloadLink = document.createElement('a');
            downloadLink.download = `QRCode-${device.deviceSn}.png`;
            downloadLink.href = pngFile;
            downloadLink.click();
        };

        img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
    };

    return (
        <Modal
            title="设备二维码身份证"
            open={visible}
            onCancel={onCancel}
            footer={[
                <Button key="close" onClick={onCancel}>
                    关闭
                </Button>,
                <Button key="print" icon={<PrinterOutlined />} onClick={handlePrint}>
                    打印
                </Button>,
            ]}
            width={400}
        >
            <div
                style={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    padding: '24px',
                    border: '1px solid #f0f0f0',
                    borderRadius: '8px',
                    background: '#fff'
                }}
                ref={printRef}
            >
                <Title level={4} style={{ margin: '0 0 16px 0' }}>设备身份证</Title>
                <div style={{ marginBottom: '16px' }}>
                    <QRCodeSVG
                        id="device-qrcode-svg"
                        value={qrCodeValue}
                        size={200}
                        level="H"
                        includeMargin
                    />
                </div>
                <Title level={5} style={{ margin: '0 0 8px 0' }}>{device.deviceName}</Title>
                <Text strong style={{ fontSize: '16px', fontFamily: 'monospace' }}>SN: {device.deviceSn}</Text>
                <Text type="secondary" style={{ marginTop: '8px' }}>扫码查看设备详情</Text>
            </div>

            <div style={{ marginTop: '16px', textAlign: 'center' }}>
                <Text type="secondary" style={{ fontSize: '12px' }}>
                    链接地址: {qrCodeValue}
                </Text>
            </div>
        </Modal>
    );
};

export default DeviceQRCodeModal;
