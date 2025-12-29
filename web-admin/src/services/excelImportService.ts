import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  // 使用 authService.getAuthHeaders() 确保认证头正确
  const headers = authService.getAuthHeaders();
  const finalHeaders: HeadersInit = {
    ...headers,
    ...(options.headers as HeadersInit),
  };

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: '请求失败' }));
    throw new Error(error.message || `HTTP error! status: ${response.status}`);
  }

  return response.json();
}

export interface ExcelImportResult {
  projects: ProjectImportData[];
  errors: ImportError[];
  totalRows: number;
  successRows: number;
  errorRows: number;
}

export interface ProjectImportData {
  projectNo: string;
  projectName: string;
  customerName?: string;
  deviceType?: string;
  industryType?: string;
  salesAmount?: number;
  quantity: number;
  orderDate?: string;
  requiredDeliveryDate?: string;
  actualDeliveryDate?: string;
  deliveryDelayDays?: number;
  projectStatus: string;
  projectManagerName?: string;
  problems: ProblemImportData[];
  excelRowNumber: number;
}

export interface ProblemImportData {
  problemSequence: number;
  problemCategory: string;
  problemDescription: string;
  priority?: string;
  foundDate: string;
  completedDate?: string;
  processingDays?: number;
  primaryDepartment: string;
  primaryResponsible: string;
  collaboratingDepartment?: string;
  collaboratingPerson?: string;
  status: string;
  solution?: string;
  solutionDetails?: string;
  verificationStatus?: string;
  customerFeedback?: string;
  satisfactionScore?: number;
  verifiedAt?: string;
  verifiedBy?: string;
  relatedTicketNo?: string;
  knowledgeBaseId?: string;
  isRepeatProblem: boolean;
  notes?: string;
  excelRowNumber: number;
}

export interface ImportError {
  rowNumber: number;
  field: string;
  errorType: string;
  message: string;
  suggestion?: string;
}

export interface ValidationResult {
  isValid: boolean;
  errors: Array<{
    field: string;
    code: string;
    message: string;
  }>;
}

export interface ImportExecutionResult {
  success: boolean;
  projectsCreated: number;
  projectsUpdated: number;
  problemsCreated: number;
  problemsUpdated: number;
  errors: ImportError[];
  message: string;
}

class ExcelImportService {
  /**
   * 解析Excel文件（预览）
   */
  async parseExcel(file: File): Promise<ExcelImportResult> {
    const formData = new FormData();
    formData.append('file', file);

    // 使用 authService.getAuthHeaders() 确保认证头正确
    const headers = authService.getAuthHeaders();

    const response = await fetch(`${API_BASE_URL}/api/excel-import/parse`, {
      method: 'POST',
      body: formData,
      headers,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: '请求失败' }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * 验证导入数据
   */
  async validateImportData(importResult: ExcelImportResult): Promise<ValidationResult> {
    const response = await request('/api/excel-import/validate', {
      method: 'POST',
      body: JSON.stringify(importResult),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    return response;
  }

  /**
   * 执行导入
   */
  async executeImport(importResult: ExcelImportResult): Promise<ImportExecutionResult> {
    const response = await request('/api/excel-import/execute', {
      method: 'POST',
      body: JSON.stringify({ importResult }),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    return response;
  }

  /**
   * 下载Excel模板
   */
  async downloadTemplate(): Promise<void> {
    // 使用 authService.getAuthHeaders() 确保认证头正确
    const headers = authService.getAuthHeaders();

    const response = await fetch(`${API_BASE_URL}/api/excel-import/template`, {
      method: 'GET',
      headers,
    });

    if (!response.ok) {
      throw new Error('下载模板失败');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = '现场问题导入模板.xlsx';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  }
}

export const excelImportService = new ExcelImportService();

