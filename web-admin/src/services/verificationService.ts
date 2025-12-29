import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

export interface SubmitVerificationRequest {
  solutionId?: string;
  runCount: number;
  passCount: number;
  failCount: number;
  checklistResultJson: Record<string, any>;
  evidenceAttachmentIds: string[];
  note?: string;
}

export interface VerificationDto {
  verificationId: string;
  ticketId: string;
  solutionId?: string;
  executedBy: string;
  executedByName?: string;
  runCount: number;
  passCount: number;
  failCount: number;
  result: string;
  checklistResultJson: Record<string, any>;
  evidenceAttachmentIds: string[];
  note?: string;
  verifiedAt: string;
  createdAt: string;
  updatedAt: string;
}

class VerificationService {
  /**
   * 提交验证结果
   */
  async submitVerification(ticketId: string, request: SubmitVerificationRequest): Promise<VerificationDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/verifications`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '提交验证结果失败' }));
      throw new Error(error.message || '提交验证结果失败');
    }

    return response.json();
  }

  /**
   * 获取工单的验证历史
   */
  async getVerificationHistory(ticketId: string): Promise<VerificationDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/verifications`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取验证历史失败');
    }

    return response.json();
  }

  /**
   * 获取验证详情
   */
  async getVerification(verificationId: string): Promise<VerificationDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/verifications/${verificationId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取验证详情失败');
    }

    return response.json();
  }
}

export const verificationService = new VerificationService();


