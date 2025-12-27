import request from '../utils/request';

/**
 * 用户角色枚举
 */
export enum UserRole {
  FieldEngineer = 'FieldEngineer',
  CS = 'CS',
  SeniorEngineer = 'SeniorEngineer',
  Admin = 'Admin',
}

/**
 * 用户 DTO
 */
export interface UserDto {
  id: string;
  corpId?: string;
  wecomUserId?: string;
  username?: string;
  name: string;
  mobile?: string;
  email?: string;
  deptId?: string;
  deptName?: string;
  role: UserRole;
  loginType?: string; // 'WeCom' | 'Password'
  isActive: boolean;
  mustChangePassword?: boolean;
  lastPasswordChangeAt?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * 创建用户请求
 */
export interface CreateUserRequest {
  username: string;
  name: string;
  password: string;
  role: UserRole;
  email?: string;
  mobile?: string;
  loginType?: string;
}

/**
 * 更新用户请求
 */
export interface UpdateUserRequest {
  role?: UserRole;
  isActive?: boolean;
}

/**
 * 用户列表查询参数
 */
export interface UserListQuery {
  page?: number;
  pageSize?: number;
  searchQuery?: string;
  role?: UserRole;
  isActive?: boolean;
  deptId?: string;
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
 * 用户统计
 */
export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  roleDistribution: Record<UserRole, number>;
}

/**
 * 用户管理服务
 */
class UserManagementService {
  private readonly baseUrl = '/api/users';

  /**
   * 获取用户列表
   */
  async getUsers(query: UserListQuery = {}): Promise<PagedResult<UserDto>> {
    const params = new URLSearchParams();

    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.role) params.append('role', query.role);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());
    if (query.deptId) params.append('deptId', query.deptId);

    return request.get(`${this.baseUrl}?${params.toString()}`);
  }

  /**
   * 获取单个用户详情
   */
  async getUserById(userId: string): Promise<UserDto> {
    return request.get(`${this.baseUrl}/${userId}`);
  }

  /**
   * 创建用户
   */
  async createUser(data: CreateUserRequest): Promise<UserDto> {
    return request.post(this.baseUrl, data);
  }

  /**
   * 更新用户
   */
  async updateUser(userId: string, data: UpdateUserRequest): Promise<UserDto> {
    return request.put(`${this.baseUrl}/${userId}`, data);
  }

  /**
   * 更新用户状态
   */
  async updateUserStatus(userId: string, isActive: boolean): Promise<void> {
    return request.patch(`${this.baseUrl}/${userId}/status`, { isActive });
  }

  /**
   * 删除用户
   */
  async deleteUser(userId: string): Promise<void> {
    return request.delete(`${this.baseUrl}/${userId}`);
  }

  /**
   * 批量更新用户角色
   */
  async batchUpdateRole(userIds: string[], role: UserRole): Promise<void> {
    return request.post(`${this.baseUrl}/batch/update-role`, {
      userIds,
      role,
    });
  }

  /**
   * 批量启用/禁用用户
   */
  async batchUpdateStatus(userIds: string[], isActive: boolean): Promise<void> {
    return request.post(`${this.baseUrl}/batch/update-status`, {
      userIds,
      isActive,
    });
  }

  /**
   * 获取用户统计信息
   */
  async getUserStatistics(): Promise<UserStatistics> {
    return request.get(`${this.baseUrl}/statistics`);
  }

  /**
   * 同步企业微信用户
   */
  async syncWeComUsers(): Promise<{
    addedCount: number;
    updatedCount: number;
    errorCount: number;
  }> {
    return request.post(`${this.baseUrl}/sync/wecom`);
  }

  /**
   * 获取部门列表
   */
  async getDepartments(): Promise<Array<{ deptId: string; deptName: string }>> {
    return request.get('/api/departments');
  }

  /**
   * 导出用户列表
   */
  async exportUsers(query: UserListQuery = {}): Promise<Blob> {
    const params = new URLSearchParams();

    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.role) params.append('role', query.role);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());
    if (query.deptId) params.append('deptId', query.deptId);

    return request.get(`${this.baseUrl}/export?${params.toString()}`, {
      responseType: 'blob',
    });
  }

  /**
   * 重置用户密码（管理员操作）
   */
  async resetPassword(
    userId: string,
    newPassword: string,
    mustChangePassword: boolean = true
  ): Promise<{ success: boolean; message: string }> {
    return request.post('/api/auth/reset-password', {
      userId,
      newPassword,
      mustChangePassword,
    });
  }
}

export const userManagementService = new UserManagementService();
export default userManagementService;
