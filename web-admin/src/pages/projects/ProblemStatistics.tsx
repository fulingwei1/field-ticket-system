"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  Table,
  Select,
  DatePicker,
  Button,
  Space,
  message,
  Tag,
  Progress,
  Typography,
  Alert,
} from 'antd';
import {
  ReloadOutlined,
  BarChartOutlined,
  FileTextOutlined,
  TeamOutlined,
  CustomerServiceOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import {
  projectService,
  ProblemStatisticsDto,
  ProblemStatisticsFilter,
  CategoryStatistics,
  DepartmentStatistics,
  CustomerStatistics,
} from '../../services/projectService';
import dayjs, { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;
const { Option } = Select;
const { Title } = Typography;

export default function ProblemStatistics() {
  const [loading, setLoading] = useState(false);
  const [statistics, setStatistics] = useState<ProblemStatisticsDto | null>(null);
  const [problemCategory, setProblemCategory] = useState<string | undefined>();
  const [primaryDepartment, setPrimaryDepartment] = useState<string | undefined>();
  const [customerName, setCustomerName] = useState<string | undefined>();
  const [status, setStatus] = useState<string | undefined>();
  const [foundDateRange, setFoundDateRange] = useState<[Dayjs | null, Dayjs | null] | null>(null);

  useEffect(() => {
    loadStatistics();
  }, [problemCategory, primaryDepartment, customerName, status, foundDateRange]);

  const loadStatistics = async () => {
    setLoading(true);
    try {
      const filter: ProblemStatisticsFilter = {
        problemCategory: problemCategory,
        primaryDepartment: primaryDepartment,
        customerName: customerName,
        status: status,
        foundDateFrom: foundDateRange?.[0]?.format('YYYY-MM-DD'),
        foundDateTo: foundDateRange?.[1]?.format('YYYY-MM-DD'),
      };
      const data = await projectService.getProblemStatistics(filter);
      setStatistics(data);
    } catch (error: any) {
      message.error('加载统计数据失败：' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const categoryColumns = [
    {
      title: '问题分类',
      dataIndex: 'category',
      key: 'category',
    },
    {
      title: '数量',
      dataIndex: 'count',
      key: 'count',
      render: (count: number) => <Tag color="blue">{count}</Tag>,
    },
    {
      title: '占比',
      dataIndex: 'percentage',
      key: 'percentage',
      render: (percentage: number) => (
        <Progress percent={Math.round(percentage)} size="small" />
      ),
    },
  ];

  const departmentColumns = [
    {
      title: '部门',
      dataIndex: 'department',
      key: 'department',
    },
    {
      title: '问题数量',
      dataIndex: 'count',
      key: 'count',
      render: (count: number) => <Tag color="orange">{count}</Tag>,
    },
    {
      title: '平均处理周期',
      dataIndex: 'averageProcessingDays',
      key: 'averageProcessingDays',
      render: (days: number) => `${Math.round(days)} 天`,
    },
    {
      title: '平均满意度',
      dataIndex: 'averageSatisfactionScore',
      key: 'averageSatisfactionScore',
      render: (score: number) => (
        <Tag color={score >= 4 ? 'success' : score >= 3 ? 'warning' : 'error'}>
          {score.toFixed(1)} 分
        </Tag>
      ),
    },
  ];

  const customerColumns = [
    {
      title: '客户名称',
      dataIndex: 'customerName',
      key: 'customerName',
    },
    {
      title: '问题数量',
      dataIndex: 'count',
      key: 'count',
      render: (count: number) => <Tag color="green">{count}</Tag>,
    },
    {
      title: '平均满意度',
      dataIndex: 'averageSatisfactionScore',
      key: 'averageSatisfactionScore',
      render: (score: number) => (
        <Tag color={score >= 4 ? 'success' : score >= 3 ? 'warning' : 'error'}>
          {score.toFixed(1)} 分
        </Tag>
      ),
    },
  ];

  return (
    <div>
      <Card>
        <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
          <Title level={4}>
            <BarChartOutlined /> 问题分类统计
          </Title>
          <Button icon={<ReloadOutlined />} onClick={loadStatistics}>
            刷新
          </Button>
        </Space>

        {/* 筛选条件 */}
        <Space wrap style={{ marginBottom: 16 }}>
          <Select
            placeholder="问题分类"
            value={problemCategory}
            onChange={setProblemCategory}
            style={{ width: 120 }}
            allowClear
          >
            <Option value="设计">设计</Option>
            <Option value="工艺">工艺</Option>
            <Option value="管理">管理</Option>
            <Option value="其他">其他</Option>
          </Select>
          <Select
            placeholder="主负责部门"
            value={primaryDepartment}
            onChange={setPrimaryDepartment}
            style={{ width: 150 }}
            allowClear
          />
          <Select
            placeholder="处理状态"
            value={status}
            onChange={setStatus}
            style={{ width: 120 }}
            allowClear
          >
            <Option value="待分配">待分配</Option>
            <Option value="处理中">处理中</Option>
            <Option value="待验证">待验证</Option>
            <Option value="验证中">验证中</Option>
            <Option value="已验证">已验证</Option>
            <Option value="验证失败">验证失败</Option>
            <Option value="已关闭">已关闭</Option>
          </Select>
          <RangePicker
            placeholder={['发现日期起', '发现日期止']}
            value={foundDateRange}
            onChange={setFoundDateRange}
          />
        </Space>

        {/* 统计概览 */}
        {statistics && (
          <Row gutter={16} style={{ marginBottom: 24 }}>
            <Col span={6}>
              <Card>
                <Statistic
                  title="总问题数"
                  value={statistics.totalProblems}
                  prefix={<FileTextOutlined />}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card>
                <Statistic
                  title="已关闭"
                  value={statistics.closedProblems}
                  valueStyle={{ color: '#3f8600' }}
                  prefix={<CheckCircleOutlined />}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card>
                <Statistic
                  title="进行中"
                  value={statistics.openProblems}
                  valueStyle={{ color: '#cf1322' }}
                  prefix={<FileTextOutlined />}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card>
                <Statistic
                  title="平均处理周期"
                  value={Math.round(statistics.averageProcessingDays)}
                  suffix="天"
                  prefix={<ReloadOutlined />}
                />
              </Card>
            </Col>
          </Row>
        )}

        {/* 分类统计 */}
        {statistics && (
          <Card title="按问题分类统计" style={{ marginBottom: 16 }}>
            <Table
              columns={categoryColumns}
              dataSource={statistics.categoryStatistics}
              rowKey="category"
              pagination={false}
              loading={loading}
            />
          </Card>
        )}

        {/* 部门统计 */}
        {statistics && (
          <Card title="按部门统计" style={{ marginBottom: 16 }}>
            <Table
              columns={departmentColumns}
              dataSource={statistics.departmentStatistics}
              rowKey="department"
              pagination={false}
              loading={loading}
            />
          </Card>
        )}

        {/* 客户统计 */}
        {statistics && (
          <Card title="按客户统计">
            <Table
              columns={customerColumns}
              dataSource={statistics.customerStatistics}
              rowKey="customerName"
              pagination={false}
              loading={loading}
            />
          </Card>
        )}
      </Card>
    </div>
  );
}

