/**
 * Step 1: 扫码/选择设备
 */
import { apiService } from '../../../services/api';
import { storageService } from '../../../services/storage';
import { PAGES } from '../../../utils/constants';
import type { Device } from '../../../types';

Page({
  data: {
    deviceList: [] as Device[],
    selectedDevice: null as Device | null,
    searchKeyword: '',
    loading: false,
  },

  onLoad() {
    this.loadDevices();
  },

  /**
   * 加载设备列表
   */
  async loadDevices() {
    this.setData({ loading: true });
    try {
      const result = await apiService.getDevices({ pageSize: 50 });
      this.setData({ deviceList: result.data });
    } catch (error: any) {
      wx.showToast({
        title: error.message || '加载设备列表失败',
        icon: 'none',
      });
    } finally {
      this.setData({ loading: false });
    }
  },

  /**
   * 搜索设备
   */
  onSearchInput(e: any) {
    this.setData({ searchKeyword: e.detail.value });
    this.filterDevices();
  },

  /**
   * 过滤设备列表
   */
  filterDevices() {
    const keyword = this.data.searchKeyword.toLowerCase();
    if (!keyword) {
      this.loadDevices();
      return;
    }

    const filtered = this.data.deviceList.filter((device) => {
      return (
        device.device_sn.toLowerCase().includes(keyword) ||
        device.device_name.toLowerCase().includes(keyword) ||
        device.customer_name?.toLowerCase().includes(keyword)
      );
    });

    this.setData({ deviceList: filtered });
  },

  /**
   * 扫描二维码
   */
  async onScanQRCode() {
    try {
      const res = await new Promise<WechatMiniprogram.ScanCodeSuccessCallbackResult>((resolve, reject) => {
        wx.scanCode({
          success: resolve,
          fail: reject,
        });
      });

      // 解析二维码，获取设备ID或设备SN
      const deviceId = this.parseQRCode(res.result);
      if (deviceId) {
        await this.selectDevice(deviceId);
      } else {
        wx.showToast({
          title: '无法识别二维码',
          icon: 'none',
        });
      }
    } catch (error: any) {
      if (error.errMsg && !error.errMsg.includes('cancel')) {
        wx.showToast({
          title: '扫码失败',
          icon: 'none',
        });
      }
    }
  },

  /**
   * 解析二维码
   */
  parseQRCode(qrCode: string): string | null {
    // 假设二维码格式为：device:{deviceId} 或 device_sn:{deviceSn}
    if (qrCode.startsWith('device:')) {
      return qrCode.replace('device:', '');
    }
    if (qrCode.startsWith('device_sn:')) {
      // 通过设备SN查找设备ID
      const device = this.data.deviceList.find((d) => d.device_sn === qrCode.replace('device_sn:', ''));
      return device?.id || null;
    }
    // 尝试直接作为设备ID或设备SN
    const device = this.data.deviceList.find((d) => d.id === qrCode || d.device_sn === qrCode);
    return device?.id || null;
  },

  /**
   * 选择设备
   */
  async selectDevice(deviceId: string) {
    try {
      const device = await apiService.getDevice(deviceId);
      if (device) {
        this.setData({ selectedDevice: device });
        // 保存设备信息到本地存储
        storageService.setDeviceInfo(device);
        // 预填客户项目信息（在下一步使用）
        wx.showToast({
          title: '设备选择成功',
          icon: 'success',
        });
      } else {
        wx.showToast({
          title: '设备不存在',
          icon: 'none',
        });
      }
    } catch (error: any) {
      wx.showToast({
        title: error.message || '获取设备信息失败',
        icon: 'none',
      });
    }
  },

  /**
   * 手动选择设备
   */
  onSelectDevice(e: any) {
    const deviceId = e.currentTarget.dataset.deviceId;
    this.selectDevice(deviceId);
  },

  /**
   * 更换设备
   */
  onChangeDevice() {
    this.setData({ selectedDevice: null });
    storageService.clearDeviceInfo();
  },

  /**
   * 下一步
   */
  onNext() {
    if (!this.data.selectedDevice) {
      wx.showToast({
        title: '请先选择设备',
        icon: 'none',
      });
      return;
    }

    // 跳转到下一步
    wx.navigateTo({
      url: PAGES.TICKET_CREATE_STEP2,
    });
  },
});

