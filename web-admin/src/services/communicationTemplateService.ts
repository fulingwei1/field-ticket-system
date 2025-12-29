/**
 * 客户沟通模板服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface CommunicationTemplateDto {
  templateId: string;
  code: string;
  name: string;
  scenario: string;
  contentTemplate: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommunicationTemplateRequest {
  code: string;
  name: string;
  scenario: string;
  contentTemplate: string;
  isActive?: boolean;
}

export interface UpdateCommunicationTemplateRequest {
  name?: string;
  scenario?: string;
  contentTemplate?: string;
  isActive?: boolean;
}

export interface CreateCommunicationRequest {
  ticketId: string;
  templateId?: string;
  content: string;
  communicationType?: string;
  customerFeedback?: string;
}

export interface CustomerCommunicationDto {
  communicationId: string;
  ticketId: string;
  templateId?: string;
  templateName?: string;
  content: string;
  communicationType: string;
  communicatedBy: string;
  communicatedByName?: string;
  communicatedAt: string;
  customerFeedback?: string;
}

class CommunicationTemplateService {
  /**
   * 获取模板列表
   */
  async getTemplates(scenario?: string): Promise<CommunicationTemplateDto[]> {
    const params = scenario ? `?scenario=${encodeURIComponent(scenario)}` : '';
    const response = await fetch(`${API_BASE_URL}/api/communication-templates${params}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取模板列表失败' }));
      throw new Error(error.message || '获取模板列表失败');
    }

    return response.json();
  }

  /**
   * 获取模板详情
   */
  async getTemplate(templateId: string): Promise<CommunicationTemplateDto> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/${templateId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取模板详情失败' }));
      throw new Error(error.message || '获取模板详情失败');
    }

    return response.json();
  }

  /**
   * 根据编号获取模板
   */
  async getTemplateByCode(code: string): Promise<CommunicationTemplateDto> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/code/${encodeURIComponent(code)}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取模板失败' }));
      throw new Error(error.message || '获取模板失败');
    }

    return response.json();
  }

  /**
   * 创建模板
   */
  async createTemplate(request: CreateCommunicationTemplateRequest): Promise<string> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '创建模板失败' }));
      throw new Error(error.message || '创建模板失败');
    }

    const result = await response.json();
    return result.templateId;
  }

  /**
   * 更新模板
   */
  async updateTemplate(templateId: string, request: UpdateCommunicationTemplateRequest): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/${templateId}`, {
      method: 'PUT',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '更新模板失败' }));
      throw new Error(error.message || '更新模板失败');
    }
  }

  /**
   * 删除模板
   */
  async deleteTemplate(templateId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/${templateId}`, {
      method: 'DELETE',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '删除模板失败' }));
      throw new Error(error.message || '删除模板失败');
    }
  }

  /**
   * 渲染模板（替换变量）
   */
  async renderTemplate(templateId: string, variables: Record<string, string>): Promise<string> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/${templateId}/render`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(variables),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '渲染模板失败' }));
      throw new Error(error.message || '渲染模板失败');
    }

    const result = await response.json();
    return result.content;
  }

  /**
   * 保存沟通记录
   */
  async saveCommunication(request: CreateCommunicationRequest): Promise<string> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/communications`, {
      method: 'POST',
      headers: {
        ...authService.getAuthHeaders(),
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '保存沟通记录失败' }));
      throw new Error(error.message || '保存沟通记录失败');
    }

    const result = await response.json();
    return result.communicationId;
  }

  /**
   * 获取工单的沟通记录
   */
  async getTicketCommunications(ticketId: string): Promise<CustomerCommunicationDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/communication-templates/tickets/${ticketId}/communications`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '获取沟通记录失败' }));
      throw new Error(error.message || '获取沟通记录失败');
    }

    return response.json();
  }
}

export const communicationTemplateService = new CommunicationTemplateService();


