import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface CustomerDto {
  customerId: string; // Backend returns Guid, will be serialized as string
  customerName: string;
  customerCode?: string;
  industryType?: string;
  contactPerson?: string;
  contactPhone?: string;
}

class CustomerService {

  private async getErrorMessage(response: Response): Promise<string> {
    let errorMessage = `HTTP error! status: ${response.status}`;

    try {
      const error = await response.json();
      errorMessage = error.message || error.detail || error.title || errorMessage;
    } catch {
      const errorText = await response.text();
      if (errorText) {
        errorMessage = errorText;
      }
    }

    return errorMessage;
  }

  private handleUnauthorized(): never {
    console.error('[CustomerService] 401 Unauthorized - token may be expired or invalid');
    authService.clearAuth();
    if (typeof window !== 'undefined') {
      window.location.href = '/login';
    }
    throw new Error('认证失败，请重新登录。可能是 token 已过期，请清除浏览器缓存后重新登录。');
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    // 直接使用 authService.getAuthHeaders()，与 ticketService 保持一致
    const headers = authService.getAuthHeaders();
    
    // 合并用户提供的 headers
    const finalHeaders = {
      ...headers,
      ...(options.headers || {}),
    };
    
    console.log('[CustomerService] Making request to:', `${API_BASE_URL}${endpoint}`);
    console.log('[CustomerService] Headers:', {
      hasAuthorization: !!finalHeaders.Authorization,
      authorizationValue: finalHeaders.Authorization?.toString().substring(0, 30) + '...' || 'N/A',
      allHeaders: finalHeaders
    });

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: finalHeaders,
    });

    console.log('[CustomerService] Response status:', response.status);

    if (!response.ok) {
      if (response.status === 401) {
        console.error('[CustomerService] 401 Unauthorized - attempting token refresh');
        try {
          const refreshed = await authService.refreshToken();
          const retryHeaders = authService.getAuthHeaders();
          const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
            ...options,
            headers: {
              ...retryHeaders,
              ...(options.headers || {}),
            },
          });

          if (!retryResponse.ok) {
            if (retryResponse.status === 401) {
              this.handleUnauthorized();
            }
            const retryErrorMessage = await this.getErrorMessage(retryResponse);
            throw new Error(retryErrorMessage);
          }

          return retryResponse.json();
        } catch {
          this.handleUnauthorized();
        }
      }

      const errorMessage = await this.getErrorMessage(response);
      throw new Error(errorMessage);
    }

    return response.json();
  }

  /**
   * 获取客户列表
   */
  async getCustomers(search?: string, page: number = 1, pageSize: number = 100): Promise<CustomerDto[]> {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    params.append('page', String(page));
    params.append('pageSize', String(pageSize));

    try {
      const data = await this.request<CustomerDto[]>(`/api/customers?${params.toString()}`);
      // 确保 customerId 是字符串格式（后端返回的 Guid 会被序列化为字符串）
      return data.map(c => ({
        ...c,
        customerId: String(c.customerId)
      }));
    } catch (error) {
      console.error('[CustomerService] getCustomers error:', error);
      throw error;
    }
  }

  /**
   * 获取客户详情
   */
  async getCustomer(customerId: string): Promise<CustomerDto> {
    return this.request<CustomerDto>(`/api/customers/${customerId}`);
  }
}

export const customerService = new CustomerService();


帮我完善设备的客户和项目的数据bang帮我重新启动一下项目