import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

/**
 * 深度分析结果
 */
export interface DeepAnalysisResult {
  explicitMissing: MissingInfoItem[];
  implicitMissing: ImplicitInfoRequirement[];
  additionalInfo: AdditionalInfoSuggestion[];
  personalizedQuestions: PersonalizedQuestion[];
  contextInfo?: ContextInfo;
}

/**
 * 缺失信息项
 */
export interface MissingInfoItem {
  field: string;
  question: string;
  type: string;
  required: boolean;
  confidence: number;
  reason?: string;
  relatedTicketIds?: string[];
}

/**
 * 额外信息建议
 */
export interface AdditionalInfoSuggestion {
  field: string;
  question: string;
  type: string;
  required: boolean;
  reason?: string;
}

/**
 * 上下文信息
 */
export interface ContextInfo {
  semanticInfo?: SemanticInfo;
  keyInfo: KeyInfo[];
  relatedTickets: RelatedTicket[];
  deviceContext?: DeviceContext;
}

/**
 * 语义分析结果
 */
export interface SemanticInfo {
  severity: number;
  categories: string[];
  entities: string[];
  sentiment: string;
}

/**
 * 关键信息
 */
export interface KeyInfo {
  type: string;
  content: string;
  importance: number;
}

/**
 * 关联工单
 */
export interface RelatedTicket {
  ticketId: string;
  ticketNo: string;
  similarity: number;
  reason?: string;
}

/**
 * 设备上下文
 */
export interface DeviceContext {
  deviceType?: string;
  historicalIssues?: Record<string, number>;
  commonPatterns?: string[];
}

/**
 * 隐含信息需求
 */
export interface ImplicitInfoRequirement {
  requirementId: string;
  category: string;
  description: string;
  priority: 'high' | 'medium' | 'low';
  reason: string;
  suggestedQuestion?: string;
}

/**
 * 个性化问题
 */
export interface PersonalizedQuestion {
  questionId: string;
  question: string;
  type: string; // 'yes_no' | 'number' | 'text' | 'select' | 'file'
  required: boolean;
  options?: string[];
  hint?: string;
  field?: string;
  priority: number; // 1-5, 5最高
  personalizationReason?: string;
}

/**
 * 对话结果
 */
export interface ConversationResult {
  nextQuestion?: PersonalizedQuestion;
  isComplete: boolean;
  collectedInfo: Record<string, any>;
  history: ConversationTurn[];
}

/**
 * 对话轮次
 */
export interface ConversationTurn {
  questionId: string;
  question: string;
  answer?: string;
  timestamp: string;
}

/**
 * 对话历史项
 */
export interface ConversationHistoryItem {
  conversationId: string;
  ticketId: string;
  roundNumber: number;
  questionId: string;
  question: string;
  questionType: string;
  userAnswer?: string;
  answeredAt?: string;
  isAnswered: boolean;
  isSkipped: boolean;
  createdAt: string;
}

/**
 * 继续对话请求
 */
export interface ContinueConversationRequest {
  userAnswer: string;
  questionId: string;
}

/**
 * AI深度分析服务
 */
export const aiDeepAnalysisService = {
  /**
   * 深度分析工单内容
   */
  async analyzeTicket(ticketId: string, jcCode?: string): Promise<DeepAnalysisResult> {
    const url = `${API_BASE_URL}/api/tickets/${ticketId}/ai-analysis/deep${jcCode ? `?jcCode=${jcCode}` : ''}`;
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authService.getToken()}`,
      },
    });
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.message || 'AI深度分析失败');
    }
    return response.json();
  },

  /**
   * 继续多轮对话
   */
  async continueConversation(
    ticketId: string,
    request: ContinueConversationRequest
  ): Promise<ConversationResult> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/conversation/continue`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authService.getToken()}`,
      },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.message || '继续对话失败');
    }
    return response.json();
  },

  /**
   * 获取对话历史
   */
  async getConversationHistory(ticketId: string): Promise<ConversationHistoryItem[]> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/conversation/history`, {
      headers: {
        Authorization: `Bearer ${authService.getToken()}`,
      },
    });
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.message || '获取对话历史失败');
    }
    return response.json();
  },
};

