/**
 * 工单列表页面
 */
import { apiService } from '../../../services/api';
import { PAGES, TICKET_STATUS } from '../../../utils/constants';
import type { Ticket } from '../../../types';

Page({
  data: {
    tickets: [] as Ticket[],
    loading: false,
    refreshing: false,
    hasMore: true,
    page: 1,
    pageSize: 20,
    statusFilter: '' as string,
    statusOptions: [
      { label: '全部', value: '' },
      { label: '草稿', value: TICKET_STATUS.DRAFT },
      { label: '已提交', value: TICKET_STATUS.SUBMITTED },
      { label: '分诊中', value: TICKET_STATUS.TRIAGE },
      { label: '已出方案', value: TICKET_STATUS.SOLUTION_ISSUED },
      { label: '验证中', value: TICKET_STATUS.VERIFICATION },
      { label: '已解决', value: TICKET_STATUS.RESOLVED },
      { label: '已关闭', value: TICKET_STATUS.CLOSED },
    ],
    statusIndex: 0,
  },

  onLoad() {
    this.loadTickets();
  },

  onShow() {
    // 每次显示时刷新列表
    this.refreshTickets();
  },

  /**
   * 加载工单列表
   */
  async loadTickets(loadMore = false) {
    if (this.data.loading) {
      return;
    }

    this.setData({ loading: true });

    try {
      const page = loadMore ? this.data.page + 1 : 1;
      const params: {
        page?: number;
        pageSize?: number;
        status?: string;
      } = {
        page,
        pageSize: this.data.pageSize,
      };

      if (this.data.statusFilter) {
        params.status = this.data.statusFilter;
      }

      const result = await apiService.getTickets(params);

      // 为每个工单添加状态文本和颜色
      const tickets = (loadMore
        ? [...this.data.tickets, ...result.data]
        : result.data).map((ticket) => ({
        ...ticket,
        statusText: this.getStatusText(ticket.status),
        statusColor: this.getStatusColor(ticket.status),
      }));

      this.setData({
        tickets,
        page,
        hasMore: tickets.length < result.total,
        loading: false,
        refreshing: false,
      });
    } catch (error: any) {
      wx.showToast({
        title: error.message || '加载失败',
        icon: 'none',
      });
      this.setData({ loading: false, refreshing: false });
    }
  },

  /**
   * 刷新工单列表
   */
  async refreshTickets() {
    this.setData({ refreshing: true, page: 1, hasMore: true });
    await this.loadTickets(false);
  },

  /**
   * 加载更多
   */
  onLoadMore() {
    if (this.data.hasMore && !this.data.loading) {
      this.loadTickets(true);
    }
  },

  /**
   * 选择工单状态筛选
   */
  onStatusFilterChange(e: any) {
    const index = parseInt(e.detail.value);
    const status = this.data.statusOptions[index].value;
    this.setData({ 
      statusFilter: status, 
      statusIndex: index,
      page: 1, 
      hasMore: true 
    });
    this.loadTickets(false);
  },

  /**
   * 点击工单项
   */
  onTicketTap(e: any) {
    const ticketId = e.currentTarget.dataset.id;
    // 跳转到工单详情页（如果存在）
    // wx.navigateTo({
    //   url: `/pages/ticket/detail/detail?id=${ticketId}`,
    // });
    
    // 暂时显示工单信息
    const ticket = this.data.tickets.find((t) => t.id === ticketId);
    if (ticket) {
      wx.showModal({
        title: '工单详情',
        content: `工单号：${ticket.ticket_no}\n状态：${this.getStatusText(ticket.status)}\n问题域：${ticket.domain}`,
        showCancel: false,
      });
    }
  },

  /**
   * 创建新工单
   */
  onCreateTicket() {
    wx.navigateTo({
      url: PAGES.TICKET_CREATE_STEP1,
    });
  },

  /**
   * 获取状态文本
   */
  getStatusText(status: string): string {
    const statusMap: Record<string, string> = {
      [TICKET_STATUS.DRAFT]: '草稿',
      [TICKET_STATUS.SUBMITTED]: '已提交',
      [TICKET_STATUS.TRIAGE]: '分诊中',
      [TICKET_STATUS.SOLUTION_ISSUED]: '已出方案',
      [TICKET_STATUS.VERIFICATION]: '验证中',
      [TICKET_STATUS.RESOLVED]: '已解决',
      [TICKET_STATUS.CLOSED]: '已关闭',
    };
    return statusMap[status] || status;
  },

  /**
   * 获取状态颜色
   */
  getStatusColor(status: string): string {
    const colorMap: Record<string, string> = {
      [TICKET_STATUS.DRAFT]: '#999',
      [TICKET_STATUS.SUBMITTED]: '#1890ff',
      [TICKET_STATUS.TRIAGE]: '#faad14',
      [TICKET_STATUS.SOLUTION_ISSUED]: '#722ed1',
      [TICKET_STATUS.VERIFICATION]: '#13c2c2',
      [TICKET_STATUS.RESOLVED]: '#52c41a',
      [TICKET_STATUS.CLOSED]: '#999',
    };
    return colorMap[status] || '#999';
  },
});

