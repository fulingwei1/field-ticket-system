import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface ProjectQueryFilter {
  projectNo?: string;
  projectName?: string;
  customerName?: string;
  deviceType?: string;
  projectStatus?: string;
  orderDateFrom?: string;
  orderDateTo?: string;
}

export interface ProjectDto {
  projectId: string;
  projectNo: string;
  projectName: string;
  customerId: string;
  customerName?: string;
  deviceType: string;
  industryType?: string;
  salesAmount?: number;
  quantity: number;
  orderDate?: string;
  requiredDeliveryDate?: string;
  actualDeliveryDate?: string;
  deliveryDelayDays?: number;
  projectStatus: string;
  projectManagerName?: string;
  problemCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectDetailDto extends ProjectDto {
  problems: FieldProblemDto[];
}

export interface FieldProblemDto {
  problemId: string;
  projectId: string;
  projectNo: string;
  projectName: string;
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
  verificationStatus?: string;
  satisfactionScore?: number;
  isRepeatProblem: boolean;
  createdAt: string;
}

export interface FieldProblemDetailDto extends FieldProblemDto {
  solution?: string;
  solutionDetails?: string;
  customerFeedback?: string;
  verifiedAt?: string;
  verifiedBy?: string;
  relatedTicketNo?: string;
  knowledgeBaseId?: string;
  notes?: string;
  rootCauseAnalysis?: RootCauseAnalysisDto;
}

export interface RootCauseAnalysisDto {
  analysisId: string;
  problemId: string;
  why1?: string;
  why2?: string;
  why3?: string;
  why4?: string;
  why5?: string;
  rootCause: string;
  rootCauseCategory?: string;
  preventiveMeasures?: string;
  verificationMethod?: string;
  analyzedBy?: string;
  analyzedByName?: string;
  analyzedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProblemStatisticsFilter {
  problemCategory?: string;
  primaryDepartment?: string;
  customerName?: string;
  foundDateFrom?: string;
  foundDateTo?: string;
  status?: string;
}

export interface ProblemStatisticsDto {
  totalProblems: number;
  closedProblems: number;
  openProblems: number;
  averageProcessingDays: number;
  averageSatisfactionScore: number;
  categoryStatistics: CategoryStatistics[];
  departmentStatistics: DepartmentStatistics[];
  customerStatistics: CustomerStatistics[];
}

export interface CategoryStatistics {
  category: string;
  count: number;
  percentage: number;
}

export interface DepartmentStatistics {
  department: string;
  count: number;
  averageProcessingDays: number;
  averageSatisfactionScore: number;
}

export interface CustomerStatistics {
  customerName: string;
  count: number;
  averageSatisfactionScore: number;
}

class ProjectService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = authService.getToken();
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * 获取项目列表
   */
  async getProjects(
    filter: ProjectQueryFilter,
    page: number = 1,
    pageSize: number = 20
  ): Promise<{ items: ProjectDto[]; total: number; page: number; pageSize: number }> {
    const params = new URLSearchParams();
    if (filter.projectNo) params.append('projectNo', filter.projectNo);
    if (filter.projectName) params.append('projectName', filter.projectName);
    if (filter.customerName) params.append('customerName', filter.customerName);
    if (filter.deviceType) params.append('deviceType', filter.deviceType);
    if (filter.projectStatus) params.append('projectStatus', filter.projectStatus);
    if (filter.orderDateFrom) params.append('orderDateFrom', filter.orderDateFrom);
    if (filter.orderDateTo) params.append('orderDateTo', filter.orderDateTo);
    params.append('page', String(page));
    params.append('pageSize', String(pageSize));

    return this.request<{ items: ProjectDto[]; total: number; page: number; pageSize: number }>(
      `/api/projects?${params.toString()}`
    );
  }

  /**
   * 获取项目详情
   */
  async getProject(projectId: string): Promise<ProjectDetailDto> {
    return this.request<ProjectDetailDto>(`/api/projects/${projectId}`);
  }

  /**
   * 获取项目的问题列表
   */
  async getProjectProblems(projectId: string): Promise<FieldProblemDto[]> {
    return this.request<FieldProblemDto[]>(`/api/projects/${projectId}/problems`);
  }

  /**
   * 获取问题详情
   */
  async getProblem(problemId: string): Promise<FieldProblemDetailDto> {
    return this.request<FieldProblemDetailDto>(`/api/projects/problems/${problemId}`);
  }

  /**
   * 获取问题统计
   */
  async getProblemStatistics(filter: ProblemStatisticsFilter): Promise<ProblemStatisticsDto> {
    const params = new URLSearchParams();
    if (filter.problemCategory) params.append('problemCategory', filter.problemCategory);
    if (filter.primaryDepartment) params.append('primaryDepartment', filter.primaryDepartment);
    if (filter.customerName) params.append('customerName', filter.customerName);
    if (filter.foundDateFrom) params.append('foundDateFrom', filter.foundDateFrom);
    if (filter.foundDateTo) params.append('foundDateTo', filter.foundDateTo);
    if (filter.status) params.append('status', filter.status);

    return this.request<ProblemStatisticsDto>(
      `/api/projects/statistics/problems?${params.toString()}`
    );
  }
}

export const projectService = new ProjectService();




