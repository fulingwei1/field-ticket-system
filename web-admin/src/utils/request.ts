import { message as antMessage } from 'antd';
import { authService } from '../services/authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

/**
 * HTTP 错误类
 */
export class HttpError extends Error {
  constructor(
    message: string,
    public status: number,
    public statusText: string,
    public data?: any
  ) {
    super(message);
    this.name = 'HttpError';
  }
}

/**
 * 网络错误类
 */
export class NetworkError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'NetworkError';
  }
}

interface RequestOptions extends RequestInit {
  responseType?: 'json' | 'blob' | 'text';
  showError?: boolean; // 是否自动显示错误提示
  retryCount?: number; // 重试次数
}

/**
 * 统一的请求工具
 * 封装 fetch API，自动处理认证、错误和重试
 */
class Request {
  private defaultOptions: RequestOptions = {
    showError: true,
    retryCount: 0,
  };

  /**
   * 获取请求头
   */
  private getHeaders(customHeaders?: HeadersInit): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...customHeaders,
    };

    const token = authService.getToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    return headers;
  }

  /**
   * 处理响应错误
   */
  private async handleError(response: Response, options: RequestOptions): Promise<never> {
    let errorMessage = '请求失败';
    let errorData: any;

    try {
      errorData = await response.json();
      errorMessage = errorData.message || errorData.error || errorMessage;
    } catch {
      errorMessage = response.statusText || errorMessage;
    }

    // 处理特定状态码
    if (response.status === 401) {
      errorMessage = '登录已过期，请重新登录';
      authService.clearAuth();
      window.location.href = '/login';
    } else if (response.status === 403) {
      errorMessage = '没有权限访问该资源';
    } else if (response.status === 404) {
      errorMessage = '请求的资源不存在';
    } else if (response.status === 500) {
      errorMessage = '服务器内部错误';
    } else if (response.status === 502 || response.status === 503) {
      errorMessage = '服务暂时不可用，请稍后再试';
    }

    // 自动显示错误提示
    if (options.showError !== false) {
      antMessage.error(errorMessage);
    }

    throw new HttpError(errorMessage, response.status, response.statusText, errorData);
  }

  /**
   * 处理响应
   */
  private async handleResponse<T>(response: Response, options: RequestOptions): Promise<T> {
    if (!response.ok) {
      return this.handleError(response, options);
    }

    const responseType = options.responseType || 'json';

    if (responseType === 'blob') {
      return response.blob() as any;
    } else if (responseType === 'text') {
      return response.text() as any;
    } else {
      const data = await response.json();
      return data;
    }
  }

  /**
   * 执行请求（支持重试）
   */
  private async executeRequest<T>(
    url: string,
    options: RequestOptions,
    retryCount = 0
  ): Promise<T> {
    try {
      const response = await fetch(url, options);
      return this.handleResponse<T>(response, options);
    } catch (error) {
      // 网络错误或超时，可以重试
      if (
        error instanceof TypeError &&
        retryCount < (options.retryCount || 0)
      ) {
        console.warn(`请求失败，正在重试 (${retryCount + 1}/${options.retryCount})...`);
        await new Promise((resolve) => setTimeout(resolve, 1000 * (retryCount + 1)));
        return this.executeRequest(url, options, retryCount + 1);
      }

      // 如果是 HttpError，直接抛出
      if (error instanceof HttpError) {
        throw error;
      }

      // 网络错误
      const networkError = new NetworkError(
        error instanceof Error ? error.message : '网络连接失败，请检查网络设置'
      );

      if (options.showError !== false) {
        antMessage.error('网络连接失败，请检查网络设置');
      }

      throw networkError;
    }
  }

  /**
   * GET 请求
   */
  async get<T = any>(url: string, params?: any, options?: RequestOptions): Promise<T> {
    let fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    // 处理查询参数
    if (params && typeof params === 'object') {
      const queryParams = new URLSearchParams();
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          queryParams.append(key, String(value));
        }
      });
      if (queryParams.toString()) {
        fullUrl += (fullUrl.includes('?') ? '&' : '?') + queryParams.toString();
      }
    }

    return this.executeRequest<T>(fullUrl, {
      method: 'GET',
      headers: this.getHeaders(options?.headers),
      ...this.defaultOptions,
      ...options,
    });
  }

  /**
   * POST 请求
   */
  async post<T = any>(url: string, body?: any, options?: RequestOptions): Promise<T> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    // 处理 FormData
    const isFormData = body instanceof FormData;
    const headers = this.getHeaders(options?.headers);
    if (isFormData) {
      delete (headers as any)['Content-Type']; // Let browser set Content-Type with boundary
    }

    return this.executeRequest<T>(fullUrl, {
      method: 'POST',
      headers,
      body: isFormData ? body : body ? JSON.stringify(body) : undefined,
      ...this.defaultOptions,
      ...options,
    });
  }

  /**
   * PUT 请求
   */
  async put<T = any>(url: string, body?: any, options?: RequestOptions): Promise<T> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    return this.executeRequest<T>(fullUrl, {
      method: 'PUT',
      headers: this.getHeaders(options?.headers),
      body: body ? JSON.stringify(body) : undefined,
      ...this.defaultOptions,
      ...options,
    });
  }

  /**
   * PATCH 请求
   */
  async patch<T = any>(url: string, body?: any, options?: RequestOptions): Promise<T> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    return this.executeRequest<T>(fullUrl, {
      method: 'PATCH',
      headers: this.getHeaders(options?.headers),
      body: body ? JSON.stringify(body) : undefined,
      ...this.defaultOptions,
      ...options,
    });
  }

  /**
   * DELETE 请求
   */
  async delete<T = any>(url: string, options?: RequestOptions): Promise<T> {
    const fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`;

    return this.executeRequest<T>(fullUrl, {
      method: 'DELETE',
      headers: this.getHeaders(options?.headers),
      ...this.defaultOptions,
      ...options,
    });
  }
}

export const request = new Request();
export default request;
