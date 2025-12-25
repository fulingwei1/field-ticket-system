"use client";

import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Space,
  message,
  Spin,
  Tabs,
  Table,
  Empty,
  Alert,
  Divider,
} from 'antd';
import {
  ArrowLeftOutlined,
  EditOutlined,
  CheckCircleOutlined,
  SolutionOutlined,
  FileTextOutlined,
  QuestionCircleOutlined,
  ClockCircleOutlined,
  LinkOutlined,
  MessageOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons';
import { ticketService, TicketDto } from '../../services/ticketService';
import { solutionService, SolutionDto } from '../../services/solutionService';
import { verificationService, VerificationDto } from '../../services/verificationService';
import { attachmentService } from '../../services/attachmentService';
import { MissingInfoQuestionnaire } from '../../components/tickets/MissingInfoQuestionnaire';
import { AIEnhancedMissingInfoQuestionnaire } from '../../components/tickets/AIEnhancedMissingInfoQuestionnaire';
import TicketStatusFlow from '../../components/tickets/TicketStatusFlow';
import TicketAssociations from '../../components/tickets/TicketAssociations';
import CustomerCommunication from '../../components/tickets/CustomerCommunication';
import ResponsibilityAttribution from '../../components/tickets/ResponsibilityAttribution';
import RecentChanges from '../../components/tickets/RecentChanges';

const { TabPane } = Tabs;

export default function TicketDetail() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [ticket, setTicket] = useState<TicketDto | null>(null);
  const [solutions, setSolutions] = useState<SolutionDto[]>([]);
  const [verifications, setVerifications] = useState<VerificationDto[]>([]);
  const [attachments, setAttachments] = useState<any[]>([]);
  const [activeTab, setActiveTab] = useState('info');

  useEffect(() => {
    if (ticketId) {
      loadTicketData();
    }
  }, [ticketId]);

  const loadTicketData = async () => {
    if (!ticketId) return;

    try {
      setLoading(true);
      const [ticketData, solutionsData, verificationsData, attachmentsData] = await Promise.all([
        ticketService.getTicket(ticketId),
        solutionService.getTicketSolutions(ticketId).catch(() => []),
        verificationService.getVerificationHistory(ticketId).catch(() => []),
        attachmentService.getTicketAttachments(ticketId).catch(() => []),
      ]);

      setTicket(ticketData);
      setSolutions(solutionsData);
      setVerifications(verificationsData);
      setAttachments(attachmentsData);
    } catch (error) {
      console.error('Failed to load ticket data:', error);
      message.error('加载工单详情失败');
    } finally {
      setLoading(false);
    }
  };

  const domainMap: Record<string, { label: string; color: string }> = {
    A: { label: '机械/动作', color: 'blue' },
    B: { label: '电气/IO', color: 'green' },
    C: { label: 'PLC/程序', color: 'orange' },
    D: { label: '测试/判定', color: 'purple' },
    E: { label: '系统/环境', color: 'red' },
  };

  const statusMap: Record<string, { label: string; color: string }> = {
    Draft: { label: '草稿', color: 'default' },
    Submitted: { label: '已提交', color: 'processing' },
    Triage: { label: '分诊中', color: 'warning' },
    SolutionIssued: { label: '方案已发布', color: 'success' },
    Verifying: { label: '验证中', color: 'processing' },
    Closed: { label: '已关闭', color: 'default' },
    Reopened: { label: '已重开', color: 'warning' },
  };

  const priorityMap: Record<string, { label: string; color: string }> = {
    Low: { label: '低', color: 'default' },
    Medium: { label: '中', color: 'warning' },
    High: { label: '高', color: 'error' },
    Urgent: { label: '紧急', color: 'red' },
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!ticket) {
    return (
      <Card>
        <Empty description="工单不存在" />
      </Card>
    );
  }

  const domainInfo = domainMap[ticket.domain] || { label: ticket.domain, color: 'default' };
  const statusInfo = statusMap[ticket.status] || { label: ticket.status, color: 'default' };
  const priorityInfo = priorityMap[ticket.priority] || { label: ticket.priority, color: 'default' };

  return (
    <div>
      {/* 头部操作栏 */}
      <Card style={{ marginBottom: 16 }}>
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/tickets')}>
            返回列表
          </Button>
          <Divider type="vertical" />
          {ticket.status === 'Draft' && (
            <Button
              type="primary"
              icon={<EditOutlined />}
              onClick={() => navigate(`/tickets/${ticketId}/edit`)}
            >
              编辑
            </Button>
          )}
          {ticket.status === 'Submitted' && (
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              onClick={() => navigate(`/tickets/${ticketId}/triage`)}
            >
              分诊
            </Button>
          )}
          {ticket.status === 'Triage' && (
            <Button
              type="primary"
              icon={<SolutionOutlined />}
              onClick={() => navigate(`/solutions/tickets/${ticketId}`)}
            >
              创建解决方案
            </Button>
          )}
          {ticket.status === 'SolutionIssued' && (
            <Button
              type="primary"
              icon={<CheckCircleOutlined />}
              onClick={() => navigate(`/tickets/${ticketId}/verification`)}
            >
              执行验证
            </Button>
          )}
        </Space>
      </Card>

      {/* 工单基本信息 */}
      <Card title="工单信息" style={{ marginBottom: 16 }}>
        <Descriptions column={2} bordered>
          <Descriptions.Item label="工单编号">
            {ticket.ticketNo || '草稿'}
          </Descriptions.Item>
          <Descriptions.Item label="状态">
            <Tag color={statusInfo.color}>{statusInfo.label}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="紧急度">
            <Tag color={priorityInfo.color}>{priorityInfo.label}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="问题域">
            <Tag color={domainInfo.color}>{domainInfo.label}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="步骤代码">{ticket.stepCode}</Descriptions.Item>
          <Descriptions.Item label="步骤名称">{ticket.stepName || '-'}</Descriptions.Item>
          <Descriptions.Item label="问题标题" span={2}>
            {ticket.symptomTitle}
          </Descriptions.Item>
          {ticket.symptomDetail && (
            <Descriptions.Item label="问题详情" span={2}>
              {ticket.symptomDetail}
            </Descriptions.Item>
          )}
          <Descriptions.Item label="软件版本">{ticket.swVersion}</Descriptions.Item>
          <Descriptions.Item label="PLC版本">{ticket.plcVersion}</Descriptions.Item>
          <Descriptions.Item label="参数版本">{ticket.paramVersion}</Descriptions.Item>
          <Descriptions.Item label="复现率">
            {ticket.reproRate !== null && ticket.reproRate !== undefined
              ? `${ticket.reproRate}%`
              : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="重启恢复">
            {ticket.rebootRecovers !== null && ticket.rebootRecovers !== undefined
              ? ticket.rebootRecovers
                ? '是'
                : '否'
              : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="环境相关">
            {ticket.envRelated !== null && ticket.envRelated !== undefined
              ? ticket.envRelated
                ? '是'
                : '否'
              : '-'}
          </Descriptions.Item>
          {ticket.alarmCode && (
            <Descriptions.Item label="报警代码">{ticket.alarmCode}</Descriptions.Item>
          )}
          {ticket.currentJcCode && (
            <Descriptions.Item label="判断卡代码">{ticket.currentJcCode}</Descriptions.Item>
          )}
          <Descriptions.Item label="已确认事实">
            {ticket.confirmedAsFact ? '是' : '否'}
          </Descriptions.Item>
          {ticket.confirmedAt && (
            <Descriptions.Item label="确认时间">
              {new Date(ticket.confirmedAt).toLocaleString()}
            </Descriptions.Item>
          )}
          <Descriptions.Item label="创建时间">
            {new Date(ticket.createdAt).toLocaleString()}
          </Descriptions.Item>
          <Descriptions.Item label="更新时间">
            {new Date(ticket.updatedAt).toLocaleString()}
          </Descriptions.Item>
          {ticket.submittedAt && (
            <Descriptions.Item label="提交时间">
              {new Date(ticket.submittedAt).toLocaleString()}
            </Descriptions.Item>
          )}
          {ticket.closedAt && (
            <Descriptions.Item label="关闭时间">
              {new Date(ticket.closedAt).toLocaleString()}
            </Descriptions.Item>
          )}
        </Descriptions>
      </Card>

      {/* 标签页内容 */}
      <Card>
        <Tabs activeKey={activeTab} onChange={setActiveTab}>
          {/* 事实表 */}
          <TabPane tab={<span><FileTextOutlined /> 事实表</span>} key="facts">
            <Card>
              <pre style={{ background: '#f5f5f5', padding: 16, borderRadius: 4 }}>
                {JSON.stringify(ticket.factsJson, null, 2)}
              </pre>
            </Card>
          </TabPane>

          {/* 已采取行动 */}
          <TabPane tab={<span><CheckCircleOutlined /> 已采取行动</span>} key="actions">
            <Card>
              {ticket.actionsTaken && ticket.actionsTaken.length > 0 ? (
                <ul>
                  {ticket.actionsTaken.map((action, index) => (
                    <li key={index}>{action}</li>
                  ))}
                </ul>
              ) : (
                <Empty description="暂无已采取的行动" />
              )}
              {ticket.actionsTakenNote && (
                <div style={{ marginTop: 16 }}>
                  <strong>备注：</strong>
                  <p>{ticket.actionsTakenNote}</p>
                </div>
              )}
            </Card>
          </TabPane>

          {/* 状态流转 */}
          <TabPane tab={<span><ClockCircleOutlined /> 状态流转</span>} key="status-flow">
            {ticketId && <TicketStatusFlow ticketId={ticketId} />}
          </TabPane>

          {/* 关联工单 */}
          <TabPane tab={<span><LinkOutlined /> 关联工单</span>} key="associations">
            {ticketId && <TicketAssociations ticketId={ticketId} />}
          </TabPane>

          {/* 客户沟通 */}
          <TabPane tab={<span><MessageOutlined /> 客户沟通</span>} key="communication">
            {ticketId && (
              <CustomerCommunication
                ticketId={ticketId}
                ticketData={{
                  customerName: ticket?.customerName,
                  deviceSn: ticket?.deviceSn,
                  symptomTitle: ticket?.symptomTitle,
                  stepName: ticket?.stepName,
                  solutionCode: solutions[0]?.solutionCode,
                  releaseVersion: solutions[0]?.releaseVersion,
                }}
              />
            )}
          </TabPane>

          {/* 责任归因 */}
          <TabPane tab={<span><InfoCircleOutlined /> 责任归因</span>} key="attribution">
            {ticketId && <ResponsibilityAttribution ticketId={ticketId} />}
          </TabPane>

          {/* 最近变更 */}
          {ticket && ticket.status !== 'Draft' && (
            <TabPane tab={<span><ClockCircleOutlined /> 最近变更</span>} key="recent-changes">
              {ticketId && <RecentChanges ticketId={ticketId} />}
            </TabPane>
          )}

          {/* 附件 */}
          <TabPane tab={<span><FileTextOutlined /> 附件 ({attachments.length})</span>} key="attachments">
            <Card>
              {attachments.length > 0 ? (
                <Table
                  dataSource={attachments}
                  columns={[
                    {
                      title: '文件名',
                      dataIndex: 'fileName',
                      key: 'fileName',
                    },
                    {
                      title: '大小',
                      dataIndex: 'fileSize',
                      key: 'fileSize',
                      render: (size: number) => {
                        if (size < 1024) return `${size} B`;
                        if (size < 1024 * 1024) return `${(size / 1024).toFixed(2)} KB`;
                        return `${(size / (1024 * 1024)).toFixed(2)} MB`;
                      },
                    },
                    {
                      title: '上传时间',
                      dataIndex: 'uploadedAt',
                      key: 'uploadedAt',
                      render: (time: string) => new Date(time).toLocaleString(),
                    },
                    {
                      title: '操作',
                      key: 'action',
                      render: (_: any, record: any) => (
                        <Button
                          type="link"
                          onClick={async () => {
                            try {
                              const url = await attachmentService.getDownloadUrl(record.attachmentId);
                              window.open(url, '_blank');
                            } catch (error) {
                              message.error('获取下载链接失败');
                            }
                          }}
                        >
                          下载
                        </Button>
                      ),
                    },
                  ]}
                  rowKey="attachmentId"
                  pagination={false}
                />
              ) : (
                <Empty description="暂无附件" />
              )}
            </Card>
          </TabPane>

          {/* 解决方案 */}
          <TabPane tab={<span><SolutionOutlined /> 解决方案 ({solutions.length})</span>} key="solutions">
            <Card>
              {solutions.length > 0 ? (
                <Table
                  dataSource={solutions}
                  columns={[
                    {
                      title: '方案编号',
                      dataIndex: 'solutionCode',
                      key: 'solutionCode',
                    },
                    {
                      title: '标题',
                      dataIndex: 'title',
                      key: 'title',
                    },
                    {
                      title: '类型',
                      dataIndex: 'solutionType',
                      key: 'solutionType',
                    },
                    {
                      title: '状态',
                      dataIndex: 'status',
                      key: 'status',
                      render: (status: string) => {
                        const statusMap: Record<string, { label: string; color: string }> = {
                          Draft: { label: '草稿', color: 'default' },
                          Published: { label: '已发布', color: 'success' },
                        };
                        const info = statusMap[status] || { label: status, color: 'default' };
                        return <Tag color={info.color}>{info.label}</Tag>;
                      },
                    },
                    {
                      title: '发布时间',
                      dataIndex: 'publishedAt',
                      key: 'publishedAt',
                      render: (time: string | null) =>
                        time ? new Date(time).toLocaleString() : '-',
                    },
                    {
                      title: '操作',
                      key: 'action',
                      render: (_: any, record: SolutionDto) => (
                        <Button
                          type="link"
                          onClick={() =>
                            navigate(`/tickets/${ticketId}/solutions/${record.solutionId}`)
                          }
                        >
                          查看
                        </Button>
                      ),
                    },
                  ]}
                  rowKey="solutionId"
                  pagination={false}
                />
              ) : (
                <Empty description="暂无解决方案" />
              )}
              {ticket.status === 'Triage' && (
                <div style={{ marginTop: 16 }}>
                  <Button
                    type="primary"
                    icon={<SolutionOutlined />}
                    onClick={() => navigate(`/tickets/${ticketId}/solutions/new`)}
                  >
                    创建解决方案
                  </Button>
                </div>
              )}
            </Card>
          </TabPane>

          {/* 验证历史 */}
          <TabPane tab={<span><CheckCircleOutlined /> 验证历史 ({verifications.length})</span>} key="verifications">
            <Card>
              {verifications.length > 0 ? (
                <Table
                  dataSource={verifications}
                  columns={[
                    {
                      title: '验证时间',
                      dataIndex: 'verifiedAt',
                      key: 'verifiedAt',
                      render: (time: string) => new Date(time).toLocaleString(),
                    },
                    {
                      title: '执行人',
                      dataIndex: 'executedByName',
                      key: 'executedByName',
                    },
                    {
                      title: '运行次数',
                      dataIndex: 'runCount',
                      key: 'runCount',
                    },
                    {
                      title: '通过次数',
                      dataIndex: 'passCount',
                      key: 'passCount',
                    },
                    {
                      title: '失败次数',
                      dataIndex: 'failCount',
                      key: 'failCount',
                    },
                    {
                      title: '结果',
                      dataIndex: 'result',
                      key: 'result',
                      render: (result: string) => {
                        const resultMap: Record<string, { label: string; color: string }> = {
                          PASS: { label: '通过', color: 'success' },
                          FAIL: { label: '失败', color: 'error' },
                          PARTIAL: { label: '部分通过', color: 'warning' },
                        };
                        const info = resultMap[result] || { label: result, color: 'default' };
                        return <Tag color={info.color}>{info.label}</Tag>;
                      },
                    },
                    {
                      title: '操作',
                      key: 'action',
                      render: (_: any, record: VerificationDto) => (
                        <Button
                          type="link"
                          onClick={() => {
                            // TODO: 实现验证详情查看
                            message.info('验证详情查看功能待实现');
                          }}
                        >
                          查看详情
                        </Button>
                      ),
                    },
                  ]}
                  rowKey="verificationId"
                  pagination={false}
                />
              ) : (
                <Empty description="暂无验证记录" />
              )}
              {ticket.status === 'SolutionIssued' && (
                <div style={{ marginTop: 16 }}>
                  <Button
                    type="primary"
                    icon={<CheckCircleOutlined />}
                    onClick={() => navigate(`/tickets/${ticketId}/verification`)}
                  >
                    提交验证结果
                  </Button>
                </div>
              )}
            </Card>
          </TabPane>

          {/* 问诊式补全 */}
          {ticket.status === 'Draft' && (
            <TabPane tab={<span><QuestionCircleOutlined /> 补全信息</span>} key="missing-info">
              <Card>
                <AIEnhancedMissingInfoQuestionnaire
                  ticketId={ticketId!}
                  jcCode={ticket.currentJcCode}
                  enableAI={true}
                  onComplete={(ticketId) => {
                    message.success('信息补全成功');
                    loadTicketData();
                  }}
                />
              </Card>
            </TabPane>
          )}
        </Tabs>
      </Card>
    </div>
  );
}

