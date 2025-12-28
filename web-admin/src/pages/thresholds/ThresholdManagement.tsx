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
  DatePicker,
  Tag,
  Progress,
  Descriptions,
  Alert,
  Spin,
} from 'antd';
import {
  PlusOutlined,
  ReloadOutlined,
  ExperimentOutlined,
  BarChartOutlined,
  ThunderboltOutlined,
} from '@ant-design/icons';
import {
  smartThresholdService,
  ThresholdConfigDto,
  ThresholdEvaluation,
  LearnOptimalThresholdRequest,
  AutoOptimizeThresholdRequest,
} from '../../services/smartThresholdService';
import dayjs, { Dayjs } from 'dayjs';

const { Option } = Select;

export default function ThresholdManagement() {
  const [loading, setLoading] = useState(false);
  const [configs, setConfigs] = useState<ThresholdConfigDto[]>([]);
  const [selectedConfig, setSelectedConfig] = useState<ThresholdConfigDto | null>(null);
  const [evaluation, setEvaluation] = useState<ThresholdEvaluation | null>(null);
  const [evaluating, setEvaluating] = useState(false);
  const [learning, setLearning] = useState(false);
  const [optimizing, setOptimizing] = useState(false);
  const [learnModalVisible, setLearnModalVisible] = useState(false);
  const [evaluateModalVisible, setEvaluateModalVisible] = useState(false);
  const [optimizeModalVisible, setOptimizeModalVisible] = useState(false);
  const [learnForm] = Form.useForm();
  const [evaluateForm] = Form.useForm();
  const [optimizeForm] = Form.useForm();

  useEffect(() => {
    loadConfigs();
  }, []);

  const loadConfigs = async () => {
    setLoading(true);
    try {
      const data = await smartThresholdService.getThresholdConfigs();
      setConfigs(data);
    } catch (error: any) {
      message.error('加载阈值配置失败');
      console.error('Failed to load configs:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLearnOptimal = async (values: any) => {
    setLearning(true);
    try {
      const request: LearnOptimalThresholdRequest = {
        scenarioType: values.scenarioType,
        scenarioValue: values.scenarioValue,
        fromDate: values.dateRange?.[0]?.format('YYYY-MM-DD'),
        toDate: values.dateRange?.[1]?.format('YYYY-MM-DD'),
      };
      await smartThresholdService.learnOptimalThreshold(request);
      message.success('阈值学习完成');
      setLearnModalVisible(false);
      learnForm.resetFields();
      loadConfigs();
    } catch (error: any) {
      message.error('学习阈值失败');
      console.error('Failed to learn threshold:', error);
    } finally {
      setLearning(false);
    }
  };

  const handleEvaluate = async (configId: string, values?: any) => {
    setEvaluating(true);
    try {
      const evaluation = await smartThresholdService.evaluateThreshold(
        configId,
        values?.fromDate?.format('YYYY-MM-DD'),
        values?.toDate?.format('YYYY-MM-DD')
      );
      setEvaluation(evaluation);
      setSelectedConfig(configs.find(c => c.configId === configId) || null);
      setEvaluateModalVisible(true);
    } catch (error: any) {
      message.error('评估阈值失败');
      console.error('Failed to evaluate threshold:', error);
    } finally {
      setEvaluating(false);
    }
  };

  const handleAutoOptimize = async (configId: string, values: any) => {
    setOptimizing(true);
    try {
      const request: AutoOptimizeThresholdRequest = {
        configId,
        applyOptimization: values.applyOptimization,
      };
      await smartThresholdService.autoOptimizeThreshold(configId, request);
      message.success('阈值优化完成');
      setOptimizeModalVisible(false);
      optimizeForm.resetFields();
      loadConfigs();
    } catch (error: any) {
      message.error('优化阈值失败');
      console.error('Failed to optimize threshold:', error);
    } finally {
      setOptimizing(false);
    }
  };

  const columns = [
    {
      title: '配置名称',
      dataIndex: 'configName',
      key: 'configName',
    },
    {
      title: '场景类型',
      dataIndex: 'scenarioType',
      key: 'scenarioType',
      render: (type: string) => {
        const typeMap: Record<string, { label: string; color: string }> = {
          device_type: { label: '设备类型', color: 'blue' },
          problem_type: { label: '问题类型', color: 'green' },
          default: { label: '默认', color: 'default' },
        };
        const info = typeMap[type] || { label: type, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '场景值',
      dataIndex: 'scenarioValue',
      key: 'scenarioValue',
    },
    {
      title: '时间窗口',
      dataIndex: 'timeWindowDays',
      key: 'timeWindowDays',
      render: (days: number) => `${days} 天`,
    },
    {
      title: '触发次数',
      dataIndex: 'triggerCount',
      key: 'triggerCount',
    },
    {
      title: '准确率',
      dataIndex: 'accuracyRate',
      key: 'accuracyRate',
      render: (rate: number | undefined) =>
        rate !== undefined ? (
          <Progress percent={Math.round(rate * 100)} size="small" />
        ) : (
          '-'
        ),
    },
    {
      title: '状态',
      key: 'status',
      render: (_: any, record: ThresholdConfigDto) => (
        <Space>
          <Tag color={record.isActive ? 'success' : 'default'}>
            {record.isActive ? '启用' : '禁用'}
          </Tag>
          {record.isAutoOptimized && (
            <Tag color="purple">自动优化</Tag>
          )}
        </Space>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_: any, record: ThresholdConfigDto) => (
        <Space>
          <Button
            size="small"
            icon={<BarChartOutlined />}
            onClick={() => handleEvaluate(record.configId)}
          >
            评估
          </Button>
          <Button
            size="small"
            icon={<ThunderboltOutlined />}
            onClick={() => {
              setSelectedConfig(record);
              setOptimizeModalVisible(true);
            }}
          >
            优化
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Card>
        <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
          <h2>智能阈值管理</h2>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadConfigs}>
              刷新
            </Button>
            <Button
              type="primary"
              icon={<ExperimentOutlined />}
              onClick={() => setLearnModalVisible(true)}
            >
              学习最优阈值
            </Button>
          </Space>
        </Space>

        <Table
          columns={columns}
          dataSource={configs}
          rowKey="configId"
          loading={loading}
          pagination={{ pageSize: 10 }}
        />
      </Card>

      {/* 学习最优阈值模态框 */}
      <Modal
        title="学习最优阈值"
        open={learnModalVisible}
        onCancel={() => {
          setLearnModalVisible(false);
          learnForm.resetFields();
        }}
        onOk={() => learnForm.submit()}
        confirmLoading={learning}
      >
        <Form form={learnForm} onFinish={handleLearnOptimal} layout="vertical">
          <Form.Item
            name="scenarioType"
            label="场景类型"
            rules={[{ required: true, message: '请选择场景类型' }]}
          >
            <Select>
              <Option value="device_type">设备类型</Option>
              <Option value="problem_type">问题类型</Option>
              <Option value="default">默认</Option>
            </Select>
          </Form.Item>
          <Form.Item name="scenarioValue" label="场景值">
            <Input placeholder="可选，如设备型号或问题域" />
          </Form.Item>
          <Form.Item name="dateRange" label="数据时间范围">
            <DatePicker.RangePicker style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>

      {/* 评估阈值效果模态框 */}
      <Modal
        title="阈值效果评估"
        open={evaluateModalVisible}
        onCancel={() => {
          setEvaluateModalVisible(false);
          setEvaluation(null);
          evaluateForm.resetFields();
        }}
        footer={[
          <Button key="close" onClick={() => setEvaluateModalVisible(false)}>
            关闭
          </Button>,
        ]}
        width={600}
      >
        {evaluating ? (
          <Spin />
        ) : evaluation ? (
          <Descriptions bordered column={1}>
            <Descriptions.Item label="总触发次数">
              {evaluation.totalTriggers}
            </Descriptions.Item>
            <Descriptions.Item label="正确触发">
              {evaluation.correctTriggers}
            </Descriptions.Item>
            <Descriptions.Item label="误触发">
              {evaluation.falsePositives}
            </Descriptions.Item>
            <Descriptions.Item label="漏触发">
              {evaluation.falseNegatives}
            </Descriptions.Item>
            <Descriptions.Item label="准确率">
              <Progress
                percent={Math.round(evaluation.accuracyRate * 100)}
                status={evaluation.accuracyRate >= 0.8 ? 'success' : evaluation.accuracyRate >= 0.6 ? 'normal' : 'exception'}
              />
            </Descriptions.Item>
            <Descriptions.Item label="误触发率">
              <Progress
                percent={Math.round(evaluation.falsePositiveRate * 100)}
                status={evaluation.falsePositiveRate <= 0.2 ? 'success' : evaluation.falsePositiveRate <= 0.4 ? 'normal' : 'exception'}
              />
            </Descriptions.Item>
            <Descriptions.Item label="综合评分">
              <Progress
                percent={Math.round(evaluation.score * 100)}
                status={evaluation.score >= 0.7 ? 'success' : 'exception'}
              />
            </Descriptions.Item>
          </Descriptions>
        ) : null}
      </Modal>

      {/* 自动优化阈值模态框 */}
      <Modal
        title="自动优化阈值"
        open={optimizeModalVisible}
        onCancel={() => {
          setOptimizeModalVisible(false);
          optimizeForm.resetFields();
        }}
        onOk={() => optimizeForm.submit()}
        confirmLoading={optimizing}
      >
        {selectedConfig && (
          <Alert
            message="当前配置"
            description={
              <div>
                <p>时间窗口: {selectedConfig.timeWindowDays} 天</p>
                <p>触发次数: {selectedConfig.triggerCount}</p>
                {selectedConfig.accuracyRate !== undefined && (
                  <p>当前准确率: {Math.round(selectedConfig.accuracyRate * 100)}%</p>
                )}
              </div>
            }
            type="info"
            style={{ marginBottom: 16 }}
          />
        )}
        <Form
          form={optimizeForm}
          onFinish={(values) => selectedConfig && handleAutoOptimize(selectedConfig.configId, values)}
          layout="vertical"
          initialValues={{ applyOptimization: false }}
        >
          <Form.Item
            name="applyOptimization"
            valuePropName="checked"
            label="应用优化结果"
          >
            <Select>
              <Option value={false}>仅预览优化结果</Option>
              <Option value={true}>应用优化结果</Option>
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}








