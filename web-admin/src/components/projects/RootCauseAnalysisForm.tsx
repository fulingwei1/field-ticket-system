"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Form,
  Input,
  Select,
  Button,
  Space,
  message,
  Alert,
  List,
  Tag,
  Typography,
  Spin,
} from 'antd';
import {
  SaveOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  rootCauseAnalysisService,
  RootCauseAnalysisDto,
  CreateRootCauseAnalysisRequest,
  FiveWhyTemplate,
  PreventiveMeasureSuggestion,
} from '../../services/rootCauseAnalysisService';

const { TextArea } = Input;
const { Option } = Select;
const { Title, Text } = Typography;

interface RootCauseAnalysisFormProps {
  problemId: string;
  problemCategory: string;
  existingAnalysis?: RootCauseAnalysisDto | null;
  onSave: (values: CreateRootCauseAnalysisRequest) => Promise<void>;
  loading?: boolean;
}

export default function RootCauseAnalysisForm({
  problemId,
  problemCategory,
  existingAnalysis,
  onSave,
  loading = false,
}: RootCauseAnalysisFormProps) {
  const [form] = Form.useForm();
  const [template, setTemplate] = useState<FiveWhyTemplate | null>(null);
  const [suggestions, setSuggestions] = useState<PreventiveMeasureSuggestion[]>([]);
  const [loadingTemplate, setLoadingTemplate] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<string>('');

  useEffect(() => {
    loadTemplate();
    if (existingAnalysis) {
      form.setFieldsValue({
        why1: existingAnalysis.why1,
        why2: existingAnalysis.why2,
        why3: existingAnalysis.why3,
        why4: existingAnalysis.why4,
        why5: existingAnalysis.why5,
        rootCause: existingAnalysis.rootCause,
        rootCauseCategory: existingAnalysis.rootCauseCategory,
        preventiveMeasures: existingAnalysis.preventiveMeasures,
        verificationMethod: existingAnalysis.verificationMethod,
      });
      if (existingAnalysis.rootCauseCategory) {
        loadSuggestions(existingAnalysis.rootCauseCategory);
      }
    }
  }, [existingAnalysis, problemCategory]);

  const loadTemplate = async () => {
    setLoadingTemplate(true);
    try {
      const data = await rootCauseAnalysisService.getFiveWhyTemplate(problemCategory);
      setTemplate(data);
    } catch (error: any) {
      message.error('加载模板失败：' + error.message);
    } finally {
      setLoadingTemplate(false);
    }
  };

  const loadSuggestions = async (category: string) => {
    try {
      const data = await rootCauseAnalysisService.getPreventiveMeasureSuggestions(category);
      setSuggestions(data);
    } catch (error: any) {
      // 忽略错误，建议是可选的
    }
  };

  const handleCategoryChange = (category: string) => {
    setSelectedCategory(category);
    loadSuggestions(category);
  };

  const handleSubmit = async (values: CreateRootCauseAnalysisRequest) => {
    try {
      await onSave(values);
    } catch (error: any) {
      message.error('保存失败：' + error.message);
    }
  };

  if (loadingTemplate) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin />
      </div>
    );
  }

  return (
    <div>
      {template && (
        <Alert
          message="5Why分析模板"
          description={
            <div>
              <Text>问题分类：{template.problemCategory}</Text>
              {template.commonRootCauses.length > 0 && (
                <div style={{ marginTop: 8 }}>
                  <Text strong>常见根本原因：</Text>
                  <Space wrap style={{ marginTop: 4 }}>
                    {template.commonRootCauses.map((cause, index) => (
                      <Tag key={index}>{cause}</Tag>
                    ))}
                  </Space>
                </div>
              )}
            </div>
          }
          type="info"
          style={{ marginBottom: 16 }}
        />
      )}

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          rootCauseCategory: existingAnalysis?.rootCauseCategory || '',
        }}
      >
        <Card title="5Why分析" style={{ marginBottom: 16 }}>
          <Form.Item
            label={template?.whyQuestions[0] || "Why 1: 为什么会出现这个问题？"}
            name="why1"
            rules={[{ required: true, message: '请输入Why1' }]}
          >
            <TextArea rows={2} placeholder="请输入第一个为什么" />
          </Form.Item>

          <Form.Item
            label={template?.whyQuestions[1] || "Why 2: 为什么会有这个原因？"}
            name="why2"
            rules={[{ required: true, message: '请输入Why2' }]}
          >
            <TextArea rows={2} placeholder="请输入第二个为什么" />
          </Form.Item>

          <Form.Item
            label={template?.whyQuestions[2] || "Why 3: 为什么？"}
            name="why3"
          >
            <TextArea rows={2} placeholder="请输入第三个为什么（可选）" />
          </Form.Item>

          <Form.Item
            label={template?.whyQuestions[3] || "Why 4: 为什么？"}
            name="why4"
          >
            <TextArea rows={2} placeholder="请输入第四个为什么（可选）" />
          </Form.Item>

          <Form.Item
            label={template?.whyQuestions[4] || "Why 5: 为什么？"}
            name="why5"
          >
            <TextArea rows={2} placeholder="请输入第五个为什么（可选）" />
          </Form.Item>
        </Card>

        <Card title="根本原因和预防措施" style={{ marginBottom: 16 }}>
          <Form.Item
            label="根本原因"
            name="rootCause"
            rules={[{ required: true, message: '请输入根本原因' }]}
          >
            <TextArea rows={3} placeholder="基于5Why分析得出的根本原因" />
          </Form.Item>

          <Form.Item
            label="根本原因分类"
            name="rootCauseCategory"
          >
            <Select
              placeholder="选择根本原因分类"
              onChange={handleCategoryChange}
            >
              <Option value="设计">设计</Option>
              <Option value="工艺">工艺</Option>
              <Option value="管理">管理</Option>
              <Option value="其他">其他</Option>
            </Select>
          </Form.Item>

          {suggestions.length > 0 && (
            <Alert
              message="预防措施建议"
              description={
                <List
                  size="small"
                  dataSource={suggestions}
                  renderItem={(item) => (
                    <List.Item>
                      <Space direction="vertical" style={{ width: '100%' }}>
                        <Space>
                          <Tag color={item.priority <= 2 ? 'red' : 'blue'}>
                            优先级 {item.priority}
                          </Tag>
                          <Text strong>{item.measure}</Text>
                        </Space>
                        <Text type="secondary">{item.description}</Text>
                        {item.verificationMethod && (
                          <Text type="secondary" style={{ fontSize: 12 }}>
                            验证方法：{item.verificationMethod}
                          </Text>
                        )}
                      </Space>
                    </List.Item>
                  )}
                />
              }
              type="info"
              style={{ marginBottom: 16 }}
            />
          )}

          <Form.Item
            label="预防措施"
            name="preventiveMeasures"
          >
            <TextArea rows={4} placeholder="基于根本原因制定的预防措施" />
          </Form.Item>

          <Form.Item
            label="验证方法"
            name="verificationMethod"
          >
            <TextArea rows={2} placeholder="如何验证预防措施的有效性" />
          </Form.Item>
        </Card>

        <Form.Item>
          <Space>
            <Button type="primary" htmlType="submit" icon={<SaveOutlined />} loading={loading}>
              保存分析
            </Button>
            <Button icon={<ReloadOutlined />} onClick={loadTemplate}>
              重新加载模板
            </Button>
          </Space>
        </Form.Item>
      </Form>

      {existingAnalysis && (
        <Card title="分析历史" style={{ marginTop: 16 }}>
          <Descriptions column={1} bordered>
            <Descriptions.Item label="分析时间">
              {existingAnalysis.analyzedAt
                ? dayjs(existingAnalysis.analyzedAt).format('YYYY-MM-DD HH:mm:ss')
                : '-'}
            </Descriptions.Item>
            <Descriptions.Item label="分析人">
              {existingAnalysis.analyzedByName || '-'}
            </Descriptions.Item>
            <Descriptions.Item label="最后更新时间">
              {dayjs(existingAnalysis.updatedAt).format('YYYY-MM-DD HH:mm:ss')}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}
    </div>
  );
}

