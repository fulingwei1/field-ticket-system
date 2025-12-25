import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface DiagnosisConversationDto {
  conversationId: string;
  ticketId: string;
  currentHypothesis?: string;
  currentConfidence?: number;
  conversationRound: number;
  status: string;
  diagnosisPath: any;
  createdAt: string;
  updatedAt: string;
}

export interface HypothesisDto {
  hypothesisId: string;
  description: string;
  confidence: number;
  evidence: string[];
  reasoning: string;
}

export interface VerificationStepDto {
  stepId: string;
  conversationId: string;
  hypothesisId: string;
  stepDescription: string;
  stepType: string;
  expectedResult?: string;
  actualResult?: string;
  verificationStatus: string;
  verificationNotes?: string;
  createdAt: string;
}

export interface StartConversationRequest {
  ticketId: string;
}

export interface SubmitVerificationResultRequest {
  stepId: string;
  actualResult: string;
  verificationNotes?: string;
}

export interface AdjustHypothesisRequest {
  hypothesisId: string;
  adjustmentReason: string;
}

class ConversationalDiagnosisService {
  /**
   * 开始诊断对话
   */
  async startConversation(ticketId: string): Promise<DiagnosisConversationDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/diagnosis/start`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify({ ticketId }),
    });

    if (!response.ok) {
      throw new Error('Failed to start conversation');
    }

    return response.json();
  }

  /**
   * 生成初始假设
   */
  async generateInitialHypotheses(ticketId: string): Promise<HypothesisDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/diagnosis/hypotheses`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to generate hypotheses');
    }

    return response.json();
  }

  /**
   * 生成验证步骤
   */
  async generateVerificationSteps(conversationId: string, hypothesisId: string): Promise<VerificationStepDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/conversations/${conversationId}/verification-steps?hypothesisId=${hypothesisId}`,
      {
        method: 'POST',
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to generate verification steps');
    }

    return response.json();
  }

  /**
   * 提交验证结果
   */
  async submitVerificationResult(
    conversationId: string,
    request: SubmitVerificationResultRequest
  ): Promise<DiagnosisConversationDto> {
    const response = await fetch(
      `${API_BASE_URL}/api/conversations/${conversationId}/verification-results`,
      {
        method: 'POST',
        headers: authService.getAuthHeaders(),
        body: JSON.stringify(request),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to submit verification result');
    }

    return response.json();
  }

  /**
   * 调整假设
   */
  async adjustHypothesis(conversationId: string, request: AdjustHypothesisRequest): Promise<HypothesisDto> {
    const response = await fetch(`${API_BASE_URL}/api/conversations/${conversationId}/adjust-hypothesis`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to adjust hypothesis');
    }

    return response.json();
  }

  /**
   * 完成诊断
   */
  async completeDiagnosis(conversationId: string): Promise<DiagnosisConversationDto> {
    const response = await fetch(`${API_BASE_URL}/api/conversations/${conversationId}/complete`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to complete diagnosis');
    }

    return response.json();
  }

  /**
   * 获取诊断路径
   */
  async getDiagnosisPath(conversationId: string): Promise<any> {
    const response = await fetch(`${API_BASE_URL}/api/conversations/${conversationId}/diagnosis-path`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get diagnosis path');
    }

    return response.json();
  }

  /**
   * 获取对话详情
   */
  async getConversation(conversationId: string): Promise<DiagnosisConversationDto> {
    const response = await fetch(`${API_BASE_URL}/api/conversations/${conversationId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get conversation');
    }

    return response.json();
  }
}

export const conversationalDiagnosisService = new ConversationalDiagnosisService();

