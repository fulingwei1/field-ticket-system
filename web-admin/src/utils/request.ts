import { authService } from '../services/authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

/**
 * 统一的请求工具
 * 封装 fetch API，自动处理认证和错误
 */
class Request {
  /**
   * 获取请求头
   */
  private buildHeaders(token?: string | null, extraHeaders?: HeadersInit): HeadersInit {
    return {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(extraHeaders || {}),
    };
  }

  private async tryRefreshToken(): Promise<string | null> {
    try {
      const refreshed = await authService.refreshToken();
      return refreshed.token;
    } catch {
      return null;
    }
  }

  private async fetchWithAuth(
    url: string,
    options: RequestInit,
    allowRefresh: boolean = true
  ): Promise<Response> {
    const token = authService.getToken();
    const response = await fetch(url, {
      ...options,
      headers: this.buildHeaders(token, options.headers),
    });

    if (response.status !== 401 || !allowRefresh) {
      return response;
    }

    const refreshedToken = await this.tryRefreshToken();
    if (!refreshedToken) {
      authService.clearAuth();
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
      return response;
    }

    return fetch(url, {
      ...options,
      headers: this.buildHeaders(refreshedToken, options.headers),
    });
  }

  /**
   * GET 请求
   */
  async get<T>(url: string, params?: Record<string, any>): Promise<{ data: T }> {
    let fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;
    
    if (params) {
      const queryParams = new URLSearchParams();
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
      if (queryParams.toString()) {
        fullUrl += `?${queryParams.toString()}`;
      }
    }

    const response = await this.fetchWithAuth(fullUrl, {
      method: 'GET',
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return { data };
  }

  /**
   * POST 请求
   */
  async post<T>(url: string, body?: any): Promise<{ data: T }> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    const response = await this.fetchWithAuth(fullUrl, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return { data };
  }

  /**
   * PUT 请求
   */
  async put<T>(url: string, body?: any): Promise<{ data: T }> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    const response = await this.fetchWithAuth(fullUrl, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return { data };
  }

  /**
   * DELETE 请求
   */
  async delete<T>(url: string): Promise<{ data: T }> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    const response = await this.fetchWithAuth(fullUrl, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    return { data };
  }
}

export const request = new Request();













