/**
 * 小程序入口文件
 */
import { authService } from './services/auth';

App<IAppOption>({
  globalData: {
    userInfo: null,
  },
  
  async onLaunch() {
    console.log('小程序启动');
    
    // 检查登录状态
    const token = authService.getToken();
    if (token) {
      try {
        const user = await authService.getCurrentUser();
        if (user) {
          this.globalData.userInfo = user;
        } else {
          // Token 无效，清除
          authService.clearAuth();
        }
      } catch (error) {
        console.error('获取用户信息失败:', error);
        authService.clearAuth();
      }
    }
  },
  
  onShow() {
    console.log('小程序显示');
  },
  
  onHide() {
    console.log('小程序隐藏');
  },
  
  onError(msg: string) {
    console.error('小程序错误:', msg);
    wx.showToast({
      title: '系统错误',
      icon: 'none',
    });
  },
});

interface IAppOption {
  globalData: {
    userInfo: any;
  };
}

