/**
 * 首页
 */
import { authService } from '../../services/auth';
import { PAGES } from '../../utils/constants';

Page({
  data: {
    userInfo: null as any,
  },

  onLoad() {
    this.loadUserInfo();
  },

  onShow() {
    // 检查登录状态
    if (!authService.isAuthenticated()) {
      wx.reLaunch({
        url: PAGES.LOGIN,
      });
      return;
    }
    
    this.loadUserInfo();
  },

  /**
   * 加载用户信息
   */
  loadUserInfo() {
    const user = authService.getUser();
    if (user) {
      this.setData({ userInfo: user });
    } else {
      // 尝试从服务器获取
      authService.getCurrentUser().then((user) => {
        if (user) {
          this.setData({ userInfo: user });
        }
      });
    }
  },

  /**
   * 创建工单
   */
  handleCreateTicket() {
    wx.navigateTo({
      url: PAGES.TICKET_CREATE_STEP1,
    });
  },

  /**
   * 查看工单列表
   */
  handleViewTickets() {
    wx.switchTab({
      url: PAGES.TICKET_LIST,
    });
  },

  /**
   * 退出登录
   */
  handleLogout() {
    wx.showModal({
      title: '提示',
      content: '确定要退出登录吗？',
      success: (res) => {
        if (res.confirm) {
          authService.logout();
        }
      },
    });
  },
});

