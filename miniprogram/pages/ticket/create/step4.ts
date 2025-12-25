/**
 * Step 4: 附件上传
 */
import { apiService } from '../../../services/api';
import { storageService } from '../../../services/storage';
import { PAGES } from '../../../utils/constants';
import type { Attachment } from '../../../types';

Page({
  data: {
    attachments: [] as Attachment[],
    uploading: false,
    uploadProgress: 0,
  },

  onLoad() {
    // 从本地存储获取已上传的附件
    const savedAttachments = storageService.get<Attachment[]>('ticket_create_attachments');
    if (savedAttachments) {
      this.setData({ attachments: savedAttachments });
    }
  },

  /**
   * 拍照上传
   */
  async takePhoto() {
    try {
      const res = await new Promise<WechatMiniprogram.ChooseMediaSuccessCallbackResult>((resolve, reject) => {
        wx.chooseMedia({
          count: 1,
          mediaType: ['image'],
          sourceType: ['camera'],
          success: resolve,
          fail: reject,
        });
      });

      if (res.tempFiles && res.tempFiles.length > 0) {
        await this.uploadFile(res.tempFiles[0].tempFilePath, 'image');
      }
    } catch (error: any) {
      if (error.errMsg && !error.errMsg.includes('cancel')) {
        wx.showToast({
          title: '拍照失败',
          icon: 'none',
        });
      }
    }
  },

  /**
   * 选择图片
   */
  async chooseImage() {
    try {
      const res = await new Promise<WechatMiniprogram.ChooseImageSuccessCallbackResult>((resolve, reject) => {
        wx.chooseImage({
          count: 9,
          success: resolve,
          fail: reject,
        });
      });

      if (res.tempFilePaths && res.tempFilePaths.length > 0) {
        for (const filePath of res.tempFilePaths) {
          await this.uploadFile(filePath, 'image');
        }
      }
    } catch (error: any) {
      if (error.errMsg && !error.errMsg.includes('cancel')) {
        wx.showToast({
          title: '选择图片失败',
          icon: 'none',
        });
      }
    }
  },

  /**
   * 选择视频
   */
  async chooseVideo() {
    try {
      const res = await new Promise<WechatMiniprogram.ChooseVideoSuccessCallbackResult>((resolve, reject) => {
        wx.chooseVideo({
          sourceType: ['album', 'camera'],
          maxDuration: 60,
          success: resolve,
          fail: reject,
        });
      });

      if (res.tempFilePath) {
        await this.uploadFile(res.tempFilePath, 'video');
      }
    } catch (error: any) {
      if (error.errMsg && !error.errMsg.includes('cancel')) {
        wx.showToast({
          title: '选择视频失败',
          icon: 'none',
        });
      }
    }
  },

  /**
   * 选择文件
   */
  async chooseFile() {
    try {
      const res = await new Promise<WechatMiniprogram.ChooseMessageFileSuccessCallbackResult>((resolve, reject) => {
        wx.chooseMessageFile({
          count: 5,
          success: resolve,
          fail: reject,
        });
      });

      if (res.tempFiles && res.tempFiles.length > 0) {
        for (const file of res.tempFiles) {
          await this.uploadFile(file.path, 'file');
        }
      }
    } catch (error: any) {
      if (error.errMsg && !error.errMsg.includes('cancel')) {
        wx.showToast({
          title: '选择文件失败',
          icon: 'none',
        });
      }
    }
  },

  /**
   * 上传文件
   */
  async uploadFile(filePath: string, fileType: 'image' | 'video' | 'file') {
    // 从本地存储获取工单ID（如果已创建草稿）
    const ticketId = storageService.get('ticket_draft_id');
    if (!ticketId) {
      wx.showToast({
        title: '请先完成前面的步骤',
        icon: 'none',
      });
      return;
    }

    this.setData({ uploading: true, uploadProgress: 0 });

    try {
      // 获取文件信息
      const fileInfo = await new Promise<WechatMiniprogram.GetFileInfoSuccessCallbackResult>((resolve, reject) => {
        wx.getFileInfo({
          filePath,
          success: resolve,
          fail: reject,
        });
      });

      // 确定文件类型
      let attachmentType: 'photo' | 'video' | 'log' | 'file' = 'file';
      if (fileType === 'image') {
        attachmentType = 'photo';
      } else if (fileType === 'video') {
        attachmentType = 'video';
      }

      // 上传文件
      const attachment = await apiService.uploadAttachment(ticketId, filePath, attachmentType);

      // 添加到附件列表
      const attachments = [...this.data.attachments, attachment];
      this.setData({ attachments });

      // 保存到本地存储
      storageService.set('ticket_create_attachments', attachments);

      wx.showToast({
        title: '上传成功',
        icon: 'success',
      });
    } catch (error: any) {
      wx.showToast({
        title: error.message || '上传失败',
        icon: 'none',
      });
    } finally {
      this.setData({ uploading: false, uploadProgress: 0 });
    }
  },

  /**
   * 删除附件
   */
  onDeleteAttachment(e: any) {
    const index = e.currentTarget.dataset.index;
    const attachments = [...this.data.attachments];
    attachments.splice(index, 1);
    this.setData({ attachments });
    storageService.set('ticket_create_attachments', attachments);
  },

  /**
   * 上一步
   */
  onPrev() {
    wx.navigateBack();
  },

  /**
   * 下一步
   */
  onNext() {
    // 跳转到下一步
    wx.navigateTo({
      url: PAGES.TICKET_CREATE_STEP5,
    });
  },
});

