"use client";

import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Space,
  message,
  Spin,
  Tabs,
  Typography,
  Alert,
} from 'antd';
import {
  ArrowLeftOutlined,
  EditOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import {
  projectService,
  FieldProblemDetailDto,
} from '../../services/projectService';
import {
  rootCauseAnalysisService,
  RootCauseAnalysisDto,
  CreateRootCauseAnalysisRequest,
} from '../../services/rootCauseAnalysisService';
import RootCauseAnalysisForm from '../../components/projects/RootCauseAnalysisForm';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { TabPane } = Tabs;

export default function ProblemDetail() {
  const { problemId } = useParams<{ problemId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [problem, setProblem] = useState<FieldProblemDetailDto | null>(null);
  const [rootCauseAnalysis, setRootCauseAnalysis] = useState<RootCauseAnalysisDto | null>(null);
  const [loadingAnalysis, setLoadingAnalysis] = useState(false);

  useEffect(() => {
    if (problemId) {
      loadProblem();
      loadRootCauseAnalysis();
    }
  }, [problemId]);

  const loadProblem = async () => {
    if (!problemId) return;
    setLoading(true);
    try {
      const data = await projectService.getProblem(problemId);
      setProblem(data);
      if (data.rootCauseAnalysis) {
        setRootCauseAnalysis(data.rootCauseAnalysis);
      }
    } catch (error: any) {
      message.error('加载问题详情失败：' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const loadRootCauseAnalysis = async () => {
    if (!problemId) return;
    setLoadingAnalysis(true);
    try {
      const analysis = await rootCauseAnalysisService.getAnalysisByProblemId(problemId);
      setRootCauseAnalysis(analysis);
    } catch (error: any) {
      // 如果没有分析记录，不显示错误
      if (!error.message.includes('404')) {
        message.error('加载根本原因分析失败：' + error.message);
      }
    } finally {
      setLoadingAnalysis(false);
    }
  };

  const handleSaveAnalysis = async (values: CreateRootCauseAnalysisRequest) => {
    if (!problemId) return;
    try {
      const analysis = await rootCauseAnalysisService.createOrUpdateAnalysis(problemId, values);
      setRootCauseAnalysis(analysis);
      message.success('根本原因分析已保存');
    } catch (error: any) {
      message.error('保存失败：' + error.message);
    }
  };

  const getStatusColor = (status: string) => {
    const statusMap: Record<string, string> = {
      '待分配': 'default',
      '处理中': 'processing',
      '待验证': 'warning',
      '验证中': 'processing',
      '已验证': 'success',
      '验证失败': 'error',
      '已关闭': 'default',
    };
    return statusMap[status] || 'default';
  };

  const getPriorityColor = (priority?: string) => {
    const priorityMap: Record<string, string> = {
      'P1': 'red',
      'P2': 'orange',
      'P3': 'blue',
    };
    return priorityMap[priority || ''] || 'default';
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!problem) {
    return (
      <Card>
        <Alert message="问题不存在" type="error" />
      </Card>
    );
  }

  return (
    <div>
      <Card>
        <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
          <Space>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
              返回
            </Button>
            <Title level={4} style={{ margin: 0 }}>
              <FileTextOutlined /> 问题详情
            </Title>
          </Space>
        </Space>

        <Tabs defaultActiveKey="info">
          <TabPane tab="基本信息" key="info">
            <Descriptions bordered column={2}>
              <Descriptions.Item label="项目号">{problem.projectNo}</Descriptions.Item>
              <Descriptions.Item label="项目名称">{problem.projectName}</Descriptions.Item>
              <Descriptions.Item label="问题序号">{problem.problemSequence}</Descriptions.Item>
              <Descriptions.Item label="问题分类">
                <Tag>{problem.problemCategory}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="优先级" span={2}>
                <Tag color={getPriorityColor(problem.priority)}>
                  {problem.priority || '-'}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="问题描述" span={2}>
                {problem.problemDescription}
              </Descriptions.Item>
              <Descriptions.Item label="发现日期">
                {dayjs(problem.foundDate).format('YYYY-MM-DD')}
              </Descriptions.Item>
              <Descriptions.Item label="完成日期">
                {problem.completedDate ? dayjs(problem.completedDate).format('YYYY-MM-DD') : '-'}
              </Descriptions.Item>
              <Descriptions.Item label="处理周期">
                {problem.processingDays !== null && problem.processingDays !== undefined
                  ? `${problem.processingDays} 天`
                  : '-'}
              </Descriptions.Item>
              <Descriptions.Item label="处理状态">
                <Tag color={getStatusColor(problem.status)}>{problem.status}</Tag>
              </Descriptions.Item>
              <Descriptions.Item label="主负责部门">{problem.primaryDepartment}</Descriptions.Item>
              <Descriptions.Item label="主负责人">{problem.primaryResponsible}</Descriptions.Item>
              <Descriptions.Item label="协作部门">{problem.collaboratingDepartment || '-'}</Descriptions.Item>
              <Descriptions.Item label="协作人员">{problem.collaboratingPerson || '-'}</Descriptions.Item>
              <Descriptions.Item label="处理方案" span={2}>
                {problem.solution || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="处理详情" span={2}>
                {problem.solutionDetails || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="验证状态">
                {problem.verificationStatus ? (
                  <Tag color={problem.verificationStatus === '验证通过' ? 'success' : 'error'}>
                    {problem.verificationStatus}
                  </Tag>
                ) : (
                  '-'
                )}
              </Descriptions.Item>
              <Descriptions.Item label="满意度评分">
                {problem.satisfactionScore ? (
                  <Tag color={problem.satisfactionScore >= 4 ? 'success' : problem.satisfactionScore >= 3 ? 'warning' : 'error'}>
                    {problem.satisfactionScore} 分
                  </Tag>
                ) : (
                  '-'
                )}
              </Descriptions.Item>
              <Descriptions.Item label="客户反馈" span={2}>
                {problem.customerFeedback || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="是否重复问题">
                <Tag color={problem.isRepeatProblem ? 'orange' : 'default'}>
                  {problem.isRepeatProblem ? '是' : '否'}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="关联工单号">
                {problem.relatedTicketNo ? (
                  <Button
                    type="link"
                    onClick={() => navigate(`/tickets/${problem.relatedTicketNo}`)}
                  >
                    {problem.relatedTicketNo}
                  </Button>
                ) : (
                  '-'
                )}
              </Descriptions.Item>
              <Descriptions.Item label="备注" span={2}>
                {problem.notes || '-'}
              </Descriptions.Item>
            </Descriptions>
          </TabPane>
          <TabPane tab="根本原因分析" key="analysis">
            <RootCauseAnalysisForm
              problemId={problemId!}
              problemCategory={problem.problemCategory}
              existingAnalysis={rootCauseAnalysis}
              onSave={handleSaveAnalysis}
              loading={loadingAnalysis}
            />
          </TabPane>
        </Tabs>
      </Card>
    </div>
  );
}


