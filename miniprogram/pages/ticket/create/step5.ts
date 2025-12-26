/**
 * Step 5: 预览确认、提交
 */
import { apiService } from '../../../services/api';
import { storageService } from '../../../services/storage';
import { PLATFORM } from '../../../utils/constants';
import type { CreateTicketRequest, Device, Ticket, FactsJson, Attachment } from '../../../types';

// Step 2 数据接口
interface Step2Data {
  domain: string;
  stepCode: string;
  stepName?: string;
  swVersion: string;
  plcVersion: string;
  paramVersion: string;
}

// Step 3 数据接口
interface Step3Data {
  domain: string;
  facts: Record<string, 'yes' | 'no' | 'na'>;
  symptomTitle: string;
  symptomDetail?: string;
  reproRate: number;
  rebootRecovers: boolean;
  envRelated: boolean;
}

Page({
  data: {
    device: null as Device | null,
    step2Data: null as Step2Data | null,
    step3Data: null as Step3Data | null,
    attachments: [] as Attachment[],
    ticket: null as Ticket | null,
    submitting: false,
  },

  onLoad() {
    this.loadPreviewData();
  },

  /**
   * 加载预览数据
   */
  async loadPreviewData() {
    const device = storageService.getDeviceInfo();
    const step2Data = storageService.get('ticket_create_step2');
    const step3Data = storageService.get('ticket_create_step3');
    const attachments = storageService.get('ticket_create_attachments') || [];
    const ticketId = storageService.get('ticket_draft_id');

    if (!device || !step2Data || !step3Data) {
      wx.showToast({
        title: '数据不完整，请重新填写',
        icon: 'none',
      });
      setTimeout(() => {
        wx.reLaunch({
          url: '/pages/index/index',
        });
      }, 1500);
      return;
    }

    this.setData({
      device,
      step2Data,
      step3Data,
      attachments,
    });

    // 如果已有工单草稿，直接使用；否则创建新的
    if (ticketId) {
      try {
        const ticket = await apiService.getTicket(ticketId);
        if (ticket) {
          this.setData({ ticket });
          return;
        }
      } catch (error) {
        console.error('获取工单失败:', error);
        // 如果获取失败，继续创建新工单
      }
    }

    // 如果没有工单草稿或获取失败，创建新的
    try {
      const request = this.buildCreateRequest();
      const ticket = await apiService.createTicket(request);
      this.setData({ ticket });
      // 保存工单 ID
      storageService.set('ticket_draft_id', ticket.id);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : '创建工单失败';
      wx.showToast({
        title: message,
        icon: 'none',
      });
    }
  },

  /**
   * 构建创建工单请求
   */
  buildCreateRequest(): CreateTicketRequest {
    const { device, step2Data, step3Data } = this.data;
    if (!device || !step2Data || !step3Data) {
      throw new Error('数据不完整');
    }

    // 构建 factsJson
    // 将 step3Data.facts 转换为 FactsJson 格式
    const factsJson: FactsJson = {
      environment: {
        repro_rate: step3Data.reproRate,
        reboot_recovers: step3Data.rebootRecovers,
        env_related: step3Data.envRelated,
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
    if (domainKey && step3Data.facts) {
      const domainFacts: Record<string, 'YES' | 'NO' | 'NA'> = {};
      for (const [key, value] of Object.entries(step3Data.facts)) {
        domainFacts[key] = value.toUpperCase() as 'YES' | 'NO' | 'NA';
      }
      factsJson[domainKey] = domainFacts as FactsJson[typeof domainKey];
    }

    return {
      deviceId: device.id,
      domain: step2Data.domain,
      stepCode: step2Data.stepCode,
      stepName: step2Data.stepName || '',
      symptomTitle: step3Data.symptomTitle,
      symptomDetail: step3Data.symptomDetail || '',
      reproRate: step3Data.reproRate || 0,
      rebootRecovers: step3Data.rebootRecovers || false,
      envRelated: step3Data.envRelated || false,
      swVersion: step2Data.swVersion,
      plcVersion: step2Data.plcVersion,
      paramVersion: step2Data.paramVersion,
      factsJson,
      actionsTaken: [],
      confirmedAsFact: false,
    };
  },

  /**
   * 检查缺失信息
   */
  async checkMissingInfo() {
    try {
      const result = await apiService.getMissingInfo(this.data.ticket.id);
      if (result.questions && result.questions.length > 0) {
        // 有缺失信息，跳转到补全页面
        wx.navigateTo({
          url: `/pages/ticket/missing-info/missing-info?ticketId=${this.data.ticket.id}`,
        });
        return true;
      }
      return false;
    } catch (error) {
      console.error('Check missing info error:', error);
      return false;
    }
  },

  /**
   * 提交工单
   */
  async onSubmit() {
    if (this.data.submitting) {
      return;
    }

    // 先检查缺失信息
    const hasMissingInfo = await this.checkMissingInfo();
    if (hasMissingInfo) {
      // 跳转到补全页面，不继续提交
      return;
    }

    wx.showModal({
      title: '确认提交',
      content: '确定要提交工单吗？提交后将无法修改。',
      success: async (res) => {
        if (res.confirm) {
          await this.doSubmit();
        }
      },
    });
  },

  /**
   * 执行提交
   */
  async doSubmit() {
    this.setData({ submitting: true });

    try {
      // 1. 确保工单已创建
      let ticket = this.data.ticket;
      if (!ticket) {
        const request = this.buildCreateRequest();
        ticket = await apiService.createTicket(request);
        this.setData({ ticket });
      }

      // 2. 附件已在 Step 4 上传时关联到工单，无需再次关联
      // 附件上传时已经通过 ticketId 关联到工单，后端会自动处理

      // 3. 提交工单
      await apiService.submitTicket(ticket.id);

      // 4. 清除本地存储
      this.clearLocalStorage();

      wx.showToast({
        title: '提交成功',
        icon: 'success',
      });

      // 5. 跳转到工单列表
      setTimeout(() => {
        wx.reLaunch({
          url: '/pages/ticket/list/list',
        });
      }, 1500);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : '提交失败';
      wx.showToast({
        title: message,
        icon: 'none',
      });
    } finally {
      this.setData({ submitting: false });
    }
  },

  /**
   * 清除本地存储
   */
  clearLocalStorage() {
    storageService.clearDeviceInfo();
    storageService.remove('ticket_create_step2');
    storageService.remove('ticket_create_step3');
    storageService.remove('ticket_create_attachments');
  },

  /**
   * 缺失信息补全完成回调
   */
  onMissingInfoCompleted() {
    // 重新检查缺失信息
    this.checkMissingInfo().then((hasMissingInfo) => {
      if (!hasMissingInfo) {
        // 没有缺失信息了，可以提交
        this.doSubmit();
      }
    });
  },

  /**
   * 上一步
   */
  onPrev() {
    wx.navigateBack();
  },
});

