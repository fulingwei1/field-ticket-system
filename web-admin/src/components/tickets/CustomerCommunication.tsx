"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Button,
  Select,
  Input,
  Space,
  message,
  Modal,
  Form,
  Table,
  Tag,
  Empty,
  Typography,
  Divider,
} from 'antd';
import {
  MessageOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import {
  communicationTemplateService,
  CommunicationTemplateDto,
  CustomerCommunicationDto,
  CreateCommunicationRequest,
} from '../../services/communicationTemplateService';

const { TextArea } = Input;
const { Option } = Select;
const { Text } = Typography;

interface CustomerCommunicationProps {
  ticketId: string;
  ticketData?: {
    customerName?: string;
    deviceSn?: string;
    symptomTitle?: string;
    stepName?: string;
    solutionCode?: string;
    releaseVersion?: string;
  };
}

export default function CustomerCommunication({ ticketId, ticketData }: CustomerCommunicationProps) {
  const [loading, setLoading] = useState(false);
  const [templates, setTemplates] = useState<CommunicationTemplateDto[]>([]);
  const [communications, setCommunications] = useState<CustomerCommunicationDto[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<string>('');
  const [renderedContent, setRenderedContent] = useState<string>('');
  const [modalVisible, setModalVisible] = useState(false);
  const [previewModalVisible, setPreviewModalVisible] = useState(false);
  const [form] = Form.useForm();

  useEffect(() => {
    loadTemplates();
    loadCommunications();
  }, [ticketId]);

  const loadTemplates = async () => {
    try {
      const templateList = await communicationTemplateService.getTemplates();
      setTemplates(templateList);
    } catch (error: any) {
      console.error('Failed to load templates:', error);
    }
  };

  const loadCommunications = async () => {
    try {
      setLoading(true);
      const commList = await communicationTemplateService.getTicketCommunications(ticketId);
      setCommunications(commList);
    } catch (error: any) {
      message.error(error.message || '加载沟通记录失败');
    } finally {
      setLoading(false);
    }
  };

  const handleTemplateChange = async (templateId: string) => {
    setSelectedTemplate(templateId);
    
    if (!templateId) {
      setRenderedContent('');
      return;
    }

    try {
      // 准备变量
      const variables: Record<string, string> = {
        customer_name: ticketData?.customerName || '客户',
        device_sn: ticketData?.deviceSn || '设备SN',
        symptom_title: ticketData?.symptomTitle || '问题描述',
        step_name: ticketData?.stepName || '步骤名称',
        solution_code: ticketData?.solutionCode || '解决方案编号',
        release_version: ticketData?.releaseVersion || '发布版本',
        expected_hours: '24',
      };

      const content = await communicationTemplateService.renderTemplate(templateId, variables);
      setRenderedContent(content);
      form.setFieldsValue({ content });
    } catch (error: any) {
      message.error(error.message || '渲染模板失败');
    }
  };

  const handleSubmit = async (values: any) => {
    try {
      const request: CreateCommunicationRequest = {
        ticketId,
        templateId: selectedTemplate || undefined,
        content: values.content,
        communicationType: values.communicationType || 'wechat',
        customerFeedback: values.customerFeedback,
      };

      await communicationTemplateService.saveCommunication(request);
      message.success('沟通记录保存成功');
      setModalVisible(false);
      form.resetFields();
      setSelectedTemplate('');
      setRenderedContent('');
      loadCommunications();
    } catch (error: any) {
      message.error(error.message || '保存沟通记录失败');
    }
  };

  const getCommunicationTypeLabel = (type: string) => {
    const labels: Record<string, string> = {
      wechat: '微信',
      phone: '电话',
      email: '邮件',
      onsite: '现场',
      sms: '短信',
    };
    return labels[type] || type;
  };

  const columns = [
    {
      title: '沟通时间',
      dataIndex: 'communicatedAt',
      key: 'communicatedAt',
      width: 180,
      render: (text: string) => new Date(text).toLocaleString(),
    },
    {
      title: '沟通方式',
      dataIndex: 'communicationType',
      key: 'communicationType',
      width: 100,
      render: (type: string) => <Tag>{getCommunicationTypeLabel(type)}</Tag>,
    },
    {
      title: '模板',
      dataIndex: 'templateName',
      key: 'templateName',
      width: 150,
      render: (text: string) => text || '-',
    },
    {
      title: '沟通内容',
      dataIndex: 'content',
      key: 'content',
      ellipsis: true,
    },
    {
      title: '沟通人',
      dataIndex: 'communicatedByName',
      key: 'communicatedByName',
      width: 120,
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: any, record: CustomerCommunicationDto) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => {
            setPreviewModalVisible(true);
            form.setFieldsValue({ previewContent: record.content });
          }}
        >
          查看
        </Button>
      ),
    },
  ];

  return (
    <div>
      <Card
        title={
          <Space>
            <MessageOutlined />
            <span>客户沟通记录</span>
          </Space>
        }
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setModalVisible(true)}
          >
            新建沟通
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={communications}
          rowKey="communicationId"
          loading={loading}
          pagination={false}
          locale={{ emptyText: <Empty description="暂无沟通记录" /> }}
        />
      </Card>

      {/* 新建沟通模态框 */}
      <Modal
        title="新建客户沟通"
        open={modalVisible}
        onCancel={() => {
          setModalVisible(false);
          form.resetFields();
          setSelectedTemplate('');
          setRenderedContent('');
        }}
        onOk={() => form.submit()}
        width={700}
      >
        <Form form={form} onFinish={handleSubmit} layout="vertical">
          <Form.Item label="选择模板" name="templateId">
            <Select
              placeholder="选择沟通模板（可选）"
              allowClear
              onChange={handleTemplateChange}
            >
              {templates.map((tpl) => (
                <Option key={tpl.templateId} value={tpl.templateId}>
                  {tpl.name} ({tpl.scenario})
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            label="沟通内容"
            name="content"
            rules={[{ required: true, message: '请输入沟通内容' }]}
          >
            <TextArea
              rows={6}
              placeholder="沟通内容（选择模板后会自动填充）"
              value={renderedContent}
              onChange={(e) => {
                setRenderedContent(e.target.value);
                form.setFieldsValue({ content: e.target.value });
              }}
            />
          </Form.Item>

          <Form.Item label="沟通方式" name="communicationType" initialValue="wechat">
            <Select>
              <Option value="wechat">微信</Option>
              <Option value="phone">电话</Option>
              <Option value="email">邮件</Option>
              <Option value="onsite">现场</Option>
              <Option value="sms">短信</Option>
            </Select>
          </Form.Item>

          <Form.Item label="客户反馈" name="customerFeedback">
            <TextArea rows={3} placeholder="客户反馈（可选）" />
          </Form.Item>
        </Form>
      </Modal>

      {/* 查看详情模态框 */}
      <Modal
        title="沟通记录详情"
        open={previewModalVisible}
        onCancel={() => {
          setPreviewModalVisible(false);
          form.resetFields();
        }}
        footer={[
          <Button key="close" onClick={() => setPreviewModalVisible(false)}>
            关闭
          </Button>,
        ]}
        width={600}
      >
        <Form form={form} layout="vertical">
          <Form.Item label="沟通内容" name="previewContent">
            <TextArea rows={8} readOnly />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}











