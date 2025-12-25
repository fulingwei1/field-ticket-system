/**
 * 认证服务
 */
import { request } from '../utils/request';
import { STORAGE_KEYS } from '../utils/constants';
import type { UserInfo } from '../types';

export class AuthService {
  /**
   * 企业微信小程序登录
   */
  async login(): Promise<string> {
    try {
      // 1. 获取企业微信code
      const loginRes = await new Promise<WechatMiniprogram.LoginSuccessCallbackResult>((resolve, reject) => {
        wx.qy.login({
          success: resolve,
          fail: reject,
        });
      });

      // 2. 调用后端接口
      const response = await request.post<{
        token: string;
        refreshToken: string;
        expiresIn: number;
        user: UserInfo;
      }>('/api/auth/wecom/miniprogram-login', {
        code: loginRes.code,
      });

      if (response.success && response.data) {
        // 3. 存储Token和用户信息
        wx.setStorageSync(STORAGE_KEYS.TOKEN, response.data.token);
        wx.setStorageSync(STORAGE_KEYS.REFRESH_TOKEN, response.data.refreshToken);
        wx.setStorageSync(STORAGE_KEYS.USER_INFO, JSON.stringify(response.data.user));
        
        return response.data.token;
      } else {
        throw new Error(response.message || '登录失败');
      }
    } catch (error: any) {
      console.error('Login error:', error);
      throw error;
    }
  }

  /**
   * 获取当前用户信息
   */
  async getCurrentUser(): Promise<UserInfo | null> {
    try {
      const response = await request.get<UserInfo>('/api/auth/me');
      
      if (response.success && response.data) {
        // 保存用户信息
        wx.setStorageSync(STORAGE_KEYS.USER_INFO, JSON.stringify(response.data));
        return response.data;
      }
      
      return null;
    } catch (error) {
      console.error('Get current user error:', error);
      return null;
    }
  }

  /**
   * 刷新Token
   */
  async refreshToken(): Promise<string> {
    const refreshToken = wx.getStorageSync(STORAGE_KEYS.REFRESH_TOKEN);
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    try {
      const response = await request.post<{
        token: string;
        refreshToken: string;
        expiresIn: number;
      }>('/api/auth/refresh', {
        refreshToken,
      });

      if (response.success && response.data) {
        // 保存新Token
        wx.setStorageSync(STORAGE_KEYS.TOKEN, response.data.token);
        wx.setStorageSync(STORAGE_KEYS.REFRESH_TOKEN, response.data.refreshToken);
        
        return response.data.token;
      } else {
        throw new Error(response.message || '刷新Token失败');
      }
    } catch (error: any) {
      // 刷新失败，清除认证信息
      this.clearAuth();
      throw error;
    }
  }

  /**
   * 获取Token
   */
  getToken(): string | null {
    return wx.getStorageSync(STORAGE_KEYS.TOKEN) || null;
  }

  /**
   * 获取用户信息
   */
  getUser(): UserInfo | null {
    const userStr = wx.getStorageSync(STORAGE_KEYS.USER_INFO);
    if (!userStr) {
      return null;
    }
    
    try {
      return JSON.parse(userStr) as UserInfo;
    } catch {
      return null;
    }
  }

  /**
   * 清除认证信息
   */
  clearAuth(): void {
    wx.removeStorageSync(STORAGE_KEYS.TOKEN);
    wx.removeStorageSync(STORAGE_KEYS.REFRESH_TOKEN);
    wx.removeStorageSync(STORAGE_KEYS.USER_INFO);
  }

  /**
   * 检查是否已登录
   */
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  /**
   * 退出登录
   */
  async logout(): Promise<void> {
    this.clearAuth();
    wx.reLaunch({
      url: '/pages/login/login',
    });
  }
}

export const authService = new AuthService();

