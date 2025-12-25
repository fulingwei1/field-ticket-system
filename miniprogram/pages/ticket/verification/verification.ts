/**
 * 验证结果提交页面
 */
import { apiService } from '../../../services/api';
import { storageService } from '../../../services/storage';

Page({
  data: {
    ticketId: '',
    ticket: null as any,
    solutions: [] as any[],
    selectedSolutionId: '',
    selectedSolutionIndex: 0,
    selectedSolution: null as any,
    checklistItems: [] as any[],
    checklistResult: {} as Record<string, any>,
    runCount: 1,
    passCount: 1,
    failCount: 0,
    evidenceAttachmentIds: [] as string[],
    note: '',
    loading: false,
    loadingData: true,
  },

  onLoad(options: any) {
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
    this.loadData();
  },

  /**
   * 加载数据
   */
  async loadData() {
    try {
      this.setData({ loadingData: true });

      // 加载工单信息
      const ticket = await apiService.getTicket(this.data.ticketId);
      if (!ticket) {
        throw new Error('工单不存在');
      }

      // 验证工单状态
      if (ticket.status !== 'SolutionIssued') {
        wx.showModal({
          title: '提示',
          content: '只能对已发布解决方案的工单进行验证',
          showCancel: false,
          success: () => {
            wx.navigateBack();
          },
        });
        return;
      }

      this.setData({ ticket });

      // 加载解决方案列表
      const solutions = await apiService.getTicketSolutions(this.data.ticketId);
      const publishedSolution = solutions.find((s: any) => s.status === 'Published');
      
      if (publishedSolution) {
        const index = solutions.findIndex((s: any) => s.solutionId === publishedSolution.solutionId);
        this.setData({
          solutions,
          selectedSolutionId: publishedSolution.solutionId,
          selectedSolutionIndex: index >= 0 ? index : 0,
          selectedSolution: publishedSolution,
        });
        this.loadChecklist(publishedSolution);
      } else {
        this.setData({ solutions });
        wx.showToast({
          title: '暂无已发布的解决方案',
          icon: 'none',
        });
      }
    } catch (error: any) {
      wx.showToast({
        title: error.message || '加载数据失败',
        icon: 'none',
      });
    } finally {
      this.setData({ loadingData: false });
    }
  },

  /**
   * 加载验证清单
   */
  loadChecklist(solution: any) {
    const checklist = solution.verificationChecklistJson;
    if (!checklist || typeof checklist !== 'object') {
      this.setData({ checklistItems: [] });
      return;
    }

    const items: any[] = [];
    const checklistResult: Record<string, any> = {};

    // 处理前置检查
    if (checklist.precheck && Array.isArray(checklist.precheck)) {
      checklist.precheck.forEach((item: string, index: number) => {
        const id = `precheck-${index}`;
        items.push({
          id,
          text: item,
          type: 'precheck',
          required: true,
        });
        checklistResult[id] = {
          completed: false,
          required: true,
        };
      });
    }

    // 处理步骤
    if (checklist.steps && Array.isArray(checklist.steps)) {
      checklist.steps.forEach((step: any, index: number) => {
        const id = step.id || `step-${index}`;
        items.push({
          id,
          text: step.text || '',
          type: step.type || 'action',
          required: step.required !== false,
          acceptanceCriteria: step.acceptance_criteria,
        });
        checklistResult[id] = {
          completed: false,
          required: step.required !== false,
        };
      });
    }

    this.setData({
      checklistItems: items,
      checklistResult,
    });
  },

  /**
   * 选择解决方案
   */
  onSolutionChange(e: any) {
    const solutionId = e.detail.value;
    const solution = this.data.solutions.find((s: any) => s.solutionId === solutionId);
    if (solution) {
      this.setData({
        selectedSolutionId: solutionId,
        selectedSolution: solution,
      });
      this.loadChecklist(solution);
    }
  },

  /**
   * 清单项勾选
   */
  onChecklistChange(e: any) {
    const id = e.currentTarget.dataset.id;
    const checked = e.detail.value.length > 0;
    const checklistResult = { ...this.data.checklistResult };
    if (checklistResult[id]) {
      checklistResult[id].completed = checked;
    }
    this.setData({ checklistResult });
  },

  /**
   * 运行次数变化
   */
  onRunCountChange(e: any) {
    const runCount = parseInt(e.detail.value) || 1;
    const passCount = Math.min(this.data.passCount, runCount);
    const failCount = runCount - passCount;
    this.setData({ runCount, passCount, failCount });
  },

  /**
   * 通过次数变化
   */
  onPassCountChange(e: any) {
    const passCount = parseInt(e.detail.value) || 0;
    const runCount = this.data.runCount;
    const failCount = Math.max(0, runCount - passCount);
    this.setData({ passCount, failCount });
  },

  /**
   * 上传证据
   */
  async uploadEvidence() {
    try {
      wx.chooseMedia({
        count: 9,
        mediaType: ['image', 'video'],
        sourceType: ['album', 'camera'],
        success: async (res) => {
          wx.showLoading({ title: '上传中...' });
          try {
            const uploadPromises = res.tempFiles.map((file: any) =>
              apiService.uploadAttachment(this.data.ticketId, file.tempFilePath, file.fileType === 'video' ? 'video' : 'photo')
            );
            const attachments = await Promise.all(uploadPromises);
            const attachmentIds = attachments.map((a: any) => a.id);
            this.setData({
              evidenceAttachmentIds: [...this.data.evidenceAttachmentIds, ...attachmentIds],
            });
            wx.hideLoading();
            wx.showToast({
              title: '上传成功',
              icon: 'success',
            });
          } catch (error: any) {
            wx.hideLoading();
            wx.showToast({
              title: error.message || '上传失败',
              icon: 'none',
            });
          }
        },
        fail: (error: any) => {
          if (error.errMsg && !error.errMsg.includes('cancel')) {
            wx.showToast({
              title: '选择文件失败',
              icon: 'none',
            });
          }
        },
      });
    } catch (error: any) {
      wx.showToast({
        title: error.message || '上传失败',
        icon: 'none',
      });
    }
  },

  /**
   * 删除证据
   */
  removeEvidence(e: any) {
    const index = e.currentTarget.dataset.index;
    const evidenceAttachmentIds = [...this.data.evidenceAttachmentIds];
    evidenceAttachmentIds.splice(index, 1);
    this.setData({ evidenceAttachmentIds });
  },

  /**
   * 备注输入
   */
  onNoteInput(e: any) {
    this.setData({ note: e.detail.value });
  },

  /**
   * 提交验证结果
   */
  async submitVerification() {
    // 验证数据
    if (!this.data.selectedSolutionId) {
      wx.showToast({
        title: '请选择解决方案',
        icon: 'none',
      });
      return;
    }

    if (this.data.runCount < 1) {
      wx.showToast({
        title: '验证次数必须大于0',
        icon: 'none',
      });
      return;
    }

    if (this.data.runCount !== this.data.passCount + this.data.failCount) {
      wx.showToast({
        title: '验证次数必须等于通过次数加失败次数',
        icon: 'none',
      });
      return;
    }

    try {
      this.setData({ loading: true });
      wx.showLoading({ title: '提交中...' });

      const request = {
        solutionId: this.data.selectedSolutionId,
        runCount: this.data.runCount,
        passCount: this.data.passCount,
        failCount: this.data.failCount,
        checklistResultJson: this.data.checklistResult,
        evidenceAttachmentIds: this.data.evidenceAttachmentIds,
        note: this.data.note || undefined,
      };

      await apiService.submitVerification(this.data.ticketId, request);

      wx.hideLoading();
      wx.showToast({
        title: '提交成功',
        icon: 'success',
      });

      setTimeout(() => {
        wx.navigateBack();
      }, 1500);
    } catch (error: any) {
      wx.hideLoading();
      wx.showToast({
        title: error.message || '提交失败',
        icon: 'none',
      });
    } finally {
      this.setData({ loading: false });
    }
  },
});

