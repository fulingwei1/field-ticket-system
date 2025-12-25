/**
 * 请求封装
 */
import { API_BASE_URL, PLATFORM, STORAGE_KEYS } from './constants';

export interface RequestOptions {
  url: string;
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  data?: any;
  header?: Record<string, string>;
  showLoading?: boolean;
  loadingText?: string;
}

export interface ResponseData<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  code?: number;
}

class Request {
  private baseURL = API_BASE_URL;

  /**
   * 发送请求
   */
  async request<T = any>(options: RequestOptions): Promise<ResponseData<T>> {
    const { url, method = 'GET', data, header = {}, showLoading = true, loadingText = '加载中...' } = options;

    // 显示加载提示
    if (showLoading) {
      wx.showLoading({
        title: loadingText,
        mask: true,
      });
    }

    try {
      // 获取Token
      const token = wx.getStorageSync(STORAGE_KEYS.TOKEN);

      // 构建请求头
      const requestHeader: Record<string, string> = {
        'Content-Type': 'application/json',
        'X-Platform': PLATFORM,
        ...header,
      };

      if (token) {
        requestHeader['Authorization'] = `Bearer ${token}`;
      }

      // 发送请求
      const response = await new Promise<WechatMiniprogram.RequestSuccessCallbackResult>((resolve, reject) => {
        wx.request({
          url: `${this.baseURL}${url}`,
          method: method as any,
          data: data,
          header: requestHeader,
          success: resolve,
          fail: reject,
        });
      });

      // 隐藏加载提示
      if (showLoading) {
        wx.hideLoading();
      }

      // 处理响应
      if (response.statusCode === 200) {
        const result = response.data as ResponseData<T>;
        
        // 如果返回成功，直接返回数据
        if (result.success !== false) {
          return result;
        }
        
        // 处理业务错误
        this.handleError(result.message || '请求失败');
        return result;
      } else if (response.statusCode === 401) {
        // Token 过期，尝试刷新
        await this.handleTokenExpired();
        // 重试请求
        return this.request(options);
      } else {
        // HTTP 错误
        this.handleError(`请求失败: ${response.statusCode}`);
        return {
          success: false,
          message: `请求失败: ${response.statusCode}`,
        };
      }
    } catch (error: any) {
      // 隐藏加载提示
      if (showLoading) {
        wx.hideLoading();
      }

      // 处理错误
      const errorMsg = error.errMsg || error.message || '网络错误';
      this.handleError(errorMsg);
      return {
        success: false,
        message: errorMsg,
      };
    }
  }

  /**
   * GET 请求
   */
  async get<T = any>(url: string, data?: any, options?: Omit<RequestOptions, 'url' | 'method' | 'data'>): Promise<ResponseData<T>> {
    return this.request<T>({
      url,
      method: 'GET',
      data,
      ...options,
    });
  }

  /**
   * POST 请求
   */
  async post<T = any>(url: string, data?: any, options?: Omit<RequestOptions, 'url' | 'method' | 'data'>): Promise<ResponseData<T>> {
    return this.request<T>({
      url,
      method: 'POST',
      data,
      ...options,
    });
  }

  /**
   * PUT 请求
   */
  async put<T = any>(url: string, data?: any, options?: Omit<RequestOptions, 'url' | 'method' | 'data'>): Promise<ResponseData<T>> {
    return this.request<T>({
      url,
      method: 'PUT',
      data,
      ...options,
    });
  }

  /**
   * DELETE 请求
   */
  async delete<T = any>(url: string, data?: any, options?: Omit<RequestOptions, 'url' | 'method' | 'data'>): Promise<ResponseData<T>> {
    return this.request<T>({
      url,
      method: 'DELETE',
      data,
      ...options,
    });
  }

  /**
   * 处理Token过期
   */
  private async handleTokenExpired(): Promise<void> {
    const refreshToken = wx.getStorageSync(STORAGE_KEYS.REFRESH_TOKEN);
    if (!refreshToken) {
      // 没有刷新Token，跳转到登录页
      this.redirectToLogin();
      return;
    }

    try {
      // 尝试刷新Token
      const response = await this.post<{ token: string; refreshToken: string; expiresIn: number }>('/api/auth/refresh', {
        refreshToken,
      });

      if (response.success && response.data) {
        // 保存新Token
        wx.setStorageSync(STORAGE_KEYS.TOKEN, response.data.token);
        wx.setStorageSync(STORAGE_KEYS.REFRESH_TOKEN, response.data.refreshToken);
      } else {
        // 刷新失败，跳转到登录页
        this.redirectToLogin();
      }
    } catch (error) {
      // 刷新失败，跳转到登录页
      this.redirectToLogin();
    }
  }

  /**
   * 跳转到登录页
   */
  private redirectToLogin(): void {
    wx.removeStorageSync(STORAGE_KEYS.TOKEN);
    wx.removeStorageSync(STORAGE_KEYS.REFRESH_TOKEN);
    wx.removeStorageSync(STORAGE_KEYS.USER_INFO);
    
    wx.reLaunch({
      url: '/pages/login/login',
    });
  }

  /**
   * 处理错误
   */
  private handleError(message: string): void {
    console.error('Request error:', message);
    wx.showToast({
      title: message,
      icon: 'none',
      duration: 2000,
    });
  }
}

export const request = new Request();

