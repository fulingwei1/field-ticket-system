import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface GenerateQRCodeResponse {
  qrCode: string; // Base64 图片
  deviceId: string;
}

export interface ParseQRCodeResponse {
  deviceId: string;
  device?: {
    deviceId: string;
    deviceSn: string;
    deviceName?: string;
  };
}

class QRCodeService {
  /**
   * 生成设备二维码
   */
  async generateDeviceQRCode(deviceId: string): Promise<GenerateQRCodeResponse> {
    const response = await fetch(`${API_BASE_URL}/api/qrcodes/devices/${deviceId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('生成二维码失败');
    }

    return response.json();
  }

  /**
   * 批量生成设备二维码（下载ZIP）
   */
  async batchGenerateDeviceQRCodes(deviceIds: string[]): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/qrcodes/devices/batch`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ deviceIds }),
    });

    if (!response.ok) {
      throw new Error('批量生成二维码失败');
    }

    // 下载ZIP文件
    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `device_qrcodes_${new Date().toISOString().slice(0, 10)}.zip`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  /**
   * 解析二维码内容
   */
  async parseQRCode(qrCodeContent: string): Promise<ParseQRCodeResponse> {
    const response = await fetch(`${API_BASE_URL}/api/qrcodes/parse`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ qrCodeContent }),
    });

    if (!response.ok) {
      throw new Error('解析二维码失败');
    }

    return response.json();
  }
}

export const qrCodeService = new QRCodeService();

















