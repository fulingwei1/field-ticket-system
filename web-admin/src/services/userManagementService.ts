import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface UserListItemDto {
  id: string;
  name: string;
  username?: string;
  mobile?: string;
  deptId?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UserDetailDto {
  id: string;
  corpId: string;
  weComUserId: string;
  name: string;
  username?: string;
  mobile?: string;
  deptId?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserRequest {
  name: string;
  username?: string;
  password?: string;
  mobile?: string;
  deptId?: string;
  role: string;
  isActive: boolean;
  weComUserId?: string;
}

export interface UpdateUserRequest {
  name?: string;
  username?: string;
  password?: string;
  mobile?: string;
  deptId?: string;
  role?: string;
  isActive?: boolean;
  weComUserId?: string;
}

export interface UserListResponse {
  items: UserListItemDto[];
  total: number;
  page: number;
  pageSize: number;
}

class UserManagementService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...authService.getAuthHeaders(),
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: 'Request failed',
      }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * 获取用户列表
   */
  async getUsers(params: {
    search?: string;
    role?: string;
    isActive?: boolean;
    deptId?: string;
    page?: number;
    pageSize?: number;
  }): Promise<UserListResponse> {
    const queryParams = new URLSearchParams();
    if (params.search) queryParams.append('search', params.search);
    if (params.role) queryParams.append('role', params.role);
    if (params.isActive !== undefined)
      queryParams.append('isActive', String(params.isActive));
    if (params.deptId) queryParams.append('deptId', params.deptId);
    queryParams.append('page', String(params.page || 1));
    queryParams.append('pageSize', String(params.pageSize || 20));

    return this.request<UserListResponse>(
      `/api/user-management?${queryParams.toString()}`
    );
  }

  /**
   * 获取用户详情
   */
  async getUser(userId: string): Promise<UserDetailDto> {
    return this.request<UserDetailDto>(`/api/user-management/${userId}`);
  }

  /**
   * 创建用户
   */
  async createUser(request: CreateUserRequest): Promise<UserDetailDto> {
    return this.request<UserDetailDto>('/api/user-management', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * 更新用户
   */
  async updateUser(
    userId: string,
    request: UpdateUserRequest
  ): Promise<UserDetailDto> {
    return this.request<UserDetailDto>(`/api/user-management/${userId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  /**
   * 删除用户（软删除）
   */
  async deleteUser(userId: string): Promise<void> {
    await this.request(`/api/user-management/${userId}`, {
      method: 'DELETE',
    });
  }

  /**
   * 重置用户密码
   */
  async resetPassword(userId: string, newPassword: string): Promise<void> {
    await this.request(`/api/user-management/${userId}/reset-password`, {
      method: 'POST',
      body: JSON.stringify({ newPassword }),
    });
  }
}

export const userManagementService = new UserManagementService();


