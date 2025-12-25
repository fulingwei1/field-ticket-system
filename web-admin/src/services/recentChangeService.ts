import { request } from '../utils/request';

/**
 * 变更类型
 */
export type ChangeType = 'program_upgrade' | 'param_change' | 'component_replacement' | 'config_change';

/**
 * 相关变更DTO
 */
export interface RelatedChangeDto {
  changeId: string;
  deviceId: string;
  deviceSn?: string;
  changeType: ChangeType;
  changeDate: string;
  changeDetail: Record<string, any>;
  impactScope?: Record<string, any>;
  relevanceScore: number;
  relevanceReasons: string[];
  createdBy?: string;
  createdByName?: string;
  createdAt: string;
}

/**
 * 设备变更记录DTO
 */
export interface DeviceChangeLogDto {
  changeId: string;
  deviceId: string;
  deviceSn?: string;
  changeType: ChangeType;
  changeDate: string;
  changeDetail: Record<string, any>;
  impactScope?: Record<string, any>;
  createdBy?: string;
  createdByName?: string;
  createdAt: string;
}

/**
 * 创建设备变更请求
 */
export interface CreateDeviceChangeRequest {
  deviceId: string;
  changeType: ChangeType;
  changeDate: string;
  changeDetail: Record<string, any>;
  impactScope?: Record<string, any>;
}

/**
 * 最近变更服务
 */
export const recentChangeService = {
  /**
   * 获取工单相关的最近变更
   */
  async getRelatedChanges(
    ticketId: string,
    daysBefore: number = 7,
    daysAfter: number = 7
  ): Promise<RelatedChangeDto[]> {
    const response = await request.get<RelatedChangeDto[]>(
      `/api/recent-changes/tickets/${ticketId}`,
      { daysBefore, daysAfter }
    );
    return response.data;
  },

  /**
   * 获取设备的变更历史
   */
  async getDeviceChanges(
    deviceId: string,
    fromDate?: string,
    toDate?: string
  ): Promise<DeviceChangeLogDto[]> {
    const params: Record<string, any> = {};
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    
    const response = await request.get<DeviceChangeLogDto[]>(
      `/api/recent-changes/devices/${deviceId}`,
      params
    );
    return response.data;
  },

  /**
   * 记录设备变更
   */
  async recordChange(changeRequest: CreateDeviceChangeRequest): Promise<DeviceChangeLogDto> {
    const response = await request.post<DeviceChangeLogDto>(
      '/api/recent-changes/devices',
      changeRequest
    );
    return response.data;
  },
};

