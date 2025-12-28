import request from './request';

export interface OperationLogQueryRequest {
  page: number;
  pageSize: number;
  operationType?: string;
  operatorId?: string;
  result?: string;
  startTime?: string;
  endTime?: string;
  searchKeyword?: string;
}

export interface OperationLogDto {
  id: string;
  operationType: string;
  operationTypeDisplay: string;
  operatorId: string;
  operatorName: string;
  operatedAt: string;
  description: string;
  result: string;
  totalCount: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  sourceFileName?: string;
  ipAddress?: string;
}

export interface OperationLogQueryResult {
  total: number;
  items: OperationLogDto[];
}

export interface OperationLogDetailDto extends OperationLogDto {
  details: any;
  errorMessages: string[];
  userAgent?: string;
}

export interface OperationLogStatistics {
  totalOperations: number;
  byType: OperationTypeStats[];
  byResult: OperationResultStats[];
  last7Days: DailyStats[];
}

export interface OperationTypeStats {
  operationType: string;
  displayName: string;
  count: number;
}

export interface OperationResultStats {
  result: string;
  count: number;
}

export interface DailyStats {
  date: string;
  count: number;
}

class OperationLogService {
  private baseUrl = '/api/operation-logs';

  /**
   * 查询操作日志
   */
  async query(params: OperationLogQueryRequest): Promise<OperationLogQueryResult> {
    return request.post(`${this.baseUrl}/query`, params);
  }

  /**
   * 获取日志详情
   */
  async getDetail(logId: string): Promise<OperationLogDetailDto> {
    return request.get(`${this.baseUrl}/${logId}`);
  }

  /**
   * 获取统计信息
   */
  async getStatistics(startDate?: string, endDate?: string): Promise<OperationLogStatistics> {
    return request.get(`${this.baseUrl}/statistics`, {
      params: { startDate, endDate },
    });
  }

  /**
   * 清理旧日志
   */
  async cleanup(beforeDate: string): Promise<number> {
    return request.delete(`${this.baseUrl}/cleanup`, {
      params: { beforeDate },
    });
  }
}

export default new OperationLogService();
