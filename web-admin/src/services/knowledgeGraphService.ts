import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

export interface KnowledgeNodeDto {
  nodeId: string;
  nodeType: string;
  nodeCode: string;
  nodeName: string;
  properties?: any;
  createdAt: string;
}

export interface KnowledgeRelationDto {
  edgeId: string;
  sourceNodeId: string;
  sourceNodeName: string;
  targetNodeId: string;
  targetNodeName: string;
  edgeType: string;
  weight: number;
  metadata?: any;
}

export interface KnowledgeGraphDto {
  nodes: KnowledgeNodeDto[];
  edges: KnowledgeRelationDto[];
  totalNodes: number;
  totalEdges: number;
}

class KnowledgeGraphService {
  /**
   * 构建知识图谱
   */
  async buildKnowledgeGraph(): Promise<KnowledgeGraphDto> {
    const response = await fetch(`${API_BASE_URL}/api/knowledge-graph/build`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to build knowledge graph');
    }

    return response.json();
  }

  /**
   * 知识检索
   */
  async searchKnowledge(query: string, topK: number = 10, nodeType?: string): Promise<KnowledgeNodeDto[]> {
    const queryParams = new URLSearchParams();
    queryParams.append('query', query);
    queryParams.append('topK', topK.toString());
    if (nodeType) queryParams.append('nodeType', nodeType);

    const response = await fetch(`${API_BASE_URL}/api/knowledge-graph/search?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to search knowledge');
    }

    return response.json();
  }

  /**
   * 知识推荐
   */
  async recommendKnowledge(ticketId: string): Promise<KnowledgeNodeDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/knowledge-graph/recommend?ticketId=${ticketId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to recommend knowledge');
    }

    return response.json();
  }

  /**
   * 获取知识关系
   */
  async getKnowledgeRelations(nodeId?: string): Promise<KnowledgeRelationDto[]> {
    const queryParams = new URLSearchParams();
    if (nodeId) queryParams.append('nodeId', nodeId);

    const response = await fetch(`${API_BASE_URL}/api/knowledge-graph/relations?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get knowledge relations');
    }

    return response.json();
  }

  /**
   * 挖掘知识关系
   */
  async mineKnowledgeRelations(): Promise<KnowledgeRelationDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/knowledge-graph/mine-relations`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to mine knowledge relations');
    }

    return response.json();
  }
}

export const knowledgeGraphService = new KnowledgeGraphService();

