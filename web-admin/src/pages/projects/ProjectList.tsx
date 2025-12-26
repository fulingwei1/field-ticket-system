"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Input,
  Select,
  DatePicker,
  Tag,
  message,
  Modal,
  Descriptions,
  Tabs,
  Typography,
} from 'antd';
import {
  SearchOutlined,
  EyeOutlined,
  ReloadOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import { projectService, ProjectDto, ProjectDetailDto, FieldProblemDto } from '../../services/projectService';
import { useNavigate } from 'react-router-dom';
import dayjs, { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;
const { Option } = Select;
const { Title, Text } = Typography;
const { TabPane } = Tabs;

export default function ProjectList() {
  const [loading, setLoading] = useState(false);
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [selectedProject, setSelectedProject] = useState<ProjectDetailDto | null>(null);
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  
  // 筛选条件
  const [projectNo, setProjectNo] = useState('');
  const [projectName, setProjectName] = useState('');
  const [customerName, setCustomerName] = useState('');
  const [deviceType, setDeviceType] = useState<string | undefined>();
  const [projectStatus, setProjectStatus] = useState<string | undefined>();
  const [orderDateRange, setOrderDateRange] = useState<[Dayjs | null, Dayjs | null] | null>(null);

  const navigate = useNavigate();

  useEffect(() => {
    loadProjects();
  }, [page, pageSize, projectNo, projectName, customerName, deviceType, projectStatus, orderDateRange]);

  const loadProjects = async () => {
    setLoading(true);
    try {
      const filter = {
        projectNo: projectNo || undefined,
        projectName: projectName || undefined,
        customerName: customerName || undefined,
        deviceType: deviceType,
        projectStatus: projectStatus,
        orderDateFrom: orderDateRange?.[0]?.format('YYYY-MM-DD'),
        orderDateTo: orderDateRange?.[1]?.format('YYYY-MM-DD'),
      };
      const result = await projectService.getProjects(filter, page, pageSize);
      setProjects(result.items);
      setTotal(result.total);
    } catch (error: any) {
      message.error('加载项目列表失败：' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const handleViewDetail = async (projectId: string) => {
    setLoadingDetail(true);
    try {
      const project = await projectService.getProject(projectId);
      setSelectedProject(project);
      setDetailModalVisible(true);
    } catch (error: any) {
      message.error('加载项目详情失败：' + error.message);
    } finally {
      setLoadingDetail(false);
    }
  };

  const getStatusColor = (status: string) => {
    const statusMap: Record<string, string> = {
      '进行中': 'processing',
      '已交付': 'success',
      '已验证': 'success',
      '有问题': 'error',
    };
    return statusMap[status] || 'default';
  };

  const columns = [
    {
      title: '项目号',
      dataIndex: 'projectNo',
      key: 'projectNo',
      width: 150,
    },
    {
      title: '项目名称',
      dataIndex: 'projectName',
      key: 'projectName',
      width: 200,
    },
    {
      title: '客户名称',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 150,
    },
    {
      title: '设备类型',
      dataIndex: 'deviceType',
      key: 'deviceType',
      width: 100,
    },
    {
      title: '项目状态',
      dataIndex: 'projectStatus',
      key: 'projectStatus',
      width: 100,
      render: (status: string) => (
        <Tag color={getStatusColor(status)}>{status}</Tag>
      ),
    },
    {
      title: '问题数量',
      dataIndex: 'problemCount',
      key: 'problemCount',
      width: 100,
      render: (count: number) => (
        <Tag color={count > 0 ? 'orange' : 'default'}>{count}</Tag>
      ),
    },
    {
      title: '项目经理',
      dataIndex: 'projectManagerName',
      key: 'projectManagerName',
      width: 120,
    },
    {
      title: '下单日期',
      dataIndex: 'orderDate',
      key: 'orderDate',
      width: 120,
      render: (date: string) => date ? dayjs(date).format('YYYY-MM-DD') : '-',
    },
    {
      title: '操作',
      key: 'actions',
      width: 120,
      render: (_: any, record: ProjectDto) => (
        <Space>
          <Button
            size="small"
            icon={<EyeOutlined />}
            onClick={() => handleViewDetail(record.projectId)}
          >
            查看
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Card>
        <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
          <Title level={4}>
            <FileTextOutlined /> 项目管理
          </Title>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadProjects}>
              刷新
            </Button>
            <Button
              type="primary"
              onClick={() => navigate('/projects/excel-import')}
            >
              Excel导入
            </Button>
          </Space>
        </Space>

        {/* 筛选条件 */}
        <Space wrap style={{ marginBottom: 16 }}>
          <Input
            placeholder="项目号"
            value={projectNo}
            onChange={(e) => setProjectNo(e.target.value)}
            style={{ width: 150 }}
            allowClear
          />
          <Input
            placeholder="项目名称"
            value={projectName}
            onChange={(e) => setProjectName(e.target.value)}
            style={{ width: 200 }}
            allowClear
          />
          <Input
            placeholder="客户名称"
            value={customerName}
            onChange={(e) => setCustomerName(e.target.value)}
            style={{ width: 150 }}
            allowClear
          />
          <Select
            placeholder="设备类型"
            value={deviceType}
            onChange={setDeviceType}
            style={{ width: 120 }}
            allowClear
          >
            <Option value="线体">线体</Option>
            <Option value="单机">单机</Option>
            <Option value="其他">其他</Option>
          </Select>
          <Select
            placeholder="项目状态"
            value={projectStatus}
            onChange={setProjectStatus}
            style={{ width: 120 }}
            allowClear
          >
            <Option value="进行中">进行中</Option>
            <Option value="已交付">已交付</Option>
            <Option value="已验证">已验证</Option>
            <Option value="有问题">有问题</Option>
          </Select>
          <RangePicker
            placeholder={['下单日期起', '下单日期止']}
            value={orderDateRange}
            onChange={setOrderDateRange}
          />
          <Button type="primary" icon={<SearchOutlined />} onClick={loadProjects}>
            搜索
          </Button>
        </Space>

        <Table
          columns={columns}
          dataSource={projects}
          rowKey="projectId"
          loading={loading}
          pagination={{
            current: page,
            pageSize: pageSize,
            total: total,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条`,
            onChange: (page, pageSize) => {
              setPage(page);
              setPageSize(pageSize);
            },
          }}
        />
      </Card>

      {/* 项目详情模态框 */}
      <Modal
        title="项目详情"
        open={detailModalVisible}
        onCancel={() => {
          setDetailModalVisible(false);
          setSelectedProject(null);
        }}
        footer={null}
        width={900}
      >
        {loadingDetail ? (
          <div style={{ textAlign: 'center', padding: '50px' }}>
            <Text>加载中...</Text>
          </div>
        ) : selectedProject ? (
          <Tabs defaultActiveKey="info">
            <TabPane tab="项目信息" key="info">
              <Descriptions bordered column={2}>
                <Descriptions.Item label="项目号">{selectedProject.projectNo}</Descriptions.Item>
                <Descriptions.Item label="项目名称">{selectedProject.projectName}</Descriptions.Item>
                <Descriptions.Item label="客户名称">{selectedProject.customerName || '-'}</Descriptions.Item>
                <Descriptions.Item label="设备类型">{selectedProject.deviceType}</Descriptions.Item>
                <Descriptions.Item label="行业类型">{selectedProject.industryType || '-'}</Descriptions.Item>
                <Descriptions.Item label="项目状态">
                  <Tag color={getStatusColor(selectedProject.projectStatus)}>
                    {selectedProject.projectStatus}
                  </Tag>
                </Descriptions.Item>
                <Descriptions.Item label="销售金额">
                  {selectedProject.salesAmount ? `¥${selectedProject.salesAmount.toLocaleString()}` : '-'}
                </Descriptions.Item>
                <Descriptions.Item label="数量">{selectedProject.quantity}</Descriptions.Item>
                <Descriptions.Item label="下单日期">
                  {selectedProject.orderDate ? dayjs(selectedProject.orderDate).format('YYYY-MM-DD') : '-'}
                </Descriptions.Item>
                <Descriptions.Item label="要求交货日期">
                  {selectedProject.requiredDeliveryDate ? dayjs(selectedProject.requiredDeliveryDate).format('YYYY-MM-DD') : '-'}
                </Descriptions.Item>
                <Descriptions.Item label="实际交货日期">
                  {selectedProject.actualDeliveryDate ? dayjs(selectedProject.actualDeliveryDate).format('YYYY-MM-DD') : '-'}
                </Descriptions.Item>
                <Descriptions.Item label="延期天数">
                  {selectedProject.deliveryDelayDays !== null && selectedProject.deliveryDelayDays !== undefined
                    ? `${selectedProject.deliveryDelayDays} 天`
                    : '-'}
                </Descriptions.Item>
                <Descriptions.Item label="项目经理">{selectedProject.projectManagerName || '-'}</Descriptions.Item>
                <Descriptions.Item label="问题数量" span={2}>
                  <Tag color={selectedProject.problemCount > 0 ? 'orange' : 'default'}>
                    {selectedProject.problemCount} 个
                  </Tag>
                </Descriptions.Item>
              </Descriptions>
            </TabPane>
            <TabPane tab={`问题列表 (${selectedProject.problems.length})`} key="problems">
              <Table
                columns={[
                  { title: '序号', dataIndex: 'problemSequence', key: 'sequence', width: 80 },
                  { title: '分类', dataIndex: 'problemCategory', key: 'category', width: 100 },
                  { title: '问题描述', dataIndex: 'problemDescription', key: 'description', ellipsis: true },
                  { title: '优先级', dataIndex: 'priority', key: 'priority', width: 80 },
                  { title: '状态', dataIndex: 'status', key: 'status', width: 100 },
                  { title: '主负责人', dataIndex: 'primaryResponsible', key: 'responsible', width: 120 },
                  {
                    title: '操作',
                    key: 'actions',
                    width: 100,
                    render: (_: any, record: FieldProblemDto) => (
                      <Button
                        size="small"
                        onClick={() => navigate(`/projects/problems/${record.problemId}`)}
                      >
                        查看详情
                      </Button>
                    ),
                  },
                ]}
                dataSource={selectedProject.problems}
                rowKey="problemId"
                pagination={false}
                size="small"
              />
            </TabPane>
          </Tabs>
        ) : null}
      </Modal>
    </div>
  );
}


