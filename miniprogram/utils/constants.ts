/**
 * 常量定义
 */

// API 基础URL（需要根据实际环境配置）
export const API_BASE_URL = 'https://api.example.com';

// 平台标识
export const PLATFORM = 'miniprogram';

// 存储键名
export const STORAGE_KEYS = {
  TOKEN: 'field_ticket_token',
  REFRESH_TOKEN: 'field_ticket_refresh_token',
  USER_INFO: 'field_ticket_user',
  DEVICE_INFO: 'field_ticket_device',
} as const;

// 工单状态
export const TICKET_STATUS = {
  DRAFT: 'Draft',
  SUBMITTED: 'Submitted',
  TRIAGE: 'Triage',
  SOLUTION_ISSUED: 'SolutionIssued',
  VERIFICATION: 'Verification',
  RESOLVED: 'Resolved',
  CLOSED: 'Closed',
} as const;

// 问题域
export const DOMAINS = {
  A: 'A',
  B: 'B',
  C: 'C',
  D: 'D',
  E: 'E',
} as const;

// 验证结果
export const VERIFICATION_RESULT = {
  PASS: 'pass',
  FAIL: 'fail',
} as const;

// 页面路径
export const PAGES = {
  LOGIN: '/pages/login/login',
  INDEX: '/pages/index/index',
  TICKET_LIST: '/pages/ticket/list/list',
  TICKET_CREATE_STEP1: '/pages/ticket/create/step1',
  TICKET_CREATE_STEP2: '/pages/ticket/create/step2',
  TICKET_CREATE_STEP3: '/pages/ticket/create/step3',
  TICKET_CREATE_STEP4: '/pages/ticket/create/step4',
  TICKET_CREATE_STEP5: '/pages/ticket/create/step5',
} as const;

// 问题域选项
export const DOMAIN_OPTIONS = [
  { value: 'A', label: 'A - 机械问题' },
  { value: 'B', label: 'B - 电气问题' },
  { value: 'C', label: 'C - 软件问题' },
  { value: 'D', label: 'D - 参数问题' },
  { value: 'E', label: 'E - 其他问题' },
] as const;

