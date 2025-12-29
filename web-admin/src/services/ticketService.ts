import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface CreateTicketRequest {
  localDraftId?: string;
  customerId?: string;
  projectId?: string;
  deviceId: string;
  stationId?: string;
  domain: string; // A/B/C/D/E
  stepCode: string;
  stepName?: string;
  symptomTitle: string;
  symptomDetail?: string;
  reproRate?: number;
  rebootRecovers?: boolean;
  envRelated?: boolean;
  swVersion: string;
  plcVersion: string;
  paramVersion: string;
  factsJson: Record<string, any>;
  actionsTaken?: string[];
  actionsTakenNote?: string;
  alarmCode?: string;
  confirmedAsFact: boolean;
}

export interface UpdateTicketRequest {
  domain?: string;
  stepCode?: string;
  stepName?: string;
  symptomTitle?: string;
  symptomDetail?: string;
  reproRate?: number;
  rebootRecovers?: boolean;
  envRelated?: boolean;
  swVersion?: string;
  plcVersion?: string;
  paramVersion?: string;
  factsJson?: Record<string, any>;
  actionsTaken?: string[];
  actionsTakenNote?: string;
  alarmCode?: string;
  confirmedAsFact?: boolean;
}

export interface TicketDto {
  ticketId: string;
  ticketNo: string;
  customerId: string;
  projectId: string;
  deviceId: string;
  stationId?: string;
  createdByUserId: string;
  domain: string;
  stepCode: string;
  stepName?: string;
  symptomTitle: string;
  symptomDetail?: string;
  reproRate?: number;
  rebootRecovers?: boolean;
  envRelated?: boolean;
  swVersion: string;
  plcVersion: string;
  paramVersion: string;
  factsJson: Record<string, any>;
  actionsTaken: string[];
  actionsTakenNote?: string;
  alarmCode?: string;
  confirmedAsFact: boolean;
  confirmedAt?: string;
  status: string;
  priority: string;
  currentJcCode?: string;
  assignedTo?: string;
  createdAt: string;
  updatedAt: string;
  submittedAt?: string;
  closedAt?: string;
  attachmentCount: number;
}

export interface TicketListItemDto {
  ticketId: string;
  ticketNo: string;
  customerName: string;
  deviceSn: string;
  domain: string;
  stepCode: string;
  symptomTitle: string;
  status: string;
  priority: string;
  createdByName: string;
  createdAt: string;
  submittedAt?: string;
}

export interface ValidationError {
  field: string;
  code: string;
  message: string;
}

class TicketService {
  /**
   * 创建工单草稿
   */
  async createDraft(request: CreateTicketRequest, idempotencyKey?: string): Promise<TicketDto> {
    const headers = authService.getAuthHeaders();
    if (idempotencyKey) {
      (headers as any)['X-Idempotency-Key'] = idempotencyKey;
    }

    const response = await fetch(`${API_BASE_URL}/api/tickets`, {
      method: 'POST',
      headers,
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to create ticket');
    }

    return response.json();
  }

  /**
   * 更新工单草稿
   */
  async updateDraft(ticketId: string, request: UpdateTicketRequest): Promise<TicketDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}`, {
      method: 'PUT',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to update ticket');
    }

    return response.json();
  }

  /**
   * 提交工单
   */
  async submitTicket(ticketId: string): Promise<{ success: boolean; ticketNo: string; status: string; errors?: ValidationError[] }> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/submit`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (response.status === 422) {
      const data = await response.json();
      return { success: false, ticketNo: '', status: '', errors: data.errors };
    }

    if (!response.ok) {
      throw new Error('Failed to submit ticket');
    }

    return response.json();
  }

  /**
   * 获取工单详情
   */
  async getTicket(ticketId: string): Promise<TicketDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get ticket');
    }

    const data = await response.json();
    // 后端返回的是 { ticket, relatedChanges }，需要提取 ticket
    return data.ticket || data;
  }

  /**
   * 获取工单列表
   */
  async getTickets(params: {
    status?: string[];
    customerId?: string;
    deviceSn?: string;
    domain?: string;
    priority?: string;
    createdBy?: string;
    dateFrom?: string;
    dateTo?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: TicketListItemDto[]; total: number; page: number; pageSize: number }> {
    const queryParams = new URLSearchParams();
    if (params.status) params.status.forEach(s => queryParams.append('status', s));
    if (params.customerId) queryParams.append('customerId', params.customerId);
    if (params.deviceSn) queryParams.append('deviceSn', params.deviceSn);
    if (params.domain) queryParams.append('domain', params.domain);
    if (params.priority) queryParams.append('priority', params.priority);
    if (params.createdBy) queryParams.append('createdBy', params.createdBy);
    if (params.dateFrom) queryParams.append('dateFrom', params.dateFrom);
    if (params.dateTo) queryParams.append('dateTo', params.dateTo);
    queryParams.append('page', (params.page || 1).toString());
    queryParams.append('pageSize', (params.pageSize || 20).toString());

    const response = await fetch(`${API_BASE_URL}/api/tickets?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const errorText = await response.text();
      let errorMessage = `Failed to get tickets: ${response.status} ${response.statusText}`;
      try {
        const errorJson = JSON.parse(errorText);
        errorMessage = errorJson.message || errorJson.detail || errorMessage;
      } catch {
        if (errorText) {
          errorMessage += ` - ${errorText}`;
        }
      }
      throw new Error(errorMessage);
    }

    return response.json();
  }

  /**
   * 获取工单缺失信息分析
   */
  async getMissingInfo(ticketId: string, jcCode?: string): Promise<MissingInfoAnalysisResult> {
    const queryParams = new URLSearchParams();
    if (jcCode) queryParams.append('jcCode', jcCode);

    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/missing-info?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get missing info');
    }

    return response.json();
  }

  /**
   * 补全缺失信息
   */
  async completeMissingInfo(ticketId: string, request: CompleteMissingInfoRequest): Promise<TicketDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/complete-missing-info`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to complete missing info');
    }

    return response.json();
  }
}

export interface MissingInfoItem {
  field: string;
  question: string;
  type: 'yes_no' | 'number' | 'text' | 'file' | 'select';
  required: boolean;
  domain?: string;
  options?: string[];
  hint?: string;
}

export interface QuestionItem {
  questionId: string;
  question: string;
  type: 'yes_no' | 'number' | 'text' | 'file' | 'select';
  required: boolean;
  options?: string[];
  hint?: string;
  field?: string;
}

export interface MissingInfoAnalysisResult {
  missingInfo: MissingInfoItem[];
  questions: QuestionItem[];
  hasCriticalMissing: boolean;
}

export interface CompleteMissingInfoRequest {
  answers: Record<string, any>;
  attachments?: Record<string, string[]>;
}

export const ticketService = new TicketService();


