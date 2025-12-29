/**
 * 认证服务
 */
export interface WeComLoginUrlResponse {
  url: string;
  state: string;
}

export interface AuthResult {
  token: string;
  refreshToken: string;
  expiresIn: number;
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  name: string;
  mobile?: string;
  role: string;
  deptName?: string;
}

const API_BASE_URL = import.meta.env.VITE_API_URL || '';

class AuthService {
  private tokenKey = 'field_ticket_token';
  private refreshTokenKey = 'field_ticket_refresh_token';
  private userKey = 'field_ticket_user';

  /**
   * 获取企业微信登录URL
   */
  async getWeComLoginUrl(state?: string): Promise<WeComLoginUrlResponse> {
    const url = state
      ? `${API_BASE_URL}/api/auth/wecom/login-url?state=${state}`
      : `${API_BASE_URL}/api/auth/wecom/login-url`;

    const response = await fetch(url);
    if (!response.ok) {
      throw new Error('Failed to get WeCom login URL');
    }

    return response.json();
  }

  /**
   * 处理企业微信回调
   */
  async handleWeComCallback(code: string, state?: string): Promise<AuthResult> {
    const response = await fetch(`${API_BASE_URL}/api/auth/wecom/callback`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ code, state }),
    });

    if (!response.ok) {
      throw new Error('Failed to handle WeCom callback');
    }

    const result: AuthResult = await response.json();
    this.saveAuth(result);
    return result;
  }

  /**
   * 刷新Token
   */
  async refreshToken(): Promise<AuthResult> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      this.clearAuth();
      throw new Error('Failed to refresh token');
    }

    const result: AuthResult = await response.json();
    this.saveAuth(result);
    return result;
  }

  /**
   * 获取当前用户信息
   */
  async getCurrentUser(): Promise<UserInfo | null> {
    const token = this.getToken();
    if (!token) {
      return null;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        if (response.status === 401) {
          // Token 过期，尝试刷新
          try {
            await this.refreshToken();
            return this.getCurrentUser();
          } catch {
            this.clearAuth();
            return null;
          }
        }
        return null;
      }

      const user: UserInfo = await response.json();
      this.saveUser(user);
      return user;
    } catch (error) {
      console.error('Failed to get current user:', error);
      return null;
    }
  }

  /**
   * 保存认证信息
   */
  private saveAuth(result: AuthResult): void {
    localStorage.setItem(this.tokenKey, result.token);
    localStorage.setItem(this.refreshTokenKey, result.refreshToken);
    localStorage.setItem(this.userKey, JSON.stringify(result.user));
  }

  /**
   * 保存用户信息
   */
  private saveUser(user: UserInfo): void {
    localStorage.setItem(this.userKey, JSON.stringify(user));
  }

  /**
   * 获取Token
   */
  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  /**
   * 获取Refresh Token
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  /**
   * 获取用户信息
   */
  getUser(): UserInfo | null {
    const userStr = localStorage.getItem(this.userKey);
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
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.userKey);
  }

  /**
   * 检查是否已登录
   */
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  /**
   * 获取认证请求头
   */
  getAuthHeaders(extraHeaders: HeadersInit = {}): HeadersInit {
    const token = this.getToken();
    return {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...extraHeaders,
    };
  }

  /**
   * 账号密码登录
   */
  async loginWithPassword(username: string, password: string): Promise<AuthResult> {
    const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ username, password }),
    });

    if (!response.ok) {
      if (response.status === 401) {
        const error = await response.json().catch(() => ({ message: '用户名或密码错误' }));
        throw new Error(error.message || '用户名或密码错误');
      }
      throw new Error('登录失败，请稍后重试');
    }

    const result: AuthResult = await response.json();
    this.saveAuth(result);
    return result;
  }
}

export const authService = new AuthService();

