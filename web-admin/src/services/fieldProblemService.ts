import request from './request';

// 自动生成问题结果
export interface AutoGenerateProblemResult {
  success: boolean;
  problemId?: string;
  message?: string;
  isRepeatProblem: boolean;
  relatedHistoryProblemId?: string;
  similarityScore?: number;
}

// 问题热点
export interface ProblemHotspot {
  problemCategory: string;
  count: number;
  percentage: number;
  topSymptoms: string[];
  averageProcessingDays: number;
  repeatCount: number;
}

// 问题趋势
export interface ProblemTrend {
  date: string;
  totalCount: number;
  newCount: number;
  resolvedCount: number;
  repeatCount: number;
  averageProcessingDays: number;
}

// 问题统计请求
export interface ProblemStatisticsRequest {
  startDate?: string;
  endDate?: string;
  projectId?: string;
  problemCategory?: string;
  topN?: number;
}

// 问题统计响应
export interface ProblemStatisticsResponse {
  totalCount: number;
  resolvedCount: number;
  repeatProblemCount: number;
  repeatRate: number;
  averageProcessingDays: number;
  hotspots: ProblemHotspot[];
  trends: ProblemTrend[];
}

// 重复问题检测响应
export interface RepeatDetectionResponse {
  ticketId: string;
  isRepeat: boolean;
  relatedProblemId?: string;
  similarityScore: number;
}

class FieldProblemService {
  private baseUrl = '/api/field-problems';

  /**
   * 从工单自动生成问题记录
   */
  async generateFromTicket(ticketId: string): Promise<AutoGenerateProblemResult> {
    return request.post(`${this.baseUrl}/generate-from-ticket/${ticketId}`);
  }

  /**
   * 检测重复问题
   */
  async detectRepeat(
    ticketId: string,
    threshold?: number
  ): Promise<RepeatDetectionResponse> {
    const params = threshold !== undefined ? { threshold } : {};
    return request.post(`${this.baseUrl}/${ticketId}/detect-repeat`, null, { params });
  }

  /**
   * 获取问题统计
   */
  async getStatistics(req: ProblemStatisticsRequest): Promise<ProblemStatisticsResponse> {
    return request.post(`${this.baseUrl}/statistics`, req);
  }

  /**
   * 获取问题热点
   */
  async getHotspots(
    projectId?: string,
    topN?: number,
    days?: number
  ): Promise<ProblemHotspot[]> {
    const params: any = {};
    if (projectId) params.projectId = projectId;
    if (topN !== undefined) params.topN = topN;
    if (days !== undefined) params.days = days;

    return request.get(`${this.baseUrl}/hotspots`, { params });
  }
}

export default new FieldProblemService();
