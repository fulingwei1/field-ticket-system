/**
 * Step 3: 事实表填写
 */
import { storageService } from '../../../services/storage';
import { PAGES } from '../../../utils/constants';
import { request } from '../../../utils/request';

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
      url: PAGES.TICKET_CREATE_STEP4,
    });
  },
});

