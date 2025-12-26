/**
 * 判断卡质量评分服务
 */
import { request } from '../utils/request';

export interface JudgementCardQualityScoreDto {
  jcCode: string;
  title: string;
  completenessScore: number;
  logicConsistencyScore: number;
  verifiabilityScore: number;
  evidenceScore: number;
  totalScore: number;
  level: 'Poor' | 'Fair' | 'Good' | 'Excellent';
  issues: QualityIssueDto[];
  scoredAt: string;
}

export interface QualityIssueDto {
  type: string;
  description: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  suggestion?: string;
}

export interface BatchScoreRequest {
  jcCodes: string[];
}

class JudgementCardQualityService {
  /**
   * 获取判断卡质量评分
   */
  async getQualityScore(jcCode: string): Promise<JudgementCardQualityScoreDto> {
    const response = await request.get<JudgementCardQualityScoreDto>(
      `/api/judgement-cards/quality/${jcCode}/score`
    );
    return response.data;
  }

  /**
   * 批量评分判断卡
   */
  async batchScore(jcCodes: string[]): Promise<JudgementCardQualityScoreDto[]> {
    const response = await request.post<JudgementCardQualityScoreDto[]>(
      '/api/judgement-cards/quality/batch-score',
      { jcCodes }
    );
    return response.data;
  }

  /**
   * 获取质量问题列表
   */
  async getQualityIssues(jcCode: string): Promise<QualityIssueDto[]> {
    const response = await request.get<QualityIssueDto[]>(
      `/api/judgement-cards/quality/${jcCode}/issues`
    );
    return response.data;
  }
}

export const judgementCardQualityService = new JudgementCardQualityService();











