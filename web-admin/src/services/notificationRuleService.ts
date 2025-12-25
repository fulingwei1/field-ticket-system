import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface SaveNotificationRuleRequest {
  ruleId?: string;
  ruleLevel: 'customer' | 'project' | 'device';
  customerId?: string;
  projectId?: string;
  deviceId?: string;
  triggerEvent: string;
  recipientsConfig: Record<string, any>;
  templateOverride?: string;
  isActive: boolean;
}

export interface NotificationRuleDto {
  ruleId: string;
  ruleLevel: string;
  customerId?: string;
  projectId?: string;
  deviceId?: string;
  triggerEvent: string;
  recipientsConfig: Record<string, any>;
  templateOverride?: string;
  isActive: boolean;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface NotificationLogDto {
  logId: string;
  ticketId: string;
  triggerEvent: string;
  ruleId?: string;
  recipients: string[];
  sentCount: number;
  failedCount: number;
  messageContent: string;
  sentAt: string;
}

class NotificationRuleService {
  /**
   * 获取通知规则列表
   */
  async getNotificationRules(params?: {
    customerId?: string;
    projectId?: string;
    deviceId?: string;
    triggerEvent?: string;
  }): Promise<NotificationRuleDto[]> {
    const queryParams = new URLSearchParams();
    if (params?.customerId) queryParams.append('customerId', params.customerId);
    if (params?.projectId) queryParams.append('projectId', params.projectId);
    if (params?.deviceId) queryParams.append('deviceId', params.deviceId);
    if (params?.triggerEvent) queryParams.append('triggerEvent', params.triggerEvent);

    const response = await fetch(`${API_BASE_URL}/api/notification-rules?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取通知规则列表失败');
    }

    return response.json();
  }

  /**
   * 创建通知规则
   */
  async createNotificationRule(request: SaveNotificationRuleRequest): Promise<NotificationRuleDto> {
    const response = await fetch(`${API_BASE_URL}/api/notification-rules`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '创建通知规则失败' }));
      throw new Error(error.message || '创建通知规则失败');
    }

    return response.json();
  }

  /**
   * 更新通知规则
   */
  async updateNotificationRule(
    ruleId: string,
    request: SaveNotificationRuleRequest
  ): Promise<NotificationRuleDto> {
    const response = await fetch(`${API_BASE_URL}/api/notification-rules/${ruleId}`, {
      method: 'PUT',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '更新通知规则失败' }));
      throw new Error(error.message || '更新通知规则失败');
    }

    return response.json();
  }

  /**
   * 删除通知规则
   */
  async deleteNotificationRule(ruleId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/notification-rules/${ruleId}`, {
      method: 'DELETE',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('删除通知规则失败');
    }
  }

  /**
   * 获取工单的通知日志
   */
  async getNotificationLogs(ticketId: string): Promise<NotificationLogDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/notification-rules/tickets/${ticketId}/logs`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取通知日志失败');
    }

    return response.json();
  }
}

export const notificationRuleService = new NotificationRuleService();

