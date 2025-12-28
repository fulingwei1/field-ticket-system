import request from './request';

// 推荐解决方案请求
export interface RecommendSolutionsRequest {
  ticketId: string;
  topK?: number;
  includeExpired?: boolean;
  minSimilarityScore?: number;
}

// 解决方案统计信息
export interface SolutionStatistics {
  totalUsageCount: number;
  verificationCount: number;
  successCount: number;
  failureCount: number;
  successRate: number;
  daysSinceLastUse: number;
}

// 匹配原因
export interface SolutionMatchReason {
  reasonType: string;
  description: string;
  weight: number;
}

// 解决方案推荐结果
export interface SolutionRecommendationDto {
  solutionId: string;
  solutionCode: string;
  sourceTicketId: string;
  sourceTicketNo: string;
  title: string;
  description?: string;
  solutionType: string;
  releaseType: string;
  requiredSwVersion?: string;
  requiredPlcVersion?: string;
  newSwVersion?: string;
  newPlcVersion?: string;
  matchScore: number;
  matchReasons: SolutionMatchReason[];
  scoreBreakdown: Record<string, number>;
  statistics: SolutionStatistics;
  isVersionCompatible: boolean;
  versionCompatibilityMessage?: string;
  createdAt: string;
  publishedAt?: string;
  lastVerifiedAt?: string;
}

// 推荐解决方案响应
export interface RecommendSolutionsResponse {
  ticketId: string;
  ticketNo: string;
  recommendations: SolutionRecommendationDto[];
  totalCandidates: number;
  message?: string;
}

class SolutionRecommendationService {
  private baseUrl = '/api/solutions/recommendations';

  /**
   * 推荐解决方案
   */
  async recommendSolutions(req: RecommendSolutionsRequest): Promise<RecommendSolutionsResponse> {
    return request.post(this.baseUrl, req);
  }

  /**
   * 获取解决方案统计信息
   */
  async getStatistics(solutionId: string): Promise<SolutionStatistics> {
    return request.get(`${this.baseUrl}/${solutionId}/statistics`);
  }

  /**
   * 检查版本兼容性
   */
  async checkCompatibility(solutionId: string, ticketId: string): Promise<{
    solutionId: string;
    ticketId: string;
    isCompatible: boolean;
    message?: string;
  }> {
    return request.post(`${this.baseUrl}/check-compatibility`, {
      solutionId,
      ticketId,
    });
  }

  /**
   * 计算工单相似度
   */
  async calculateSimilarity(ticketId1: string, ticketId2: string): Promise<{
    ticketId1: string;
    ticketId2: string;
    similarityScore: number;
  }> {
    return request.post(`${this.baseUrl}/calculate-similarity`, {
      ticketId1,
      ticketId2,
    });
  }
}

export default new SolutionRecommendationService();
