import request from '../utils/request';

/**
 * 问题域枚举
 */
export enum ProblemDomain {
  A = 'A', // 机械/动作
  B = 'B', // 电气/IO
  C = 'C', // PLC/程序
  D = 'D', // 测试/判定
  E = 'E', // 系统/环境
}

/**
 * 版本渠道
 */
export enum VersionChannel {
  Stable = 'Stable',
  Beta = 'Beta',
  Alpha = 'Alpha',
}

/**
 * 判断卡 DTO
 */
export interface JudgementCardDto {
  cardId: string;
  cardTitle: string;
  cardContent: string;
  domain: ProblemDomain;
  procedureSteps?: string[];
  versionChannel?: VersionChannel;
  plcVersionRange?: string;
  uiVersionRange?: string;
  hwVersionRange?: string;
  confidence: number;
  qualityScore?: number;
  usageCount: number;
  successRate?: number;
  isActive: boolean;
  createdBy: string;
  createdByName?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * 创建判断卡请求
 */
export interface CreateJudgementCardRequest {
  cardTitle: string;
  cardContent: string;
  domain: ProblemDomain;
  procedureSteps?: string[];
  versionChannel?: VersionChannel;
  plcVersionRange?: string;
  uiVersionRange?: string;
  hwVersionRange?: string;
  confidence: number;
}

/**
 * 更新判断卡请求
 */
export interface UpdateJudgementCardRequest {
  cardTitle?: string;
  cardContent?: string;
  domain?: ProblemDomain;
  procedureSteps?: string[];
  versionChannel?: VersionChannel;
  plcVersionRange?: string;
  uiVersionRange?: string;
  hwVersionRange?: string;
  confidence?: number;
  isActive?: boolean;
}

/**
 * 判断卡列表查询参数
 */
export interface JudgementCardListQuery {
  page?: number;
  pageSize?: number;
  searchQuery?: string;
  domain?: ProblemDomain;
  isActive?: boolean;
  minConfidence?: number;
  minQualityScore?: number;
  sortBy?: 'confidence' | 'qualityScore' | 'usageCount' | 'successRate' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
}

/**
 * 分页结果
 */
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

/**
 * 判断卡统计
 */
export interface JudgementCardStatistics {
  totalCards: number;
  activeCards: number;
  domainDistribution: Record<ProblemDomain, number>;
  averageConfidence: number;
  averageQualityScore: number;
  totalUsageCount: number;
}

/**
 * 判断卡使用记录
 */
export interface CardUsageRecord {
  usageId: string;
  cardId: string;
  ticketId: string;
  ticketNumber?: string;
  engineerId: string;
  engineerName?: string;
  wasSuccessful: boolean;
  feedback?: string;
  usedAt: string;
}

/**
 * 判断卡管理服务
 */
class JudgementCardService {
  private readonly baseUrl = '/api/judgement-cards';

  /**
   * 获取判断卡列表
   */
  async getCards(query: JudgementCardListQuery = {}): Promise<PagedResult<JudgementCardDto>> {
    const params = new URLSearchParams();

    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.domain) params.append('domain', query.domain);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());
    if (query.minConfidence) params.append('minConfidence', query.minConfidence.toString());
    if (query.minQualityScore) params.append('minQualityScore', query.minQualityScore.toString());
    if (query.sortBy) params.append('sortBy', query.sortBy);
    if (query.sortOrder) params.append('sortOrder', query.sortOrder);

    return request.get(`${this.baseUrl}?${params.toString()}`);
  }

  /**
   * 获取单个判断卡详情
   */
  async getCardById(cardId: string): Promise<JudgementCardDto> {
    return request.get(`${this.baseUrl}/${cardId}`);
  }

  /**
   * 创建判断卡
   */
  async createCard(data: CreateJudgementCardRequest): Promise<JudgementCardDto> {
    return request.post(this.baseUrl, data);
  }

  /**
   * 更新判断卡
   */
  async updateCard(cardId: string, data: UpdateJudgementCardRequest): Promise<JudgementCardDto> {
    return request.put(`${this.baseUrl}/${cardId}`, data);
  }

  /**
   * 删除判断卡
   */
  async deleteCard(cardId: string): Promise<void> {
    return request.delete(`${this.baseUrl}/${cardId}`);
  }

  /**
   * 复制判断卡
   */
  async copyCard(cardId: string, newTitle?: string): Promise<JudgementCardDto> {
    return request.post(`${this.baseUrl}/${cardId}/copy`, {
      newTitle,
    });
  }

  /**
   * 批量更新判断卡状态
   */
  async batchUpdateStatus(cardIds: string[], isActive: boolean): Promise<void> {
    return request.post(`${this.baseUrl}/batch/update-status`, {
      cardIds,
      isActive,
    });
  }

  /**
   * 获取判断卡使用记录
   */
  async getCardUsageHistory(
    cardId: string,
    page = 1,
    pageSize = 20
  ): Promise<PagedResult<CardUsageRecord>> {
    return request.get(`${this.baseUrl}/${cardId}/usage-history?page=${page}&pageSize=${pageSize}`);
  }

  /**
   * 获取判断卡统计信息
   */
  async getStatistics(): Promise<JudgementCardStatistics> {
    return request.get(`${this.baseUrl}/statistics`);
  }

  /**
   * 推荐判断卡（基于工单特征）
   */
  async recommendCards(ticketId: string, limit = 5): Promise<JudgementCardDto[]> {
    return request.post(`${this.baseUrl}/recommend`, {
      ticketId,
      limit,
    });
  }

  /**
   * 评估判断卡质量
   */
  async evaluateCardQuality(cardId: string): Promise<{
    qualityScore: number;
    factors: Record<string, number>;
    suggestions: string[];
  }> {
    return request.post(`${this.baseUrl}/${cardId}/evaluate-quality`);
  }

  /**
   * 更新判断卡使用反馈
   */
  async submitUsageFeedback(
    cardId: string,
    ticketId: string,
    wasSuccessful: boolean,
    feedback?: string
  ): Promise<void> {
    return request.post(`${this.baseUrl}/${cardId}/usage-feedback`, {
      ticketId,
      wasSuccessful,
      feedback,
    });
  }

  /**
   * 导出判断卡
   */
  async exportCards(query: JudgementCardListQuery = {}): Promise<Blob> {
    const params = new URLSearchParams();

    if (query.searchQuery) params.append('searchQuery', query.searchQuery);
    if (query.domain) params.append('domain', query.domain);
    if (query.isActive !== undefined) params.append('isActive', query.isActive.toString());

    return request.get(`${this.baseUrl}/export?${params.toString()}`, {
      responseType: 'blob',
    });
  }

  /**
   * 导入判断卡
   */
  async importCards(file: File): Promise<{
    successCount: number;
    failCount: number;
    errors?: string[];
  }> {
    const formData = new FormData();
    formData.append('file', file);

    return request.post(`${this.baseUrl}/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }
}

export const judgementCardService = new JudgementCardService();
export default judgementCardService;
