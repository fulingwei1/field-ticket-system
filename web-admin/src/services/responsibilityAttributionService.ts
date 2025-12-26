/**
 * 责任归因服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface ResponsibilityAttributionDto {
  ticketId: string;
  rootResponsibility?: string;
  rootResponsibilityName?: string;
  isPreventable?: boolean;
  responsibilityNotes?: string;
  attributedBy?: string;
  attributedByName?: string;
  attributedAt?: string;
}

export interface AttributeResponsibilityRequest {
  rootResponsibility: string;
  isPreventable: boolean;
  notes?: string;
}

export interface ResponsibilityStatisticsDto {
  distribution: ResponsibilityDistributionDto[];
  preventabilityStats: PreventabilityStatisticsDto[];
  totalAttributed: number;
  totalTickets: number;
  attributionRate: number;
}

export interface ResponsibilityDistributionDto {
  rootResponsibility: string;
  rootResponsibilityName: string;
  count: number;
  percentage: number;
}

export interface PreventabilityStatisticsDto {
  isPreventable: boolean;
  label: string;
  count: number;
  percentage: number;
}

class ResponsibilityAttributionService {
  /**
   * 归因工单问题
   */
  async attributeResponsibility(
    ticketId: string,
    request: AttributeResponsibilityRequest
  ): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/attribute-responsibility`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '归因失败' }));
      throw new Error(error.message || '归因失败');
    }
  }

  /**
   * 获取工单归因信息
   */
  async getAttribution(ticketId: string): Promise<ResponsibilityAttributionDto | null> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/attribution`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      const error = await response.json().catch(() => ({ message: '获取归因信息失败' }));
      throw new Error(error.message || '获取归因信息失败');
    }

    return response.json();
  }

  /**
   * 获取责任归因统计
   */
  async getStatistics(fromDate?: string, toDate?: string): Promise<ResponsibilityStatisticsDto> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const response = await fetch(`${API_BASE_URL}/api/statistics/responsibility?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取统计失败' }));
      throw new Error(error.message || '获取统计失败');
    }

    return response.json();
  }
}

export const responsibilityAttributionService = new ResponsibilityAttributionService();









