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
  Tag,
  Popconfirm,
  Tabs,
  Descriptions,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import {
  notificationRuleService,
  SaveNotificationRuleRequest,
  NotificationRuleDto,
  NotificationLogDto,
} from '../../services/notificationRuleService';

const { TextArea } = Input;
const { Option } = Select;
const { TabPane } = Tabs;

export default function NotificationRuleConfig() {
  const [rules, setRules] = useState<NotificationRuleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingRule, setEditingRule] = useState<NotificationRuleDto | null>(null);
  const [form] = Form.useForm();
  const [activeTab, setActiveTab] = useState('rules');

  useEffect(() => {
    loadRules();
  }, []);

  const loadRules = async () => {
    try {
      setLoading(true);
      const data = await notificationRuleService.getNotificationRules();
      setRules(data);
    } catch (error) {
      console.error('Failed to load notification rules:', error);
      message.error('加载通知规则失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingRule(null);
    form.resetFields();
    form.setFieldsValue({
      ruleLevel: 'customer',
      triggerEvent: 'ticket_submitted',
      isActive: true,
      recipientsConfig: { userIds: [], chatIds: [] },
    });
    setModalVisible(true);
  };

  const handleEdit = (rule: NotificationRuleDto) => {
    setEditingRule(rule);
    form.setFieldsValue({
      ...rule,
      recipientsConfig: typeof rule.recipientsConfig === 'string'
        ? JSON.parse(rule.recipientsConfig)
        : rule.recipientsConfig,
    });
    setModalVisible(true);
  };

  const handleDelete = async (ruleId: string) => {
    try {
      await notificationRuleService.deleteNotificationRule(ruleId);
      message.success('删除成功');
      loadRules();
    } catch (error) {
      console.error('Failed to delete rule:', error);
      message.error('删除失败');
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const request: SaveNotificationRuleRequest = {
        ruleId: editingRule?.ruleId,
        ruleLevel: values.ruleLevel,
        customerId: values.customerId,
        projectId: values.projectId,
        deviceId: values.deviceId,
        triggerEvent: values.triggerEvent,
        recipientsConfig: values.recipientsConfig || {},
        templateOverride: values.templateOverride,
        isActive: values.isActive ?? true,
      };

      if (editingRule) {
        await notificationRuleService.updateNotificationRule(editingRule.ruleId, request);
        message.success('更新成功');
      } else {
        await notificationRuleService.createNotificationRule(request);
        message.success('创建成功');
      }

      setModalVisible(false);
      loadRules();
    } catch (error) {
      console.error('Failed to save rule:', error);
      message.error(editingRule ? '更新失败' : '创建失败');
    }
  };

  const ruleLevelMap: Record<string, { label: string; color: string }> = {
    customer: { label: '客户级别', color: 'blue' },
    project: { label: '项目级别', color: 'green' },
    device: { label: '设备级别', color: 'orange' },
  };

  const triggerEventMap: Record<string, string> = {
    ticket_submitted: '工单提交',
    solution_published: '解决方案发布',
    verification_completed: '验证完成',
    ticket_closed: '工单关闭',
  };

  const columns = [
    {
      title: '规则级别',
      dataIndex: 'ruleLevel',
      key: 'ruleLevel',
      render: (level: string) => {
        const info = ruleLevelMap[level] || { label: level, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '触发事件',
      dataIndex: 'triggerEvent',
      key: 'triggerEvent',
      render: (event: string) => triggerEventMap[event] || event,
    },
    {
      title: '接收人配置',
      dataIndex: 'recipientsConfig',
      key: 'recipientsConfig',
      render: (config: Record<string, any>) => {
        const userIds = config?.userIds || [];
        const chatIds = config?.chatIds || [];
        return (
          <span>
            {userIds.length > 0 && <Tag>用户: {userIds.length}</Tag>}
            {chatIds.length > 0 && <Tag>群组: {chatIds.length}</Tag>}
            {userIds.length === 0 && chatIds.length === 0 && <span>-</span>}
          </span>
        );
      },
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (active: boolean) => (
        <Tag color={active ? 'success' : 'default'}>{active ? '启用' : '禁用'}</Tag>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (time: string) => new Date(time).toLocaleString(),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: NotificationRuleDto) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
          <Popconfirm
            title="确定要删除这个通知规则吗？"
            onConfirm={() => handleDelete(record.ruleId)}
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Card
        title="通知规则配置"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            新建规则
          </Button>
        }
      >
        <Table
          dataSource={rules}
          columns={columns}
          rowKey="ruleId"
          loading={loading}
          pagination={{
            pageSize: 20,
            showTotal: (total) => `共 ${total} 条`,
          }}
        />
      </Card>

      {/* 创建/编辑规则弹窗 */}
      <Modal
        title={editingRule ? '编辑通知规则' : '新建通知规则'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={800}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="ruleLevel"
            label="规则级别"
            rules={[{ required: true, message: '请选择规则级别' }]}
          >
            <Select>
              <Option value="customer">客户级别</Option>
              <Option value="project">项目级别</Option>
              <Option value="device">设备级别</Option>
            </Select>
          </Form.Item>

          <Form.Item
            noStyle
            shouldUpdate={(prevValues, currentValues) =>
              prevValues.ruleLevel !== currentValues.ruleLevel
            }
          >
            {({ getFieldValue }) => {
              const ruleLevel = getFieldValue('ruleLevel');
              return (
                <>
                  {ruleLevel === 'customer' && (
                    <Form.Item name="customerId" label="客户ID">
                      <Input placeholder="请输入客户ID" />
                    </Form.Item>
                  )}
                  {ruleLevel === 'project' && (
                    <>
                      <Form.Item name="customerId" label="客户ID">
                        <Input placeholder="请输入客户ID" />
                      </Form.Item>
                      <Form.Item name="projectId" label="项目ID">
                        <Input placeholder="请输入项目ID" />
                      </Form.Item>
                    </>
                  )}
                  {ruleLevel === 'device' && (
                    <>
                      <Form.Item name="customerId" label="客户ID">
                        <Input placeholder="请输入客户ID" />
                      </Form.Item>
                      <Form.Item name="projectId" label="项目ID">
                        <Input placeholder="请输入项目ID" />
                      </Form.Item>
                      <Form.Item name="deviceId" label="设备ID">
                        <Input placeholder="请输入设备ID" />
                      </Form.Item>
                    </>
                  )}
                </>
              );
            }}
          </Form.Item>

          <Form.Item
            name="triggerEvent"
            label="触发事件"
            rules={[{ required: true, message: '请选择触发事件' }]}
          >
            <Select>
              <Option value="ticket_submitted">工单提交</Option>
              <Option value="solution_published">解决方案发布</Option>
              <Option value="verification_completed">验证完成</Option>
              <Option value="ticket_closed">工单关闭</Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="recipientsConfig"
            label="接收人配置"
            tooltip="JSON格式，支持 userIds, chatIds, roles, departments"
            rules={[{ required: true, message: '请输入接收人配置' }]}
          >
            <TextArea
              rows={6}
              placeholder='{"userIds": ["user1", "user2"], "chatIds": ["chat1"]}'
            />
          </Form.Item>

          <Form.Item name="templateOverride" label="自定义模板（可选）">
            <TextArea
              rows={4}
              placeholder="留空则使用默认模板。支持变量：{ticket_no}, {symptom_title}, {priority}, {web_url}, {solution_code}, {summary}, {app_deeplink}"
            />
          </Form.Item>

          <Form.Item name="isActive" label="启用状态" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

