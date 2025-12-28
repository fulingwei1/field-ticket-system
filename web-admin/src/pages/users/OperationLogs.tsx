import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Table,
  Card,
  Tag,
  Space,
  message,
  Result,
  Button,
  DatePicker,
  Select,
  Input,
  Row,
  Col,
  Statistic,
  Modal,
} from 'antd';
import {
  HistoryOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExclamationCircleOutlined,
  StopOutlined,
  EyeOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import operationLogService, {
  OperationLogDto,
  OperationLogDetailDto,
  OperationLogQueryRequest,
} from '../../services/operationLogService';
import authService from '../../services/authService';

const { RangePicker } = DatePicker;
const { Option } = Select;
const { Search } = Input;

/**
 * 操作日志页面
 */
const OperationLogs: React.FC = () => {
  const navigate = useNavigate();
  const [logs, setLogs] = useState<OperationLogDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [operationType, setOperationType] = useState<string | undefined>();
  const [result, setResult] = useState<string | undefined>();
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs | null, dayjs.Dayjs | null] | null>(null);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [selectedLog, setSelectedLog] = useState<OperationLogDetailDto | null>(null);

  // 权限检查
  useEffect(() => {
    if (!authService.isAdmin()) {
      message.error('只有管理员才能访问操作日志');
      navigate('/');
    }
  }, [navigate]);

  // 加载日志
  useEffect(() => {
    loadLogs();
  }, [page, pageSize, operationType, result, dateRange, searchKeyword]);

  const loadLogs = async () => {
    setLoading(true);
    try {
      const request: OperationLogQueryRequest = {
        page,
        pageSize,
        operationType,
        result,
        searchKeyword: searchKeyword || undefined,
      };

      if (dateRange && dateRange[0] && dateRange[1]) {
        request.startTime = dateRange[0].toISOString();
        request.endTime = dateRange[1].toISOString();
      }

      const queryResult = await operationLogService.query(request);
      setLogs(queryResult.items);
      setTotal(queryResult.total);
    } catch (error: any) {
      console.error('Failed to load logs:', error);
      message.error(error.message || '加载日志失败');
    } finally {
      setLoading(false);
    }
  };

  const handleViewDetail = async (logId: string) => {
    try {
      setLoading(true);
      const detail = await operationLogService.getDetail(logId);
      setSelectedLog(detail);
      setDetailModalVisible(true);
    } catch (error: any) {
      message.error(error.message || '获取详情失败');
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    setOperationType(undefined);
    setResult(undefined);
    setDateRange(null);
    setSearchKeyword('');
    setPage(1);
  };

  if (!authService.isAdmin()) {
    return (
      <div style={{ padding: 24 }}>
        <Result
          status="403"
          title="访问受限"
          subTitle="抱歉，您没有权限访问此页面。"
          icon={<StopOutlined />}
          extra={<Button type="primary" onClick={() => navigate('/')}>返回首页</Button>}
        />
      </div>
    );
  }

  const columns = [
    {
      title: '操作时间',
      dataIndex: 'operatedAt',
      key: 'operatedAt',
      width: 180,
      render: (time: string) => dayjs(time).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: '操作类型',
      dataIndex: 'operationTypeDisplay',
      key: 'operationType',
      width: 140,
      render: (text: string, record: OperationLogDto) => (
        <Tag color="blue">{text}</Tag>
      ),
    },
    {
      title: '操作人',
      dataIndex: 'operatorName',
      key: 'operatorName',
      width: 100,
    },
    {
      title: '操作描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '结果',
      dataIndex: 'result',
      key: 'result',
      width: 100,
      render: (result: string) => {
        const config = {
          Success: { color: 'success', icon: <CheckCircleOutlined />, text: '成功' },
          Failed: { color: 'error', icon: <CloseCircleOutlined />, text: '失败' },
          Partial: { color: 'warning', icon: <ExclamationCircleOutlined />, text: '部分成功' },
        }[result] || { color: 'default', icon: null, text: result };

        return (
          <Tag color={config.color} icon={config.icon}>
            {config.text}
          </Tag>
        );
      },
    },
    {
      title: '统计',
      key: 'stats',
      width: 180,
      render: (_: any, record: OperationLogDto) => (
        <Space size="small">
          <span>总:{record.totalCount}</span>
          {record.successCount > 0 && <Tag color="green">成功:{record.successCount}</Tag>}
          {record.failedCount > 0 && <Tag color="red">失败:{record.failedCount}</Tag>}
          {record.skippedCount > 0 && <Tag color="orange">跳过:{record.skippedCount}</Tag>}
        </Space>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 80,
      fixed: 'right' as const,
      render: (_: any, record: OperationLogDto) => (
        <Button
          type="link"
          size="small"
          icon={<EyeOutlined />}
          onClick={() => handleViewDetail(record.id)}
        >
          详情
        </Button>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Card
        title={
          <Space>
            <HistoryOutlined />
            <span>操作日志</span>
          </Space>
        }
      >
        {/* 筛选条件 */}
        <Space direction="vertical" size="middle" style={{ width: '100%', marginBottom: 16 }}>
          <Row gutter={16}>
            <Col span={6}>
              <Select
                placeholder="操作类型"
                allowClear
                style={{ width: '100%' }}
                value={operationType}
                onChange={setOperationType}
              >
                <Option value="EmployeeImport">员工批量导入</Option>
                <Option value="EmployeeUpdate">员工批量更新</Option>
                <Option value="AccountActivation">账户开通</Option>
                <Option value="AccountBatchActivation">批量账户开通</Option>
              </Select>
            </Col>
            <Col span={6}>
              <Select
                placeholder="操作结果"
                allowClear
                style={{ width: '100%' }}
                value={result}
                onChange={setResult}
              >
                <Option value="Success">成功</Option>
                <Option value="Failed">失败</Option>
                <Option value="Partial">部分成功</Option>
              </Select>
            </Col>
            <Col span={8}>
              <RangePicker
                style={{ width: '100%' }}
                value={dateRange}
                onChange={setDateRange}
                showTime
                format="YYYY-MM-DD HH:mm"
              />
            </Col>
            <Col span={4}>
              <Space>
                <Button onClick={handleReset}>重置</Button>
                <Button type="primary" icon={<ReloadOutlined />} onClick={loadLogs}>
                  刷新
                </Button>
              </Space>
            </Col>
          </Row>
          <Row>
            <Col span={24}>
              <Search
                placeholder="搜索操作人或描述"
                allowClear
                onSearch={setSearchKeyword}
                style={{ width: '100%' }}
              />
            </Col>
          </Row>
        </Space>

        {/* 日志表格 */}
        <Table
          columns={columns}
          dataSource={logs}
          rowKey="id"
          loading={loading}
          scroll={{ x: 1200 }}
          pagination={{
            current: page,
            pageSize: pageSize,
            total: total,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total) => `共 ${total} 条记录`,
            onChange: (page, pageSize) => {
              setPage(page);
              setPageSize(pageSize);
            },
          }}
        />
      </Card>

      {/* 详情对话框 */}
      <Modal
        title="操作日志详情"
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setDetailModalVisible(false)}>
            关闭
          </Button>,
        ]}
        width={800}
      >
        {selectedLog && (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Row gutter={16}>
              <Col span={12}>
                <Statistic title="操作人" value={selectedLog.operatorName} />
              </Col>
              <Col span={12}>
                <Statistic
                  title="操作时间"
                  value={dayjs(selectedLog.operatedAt).format('YYYY-MM-DD HH:mm:ss')}
                />
              </Col>
            </Row>
            <Row gutter={16}>
              <Col span={8}>
                <Statistic title="总数" value={selectedLog.totalCount} />
              </Col>
              <Col span={8}>
                <Statistic title="成功" value={selectedLog.successCount} valueStyle={{ color: '#3f8600' }} />
              </Col>
              <Col span={8}>
                <Statistic title="失败" value={selectedLog.failedCount} valueStyle={{ color: '#cf1322' }} />
              </Col>
            </Row>

            {selectedLog.sourceFileName && (
              <div>
                <strong>源文件：</strong>
                {selectedLog.sourceFileName}
              </div>
            )}

            {selectedLog.ipAddress && (
              <div>
                <strong>IP地址：</strong>
                {selectedLog.ipAddress}
              </div>
            )}

            {selectedLog.errorMessages.length > 0 && (
              <div>
                <strong>错误消息：</strong>
                <ul style={{ margin: '8px 0', paddingLeft: 20 }}>
                  {selectedLog.errorMessages.map((msg, idx) => (
                    <li key={idx} style={{ color: '#cf1322' }}>
                      {msg}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {selectedLog.details && (
              <div>
                <strong>详细数据：</strong>
                <pre
                  style={{
                    background: '#f5f5f5',
                    padding: 12,
                    borderRadius: 4,
                    maxHeight: 300,
                    overflow: 'auto',
                  }}
                >
                  {JSON.stringify(selectedLog.details, null, 2)}
                </pre>
              </div>
            )}
          </Space>
        )}
      </Modal>
    </div>
  );
};

export default OperationLogs;
