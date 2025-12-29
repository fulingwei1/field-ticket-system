"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  message,
  Modal,
  Form,
  Input,
  Select,
  Switch,
  Popconfirm,
  Tag,
  Tooltip,
  Empty,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  CopyOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import {
  ticketTemplateService,
  TicketTemplateDto,
  CreateTicketTemplateRequest,
  UpdateTicketTemplateRequest,
} from '../../services/ticketTemplateService';
import { useNavigate } from 'react-router-dom';

const { TextArea } = Input;
const { Option } = Select;

export default function TicketTemplateList() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [templates, setTemplates] = useState<TicketTemplateDto[]>([]);
  const [isCreateModalVisible, setIsCreateModalVisible] = useState(false);
  const [isEditModalVisible, setIsEditModalVisible] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<TicketTemplateDto | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadTemplates();
  }, []);

  const loadTemplates = async () => {
    try {
      setLoading(true);
      const data = await ticketTemplateService.getTemplates();
      setTemplates(data);
    } catch (error: any) {
      message.error(error.message || '加载模板列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (values: CreateTicketTemplateRequest) => {
    try {
      await ticketTemplateService.createTemplate(values);
      message.success('创建模板成功');
      setIsCreateModalVisible(false);
      form.resetFields();
      loadTemplates();
    } catch (error: any) {
      message.error(error.message || '创建模板失败');
    }
  };

  const handleEdit = (template: TicketTemplateDto) => {
    setEditingTemplate(template);
    form.setFieldsValue({
      name: template.name,
      description: template.description,
      category: template.category,
      isPublic: template.isPublic,
    });
    setIsEditModalVisible(true);
  };

  const handleUpdate = async (values: UpdateTicketTemplateRequest) => {
    if (!editingTemplate) return;

    try {
      await ticketTemplateService.updateTemplate(editingTemplate.templateId, values);
      message.success('更新模板成功');
      setIsEditModalVisible(false);
      setEditingTemplate(null);
      form.resetFields();
      loadTemplates();
    } catch (error: any) {
      message.error(error.message || '更新模板失败');
    }
  };

  const handleDelete = async (templateId: string) => {
    try {
      await ticketTemplateService.deleteTemplate(templateId);
      message.success('删除模板成功');
      loadTemplates();
    } catch (error: any) {
      message.error(error.message || '删除模板失败');
    }
  };

  const handleUseTemplate = (template: TicketTemplateDto) => {
    // 跳转到工单创建页面，并传递模板ID
    navigate(`/tickets/create?templateId=${template.templateId}`);
  };

  const columns = [
    {
      title: '模板名称',
      dataIndex: 'name',
      key: 'name',
      render: (text: string, record: TicketTemplateDto) => (
        <Space>
          <FileTextOutlined />
          <Button
            type="link"
            onClick={() => handleUseTemplate(record)}
            style={{ padding: 0 }}
          >
            {text}
          </Button>
        </Space>
      ),
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '分类',
      dataIndex: 'category',
      key: 'category',
      render: (category?: string) => (category ? <Tag>{category}</Tag> : '-'),
    },
    {
      title: '使用次数',
      dataIndex: 'usageCount',
      key: 'usageCount',
      render: (count: number) => (
        <Tooltip title={`最后使用: ${count > 0 ? '有' : '无'}`}>
          {count}
        </Tooltip>
      ),
    },
    {
      title: '公开',
      dataIndex: 'isPublic',
      key: 'isPublic',
      render: (isPublic: boolean) => (
        <Tag color={isPublic ? 'green' : 'default'}>
          {isPublic ? '是' : '否'}
        </Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: TicketTemplateDto) => (
        <Space>
          <Button
            type="link"
            icon={<CopyOutlined />}
            onClick={() => handleUseTemplate(record)}
            size="small"
          >
            使用
          </Button>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
            size="small"
          >
            编辑
          </Button>
          <Popconfirm
            title="确定要删除此模板吗？"
            onConfirm={() => handleDelete(record.templateId)}
            okText="确定"
            cancelText="取消"
          >
            <Button
              type="link"
              danger
              icon={<DeleteOutlined />}
              size="small"
            >
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="工单模板管理"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setIsCreateModalVisible(true)}
          >
            创建模板
          </Button>
        }
      >
        {templates.length === 0 ? (
          <Empty description="暂无模板，点击上方按钮创建" />
        ) : (
          <Table
            columns={columns}
            dataSource={templates}
            rowKey="templateId"
            loading={loading}
            pagination={false}
          />
        )}
      </Card>

      {/* 创建模板模态框 */}
      <Modal
        title="创建模板"
        open={isCreateModalVisible}
        onCancel={() => {
          setIsCreateModalVisible(false);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        width={600}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleCreate}
          initialValues={{ isPublic: false }}
        >
          <Form.Item
            name="name"
            label="模板名称"
            rules={[{ required: true, message: '请输入模板名称' }]}
          >
            <Input placeholder="请输入模板名称" />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <TextArea rows={3} placeholder="请输入模板描述" />
          </Form.Item>
          <Form.Item name="category" label="分类">
            <Select placeholder="请选择分类" allowClear>
              <Option value="问题域A">问题域A</Option>
              <Option value="问题域B">问题域B</Option>
              <Option value="问题域C">问题域C</Option>
              <Option value="问题域D">问题域D</Option>
              <Option value="问题域E">问题域E</Option>
              <Option value="常见问题">常见问题</Option>
              <Option value="设备故障">设备故障</Option>
            </Select>
          </Form.Item>
          <Form.Item name="isPublic" label="公开" valuePropName="checked">
            <Switch />
          </Form.Item>
          <Form.Item
            name="templateData"
            label="模板数据"
            rules={[{ required: true, message: '请输入模板数据（JSON格式）' }]}
          >
            <TextArea
              rows={10}
              placeholder='请输入模板数据，JSON格式，例如：{"domain":"A","stepCode":"Step_120","symptomTitle":"问题描述",...}'
            />
          </Form.Item>
        </Form>
      </Modal>

      {/* 编辑模板模态框 */}
      <Modal
        title="编辑模板"
        open={isEditModalVisible}
        onCancel={() => {
          setIsEditModalVisible(false);
          setEditingTemplate(null);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        width={600}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleUpdate}
        >
          <Form.Item
            name="name"
            label="模板名称"
            rules={[{ required: true, message: '请输入模板名称' }]}
          >
            <Input placeholder="请输入模板名称" />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <TextArea rows={3} placeholder="请输入模板描述" />
          </Form.Item>
          <Form.Item name="category" label="分类">
            <Select placeholder="请选择分类" allowClear>
              <Option value="问题域A">问题域A</Option>
              <Option value="问题域B">问题域B</Option>
              <Option value="问题域C">问题域C</Option>
              <Option value="问题域D">问题域D</Option>
              <Option value="问题域E">问题域E</Option>
              <Option value="常见问题">常见问题</Option>
              <Option value="设备故障">设备故障</Option>
            </Select>
          </Form.Item>
          <Form.Item name="isPublic" label="公开" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}




















