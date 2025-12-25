"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Button,
  Space,
  message,
  Spin,
  Table,
  Tag,
  Descriptions,
  Modal,
  Input,
  Alert,
  Popconfirm,
} from 'antd';
import {
  ArrowLeftOutlined,
  ReloadOutlined,
  SwapOutlined,
  RollbackOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import {
  knowledgeVersionService,
  KnowledgeVersionDto,
  VersionComparisonDto,
  ExpiredKnowledgeDto,
} from '../../services/knowledgeVersionService';

const { TextArea } = Input;

export default function VersionHistory() {
  const { knowledgeId, knowledgeType } = useParams<{ knowledgeId: string; knowledgeType: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [versions, setVersions] = useState<KnowledgeVersionDto[]>([]);
  const [comparison, setComparison] = useState<VersionComparisonDto | null>(null);
  const [comparisonVisible, setComparisonVisible] = useState(false);
  const [expiredKnowledge, setExpiredKnowledge] = useState<ExpiredKnowledgeDto[]>([]);
  const [version1, setVersion1] = useState<string | null>(null);
  const [version2, setVersion2] = useState<string | null>(null);

  useEffect(() => {
    if (knowledgeId && knowledgeType) {
      loadVersionHistory();
    } else {
      loadExpiredKnowledge();
    }
  }, [knowledgeId, knowledgeType]);

  const loadVersionHistory = async () => {
    if (!knowledgeId || !knowledgeType) return;
    try {
      setLoading(true);
      const vers = await knowledgeVersionService.getVersionHistory(knowledgeId, knowledgeType);
      setVersions(vers);
    } catch (error: any) {
      message.error(error.message || '加载版本历史失败');
    } finally {
      setLoading(false);
    }
  };

  const loadExpiredKnowledge = async () => {
    try {
      setLoading(true);
      const expired = await knowledgeVersionService.checkExpiredKnowledge();
      setExpiredKnowledge(expired);
    } catch (error: any) {
      message.error(error.message || '加载过期知识失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCompare = async () => {
    if (!version1 || !version2) {
      message.warning('请选择两个版本进行对比');
      return;
    }
    try {
      setLoading(true);
      const comp = await knowledgeVersionService.compareVersions(version1, version2);
      setComparison(comp);
      setComparisonVisible(true);
    } catch (error: any) {
      message.error(error.message || '版本对比失败');
    } finally {
      setLoading(false);
    }
  };

  const handleRollback = async (versionId: string) => {
    if (!knowledgeId || !knowledgeType) return;
    try {
      setLoading(true);
      await knowledgeVersionService.rollbackVersion(knowledgeId, knowledgeType, versionId, {
        targetVersionId: versionId,
        changeReason: '版本回滚',
      });
      message.success('版本回滚成功');
      loadVersionHistory();
    } catch (error: any) {
      message.error(error.message || '版本回滚失败');
    } finally {
      setLoading(false);
    }
  };

  const changeTypeMap: Record<string, string> = {
    created: '创建',
    updated: '更新',
    deleted: '删除',
  };

  const columns = [
    {
      title: '版本号',
      dataIndex: 'versionNumber',
      key: 'versionNumber',
      render: (text: string, record: KnowledgeVersionDto) => (
        <Space>
          <span>{text}</span>
          {record.isCurrent && <Tag color="success">当前版本</Tag>}
        </Space>
      ),
    },
    {
      title: '变更类型',
      dataIndex: 'changeType',
      key: 'changeType',
      render: (type?: string) => (type ? <Tag>{changeTypeMap[type] || type}</Tag> : '-'),
    },
    {
      title: '变更原因',
      dataIndex: 'changeReason',
      key: 'changeReason',
      ellipsis: true,
    },
    {
      title: '创建人',
      dataIndex: 'createdByName',
      key: 'createdByName',
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleString(),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: KnowledgeVersionDto) => (
        <Space>
          <Button
            type="link"
            size="small"
            onClick={() => {
              if (version1) {
                setVersion2(record.versionId);
              } else {
                setVersion1(record.versionId);
              }
            }}
          >
            {version1 === record.versionId || version2 === record.versionId ? '取消选择' : '选择对比'}
          </Button>
          {!record.isCurrent && (
            <Popconfirm
              title="确定要回滚到此版本吗？"
              onConfirm={() => handleRollback(record.versionId)}
            >
              <Button type="link" size="small" danger icon={<RollbackOutlined />}>
                回滚
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 头部 */}
        <Card>
          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <Space>
              <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
                返回
              </Button>
              <h2 style={{ margin: 0 }}>
                {knowledgeId ? '知识版本历史' : '过期知识检查'}
              </h2>
            </Space>
            <Space>
              {version1 && version2 && (
                <Button
                  type="primary"
                  icon={<SwapOutlined />}
                  onClick={handleCompare}
                  loading={loading}
                >
                  对比版本
                </Button>
              )}
              <Button icon={<ReloadOutlined />} onClick={knowledgeId ? loadVersionHistory : loadExpiredKnowledge} loading={loading}>
                刷新
              </Button>
            </Space>
          </Space>
        </Card>

        {/* 版本历史 */}
        {knowledgeId && knowledgeType && (
          <Card title="版本列表">
            <Table
              columns={columns}
              dataSource={versions}
              rowKey="versionId"
              loading={loading}
              pagination={false}
            />
          </Card>
        )}

        {/* 过期知识 */}
        {!knowledgeId && (
          <Card title="过期知识">
            {expiredKnowledge.length > 0 ? (
              <Table
                columns={[
                  { title: '知识ID', dataIndex: 'knowledgeId', key: 'knowledgeId' },
                  { title: '知识类型', dataIndex: 'knowledgeType', key: 'knowledgeType' },
                  { title: '知识名称', dataIndex: 'knowledgeName', key: 'knowledgeName' },
                  { title: '当前版本', dataIndex: 'currentVersion', key: 'currentVersion' },
                  {
                    title: '过期日期',
                    dataIndex: 'expiryDate',
                    key: 'expiryDate',
                    render: (text?: string) => (text ? new Date(text).toLocaleDateString() : '-'),
                  },
                  {
                    title: '过期天数',
                    dataIndex: 'daysSinceExpiry',
                    key: 'daysSinceExpiry',
                    render: (days: number) => (
                      <Tag color={days > 30 ? 'error' : days > 7 ? 'warning' : 'default'}>
                        {days} 天
                      </Tag>
                    ),
                  },
                ]}
                dataSource={expiredKnowledge}
                rowKey="knowledgeId"
                loading={loading}
              />
            ) : (
              <Alert message="没有过期知识" type="success" showIcon />
            )}
          </Card>
        )}

        {/* 版本对比对话框 */}
        <Modal
          title="版本对比"
          open={comparisonVisible}
          onCancel={() => setComparisonVisible(false)}
          width={800}
          footer={[
            <Button key="close" onClick={() => setComparisonVisible(false)}>
              关闭
            </Button>,
          ]}
        >
          {comparison && (
            <Space direction="vertical" style={{ width: '100%' }}>
              <Descriptions column={2} size="small">
                <Descriptions.Item label="版本1">{comparison.version1.versionNumber}</Descriptions.Item>
                <Descriptions.Item label="版本2">{comparison.version2.versionNumber}</Descriptions.Item>
              </Descriptions>
              {comparison.differences.length > 0 ? (
                <Table
                  columns={[
                    { title: '字段', dataIndex: 'field', key: 'field' },
                    {
                      title: '变更类型',
                      dataIndex: 'changeType',
                      key: 'changeType',
                      render: (type: string) => {
                        const colorMap: Record<string, string> = {
                          added: 'success',
                          deleted: 'error',
                          modified: 'warning',
                        };
                        const textMap: Record<string, string> = {
                          added: '新增',
                          deleted: '删除',
                          modified: '修改',
                        };
                        return <Tag color={colorMap[type]}>{textMap[type] || type}</Tag>;
                      },
                    },
                    {
                      title: '旧值',
                      dataIndex: 'oldValue',
                      key: 'oldValue',
                      render: (val: any) => (val !== null && val !== undefined ? String(val) : '-'),
                    },
                    {
                      title: '新值',
                      dataIndex: 'newValue',
                      key: 'newValue',
                      render: (val: any) => (val !== null && val !== undefined ? String(val) : '-'),
                    },
                  ]}
                  dataSource={comparison.differences}
                  rowKey="field"
                  pagination={false}
                  size="small"
                />
              ) : (
                <Alert message="两个版本内容相同" type="info" showIcon />
              )}
            </Space>
          )}
        </Modal>
      </Space>
    </div>
  );
}

