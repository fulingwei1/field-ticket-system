import React, { useState } from 'react';
import {
  Card,
  Upload,
  Button,
  Table,
  Alert,
  Steps,
  message,
  Space,
  Typography,
  Tag,
  Modal,
  Select,
  Checkbox,
  Divider,
  Row,
  Col,
  Statistic,
  Result,
  Descriptions,
  Tooltip,
} from 'antd';
import {
  UploadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  FileExcelOutlined,
  DownloadOutlined,
  UserAddOutlined,
  InfoCircleOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import type { UploadFile, UploadProps } from 'antd/es/upload';
import {
  employeeImportService,
  type EmployeeImportResult,
  type EmployeeImportSuccess,
  type EmployeeImportError,
} from '../../services/employeeImportService';

const { Title, Text, Paragraph } = Typography;
const { Step } = Steps;
const { Option } = Select;

/**
 * 员工批量导入页面
 */
export default function EmployeeImport() {
  const [currentStep, setCurrentStep] = useState(0);
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [importResult, setImportResult] = useState<EmployeeImportResult | null>(null);
  const [defaultRole, setDefaultRole] = useState('FieldEngineer');
  const [overwriteExisting, setOverwriteExisting] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeImportSuccess | null>(null);
  const [detailModalVisible, setDetailModalVisible] = useState(false);

  // 下载Excel模板
  const handleDownloadTemplate = async () => {
    try {
      const blob = await employeeImportService.downloadTemplate();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = '员工导入模板.xlsx';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
      message.success('模板下载成功');
    } catch (error) {
      console.error('Failed to download template:', error);
      message.error('模板下载失败');
    }
  };

  // 上传文件
  const handleUpload: UploadProps['customRequest'] = async (options) => {
    const { file, onSuccess, onError } = options;
    const fileObj = file as File;

    // 验证文件类型
    if (!fileObj.name.endsWith('.xlsx') && !fileObj.name.endsWith('.xls')) {
      const error = new Error('只支持Excel文件格式（.xlsx, .xls）');
      onError?.(error);
      message.error(error.message);
      return;
    }

    setUploading(true);
    try {
      const result = await employeeImportService.importFromExcel(
        fileObj,
        defaultRole,
        overwriteExisting
      );
      setImportResult(result);
      onSuccess?.(result as any);

      if (result.failureCount > 0) {
        message.warning(`导入完成，成功 ${result.successCount} 个，失败 ${result.failureCount} 个`);
      } else {
        message.success(`导入成功 ${result.successCount} 个员工账户`);
      }

      setCurrentStep(1);
    } catch (error: any) {
      onError?.(error);
      message.error(error.message || '导入失败');
    } finally {
      setUploading(false);
    }
  };

  const handleFileChange: UploadProps['onChange'] = (info) => {
    let newFileList = [...info.fileList];
    // 只保留最新的一个文件
    newFileList = newFileList.slice(-1);
    setFileList(newFileList);
  };

  // 重新开始
  const handleReset = () => {
    setCurrentStep(0);
    setFileList([]);
    setImportResult(null);
    setDefaultRole('FieldEngineer');
    setOverwriteExisting(false);
  };

  // 查看员工详情
  const handleViewDetail = (employee: EmployeeImportSuccess) => {
    setSelectedEmployee(employee);
    setDetailModalVisible(true);
  };

  // 成功列表列定义
  const successColumns = [
    {
      title: '行号',
      dataIndex: 'rowNumber',
      key: 'rowNumber',
      width: 80,
    },
    {
      title: '姓名',
      dataIndex: 'name',
      key: 'name',
      width: 120,
    },
    {
      title: '登录账号',
      dataIndex: 'username',
      key: 'username',
      width: 120,
      render: (username: string) => (
        <Text style={{ fontFamily: 'monospace', color: '#1890ff' }}>{username}</Text>
      ),
    },
    {
      title: '初始密码',
      dataIndex: 'password',
      key: 'password',
      width: 150,
      render: (password: string) => (
        <Text copyable style={{ fontFamily: 'monospace', color: '#52c41a' }}>
          {password}
        </Text>
      ),
    },
    {
      title: '部门',
      dataIndex: 'deptName',
      key: 'deptName',
      width: 150,
    },
    {
      title: '上级',
      dataIndex: 'supervisorName',
      key: 'supervisorName',
      width: 120,
      render: (supervisorName?: string) => supervisorName || <Text type="secondary">-</Text>,
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
      width: 130,
      render: (role: string) => {
        const roleMap: Record<string, { label: string; color: string }> = {
          FieldEngineer: { label: '现场工程师', color: 'blue' },
          CS: { label: '客服', color: 'green' },
          SeniorEngineer: { label: '高级工程师', color: 'orange' },
          Admin: { label: '管理员', color: 'red' },
        };
        const roleInfo = roleMap[role] || { label: role, color: 'default' };
        return <Tag color={roleInfo.color}>{roleInfo.label}</Tag>;
      },
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: any, record: EmployeeImportSuccess) => (
        <Button
          type="link"
          size="small"
          icon={<EyeOutlined />}
          onClick={() => handleViewDetail(record)}
        >
          详情
        </Button>
      ),
    },
  ];

  // 错误列表列定义
  const errorColumns = [
    {
      title: '行号',
      dataIndex: 'rowNumber',
      key: 'rowNumber',
      width: 80,
    },
    {
      title: '姓名',
      dataIndex: 'name',
      key: 'name',
      width: 120,
      render: (name?: string) => name || <Text type="secondary">-</Text>,
    },
    {
      title: '错误原因',
      dataIndex: 'error',
      key: 'error',
      render: (error: string) => <Text type="danger">{error}</Text>,
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Card>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          {/* 页面标题 */}
          <div>
            <Title level={2}>
              <FileExcelOutlined /> 员工批量导入
            </Title>
            <Paragraph type="secondary">
              通过Excel文件批量导入员工信息，系统将自动创建账户并生成初始密码
            </Paragraph>
          </div>

          {/* 步骤条 */}
          <Steps current={currentStep}>
            <Step title="准备导入" description="下载模板并填写员工信息" />
            <Step title="查看结果" description="查看导入结果和生成的密码" />
          </Steps>

          <Divider />

          {/* 步骤1: 准备导入 */}
          {currentStep === 0 && (
            <Space direction="vertical" size="large" style={{ width: '100%' }}>
              {/* 说明卡片 */}
              <Alert
                message="导入说明"
                description={
                  <div>
                    <p><strong>1. 下载模板：</strong>点击下方按钮下载Excel模板文件</p>
                    <p><strong>2. 填写信息：</strong>在模板中填写员工的基本信息（姓名、身份证号、部门等）</p>
                    <p><strong>3. 上传文件：</strong>选择填写好的Excel文件进行上传</p>
                    <p><strong>4. 密码规则：</strong>系统将自动生成初始密码：<Text code>姓名拼音 + 身份证后4位</Text></p>
                    <p><strong>5. 账户开通：</strong>导入的账户需要管理员在"账户开通"页面手动开通后才能使用</p>
                  </div>
                }
                type="info"
                showIcon
                icon={<InfoCircleOutlined />}
              />

              {/* 下载模板 */}
              <Card title="第1步：下载Excel模板" size="small">
                <Button
                  type="primary"
                  icon={<DownloadOutlined />}
                  onClick={handleDownloadTemplate}
                  size="large"
                >
                  下载员工导入模板
                </Button>
                <Divider type="vertical" />
                <Text type="secondary">
                  模板包含：姓名、身份证号、部门名称、部门号、上级姓名、手机号、邮箱、角色
                </Text>
              </Card>

              {/* 导入设置 */}
              <Card title="第2步：导入设置" size="small">
                <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                  <div>
                    <Text strong>默认角色：</Text>
                    <Divider type="vertical" />
                    <Select
                      value={defaultRole}
                      onChange={setDefaultRole}
                      style={{ width: 200 }}
                    >
                      <Option value="FieldEngineer">现场工程师</Option>
                      <Option value="CS">客服</Option>
                      <Option value="SeniorEngineer">高级工程师</Option>
                      <Option value="Admin">管理员</Option>
                    </Select>
                    <Divider type="vertical" />
                    <Tooltip title="如果Excel中未指定角色，将使用此默认角色">
                      <InfoCircleOutlined style={{ color: '#999' }} />
                    </Tooltip>
                  </div>
                  <div>
                    <Checkbox
                      checked={overwriteExisting}
                      onChange={(e) => setOverwriteExisting(e.target.checked)}
                    >
                      覆盖已存在的用户
                    </Checkbox>
                    <Divider type="vertical" />
                    <Text type="secondary">
                      如果勾选，将更新已存在用户的信息；否则跳过已存在的用户
                    </Text>
                  </div>
                </Space>
              </Card>

              {/* 上传文件 */}
              <Card title="第3步：上传Excel文件" size="small">
                <Upload
                  fileList={fileList}
                  customRequest={handleUpload}
                  onChange={handleFileChange}
                  accept=".xlsx,.xls"
                  maxCount={1}
                >
                  <Button
                    type="primary"
                    icon={<UploadOutlined />}
                    loading={uploading}
                    size="large"
                  >
                    {uploading ? '导入中...' : '选择并上传Excel文件'}
                  </Button>
                </Upload>
                <Divider type="vertical" />
                <Text type="secondary">支持 .xlsx 和 .xls 格式</Text>
              </Card>
            </Space>
          )}

          {/* 步骤2: 查看结果 */}
          {currentStep === 1 && importResult && (
            <Space direction="vertical" size="large" style={{ width: '100%' }}>
              {/* 统计信息 */}
              <Row gutter={16}>
                <Col span={6}>
                  <Card>
                    <Statistic
                      title="总计"
                      value={importResult.totalCount}
                      prefix={<UserAddOutlined />}
                    />
                  </Card>
                </Col>
                <Col span={6}>
                  <Card>
                    <Statistic
                      title="成功"
                      value={importResult.successCount}
                      valueStyle={{ color: '#52c41a' }}
                      prefix={<CheckCircleOutlined />}
                    />
                  </Card>
                </Col>
                <Col span={6}>
                  <Card>
                    <Statistic
                      title="失败"
                      value={importResult.failureCount}
                      valueStyle={{ color: '#ff4d4f' }}
                      prefix={<CloseCircleOutlined />}
                    />
                  </Card>
                </Col>
                <Col span={6}>
                  <Card>
                    <Statistic
                      title="跳过"
                      value={importResult.skippedCount}
                      valueStyle={{ color: '#faad14' }}
                    />
                  </Card>
                </Col>
              </Row>

              {/* 提示信息 */}
              {importResult.successCount > 0 && (
                <Alert
                  message="重要提示"
                  description={
                    <div>
                      <p>✅ 成功导入 {importResult.successCount} 个员工账户</p>
                      <p>⚠️ 所有账户当前处于<Text strong>未开通</Text>状态</p>
                      <p>📋 请前往<Text strong>"账户开通"</Text>页面审核并开通这些账户</p>
                      <p>🔐 请将下方的登录信息（用户名和密码）告知员工</p>
                    </div>
                  }
                  type="success"
                  showIcon
                />
              )}

              {/* 成功列表 */}
              {importResult.successList.length > 0 && (
                <Card
                  title={
                    <Space>
                      <CheckCircleOutlined style={{ color: '#52c41a' }} />
                      <span>导入成功 ({importResult.successList.length})</span>
                    </Space>
                  }
                  extra={
                    <Text type="secondary">
                      <InfoCircleOutlined /> 请复制密码并告知员工
                    </Text>
                  }
                >
                  <Table
                    columns={successColumns}
                    dataSource={importResult.successList}
                    rowKey="userId"
                    pagination={{ pageSize: 10 }}
                    scroll={{ x: 1000 }}
                  />
                </Card>
              )}

              {/* 错误列表 */}
              {importResult.errorList.length > 0 && (
                <Card
                  title={
                    <Space>
                      <CloseCircleOutlined style={{ color: '#ff4d4f' }} />
                      <span>导入失败 ({importResult.errorList.length})</span>
                    </Space>
                  }
                >
                  <Table
                    columns={errorColumns}
                    dataSource={importResult.errorList}
                    rowKey="rowNumber"
                    pagination={{ pageSize: 10 }}
                  />
                </Card>
              )}

              {/* 操作按钮 */}
              <Space>
                <Button type="primary" size="large" onClick={handleReset}>
                  继续导入
                </Button>
                <Button size="large" onClick={() => window.location.href = '/users/activate'}>
                  前往账户开通页面
                </Button>
              </Space>
            </Space>
          )}
        </Space>
      </Card>

      {/* 员工详情Modal */}
      <Modal
        title="员工详细信息"
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setDetailModalVisible(false)}>
            关闭
          </Button>,
        ]}
        width={600}
      >
        {selectedEmployee && (
          <Descriptions column={1} bordered>
            <Descriptions.Item label="Excel行号">
              {selectedEmployee.rowNumber}
            </Descriptions.Item>
            <Descriptions.Item label="用户ID">
              <Text copyable>{selectedEmployee.userId}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="姓名">
              {selectedEmployee.name}
            </Descriptions.Item>
            <Descriptions.Item label="登录账号">
              <Text copyable style={{ fontFamily: 'monospace', color: '#1890ff' }}>
                {selectedEmployee.username}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="初始密码">
              <Text copyable style={{ fontFamily: 'monospace', color: '#52c41a' }}>
                {selectedEmployee.password}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="部门">
              {selectedEmployee.deptName}
            </Descriptions.Item>
            <Descriptions.Item label="上级">
              {selectedEmployee.supervisorName || <Text type="secondary">-</Text>}
            </Descriptions.Item>
            <Descriptions.Item label="角色">
              <Tag color="blue">{selectedEmployee.role}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="账户状态">
              <Tag color="orange">未开通（需要管理员开通）</Tag>
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </div>
  );
}
