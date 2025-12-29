"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Input,
  Space,
  Tag,
  message,
  Spin,
  Typography,
  Descriptions,
  Modal,
  Form,
  Select,
  Timeline,
  Divider,
  Alert,
  Tabs,
  Empty,
} from 'antd';
import {
  SearchOutlined,
  ReloadOutlined,
  HistoryOutlined,
  SwapOutlined,
  PlusOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  MinusCircleOutlined,
} from '@ant-design/icons';
import {
  judgementCardVersionService,
  JudgementCardVersionDto,
  VersionComparisonDto,
  JudgementCardUsageHistoryDto,
  CreateNewVersionRequest,
} from '../../services/judgementCardVersionService';
import { triageService, JudgementCardDto } from '../../services/triageService';

const { Title, Text } = Typography;
const { Search } = Input;
const { TextArea } = Input;
const { Option } = Select;
const { TabPane } = Tabs;

export default function JudgementCardVersion() {
  const [loading, setLoading] = useState(false);
  const [judgementCards, setJudgementCards] = useState<JudgementCardDto[]>([]);
  const [selectedJc, setSelectedJc] = useState<JudgementCardDto | null>(null);
  const [versions, setVersions] = useState<JudgementCardVersionDto[]>([]);
  const [usageHistory, setUsageHistory] = useState<JudgementCardUsageHistoryDto[]>([]);
  const [comparison, setComparison] = useState<VersionComparisonDto | null>(null);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [versionModalVisible, setVersionModalVisible] = useState(false);
  const [compareModalVisible, setCompareModalVisible] = useState(false);
  const [historyModalVisible, setHistoryModalVisible] = useState(false);
  const [form] = Form.useForm();
  const [compareForm] = Form.useForm();

  useEffect(() => {
    loadJudgementCards();
  }, []);

  const loadJudgementCards = async () => {
    try {
      setLoading(true);
      const cards = await triageService.getJudgementCards({
        status: 'Active',
      });
      setJudgementCards(cards);
    } catch (error: any) {
      message.error(error.message || '加载判断卡列表失败');
    } finally {
      setLoading(false);
    }
  };

  const loadVersions = async (jcCode: string) => {
    try {
      setLoading(true);
      const versionList = await judgementCardVersionService.getVersions(jcCode);
      setVersions(versionList);
    } catch (error: any) {
      message.error(`加载版本列表失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const loadUsageHistory = async (jcCode: string, version?: number) => {
    try {
      setLoading(true);
      const history = await judgementCardVersionService.getUsageHistory(jcCode, version);
      setUsageHistory(history);
    } catch (error: any) {
      message.error(`加载使用历史失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleViewVersions = async (record: JudgementCardDto) => {
    setSelectedJc(record);
    setVersionModalVisible(true);
    await loadVersions(record.jcCode);
  };

  const handleCompareVersions = async (jcCode: string, values: any) => {
    try {
      setLoading(true);
      const result = await judgementCardVersionService.compareVersions(
        jcCode,
        values.version1,
        values.version2
      );
      setComparison(result);
    } catch (error: any) {
      message.error(`版本对比失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleViewHistory = async (record: JudgementCardDto) => {
    setSelectedJc(record);
    setHistoryModalVisible(true);
    await loadUsageHistory(record.jcCode);
  };

  const handleCreateVersion = async (values: any) => {
    if (!selectedJc) return;

    try {
      setLoading(true);
      const request: CreateNewVersionRequest = {
        updateRequest: {
          title: values.title,
          description: values.description,
          domain: values.domain,
          hypothesisTemplate: values.hypothesisTemplate,
          nextActionTemplate: values.nextActionTemplate,
        },
        changeReason: values.changeReason,
      };

      await judgementCardVersionService.createNewVersion(selectedJc.jcCode, request);
      message.success('新版本创建成功');
      setVersionModalVisible(false);
      form.resetFields();
      await loadVersions(selectedJc.jcCode);
    } catch (error: any) {
      message.error(`创建新版本失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const getChangeTypeIcon = (changeType: string) => {
    switch (changeType) {
      case 'added':
        return <PlusOutlined style={{ color: '#52c41a' }} />;
      case 'removed':
        return <MinusCircleOutlined style={{ color: '#ff4d4f' }} />;
      case 'modified':
        return <SwapOutlined style={{ color: '#1890ff' }} />;
      default:
        return null;
    }
  };

  const getChangeTypeColor = (changeType: string) => {
    switch (changeType) {
      case 'added':
        return 'success';
      case 'removed':
        return 'error';
      case 'modified':
        return 'processing';
      default:
        return 'default';
    }
  };

  const getResultColor = (result?: string) => {
    switch (result) {
      case 'correct':
        return 'success';
      case 'incorrect':
        return 'error';
      case 'partial':
        return 'warning';
      default:
        return 'default';
    }
  };

  const getResultText = (result?: string) => {
    switch (result) {
      case 'correct':
        return '正确';
      case 'incorrect':
        return '错误';
      case 'partial':
        return '部分正确';
      default:
        return '未评价';
    }
  };

  const filteredCards = judgementCards.filter(card => {
    if (!searchKeyword) return true;
    const keyword = searchKeyword.toLowerCase();
    return (
      card.jcCode.toLowerCase().includes(keyword) ||
      card.title.toLowerCase().includes(keyword)
    );
  });

  const versionColumns = [
    {
      title: '版本号',
      dataIndex: 'version',
      key: 'version',
      width: 100,
      render: (version: number) => <Tag color="blue">v{version}</Tag>,
    },
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
    },
    {
      title: '问题域',
      dataIndex: 'domain',
      key: 'domain',
      width: 100,
    },
    {
      title: '状态',
      key: 'isCurrent',
      width: 100,
      render: (_: any, record: JudgementCardVersionDto) => (
        record.isCurrent ? (
          <Tag color="success">当前版本</Tag>
        ) : (
          <Tag>历史版本</Tag>
        )
      ),
    },
    {
      title: '变更原因',
      dataIndex: 'changeReason',
      key: 'changeReason',
      ellipsis: true,
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      render: (_: any, record: JudgementCardVersionDto) => (
        <Space>
          <Button
            size="small"
            onClick={() => {
              compareForm.setFieldsValue({
                version1: record.version,
                version2: versions.find(v => v.isCurrent)?.version || record.version,
              });
              setCompareModalVisible(true);
            }}
          >
            对比
          </Button>
          <Button
            size="small"
            onClick={() => loadUsageHistory(selectedJc?.jcCode || '', record.version)}
          >
            使用历史
          </Button>
        </Space>
      ),
    },
  ];

  const columns = [
    {
      title: '判断卡编号',
      dataIndex: 'jcCode',
      key: 'jcCode',
      width: 120,
    },
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
    },
    {
      title: '问题域',
      dataIndex: 'domain',
      key: 'domain',
      width: 100,
    },
    {
      title: '当前版本',
      dataIndex: 'version',
      key: 'version',
      width: 100,
      render: (version: number) => <Tag color="blue">v{version}</Tag>,
    },
    {
      title: '使用次数',
      dataIndex: 'usageCount',
      key: 'usageCount',
      width: 100,
    },
    {
      title: '操作',
      key: 'action',
      width: 250,
      render: (_: any, record: JudgementCardDto) => (
        <Space>
          <Button
            size="small"
            icon={<HistoryOutlined />}
            onClick={() => handleViewVersions(record)}
          >
            版本历史
          </Button>
          <Button
            size="small"
            icon={<SwapOutlined />}
            onClick={() => {
              setSelectedJc(record);
              compareForm.resetFields();
              setCompareModalVisible(true);
            }}
          >
            版本对比
          </Button>
          <Button
            size="small"
            onClick={() => handleViewHistory(record)}
          >
            使用历史
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Title level={2}>判断卡版本管理</Title>
            <Space>
              <Search
                placeholder="搜索判断卡编号或标题"
                allowClear
                style={{ width: 300 }}
                onSearch={setSearchKeyword}
                onChange={(e) => setSearchKeyword(e.target.value)}
              />
              <Button
                icon={<ReloadOutlined />}
                onClick={loadJudgementCards}
              >
                刷新
              </Button>
            </Space>
          </div>

          <Alert
            message="版本管理说明"
            description="判断卡支持多版本管理，每次修改必须填写变更原因。可以查看版本历史、对比不同版本、查看使用历史。"
            type="info"
            showIcon
            style={{ marginBottom: '16px' }}
          />

          <Table
            columns={columns}
            dataSource={filteredCards}
            rowKey="jcCode"
            loading={loading}
            pagination={{
              pageSize: 20,
              showSizeChanger: true,
              showTotal: (total) => `共 ${total} 张判断卡`,
            }}
          />
        </Space>
      </Card>

      {/* 版本历史弹窗 */}
      <Modal
        title={`版本历史 - ${selectedJc?.jcCode}`}
        open={versionModalVisible}
        onCancel={() => {
          setVersionModalVisible(false);
          setVersions([]);
        }}
        footer={null}
        width={1000}
      >
        <Tabs defaultActiveKey="versions">
          <TabPane tab="版本列表" key="versions">
            <Space direction="vertical" style={{ width: '100%' }} size="middle">
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => {
                  form.resetFields();
                  form.setFieldsValue({
                    title: selectedJc?.title,
                    description: selectedJc?.description,
                    domain: selectedJc?.domain,
                  });
                }}
              >
                创建新版本
              </Button>
              <Table
                columns={versionColumns}
                dataSource={versions}
                rowKey="version"
                pagination={false}
                size="small"
              />
            </Space>
          </TabPane>
        </Tabs>
      </Modal>

      {/* 创建新版本弹窗 */}
      <Modal
        title={`创建新版本 - ${selectedJc?.jcCode}`}
        open={form.isFieldsTouched()}
        onCancel={() => form.resetFields()}
        onOk={() => form.submit()}
        confirmLoading={loading}
        width={600}
      >
        <Alert
          message="重要提示"
          description="创建新版本必须填写变更原因（推翻原因），这是硬规则要求。"
          type="warning"
          showIcon
          style={{ marginBottom: '16px' }}
        />
        <Form
          form={form}
          layout="vertical"
          onFinish={handleCreateVersion}
        >
          <Form.Item
            name="title"
            label="标题"
          >
            <Input />
          </Form.Item>
          <Form.Item
            name="description"
            label="描述"
          >
            <TextArea rows={3} />
          </Form.Item>
          <Form.Item
            name="domain"
            label="问题域"
          >
            <Select>
              <Option value="A">A - 机械</Option>
              <Option value="B">B - 电气</Option>
              <Option value="C">C - 软件</Option>
              <Option value="D">D - 工艺</Option>
              <Option value="E">E - 其他</Option>
            </Select>
          </Form.Item>
          <Form.Item
            name="hypothesisTemplate"
            label="假设模板"
          >
            <TextArea rows={3} />
          </Form.Item>
          <Form.Item
            name="nextActionTemplate"
            label="下一步动作模板"
          >
            <TextArea rows={3} />
          </Form.Item>
          <Form.Item
            name="changeReason"
            label="变更原因（推翻原因）"
            rules={[{ required: true, message: '必须填写变更原因' }]}
          >
            <TextArea rows={4} placeholder="请详细说明为什么要创建新版本，推翻了上一版本的哪些内容" />
          </Form.Item>
        </Form>
      </Modal>

      {/* 版本对比弹窗 */}
      <Modal
        title={`版本对比 - ${selectedJc?.jcCode}`}
        open={compareModalVisible}
        onCancel={() => {
          setCompareModalVisible(false);
          setComparison(null);
          compareForm.resetFields();
        }}
        footer={null}
        width={900}
      >
        <Form
          form={compareForm}
          layout="inline"
          onFinish={(values) => handleCompareVersions(selectedJc?.jcCode || '', values)}
          style={{ marginBottom: '16px' }}
        >
          <Form.Item
            name="version1"
            label="版本1"
            rules={[{ required: true }]}
          >
            <Select style={{ width: 100 }}>
              {versions.map(v => (
                <Option key={v.version} value={v.version}>v{v.version}</Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item
            name="version2"
            label="版本2"
            rules={[{ required: true }]}
          >
            <Select style={{ width: 100 }}>
              {versions.map(v => (
                <Option key={v.version} value={v.version}>v{v.version}</Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading}>
              对比
            </Button>
          </Form.Item>
        </Form>

        {comparison && (
          <div>
            <Descriptions title="对比结果" bordered column={1}>
              <Descriptions.Item label="判断卡编号">
                {comparison.jcCode}
              </Descriptions.Item>
              <Descriptions.Item label="对比版本">
                v{comparison.version1} vs v{comparison.version2}
              </Descriptions.Item>
            </Descriptions>

            <Divider>变更列表</Divider>

            {comparison.changes.length > 0 ? (
              <Timeline>
                {comparison.changes.map((change, index) => (
                  <Timeline.Item
                    key={index}
                    dot={getChangeTypeIcon(change.changeType)}
                    color={
                      change.changeType === 'added' ? 'green' :
                      change.changeType === 'removed' ? 'red' : 'blue'
                    }
                  >
                    <Space direction="vertical" size="small">
                      <div>
                        <Tag color={getChangeTypeColor(change.changeType)}>
                          {change.changeType === 'added' ? '新增' :
                           change.changeType === 'removed' ? '删除' : '修改'}
                        </Tag>
                        <Text strong>{change.field}</Text>
                      </div>
                      {change.oldValue && (
                        <Text type="secondary" delete>
                          旧值: {change.oldValue}
                        </Text>
                      )}
                      {change.newValue && (
                        <Text>
                          新值: {change.newValue}
                        </Text>
                      )}
                    </Space>
                  </Timeline.Item>
                ))}
              </Timeline>
            ) : (
              <Empty description="两个版本无差异" />
            )}
          </div>
        )}
      </Modal>

      {/* 使用历史弹窗 */}
      <Modal
        title={`使用历史 - ${selectedJc?.jcCode}`}
        open={historyModalVisible}
        onCancel={() => {
          setHistoryModalVisible(false);
          setUsageHistory([]);
        }}
        footer={null}
        width={800}
      >
        {usageHistory.length > 0 ? (
          <Table
            columns={[
              {
                title: '版本',
                dataIndex: 'jcVersion',
                key: 'jcVersion',
                width: 100,
                render: (version: number) => <Tag color="blue">v{version}</Tag>,
              },
              {
                title: '工单ID',
                dataIndex: 'ticketId',
                key: 'ticketId',
                ellipsis: true,
              },
              {
                title: '使用时间',
                dataIndex: 'usedAt',
                key: 'usedAt',
                width: 180,
                render: (date: string) => new Date(date).toLocaleString('zh-CN'),
              },
              {
                title: '使用结果',
                dataIndex: 'result',
                key: 'result',
                width: 120,
                render: (result?: string) => (
                  <Tag color={getResultColor(result)}>
                    {getResultText(result)}
                  </Tag>
                ),
              },
              {
                title: '反馈',
                dataIndex: 'feedback',
                key: 'feedback',
                ellipsis: true,
              },
            ]}
            dataSource={usageHistory}
            rowKey="id"
            pagination={{
              pageSize: 10,
            }}
            size="small"
          />
        ) : (
          <Empty description="暂无使用历史" />
        )}
      </Modal>
    </div>
  );
}



















