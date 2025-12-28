import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface RootCauseAnalysisDto {
  analysisId: string;
  problemId: string;
  why1?: string;
  why2?: string;
  why3?: string;
  why4?: string;
  why5?: string;
  rootCause: string;
  rootCauseCategory?: string;
  preventiveMeasures?: string;
  verificationMethod?: string;
  analyzedBy?: string;
  analyzedByName?: string;
  analyzedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRootCauseAnalysisRequest {
  why1?: string;
  why2?: string;
  why3?: string;
  why4?: string;
  why5?: string;
  rootCause: string;
  rootCauseCategory?: string;
  preventiveMeasures?: string;
  verificationMethod?: string;
}

export interface FiveWhyTemplate {
  problemCategory: string;
  whyQuestions: string[];
  commonRootCauses: string[];
  suggestedPreventiveMeasures: string[];
}

export interface PreventiveMeasureSuggestion {
  category: string;
  measure: string;
  description: string;
  verificationMethod?: string;
  priority: number;
}

class RootCauseAnalysisService {
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
   * 创建或更新根本原因分析
   */
  async createOrUpdateAnalysis(
    problemId: string,
    request: CreateRootCauseAnalysisRequest
  ): Promise<RootCauseAnalysisDto> {
    return this.request<RootCauseAnalysisDto>(
      `/api/root-cause-analysis/problems/${problemId}`,
      {
        method: 'POST',
        body: JSON.stringify(request),
      }
    );
  }

  /**
   * 获取问题的根本原因分析
   */
  async getAnalysisByProblemId(problemId: string): Promise<RootCauseAnalysisDto | null> {
    try {
      return await this.request<RootCauseAnalysisDto>(
        `/api/root-cause-analysis/problems/${problemId}`
      );
    } catch (error: any) {
      if (error.message.includes('404')) {
        return null;
      }
      throw error;
    }
  }

  /**
   * 获取根本原因分析详情
   */
  async getAnalysis(analysisId: string): Promise<RootCauseAnalysisDto> {
    return this.request<RootCauseAnalysisDto>(`/api/root-cause-analysis/${analysisId}`);
  }

  /**
   * 删除根本原因分析
   */
  async deleteAnalysis(analysisId: string): Promise<void> {
    await this.request(`/api/root-cause-analysis/${analysisId}`, {
      method: 'DELETE',
    });
  }

  /**
   * 获取5Why分析模板
   */
  async getFiveWhyTemplate(problemCategory: string): Promise<FiveWhyTemplate> {
    return this.request<FiveWhyTemplate>(
      `/api/root-cause-analysis/templates/five-why?problemCategory=${encodeURIComponent(problemCategory)}`
    );
  }

  /**
   * 获取预防措施建议
   */
  async getPreventiveMeasureSuggestions(
    rootCauseCategory: string
  ): Promise<PreventiveMeasureSuggestion[]> {
    return this.request<PreventiveMeasureSuggestion[]>(
      `/api/root-cause-analysis/suggestions/preventive-measures?rootCauseCategory=${encodeURIComponent(rootCauseCategory)}`
    );
  }
}

export const rootCauseAnalysisService = new RootCauseAnalysisService();








