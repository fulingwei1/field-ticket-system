/**
 * 工单批量操作服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface BatchOperationResult {
  success: boolean;
  totalCount: number;
  successCount: number;
  failureCount: number;
  errors: BatchOperationError[];
  message: string;
}

export interface BatchOperationError {
  ticketId: string;
  ticketNo: string;
  errorMessage: string;
}

export interface BatchUpdateStatusRequest {
  ticketIds: string[];
  newStatus: string;
  reason?: string;
}

export interface BatchAssignEngineerRequest {
  ticketIds: string[];
  engineerId: string;
}

export interface BatchDeleteRequest {
  ticketIds: string[];
}

export interface BatchUpdatePriorityRequest {
  ticketIds: string[];
  priority: string;
}

export interface BatchTagRequest {
  ticketIds: string[];
  tags: string[];
}

class TicketBatchService {
  /**
   * 批量更新状态
   */
  async batchUpdateStatus(request: BatchUpdateStatusRequest): Promise<BatchOperationResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/batch/update-status`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({
        ticketIds: request.ticketIds,
        newStatus: request.newStatus,
        reason: request.reason,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '批量更新状态失败' }));
      throw new Error(error.message || '批量更新状态失败');
    }

    return response.json();
  }

  /**
   * 批量分配工程师
   */
  async batchAssignEngineer(request: BatchAssignEngineerRequest): Promise<BatchOperationResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/batch/assign-engineer`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({
        ticketIds: request.ticketIds,
        engineerId: request.engineerId,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '批量分配工程师失败' }));
      throw new Error(error.message || '批量分配工程师失败');
    }

    return response.json();
  }

  /**
   * 批量删除
   */
  async batchDelete(request: BatchDeleteRequest): Promise<BatchOperationResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/batch/delete`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({
        ticketIds: request.ticketIds,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '批量删除失败' }));
      throw new Error(error.message || '批量删除失败');
    }

    return response.json();
  }

  /**
   * 批量更新优先级
   */
  async batchUpdatePriority(request: BatchUpdatePriorityRequest): Promise<BatchOperationResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/batch/update-priority`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({
        ticketIds: request.ticketIds,
        priority: request.priority,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '批量更新优先级失败' }));
      throw new Error(error.message || '批量更新优先级失败');
    }

    return response.json();
  }

  /**
   * 批量标记
   */
  async batchTag(request: BatchTagRequest): Promise<BatchOperationResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/batch/tag`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({
        ticketIds: request.ticketIds,
        tags: request.tags,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '批量标记失败' }));
      throw new Error(error.message || '批量标记失败');
    }

    return response.json();
  }
}

export const ticketBatchService = new TicketBatchService();











