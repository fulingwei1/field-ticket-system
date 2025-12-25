/**
 * API接口服务
 */
import { request } from '../utils/request';
import { API_BASE_URL } from '../utils/constants';
import type { Ticket, CreateTicketRequest, Device, VerificationResult, SubmitVerificationRequest, Communication, CreateCommunicationRequest, Attachment, Solution, VerificationHistory, MissingInfoAnalysisResult, CompleteMissingInfoRequest } from '../types';

export class ApiService {
  /**
   * 获取工单列表
   */
  async getTickets(params?: {
    status?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ data: Ticket[]; total: number }> {
    const response = await request.get<{ data: Ticket[]; total: number }>('/api/tickets', params);
    return response.data || { data: [], total: 0 };
  }

  /**
   * 获取工单详情
   */
  async getTicket(ticketId: string): Promise<Ticket | null> {
    const response = await request.get<Ticket>(`/api/tickets/${ticketId}`);
    return response.data || null;
  }

  /**
   * 创建工单
   */
  async createTicket(data: CreateTicketRequest): Promise<Ticket> {
    // 转换数据格式以匹配后端API
    const requestData = {
      deviceId: data.deviceId,
      stationId: data.stationId,
      domain: data.domain,
      stepCode: data.stepCode,
      stepName: data.stepName,
      symptomTitle: data.symptomTitle,
      symptomDetail: data.symptomDetail,
      reproRate: data.reproRate,
      rebootRecovers: data.rebootRecovers,
      envRelated: data.envRelated,
      swVersion: data.swVersion,
      plcVersion: data.plcVersion,
      paramVersion: data.paramVersion,
      factsJson: data.factsJson,
      actionsTaken: data.actionsTaken || [],
      actionsTakenNote: data.actionsTakenNote,
      alarmCode: data.alarmCode,
      confirmedAsFact: data.confirmedAsFact,
    };

    const response = await request.post<Ticket>('/api/tickets', requestData);
    if (!response.success || !response.data) {
      throw new Error(response.message || '创建工单失败');
    }
    return response.data;
  }

  /**
   * 提交工单
   */
  async submitTicket(ticketId: string): Promise<{ success: boolean; ticketNo: string; status: string }> {
    const response = await request.post<{ success: boolean; ticketNo: string; status: string }>(
      `/api/tickets/${ticketId}/submit`,
      {}
    );
    if (!response.success || !response.data) {
      throw new Error(response.message || '提交工单失败');
    }
    return response.data;
  }

  /**
   * 更新工单
   */
  async updateTicket(ticketId: string, data: Partial<CreateTicketRequest>): Promise<Ticket> {
    const response = await request.put<Ticket>(`/api/tickets/${ticketId}`, data);
    if (!response.success || !response.data) {
      throw new Error(response.message || '更新工单失败');
    }
    return response.data;
  }

  /**
   * 获取设备列表
   */
  async getDevices(params?: {
    keyword?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ data: Device[]; total: number }> {
    const response = await request.get<{ data: Device[]; total: number }>('/api/devices', params);
    return response.data || { data: [], total: 0 };
  }

  /**
   * 获取设备详情
   */
  async getDevice(deviceId: string): Promise<Device | null> {
    const response = await request.get<Device>(`/api/devices/${deviceId}`);
    return response.data || null;
  }

  /**
   * 上传附件
   */
  async uploadAttachment(
    ticketId: string,
    filePath: string,
    fileType: 'photo' | 'video' | 'log' | 'file' = 'file'
  ): Promise<Attachment> {
    // 小程序上传文件需要使用 wx.uploadFile
    return new Promise((resolve, reject) => {
      const token = wx.getStorageSync('token');
      
      wx.uploadFile({
        url: `${API_BASE_URL}/api/attachments`,
        filePath: filePath,
        name: 'file',
        formData: {
          ticketId: ticketId,
          fileType: fileType,
        },
        header: {
          'Authorization': `Bearer ${token}`,
          'X-Platform': 'miniprogram',
        },
        success: (res) => {
          try {
            if (res.statusCode === 200) {
              const data = JSON.parse(res.data);
              // 后端直接返回 AttachmentDto，不需要包装
              resolve(data as Attachment);
            } else if (res.statusCode === 401) {
              // Token 过期，跳转到登录页
              wx.removeStorageSync('token');
              wx.removeStorageSync('refreshToken');
              wx.reLaunch({
                url: '/pages/login/login',
              });
              reject(new Error('登录已过期，请重新登录'));
            } else {
              let errorMessage = '上传失败';
              try {
                const errorData = JSON.parse(res.data);
                errorMessage = errorData.message || errorData.detail || errorMessage;
              } catch {
                // 解析失败，使用默认错误信息
              }
              reject(new Error(errorMessage));
            }
          } catch (error) {
            reject(new Error('解析响应失败'));
          }
        },
        fail: (error) => {
          reject(new Error(error.errMsg || '上传失败'));
        },
      });
    });
  }

  /**
   * 获取工单的解决方案列表
   */
  async getTicketSolutions(ticketId: string): Promise<Solution[]> {
    const response = await request.get<Solution[]>(`/api/solutions/tickets/${ticketId}`);
    return response.data || [];
  }

  /**
   * 提交验证结果
   */
  async submitVerification(ticketId: string, data: {
    solutionId?: string;
    runCount: number;
    passCount: number;
    failCount: number;
    checklistResultJson: Record<string, unknown>;
    evidenceAttachmentIds: string[];
    note?: string;
  }): Promise<VerificationResult> {
    const response = await request.post<VerificationResult>(`/api/verifications/tickets/${ticketId}`, data);
    if (!response.success || !response.data) {
      throw new Error(response.message || '提交验证结果失败');
    }
    return response.data;
  }

  /**
   * 获取工单的验证历史
   */
  async getVerificationHistory(ticketId: string): Promise<VerificationHistory[]> {
    const response = await request.get<VerificationHistory[]>(`/api/verifications/tickets/${ticketId}`);
    return response.data || [];
  }

  /**
   * 获取沟通记录列表
   */
  async getCommunications(ticketId: string): Promise<Communication[]> {
    const response = await request.get<Communication[]>(`/api/tickets/${ticketId}/communications`);
    return response.data || [];
  }

  /**
   * 创建沟通记录
   */
  async createCommunication(ticketId: string, data: CreateCommunicationRequest): Promise<Communication> {
    const response = await request.post<Communication>(`/api/tickets/${ticketId}/communications`, data);
    if (!response.success || !response.data) {
      throw new Error(response.message || '创建沟通记录失败');
    }
    return response.data;
  }

  /**
   * 获取工单缺失信息分析
   */
  async getMissingInfo(ticketId: string, jcCode?: string): Promise<MissingInfoAnalysisResult> {
    const params: Record<string, string> = {};
    if (jcCode) params.jcCode = jcCode;
    
    const response = await request.get<MissingInfoAnalysisResult>(`/api/tickets/${ticketId}/missing-info`, params);
    
    return response.data || { missingInfo: [], questions: [], hasCriticalMissing: false };
  }

  /**
   * 补全缺失信息
   */
  async completeMissingInfo(ticketId: string, data: CompleteMissingInfoRequest): Promise<Ticket> {
    const response = await request.post<Ticket>(`/api/tickets/${ticketId}/complete-missing-info`, data);
    if (!response.success || !response.data) {
      throw new Error(response.message || '补全信息失败');
    }
    return response.data;
  }
}

export const apiService = new ApiService();

