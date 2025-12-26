import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface UserProfileDto {
  profileId: string;
  userId: string;
  userName?: string;
  commonFields: Record<string, any>;
  expertiseLevel?: 'beginner' | 'intermediate' | 'expert';
  expertiseScore?: number;
  totalTickets: number;
  averageCompletionTime?: number;
  commonMistakes?: Record<string, any>;
  updatedAt: string;
}

export interface PreFillData {
  fields: Record<string, any>;
  suggestedQuestions: string[];
  confidence: number;
}

export interface PersonalizedQuestion {
  questionId: string;
  question: string;
  type: 'yes_no' | 'text' | 'number' | 'select';
  relevanceScore: number;
  reason?: string;
}

export interface UpdateUserProfileRequest {
  ticketId: string;
  filledFields?: Record<string, any>;
  fillingTime?: number;
  skippedFields?: Record<string, any>;
}

class UserProfileService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = authService.getToken();
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * 获取用户画像
   */
  async getUserProfile(userId: string): Promise<UserProfileDto> {
    return this.request<UserProfileDto>(`/api/users/${userId}/profile`);
  }

  /**
   * 构建用户画像
   */
  async buildUserProfile(userId: string): Promise<UserProfileDto> {
    return this.request<UserProfileDto>(`/api/users/${userId}/profile/build`, {
      method: 'POST',
    });
  }

  /**
   * 获取智能预填充数据
   */
  async getPreFillData(userId: string, deviceId?: string): Promise<PreFillData> {
    const params = deviceId ? `?deviceId=${deviceId}` : '';
    return this.request<PreFillData>(`/api/users/${userId}/prefill${params}`);
  }

  /**
   * 获取个性化问题推荐
   */
  async getPersonalizedQuestions(
    userId: string,
    ticketId: string
  ): Promise<PersonalizedQuestion[]> {
    return this.request<PersonalizedQuestion[]>(
      `/api/users/${userId}/personalized-questions?ticketId=${ticketId}`
    );
  }

  /**
   * 更新用户画像
   */
  async updateUserProfile(
    userId: string,
    request: UpdateUserProfileRequest
  ): Promise<void> {
    await this.request(`/api/users/${userId}/profile/update`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }
}

export const userProfileService = new UserProfileService();




