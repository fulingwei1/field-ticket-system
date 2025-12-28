/**
 * 工单搜索服务
 */
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface SearchRequest {
  query?: string;
  searchField?: string;
  statuses?: string[];
  domain?: string;
  priority?: string;
  customerId?: string;
  deviceSn?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

export interface TicketSearchResultItem {
  ticketId: string;
  ticketNo: string;
  status: string;
  domain?: string;
  stepCode?: string;
  symptomTitle?: string;
  symptomDetail?: string;
  priority?: string;
  customerName?: string;
  deviceSn?: string;
  createdByName?: string;
  createdAt: string;
  matches: string[];
  relevanceScore: number;
}

export interface SearchResult {
  items: TicketSearchResultItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

class TicketSearchService {
  /**
   * 搜索工单
   */
  async searchTickets(request: SearchRequest): Promise<SearchResult> {
    const params = new URLSearchParams();
    
    if (request.query) params.append('q', request.query);
    if (request.searchField) params.append('searchField', request.searchField);
    if (request.statuses && request.statuses.length > 0) {
      params.append('statuses', request.statuses.join(','));
    }
    if (request.domain) params.append('domain', request.domain);
    if (request.priority) params.append('priority', request.priority);
    if (request.customerId) params.append('customerId', request.customerId);
    if (request.deviceSn) params.append('deviceSn', request.deviceSn);
    if (request.dateFrom) params.append('dateFrom', request.dateFrom);
    if (request.dateTo) params.append('dateTo', request.dateTo);
    if (request.page) params.append('page', request.page.toString());
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());

    const response = await fetch(`${API_BASE_URL}/api/tickets/search?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '搜索失败' }));
      throw new Error(error.message || '搜索失败');
    }

    return response.json();
  }

  /**
   * 获取搜索建议
   */
  async getSearchSuggestions(query: string, limit: number = 10): Promise<string[]> {
    if (!query || query.length < 2) {
      return [];
    }

    const params = new URLSearchParams({
      q: query,
      limit: limit.toString(),
    });

    const response = await fetch(`${API_BASE_URL}/api/tickets/search/suggestions?${params.toString()}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      return [];
    }

    return response.json();
  }
}

export const ticketSearchService = new TicketSearchService();














