import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

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

export interface AnalyzeSkillLevelRequest {
  engineerId: string;
  periodType: string;
  periodStart: string;
}

export interface GenerateDevelopmentSuggestionRequest {
  engineerId: string;
  periodType: string;
  periodStart: string;
}

export interface GeneratePerformanceEvaluationRequest {
  engineerId: string;
  periodType: string;
  periodStart: string;
}

export interface SkillLevelAnalysisDto {
  analysisId: string;
  engineerId: string;
  engineerName?: string;
  periodType: string;
  periodStart: string;
  periodEnd: string;
  skillAssessment?: any;
  skillDimensions: Record<string, {
    dimensionName: string;
    score: number;
    level: string;
    description: string;
    evidence: string[];
  }>;
  overallSkillLevel: string;
  skillScore: number;
  summary: string;
  strengths: string[];
  improvementAreas: string[];
  aiModel?: string;
  confidenceScore?: number;
  createdAt: string;
  createdBy?: string;
}

export interface DevelopmentSuggestionDto {
  analysisId: string;
  engineerId: string;
  engineerName?: string;
  periodType: string;
  periodStart: string;
  periodEnd: string;
  developmentCharacteristics: string;
  suggestions: Array<{
    category: string;
    title: string;
    description: string;
    priority: string;
    actionItems: string[];
    expectedOutcome: string;
  }>;
  shortTermGoals: string[];
  mediumTermGoals: string[];
  longTermGoals: string[];
  recommendedResources: string[];
  aiModel?: string;
  confidenceScore?: number;
  createdAt: string;
  createdBy?: string;
}

export interface PerformanceEvaluationDto {
  analysisId: string;
  engineerId: string;
  engineerName?: string;
  periodType: string;
  periodStart: string;
  periodEnd: string;
  evaluationSummary: string;
  performanceLevel: string;
  overallScore: number;
  dimensionEvaluations: Record<string, {
    dimensionName: string;
    score: number;
    level: string;
    evaluation: string;
    strengths: string[];
    weaknesses: string[];
  }>;
  highlights: string[];
  areasForImprovement: string[];
  evidence: Array<{
    type: string;
    description: string;
    referenceId?: string;
    occurredAt?: string;
  }>;
  comparison?: {
    teamAverageScore: number;
    departmentAverageScore: number;
    comparisonSummary: string;
    advantages: string[];
    gaps: string[];
  };
  aiModel?: string;
  confidenceScore?: number;
  createdAt: string;
  createdBy?: string;
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

  /**
   * 分析工程师技能水平
   */
  async analyzeSkillLevel(request: AnalyzeSkillLevelRequest): Promise<{
    success: boolean;
    result: SkillLevelAnalysisDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/skill-level`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('无权分析该工程师的技能水平');
      }
      if (response.status === 404) {
        throw new Error('工程师不存在');
      }
      throw new Error('分析技能水平失败');
    }

    return response.json();
  }

  /**
   * 生成工程师发展建议
   */
  async generateDevelopmentSuggestion(request: GenerateDevelopmentSuggestionRequest): Promise<{
    success: boolean;
    result: DevelopmentSuggestionDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/development-suggestion`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('无权生成该工程师的发展建议');
      }
      if (response.status === 404) {
        throw new Error('工程师不存在');
      }
      throw new Error('生成发展建议失败');
    }

    return response.json();
  }

  /**
   * 生成综合绩效评价
   */
  async generatePerformanceEvaluation(request: GeneratePerformanceEvaluationRequest): Promise<{
    success: boolean;
    result: PerformanceEvaluationDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/ai-analysis/performance-evaluation`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 403) {
        throw new Error('无权生成该工程师的绩效评价');
      }
      if (response.status === 404) {
        throw new Error('工程师不存在');
      }
      if (response.status === 400) {
        throw new Error('绩效数据不存在，请先计算绩效');
      }
      throw new Error('生成绩效评价失败');
    }

    return response.json();
  }
}

export const aiAnalysisService = new AiAnalysisService();


