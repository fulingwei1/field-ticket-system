import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface ThresholdConfigDto {
  configId: string;
  configName: string;
  scenarioType: string;
  scenarioValue?: string;
  timeWindowDays: number;
  triggerCount: number;
  matchCriteria: Record<string, any>;
  triggerRate?: number;
  accuracyRate?: number;
  falsePositiveRate?: number;
  isActive: boolean;
  isAutoOptimized: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ThresholdEvaluation {
  configId: string;
  totalTriggers: number;
  correctTriggers: number;
  falsePositives: number;
  falseNegatives: number;
  accuracyRate: number;
  falsePositiveRate: number;
  falseNegativeRate: number;
  score: number;
}

export interface LearnOptimalThresholdRequest {
  scenarioType: string;
  scenarioValue?: string;
  fromDate?: string;
  toDate?: string;
}

export interface AutoOptimizeThresholdRequest {
  configId: string;
  applyOptimization: boolean;
}

class SmartThresholdService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = authService.getToken();
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * 学习最优阈值
   */
  async learnOptimalThreshold(
    request: LearnOptimalThresholdRequest
  ): Promise<ThresholdConfigDto> {
    return this.request<ThresholdConfigDto>('/api/thresholds/learn-optimal', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * 获取场景阈值
   */
  async getScenarioThreshold(ticketId: string): Promise<ThresholdConfigDto | null> {
    try {
      return await this.request<ThresholdConfigDto>(
        `/api/thresholds/scenario?ticketId=${ticketId}`
      );
    } catch (error: any) {
      if (error.message.includes('404')) {
        return null;
      }
      throw error;
    }
  }

  /**
   * 评估阈值效果
   */
  async evaluateThreshold(
    configId: string,
    fromDate?: string,
    toDate?: string
  ): Promise<ThresholdEvaluation> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.request<ThresholdEvaluation>(
      `/api/thresholds/${configId}/evaluate${query}`
    );
  }

  /**
   * 自动优化阈值
   */
  async autoOptimizeThreshold(
    configId: string,
    request: AutoOptimizeThresholdRequest
  ): Promise<ThresholdConfigDto> {
    return this.request<ThresholdConfigDto>(
      `/api/thresholds/${configId}/auto-optimize`,
      {
        method: 'POST',
        body: JSON.stringify(request),
      }
    );
  }

  /**
   * 获取阈值配置列表
   */
  async getThresholdConfigs(
    scenarioType?: string,
    isActive?: boolean
  ): Promise<ThresholdConfigDto[]> {
    const params = new URLSearchParams();
    if (scenarioType) params.append('scenarioType', scenarioType);
    if (isActive !== undefined) params.append('isActive', String(isActive));
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.request<ThresholdConfigDto[]>(`/api/thresholds${query}`);
  }
}

export const smartThresholdService = new SmartThresholdService();


