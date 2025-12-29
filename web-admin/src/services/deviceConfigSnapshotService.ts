/**
 * 设备配置快照服务
 */
import { request } from '../utils/request';

export interface ConfigSnapshotDto {
  snapshotId: string;
  deviceId: string;
  snapshotType: 'delivery' | 'change' | 'problem';
  snapshotAt: string;
  configJson: Record<string, any>;
  standardConfigJson?: Record<string, any>;
  differences?: ConfigDifference[];
  createdBy?: string;
  createdAt: string;
}

export interface ConfigDifference {
  field: string;
  standard?: string;
  actual?: string;
  impact?: string;
  differenceType: 'added' | 'removed' | 'changed';
}

export interface CreateConfigSnapshotRequest {
  deviceId: string;
  snapshotType: 'delivery' | 'change' | 'problem';
  snapshotAt: string;
  configJson: Record<string, any>;
  standardConfigJson?: Record<string, any>;
  differences?: ConfigDifference[];
}

class DeviceConfigSnapshotService {
  /**
   * 创建配置快照
   */
  async createSnapshot(request: CreateConfigSnapshotRequest): Promise<string> {
    const response = await request.post<{ snapshotId: string }>(
      '/api/device-config-snapshots',
      request
    );
    return response.data.snapshotId;
  }

  /**
   * 检查配置差异
   */
  async checkDifferences(
    deviceId: string,
    currentConfig?: Record<string, any>
  ): Promise<ConfigDifference[]> {
    const params = currentConfig ? { ...currentConfig } : {};
    const response = await request.get<ConfigDifference[]>(
      `/api/device-config-snapshots/devices/${deviceId}/differences`,
      params
    );
    return response.data;
  }

  /**
   * 获取快照历史
   */
  async getSnapshotHistory(
    deviceId: string,
    snapshotType?: string,
    limit?: number
  ): Promise<ConfigSnapshotDto[]> {
    const params: Record<string, any> = {};
    if (snapshotType) params.snapshotType = snapshotType;
    if (limit) params.limit = limit;
    
    const response = await request.get<ConfigSnapshotDto[]>(
      `/api/device-config-snapshots/devices/${deviceId}/history`,
      params
    );
    return response.data;
  }

  /**
   * 获取标准配置
   */
  async getStandardConfig(deviceId: string): Promise<Record<string, any> | null> {
    try {
      const response = await request.get<Record<string, any>>(
        `/api/device-config-snapshots/devices/${deviceId}/standard`
      );
      return response.data;
    } catch (error: any) {
      if (error.status === 404) {
        return null;
      }
      throw error;
    }
  }

  /**
   * 获取当前配置
   */
  async getCurrentConfig(deviceId: string): Promise<Record<string, any>> {
    const response = await request.get<Record<string, any>>(
      `/api/device-config-snapshots/devices/${deviceId}/current`
    );
    return response.data;
  }
}

export const deviceConfigSnapshotService = new DeviceConfigSnapshotService();





















