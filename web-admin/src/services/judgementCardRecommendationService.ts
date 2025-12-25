import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface JudgementCardRecommendationDto {
  judgementCard: {
    judgementCardId: string;
    jcCode: string;
    title: string;
    description?: string;
    domain: string;
    version: number;
    usageCount: number;
    lastUsedAt?: string;
  };
  matchScore: number;
  matchReasons: string[];
  scoreBreakdown: Record<string, number>;
}

export interface RecommendJudgementCardsResponse {
  recommendations: JudgementCardRecommendationDto[];
  totalCandidates: number;
}

class JudgementCardRecommendationService {
  /**
   * 推荐判断卡
   */
  async recommendJudgementCards(params: {
    domain?: string;
    stepCode?: string;
    symptomTitle?: string;
    topK?: number;
  }): Promise<RecommendJudgementCardsResponse> {
    const queryParams = new URLSearchParams();
    if (params.domain) queryParams.append('domain', params.domain);
    if (params.stepCode) queryParams.append('stepCode', params.stepCode);
    if (params.symptomTitle) queryParams.append('symptomTitle', params.symptomTitle);
    if (params.topK) queryParams.append('topK', params.topK.toString());

    const response = await fetch(`${API_BASE_URL}/api/judgement-cards/recommend?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to recommend judgement cards');
    }

    return response.json();
  }
}

export const judgementCardRecommendationService = new JudgementCardRecommendationService();

