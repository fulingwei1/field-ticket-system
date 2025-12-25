/**
 * 问诊式补全缺失信息页面
 */
import { apiService } from '../../../services/api';
import { PAGES } from '../../../utils/constants';
import type { QuestionItem } from '../../../types';

Page({
  data: {
    ticketId: '',
    questions: [] as QuestionItem[],
    answers: {} as Record<string, string | number | boolean | null | undefined | string[]>,
    attachments: {} as Record<string, string[]>, // questionId -> attachmentIds[]
    hasCriticalMissing: false,
    loading: false,
    submitting: false,
  },

  onLoad(options: { ticketId?: string }) {
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
    this.loadMissingInfo();
  },

  /**
   * 加载缺失信息
   */
  async loadMissingInfo() {
    this.setData({ loading: true });
    try {
      const result = await apiService.getMissingInfo(this.data.ticketId);
      this.setData({
        questions: result.questions || [],
        hasCriticalMissing: result.hasCriticalMissing || false,
      });

      // 初始化答案
      const answers: Record<string, string | number | boolean | null | undefined | string[]> = {};
      result.questions?.forEach((q) => {
        if (q.type === 'yes_no') {
          answers[q.questionId] = undefined;
        } else if (q.type === 'number') {
          answers[q.questionId] = undefined;
        } else if (q.type === 'text') {
          answers[q.questionId] = '';
        } else if (q.type === 'select') {
          answers[q.questionId] = undefined;
        }
      });
      this.setData({ answers });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : '加载失败';
      wx.showToast({
        title: message,
        icon: 'none',
      });
    } finally {
      this.setData({ loading: false });
    }
  },

  /**
   * 选择答案（yes_no类型）
   */
  onAnswerChange(e: WechatMiniprogram.TouchEvent) {
    const { questionId, value } = e.currentTarget.dataset;
    const answers = { ...this.data.answers };
    answers[questionId] = value;
    this.setData({ answers });
  },

  /**
   * 输入答案（number/text类型）
   */
  onAnswerInput(e: WechatMiniprogram.InputEvent) {
    const { questionId } = e.currentTarget.dataset;
    const value = e.detail.value;
    const answers = { ...this.data.answers };
    answers[questionId] = value;
    this.setData({ answers });
  },

  /**
   * 选择答案（select类型）
   */
  onSelectChange(e: WechatMiniprogram.PickerChangeEvent) {
    const { questionId } = e.currentTarget.dataset;
    const value = e.detail.value;
    const answers = { ...this.data.answers };
    answers[questionId] = value;
    this.setData({ answers });
  },

  /**
   * 上传文件
   */
  async onFileUpload(e: WechatMiniprogram.TouchEvent) {
    const { questionId } = e.currentTarget.dataset;
    const question = this.data.questions.find((q) => q.questionId === questionId);
    if (!question) {
      return;
    }

    try {
      // 选择文件
      const res = await new Promise<WechatMiniprogram.ChooseMessageFileSuccessCallbackResult>(
        (resolve, reject) => {
          wx.chooseMessageFile({
            count: 5,
            success: resolve,
            fail: reject,
          });
        }
      );

      if (!res.tempFiles || res.tempFiles.length === 0) {
        return;
      }

      wx.showLoading({
        title: '上传中...',
        mask: true,
      });

      // 上传每个文件
      const attachmentIds: string[] = [];
      for (const file of res.tempFiles) {
        try {
          const attachment = await apiService.uploadAttachment(
            this.data.ticketId,
            file.path,
            'file'
          );
          attachmentIds.push(attachment.attachmentId);
        } catch (error: unknown) {
          const fileName = file.name || '文件';
          wx.showToast({
            title: `上传失败: ${fileName}`,
            icon: 'none',
          });
        }
      }

      wx.hideLoading();

      if (attachmentIds.length > 0) {
        // 保存附件ID到答案中
        const attachments = { ...this.data.attachments };
        if (!attachments[questionId]) {
          attachments[questionId] = [];
        }
        attachments[questionId].push(...attachmentIds);
        this.setData({ attachments });

        // 更新答案
        const answers = { ...this.data.answers };
        answers[questionId] = attachmentIds;
        this.setData({ answers });

        wx.showToast({
          title: `成功上传 ${attachmentIds.length} 个文件`,
          icon: 'success',
        });
      }
    } catch (error: unknown) {
      wx.hideLoading();
      if (error && typeof error === 'object' && 'errMsg' in error) {
        const errMsg = (error as { errMsg?: string }).errMsg;
        if (errMsg && !errMsg.includes('cancel')) {
          const message = error instanceof Error ? error.message : '选择文件失败';
          wx.showToast({
            title: message,
            icon: 'none',
          });
        }
      }
    }
  },

  /**
   * 验证表单
   */
  validateForm(): boolean {
    const { questions, answers } = this.data;

    for (const question of questions) {
      if (question.required) {
        const answer = answers[question.questionId];
        if (answer === undefined || answer === null || answer === '') {
          wx.showToast({
            title: `请回答：${question.question}`,
            icon: 'none',
          });
          return false;
        }
      }
    }

    return true;
  },

  /**
   * 提交补全信息
   */
  async onSubmit() {
    if (!this.validateForm()) {
      return;
    }

    this.setData({ submitting: true });

    try {
      await apiService.completeMissingInfo(this.data.ticketId, {
        answers: this.data.answers,
        attachments: this.data.attachments,
      });

      wx.showToast({
        title: '补全成功',
        icon: 'success',
      });

      // 触发页面事件，通知上一页补全完成
      const pages = getCurrentPages();
      const prevPage = pages[pages.length - 2];
      if (prevPage && prevPage.onMissingInfoCompleted) {
        prevPage.onMissingInfoCompleted();
      }

      // 返回上一页
      setTimeout(() => {
        wx.navigateBack();
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
   * 跳过
   */
  onSkip() {
    if (this.data.hasCriticalMissing) {
      wx.showToast({
        title: '有关键信息缺失，无法跳过',
        icon: 'none',
      });
      return;
    }

    wx.showModal({
      title: '确认跳过',
      content: '确定要跳过信息补全吗？',
      success: (res) => {
        if (res.confirm) {
          wx.navigateBack();
        }
      },
    });
  },
});

