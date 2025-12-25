/**
 * 客户沟通记录页面
 */
import { apiService } from '../../../services/api';

Page({
  data: {
    ticketId: '',
    ticket: null as any,
    communications: [] as any[],
    loading: false,
    loadingData: true,
    showAddModal: false,
    newCommunication: {
      content: '',
      type: 'call' as 'call' | 'message' | 'email' | 'other',
    },
  },

  onLoad(options: any) {
    const ticketId = options.ticketId;
    if (!ticketId) {
      wx.showToast({
        title: '工单ID缺失',
        icon: 'none',
      });
      setTimeout(() => {
        wx.navigateBack();
      }, 1500);
      return;
    }

    this.setData({ ticketId });
    this.loadData();
  },

  /**
   * 加载数据
   */
  async loadData() {
    try {
      this.setData({ loadingData: true });

      // 加载工单信息
      const ticket = await apiService.getTicket(this.data.ticketId);
      if (!ticket) {
        throw new Error('工单不存在');
      }

      this.setData({ ticket });

      // 加载沟通记录
      await this.loadCommunications();
    } catch (error: any) {
      wx.showToast({
        title: error.message || '加载数据失败',
        icon: 'none',
      });
    } finally {
      this.setData({ loadingData: false });
    }
  },

  /**
   * 加载沟通记录
   */
  async loadCommunications() {
    try {
      const communications = await apiService.getCommunications(this.data.ticketId);
      this.setData({ communications });
    } catch (error: any) {
      console.error('加载沟通记录失败:', error);
      // 如果后端API不存在，显示空列表
      this.setData({ communications: [] });
    }
  },

  /**
   * 显示添加沟通记录弹窗
   */
  showAddModal() {
    this.setData({
      showAddModal: true,
      newCommunication: {
        content: '',
        type: 'call',
      },
    });
  },

  /**
   * 隐藏添加沟通记录弹窗
   */
  hideAddModal() {
    this.setData({ showAddModal: false });
  },

  /**
   * 沟通类型选择
   */
  onTypeChange(e: any) {
    const index = e.detail.value;
    const types: ('call' | 'message' | 'email' | 'other')[] = ['call', 'message', 'email', 'other'];
    const type = types[index] || 'call';
    this.setData({
      'newCommunication.type': type,
    });
  },

  /**
   * 沟通内容输入
   */
  onContentInput(e: any) {
    this.setData({
      'newCommunication.content': e.detail.value,
    });
  },

  /**
   * 提交沟通记录
   */
  async submitCommunication() {
    const { content, type } = this.data.newCommunication;

    if (!content || content.trim().length === 0) {
      wx.showToast({
        title: '请输入沟通内容',
        icon: 'none',
      });
      return;
    }

    try {
      this.setData({ loading: true });
      wx.showLoading({ title: '提交中...' });

      const request = {
        content: content.trim(),
        type,
        platform: 'miniprogram' as const,
      };

      await apiService.createCommunication(this.data.ticketId, request);

      wx.hideLoading();
      wx.showToast({
        title: '提交成功',
        icon: 'success',
      });

      this.hideAddModal();
      await this.loadCommunications();
    } catch (error: any) {
      wx.hideLoading();
      wx.showToast({
        title: error.message || '提交失败',
        icon: 'none',
      });
    } finally {
      this.setData({ loading: false });
    }
  },

  /**
   * 下拉刷新
   */
  async onPullDownRefresh() {
    await this.loadCommunications();
    wx.stopPullDownRefresh();
  },
});

