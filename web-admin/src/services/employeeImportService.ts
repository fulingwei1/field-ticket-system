import request from '../utils/request';

/**
 * 员工导入行数据
 */
export interface EmployeeImportRow {
  name: string;
  idCard: string;
  deptId?: string;
  deptName: string;
  supervisorName?: string;
  mobile?: string;
  email?: string;
  role?: string;
  rowNumber: number;
}

/**
 * 导入成功的员工信息
 */
export interface EmployeeImportSuccess {
  rowNumber: number;
  userId: string;
  name: string;
  username: string;
  password: string;
  deptName: string;
  supervisorName?: string;
  role: string;
}

/**
 * 导入错误信息
 */
export interface EmployeeImportError {
  rowNumber: number;
  name?: string;
  error: string;
}

/**
 * 导入结果
 */
export interface EmployeeImportResult {
  totalCount: number;
  successCount: number;
  failureCount: number;
  skippedCount: number;
  successList: EmployeeImportSuccess[];
  errorList: EmployeeImportError[];
}

/**
 * 批量导入请求
 */
export interface EmployeeImportRequest {
  employees: Omit<EmployeeImportRow, 'rowNumber'>[];
  defaultRole?: string;
  overwriteExisting?: boolean;
}

/**
 * 账户开通响应
 */
export interface ActivateAccountResponse {
  success: boolean;
  message: string;
  user?: {
    id: string;
    username: string;
    name: string;
    deptName: string;
    isActivated: boolean;
  };
}

/**
 * 批量开通响应
 */
export interface BatchActivateResponse {
  success: boolean;
  message: string;
  activatedCount: number;
  users: Array<{
    id: string;
    username: string;
    name: string;
    isActivated: boolean;
  }>;
}

/**
 * 未开通账户信息
 */
export interface InactivatedUserDto {
  id: string;
  username: string;
  name: string;
  deptName?: string;
  supervisorName?: string;
  mobile?: string;
  email?: string;
  role: string;
  idCardLastFour?: string;
  createdAt: string;
}

/**
 * 员工导入服务
 */
class EmployeeImportService {
  private readonly baseUrl = '/api/employees';

  /**
   * 从Excel文件导入员工
   */
  async importFromExcel(
    file: File,
    defaultRole: string = 'FieldEngineer',
    overwriteExisting: boolean = false
  ): Promise<EmployeeImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('defaultRole', defaultRole);
    formData.append('overwriteExisting', overwriteExisting.toString());

    return request.post(`${this.baseUrl}/import/excel`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  /**
   * 从JSON数据导入员工
   */
  async importFromJson(data: EmployeeImportRequest): Promise<EmployeeImportResult> {
    return request.post(`${this.baseUrl}/import/json`, data);
  }

  /**
   * 下载Excel导入模板
   */
  async downloadTemplate(): Promise<Blob> {
    return request.get(`${this.baseUrl}/import/template`, {
      responseType: 'blob',
    });
  }

  /**
   * 开通单个账户
   */
  async activateAccount(userId: string): Promise<ActivateAccountResponse> {
    return request.post(`${this.baseUrl}/${userId}/activate`);
  }

  /**
   * 批量开通账户
   */
  async batchActivateAccounts(userIds: string[]): Promise<BatchActivateResponse> {
    return request.post(`${this.baseUrl}/batch/activate`, userIds);
  }

  /**
   * 获取未开通账户列表
   */
  async getInactivatedUsers(params?: {
    page?: number;
    pageSize?: number;
    searchQuery?: string;
  }): Promise<{
    items: InactivatedUserDto[];
    total: number;
    page: number;
    pageSize: number;
  }> {
    const query = new URLSearchParams();
    query.append('isActivated', 'false');
    if (params?.page) query.append('page', params.page.toString());
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString());
    if (params?.searchQuery) query.append('searchQuery', params.searchQuery);

    return request.get(`/api/users?${query.toString()}`);
  }
}

export const employeeImportService = new EmployeeImportService();
export default employeeImportService;
