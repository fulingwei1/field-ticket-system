"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Form,
  Button,
  Space,
  message,
  Spin,
  Descriptions,
  Tag,
} from 'antd';
import { ArrowLeftOutlined, SaveOutlined, SendOutlined } from '@ant-design/icons';
import { solutionService, CreateSolutionRequest, UpdateSolutionRequest, SolutionDto } from '../../services/solutionService';
import { ticketService, TicketDto } from '../../services/ticketService';
import SolutionForm from '../../components/solutions/SolutionForm';

export default function SolutionEditor() {
  const { ticketId, solutionId } = useParams<{ ticketId: string; solutionId?: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [ticket, setTicket] = useState<TicketDto | null>(null);
  const [solution, setSolution] = useState<SolutionDto | null>(null);
  const [loadingData, setLoadingData] = useState(true);
  const isEdit = !!solutionId;

  useEffect(() => {
    loadData();
  }, [ticketId, solutionId]);

  const loadData = async () => {
    try {
      setLoadingData(true);
      
      let actualTicketId = ticketId;
      
      // 如果是编辑模式，先从解决方案获取ticketId
      if (solutionId && !ticketId) {
        const solutionData = await solutionService.getSolution(solutionId);
        setSolution(solutionData);
        actualTicketId = solutionData.ticketId;
      }
      
      if (!actualTicketId) {
        message.error('工单ID不存在');
        navigate('/tickets');
        return;
      }
      
      // 加载工单信息
      const ticketData = await ticketService.getTicket(actualTicketId);
      setTicket(ticketData);

      // 如果是编辑模式，加载解决方案（如果还没加载）
      if (solutionId && !solution) {
        const solutionData = await solutionService.getSolution(solutionId);
        setSolution(solutionData);
        
        // 填充表单
        form.setFieldsValue({
          title: solutionData.title,
          description: solutionData.description,
          solutionType: solutionData.solutionType,
          releaseType: solutionData.releaseType,
          requiredSwVersion: solutionData.requiredSwVersion,
          requiredPlcVersion: solutionData.requiredPlcVersion,
          requiredParamVersion: solutionData.requiredParamVersion,
          newSwVersion: solutionData.newSwVersion,
          newPlcVersion: solutionData.newPlcVersion,
          newParamVersion: solutionData.newParamVersion,
          changeDetailJson: JSON.stringify(solutionData.changeDetailJson, null, 2),
          verificationChecklistJson: JSON.stringify(solutionData.verificationChecklistJson, null, 2),
          implementationSteps: solutionData.implementationSteps,
          estimatedImplementationTime: solutionData.estimatedImplementationTime,
          riskLevel: solutionData.riskLevel,
          riskDescription: solutionData.riskDescription,
          rollbackPossible: solutionData.rollbackPossible,
          rollbackProcedure: solutionData.rollbackProcedure,
        });
      }
    } catch (error: any) {
      message.error(error.message || '加载数据失败');
      navigate('/tickets');
    } finally {
      setLoadingData(false);
    }
  };

  const handleSave = async () => {
    const actualTicketId = ticketId || solution?.ticketId;
    if (!actualTicketId) {
      message.error('工单ID不存在');
      return;
    }

    try {
      const values = await form.validateFields();
      
      // 解析JSON字段
      const changeDetailJson = typeof values.changeDetailJson === 'string'
        ? JSON.parse(values.changeDetailJson)
        : values.changeDetailJson;
      const verificationChecklistJson = typeof values.verificationChecklistJson === 'string'
        ? JSON.parse(values.verificationChecklistJson)
        : values.verificationChecklistJson;

      setSaving(true);

      if (isEdit && solutionId) {
        // 更新解决方案
        const updateRequest: UpdateSolutionRequest = {
          title: values.title,
          description: values.description,
          solutionType: values.solutionType,
          releaseType: values.releaseType,
          requiredSwVersion: values.requiredSwVersion,
          requiredPlcVersion: values.requiredPlcVersion,
          requiredParamVersion: values.requiredParamVersion,
          newSwVersion: values.newSwVersion,
          newPlcVersion: values.newPlcVersion,
          newParamVersion: values.newParamVersion,
          changeDetailJson,
          verificationChecklistJson,
          implementationSteps: values.implementationSteps,
          estimatedImplementationTime: values.estimatedImplementationTime,
          riskLevel: values.riskLevel,
          riskDescription: values.riskDescription,
          rollbackPossible: values.rollbackPossible,
          rollbackProcedure: values.rollbackProcedure,
        };

        await solutionService.updateSolution(solutionId, updateRequest);
        message.success('解决方案已保存');
      } else {
        // 创建解决方案
        const createRequest: CreateSolutionRequest = {
          title: values.title,
          description: values.description,
          solutionType: values.solutionType,
          releaseType: values.releaseType,
          requiredSwVersion: values.requiredSwVersion,
          requiredPlcVersion: values.requiredPlcVersion,
          requiredParamVersion: values.requiredParamVersion,
          newSwVersion: values.newSwVersion,
          newPlcVersion: values.newPlcVersion,
          newParamVersion: values.newParamVersion,
          changeDetailJson,
          verificationChecklistJson,
          implementationSteps: values.implementationSteps,
          estimatedImplementationTime: values.estimatedImplementationTime,
          riskLevel: values.riskLevel,
          riskDescription: values.riskDescription,
          rollbackPossible: values.rollbackPossible,
          rollbackProcedure: values.rollbackProcedure,
        };

        const newSolution = await solutionService.createSolution(actualTicketId, createRequest);
        message.success('解决方案已创建');
        navigate(`/solutions/${newSolution.solutionId}/edit`);
      }
    } catch (error: any) {
      if (error.errorFields) {
        // 表单验证错误
        return;
      }
      message.error(error.message || '保存失败');
    } finally {
      setSaving(false);
    }
  };

  const handlePublish = async () => {
    if (!solutionId) {
      message.warning('请先保存解决方案');
      return;
    }

    const actualTicketId = ticketId || solution?.ticketId;
    if (!actualTicketId) {
      message.error('工单ID不存在');
      return;
    }

    try {
      setPublishing(true);
      const result = await solutionService.publishSolution(solutionId);
      message.success(`解决方案已发布，编号：${result.solutionCode}`);
      navigate(`/tickets/${actualTicketId}`);
    } catch (error: any) {
      message.error(error.message || '发布失败');
    } finally {
      setPublishing(false);
    }
  };

  if (loadingData) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!ticket) {
    return null;
  }

  return (
    <div style={{ padding: '24px' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 返回按钮 */}
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => {
            const actualTicketId = ticketId || solution?.ticketId;
            if (actualTicketId) {
              navigate(`/tickets/${actualTicketId}`);
            } else {
              navigate('/tickets');
            }
          }}
        >
          返回工单详情
        </Button>

        {/* 工单信息 */}
        <Card title="关联工单">
          <Descriptions column={2} bordered>
            <Descriptions.Item label="工单编号">{ticket.ticketNo || '未生成'}</Descriptions.Item>
            <Descriptions.Item label="状态">
              <Tag color={ticket.status === 'Triage' || ticket.status === 'SolutionIssued' ? 'blue' : 'default'}>
                {ticket.status}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="问题域">{ticket.domain}</Descriptions.Item>
            <Descriptions.Item label="步骤代码">{ticket.stepCode}</Descriptions.Item>
            <Descriptions.Item label="症状标题" span={2}>
              {ticket.symptomTitle}
            </Descriptions.Item>
          </Descriptions>
        </Card>

        {/* 解决方案表单 */}
        <Card
          title={isEdit ? '编辑解决方案' : '创建解决方案'}
          extra={
            <Space>
              <Button
                icon={<SaveOutlined />}
                onClick={handleSave}
                loading={saving}
              >
                保存
              </Button>
              {isEdit && solution?.status === 'Draft' && (
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  onClick={handlePublish}
                  loading={publishing}
                >
                  发布
                </Button>
              )}
            </Space>
          }
        >
          <SolutionForm form={form} isEdit={isEdit} />
        </Card>
      </Space>
    </div>
  );
}
