/**
 * 工单模板服务
 */
import { authService } from './authService';
import { CreateTicketRequest } from './ticketService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface TicketTemplateDto {
  templateId: string;
  name: string;
  description?: string;
  category?: string;
  createdByUserId: string;
  createdByUserName?: string;
  createdAt: string;
  updatedAt: string;
  usageCount: number;
  lastUsedAt?: string;
  isPublic: boolean;
  templateData: any; // JSON object
}

export interface CreateTicketTemplateRequest {
  name: string;
  description?: string;
  category?: string;
  isPublic: boolean;
  templateData: any; // JSON object
}

export interface UpdateTicketTemplateRequest {
  name?: string;
  description?: string;
  category?: string;
  isPublic?: boolean;
  templateData?: any; // JSON object
}

export interface CreateTemplateFromTicketRequest {
  name: string;
  description?: string;
}

export interface ApplyTemplateRequest {
  deviceId?: string;
}

class TicketTemplateService {
  /**
   * 创建模板
   */
  async createTemplate(request: CreateTicketTemplateRequest): Promise<TicketTemplateDto> {
    const response = await fetch(`${API_BASE_URL}/api/ticket-templates`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '创建模板失败' }));
      throw new Error(error.message || '创建模板失败');
    }

    return response.json();
  }

  /**
   * 从工单创建模板
   */
  async createTemplateFromTicket(
    ticketId: string,
    request: CreateTemplateFromTicketRequest
  ): Promise<TicketTemplateDto> {
    const response = await fetch(`${API_BASE_URL}/api/ticket-templates/from-ticket/${ticketId}`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '从工单创建模板失败' }));
      throw new Error(error.message || '从工单创建模板失败');
    }

    return response.json();
  }

  /**
   * 更新模板
   */
  async updateTemplate(
    templateId: string,
    request: UpdateTicketTemplateRequest
  ): Promise<TicketTemplateDto> {
    const response = await fetch(`${API_BASE_URL}/api/ticket-templates/${templateId}`, {
      method: 'PUT',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '更新模板失败' }));
      throw new Error(error.message || '更新模板失败');
    }

    return response.json();
  }

  /**
   * 删除模板
   */
  async deleteTemplate(templateId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/ticket-templates/${templateId}`, {
      method: 'DELETE',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '删除模板失败' }));
      throw new Error(error.message || '删除模板失败');
    }
  }

  /**
   * 获取模板详情
   */
  async getTemplate(templateId: string): Promise<TicketTemplateDto> {
    const response = await fetch(`${API_BASE_URL}/api/ticket-templates/${templateId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取模板失败' }));
      throw new Error(error.message || '获取模板失败');
    }

    return response.json();
  }

  /**
   * 获取模板列表
   */
  async getTemplates(category?: string, includePublic: boolean = true): Promise<TicketTemplateDto[]> {
    const params = new URLSearchParams();
    if (category) {
      params.append('category', category);
    }
    params.append('includePublic', includePublic.toString());

    const response = await fetch(
      `${API_BASE_URL}/api/ticket-templates?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取模板列表失败' }));
      throw new Error(error.message || '获取模板列表失败');
    }

    return response.json();
  }

  /**
   * 应用模板创建工单草稿
   */
  async applyTemplate(
    templateId: string,
    deviceId?: string
  ): Promise<CreateTicketRequest> {
    const response = await fetch(`${API_BASE_URL}/api/ticket-templates/${templateId}/apply`, {
      method: 'POST',
      headers: authService.getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ deviceId }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '应用模板失败' }));
      throw new Error(error.message || '应用模板失败');
    }

    return response.json();
  }
}

export const ticketTemplateService = new TicketTemplateService();









