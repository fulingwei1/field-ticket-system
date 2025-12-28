/**
 * 统计服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface StatisticsOverviewDto {
  totalTickets: number;
  openTickets: number;
  closedTickets: number;
  averageClosureTime?: string; // ISO 8601 duration format
  closureRate?: number;
  reopenRate?: number;
  fromDate?: string;
  toDate?: string;
}

export interface DomainStatisticsDto {
  domain: string;
  domainName: string;
  ticketCount: number;
  percentage: number;
  averageClosureTime?: string;
}

export interface ClosureTimeDistributionDto {
  ranges: TimeRangeCountDto[];
  averageClosureTime?: string;
  medianClosureTime?: string;
  p95ClosureTime?: string;
}

export interface TimeRangeCountDto {
  range: string; // "0-1天", "1-3天", etc.
  count: number;
  percentage: number;
}

export interface StatusStatisticsDto {
  status: string;
  statusLabel: string;
  count: number;
  percentage: number;
}

export interface TrendDataPointDto {
  date: string;
  value: number;
  label?: string;
}

class StatisticsService {
  /**
   * 获取统计概览
   */
  async getOverview(fromDate?: string, toDate?: string): Promise<StatisticsOverviewDto> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const response = await fetch(
      `${API_BASE_URL}/api/stats/overview?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取统计概览失败' }));
      throw new Error(error.message || '获取统计概览失败');
    }

    return response.json();
  }

  /**
   * 获取Top问题域统计
   */
  async getTopDomains(topN: number = 5, fromDate?: string, toDate?: string): Promise<DomainStatisticsDto[]> {
    const params = new URLSearchParams();
    params.append('topN', topN.toString());
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const response = await fetch(
      `${API_BASE_URL}/api/stats/top-domains?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取Top问题域统计失败' }));
      throw new Error(error.message || '获取Top问题域统计失败');
    }

    return response.json();
  }

  /**
   * 获取闭环时间分布
   */
  async getClosureTimeDistribution(fromDate?: string, toDate?: string): Promise<ClosureTimeDistributionDto> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const response = await fetch(
      `${API_BASE_URL}/api/stats/closure-time?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取闭环时间分布失败' }));
      throw new Error(error.message || '获取闭环时间分布失败');
    }

    return response.json();
  }

  /**
   * 获取工单状态统计
   */
  async getStatusStatistics(fromDate?: string, toDate?: string): Promise<StatusStatisticsDto[]> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const response = await fetch(
      `${API_BASE_URL}/api/stats/status?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取工单状态统计失败' }));
      throw new Error(error.message || '获取工单状态统计失败');
    }

    return response.json();
  }

  /**
   * 获取趋势数据
   */
  async getTrendData(
    metricType: string,
    fromDate: string,
    toDate: string,
    groupBy: string = 'day'
  ): Promise<TrendDataPointDto[]> {
    const params = new URLSearchParams();
    params.append('metricType', metricType);
    params.append('fromDate', fromDate);
    params.append('toDate', toDate);
    params.append('groupBy', groupBy);

    const response = await fetch(
      `${API_BASE_URL}/api/stats/trends?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取趋势数据失败' }));
      throw new Error(error.message || '获取趋势数据失败');
    }

    return response.json();
  }
}

export const statisticsService = new StatisticsService();















