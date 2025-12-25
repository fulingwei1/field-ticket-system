/**
 * 类型定义
 */

// 事实值类型：YES/NO/NA 或 true/false/null
export type FactValue = 'YES' | 'NO' | 'NA' | true | false | null;

// 事实表 JSON 结构（按问题域分类）
export interface FactsJson {
  mechanical?: {
    action_completed?: FactValue;
    position_reliable?: FactValue;
    jam_or_noise?: FactValue;
    manual_help_recovers?: FactValue;
    [key: string]: FactValue | undefined;
  };
  electrical?: {
    sensor_physical_ok?: FactValue;
    plc_io_changes?: FactValue;
    similar_points_ok?: FactValue;
    [key: string]: FactValue | undefined;
  };
  plc?: {
    stuck_step_code?: string;
    stuck_fixed?: FactValue;
    manual_single_step_pass?: FactValue;
    alarm_code?: string;
    [key: string]: FactValue | string | undefined;
  };
  test?: {
    same_unit_repeat_consistent?: FactValue;
    swap_unit_recovers?: FactValue;
    near_limits?: FactValue;
    [key: string]: FactValue | undefined;
  };
  environment?: {
    repro_rate?: number;
    reboot_recovers?: boolean;
    env_related?: boolean;
    [key: string]: FactValue | number | boolean | undefined;
  };
  [key: string]: Record<string, FactValue | string | number | boolean | undefined> | undefined;
}

// 用户信息
export interface UserInfo {
  id: string;
  name: string;
  mobile?: string;
  role: string;
  deptName?: string;
}

// 工单
export interface Ticket {
  id: string;
  ticket_no: string;
  device_id: string;
  device_name?: string;
  domain: string;
  step_code: string;
  facts_json: FactsJson;
  attachments: Attachment[];
  status: string;
  platform: 'web' | 'mobile' | 'miniprogram';
  created_at: string;
  updated_at: string;
}

// 附件（与后端 AttachmentDto 匹配）
export interface Attachment {
  attachmentId: string;
  ticketId: string;
  uploadedBy: string;
  uploadedByName?: string;
  fileType: string; // photo/video/log/file
  fileName: string;
  fileSize: number;
  mimeType?: string;
  uploadStatus: string;
  description?: string;
  createdAt: string;
  downloadUrl?: string; // 预签名URL（临时）
}

// 验证结果
export interface VerificationResult {
  ticket_id: string;
  result: 'pass' | 'fail';
  checklist: ChecklistItem[];
  evidence: Attachment[];
  platform: 'web' | 'mobile' | 'miniprogram';
  created_at: string;
}

// 验证清单项
export interface ChecklistItem {
  id: string;
  content: string;
  checked: boolean;
}

// 沟通记录
export interface Communication {
  id: string;
  ticket_id: string;
  content: string;
  type: 'call' | 'message' | 'email' | 'other';
  created_by: string;
  created_by_name?: string;
  platform: 'web' | 'mobile' | 'miniprogram';
  created_at: string;
}

// 设备信息
export interface Device {
  id: string;
  device_sn: string;
  device_name: string;
  customer_id: string;
  customer_name?: string;
  project_id?: string;
  project_name?: string;
}

// 创建工单请求（与后端模型匹配）
export interface CreateTicketRequest {
  localDraftId?: string;
  deviceId: string; // GUID
  stationId?: string; // GUID
  domain: string; // A/B/C/D/E
  stepCode: string;
  stepName?: string;
  symptomTitle: string;
  symptomDetail?: string;
  reproRate?: number;
  rebootRecovers?: boolean;
  envRelated?: boolean;
  swVersion: string;
  plcVersion: string;
  paramVersion: string;
  factsJson: FactsJson;
  actionsTaken?: string[];
  actionsTakenNote?: string;
  alarmCode?: string;
  confirmedAsFact: boolean;
}

// 事实表表单
export interface FactsForm {
  domain: string;
  facts: Record<string, 'yes' | 'no' | 'na'>;
  platform: 'miniprogram';
}

// 提交验证结果请求
export interface SubmitVerificationRequest {
  ticket_id: string;
  result: 'pass' | 'fail';
  checklist: ChecklistItem[];
  evidence: Attachment[];
  platform: 'web' | 'mobile' | 'miniprogram';
}

// 创建沟通记录请求
export interface CreateCommunicationRequest {
  ticket_id: string;
  content: string;
  type: 'call' | 'message' | 'email' | 'other';
  platform: 'web' | 'mobile' | 'miniprogram';
}

// 缺失信息项
export interface MissingInfoItem {
  field: string;
  question: string;
  type: 'yes_no' | 'number' | 'text' | 'file' | 'select';
  required: boolean;
  domain?: string;
  options?: string[];
  hint?: string;
}

// 问诊式问题项
export interface QuestionItem {
  questionId: string;
  question: string;
  type: 'yes_no' | 'number' | 'text' | 'file' | 'select';
  required: boolean;
  options?: string[];
  hint?: string;
  field?: string;
}

// 缺失信息分析结果
export interface MissingInfoAnalysisResult {
  missingInfo: MissingInfoItem[];
  questions: QuestionItem[];
  hasCriticalMissing: boolean;
}

// 补全缺失信息请求
export interface CompleteMissingInfoRequest {
  answers: Record<string, string | number | boolean | null | undefined>;
  attachments?: Record<string, string[]>;
}

// 解决方案
export interface Solution {
  solutionId: string;
  solutionCode: string;
  ticketId: string;
  title: string;
  description?: string;
  solutionType: string; // software_update/parameter_change/hardware_replacement/procedure
  releaseType: string; // PLC/SOFTWARE/PARAM/MIXED
  requiredSwVersion?: string;
  requiredPlcVersion?: string;
  requiredParamVersion?: string;
  newSwVersion?: string;
  newPlcVersion?: string;
  newParamVersion?: string;
  changeDetailJson: Record<string, unknown>;
  verificationChecklistJson: Record<string, unknown>;
  implementationSteps?: string;
  estimatedImplementationTime?: number;
  riskLevel: string; // low/medium/high
  riskDescription?: string;
  rollbackPossible: boolean;
  rollbackProcedure?: string;
  status: string; // Draft/Published/Archived
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  publishedBy?: string;
}

// 验证历史记录
export interface VerificationHistory {
  verificationId: string;
  ticketId: string;
  solutionId?: string;
  result: 'pass' | 'fail';
  runCount: number;
  passCount: number;
  failCount: number;
  checklistResultJson: Record<string, unknown>;
  evidenceAttachmentIds: string[];
  note?: string;
  platform: 'web' | 'mobile' | 'miniprogram';
  createdBy: string;
  createdByName?: string;
  createdAt: string;
}

