"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Table,
  Button,
  Space,
  message,
  Spin,
  Tag,
  Popconfirm,
} from 'antd';
import { ArrowLeftOutlined, PlusOutlined, EditOutlined, SendOutlined } from '@ant-design/icons';
import { solutionService, SolutionDto } from '../../services/solutionService';
import { ticketService, TicketDto } from '../../services/ticketService';

export default function SolutionList() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [ticket, setTicket] = useState<TicketDto | null>(null);
  const [solutions, setSolutions] = useState<SolutionDto[]>([]);
  const [publishing, setPublishing] = useState<string | null>(null);

  useEffect(() => {
    if (ticketId) {
      loadData();
    }
  }, [ticketId]);

  const loadData = async () => {
    if (!ticketId) return;
    
    try {
      setLoading(true);
      const [ticketData, solutionsData] = await Promise.all([
        ticketService.getTicket(ticketId),
        solutionService.getTicketSolutions(ticketId),
      ]);
      setTicket(ticketData);
      setSolutions(solutionsData);
    } catch (error: any) {
      message.error(error.message || '加载数据失败');
    } finally {
      setLoading(false);
    }
  };

  const handlePublish = async (solutionId: string) => {
    try {
      setPublishing(solutionId);
      await solutionService.publishSolution(solutionId);
      message.success('解决方案已发布');
      loadData();
    } catch (error: any) {
      message.error(error.message || '发布失败');
    } finally {
      setPublishing(null);
    }
  };

  const statusMap: Record<string, { label: string; color: string }> = {
    Draft: { label: '草稿', color: 'default' },
    Published: { label: '已发布', color: 'success' },
    Archived: { label: '已归档', color: 'default' },
  };

  const riskLevelMap: Record<string, { label: string; color: string }> = {
    low: { label: '低', color: 'green' },
    medium: { label: '中', color: 'orange' },
    high: { label: '高', color: 'red' },
  };

  const columns = [
    {
      title: '解决方案编号',
      dataIndex: 'solutionCode',
      key: 'solutionCode',
      render: (text: string) => text || <Tag color="default">未发布</Tag>,
    },
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
    },
    {
      title: '方案类型',
      dataIndex: 'solutionType',
      key: 'solutionType',
    },
    {
      title: '发布类型',
      dataIndex: 'releaseType',
      key: 'releaseType',
    },
    {
      title: '风险等级',
      dataIndex: 'riskLevel',
      key: 'riskLevel',
      render: (level: string) => {
        const map = riskLevelMap[level] || { label: level, color: 'default' };
        return <Tag color={map.color}>{map.label}</Tag>;
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const map = statusMap[status] || { label: status, color: 'default' };
        return <Tag color={map.color}>{map.label}</Tag>;
      },
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
      render: (_: any, record: SolutionDto) => (
        <Space>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => navigate(`/tickets/${ticketId}/solutions/${record.solutionId}`)}
          >
            编辑
          </Button>
          {record.status === 'Draft' && (
            <Popconfirm
              title="确定要发布此解决方案吗？"
              description="发布后工单状态将变为 SolutionIssued"
              onConfirm={() => handlePublish(record.solutionId)}
              okText="确定"
              cancelText="取消"
            >
              <Button
                type="link"
                icon={<SendOutlined />}
                loading={publishing === record.solutionId}
              >
                发布
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 返回按钮 */}
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate(`/tickets/${ticketId}`)}
        >
          返回工单详情
        </Button>

        {/* 工单信息 */}
        {ticket && (
          <Card title="关联工单">
            <Space>
              <span>工单编号：{ticket.ticketNo || '未生成'}</span>
              <span>状态：{ticket.status}</span>
              <span>问题域：{ticket.domain}</span>
            </Space>
          </Card>
        )}

        {/* 解决方案列表 */}
        <Card
          title="解决方案列表"
          extra={
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate(`/tickets/${ticketId}/solutions/new`)}
            >
              创建解决方案
            </Button>
          }
        >
          <Spin spinning={loading}>
            <Table
              columns={columns}
              dataSource={solutions}
              rowKey="solutionId"
              pagination={false}
            />
          </Spin>
        </Card>
      </Space>
    </div>
  );
}
