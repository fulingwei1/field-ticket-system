/**
 * 统一的 HTTP 客户端
 * 
 * 所有需要认证的 API 请求都应该使用此客户端，确保：
 * 1. Authorization 头始终被正确添加
 * 2. 统一的错误处理
 * 3. Token 刷新机制
 * 
 * 使用示例：
 * ```typescript
 * import { httpClient } from '../utils/httpClient';
 * 
 * const data = await httpClient.get<CustomerDto[]>('/api/customers');
 * const result = await httpClient.post('/api/tickets', { ... });
 * ```
 */
import { authService } from '../services/authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

class HttpClient {
  /**
   * 基础请求方法
   */
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    // 始终使用 authService.getAuthHeaders() 获取认证头
    const headers = authService.getAuthHeaders();
    
    // 合并用户提供的 headers
    const finalHeaders: HeadersInit = {
      ...headers,
      ...(options.headers as HeadersInit),
    };

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: finalHeaders,
    });

    // 处理 401 错误（token 过期）
    if (response.status === 401) {
      try {
        // 尝试刷新 token
        await authService.refreshToken();
        const retryHeaders = authService.getAuthHeaders();
        const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
          ...options,
          headers: {
            ...retryHeaders,
            ...(options.headers as HeadersInit),
          },
        });

        if (!retryResponse.ok) {
          if (retryResponse.status === 401) {
            // 刷新失败，清除认证信息并跳转登录
            authService.clearAuth();
            if (typeof window !== 'undefined') {
              window.location.href = '/login';
            }
            throw new Error('认证失败，请重新登录');
          }
          const error = await retryResponse.json().catch(() => ({ message: retryResponse.statusText }));
          throw new Error(error.message || `HTTP error! status: ${retryResponse.status}`);
        }

        return retryResponse.json();
      } catch (error: any) {
        if (error.message === '认证失败，请重新登录') {
          throw error;
        }
        authService.clearAuth();
        if (typeof window !== 'undefined') {
          window.location.href = '/login';
        }
        throw new Error('认证失败，请重新登录');
      }
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      const errorMessage = error.message || error.detail || error.title || `HTTP error! status: ${response.status}`;
      const errorWithStatus = new Error(errorMessage);
      (errorWithStatus as any).status = response.status;
      throw errorWithStatus;
    }

    return response.json();
  }

  /**
   * GET 请求
   */
  async get<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'GET',
    });
  }

  /**
   * POST 请求
   */
  async post<T>(endpoint: string, data?: any, options: RequestInit = {}): Promise<T> {
    // 如果是 FormData，直接使用，否则序列化为 JSON
    const body = data instanceof FormData ? data : (data ? JSON.stringify(data) : undefined);
    
    // 准备请求选项
    const requestOptions: RequestInit = {
      ...options,
      method: 'POST',
      body,
    };
    
    // 如果是 FormData，不要设置 Content-Type，让浏览器自动设置（包括 boundary）
    // 否则设置 JSON Content-Type
    if (!(data instanceof FormData) && data) {
      requestOptions.headers = {
        'Content-Type': 'application/json',
        ...(options.headers as HeadersInit),
      };
    }
    
    return this.request<T>(endpoint, requestOptions);
  }

  /**
   * PUT 请求
   */
  async put<T>(endpoint: string, data?: any, options: RequestInit = {}): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  /**
   * DELETE 请求
   */
  async delete<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'DELETE',
    });
  }

  /**
   * PATCH 请求
   */
  async patch<T>(endpoint: string, data?: any, options: RequestInit = {}): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  }
}

export const httpClient = new HttpClient();




