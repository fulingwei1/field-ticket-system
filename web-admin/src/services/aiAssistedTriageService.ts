/**
 * AI辅助分诊服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

export interface AIAssistedTriageResult {
  recommendedJcCode?: string;
  recommendedJcTitle?: string;
  recommendedHypothesis?: string;
  recommendedNextAction?: string;
  confidence: number;
  confidenceLevel: 'high' | 'medium' | 'low';
  escalationRequired: boolean;
  reasoning?: string;
  hypotheses: HypothesisDto[];
  actionSuggestions: ActionSuggestionDto[];
  missingInfoQuestions: MissingInfoQuestionDto[];
}

export interface HypothesisDto {
  id: string;
  rank: number;
  description: string;
  confidence: 'high' | 'medium' | 'low';
  evidence: string[];
  supportingKnowledgeIds: string[];
  knowledgeReferences?: KnowledgeChunkReference[];
}

export interface KnowledgeChunkReference {
  chunkId: string;
  title: string;
  contentSummary: string;
  sourceType: string;
  sourceId?: string;
  similarityScore: number;
}

export interface ActionSuggestionDto {
  id: string;
  description: string;
  actionType: 'check' | 'action' | 'verify';
  isVerifiable: boolean;
  verificationMethod?: string;
  priority: number;
  relatedHypothesisIds: string[];
}

export interface MissingInfoQuestionDto {
  id: string;
  question: string;
  questionType: 'explicit' | 'implicit' | 'additional';
  isRequired: boolean;
  explanation?: string;
  suggestedAnswers?: string[];
}

class AIAssistedTriageService {
  /**
   * AI辅助填写判断卡（结构化triage）
   */
  async assistTriage(ticketId: string): Promise<AIAssistedTriageResult> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/ai-assist/triage`,
      {
        method: 'POST',
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'AI辅助分诊失败' }));
      throw new Error(error.message || 'AI辅助分诊失败');
    }

    return response.json();
  }

  /**
   * 生成Top-3假设（基于RAG）
   */
  async generateHypotheses(
    ticketId: string,
    jcCode?: string
  ): Promise<HypothesisDto[]> {
    const url = new URL(
      `${API_BASE_URL}/api/tickets/${ticketId}/ai-assist/hypotheses`
    );
    if (jcCode) {
      url.searchParams.append('jcCode', jcCode);
    }

    const response = await fetch(url.toString(), {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '生成假设失败' }));
      throw new Error(error.message || '生成假设失败');
    }

    const data = await response.json();
    return data.hypotheses || [];
  }

  /**
   * 生成下一步动作建议
   */
  async generateActionSuggestions(
    ticketId: string,
    jcCode?: string,
    hypothesisIds?: string[]
  ): Promise<ActionSuggestionDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/tickets/${ticketId}/ai-assist/actions`,
      {
        method: 'POST',
        headers: {
          ...authService.getAuthHeaders(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ jcCode, hypothesisIds }),
      }
    );

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '生成动作建议失败' }));
      throw new Error(error.message || '生成动作建议失败');
    }

    const data = await response.json();
    return data.suggestions || [];
  }

  /**
   * 生成缺失信息问题清单
   */
  async generateMissingInfo(
    ticketId: string,
    jcCode?: string
  ): Promise<MissingInfoQuestionDto[]> {
    const url = new URL(
      `${API_BASE_URL}/api/tickets/${ticketId}/ai-assist/missing-info`
    );
    if (jcCode) {
      url.searchParams.append('jcCode', jcCode);
    }

    const response = await fetch(url.toString(), {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '生成缺失信息清单失败' }));
      throw new Error(error.message || '生成缺失信息清单失败');
    }

    const data = await response.json();
    return data.questions || [];
  }
}

export const aiAssistedTriageService = new AIAssistedTriageService();

