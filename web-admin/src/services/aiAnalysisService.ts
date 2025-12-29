import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface AiAnalysisResultDto {
  analysisId: string;
  analysisType: string;
  analysisDate: string;
  engineerId?: string;
  engineerName?: string;
  departmentId?: string;
  summary: string;
  keyInsights?: any;
  suggestions?: any;
  performanceAnalysis?: any;
  aiModel?: string;
  confidenceScore?: number;
  createdAt: string;
  createdBy?: string;
  createdByName?: string;
}

export interface GenerateDailySummaryRequest {
  engineerId: string;
  analysisDate: string;
}

export interface GenerateWeeklySummaryRequest {
  engineerId: string;
  weekStart: string;
}

export interface GenerateTeamAnalysisRequest {
  departmentId?: string;
  analysisDate: string;
  periodType: string;
}

export interface GenerateSchedulingSuggestionRequest {
  departmentId?: string;
  analysisDate: string;
}

class AiAnalysisService {
  /**
   * 生成每日工作总结
   */
  async generateDailySummary(request: GenerateDailySummaryRequest): Promise<{
    success: boolean;
    result: AiAnalysisResultDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/daily-summary`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('无权生成该工程师的每日总结');
      }
      if (response.status === 404) {
        throw new Error('工程师不存在');
      }
      throw new Error('生成每日总结失败');
    }

    return response.json();
  }

  /**
   * 生成每周总结
   */
  async generateWeeklySummary(request: GenerateWeeklySummaryRequest): Promise<{
    success: boolean;
    result: AiAnalysisResultDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/weekly-summary`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('生成每周总结失败');
    }

    return response.json();
  }

  /**
   * 生成团队分析
   */
  async generateTeamAnalysis(request: GenerateTeamAnalysisRequest): Promise<{
    success: boolean;
    result: AiAnalysisResultDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/team-analysis`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('无权生成团队分析');
      }
      throw new Error('生成团队分析失败');
    }

    return response.json();
  }

  /**
   * 生成人员安排建议
   */
  async generateSchedulingSuggestion(request: GenerateSchedulingSuggestionRequest): Promise<{
    success: boolean;
    result: AiAnalysisResultDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/scheduling-suggestion`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('无权生成人员安排建议');
      }
      throw new Error('生成人员安排建议失败');
    }

    return response.json();
  }

  /**
   * 获取分析结果列表
   */
  async getAnalysisResults(params: {
    analysisType?: string;
    engineerId?: string;
    departmentId?: string;
    analysisDateFrom?: string;
    analysisDateTo?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{
    items: AiAnalysisResultDto[];
    total: number;
    page: number;
    pageSize: number;
  }> {
    const queryParams = new URLSearchParams();
    if (params.analysisType) queryParams.append('analysisType', params.analysisType);
    if (params.engineerId) queryParams.append('engineerId', params.engineerId);
    if (params.departmentId) queryParams.append('departmentId', params.departmentId);
    if (params.analysisDateFrom) queryParams.append('analysisDateFrom', params.analysisDateFrom);
    if (params.analysisDateTo) queryParams.append('analysisDateTo', params.analysisDateTo);
    queryParams.append('page', (params.page || 1).toString());
    queryParams.append('pageSize', (params.pageSize || 20).toString());

    const response = await fetch(
      `${API_BASE_URL}/api/ai-analysis/results?${queryParams}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取分析结果列表失败');
    }

    return response.json();
  }

  /**
   * 获取分析结果详情
   */
  async getAnalysisResult(analysisId: string): Promise<AiAnalysisResultDto> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/results/${analysisId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('分析结果不存在');
      }
      throw new Error('获取分析结果失败');
    }

    return response.json();
  }
}

export const aiAnalysisService = new AiAnalysisService();


