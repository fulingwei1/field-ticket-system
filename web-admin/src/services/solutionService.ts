import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface CreateSolutionRequest {
  title: string;
  description?: string;
  solutionType: string;
  releaseType: string;
  requiredSwVersion?: string;
  requiredPlcVersion?: string;
  requiredParamVersion?: string;
  newSwVersion?: string;
  newPlcVersion?: string;
  newParamVersion?: string;
  changeDetailJson: Record<string, any>;
  verificationChecklistJson: Record<string, any>;
  implementationSteps?: string;
  estimatedImplementationTime?: number;
  riskLevel: string;
  riskDescription?: string;
  rollbackPossible: boolean;
  rollbackProcedure?: string;
}

export interface UpdateSolutionRequest {
  title?: string;
  description?: string;
  solutionType?: string;
  releaseType?: string;
  requiredSwVersion?: string;
  requiredPlcVersion?: string;
  requiredParamVersion?: string;
  newSwVersion?: string;
  newPlcVersion?: string;
  newParamVersion?: string;
  changeDetailJson?: Record<string, any>;
  verificationChecklistJson?: Record<string, any>;
  implementationSteps?: string;
  estimatedImplementationTime?: number;
  riskLevel?: string;
  riskDescription?: string;
  rollbackPossible?: boolean;
  rollbackProcedure?: string;
}

export interface SolutionDto {
  solutionId: string;
  solutionCode: string;
  ticketId: string;
  title: string;
  description?: string;
  solutionType: string;
  releaseType: string;
  requiredSwVersion?: string;
  requiredPlcVersion?: string;
  requiredParamVersion?: string;
  newSwVersion?: string;
  newPlcVersion?: string;
  newParamVersion?: string;
  changeDetailJson: Record<string, any>;
  verificationChecklistJson: Record<string, any>;
  implementationSteps?: string;
  estimatedImplementationTime?: number;
  riskLevel: string;
  riskDescription?: string;
  rollbackPossible: boolean;
  rollbackProcedure?: string;
  status: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  publishedBy?: string;
}

class SolutionService {
  /**
   * 创建解决方案
   */
  async createSolution(ticketId: string, request: CreateSolutionRequest): Promise<SolutionDto> {
    const response = await fetch(`${API_BASE_URL}/api/solutions/tickets/${ticketId}`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '创建解决方案失败' }));
      throw new Error(error.message || '创建解决方案失败');
    }

    return response.json();
  }

  /**
   * 更新解决方案
   */
  async updateSolution(solutionId: string, request: UpdateSolutionRequest): Promise<SolutionDto> {
    const response = await fetch(`${API_BASE_URL}/api/solutions/${solutionId}`, {
      method: 'PUT',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '更新解决方案失败' }));
      throw new Error(error.message || '更新解决方案失败');
    }

    return response.json();
  }

  /**
   * 发布解决方案
   */
  async publishSolution(solutionId: string): Promise<SolutionDto> {
    const response = await fetch(`${API_BASE_URL}/api/solutions/${solutionId}/publish`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '发布解决方案失败' }));
      throw new Error(error.message || '发布解决方案失败');
    }

    return response.json();
  }

  /**
   * 获取解决方案详情
   */
  async getSolution(solutionId: string): Promise<SolutionDto> {
    const response = await fetch(`${API_BASE_URL}/api/solutions/${solutionId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取解决方案详情失败');
    }

    return response.json();
  }

  /**
   * 获取工单的解决方案列表
   */
  async getTicketSolutions(ticketId: string): Promise<SolutionDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/solutions/tickets/${ticketId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取解决方案列表失败');
    }

    return response.json();
  }
}

export const solutionService = new SolutionService();


