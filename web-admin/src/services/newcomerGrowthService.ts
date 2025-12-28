import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface GrowthDataPointDto {
  date: string;
  judgementCardQualityScore: number;
  confidenceAccuracy: number;
  aiAdoptionRate: number;
  firstTimeResolutionRate: number;
  ticketsHandled: number;
  judgementCardsCreated: number;
}

export interface GrowthSummaryDto {
  averageJudgementCardQuality: number;
  averageConfidenceAccuracy: number;
  averageAiAdoptionRate: number;
  averageFirstTimeResolutionRate: number;
  totalTicketsHandled: number;
  totalJudgementCardsCreated: number;
  growthTrend: string; // improving, stable, declining
}

export interface NewcomerGrowthCurveDto {
  engineerId: string;
  engineerName?: string;
  startDate: string;
  endDate: string;
  dataPoints: GrowthDataPointDto[];
  summary: GrowthSummaryDto;
}

export interface TeamAverageGrowthCurveDto {
  teamId?: string;
  teamName?: string;
  startDate: string;
  endDate: string;
  averageDataPoints: GrowthDataPointDto[];
}

export interface GrowthMilestoneDto {
  milestoneType: string;
  title: string;
  description: string;
  achievedDate: string;
  score?: number;
}

export interface GrowthMetricsDto {
  engineerId: string;
  engineerName?: string;
  startDate: string;
  endDate: string;
  judgementCardQualityTrend: number;
  confidenceAccuracyTrend: number;
  aiAdoptionTrend: number;
  firstTimeResolutionTrend: number;
  overallGrowthAssessment: string;
  recommendations: string[];
}

class NewcomerGrowthService {
  /**
   * 获取个人成长曲线
   */
  async getPersonalGrowthCurve(
    engineerId?: string,
    startDate?: string,
    endDate?: string
  ): Promise<NewcomerGrowthCurveDto> {
    const params = new URLSearchParams();
    if (engineerId) params.append('engineerId', engineerId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await fetch(
      `${API_BASE_URL}/api/newcomer-growth/personal?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取个人成长曲线失败');
    }

    return response.json();
  }

  /**
   * 获取团队平均成长曲线
   */
  async getTeamAverageGrowthCurve(
    teamId?: string,
    startDate?: string,
    endDate?: string
  ): Promise<TeamAverageGrowthCurveDto> {
    const params = new URLSearchParams();
    if (teamId) params.append('teamId', teamId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await fetch(
      `${API_BASE_URL}/api/newcomer-growth/team-average?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取团队平均成长曲线失败');
    }

    return response.json();
  }

  /**
   * 获取成长里程碑
   */
  async getGrowthMilestones(engineerId?: string): Promise<GrowthMilestoneDto[]> {
    const params = new URLSearchParams();
    if (engineerId) params.append('engineerId', engineerId);

    const response = await fetch(
      `${API_BASE_URL}/api/newcomer-growth/milestones?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取成长里程碑失败');
    }

    return response.json();
  }

  /**
   * 计算成长指标
   */
  async calculateGrowthMetrics(
    engineerId?: string,
    startDate?: string,
    endDate?: string
  ): Promise<GrowthMetricsDto> {
    const params = new URLSearchParams();
    if (engineerId) params.append('engineerId', engineerId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await fetch(
      `${API_BASE_URL}/api/newcomer-growth/metrics?${params.toString()}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('计算成长指标失败');
    }

    return response.json();
  }
}

export const newcomerGrowthService = new NewcomerGrowthService();














