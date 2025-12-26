import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface EngineerLoadStatDto {
  statId: string;
  engineerId: string;
  engineerName?: string;
  statDate: string;
  mentionedCount: number;
  escalationTakenCount: number;
  judgementReusedCount: number;
  lowConfidenceTakenCount: number;
  ticketsAssigned: number;
  ticketsClosed: number;
  totalLoadScore: number;
}

export interface EngineerLoadReportDto {
  engineerId: string;
  engineerName?: string;
  startDate: string;
  endDate: string;
  totalStats: EngineerLoadStatDto;
  dailyStats: EngineerLoadStatDto[];
  analysis: LoadAnalysisDto;
}

export interface TeamLoadDistributionDto {
  teamId?: string;
  teamName?: string;
  startDate: string;
  endDate: string;
  engineerStats: EngineerLoadStatDto[];
  distributionAnalysis: LoadDistributionAnalysisDto;
}

export interface LoadTrendDto {
  period: string;
  periodStart: string;
  periodEnd: string;
  totalLoadScore: number;
  mentionedCount: number;
  escalationTakenCount: number;
  judgementReusedCount: number;
  ticketsAssigned: number;
  ticketsClosed: number;
}

export interface LoadAnalysisDto {
  loadLevel: string; // low, normal, high, very_high
  loadScore: number;
  assessment?: string;
  suggestions: string[];
}

export interface LoadDistributionAnalysisDto {
  averageLoadScore: number;
  loadBalanceScore: number; // 0-100
  recommendations: string[];
}

class EngineerLoadStatService {
  /**
   * 获取个人负载报告
   */
  async getPersonalLoadReport(
    startDate?: string,
    endDate?: string
  ): Promise<EngineerLoadReportDto> {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await fetch(
      `${API_BASE_URL}/api/engineer-load-stats/personal?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取个人负载报告失败');
    }

    return response.json();
  }

  /**
   * 获取团队负载分布
   */
  async getTeamLoadDistribution(
    teamId?: string,
    startDate?: string,
    endDate?: string
  ): Promise<TeamLoadDistributionDto> {
    const params = new URLSearchParams();
    if (teamId) params.append('teamId', teamId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await fetch(
      `${API_BASE_URL}/api/engineer-load-stats/team?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取团队负载分布失败');
    }

    return response.json();
  }

  /**
   * 获取负载趋势
   */
  async getLoadTrend(
    engineerId?: string,
    periodType: string = 'daily',
    periods: number = 30
  ): Promise<LoadTrendDto[]> {
    const params = new URLSearchParams();
    if (engineerId) params.append('engineerId', engineerId);
    params.append('periodType', periodType);
    params.append('periods', periods.toString());

    const response = await fetch(
      `${API_BASE_URL}/api/engineer-load-stats/trend?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取负载趋势失败');
    }

    return response.json();
  }
}

export const engineerLoadStatService = new EngineerLoadStatService();








