/**
 * 本地存储服务
 */
import { STORAGE_KEYS } from '../utils/constants';

export class StorageService {
  /**
   * 设置存储
   */
  set<T = any>(key: string, value: T): void {
    try {
      const data = typeof value === 'string' ? value : JSON.stringify(value);
      wx.setStorageSync(key, data);
    } catch (error) {
      console.error('Storage set error:', error);
    }
  }

  /**
   * 获取存储
   */
  get<T = any>(key: string): T | null {
    try {
      const data = wx.getStorageSync(key);
      if (!data) {
        return null;
      }
      
      // 尝试解析JSON
      try {
        return JSON.parse(data) as T;
      } catch {
        // 如果不是JSON，直接返回
        return data as T;
      }
    } catch (error) {
      console.error('Storage get error:', error);
      return null;
    }
  }

  /**
   * 删除存储
   */
  remove(key: string): void {
    try {
      wx.removeStorageSync(key);
    } catch (error) {
      console.error('Storage remove error:', error);
    }
  }

  /**
   * 清空所有存储
   */
  clear(): void {
    try {
      wx.clearStorageSync();
    } catch (error) {
      console.error('Storage clear error:', error);
    }
  }

  /**
   * 获取设备信息
   */
  getDeviceInfo() {
    return this.get(STORAGE_KEYS.DEVICE_INFO);
  }

  /**
   * 保存设备信息
   */
  setDeviceInfo(device: any): void {
    this.set(STORAGE_KEYS.DEVICE_INFO, device);
  }

  /**
   * 清除设备信息
   */
  clearDeviceInfo(): void {
    this.remove(STORAGE_KEYS.DEVICE_INFO);
  }
}

export const storageService = new StorageService();

