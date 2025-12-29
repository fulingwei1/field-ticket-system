"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Input,
  Space,
  Tag,
  Progress,
  Alert,
  message,
  Spin,
  Typography,
  Descriptions,
  Modal,
  List,
  Divider,
} from 'antd';
import {
  SearchOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  WarningOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import { judgementCardQualityService, JudgementCardQualityScoreDto, QualityIssueDto } from '../../services/judgementCardQualityService';
import { triageService, JudgementCardDto } from '../../services/triageService';

const { Title, Text } = Typography;
const { Search } = Input;

export default function JudgementCardQuality() {
  const [loading, setLoading] = useState(false);
  const [judgementCards, setJudgementCards] = useState<JudgementCardDto[]>([]);
  const [qualityScores, setQualityScores] = useState<Map<string, JudgementCardQualityScoreDto>>(new Map());
  const [selectedJc, setSelectedJc] = useState<JudgementCardDto | null>(null);
  const [scoreDetail, setScoreDetail] = useState<JudgementCardQualityScoreDto | null>(null);
  const [issues, setIssues] = useState<QualityIssueDto[]>([]);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [detailModalVisible, setDetailModalVisible] = useState(false);

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

  const loadQualityScore = async (jcCode: string) => {
    try {
      const score = await judgementCardQualityService.getQualityScore(jcCode);
      setQualityScores(prev => new Map(prev).set(jcCode, score));
      return score;
    } catch (error: any) {
      message.error(`获取 ${jcCode} 质量评分失败: ${error.message}`);
      return null;
    }
  };

  const loadQualityIssues = async (jcCode: string) => {
    try {
      const issuesList = await judgementCardQualityService.getQualityIssues(jcCode);
      setIssues(issuesList);
    } catch (error: any) {
      message.error(`获取质量问题列表失败: ${error.message}`);
    }
  };

  const handleViewDetail = async (record: JudgementCardDto) => {
    setSelectedJc(record);
    setDetailModalVisible(true);
    
    let score = qualityScores.get(record.jcCode);
    if (!score) {
      score = await loadQualityScore(record.jcCode);
    }
    
    if (score) {
      setScoreDetail(score);
      await loadQualityIssues(record.jcCode);
    }
  };

  const handleBatchScore = async () => {
    try {
      setLoading(true);
      const jcCodes = judgementCards.map(card => card.jcCode);
      const scores = await judgementCardQualityService.batchScore(jcCodes);
      
      const scoreMap = new Map<string, JudgementCardQualityScoreDto>();
      scores.forEach(score => {
        scoreMap.set(score.jcCode, score);
      });
      setQualityScores(scoreMap);
      
      message.success(`成功评分 ${scores.length} 张判断卡`);
    } catch (error: any) {
      message.error(`批量评分失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const getLevelColor = (level: string) => {
    switch (level) {
      case 'Excellent':
        return 'success';
      case 'Good':
        return 'processing';
      case 'Fair':
        return 'warning';
      case 'Poor':
        return 'error';
      default:
        return 'default';
    }
  };

  const getLevelText = (level: string) => {
    switch (level) {
      case 'Excellent':
        return '优秀';
      case 'Good':
        return '良好';
      case 'Fair':
        return '一般';
      case 'Poor':
        return '差';
      default:
        return level;
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'Critical':
        return 'red';
      case 'High':
        return 'orange';
      case 'Medium':
        return 'gold';
      case 'Low':
        return 'blue';
      default:
        return 'default';
    }
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'Critical':
        return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
      case 'High':
        return <ExclamationCircleOutlined style={{ color: '#ff9800' }} />;
      case 'Medium':
        return <WarningOutlined style={{ color: '#ffc107' }} />;
      case 'Low':
        return <CheckCircleOutlined style={{ color: '#1890ff' }} />;
      default:
        return null;
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
      title: '质量评分',
      key: 'qualityScore',
      width: 150,
      render: (_: any, record: JudgementCardDto) => {
        const score = qualityScores.get(record.jcCode);
        if (!score) {
          return (
            <Button
              size="small"
              onClick={() => loadQualityScore(record.jcCode)}
            >
              评分
            </Button>
          );
        }
        return (
          <Space>
            <Progress
              type="circle"
              size={50}
              percent={score.totalScore}
              format={(percent) => `${percent}`}
              strokeColor={
                score.level === 'Excellent' ? '#52c41a' :
                score.level === 'Good' ? '#1890ff' :
                score.level === 'Fair' ? '#faad14' : '#ff4d4f'
              }
            />
            <Tag color={getLevelColor(score.level)}>
              {getLevelText(score.level)}
            </Tag>
          </Space>
        );
      },
    },
    {
      title: '质量问题数',
      key: 'issueCount',
      width: 120,
      render: (_: any, record: JudgementCardDto) => {
        const score = qualityScores.get(record.jcCode);
        if (!score) return '-';
        const criticalCount = score.issues.filter(i => i.severity === 'Critical').length;
        const highCount = score.issues.filter(i => i.severity === 'High').length;
        return (
          <Space>
            {criticalCount > 0 && (
              <Tag color="red">严重: {criticalCount}</Tag>
            )}
            {highCount > 0 && (
              <Tag color="orange">高: {highCount}</Tag>
            )}
            <Text>{score.issues.length}</Text>
          </Space>
        );
      },
    },
    {
      title: '操作',
      key: 'action',
      width: 120,
      render: (_: any, record: JudgementCardDto) => (
        <Button
          type="link"
          onClick={() => handleViewDetail(record)}
        >
          查看详情
        </Button>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Title level={2}>判断卡质量评分</Title>
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
              <Button
                type="primary"
                onClick={handleBatchScore}
                loading={loading}
              >
                批量评分
              </Button>
            </Space>
          </div>

          <Alert
            message="质量评分说明"
            description="系统会自动评估判断卡的质量，包括完整性、逻辑一致性、可验证性和证据支撑四个维度。评分不参与KPI，仅用于质量保障。"
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

      <Modal
        title={`判断卡质量详情 - ${selectedJc?.jcCode}`}
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={null}
        width={800}
      >
        {scoreDetail && (
          <Space direction="vertical" style={{ width: '100%' }} size="large">
            <Descriptions title={selectedJc?.title} bordered column={2}>
              <Descriptions.Item label="判断卡编号">
                {scoreDetail.jcCode}
              </Descriptions.Item>
              <Descriptions.Item label="质量等级">
                <Tag color={getLevelColor(scoreDetail.level)}>
                  {getLevelText(scoreDetail.level)}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="总分">
                <Text strong style={{ fontSize: '18px' }}>
                  {scoreDetail.totalScore} / 100
                </Text>
              </Descriptions.Item>
              <Descriptions.Item label="评分时间">
                {new Date(scoreDetail.scoredAt).toLocaleString('zh-CN')}
              </Descriptions.Item>
            </Descriptions>

            <Divider>评分详情</Divider>

            <Space direction="vertical" style={{ width: '100%' }} size="middle">
              <Card size="small">
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text>完整性检查</Text>
                    <Text strong>{scoreDetail.completenessScore} / 30</Text>
                  </div>
                  <Progress
                    percent={(scoreDetail.completenessScore / 30) * 100}
                    strokeColor="#1890ff"
                  />
                </Space>
              </Card>

              <Card size="small">
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text>逻辑一致性</Text>
                    <Text strong>{scoreDetail.logicConsistencyScore} / 30</Text>
                  </div>
                  <Progress
                    percent={(scoreDetail.logicConsistencyScore / 30) * 100}
                    strokeColor="#52c41a"
                  />
                </Space>
              </Card>

              <Card size="small">
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text>可验证性</Text>
                    <Text strong>{scoreDetail.verifiabilityScore} / 20</Text>
                  </div>
                  <Progress
                    percent={(scoreDetail.verifiabilityScore / 20) * 100}
                    strokeColor="#faad14"
                  />
                </Space>
              </Card>

              <Card size="small">
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text>证据支撑</Text>
                    <Text strong>{scoreDetail.evidenceScore} / 20</Text>
                  </div>
                  <Progress
                    percent={(scoreDetail.evidenceScore / 20) * 100}
                    strokeColor="#722ed1"
                  />
                </Space>
              </Card>
            </Space>

            {issues.length > 0 && (
              <>
                <Divider>质量问题 ({issues.length})</Divider>
                <List
                  dataSource={issues}
                  renderItem={(issue) => (
                    <List.Item>
                      <List.Item.Meta
                        avatar={getSeverityIcon(issue.severity)}
                        title={
                          <Space>
                            <Tag color={getSeverityColor(issue.severity)}>
                              {issue.severity}
                            </Tag>
                            <Text strong>{issue.type}</Text>
                          </Space>
                        }
                        description={
                          <Space direction="vertical" size="small">
                            <Text>{issue.description}</Text>
                            {issue.suggestion && (
                              <Text type="secondary">
                                建议: {issue.suggestion}
                              </Text>
                            )}
                          </Space>
                        }
                      />
                    </List.Item>
                  )}
                />
              </>
            )}

            {issues.length === 0 && (
              <Alert
                message="未发现质量问题"
                description="该判断卡质量良好，未发现需要改进的问题。"
                type="success"
                showIcon
              />
            )}
          </Space>
        )}
      </Modal>
    </div>
  );
}





















