"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  message,
  Modal,
  Form,
  Select,
  DatePicker,
  Input,
  Popconfirm,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import {
  correctiveActionService,
  CorrectiveActionDto,
  CorrectiveActionQueryFilter,
} from '../../services/correctiveActionService';
import dayjs, { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;
const { Option } = Select;

const STATUS_MAP: Record<string, { label: string; color: string }> = {
  open: { label: '待处理', color: 'default' },
  in_progress: { label: '进行中', color: 'processing' },
  completed: { label: '已完成', color: 'success' },
  closed: { label: '已关闭', color: 'default' },
  cancelled: { label: '已取消', color: 'error' },
};

const RESPONSIBILITY_MAP: Record<string, string> = {
  design: '设计问题',
  software: '软件问题',
  parameter: '参数问题',
  assembly: '装配问题',
  documentation: '文档问题',
  other: '其他',
  unknown: '未知',
};

export default function CorrectiveActionList() {
  const [loading, setLoading] = useState(false);
  const [actions, setActions] = useState<CorrectiveActionDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<CorrectiveActionQueryFilter>({});
  const navigate = useNavigate();

  useEffect(() => {
    loadActions();
  }, [page, filter]);

  const loadActions = async () => {
    try {
      setLoading(true);
      const result = await correctiveActionService.getActions({
        ...filter,
        page,
        pageSize: 20,
      });
      setActions(result.items);
      setTotal(result.total);
    } catch (error: any) {
      message.error(error.message || '加载整改任务列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (actionId: string) => {
    try {
      await correctiveActionService.deleteAction(actionId);
      message.success('删除成功');
      loadActions();
    } catch (error: any) {
      message.error(error.message || '删除失败');
    }
  };

  const handleStatusChange = async (actionId: string, status: string) => {
    try {
      await correctiveActionService.updateActionStatus(actionId, status);
      message.success('状态更新成功');
      loadActions();
    } catch (error: any) {
      message.error(error.message || '状态更新失败');
    }
  };

  const columns = [
    {
      title: '任务编号',
      dataIndex: 'actionCode',
      key: 'actionCode',
      width: 150,
      render: (text: string, record: CorrectiveActionDto) => (
        <Button
          type="link"
          onClick={() => navigate(`/corrective-actions/${record.actionId}`)}
        >
          {text}
        </Button>
      ),
    },
    {
      title: '问题描述',
      dataIndex: 'problemDescription',
      key: 'problemDescription',
      ellipsis: true,
    },
    {
      title: '根因分类',
      dataIndex: 'rootResponsibilityName',
      key: 'rootResponsibility',
      width: 120,
      render: (text: string) => text ? <Tag>{text}</Tag> : '-',
    },
    {
      title: '负责人',
      dataIndex: 'responsiblePersonName',
      key: 'responsiblePersonName',
      width: 120,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => {
        const info = STATUS_MAP[status] || { label: status, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '目标完成日期',
      dataIndex: 'targetCompletionDate',
      key: 'targetCompletionDate',
      width: 120,
      render: (text: string) => text ? new Date(text).toLocaleDateString() : '-',
    },
    {
      title: '关联工单',
      dataIndex: 'relatedTicketNos',
      key: 'relatedTicketNos',
      width: 150,
      render: (ticketNos: string[]) => {
        if (!ticketNos || ticketNos.length === 0) return '-';
        return (
          <Space size="small">
            {ticketNos.slice(0, 2).map((no) => (
              <Tag key={no}>{no}</Tag>
            ))}
            {ticketNos.length > 2 && <span>+{ticketNos.length - 2}</span>}
          </Space>
        );
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (text: string) => new Date(text).toLocaleString(),
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      fixed: 'right' as const,
      render: (_: any, record: CorrectiveActionDto) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/corrective-actions/${record.actionId}`)}
          >
            查看
          </Button>
          {record.status === 'open' && (
            <Button
              type="link"
              size="small"
              icon={<CheckCircleOutlined />}
              onClick={() => handleStatusChange(record.actionId, 'in_progress')}
            >
              开始
            </Button>
          )}
          {record.status === 'in_progress' && (
            <Button
              type="link"
              size="small"
              icon={<CheckCircleOutlined />}
              onClick={() => handleStatusChange(record.actionId, 'completed')}
            >
              完成
            </Button>
          )}
          <Popconfirm
            title="确定要删除这个整改任务吗？"
            onConfirm={() => handleDelete(record.actionId)}
            okText="确定"
            cancelText="取消"
          >
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
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
        title="整改任务列表"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => navigate('/corrective-actions/new')}
          >
            创建整改任务
          </Button>
        }
      >
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          {/* 筛选条件 */}
          <Space wrap>
            <Select
              placeholder="状态"
              allowClear
              style={{ width: 120 }}
              onChange={(value) => setFilter({ ...filter, status: value || undefined })}
            >
              {Object.entries(STATUS_MAP).map(([value, info]) => (
                <Option key={value} value={value}>
                  {info.label}
                </Option>
              ))}
            </Select>
            <Select
              placeholder="根因分类"
              allowClear
              style={{ width: 120 }}
              onChange={(value) => setFilter({ ...filter, rootResponsibility: value || undefined })}
            >
              {Object.entries(RESPONSIBILITY_MAP).map(([value, label]) => (
                <Option key={value} value={value}>
                  {label}
                </Option>
              ))}
            </Select>
            <RangePicker
              onChange={(dates) => {
                if (dates) {
                  setFilter({
                    ...filter,
                    createdFrom: dates[0]?.format('YYYY-MM-DD'),
                    createdTo: dates[1]?.format('YYYY-MM-DD'),
                  });
                } else {
                  setFilter({
                    ...filter,
                    createdFrom: undefined,
                    createdTo: undefined,
                  });
                }
              }}
            />
            <Button onClick={loadActions}>查询</Button>
          </Space>

          {/* 表格 */}
          <Table
            columns={columns}
            dataSource={actions}
            rowKey="actionId"
            loading={loading}
            pagination={{
              current: page,
              pageSize: 20,
              total,
              onChange: (p) => setPage(p),
            }}
            scroll={{ x: 1400 }}
          />
        </Space>
      </Card>
    </div>
  );
}








