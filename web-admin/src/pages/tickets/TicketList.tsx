import React, { useEffect, useState } from 'react';
import {
  Table,
  Tag,
  Button,
  Space,
  message,
  Modal,
  Select,
  Input,
  Popconfirm,
  Alert,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  UserOutlined,
  FlagOutlined,
  DownloadOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import { ticketService, TicketListItemDto } from '../../services/ticketService';
import { ticketSearchService, TicketSearchResultItem } from '../../services/ticketSearchService';
import {
  ticketBatchService,
  BatchOperationResult,
} from '../../services/ticketBatchService';
import { ticketExportService } from '../../services/ticketExportService';
import { useNavigate } from 'react-router-dom';
import type { TableRowSelection } from 'antd/es/table/interface';

const TicketList: React.FC = () => {
  const [tickets, setTickets] = useState<TicketListItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [batchLoading, setBatchLoading] = useState(false);
  const [batchModalVisible, setBatchModalVisible] = useState(false);
  const [batchAction, setBatchAction] = useState<string>('');
  const [batchResult, setBatchResult] = useState<BatchOperationResult | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [isSearchMode, setIsSearchMode] = useState(false);
  const navigate = useNavigate();

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
  };

  useEffect(() => {
    if (isSearchMode && searchQuery) {
      handleSearch();
    } else {
      loadTickets();
    }
  }, [page, isSearchMode, searchQuery]);

  const loadTickets = async () => {
    setLoading(true);
    try {
      const result = await ticketService.getTickets({ page, pageSize: 20 });
      setTickets(result.items);
      setTotal(result.total);
    } catch (error: any) {
      console.error('Failed to load tickets:', error);
      const errorMessage = error?.message || '加载工单列表失败';
      message.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      setIsSearchMode(false);
      loadTickets();
      return;
    }

    setLoading(true);
    try {
      const result = await ticketSearchService.searchTickets({
        query: searchQuery,
        page,
        pageSize: 20,
      });

      // 转换搜索结果格式
      const convertedTickets: TicketListItemDto[] = result.items.map((item) => ({
        ticketId: item.ticketId,
        ticketNo: item.ticketNo,
        status: item.status,
        domain: item.domain,
        stepCode: item.stepCode,
        symptomTitle: item.symptomTitle || '',
        priority: item.priority,
        customerName: item.customerName,
        deviceSn: item.deviceSn,
        createdByName: item.createdByName,
        createdAt: item.createdAt,
      }));

      setTickets(convertedTickets);
      setTotal(result.total);
      setIsSearchMode(true);
    } catch (error: any) {
      console.error('Failed to search tickets:', error);
      message.error(error.message || '搜索失败');
    } finally {
      setLoading(false);
    }
  };

  const handleSearchChange = (value: string) => {
    setSearchQuery(value);
    if (!value.trim()) {
      setIsSearchMode(false);
      setPage(1);
      loadTickets();
    }
  };

  const handleBatchAction = (action: string) => {
    if (selectedRowKeys.length === 0) {
      message.warning('请先选择要操作的工单');
      return;
    }
    setBatchAction(action);
    setBatchModalVisible(true);
    setBatchResult(null);
  };

  const handleBatchUpdateStatus = async (newStatus: string, reason?: string) => {
    try {
      setBatchLoading(true);
      const result = await ticketBatchService.batchUpdateStatus({
        ticketIds: selectedRowKeys.map((k) => k.toString()),
        newStatus,
        reason,
      });
      setBatchResult(result);
      if (result.success) {
        message.success(result.message);
        setSelectedRowKeys([]);
        setBatchModalVisible(false);
        loadTickets();
      } else {
        message.warning(result.message);
      }
    } catch (error: any) {
      message.error(error.message || '批量更新状态失败');
    } finally {
      setBatchLoading(false);
    }
  };

  const handleBatchUpdatePriority = async (priority: string) => {
    try {
      setBatchLoading(true);
      const result = await ticketBatchService.batchUpdatePriority({
        ticketIds: selectedRowKeys.map((k) => k.toString()),
        priority,
      });
      setBatchResult(result);
      if (result.success) {
        message.success(result.message);
        setSelectedRowKeys([]);
        setBatchModalVisible(false);
        loadTickets();
      } else {
        message.warning(result.message);
      }
    } catch (error: any) {
      message.error(error.message || '批量更新优先级失败');
    } finally {
      setBatchLoading(false);
    }
  };

  const handleBatchDelete = async () => {
    try {
      setBatchLoading(true);
      const result = await ticketBatchService.batchDelete({
        ticketIds: selectedRowKeys.map((k) => k.toString()),
      });
      setBatchResult(result);
      if (result.success) {
        message.success(result.message);
        setSelectedRowKeys([]);
        setBatchModalVisible(false);
        loadTickets();
      } else {
        message.warning(result.message);
      }
    } catch (error: any) {
      message.error(error.message || '批量删除失败');
    } finally {
      setBatchLoading(false);
    }
  };

  const handleExport = async () => {
    try {
      await ticketExportService.exportToCsv({
        // 可以根据当前筛选条件导出
        fields: [
          'ticketNo',
          'status',
          'domain',
          'stepCode',
          'symptomTitle',
          'priority',
          'customerName',
          'deviceSn',
          'createdByName',
          'createdAt',
        ],
      });
      message.success('导出成功');
    } catch (error: any) {
      message.error(error.message || '导出失败');
    }
  };

  const rowSelection: TableRowSelection<TicketListItemDto> = {
    selectedRowKeys,
    onChange: (keys) => setSelectedRowKeys(keys),
    getCheckboxProps: (record) => ({
      disabled: record.status === 'Closed', // 已关闭的工单不能批量操作
    }),
  };

  const columns = [
    {
      title: '工单编号',
      dataIndex: 'ticketNo',
      key: 'ticketNo',
      render: (text: string, record: TicketListItemDto) => (
        <a onClick={() => navigate(`/tickets/${record.ticketId}`)}>{text || '草稿'}</a>
      ),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: TicketListItemDto) => (
        <Space>
          <Button type="link" size="small" onClick={() => navigate(`/tickets/${record.ticketId}`)}>
            查看
          </Button>
          {record.status === 'Submitted' && (
            <Button
              type="link"
              size="small"
              onClick={() => navigate(`/tickets/${record.ticketId}/triage`)}
            >
              分诊
            </Button>
          )}
          {record.status === 'Triage' && (
            <Button
              type="link"
              size="small"
              onClick={() => navigate(`/solutions/tickets/${record.ticketId}`)}
            >
              解决方案
            </Button>
          )}
          {record.status === 'SolutionIssued' && (
            <Button
              type="link"
              size="small"
              onClick={() => navigate(`/tickets/${record.ticketId}/verification`)}
            >
              验证
            </Button>
          )}
        </Space>
      ),
    },
    {
      title: '客户',
      dataIndex: 'customerName',
      key: 'customerName',
    },
    {
      title: '设备',
      dataIndex: 'deviceSn',
      key: 'deviceSn',
    },
    {
      title: '问题域',
      dataIndex: 'domain',
      key: 'domain',
      render: (domain: string) => {
        const info = domainMap[domain] || { label: domain, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '问题描述',
      dataIndex: 'symptomTitle',
      key: 'symptomTitle',
      ellipsis: true,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const info = statusMap[status] || { label: status, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '优先级',
      dataIndex: 'priority',
      key: 'priority',
      render: (priority: string) => <Tag>{priority}</Tag>,
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
      render: (text: string) => new Date(text).toLocaleString('zh-CN'),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>工单列表</h2>
        <Space>
          <Input.Search
            placeholder="搜索工单（标题、描述、事实表等）"
            allowClear
            enterButton={<SearchOutlined />}
            style={{ width: 300 }}
            value={searchQuery}
            onChange={(e) => handleSearchChange(e.target.value)}
            onSearch={handleSearch}
          />
          {selectedRowKeys.length > 0 && (
            <Space>
              <span>已选择 {selectedRowKeys.length} 项</span>
              <Button
                icon={<EditOutlined />}
                onClick={() => handleBatchAction('status')}
              >
                批量更新状态
              </Button>
              <Button
                icon={<FlagOutlined />}
                onClick={() => handleBatchAction('priority')}
              >
                批量更新优先级
              </Button>
              <Popconfirm
                title="确定要删除选中的工单吗？"
                onConfirm={handleBatchDelete}
                okText="确定"
                cancelText="取消"
              >
                <Button danger icon={<DeleteOutlined />}>
                  批量删除
                </Button>
              </Popconfirm>
            </Space>
          )}
          <Space>
            <Button
              icon={<DownloadOutlined />}
              onClick={() => handleExport()}
            >
              导出
            </Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/tickets/new')}>
              创建工单
            </Button>
          </Space>
        </Space>
      </div>
      <Table
        columns={columns}
        dataSource={tickets}
        loading={loading}
        rowKey="ticketId"
        rowSelection={rowSelection}
        pagination={{
          current: page,
          pageSize: 20,
          total,
          onChange: (p) => setPage(p),
        }}
      />

      {/* 批量操作模态框 */}
      <Modal
        title={
          batchAction === 'status'
            ? '批量更新状态'
            : batchAction === 'priority'
            ? '批量更新优先级'
            : '批量操作'
        }
        open={batchModalVisible}
        onCancel={() => {
          setBatchModalVisible(false);
          setBatchResult(null);
        }}
        footer={null}
        width={600}
      >
        {batchAction === 'status' && (
          <BatchUpdateStatusForm
            onFinish={handleBatchUpdateStatus}
            loading={batchLoading}
            result={batchResult}
          />
        )}
        {batchAction === 'priority' && (
          <BatchUpdatePriorityForm
            onFinish={handleBatchUpdatePriority}
            loading={batchLoading}
            result={batchResult}
          />
        )}
      </Modal>
    </div>
  );
};

// 批量更新状态表单组件
const BatchUpdateStatusForm: React.FC<{
  onFinish: (status: string, reason?: string) => void;
  loading: boolean;
  result: BatchOperationResult | null;
}> = ({ onFinish, loading, result }) => {
  const [status, setStatus] = useState<string>('');
  const [reason, setReason] = useState<string>('');

  return (
    <div>
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <div>
          <label>新状态：</label>
          <Select
            value={status}
            onChange={setStatus}
            style={{ width: '100%', marginTop: '8px' }}
            placeholder="请选择新状态"
          >
            <Select.Option value="Submitted">已提交</Select.Option>
            <Select.Option value="Triage">分诊中</Select.Option>
            <Select.Option value="SolutionIssued">方案已发布</Select.Option>
            <Select.Option value="Verifying">验证中</Select.Option>
            <Select.Option value="Closed">已关闭</Select.Option>
          </Select>
        </div>
        <div>
          <label>变更原因：</label>
          <Input.TextArea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={3}
            placeholder="请输入变更原因（可选）"
            style={{ marginTop: '8px' }}
          />
        </div>
        {result && (
          <Alert
            message={result.message}
            type={result.success ? 'success' : 'warning'}
            showIcon
            description={
              result.errors.length > 0 && (
                <ul style={{ marginTop: '8px', marginBottom: 0 }}>
                  {result.errors.map((error, index) => (
                    <li key={index}>
                      {error.ticketNo}: {error.errorMessage}
                    </li>
                  ))}
                </ul>
              )
            }
          />
        )}
        <div style={{ textAlign: 'right' }}>
          <Space>
            <Button
              onClick={() => {
                setStatus('');
                setReason('');
              }}
            >
              重置
            </Button>
            <Button
              type="primary"
              loading={loading}
              onClick={() => onFinish(status, reason)}
              disabled={!status}
            >
              确定
            </Button>
          </Space>
        </div>
      </Space>
    </div>
  );
};

// 批量更新优先级表单组件
const BatchUpdatePriorityForm: React.FC<{
  onFinish: (priority: string) => void;
  loading: boolean;
  result: BatchOperationResult | null;
}> = ({ onFinish, loading, result }) => {
  const [priority, setPriority] = useState<string>('');

  return (
    <div>
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <div>
          <label>新优先级：</label>
          <Select
            value={priority}
            onChange={setPriority}
            style={{ width: '100%', marginTop: '8px' }}
            placeholder="请选择新优先级"
          >
            <Select.Option value="P0">P0 - 紧急</Select.Option>
            <Select.Option value="P1">P1 - 高</Select.Option>
            <Select.Option value="P2">P2 - 中</Select.Option>
            <Select.Option value="P3">P3 - 低</Select.Option>
          </Select>
        </div>
        {result && (
          <Alert
            message={result.message}
            type={result.success ? 'success' : 'warning'}
            showIcon
            description={
              result.errors.length > 0 && (
                <ul style={{ marginTop: '8px', marginBottom: 0 }}>
                  {result.errors.map((error, index) => (
                    <li key={index}>
                      {error.ticketNo}: {error.errorMessage}
                    </li>
                  ))}
                </ul>
              )
            }
          />
        )}
        <div style={{ textAlign: 'right' }}>
          <Space>
            <Button onClick={() => setPriority('')}>重置</Button>
            <Button
              type="primary"
              loading={loading}
              onClick={() => onFinish(priority)}
              disabled={!priority}
            >
              确定
            </Button>
          </Space>
        </div>
      </Space>
    </div>
  );
};

export default TicketList;

