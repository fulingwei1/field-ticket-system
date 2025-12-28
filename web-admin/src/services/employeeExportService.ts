import request from './request';

export interface EmployeeExportRequest {
  format: 'Excel' | 'CSV';
  deptName?: string;
  role?: string;
  isActivated?: boolean;
  loginType?: string;
  includeInactive?: boolean;
  fields?: string[];
}

export interface ExportPreviewResult {
  totalCount: number;
  activatedCount: number;
  inactivatedCount: number;
  departmentStats: DepartmentStat[];
  roleStats: RoleStat[];
}

export interface DepartmentStat {
  deptName: string;
  count: number;
}

export interface RoleStat {
  role: string;
  count: number;
}

class EmployeeExportService {
  private baseUrl = '/api/employees/export';

  /**
   * 导出员工数据
   */
  async exportEmployees(params: EmployeeExportRequest): Promise<Blob> {
    const response = await request.post(this.baseUrl, params, {
      responseType: 'blob',
    });
    return response;
  }

  /**
   * 预览导出统计信息
   */
  async previewExportStats(params: EmployeeExportRequest): Promise<ExportPreviewResult> {
    return request.post(`${this.baseUrl}/preview`, params);
  }

  /**
   * 下载导出文件
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

  /**
   * 根据筛选条件生成文件名
   */
  generateFileName(params: EmployeeExportRequest): string {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
    const ext = params.format === 'CSV' ? 'csv' : 'xlsx';

    let name = '员工数据';
    if (params.deptName) {
      name += `_${params.deptName}`;
    }
    if (params.role) {
      name += `_${params.role}`;
    }
    if (params.isActivated !== undefined) {
      name += params.isActivated ? '_已开通' : '_未开通';
    }

    return `${name}_${timestamp}.${ext}`;
  }
}

export default new EmployeeExportService();
