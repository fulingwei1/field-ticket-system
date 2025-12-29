import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001';

export interface CalibrateConfidenceRequest {
  originalConfidence: number;
  ticketId: string;
  hypothesisId?: string;
}

export interface CalibrateConfidenceResponse {
  originalConfidence: number;
  calibratedConfidence: number;
  calibrationMethod: string;
  calibrationFactors?: any;
}

export interface TrainCalibrationModelRequest {
  fromDate?: string;
  toDate?: string;
  modelType: string;
}

export interface CalibrationModelDto {
  modelId: string;
  modelVersion: string;
  modelType: string;
  modelParameters?: any;
  accuracy?: number;
  precisionScore?: number;
  recallScore?: number;
  f1Score?: number;
  trainingDataCount?: number;
  trainedAt?: string;
  isActive: boolean;
  createdAt: string;
}

export interface EvaluateCalibrationRequest {
  modelId: string;
  fromDate?: string;
  toDate?: string;
}

export interface CalibrationEvaluationDto {
  modelId: string;
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  testDataCount: number;
  metricsByConfidenceRange: Record<string, number>;
}

export interface ConfidenceDistributionDto {
  distributionByRange: Record<string, number>;
  averageConfidence: number;
  medianConfidence: number;
  totalCount: number;
}

export interface SubmitCalibrationFeedbackRequest {
  ticketId: string;
  hypothesisId?: string;
  originalConfidence: number;
  calibratedConfidence: number;
  actualResult: string;
}

class ConfidenceCalibrationService {
  /**
   * 校准置信度
   */
  async calibrateConfidence(request: CalibrateConfidenceRequest): Promise<CalibrateConfidenceResponse> {
    const response = await fetch(`${API_BASE_URL}/api/confidence/calibrate`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to calibrate confidence');
    }

    return response.json();
  }

  /**
   * 训练校准模型
   */
  async trainCalibrationModel(request: TrainCalibrationModelRequest): Promise<CalibrationModelDto> {
    const response = await fetch(`${API_BASE_URL}/api/confidence/train-model`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to train calibration model');
    }

    return response.json();
  }

  /**
   * 评估校准效果
   */
  async evaluateCalibration(request: EvaluateCalibrationRequest): Promise<CalibrationEvaluationDto> {
    const queryParams = new URLSearchParams();
    queryParams.append('modelId', request.modelId);
    if (request.fromDate) queryParams.append('fromDate', request.fromDate);
    if (request.toDate) queryParams.append('toDate', request.toDate);

    const response = await fetch(`${API_BASE_URL}/api/confidence/evaluate?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to evaluate calibration');
    }

    return response.json();
  }

  /**
   * 获取置信度分布
   */
  async getConfidenceDistribution(fromDate?: string, toDate?: string): Promise<ConfidenceDistributionDto> {
    const queryParams = new URLSearchParams();
    if (fromDate) queryParams.append('fromDate', fromDate);
    if (toDate) queryParams.append('toDate', toDate);

    const response = await fetch(`${API_BASE_URL}/api/confidence/distribution?${queryParams}`, {
      headers: authService.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to get confidence distribution');
    }

    return response.json();
  }

  /**
   * 提交校准反馈
   */
  async submitCalibrationFeedback(request: SubmitCalibrationFeedbackRequest): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/confidence/feedback`, {
      method: 'POST',
      headers: authService.getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Failed to submit calibration feedback');
    }
  }

  /**
   * 获取活跃的校准模型
   */
  async getActiveModel(): Promise<CalibrationModelDto | null> {
    const response = await fetch(`${API_BASE_URL}/api/confidence/active-model`, {
      headers: authService.getAuthHeaders(),
    });

    if (response.status === 404) {
      return null;
    }

    if (!response.ok) {
      throw new Error('Failed to get active model');
    }

    return response.json();
  }
}

export const confidenceCalibrationService = new ConfidenceCalibrationService();

