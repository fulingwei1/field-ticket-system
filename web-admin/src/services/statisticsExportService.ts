/**
 * 统计数据导出服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

class StatisticsExportService {
  /**
   * 导出统计概览
   */
  async exportOverview(fromDate?: string, toDate?: string, format: string = 'csv'): Promise<void> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    params.append('format', format);

    const response = await fetch(`${API_BASE_URL}/api/stats/export/overview?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('导出失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `统计概览_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  /**
   * 导出Top问题域
   */
  async exportTopDomains(topN: number = 10, fromDate?: string, toDate?: string, format: string = 'csv'): Promise<void> {
    const params = new URLSearchParams();
    params.append('topN', topN.toString());
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    params.append('format', format);

    const response = await fetch(`${API_BASE_URL}/api/stats/export/top-domains?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('导出失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Top问题域统计_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  /**
   * 导出闭环时间分布
   */
  async exportClosureTimeDistribution(fromDate?: string, toDate?: string, format: string = 'csv'): Promise<void> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    params.append('format', format);

    const response = await fetch(`${API_BASE_URL}/api/stats/export/closure-time?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('导出失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `闭环时间分布_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  /**
   * 导出状态统计
   */
  async exportStatusStatistics(fromDate?: string, toDate?: string, format: string = 'csv'): Promise<void> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    params.append('format', format);

    const response = await fetch(`${API_BASE_URL}/api/stats/export/status?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('导出失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `状态统计_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  /**
   * 导出趋势数据
   */
  async exportTrendData(
    metricType: string,
    fromDate: string,
    toDate: string,
    groupBy: string = 'day',
    format: string = 'csv'
  ): Promise<void> {
    const params = new URLSearchParams();
    params.append('metricType', metricType);
    params.append('fromDate', fromDate);
    params.append('toDate', toDate);
    params.append('groupBy', groupBy);
    params.append('format', format);

    const response = await fetch(`${API_BASE_URL}/api/stats/export/trends?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('导出失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `趋势数据_${metricType}_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  /**
   * 导出完整报告
   */
  async exportFullReport(fromDate?: string, toDate?: string, format: string = 'csv'): Promise<void> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    params.append('format', format);

    const response = await fetch(`${API_BASE_URL}/api/stats/export/full-report?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('导出失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `统计报告_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }
}

export const statisticsExportService = new StatisticsExportService();









