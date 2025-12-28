const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface CustomerDto {
  customerId: string;
  customerName: string;
  customerCode?: string;
  industryType?: string;
  contactPerson?: string;
  contactPhone?: string;
}

class CustomerService {
  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const token = localStorage.getItem('token');
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token && { Authorization: `Bearer ${token}` }),
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Request failed' }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
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

    return this.request<CustomerDto[]>(`/api/customers?${params.toString()}`);
  }

  /**
   * 获取客户详情
   */
  async getCustomer(customerId: string): Promise<CustomerDto> {
    return this.request<CustomerDto>(`/api/customers/${customerId}`);
  }
}

export const customerService = new CustomerService();



