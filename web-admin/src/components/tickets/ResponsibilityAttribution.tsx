"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Form,
  Select,
  Input,
  Button,
  Space,
  message,
  Descriptions,
  Tag,
  Modal,
  Radio,
  Alert,
} from 'antd';
import {
  CheckCircleOutlined,
  EditOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons';
import {
  responsibilityAttributionService,
  ResponsibilityAttributionDto,
  AttributeResponsibilityRequest,
} from '../../services/responsibilityAttributionService';
import { aiAttributionService, AttributionSuggestionDto } from '../../services/aiAttributionService';

const { TextArea } = Input;
const { Option } = Select;

interface ResponsibilityAttributionProps {
  ticketId: string;
}

const RESPONSIBILITY_OPTIONS = [
  { value: 'design', label: '设计问题' },
  { value: 'software', label: '软件问题' },
  { value: 'parameter', label: '参数问题' },
  { value: 'assembly', label: '装配问题' },
  { value: 'documentation', label: '文档问题' },
  { value: 'other', label: '其他' },
  { value: 'unknown', label: '未知' },
];

export default function ResponsibilityAttribution({ ticketId }: ResponsibilityAttributionProps) {
  const [loading, setLoading] = useState(false);
  const [attribution, setAttribution] = useState<ResponsibilityAttributionDto | null>(null);
  const [modalVisible, setModalVisible] = useState(false);
  const [aiSuggestionLoading, setAiSuggestionLoading] = useState(false);
  const [aiSuggestion, setAiSuggestion] = useState<AttributionSuggestionDto | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadAttribution();
  }, [ticketId]);

  const loadAttribution = async () => {
    try {
      setLoading(true);
      const data = await responsibilityAttributionService.getAttribution(ticketId);
      setAttribution(data);
      if (data) {
        form.setFieldsValue({
          rootResponsibility: data.rootResponsibility,
          isPreventable: data.isPreventable,
          notes: data.responsibilityNotes,
        });
      }
    } catch (error: any) {
      console.error('Failed to load attribution:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (values: any) => {
    try {
      const request: AttributeResponsibilityRequest = {
        rootResponsibility: values.rootResponsibility,
        isPreventable: values.isPreventable,
        notes: values.notes,
      };

      await responsibilityAttributionService.attributeResponsibility(ticketId, request);
      message.success('责任归因成功');
      setModalVisible(false);
      form.resetFields();
      loadAttribution();
    } catch (error: any) {
      message.error(error.message || '归因失败');
    }
  };

  const handleGetAISuggestion = async () => {
    try {
      setAiSuggestionLoading(true);
      const suggestion = await aiAttributionService.suggestAttribution(ticketId);
      setAiSuggestion(suggestion);
      form.setFieldsValue({
        rootResponsibility: suggestion.rootResponsibility,
        isPreventable: suggestion.isPreventable ?? true,
      });
    } catch (error: any) {
      message.error(error.message || '获取AI建议失败');
    } finally {
      setAiSuggestionLoading(false);
    }
  };

  const getResponsibilityLabel = (value?: string) => {
    if (!value) return '-';
    const option = RESPONSIBILITY_OPTIONS.find(opt => opt.value === value);
    return option?.label || value;
  };

  return (
    <div>
      <Card
        title={
          <Space>
            <InfoCircleOutlined />
            <span>责任归因</span>
          </Space>
        }
        extra={
          !attribution && (
            <Button
              type="primary"
              icon={<EditOutlined />}
              onClick={() => setModalVisible(true)}
            >
              进行归因
            </Button>
          )
        }
        loading={loading}
      >
        {attribution ? (
          <Descriptions column={2} bordered>
            <Descriptions.Item label="根因分类">
              <Tag color="blue">{attribution.rootResponsibilityName || getResponsibilityLabel(attribution.rootResponsibility)}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="是否可预防">
              {attribution.isPreventable !== undefined ? (
                <Tag color={attribution.isPreventable ? 'green' : 'orange'}>
                  {attribution.isPreventable ? '可预防' : '不可预防'}
                </Tag>
              ) : (
                '-'
              )}
            </Descriptions.Item>
            {attribution.responsibilityNotes && (
              <Descriptions.Item label="归因备注" span={2}>
                {attribution.responsibilityNotes}
              </Descriptions.Item>
            )}
            {attribution.attributedByName && (
              <Descriptions.Item label="归因人">
                {attribution.attributedByName}
              </Descriptions.Item>
            )}
            {attribution.attributedAt && (
              <Descriptions.Item label="归因时间">
                {new Date(attribution.attributedAt).toLocaleString()}
              </Descriptions.Item>
            )}
          </Descriptions>
        ) : (
          <Alert
            message="未进行责任归因"
            description="请对工单进行责任归因，以便进行持续改进分析。"
            type="info"
            showIcon
            action={
              <Button size="small" onClick={() => setModalVisible(true)}>
                立即归因
              </Button>
            }
          />
        )}
      </Card>

      {/* 归因模态框 */}
      <Modal
        title="责任归因"
        open={modalVisible}
        onCancel={() => {
          setModalVisible(false);
          form.resetFields();
          setAiSuggestion(null);
        }}
        onOk={() => form.submit()}
        width={600}
      >
        <Form form={form} onFinish={handleSubmit} layout="vertical">
          <Form.Item>
            <Space>
              <Button
                type="dashed"
                loading={aiSuggestionLoading}
                onClick={handleGetAISuggestion}
              >
                获取AI建议
              </Button>
              {aiSuggestion && (
                <Tag color="blue">
                  置信度: {aiSuggestion.confidence ? `${(aiSuggestion.confidence * 100).toFixed(0)}%` : 'N/A'}
                </Tag>
              )}
            </Space>
          </Form.Item>

          {aiSuggestion && aiSuggestion.reasons && aiSuggestion.reasons.length > 0 && (
            <Alert
              message="AI建议"
              description={aiSuggestion.reasons.join('; ')}
              type="info"
              style={{ marginBottom: 16 }}
              showIcon
            />
          )}

          <Form.Item
            label="根因分类"
            name="rootResponsibility"
            rules={[{ required: true, message: '请选择根因分类' }]}
          >
            <Select placeholder="选择根因分类">
              {RESPONSIBILITY_OPTIONS.map(opt => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            label="是否可预防"
            name="isPreventable"
            rules={[{ required: true, message: '请选择是否可预防' }]}
          >
            <Radio.Group>
              <Radio value={true}>可预防</Radio>
              <Radio value={false}>不可预防</Radio>
            </Radio.Group>
          </Form.Item>

          <Form.Item label="归因备注" name="notes">
            <TextArea rows={4} placeholder="归因备注（可选）" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

