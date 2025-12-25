import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface PerformanceMetricsDto {
  metricId: string;
  engineerId: string;
  engineerName?: string;
  periodType: string;
  periodStart: string;
  periodEnd: string;
  totalTickets: number;
  ticketsResolved: number;
  ticketsPending: number;
  averageResolutionTime?: string;
  firstTimeResolutionRate?: number;
  averageResponseTime?: string;
  responseTimeP95?: string;
  onTimeResponseRate?: number;
  devicesServiced: number;
  deviceFailureRate?: number;
  repeatFailureRate?: number;
  workActivityDays: number;
  workActivityCompleteness?: number;
  ticketCreationCompleteness?: number;
  fieldFeedbackTimelinessRate?: number;
  questionReplyTimelinessRate?: number;
  verificationPassRate?: number;
  repeatProblemRate?: number;
  judgementCardUsageAccuracy?: number;
  judgementCardHitRate?: number;
  aiSuggestionAdoptionRate?: number;
  lowConfidenceUpgradeTimeliness?: number;
  judgementCardsCreated: number;
  judgementCardQualityScore?: number;
  judgementCardReuseContribution?: number;
  solutionsContributed: number;
  customerCommunicationTimeliness?: number;
  customerCommunicationQuality?: number;
  customerSatisfactionScore?: number;
  customerFeedbackCount: number;
  teamCollaborationActivity?: number;
  knowledgeSharingContribution?: number;
  ticketInformationCompleteness?: number;
  rootCauseAttributionCompleteness?: number;
  overallScore?: number;
  performanceLevel?: string;
  rankInTeam?: number;
  rankInDepartment?: number;
  calculatedAt: string;
  createdAt: string;
  updatedAt: string;
}

export interface PerformanceRankingDto {
  engineerId: string;
  engineerName: string;
  departmentName?: string;
  overallScore?: number;
  performanceLevel?: string;
  rank: number;
  totalTickets: number;
  ticketsResolved: number;
  firstTimeResolutionRate?: number;
  averageResolutionTime?: string;
}

export interface PerformanceTrendDto {
  periodStart: string;
  periodEnd: string;
  overallScore?: number;
  totalTickets: number;
  ticketsResolved: number;
  firstTimeResolutionRate?: number;
  averageResolutionTime?: string;
  onTimeResponseRate?: number;
  rankInTeam?: number;
  rankInDepartment?: number;
}

export interface CalculatePerformanceRequest {
  engineerId: string;
  periodType: string;
  periodStart: string;
  periodEnd: string;
}

class PerformanceService {
  /**
   * 获取绩效指标列表
   */
  async getMetrics(params: {
    engineerId?: string;
    departmentId?: string;
    periodType?: string;
    periodStartFrom?: string;
    periodStartTo?: string;
    minOverallScore?: number;
    performanceLevel?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: PerformanceMetricsDto[]; total: number; page: number; pageSize: number }> {
    const queryParams = new URLSearchParams();
    if (params.engineerId) queryParams.append('engineerId', params.engineerId);
    if (params.departmentId) queryParams.append('departmentId', params.departmentId);
    if (params.periodType) queryParams.append('periodType', params.periodType);
    if (params.periodStartFrom) queryParams.append('periodStartFrom', params.periodStartFrom);
    if (params.periodStartTo) queryParams.append('periodStartTo', params.periodStartTo);
    if (params.minOverallScore !== undefined) queryParams.append('minOverallScore', params.minOverallScore.toString());
    if (params.performanceLevel) queryParams.append('performanceLevel', params.performanceLevel);
    queryParams.append('page', (params.page || 1).toString());
    queryParams.append('pageSize', (params.pageSize || 20).toString());

    const response = await fetch(`${API_BASE_URL}/api/performance/metrics?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get performance metrics');
    }

    return response.json();
  }

  /**
   * 获取工程师绩效
   */
  async getEngineerPerformance(
    engineerId: string,
    periodType: string,
    periodStart: string
  ): Promise<PerformanceMetricsDto> {
    const queryParams = new URLSearchParams();
    queryParams.append('periodType', periodType);
    queryParams.append('periodStart', periodStart);

    const response = await fetch(
      `${API_BASE_URL}/api/performance/engineer/${engineerId}?${queryParams}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('Performance metrics not found');
      }
      throw new Error('Failed to get engineer performance');
    }

    return response.json();
  }

  /**
   * 获取团队绩效
   */
  async getTeamPerformance(params: {
    departmentId?: string;
    periodType: string;
    periodStart: string;
  }): Promise<PerformanceMetricsDto[]> {
    const queryParams = new URLSearchParams();
    if (params.departmentId) queryParams.append('departmentId', params.departmentId);
    queryParams.append('periodType', params.periodType);
    queryParams.append('periodStart', params.periodStart);

    const response = await fetch(`${API_BASE_URL}/api/performance/team?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get team performance');
    }

    return response.json();
  }

  /**
   * 获取绩效排名
   */
  async getRanking(params: {
    periodType: string;
    periodStart: string;
    departmentId?: string;
  }): Promise<PerformanceRankingDto[]> {
    const queryParams = new URLSearchParams();
    queryParams.append('periodType', params.periodType);
    queryParams.append('periodStart', params.periodStart);
    if (params.departmentId) queryParams.append('departmentId', params.departmentId);

    const response = await fetch(`${API_BASE_URL}/api/performance/ranking?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get performance ranking');
    }

    return response.json();
  }

  /**
   * 获取绩效趋势
   */
  async getTrends(params: {
    engineerId: string;
    periodType: string;
    fromDate: string;
    toDate: string;
  }): Promise<PerformanceTrendDto[]> {
    const queryParams = new URLSearchParams();
    queryParams.append('engineerId', params.engineerId);
    queryParams.append('periodType', params.periodType);
    queryParams.append('fromDate', params.fromDate);
    queryParams.append('toDate', params.toDate);

    const response = await fetch(`${API_BASE_URL}/api/performance/trends?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get performance trends');
    }

    return response.json();
  }

  /**
   * 手动触发绩效计算
   */
  async calculatePerformance(request: CalculatePerformanceRequest): Promise<{
    success: boolean;
    metrics: PerformanceMetricsDto;
  }> {
    const response = await fetch(`${API_BASE_URL}/api/performance/calculate`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to calculate performance');
    }

    return response.json();
  }
}

export const performanceService = new PerformanceService();


