/**
 * 判断卡版本管理服务
 */
import { request } from '../utils/request';
import { JudgementCardDto } from './triageService';

export interface JudgementCardVersionDto {
  jcCode: string;
  version: number;
  title: string;
  description?: string;
  domain: string;
  isCurrent: boolean;
  createdAt: string;
  createdBy: string;
  changeReason?: string;
  parentJcId?: string;
}

export interface VersionComparisonDto {
  jcCode: string;
  version1: number;
  version2: number;
  changes: FieldChange[];
}

export interface FieldChange {
  field: string;
  oldValue?: string;
  newValue?: string;
  changeType: 'added' | 'removed' | 'modified';
}

export interface JudgementCardUsageHistoryDto {
  id: string;
  jcCode: string;
  jcVersion: number;
  ticketId: string;
  usedBy: string;
  usedAt: string;
  result?: 'correct' | 'incorrect' | 'partial';
  feedback?: string;
}

export interface CreateNewVersionRequest {
  updateRequest: UpdateJudgementCardRequest;
  changeReason: string; // 必须填写
}

export interface UpdateJudgementCardRequest {
  title?: string;
  description?: string;
  domain?: string;
  hypothesisTemplate?: string;
  nextActionTemplate?: string;
  symptomStructure?: Record<string, any>;
  troubleshootingPath?: Record<string, any>;
}

class JudgementCardVersionService {
  /**
   * 创建新版本（必须填写变更原因）
   */
  async createNewVersion(
    jcCode: string,
    request: CreateNewVersionRequest
  ): Promise<JudgementCardDto> {
    const response = await request.post<JudgementCardDto>(
      `/api/judgement-cards/${jcCode}/versions`,
      request
    );
    return response.data;
  }

  /**
   * 获取所有版本
   */
  async getVersions(jcCode: string): Promise<JudgementCardVersionDto[]> {
    const response = await request.get<JudgementCardVersionDto[]>(
      `/api/judgement-cards/${jcCode}/versions`
    );
    return response.data;
  }

  /**
   * 获取指定版本
   */
  async getVersion(jcCode: string, version: number): Promise<JudgementCardDto> {
    const response = await request.get<JudgementCardDto>(
      `/api/judgement-cards/${jcCode}/versions/${version}`
    );
    return response.data;
  }

  /**
   * 版本对比
   */
  async compareVersions(
    jcCode: string,
    version1: number,
    version2: number
  ): Promise<VersionComparisonDto> {
    const response = await request.get<VersionComparisonDto>(
      `/api/judgement-cards/${jcCode}/versions/compare`,
      { version1, version2 }
    );
    return response.data;
  }

  /**
   * 获取使用历史
   */
  async getUsageHistory(
    jcCode: string,
    version?: number
  ): Promise<JudgementCardUsageHistoryDto[]> {
    const url = version
      ? `/api/judgement-cards/${jcCode}/versions/${version}/usage-history`
      : `/api/judgement-cards/${jcCode}/versions/usage-history`;
    const response = await request.get<JudgementCardUsageHistoryDto[]>(url);
    return response.data;
  }
}

export const judgementCardVersionService = new JudgementCardVersionService();





















