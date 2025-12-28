import request from './request';

export interface EmployeeUpdateItem {
  identifier: string;
  matchBy: 'Username' | 'Name';
  deptName?: string;
  supervisorName?: string;
  role?: string;
  phoneNumber?: string;
  email?: string;
}

export interface EmployeeBatchUpdateRequest {
  updates: EmployeeUpdateItem[];
  overwriteExisting: boolean;
}

export interface EmployeeBatchUpdateResult {
  totalCount: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  details: EmployeeUpdateItemResult[];
  errorMessages: string[];
}

export interface EmployeeUpdateItemResult {
  identifier: string;
  name: string;
  success: boolean;
  message: string;
  beforeValues: Record<string, string | null>;
  afterValues: Record<string, string | null>;
}

export interface EmployeeUpdateTemplateRequest {
  deptName?: string;
  role?: string;
  onlyInactivated?: boolean;
}

class EmployeeUpdateService {
  private baseUrl = '/api/employees/update';

  /**
   * 批量更新员工信息
   */
  async batchUpdate(data: EmployeeBatchUpdateRequest): Promise<EmployeeBatchUpdateResult> {
    return request.post(`${this.baseUrl}/batch`, data);
  }

  /**
   * 生成更新模板（包含现有数据）
   */
  async generateTemplate(params: EmployeeUpdateTemplateRequest): Promise<Blob> {
    return request.post(`${this.baseUrl}/template`, params, {
      responseType: 'blob',
    });
  }

  /**
   * 从Excel文件解析更新数据
   */
  async parseFromExcel(file: File): Promise<EmployeeUpdateItem[]> {
    const formData = new FormData();
    formData.append('file', file);

    return request.post(`${this.baseUrl}/parse`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  /**
   * 下载文件
   */
  downloadFile(blob: Blob, fileName: string) {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
}

export default new EmployeeUpdateService();
