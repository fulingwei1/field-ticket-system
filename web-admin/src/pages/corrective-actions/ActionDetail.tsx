"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Space,
  message,
  Modal,
  Form,
  Input,
  Select,
  DatePicker,
  Timeline,
} from 'antd';
import {
  ArrowLeftOutlined,
  EditOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';
import {
  correctiveActionService,
  CorrectiveActionDto,
  UpdateCorrectiveActionRequest,
  EffectivenessCheckRequest,
} from '../../services/correctiveActionService';
import dayjs from 'dayjs';

const { TextArea } = Input;
const { Option } = Select;

const STATUS_MAP: Record<string, { label: string; color: string }> = {
  open: { label: '待处理', color: 'default' },
  in_progress: { label: '进行中', color: 'processing' },
  completed: { label: '已完成', color: 'success' },
  closed: { label: '已关闭', color: 'default' },
  cancelled: { label: '已取消', color: 'error' },
};

const RESPONSIBILITY_MAP: Record<string, string> = {
  design: '设计问题',
  software: '软件问题',
  parameter: '参数问题',
  assembly: '装配问题',
  documentation: '文档问题',
  other: '其他',
  unknown: '未知',
};

export default function CorrectiveActionDetail() {
  const { actionId } = useParams<{ actionId: string }>();
  const [loading, setLoading] = useState(false);
  const [action, setAction] = useState<CorrectiveActionDto | null>(null);
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [evaluateModalVisible, setEvaluateModalVisible] = useState(false);
  const [form] = Form.useForm();
  const [evaluateForm] = Form.useForm();
  const navigate = useNavigate();

  useEffect(() => {
    if (actionId) {
      loadAction();
    }
  }, [actionId]);

  const loadAction = async () => {
    if (!actionId) return;

    try {
      setLoading(true);
      const data = await correctiveActionService.getAction(actionId);
      setAction(data);
      form.setFieldsValue({
        problemDescription: data.problemDescription,
        rootResponsibility: data.rootResponsibility,
        actionPlan: data.actionPlan,
        responsiblePersonId: data.responsiblePersonId,
        targetCompletionDate: data.targetCompletionDate ? dayjs(data.targetCompletionDate) : null,
        executionNotes: data.executionNotes,
      });
    } catch (error: any) {
      message.error(error.message || '加载整改任务详情失败');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdate = async (values: any) => {
    if (!actionId) return;

    try {
      const request: UpdateCorrectiveActionRequest = {
        problemDescription: values.problemDescription,
        rootResponsibility: values.rootResponsibility,
        actionPlan: values.actionPlan,
        responsiblePersonId: values.responsiblePersonId,
        targetCompletionDate: values.targetCompletionDate?.format('YYYY-MM-DD'),
        executionNotes: values.executionNotes,
      };

      await correctiveActionService.updateAction(actionId, request);
      message.success('更新成功');
      setEditModalVisible(false);
      loadAction();
    } catch (error: any) {
      message.error(error.message || '更新失败');
    }
  };

  const handleStatusChange = async (status: string) => {
    if (!actionId) return;

    try {
      await correctiveActionService.updateActionStatus(actionId, status);
      message.success('状态更新成功');
      loadAction();
    } catch (error: any) {
      message.error(error.message || '状态更新失败');
    }
  };

  const handleEvaluate = async (values: any) => {
    if (!actionId) return;

    try {
      const request: EffectivenessCheckRequest = {
        checkDate: values.checkDate.format('YYYY-MM-DD'),
        checkResult: values.checkResult,
        relatedTicketsAfter: values.relatedTicketsAfter,
        notes: values.notes,
      };

      await correctiveActionService.evaluateEffectiveness(actionId, request);
      message.success('效果评估成功');
      setEvaluateModalVisible(false);
      loadAction();
    } catch (error: any) {
      message.error(error.message || '效果评估失败');
    }
  };

  if (!action) {
    return <div>加载中...</div>;
  }

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title={
          <Space>
            <Button
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate('/corrective-actions')}
            >
              返回
            </Button>
            <span>整改任务详情 - {action.actionCode}</span>
          </Space>
        }
        extra={
          <Space>
            {action.status === 'open' && (
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={() => handleStatusChange('in_progress')}
              >
                开始处理
              </Button>
            )}
            {action.status === 'in_progress' && (
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={() => handleStatusChange('completed')}
              >
                标记完成
              </Button>
            )}
            <Button
              icon={<EditOutlined />}
              onClick={() => setEditModalVisible(true)}
            >
              编辑
            </Button>
            {action.status === 'completed' && (
              <Button
                onClick={() => setEvaluateModalVisible(true)}
              >
                效果评估
              </Button>
            )}
          </Space>
        }
        loading={loading}
      >
        <Descriptions column={2} bordered>
          <Descriptions.Item label="任务编号">{action.actionCode}</Descriptions.Item>
          <Descriptions.Item label="状态">
            <Tag color={STATUS_MAP[action.status]?.color}>
              {STATUS_MAP[action.status]?.label || action.status}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="触发类型">{action.triggerType === 'threshold' ? '阈值触发' : '手动创建'}</Descriptions.Item>
          <Descriptions.Item label="根因分类">
            {action.rootResponsibilityName ? (
              <Tag>{action.rootResponsibilityName}</Tag>
            ) : (
              '-'
            )}
          </Descriptions.Item>
          <Descriptions.Item label="负责人">{action.responsiblePersonName || '-'}</Descriptions.Item>
          <Descriptions.Item label="目标完成日期">
            {action.targetCompletionDate
              ? new Date(action.targetCompletionDate).toLocaleDateString()
              : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="创建时间" span={2}>
            {new Date(action.createdAt).toLocaleString()}
          </Descriptions.Item>
          <Descriptions.Item label="问题描述" span={2}>
            {action.problemDescription}
          </Descriptions.Item>
          <Descriptions.Item label="整改计划" span={2}>
            {action.actionPlan}
          </Descriptions.Item>
          {action.executionNotes && (
            <Descriptions.Item label="执行记录" span={2}>
              {action.executionNotes}
            </Descriptions.Item>
          )}
          {action.completedAt && (
            <Descriptions.Item label="完成时间">
              {new Date(action.completedAt).toLocaleString()}
            </Descriptions.Item>
          )}
          {action.completedByName && (
            <Descriptions.Item label="完成人">
              {action.completedByName}
            </Descriptions.Item>
          )}
          {action.relatedTicketNos && action.relatedTicketNos.length > 0 && (
            <Descriptions.Item label="关联工单" span={2}>
              <Space wrap>
                {action.relatedTicketNos.map((no) => (
                  <Button
                    key={no}
                    type="link"
                    onClick={() => navigate(`/tickets/${action.relatedTicketIds[action.relatedTicketNos!.indexOf(no)]}`)}
                  >
                    {no}
                  </Button>
                ))}
              </Space>
            </Descriptions.Item>
          )}
          {action.effectivenessCheck && (
            <Descriptions.Item label="效果评估" span={2}>
              <div>
                <p>评估日期: {new Date(action.effectivenessCheck.check_date as string).toLocaleDateString()}</p>
                <p>评估结果: {action.effectivenessCheck.check_result}</p>
                {action.effectivenessCheck.notes && (
                  <p>备注: {action.effectivenessCheck.notes}</p>
                )}
              </div>
            </Descriptions.Item>
          )}
        </Descriptions>

        {/* 编辑模态框 */}
        <Modal
          title="编辑整改任务"
          open={editModalVisible}
          onCancel={() => setEditModalVisible(false)}
          onOk={() => form.submit()}
          width={800}
        >
          <Form
            form={form}
            layout="vertical"
            onFinish={handleUpdate}
          >
            <Form.Item
              name="problemDescription"
              label="问题描述"
              rules={[{ required: true, message: '请输入问题描述' }]}
            >
              <TextArea rows={4} />
            </Form.Item>
            <Form.Item
              name="rootResponsibility"
              label="根因分类"
            >
              <Select>
                {Object.entries(RESPONSIBILITY_MAP).map(([value, label]) => (
                  <Option key={value} value={value}>
                    {label}
                  </Option>
                ))}
              </Select>
            </Form.Item>
            <Form.Item
              name="actionPlan"
              label="整改计划"
              rules={[{ required: true, message: '请输入整改计划' }]}
            >
              <TextArea rows={6} />
            </Form.Item>
            <Form.Item
              name="targetCompletionDate"
              label="目标完成日期"
            >
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item
              name="executionNotes"
              label="执行记录"
            >
              <TextArea rows={4} />
            </Form.Item>
          </Form>
        </Modal>

        {/* 效果评估模态框 */}
        <Modal
          title="效果评估"
          open={evaluateModalVisible}
          onCancel={() => setEvaluateModalVisible(false)}
          onOk={() => evaluateForm.submit()}
        >
          <Form
            form={evaluateForm}
            layout="vertical"
            onFinish={handleEvaluate}
          >
            <Form.Item
              name="checkDate"
              label="评估日期"
              rules={[{ required: true, message: '请选择评估日期' }]}
              initialValue={dayjs()}
            >
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item
              name="checkResult"
              label="评估结果"
              rules={[{ required: true, message: '请选择评估结果' }]}
            >
              <Select>
                <Option value="effective">有效</Option>
                <Option value="ineffective">无效</Option>
                <Option value="partial">部分有效</Option>
              </Select>
            </Form.Item>
            <Form.Item
              name="notes"
              label="备注"
            >
              <TextArea rows={4} />
            </Form.Item>
          </Form>
        </Modal>
      </Card>
    </div>
  );
}






