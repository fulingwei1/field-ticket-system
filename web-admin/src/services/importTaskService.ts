import request from './request';

export interface ImportTaskDto {
  id: string;
  taskType: string;
  taskTypeDisplay: string;
  status: string;
  statusDisplay: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  createdById: string;
  createdByName: string;
  fileName: string;
  totalCount: number;
  processedCount: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  progressPercentage: number;
  currentMessage?: string;
  errorMessage?: string;
}

export interface ImportTaskDetailDto extends ImportTaskDto {
  result?: any;
}

export interface ImportTaskQueryRequest {
  page?: number;
  pageSize?: number;
  taskType?: string;
  status?: string;
  createdById?: string;
}

export interface ImportTaskQueryResult {
  total: number;
  items: ImportTaskDto[];
}

class ImportTaskService {
  private baseUrl = '/api/import-tasks';

  /**
   * 获取任务状态
   */
  async getTaskStatus(taskId: string): Promise<ImportTaskDto> {
    return await request.get(`${this.baseUrl}/${taskId}`);
  }

  /**
   * 获取任务详情（包含结果）
   */
  async getTaskDetail(taskId: string): Promise<ImportTaskDetailDto> {
    return await request.get(`${this.baseUrl}/${taskId}/detail`);
  }

  /**
   * 查询任务列表
   */
  async queryTasks(params: ImportTaskQueryRequest): Promise<ImportTaskQueryResult> {
    return await request.post(`${this.baseUrl}/query`, params);
  }

  /**
   * 删除任务
   */
  async deleteTask(taskId: string): Promise<void> {
    await request.delete(`${this.baseUrl}/${taskId}`);
  }

  /**
   * 清理已完成的旧任务
   */
  async cleanupCompletedTasks(daysOld: number = 30): Promise<{ deletedCount: number; message: string }> {
    return await request.post(`${this.baseUrl}/cleanup?daysOld=${daysOld}`);
  }

  /**
   * 轮询任务状态直到完成
   */
  async pollTaskUntilComplete(
    taskId: string,
    onProgress?: (task: ImportTaskDto) => void,
    interval: number = 2000
  ): Promise<ImportTaskDetailDto> {
    return new Promise((resolve, reject) => {
      const poll = async () => {
        try {
          const task = await this.getTaskStatus(taskId);

          if (onProgress) {
            onProgress(task);
          }

          if (task.status === 'Completed') {
            // 获取详细结果
            const detail = await this.getTaskDetail(taskId);
            resolve(detail);
          } else if (task.status === 'Failed') {
            reject(new Error(task.errorMessage || '任务处理失败'));
          } else {
            // 继续轮询
            setTimeout(poll, interval);
          }
        } catch (error) {
          reject(error);
        }
      };

      poll();
    });
  }
}

export default new ImportTaskService();
