/**
 * Step 3: 事实表填写
 */
import { apiService } from '../../../services/api';
import { storageService } from '../../../services/storage';
import { PAGES } from '../../../utils/constants';
import { request } from '../../../utils/request';
import type { CreateTicketRequest, Device, FactsJson } from '../../../types';

// 事实表问题类型定义
interface FactQuestion {
  key: string;
  label: string;
  type: 'yes_no_na' | 'text' | 'number' | 'boolean';
  required?: boolean;
}

Page({
  data: {
    domain: '',
    facts: {} as Record<string, 'yes' | 'no' | 'na' | string | number | boolean>,
    questions: [] as FactQuestion[],
    symptomTitle: '',
    symptomDetail: '',
    reproRate: 0,
    rebootRecovers: false,
    envRelated: false,
    loading: false,
  },

  async onLoad() {
    // 从本地存储获取 Step 2 的数据
    const step2Data = storageService.get('ticket_create_step2');
    if (!step2Data || !step2Data.domain) {
      wx.showToast({
        title: '请先完成上一步',
        icon: 'none',
      });
      setTimeout(() => {
        wx.navigateBack();
      }, 1500);
      return;
    }

    const domain = step2Data.domain;
    
    // 从后端API获取事实表问题
    this.setData({ loading: true });
    try {
      const questions = await this.loadFactQuestions(domain);
      
      // 初始化事实表
      const facts: Record<string, 'yes' | 'no' | 'na' | string | number | boolean> = {};
      questions.forEach((q) => {
        if (q.type === 'yes_no_na') {
          facts[q.key] = 'na';
        } else if (q.type === 'number') {
          facts[q.key] = 0;
        } else if (q.type === 'boolean') {
          facts[q.key] = false;
        } else {
          facts[q.key] = '';
        }
      });

      this.setData({
        domain,
        questions,
        facts,
        loading: false,
      });
    } catch (error: any) {
      console.error('加载事实表问题失败:', error);
      wx.showToast({
        title: '加载事实表失败，使用默认问题',
        icon: 'none',
        duration: 2000,
      });
      // 使用默认问题
      const defaultQuestions: FactQuestion[] = [
        { key: 'repro_rate', label: '复现率 (%)', type: 'number', required: true },
        { key: 'reboot_recovers', label: '重启后是否恢复', type: 'boolean' },
        { key: 'env_related', label: '是否与环境相关', type: 'boolean' },
      ];
      const facts: Record<string, any> = {};
      defaultQuestions.forEach((q) => {
        if (q.type === 'number') {
          facts[q.key] = 0;
        } else if (q.type === 'boolean') {
          facts[q.key] = false;
        }
      });
      this.setData({
        domain,
        questions: defaultQuestions,
        facts,
        loading: false,
      });
    }
  },

  /**
   * 从后端API加载事实表问题
   */
  async loadFactQuestions(domain: string): Promise<FactQuestion[]> {
    const response = await request.get<FactQuestion[]>(
      `/api/fact-table/questions?domain=${domain}`
    );
    return response.data || [];
  },

  /**
   * 选择事实表项（YES/NO/NA）
   */
  onFactChange(e: any) {
    const { key, value } = e.currentTarget.dataset;
    const facts = { ...this.data.facts };
    facts[key] = value;
    this.setData({ facts });
  },

  /**
   * 输入文本类型的事实表项
   */
  onFactTextInput(e: any) {
    const { key } = e.currentTarget.dataset;
    const value = e.detail.value;
    const facts = { ...this.data.facts };
    facts[key] = value;
    this.setData({ facts });
  },

  /**
   * 输入数字类型的事实表项
   */
  onFactNumberInput(e: any) {
    const { key } = e.currentTarget.dataset;
    const value = parseInt(e.detail.value) || 0;
    const facts = { ...this.data.facts };
    facts[key] = value;
    this.setData({ facts });
  },

  /**
   * 切换布尔类型的事实表项
   */
  onFactBooleanChange(e: any) {
    const { key } = e.currentTarget.dataset;
    const value = e.detail.value;
    const facts = { ...this.data.facts };
    facts[key] = value;
    this.setData({ facts });
  },

  /**
   * 输入问题描述
   */
  onSymptomTitleInput(e: any) {
    this.setData({ symptomTitle: e.detail.value });
  },

  /**
   * 输入详细描述
   */
  onSymptomDetailInput(e: any) {
    this.setData({ symptomDetail: e.detail.value });
  },

  /**
   * 输入复现率
   */
  onReproRateInput(e: any) {
    const value = parseInt(e.detail.value) || 0;
    this.setData({ reproRate: Math.min(100, Math.max(0, value)) });
  },

  /**
   * 切换重启恢复
   */
  onRebootRecoversChange(e: any) {
    this.setData({ rebootRecovers: e.detail.value });
  },

  /**
   * 切换环境相关
   */
  onEnvRelatedChange(e: any) {
    this.setData({ envRelated: e.detail.value });
  },

  /**
   * 验证表单
   */
  validateForm(): boolean {
    // 验证问题描述
    if (!this.data.symptomTitle || this.data.symptomTitle.length < 10) {
      wx.showToast({
        title: '问题描述至少10个字符',
        icon: 'none',
      });
      return false;
    }

    // 验证必填的事实表字段
    for (const question of this.data.questions) {
      if (question.required) {
        const value = this.data.facts[question.key];
        if (value === undefined || value === null || value === '' || value === 'na') {
          wx.showToast({
            title: `${question.label}为必填项`,
            icon: 'none',
          });
          return false;
        }
        // 对于文本类型，检查是否为空字符串
        if (question.type === 'text' && (!value || String(value).trim() === '')) {
          wx.showToast({
            title: `${question.label}为必填项`,
            icon: 'none',
          });
          return false;
        }
      }
    }

    return true;
  },

  /**
   * 保存到本地存储
   */
  saveToStorage() {
    const formData = {
      facts: this.data.facts,
      symptomTitle: this.data.symptomTitle,
      symptomDetail: this.data.symptomDetail,
      reproRate: this.data.reproRate,
      rebootRecovers: this.data.rebootRecovers,
      envRelated: this.data.envRelated,
    };
    storageService.set('ticket_create_step3', formData);
  },

  /**
   * 上一步
   */
  onPrev() {
    wx.navigateBack();
  },

  /**
   * 构建创建工单请求
   */
  buildCreateRequest(): CreateTicketRequest {
    const step2Data = storageService.get('ticket_create_step2');
    const device = storageService.getDeviceInfo();
    
    if (!device || !step2Data) {
      throw new Error('数据不完整');
    }

    // 构建 factsJson
    const factsJson: FactsJson = {
      environment: {
        repro_rate: this.data.reproRate,
        reboot_recovers: this.data.rebootRecovers,
        env_related: this.data.envRelated,
      },
    };

    // 根据 domain 将 facts 分类到对应的域
    const domainMap: Record<string, keyof FactsJson> = {
      A: 'mechanical',
      B: 'electrical',
      C: 'plc',
      D: 'test',
    };

    const domainKey = domainMap[step2Data.domain];
    if (domainKey && this.data.facts) {
      const domainFacts: Record<string, 'YES' | 'NO' | 'NA' | string | number | boolean> = {};
      for (const [key, value] of Object.entries(this.data.facts)) {
        if (typeof value === 'string' && ['yes', 'no', 'na'].includes(value.toLowerCase())) {
          domainFacts[key] = value.toUpperCase() as 'YES' | 'NO' | 'NA';
        } else {
          // 处理其他类型（text, number, boolean）
          domainFacts[key] = value;
        }
      }
      if (Object.keys(domainFacts).length > 0) {
        factsJson[domainKey] = domainFacts as any;
      }
    }

    return {
      deviceId: device.id,
      domain: step2Data.domain,
      stepCode: step2Data.stepCode,
      stepName: step2Data.stepName || '',
      symptomTitle: this.data.symptomTitle,
      symptomDetail: this.data.symptomDetail || '',
      reproRate: this.data.reproRate || 0,
      rebootRecovers: this.data.rebootRecovers || false,
      envRelated: this.data.envRelated || false,
      swVersion: step2Data.swVersion,
      plcVersion: step2Data.plcVersion,
      paramVersion: step2Data.paramVersion,
      factsJson,
      actionsTaken: [],
      confirmedAsFact: false,
    };
  },

  /**
   * 创建工单草稿
   */
  async createDraftTicket() {
    try {
      const request = this.buildCreateRequest();
      const ticket = await apiService.createTicket(request);
      
      // 保存工单 ID 到本地存储
      storageService.set('ticket_draft_id', ticket.id);
      
      return ticket;
    } catch (error: any) {
      console.error('创建工单草稿失败:', error);
      throw error;
    }
  },

  /**
   * 下一步
   */
  async onNext() {
    if (!this.validateForm()) {
      return;
    }

    // 保存到本地存储
    this.saveToStorage();

    // 显示加载提示
    wx.showLoading({
      title: '创建工单草稿...',
      mask: true,
    });

    try {
      // 创建工单草稿，以便 Step 4 可以上传附件
      await this.createDraftTicket();
      
      wx.hideLoading();
      
      // 跳转到下一步
      wx.navigateTo({
        url: PAGES.TICKET_CREATE_STEP4,
      });
    } catch (error: any) {
      wx.hideLoading();
      wx.showToast({
        title: error.message || '创建工单失败',
        icon: 'none',
      });
    }
  },
});

