import request from '../utils/request';

/**
 * 设备 DTO
 */
export interface DeviceDto {
  deviceId: string;
  deviceSn: string;
  deviceName: string;
  deviceModel?: string;
  customerId: string;
  customerName: string;
  projectId?: string;
  projectName?: string;
  plcVersion?: string;
  uiVersion?: string;
  hwVersion?: string;
  installDate?: string;
  location?: string;
  description?: string;
  isActive: boolean;
  ticketCount?: number;
  lastTicketAt?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * 创建设备请求
 */
export interface CreateDeviceRequest {
  deviceSn: string;
  deviceName: string;
  deviceModel?: string;
  customerId: string;
  projectId?: string;
  plcVersion?: string;
  uiVersion?: string;
  hwVersion?: string;
  installDate?: string;
  location?: string;
  description?: string;
}

/**
 * 更新设备请求
 */
export interface UpdateDeviceRequest {
  deviceSn?: string;
  deviceName?: string;
  deviceModel?: string;
  customerId?: string;
  projectId?: string;
  plcVersion?: string;
  uiVersion?: string;
  hwVersion?: string;
  installDate?: string;
  location?: string;
  description?: string;
  isActive?: boolean;
}

/**
 * 设备列表查询参数
 */
export interface DeviceListQuery {
  page?: number;
  pageSize?: number;
  searchQuery?: string;
  customerId?: string;
  projectId?: string;
  isActive?: boolean;
}

/**
 * 分页结果
 */
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

/**
 * 设备配置快照
 */
export interface DeviceSnapshot {
  snapshotId: string;
  deviceId: string;
  plcVersion: string;
  uiVersion: string;
  hwVersion: string;
  configData: any;
  createdAt: string;
  createdBy: string;
  createdByName?: string;
}

/**
 * 设备管理服务
 */
class DeviceService {
  private readonly baseUrl = '/api/devices';

  /**
   * 获取设备列表
   */
  async getDevices(query: DeviceListQuery = {}): Promise<PagedResult<DeviceDto>> {
    const params = new URLSearchParams();

    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.customerId) params.append('customerId', query.customerId);
    if (query.projectId) params.append('projectId', query.projectId);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());

    return request.get(`${this.baseUrl}?${params.toString()}`);
  }

  /**
   * 获取单个设备详情
   */
  async getDeviceById(deviceId: string): Promise<DeviceDto> {
    return request.get(`${this.baseUrl}/${deviceId}`);
  }

  /**
   * 创建设备
   */
  async createDevice(data: CreateDeviceRequest): Promise<DeviceDto> {
    return request.post(this.baseUrl, data);
  }

  /**
   * 更新设备
   */
  async updateDevice(deviceId: string, data: UpdateDeviceRequest): Promise<DeviceDto> {
    return request.put(`${this.baseUrl}/${deviceId}`, data);
  }

  /**
   * 删除设备
   */
  async deleteDevice(deviceId: string): Promise<void> {
    return request.delete(`${this.baseUrl}/${deviceId}`);
  }

  /**
   * 获取设备配置快照列表
   */
  async getDeviceSnapshots(
    deviceId: string,
    page = 1,
    pageSize = 20
  ): Promise<PagedResult<DeviceSnapshot>> {
    return request.get(`${this.baseUrl}/${deviceId}/snapshots?page=${page}&pageSize=${pageSize}`);
  }

  /**
   * 创建设备配置快照
   */
  async createDeviceSnapshot(deviceId: string): Promise<DeviceSnapshot> {
    return request.post(`${this.baseUrl}/${deviceId}/snapshots`);
  }

  /**
   * 生成设备二维码
   */
  async generateQRCode(deviceId: string): Promise<{ qrCodeData: string; qrCodeUrl: string }> {
    return request.post(`${this.baseUrl}/${deviceId}/qrcode`);
  }

  /**
   * 获取设备工单列表
   */
  async getDeviceTickets(deviceId: string, page = 1, pageSize = 20): Promise<any> {
    return request.get(`${this.baseUrl}/${deviceId}/tickets?page=${page}&pageSize=${pageSize}`);
  }

  /**
   * 批量导入设备
   */
  async importDevices(file: File): Promise<{ successCount: number; failCount: number }> {
    const formData = new FormData();
    formData.append('file', file);

    return request.post(`${this.baseUrl}/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  /**
   * 导出设备列表
   */
  async exportDevices(query: DeviceListQuery = {}): Promise<Blob> {
    const params = new URLSearchParams();

    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.customerId) params.append('customerId', query.customerId);
    if (query.projectId) params.append('projectId', query.projectId);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());

    return request.get(`${this.baseUrl}/export?${params.toString()}`, {
      responseType: 'blob',
    });
  }

  /**
   * 搜索设备（兼容旧接口）
   */
  async searchDevices(keyword: string, page = 1, pageSize = 50): Promise<DeviceDto[]> {
    const result = await this.getDevices({
      page,
      pageSize,
      searchQuery: keyword,
    });
    return result.items;
  }

  /**
   * 获取设备（兼容旧接口）
   */
  async getDevice(deviceId: string): Promise<DeviceDto> {
    return this.getDeviceById(deviceId);
  }
}

export const deviceService = new DeviceService();
export default deviceService;
