import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface KnowledgeVersionDto {
  versionId: string;
  knowledgeId: string;
  knowledgeType: string;
  versionNumber: string;
  versionDescription?: string;
  content: any;
  createdBy?: string;
  createdByName?: string;
  createdAt: string;
  isCurrent: boolean;
  changeType?: string;
  changeReason?: string;
  changeSummary?: string;
}

export interface VersionComparisonDto {
  version1: KnowledgeVersionDto;
  version2: KnowledgeVersionDto;
  differences: VersionDifference[];
}

export interface VersionDifference {
  field: string;
  oldValue?: any;
  newValue?: any;
  changeType: string;
}

export interface ExpiredKnowledgeDto {
  knowledgeId: string;
  knowledgeType: string;
  knowledgeName: string;
  currentVersion: string;
  expiryDate?: string;
  daysSinceExpiry: number;
}

export interface CreateVersionRequest {
  knowledgeId: string;
  knowledgeType: string;
  content: any;
  changeReason: string;
  versionDescription?: string;
}

export interface RollbackVersionRequest {
  targetVersionId: string;
  changeReason: string;
}

class KnowledgeVersionService {
  /**
   * 创建版本
   */
  async createVersion(
    knowledgeId: string,
    knowledgeType: string,
    request: CreateVersionRequest
  ): Promise<KnowledgeVersionDto> {
    const response = await fetch(`${API_BASE_URL}/api/knowledge/${knowledgeId}/versions?knowledgeType=${knowledgeType}`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to create version');
    }

    return response.json();
  }

  /**
   * 获取版本历史
   */
  async getVersionHistory(knowledgeId: string, knowledgeType: string): Promise<KnowledgeVersionDto[]> {
    const response = await fetch(
      `${API_BASE_URL}/api/knowledge/${knowledgeId}/versions?knowledgeType=${knowledgeType}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get version history');
    }

    return response.json();
  }

  /**
   * 版本对比
   */
  async compareVersions(versionId1: string, versionId2: string): Promise<VersionComparisonDto> {
    const response = await fetch(
      `${API_BASE_URL}/api/knowledge/versions/${versionId1}/compare/${versionId2}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to compare versions');
    }

    return response.json();
  }

  /**
   * 版本回滚
   */
  async rollbackVersion(
    knowledgeId: string,
    knowledgeType: string,
    versionId: string,
    request: RollbackVersionRequest
  ): Promise<KnowledgeVersionDto> {
    const response = await fetch(
      `${API_BASE_URL}/api/knowledge/${knowledgeId}/versions/${versionId}/rollback?knowledgeType=${knowledgeType}`,
      {
        method: 'POST',
        headers: authService.getAuthHeaders(),
        body: JSON.stringify(request),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to rollback version');
    }

    return response.json();
  }

  /**
   * 检查过期知识
   */
  async checkExpiredKnowledge(): Promise<ExpiredKnowledgeDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/knowledge/expired`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to check expired knowledge');
    }

    return response.json();
  }
}

export const knowledgeVersionService = new KnowledgeVersionService();

