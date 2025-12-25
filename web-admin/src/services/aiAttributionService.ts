import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface AttributionSuggestionDto {
  rootResponsibility: string;
  isPreventable?: boolean;
  confidence: number;
  reasons: string[];
  similarTickets: SimilarTicketInfo[];
  attributionPattern: Record<string, number>;
}

export interface SimilarTicketInfo {
  ticketId: string;
  ticketNo: string;
  rootResponsibility: string;
  isPreventable?: boolean;
  similarityScore: number;
}

export interface ConsistencyCheckResultDto {
  isConsistent: boolean;
  consistencyScore: number;
  issues: InconsistencyIssue[];
  suggestions: string[];
}

export interface InconsistencyIssue {
  field: string;
  currentValue: string;
  expectedValue: string;
  reason: string;
}

export interface AttributionEvaluationDto {
  fromDate: string;
  toDate: string;
  totalTickets: number;
  attributedTickets: number;
  attributionRate: number;
  responsibilityDistribution: Record<string, number>;
  preventabilityRate: Record<string, number>;
  averageConfidence: number;
  trends: AttributionTrend[];
}

export interface AttributionTrend {
  date: string;
  ticketCount: number;
  responsibilityCount: Record<string, number>;
}

export interface AttributionStatisticsDto {
  totalTickets: number;
  attributedTickets: number;
  attributionRate: number;
  responsibilityDistribution: Record<string, number>;
  responsibilityPercentage: Record<string, number>;
  preventableCount: number;
  nonPreventableCount: number;
  preventabilityRate: number;
}

export interface CheckConsistencyRequest {
  rootResponsibility: string;
  isPreventable?: boolean;
}

class AIAttributionService {
  /**
   * 生成归因建议
   */
  async suggestAttribution(ticketId: string): Promise<AttributionSuggestionDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/ai-attribution/suggest`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to suggest attribution');
    }

    return response.json();
  }

  /**
   * 检查归因一致性
   */
  async checkConsistency(ticketId: string, request: CheckConsistencyRequest): Promise<ConsistencyCheckResultDto> {
    const response = await fetch(`${API_BASE_URL}/api/tickets/${ticketId}/ai-attribution/check-consistency`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to check consistency');
    }

    return response.json();
  }

  /**
   * 评估归因效果
   */
  async evaluateAttribution(fromDate?: string, toDate?: string): Promise<AttributionEvaluationDto> {
    const queryParams = new URLSearchParams();
    if (fromDate) queryParams.append('fromDate', fromDate);
    if (toDate) queryParams.append('toDate', toDate);

    const response = await fetch(`${API_BASE_URL}/api/attribution/evaluate?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to evaluate attribution');
    }

    return response.json();
  }

  /**
   * 获取归因统计
   */
  async getAttributionStatistics(fromDate?: string, toDate?: string): Promise<AttributionStatisticsDto> {
    const queryParams = new URLSearchParams();
    if (fromDate) queryParams.append('fromDate', fromDate);
    if (toDate) queryParams.append('toDate', toDate);

    const response = await fetch(`${API_BASE_URL}/api/attribution/statistics?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get attribution statistics');
    }

    return response.json();
  }
}

export const aiAttributionService = new AIAttributionService();

