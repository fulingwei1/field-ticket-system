import { httpClient } from '../utils/httpClient';

/**
 * 引导式工单创建会话
 */
export interface GuidedTicketCreationSession {
  sessionId: string;
  ticketId?: string;
  userId: string;
  status: 'collecting' | 'analyzing' | 'guiding' | 'completing' | 'completed';
  turnCount: number;
  maxTurns: number;
  initialText?: string;
  imageAttachmentIds: string[];
  textAnalysis?: TextAnalysisResult;
  imageAnalyses?: ImageAnalysisResult[];
  comprehensiveAnalysis?: ComprehensiveAnalysisResult;
  conversationHistory: GuidedConversationTurn[];
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
}

/**
 * 引导式对话轮次
 */
export interface GuidedConversationTurn {
  turnNumber: number;
  role: 'user' | 'assistant';
  content: string;
  attachmentIds?: string[];
  questions?: GuidedQuestion[];
  timestamp: string;
}

/**
 * 引导性问题
 */
export interface GuidedQuestion {
  questionId: string;
  question: string;
  type: 'yes_no' | 'text' | 'number' | 'select' | 'file';
  options?: string[];
  hint?: string;
  professionalTermExample?: string;
  whyImportant?: string;
}

/**
 * 引导性问题响应
 */
export interface GuidedQuestionResponse {
  session: GuidedTicketCreationSession;
  isComplete: boolean;
  questions: GuidedQuestion[];
  suggestions: string[];
  nextStepHint?: string;
}

/**
 * 文本分析结果
 */
export interface TextAnalysisResult {
  domain: string;
  professionalDescription: {
    title: string;
    detail: string;
  };
  keyInformation: {
    deviceModel?: string;
    symptom?: string;
    frequency?: string;
    environment?: string;
  };
  missingInfo: MissingInfoItem[];
  terminologySuggestions: TerminologySuggestion[];
}

/**
 * 图片分析结果
 */
export interface ImageAnalysisResult {
  deviceInfo: {
    type: string;
    model: string;
  };
  problemPhenomena: Array<{
    description: string;
    location?: string;
    professionalTerm?: string;
  }>;
  keyInformation: string[];
  ocrText?: string;
  suggestions: string[];
}

/**
 * 综合分析结果
 */
export interface ComprehensiveAnalysisResult {
  domain: string;
  professionalDescription: {
    title: string;
    detail: string;
  };
  keyInformation: {
    deviceModel?: string;
    symptom?: string;
    frequency?: string;
    environment?: string;
  };
  missingInfo: MissingInfoItem[];
  terminologySuggestions: TerminologySuggestion[];
  suggestions: string[];
}

/**
 * 缺失信息项
 */
export interface MissingInfoItem {
  field: string;
  question: string;
  type: string;
  required: boolean;
  domain?: string;
  options?: string[];
  hint?: string;
}

/**
 * 术语建议
 */
export interface TerminologySuggestion {
  original: string;
  professional: string;
  explanation: string;
}

/**
 * 工单总结
 */
export interface TicketSummary {
  symptomTitle: string;
  symptomDetail: string;
  domain: string;
  stepCode?: string;
  stepName?: string;
  factsJson: Record<string, string>;
  versionInfo?: {
    swVersion?: string;
    plcVersion?: string;
    paramVersion?: string;
  };
  confidence: {
    overall: number;
    title: number;
    detail: number;
    facts: number;
  };
}

/**
 * 引导式工单内容
 */
export interface GuidedTicketContent {
  summary: TicketSummary;
  imageAttachmentIds: string[];
  terminologySuggestions: TerminologySuggestion[];
  confidence: {
    overall: number;
    title: number;
    detail: number;
    facts: number;
  };
}

/**
 * 引导式工单创建服务
 */
class GuidedTicketCreationService {
  private baseUrl = '/api/guided-ticket-creation';

  /**
   * 创建新的引导式工单创建会话
   */
  async createSession(): Promise<GuidedTicketCreationSession> {
    return httpClient.post<GuidedTicketCreationSession>(`${this.baseUrl}/sessions`);
  }

  /**
   * 提交初始信息（文字+图片）
   */
  async submitInitialInfo(
    sessionId: string,
    textDescription: string,
    images?: File[]
  ): Promise<GuidedQuestionResponse> {
    const formData = new FormData();
    formData.append('textDescription', textDescription);
    
    if (images && images.length > 0) {
      images.forEach((image) => {
        formData.append('images', image);
      });
    }

    return httpClient.post<GuidedQuestionResponse>(
      `${this.baseUrl}/sessions/${sessionId}/initial-info`,
      formData
    );
  }

  /**
   * 回答引导性问题
   */
  async answerQuestion(
    sessionId: string,
    questionId: string,
    answer: string,
    additionalImages?: File[]
  ): Promise<GuidedQuestionResponse> {
    const formData = new FormData();
    formData.append('questionId', questionId);
    formData.append('answer', answer);
    
    if (additionalImages && additionalImages.length > 0) {
      additionalImages.forEach((image) => {
        formData.append('additionalImages', image);
      });
    }

    return httpClient.post<GuidedQuestionResponse>(
      `${this.baseUrl}/sessions/${sessionId}/answer`,
      formData
    );
  }

  /**
   * 生成工单内容
   */
  async generateTicketContent(sessionId: string): Promise<GuidedTicketContent> {
    return httpClient.post<GuidedTicketContent>(
      `${this.baseUrl}/sessions/${sessionId}/generate-content`
    );
  }

  /**
   * 使用会话创建工单
   */
  async createTicketFromSession(
    sessionId: string,
    deviceId: string
  ): Promise<any> {
    return httpClient.post(`${this.baseUrl}/sessions/${sessionId}/create-ticket`, {
      deviceId,
    });
  }

  /**
   * 获取会话状态
   */
  async getSession(sessionId: string): Promise<GuidedTicketCreationSession> {
    return httpClient.get<GuidedTicketCreationSession>(
      `${this.baseUrl}/sessions/${sessionId}`
    );
  }

  /**
   * 清理过期会话
   */
  async cleanupExpiredSessions(hours: number = 24): Promise<{ cleanedCount: number }> {
    return httpClient.post(`${this.baseUrl}/sessions/cleanup?hours=${hours}`);
  }
}

export const guidedTicketCreationService = new GuidedTicketCreationService();
