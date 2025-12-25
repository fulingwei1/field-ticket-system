/**
 * 设备服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface DeviceDto {
  deviceId: string;
  deviceSn: string;
  deviceName?: string;
  customerId?: string;
  customerName?: string;
  projectId?: string;
  projectName?: string;
  ticketCount: number;
  lastTicketAt?: string;
}

class DeviceService {
  /**
   * 获取设备列表
   */
  async getDevices(page: number = 1, pageSize: number = 50): Promise<DeviceDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/devices?page=${page}&pageSize=${pageSize}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取设备列表失败' }));
      throw new Error(error.message || '获取设备列表失败');
    }

    return response.json();
  }

  /**
   * 获取设备详情
   */
  async getDevice(deviceId: string): Promise<DeviceDto> {
    const response = await fetch(`${API_BASE_URL}/api/devices/${deviceId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取设备详情失败' }));
      throw new Error(error.message || '获取设备详情失败');
    }

    return response.json();
  }

  /**
   * 搜索设备
   */
  async searchDevices(keyword: string, page: number = 1, pageSize: number = 50): Promise<DeviceDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/devices/search?keyword=${encodeURIComponent(keyword)}&page=${page}&pageSize=${pageSize}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '搜索设备失败' }));
      throw new Error(error.message || '搜索设备失败');
    }

    return response.json();
  }
}

export const deviceService = new DeviceService();







