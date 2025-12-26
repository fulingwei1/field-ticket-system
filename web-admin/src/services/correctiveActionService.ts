/**
 * 整改任务服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface CorrectiveActionTriggerDto {
  shouldTrigger: boolean;
  reason: string;
  relatedTicketIds: string[];
  triggerData: Record<string, any>;
}

export interface CreateCorrectiveActionRequest {
  triggerType?: string;
  triggerRule?: Record<string, any>;
  relatedTicketIds: string[];
  problemDescription: string;
  rootResponsibility?: string;
  actionPlan: string;
  responsiblePersonId?: string;
  targetCompletionDate?: string;
}

export interface UpdateCorrectiveActionRequest {
  problemDescription?: string;
  rootResponsibility?: string;
  actionPlan?: string;
  responsiblePersonId?: string;
  targetCompletionDate?: string;
  executionNotes?: string;
}

export interface EffectivenessCheckRequest {
  checkDate: string;
  checkResult: string;
  relatedTicketsAfter?: string[];
  notes?: string;
}

export interface CorrectiveActionDto {
  actionId: string;
  actionCode: string;
  triggerType: string;
  relatedTicketIds: string[];
  relatedTicketNos?: string[];
  problemDescription: string;
  rootResponsibility?: string;
  rootResponsibilityName?: string;
  actionPlan: string;
  responsiblePersonId?: string;
  responsiblePersonName?: string;
  targetCompletionDate?: string;
  status: string;
  executionNotes?: string;
  completedAt?: string;
  completedBy?: string;
  completedByName?: string;
  effectivenessCheck?: Record<string, any>;
  createdAt: string;
  updatedAt: string;
}

export interface CorrectiveActionQueryFilter {
  status?: string;
  responsiblePersonId?: string;
  rootResponsibility?: string;
  createdFrom?: string;
  createdTo?: string;
  relatedTicketId?: string;
  page?: number;
  pageSize?: number;
}

class CorrectiveActionService {
  /**
   * 检查触发条件
   */
  async checkTriggers(ticketId: string): Promise<CorrectiveActionTriggerDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/check-corrective-triggers`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '检查触发条件失败' }));
      throw new Error(error.message || '检查触发条件失败');
    }

    return response.json();
  }

  /**
   * 创建整改任务
   */
  async createAction(request: CreateCorrectiveActionRequest): Promise<string> {
    const response = await fetch(`${API_BASE_URL}/api/corrective-actions`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '创建整改任务失败' }));
      throw new Error(error.message || '创建整改任务失败');
    }

    const result = await response.json();
    return result.actionId;
  }

  /**
   * 获取整改任务列表
   */
  async getActions(filter: CorrectiveActionQueryFilter = {}): Promise<{ items: CorrectiveActionDto[]; total: number; page: number; pageSize: number }> {
    const params = new URLSearchParams();
    if (filter.status) params.append('status', filter.status);
    if (filter.responsiblePersonId) params.append('responsiblePersonId', filter.responsiblePersonId);
    if (filter.rootResponsibility) params.append('rootResponsibility', filter.rootResponsibility);
    if (filter.createdFrom) params.append('createdFrom', filter.createdFrom);
    if (filter.createdTo) params.append('createdTo', filter.createdTo);
    if (filter.relatedTicketId) params.append('relatedTicketId', filter.relatedTicketId);
    if (filter.page) params.append('page', filter.page.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const response = await fetch(`${API_BASE_URL}/api/corrective-actions?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取整改任务列表失败' }));
      throw new Error(error.message || '获取整改任务列表失败');
    }

    return response.json();
  }

  /**
   * 获取整改任务详情
   */
  async getAction(actionId: string): Promise<CorrectiveActionDto> {
    const response = await fetch(`${API_BASE_URL}/api/corrective-actions/${actionId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取整改任务详情失败' }));
      throw new Error(error.message || '获取整改任务详情失败');
    }

    return response.json();
  }

  /**
   * 更新整改任务状态
   */
  async updateActionStatus(actionId: string, status: string, notes?: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/corrective-actions/${actionId}/status`, {
      method: 'PUT',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ status, notes }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '更新状态失败' }));
      throw new Error(error.message || '更新状态失败');
    }
  }

  /**
   * 更新整改任务
   */
  async updateAction(actionId: string, request: UpdateCorrectiveActionRequest): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/corrective-actions/${actionId}`, {
      method: 'PUT',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '更新失败' }));
      throw new Error(error.message || '更新失败');
    }
  }

  /**
   * 效果评估
   */
  async evaluateEffectiveness(actionId: string, request: EffectivenessCheckRequest): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/corrective-actions/${actionId}/evaluate`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '效果评估失败' }));
      throw new Error(error.message || '效果评估失败');
    }
  }

  /**
   * 删除整改任务
   */
  async deleteAction(actionId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/corrective-actions/${actionId}`, {
      method: 'DELETE',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '删除失败' }));
      throw new Error(error.message || '删除失败');
    }
  }
}

export const correctiveActionService = new CorrectiveActionService();









