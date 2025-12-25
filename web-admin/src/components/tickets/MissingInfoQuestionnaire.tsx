/**
 * 问诊式补全缺失信息组件
 */
import React, { useState, useEffect } from 'react';
import { Card, Form, Radio, Input, InputNumber, Select, Upload, Button, message, Space, Alert } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import { ticketService, type QuestionItem, type CompleteMissingInfoRequest } from '../../services/ticketService';

const { TextArea } = Input;

interface MissingInfoQuestionnaireProps {
  ticketId: string;
  jcCode?: string;
  onComplete?: (ticketId: string) => void;
  onSkip?: () => void;
}

export const MissingInfoQuestionnaire: React.FC<MissingInfoQuestionnaireProps> = ({
  ticketId,
  jcCode,
  onComplete,
  onSkip,
}) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [questions, setQuestions] = useState<QuestionItem[]>([]);
  const [hasCriticalMissing, setHasCriticalMissing] = useState(false);
  const [loadingQuestions, setLoadingQuestions] = useState(true);

  useEffect(() => {
    loadMissingInfo();
  }, [ticketId, jcCode]);

  /**
   * 加载缺失信息
   */
  const loadMissingInfo = async () => {
    try {
      setLoadingQuestions(true);
      const result = await ticketService.getMissingInfo(ticketId, jcCode);
      setQuestions(result.questions);
      setHasCriticalMissing(result.hasCriticalMissing);
      
      // 初始化表单值
      const initialValues: Record<string, any> = {};
      result.questions.forEach((q) => {
        if (q.type === 'yes_no') {
          initialValues[q.questionId] = undefined;
        } else if (q.type === 'number') {
          initialValues[q.questionId] = undefined;
        } else if (q.type === 'text') {
          initialValues[q.questionId] = '';
        } else if (q.type === 'select') {
          initialValues[q.questionId] = undefined;
        }
      });
      form.setFieldsValue(initialValues);
    } catch (error: any) {
      message.error(error.message || '加载缺失信息失败');
    } finally {
      setLoadingQuestions(false);
    }
  };

  /**
   * 提交补全信息
   */
  const handleSubmit = async (values: Record<string, any>) => {
    try {
      setLoading(true);

      // 构建答案
      const answers: Record<string, any> = {};
      questions.forEach((q) => {
        const value = values[q.questionId];
        if (value !== undefined && value !== null && value !== '') {
          answers[q.questionId] = value;
        }
      });

      const request: CompleteMissingInfoRequest = {
        answers,
      };

      await ticketService.completeMissingInfo(ticketId, request);
      message.success('信息补全成功');
      
      if (onComplete) {
        onComplete(ticketId);
      }
    } catch (error: any) {
      message.error(error.message || '补全信息失败');
    } finally {
      setLoading(false);
    }
  };

  /**
   * 渲染问题输入
   */
  const renderQuestionInput = (question: QuestionItem) => {
    const { questionId, question: questionText, type, required, options, hint } = question;

    switch (type) {
      case 'yes_no':
        return (
          <Form.Item
            name={questionId}
            label={questionText}
            rules={required ? [{ required: true, message: '请选择' }] : []}
            tooltip={hint}
          >
            <Radio.Group>
              <Radio value="yes">是</Radio>
              <Radio value="no">否</Radio>
              <Radio value="unknown">不清楚</Radio>
            </Radio.Group>
          </Form.Item>
        );

      case 'number':
        return (
          <Form.Item
            name={questionId}
            label={questionText}
            rules={required ? [{ required: true, message: '请输入' }] : []}
            tooltip={hint}
          >
            <InputNumber style={{ width: '100%' }} placeholder="请输入数字" />
          </Form.Item>
        );

      case 'text':
        return (
          <Form.Item
            name={questionId}
            label={questionText}
            rules={required ? [{ required: true, message: '请输入' }] : []}
            tooltip={hint}
          >
            <TextArea rows={4} placeholder="请输入文本" />
          </Form.Item>
        );

      case 'select':
        return (
          <Form.Item
            name={questionId}
            label={questionText}
            rules={required ? [{ required: true, message: '请选择' }] : []}
            tooltip={hint}
          >
            <Select placeholder="请选择" options={options?.map((opt) => ({ label: opt, value: opt }))} />
          </Form.Item>
        );

      case 'file':
        return (
          <Form.Item
            name={questionId}
            label={questionText}
            rules={required ? [{ required: true, message: '请上传文件' }] : []}
            tooltip={hint}
          >
            <Upload
              beforeUpload={async (file) => {
                try {
                  const { attachmentService } = await import('../../services/attachmentService');
                  const attachment = await attachmentService.uploadAttachment(
                    ticketId,
                    file,
                    'file'
                  );
                  // 保存附件ID到表单
                  form.setFieldsValue({ [questionId]: attachment.attachmentId });
                  message.success('上传成功');
                  return false; // 阻止默认上传
                } catch (error: any) {
                  message.error(error.message || '上传失败');
                  return false;
                }
              }}
              maxCount={5}
              listType="picture-card"
            >
              <div>
                <UploadOutlined />
                <div style={{ marginTop: 8 }}>上传</div>
              </div>
            </Upload>
          </Form.Item>
        );

      default:
        return null;
    }
  };

  if (loadingQuestions) {
    return <Card>加载中...</Card>;
  }

  if (questions.length === 0) {
    return (
      <Card>
        <Alert message="没有缺失信息，可以直接提交工单" type="success" showIcon />
      </Card>
    );
  }

  return (
    <Card title="信息补全" extra={hasCriticalMissing && <span style={{ color: '#ff4d4f' }}>有关键信息缺失</span>}>
      {hasCriticalMissing && (
        <Alert
          message="检测到关键信息缺失"
          description="为了更准确地诊断问题，请补充以下关键信息"
          type="warning"
          showIcon
          style={{ marginBottom: 24 }}
        />
      )}

      <Form form={form} onFinish={handleSubmit} layout="vertical">
        {questions.map((question) => (
          <div key={question.questionId} style={{ marginBottom: 24 }}>
            {renderQuestionInput(question)}
          </div>
        ))}

        <Form.Item>
          <Space>
            <Button type="primary" htmlType="submit" loading={loading}>
              完成补全
            </Button>
            {onSkip && (
              <Button onClick={onSkip} disabled={hasCriticalMissing}>
                跳过
              </Button>
            )}
          </Space>
        </Form.Item>
      </Form>
    </Card>
  );
};

