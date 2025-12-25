import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface TriageTicketRequest {
  jcCode: string;
  currentHypothesis?: string;
  nextAction?: string;
  confidence: number; // 1-5
  note?: string;
}

export interface TriageResult {
  ticketId: string;
  status: string;
  jcCode?: string;
  escalationRequired: boolean;
  escalatedTo?: string;
  message: string;
}

export interface JudgementCardDto {
  judgementCardId: string;
  jcCode: string;
  title: string;
  description?: string;
  domain: string;
  symptomStructure: Record<string, any>;
  troubleshootingPath: Record<string, any>;
  hypothesisTemplate?: string;
  nextActionTemplate?: string;
  status: string;
  version: number;
  usageCount: number;
  lastUsedAt?: string;
  createdAt: string;
  updatedAt: string;
}

class TriageService {
  /**
   * 分诊工单
   */
  async triageTicket(ticketId: string, request: TriageTicketRequest): Promise<TriageResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/triage`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '分诊失败' }));
      throw new Error(error.message || '分诊失败');
    }

    return response.json();
  }

  /**
   * 获取判断卡列表
   */
  async getJudgementCards(params?: {
    domain?: string;
    status?: string;
  }): Promise<JudgementCardDto[]> {
    const queryParams = new URLSearchParams();
    if (params?.domain) queryParams.append('domain', params.domain);
    if (params?.status) queryParams.append('status', params.status);

    const url = `${API_BASE_URL}/api/judgement-cards${queryParams.toString() ? `?${queryParams}` : ''}`;
    const response = await fetch(url, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取判断卡列表失败');
    }

    return response.json();
  }

  /**
   * 获取判断卡详情
   */
  async getJudgementCard(jcCode: string): Promise<JudgementCardDto> {
    const response = await fetch(`${API_BASE_URL}/api/judgement-cards/${jcCode}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取判断卡详情失败');
    }

    return response.json();
  }
}

export const triageService = new TriageService();


