/**
 * 工单导出服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface ExportOptions {
  statuses?: string[];
  customerId?: string;
  deviceSn?: string;
  domain?: string;
  priority?: string;
  createdBy?: string;
  dateFrom?: string;
  dateTo?: string;
  fields?: string[];
}

class TicketExportService {
  /**
   * 导出工单列表（Excel格式）
   */
  async exportToExcel(options: ExportOptions = {}): Promise<void> {
    const params = this.buildQueryParams(options);
    const url = `${API_BASE_URL}/api/tickets/export/excel?${params}`;

    this.downloadFile(url, `工单列表_${new Date().toISOString().slice(0, 10)}.csv`);
  }

  /**
   * 导出工单列表（CSV格式）
   */
  async exportToCsv(options: ExportOptions = {}): Promise<void> {
    const params = this.buildQueryParams(options);
    const url = `${API_BASE_URL}/api/tickets/export/csv?${params}`;

    this.downloadFile(url, `工单列表_${new Date().toISOString().slice(0, 10)}.csv`);
  }

  /**
   * 导出工单详情（Excel格式）
   */
  async exportTicketDetailToExcel(ticketId: string): Promise<void> {
    const url = `${API_BASE_URL}/api/tickets/export/${ticketId}/excel`;

    this.downloadFile(url, `工单详情_${ticketId}_${new Date().toISOString().slice(0, 10)}.csv`);
  }

  /**
   * 构建查询参数
   */
  private buildQueryParams(options: ExportOptions): string {
    const params = new URLSearchParams();

    if (options.statuses && options.statuses.length > 0) {
      params.append('statuses', options.statuses.join(','));
    }
    if (options.customerId) {
      params.append('customerId', options.customerId);
    }
    if (options.deviceSn) {
      params.append('deviceSn', options.deviceSn);
    }
    if (options.domain) {
      params.append('domain', options.domain);
    }
    if (options.priority) {
      params.append('priority', options.priority);
    }
    if (options.createdBy) {
      params.append('createdBy', options.createdBy);
    }
    if (options.dateFrom) {
      params.append('dateFrom', options.dateFrom);
    }
    if (options.dateTo) {
      params.append('dateTo', options.dateTo);
    }
    if (options.fields && options.fields.length > 0) {
      params.append('fields', options.fields.join(','));
    }

    return params.toString();
  }

  /**
   * 下载文件
   */
  private downloadFile(url: string, filename: string): void {
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';

    // 添加认证头（通过 fetch 下载）
    fetch(url, {
      headers: authService.getAuthHeaders(),
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error('导出失败');
        }
        return response.blob();
      })
      .then((blob) => {
        const blobUrl = window.URL.createObjectURL(blob);
        link.href = blobUrl;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(blobUrl);
      })
      .catch((error) => {
        console.error('Export failed:', error);
        throw error;
      });
  }
}

export const ticketExportService = new TicketExportService();









