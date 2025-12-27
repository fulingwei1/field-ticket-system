import request from '../utils/request';

/**
 * 客户 DTO
 */
export interface CustomerDto {
  customerId: string;
  customerCode: string;
  customerName: string;
  industry?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * 创建客户请求
 */
export interface CreateCustomerRequest {
  customerCode: string;
  customerName: string;
  industry?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  description?: string;
}

/**
 * 更新客户请求
 */
export interface UpdateCustomerRequest {
  customerCode?: string;
  customerName?: string;
  industry?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  description?: string;
  isActive?: boolean;
}

/**
 * 客户列表查询参数
 */
export interface CustomerListQuery {
  page?: number;
  pageSize?: number;
  searchQuery?: string;
  industry?: string;
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
 * 客户管理服务
 */
class CustomerService {
  private readonly baseUrl = '/api/customers';

  /**
   * 获取客户列表
   */
  async getCustomers(query: CustomerListQuery = {}): Promise<PagedResult<CustomerDto>> {
    const params = new URLSearchParams();

    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.industry) params.append('industry', query.industry);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());

    return request.get(`${this.baseUrl}?${params.toString()}`);
  }

  /**
   * 获取单个客户详情
   */
  async getCustomerById(customerId: string): Promise<CustomerDto> {
    return request.get(`${this.baseUrl}/${customerId}`);
  }

  /**
   * 创建客户
   */
  async createCustomer(data: CreateCustomerRequest): Promise<CustomerDto> {
    return request.post(this.baseUrl, data);
  }

  /**
   * 更新客户
   */
  async updateCustomer(
    customerId: string,
    data: UpdateCustomerRequest
  ): Promise<CustomerDto> {
    return request.put(`${this.baseUrl}/${customerId}`, data);
  }

  /**
   * 删除客户
   */
  async deleteCustomer(customerId: string): Promise<void> {
    return request.delete(`${this.baseUrl}/${customerId}`);
  }

  /**
   * 获取客户下的所有项目
   */
  async getCustomerProjects(customerId: string): Promise<any[]> {
    return request.get(`${this.baseUrl}/${customerId}/projects`);
  }

  /**
   * 获取客户下的所有设备
   */
  async getCustomerDevices(customerId: string): Promise<any[]> {
    return request.get(`${this.baseUrl}/${customerId}/devices`);
  }

  /**
   * 批量导入客户
   */
  async importCustomers(file: File): Promise<{ successCount: number; failCount: number }> {
    const formData = new FormData();
    formData.append('file', file);

    return request.post(`${this.baseUrl}/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  /**
   * 导出客户列表
   */
  async exportCustomers(query: CustomerListQuery = {}): Promise<Blob> {
    const params = new URLSearchParams();

    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.industry) params.append('industry', query.industry);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());

    return request.get(`${this.baseUrl}/export?${params.toString()}`, {
      responseType: 'blob',
    });
  }
}

export const customerService = new CustomerService();
export default customerService;
