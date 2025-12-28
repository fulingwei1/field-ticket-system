/**
 * 工单关联分析服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface AssociatedTicketDto {
  ticketId: string;
  ticketNo: string;
  symptomTitle: string;
  domain: string;
  stepCode: string;
  status: string;
  createdAt: string;
  closedAt?: string;
  associationType?: string; // "similar", "device", "customer", "domain"
  similarityScore?: number; // 相似度评分（0-1）
  associationReason?: string; // 关联原因
}

export interface TicketAssociationDto {
  ticketId: string;
  similarTickets: AssociatedTicketDto[];
  deviceRelatedTickets: AssociatedTicketDto[];
  customerRelatedTickets: AssociatedTicketDto[];
  domainRelatedTickets: AssociatedTicketDto[];
  totalCount: number;
}

class TicketAssociationService {
  /**
   * 获取工单的所有关联工单
   */
  async getAssociations(ticketId: string): Promise<TicketAssociationDto> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/associations`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取关联工单失败' }));
      throw new Error(error.message || '获取关联工单失败');
    }

    return response.json();
  }

  /**
   * 获取相似工单
   */
  async getSimilarTickets(ticketId: string, maxResults: number = 10): Promise<AssociatedTicketDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/associations/similar?maxResults=${maxResults}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取相似工单失败' }));
      throw new Error(error.message || '获取相似工单失败');
    }

    return response.json();
  }

  /**
   * 获取设备关联工单
   */
  async getDeviceRelatedTickets(ticketId: string, maxResults: number = 10): Promise<AssociatedTicketDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/associations/device?maxResults=${maxResults}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取设备关联工单失败' }));
      throw new Error(error.message || '获取设备关联工单失败');
    }

    return response.json();
  }

  /**
   * 获取客户关联工单
   */
  async getCustomerRelatedTickets(ticketId: string, maxResults: number = 10): Promise<AssociatedTicketDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/associations/customer?maxResults=${maxResults}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取客户关联工单失败' }));
      throw new Error(error.message || '获取客户关联工单失败');
    }

    return response.json();
  }

  /**
   * 获取问题域关联工单
   */
  async getDomainRelatedTickets(ticketId: string, maxResults: number = 10): Promise<AssociatedTicketDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/associations/domain?maxResults=${maxResults}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取问题域关联工单失败' }));
      throw new Error(error.message || '获取问题域关联工单失败');
    }

    return response.json();
  }
}

export const ticketAssociationService = new TicketAssociationService();
















