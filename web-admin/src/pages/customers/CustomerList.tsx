"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Input,
  message,
  Modal,
  Descriptions,
  Typography,
} from 'antd';
import {
  SearchOutlined,
  EyeOutlined,
  ReloadOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import { customerService, CustomerDto } from '../../services/customerService';

const { Title } = Typography;

export default function CustomerList() {
  const [loading, setLoading] = useState(false);
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerDto | null>(null);
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  
  // 筛选条件
  const [search, setSearch] = useState('');

  useEffect(() => {
    loadCustomers();
  }, [search]);

  const loadCustomers = async () => {
    setLoading(true);
    try {
      const data = await customerService.getCustomers(search || undefined);
      setCustomers(data);
    } catch (error: any) {
      console.error('[CustomerList] Failed to load customers:', error);
      console.error('[CustomerList] Error details:', {
        message: error.message,
        stack: error.stack,
        name: error.name
      });
      
      let errorMsg = '加载客户列表失败';
      if (error.message) {
        errorMsg = error.message;
      } else if (error instanceof Error) {
        errorMsg = error.message || '未知错误';
      }
      
      message.error(errorMsg);
      
      // 如果是认证错误，提示用户重新登录
      if (errorMsg.includes('认证失败') || errorMsg.includes('401') || errorMsg.includes('Unauthorized')) {
        message.warning('请重新登录后重试', 3);
      } else if (errorMsg.includes('Failed to fetch') || errorMsg.includes('NetworkError')) {
        message.error('网络连接失败，请检查网络或稍后重试', 5);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleViewDetail = (customer: CustomerDto) => {
    setSelectedCustomer(customer);
    setDetailModalVisible(true);
  };

  const columns = [
    {
      title: '客户名称',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 200,
    },
    {
      title: '客户编码',
      dataIndex: 'customerCode',
      key: 'customerCode',
      width: 150,
    },
    {
      title: '行业类型',
      dataIndex: 'industryType',
      key: 'industryType',
      width: 150,
    },
    {
      title: '联系人',
      dataIndex: 'contactPerson',
      key: 'contactPerson',
      width: 120,
    },
    {
      title: '联系电话',
      dataIndex: 'contactPhone',
      key: 'contactPhone',
      width: 150,
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: any, record: CustomerDto) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => handleViewDetail(record)}
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
            <TeamOutlined /> 客户管理
          </Title>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadCustomers}>
              刷新
            </Button>
          </Space>
        </Space>

        {/* 筛选条件 */}
        <Space wrap style={{ marginBottom: 16 }}>
          <Input
            placeholder="搜索客户名称、编码"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ width: 300 }}
            allowClear
            prefix={<SearchOutlined />}
          />
        </Space>

        {/* 客户列表 */}
        <Table
          columns={columns}
          dataSource={customers}
          rowKey="customerId"
          loading={loading}
          pagination={{
            total: customers.length,
            pageSize: 20,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条记录`,
          }}
        />
      </Card>

      {/* 客户详情模态框 */}
      <Modal
        title="客户详情"
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setDetailModalVisible(false)}>
            关闭
          </Button>,
        ]}
        width={600}
      >
        {selectedCustomer && (
          <Descriptions column={1} bordered>
            <Descriptions.Item label="客户名称">
              {selectedCustomer.customerName}
            </Descriptions.Item>
            <Descriptions.Item label="客户编码">
              {selectedCustomer.customerCode || '-'}
            </Descriptions.Item>
            <Descriptions.Item label="行业类型">
              {selectedCustomer.industryType || '-'}
            </Descriptions.Item>
            <Descriptions.Item label="联系人">
              {selectedCustomer.contactPerson || '-'}
            </Descriptions.Item>
            <Descriptions.Item label="联系电话">
              {selectedCustomer.contactPhone || '-'}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
}



