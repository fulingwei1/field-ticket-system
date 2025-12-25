/**
 * Step 2: 选问题域、步骤、版本
 */
import { storageService } from '../../../services/storage';
import { PAGES, DOMAIN_OPTIONS } from '../../../utils/constants';

Page({
  data: {
    device: null as any,
    domain: '',
    domainIndex: -1,
    stepCode: '',
    stepName: '',
    swVersion: '',
    plcVersion: '',
    paramVersion: '',
    domainOptions: DOMAIN_OPTIONS,
  },

  onLoad() {
    // 从本地存储获取设备信息
    const device = storageService.getDeviceInfo();
    if (!device) {
      wx.showToast({
        title: '请先选择设备',
        icon: 'none',
      });
      setTimeout(() => {
        wx.navigateBack();
      }, 1500);
      return;
    }

    this.setData({ device });
    
    // 如果有保存的数据，恢复表单
    const savedData = storageService.get('ticket_create_step2');
    if (savedData) {
      const domainIndex = DOMAIN_OPTIONS.findIndex((opt) => opt.value === savedData.domain);
      this.setData({
        domain: savedData.domain,
        domainIndex: domainIndex >= 0 ? domainIndex : -1,
        stepCode: savedData.stepCode || '',
        stepName: savedData.stepName || '',
        swVersion: savedData.swVersion || '',
        plcVersion: savedData.plcVersion || '',
        paramVersion: savedData.paramVersion || '',
      });
    }
  },

  /**
   * 选择问题域
   */
  onDomainChange(e: any) {
    const index = parseInt(e.detail.value);
    const domain = DOMAIN_OPTIONS[index].value;
    this.setData({ domain, domainIndex: index });
  },

  /**
   * 输入步骤代码
   */
  onStepCodeInput(e: any) {
    this.setData({ stepCode: e.detail.value });
  },

  /**
   * 输入步骤名称
   */
  onStepNameInput(e: any) {
    this.setData({ stepName: e.detail.value });
  },

  /**
   * 输入软件版本
   */
  onSwVersionInput(e: any) {
    this.setData({ swVersion: e.detail.value });
  },

  /**
   * 输入PLC版本
   */
  onPlcVersionInput(e: any) {
    this.setData({ plcVersion: e.detail.value });
  },

  /**
   * 输入参数版本
   */
  onParamVersionInput(e: any) {
    this.setData({ paramVersion: e.detail.value });
  },

  /**
   * 验证表单
   */
  validateForm(): boolean {
    if (!this.data.domain) {
      wx.showToast({
        title: '请选择问题域',
        icon: 'none',
      });
      return false;
    }

    if (!this.data.stepCode) {
      wx.showToast({
        title: '请输入步骤代码',
        icon: 'none',
      });
      return false;
    }

    if (!this.data.swVersion) {
      wx.showToast({
        title: '请输入软件版本',
        icon: 'none',
      });
      return false;
    }

    if (!this.data.plcVersion) {
      wx.showToast({
        title: '请输入PLC版本',
        icon: 'none',
      });
      return false;
    }

    if (!this.data.paramVersion) {
      wx.showToast({
        title: '请输入参数版本',
        icon: 'none',
      });
      return false;
    }

    return true;
  },

  /**
   * 保存到本地存储
   */
  saveToStorage() {
    const formData = {
      domain: this.data.domain,
      stepCode: this.data.stepCode,
      stepName: this.data.stepName,
      swVersion: this.data.swVersion,
      plcVersion: this.data.plcVersion,
      paramVersion: this.data.paramVersion,
    };
    storageService.set('ticket_create_step2', formData);
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
    if (!this.validateForm()) {
      return;
    }

    // 保存到本地存储
    this.saveToStorage();

    // 跳转到下一步
    wx.navigateTo({
      url: PAGES.TICKET_CREATE_STEP3,
    });
  },
});

