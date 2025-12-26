/**
 * 工单状态历史服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface TicketStatusHistoryDto {
  historyId: string;
  ticketId: string;
  fromStatus: string;
  toStatus: string;
  changeReason?: string;
  changedBy: string;
  changedByName?: string;
  changedAt: string;
  changeType: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  notes?: string;
}

export interface StatusFlowNodeDto {
  status: string;
  label: string;
  enteredAt?: string;
  exitedAt?: string;
  duration?: string; // ISO 8601 duration string
  changedBy?: string;
  changedByName?: string;
  isCurrent: boolean;
}

export interface StatusFlowEdgeDto {
  fromStatus: string;
  toStatus: string;
  changedAt: string;
  changeReason?: string;
  changedBy: string;
  changedByName?: string;
  changeType: string;
}

export interface TicketStatusFlowDto {
  ticketId: string;
  currentStatus: string;
  nodes: StatusFlowNodeDto[];
  edges: StatusFlowEdgeDto[];
  totalDuration?: string; // ISO 8601 duration string
}

class TicketStatusHistoryService {
  /**
   * 获取工单状态历史
   */
  async getStatusHistory(ticketId: string): Promise<TicketStatusHistoryDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/status-history`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取状态历史失败' }));
      throw new Error(error.message || '获取状态历史失败');
    }

    const data = await response.json();
    return data.history || [];
  }

  /**
   * 获取工单状态流转路径（可视化用）
   */
  async getStatusFlow(ticketId: string): Promise<TicketStatusFlowDto> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/status-history/flow`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取状态流转路径失败' }));
      throw new Error(error.message || '获取状态流转路径失败');
    }

    return response.json();
  }
}

export const ticketStatusHistoryService = new TicketStatusHistoryService();











