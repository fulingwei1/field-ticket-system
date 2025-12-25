/**
 * 登录页面
 */
import { authService } from '../../services/auth';
import { PAGES } from '../../utils/constants';

Page({
  data: {
    loading: false,
  },

  onLoad() {
    // 检查是否已登录
    if (authService.isAuthenticated()) {
      this.redirectToIndex();
    }
  },

  /**
   * 企业微信登录
   */
  async handleLogin() {
    this.setData({ loading: true });

    try {
      await authService.login();
      
      wx.showToast({
        title: '登录成功',
        icon: 'success',
      });

      // 跳转到首页
      setTimeout(() => {
        this.redirectToIndex();
      }, 1000);
    } catch (error: any) {
      console.error('Login error:', error);
      wx.showToast({
        title: error.message || '登录失败',
        icon: 'none',
      });
    } finally {
      this.setData({ loading: false });
    }
  },

  /**
   * 跳转到首页
   */
  redirectToIndex() {
    wx.switchTab({
      url: PAGES.INDEX,
    });
  },
});

