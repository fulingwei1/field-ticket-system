import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface AttachmentDto {
  attachmentId: string;
  ticketId: string;
  uploadedBy: string;
  uploadedByName?: string;
  fileType: string; // photo/video/log/file
  fileName: string;
  fileSize: number;
  mimeType?: string;
  uploadStatus: string;
  description?: string;
  createdAt: string;
  downloadUrl?: string;
}

class AttachmentService {
  /**
   * 上传附件
   */
  async uploadAttachment(
    ticketId: string,
    file: File,
    fileType: 'photo' | 'video' | 'log' | 'file'
  ): Promise<AttachmentDto> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('ticketId', ticketId);
    formData.append('fileType', fileType);

    const response = await fetch(`${API_BASE_URL}/api/attachments`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: formData,
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || '上传附件失败');
    }

    return response.json();
  }

  /**
   * 获取附件下载URL
   */
  async getDownloadUrl(attachmentId: string, expirySeconds: number = 900): Promise<string> {
    const response = await fetch(
      `${API_BASE_URL}/api/attachments/${attachmentId}/download-url?expirySeconds=${expirySeconds}`,
      {
        headers: authService.getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('获取下载URL失败');
    }

    const data = await response.json();
    return data.url;
  }

  /**
   * 删除附件
   */
  async deleteAttachment(attachmentId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/attachments/${attachmentId}`, {
      method: 'DELETE',
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('删除附件失败');
    }
  }

  /**
   * 获取工单的附件列表
   */
  async getTicketAttachments(ticketId: string): Promise<AttachmentDto[]> {
    const response = await fetch(`${API_BASE_URL}/api/attachments/tickets/${ticketId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取附件列表失败');
    }

    return response.json();
  }

  /**
   * 获取附件详情
   */
  async getAttachment(attachmentId: string): Promise<AttachmentDto> {
    const response = await fetch(`${API_BASE_URL}/api/attachments/${attachmentId}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('获取附件详情失败');
    }

    return response.json();
  }
}

export const attachmentService = new AttachmentService();

