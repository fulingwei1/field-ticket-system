import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Steps,
  Button,
  Upload,
  Table,
  message,
  Card,
  Space,
  Tag,
  Result,
  Statistic,
  Row,
  Col,
  Modal,
  Form,
  Input,
  Select,
  Checkbox,
  Alert,
  Tooltip,
} from 'antd';
import {
  UploadOutlined,
  DownloadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  InfoCircleOutlined,
  StopOutlined,
  EditOutlined,
  SyncOutlined,
} from '@ant-design/icons';
import type { UploadProps } from 'antd';
import employeeUpdateService, {
  EmployeeUpdateItem,
  EmployeeBatchUpdateRequest,
  EmployeeBatchUpdateResult,
  EmployeeUpdateTemplateRequest,
} from '../../services/employeeUpdateService';
import authService from '../../services/authService';

const { Step } = Steps;
const { Option } = Select;
const { Dragger } = Upload;

/**
 * 员工批量更新页面
 */
const EmployeeUpdate: React.FC = () => {
  const navigate = useNavigate();
  const [current, setCurrent] = useState(0);
  const [updates, setUpdates] = useState<EmployeeUpdateItem[]>([]);
  const [updateResult, setUpdateResult] = useState<EmployeeBatchUpdateResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [templateModalVisible, setTemplateModalVisible] = useState(false);
  const [templateForm] = Form.useForm<EmployeeUpdateTemplateRequest>();

  // 权限检查：只有管理员可以访问
  useEffect(() => {
    if (!authService.isAdmin()) {
      message.error('只有管理员才能访问员工批量更新功能');
      navigate('/');
    }
  }, [navigate]);

  // 如果不是管理员，显示无权限提示
  if (!authService.isAdmin()) {
    return (
      <div style={{ padding: 24 }}>
        <Result
          status="403"
          title="访问受限"
          subTitle="抱歉，您没有权限访问此页面。只有管理员才能使用员工批量更新功能。"
          icon={<StopOutlined />}
          extra={
            <Button type="primary" onClick={() => navigate('/')}>
              返回首页
            </Button>
          }
        />
      </div>
    );
  }

  const handleDownloadTemplate = () => {
    setTemplateModalVisible(true);
  };

  const handleTemplateDownload = async () => {
    try {
      const values = await templateForm.validateFields();
      setLoading(true);

      const blob = await employeeUpdateService.generateTemplate(values);
      const fileName = `员工更新模板_${new Date().toISOString().slice(0, 10)}.xlsx`;

      employeeUpdateService.downloadFile(blob, fileName);

      message.success('模板下载成功');
      setTemplateModalVisible(false);
    } catch (error: any) {
      console.error('Failed to download template:', error);
      message.error(error.message || '模板下载失败');
    } finally {
      setLoading(false);
    }
  };

  const uploadProps: UploadProps = {
    name: 'file',
    accept: '.xlsx,.xls',
    multiple: false,
    maxCount: 1,
    beforeUpload: async (file) => {
      const isExcel =
        file.type === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ||
        file.type === 'application/vnd.ms-excel';

      if (!isExcel) {
        message.error('只支持Excel文件格式（.xlsx, .xls）');
        return Upload.LIST_IGNORE;
      }

      const isLt10M = file.size / 1024 / 1024 < 10;
      if (!isLt10M) {
        message.error('文件大小不能超过10MB');
        return Upload.LIST_IGNORE;
      }

      try {
        setLoading(true);
        const parsedUpdates = await employeeUpdateService.parseFromExcel(file);

        if (parsedUpdates.length === 0) {
          message.warning('文件中没有找到需要更新的员工数据');
          return Upload.LIST_IGNORE;
        }

        setUpdates(parsedUpdates);
        setCurrent(1);
        message.success(`成功解析 ${parsedUpdates.length} 条更新记录`);
      } catch (error: any) {
        console.error('Failed to parse file:', error);
        message.error(error.message || '文件解析失败，请检查文件格式');
      } finally {
        setLoading(false);
      }

      return Upload.LIST_IGNORE;
    },
  };

  const handleUpdate = async () => {
    try {
      setLoading(true);

      const request: EmployeeBatchUpdateRequest = {
        updates,
        overwriteExisting: true,
      };

      const result = await employeeUpdateService.batchUpdate(request);
      setUpdateResult(result);
      setCurrent(2);

      if (result.failedCount === 0) {
        message.success(`批量更新完成！成功更新 ${result.successCount} 条记录`);
      } else {
        message.warning(
          `更新完成，但有 ${result.failedCount} 条记录失败，请查看详细结果`
        );
      }
    } catch (error: any) {
      console.error('Failed to batch update:', error);
      message.error(error.message || '批量更新失败');
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    setCurrent(0);
    setUpdates([]);
    setUpdateResult(null);
  };

  const updateColumns = [
    {
      title: '用户名',
      dataIndex: 'identifier',
      key: 'identifier',
      width: 120,
      fixed: 'left' as const,
    },
    {
      title: '部门',
      dataIndex: 'deptName',
      key: 'deptName',
      width: 120,
      render: (value: string | undefined) =>
        value ? <Tag color="blue">{value}</Tag> : <span style={{ color: '#999' }}>-</span>,
    },
    {
      title: '上级',
      dataIndex: 'supervisorName',
      key: 'supervisorName',
      width: 100,
      render: (value: string | undefined) =>
        value ? value : <span style={{ color: '#999' }}>-</span>,
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
      width: 120,
      render: (value: string | undefined) => {
        if (!value) return <span style={{ color: '#999' }}>-</span>;
        const roleColors: Record<string, string> = {
          Admin: 'red',
          SeniorEngineer: 'orange',
          FieldEngineer: 'blue',
          CS: 'green',
        };
        return <Tag color={roleColors[value] || 'default'}>{value}</Tag>;
      },
    },
    {
      title: '手机号',
      dataIndex: 'phoneNumber',
      key: 'phoneNumber',
      width: 130,
      render: (value: string | undefined) =>
        value ? value : <span style={{ color: '#999' }}>-</span>,
    },
    {
      title: '邮箱',
      dataIndex: 'email',
      key: 'email',
      width: 180,
      render: (value: string | undefined) =>
        value ? value : <span style={{ color: '#999' }}>-</span>,
    },
  ];

  const resultColumns = [
    {
      title: '状态',
      dataIndex: 'success',
      key: 'success',
      width: 80,
      fixed: 'left' as const,
      render: (success: boolean) =>
        success ? (
          <CheckCircleOutlined style={{ fontSize: 20, color: '#52c41a' }} />
        ) : (
          <CloseCircleOutlined style={{ fontSize: 20, color: '#ff4d4f' }} />
        ),
    },
    {
      title: '用户名',
      dataIndex: 'identifier',
      key: 'identifier',
      width: 120,
      fixed: 'left' as const,
    },
    {
      title: '姓名',
      dataIndex: 'name',
      key: 'name',
      width: 100,
    },
    {
      title: '消息',
      dataIndex: 'message',
      key: 'message',
      width: 150,
      render: (message: string, record: any) => (
        <span style={{ color: record.success ? '#52c41a' : '#ff4d4f' }}>{message}</span>
      ),
    },
    {
      title: '变更详情',
      key: 'changes',
      width: 250,
      render: (_: any, record: any) => {
        const changes = Object.keys(record.beforeValues || {})
          .filter(
            (key) =>
              record.beforeValues[key] !== record.afterValues[key]
          )
          .map((key) => (
            <div key={key} style={{ fontSize: 12 }}>
              <Tag color="orange">{key}</Tag>
              <span style={{ color: '#999' }}>{record.beforeValues[key] || '空'}</span>
              {' → '}
              <span style={{ color: '#1890ff', fontWeight: 500 }}>
                {record.afterValues[key] || '空'}
              </span>
            </div>
          ));

        return changes.length > 0 ? (
          <Space direction="vertical" size={4}>
            {changes}
          </Space>
        ) : (
          <span style={{ color: '#999' }}>无变更</span>
        );
      },
    },
  ];

  const steps = [
    {
      title: '上传文件',
      icon: <UploadOutlined />,
    },
    {
      title: '确认更新',
      icon: <EditOutlined />,
    },
    {
      title: '更新完成',
      icon: <CheckCircleOutlined />,
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Card
        title={
          <Space>
            <SyncOutlined />
            <span>员工批量更新</span>
          </Space>
        }
        extra={
          <Button onClick={() => navigate('/users')}>返回用户列表</Button>
        }
      >
        <Steps current={current} items={steps} style={{ marginBottom: 32 }} />

        {/* 步骤1：上传文件 */}
        {current === 0 && (
          <div>
            <Alert
              message="使用说明"
              description={
                <ol style={{ paddingLeft: 20, margin: 0 }}>
                  <li>点击"下载更新模板"按钮，下载包含现有员工数据的Excel模板</li>
                  <li>在Excel中修改需要更新的字段（部门、上级、角色、手机号、邮箱）</li>
                  <li>注意：用户名列不可修改，用于匹配员工</li>
                  <li>保存Excel文件后，上传到此处</li>
                  <li>系统会自动解析并批量更新员工信息</li>
                </ol>
              }
              type="info"
              showIcon
              style={{ marginBottom: 24 }}
            />

            <Space direction="vertical" size="large" style={{ width: '100%' }}>
              <Button
                type="primary"
                icon={<DownloadOutlined />}
                onClick={handleDownloadTemplate}
                size="large"
              >
                下载更新模板
              </Button>

              <Dragger {...uploadProps} style={{ padding: '40px 20px' }}>
                <p className="ant-upload-drag-icon">
                  <UploadOutlined style={{ fontSize: 48, color: '#1890ff' }} />
                </p>
                <p className="ant-upload-text">点击或拖拽Excel文件到此区域上传</p>
                <p className="ant-upload-hint">
                  支持 .xlsx 和 .xls 格式，文件大小不超过10MB
                </p>
              </Dragger>
            </Space>
          </div>
        )}

        {/* 步骤2：确认更新 */}
        {current === 1 && (
          <div>
            <Alert
              message={`准备更新 ${updates.length} 条员工记录`}
              description="请仔细核对以下更新内容，确认无误后点击"开始更新"按钮"
              type="warning"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <Table
              columns={updateColumns}
              dataSource={updates}
              rowKey="identifier"
              scroll={{ x: 800 }}
              pagination={{ pageSize: 10 }}
              style={{ marginBottom: 16 }}
            />

            <Space>
              <Button onClick={handleReset}>返回上一步</Button>
              <Button type="primary" onClick={handleUpdate} loading={loading}>
                开始更新
              </Button>
            </Space>
          </div>
        )}

        {/* 步骤3：更新完成 */}
        {current === 2 && updateResult && (
          <div>
            <Row gutter={16} style={{ marginBottom: 24 }}>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="总处理数"
                    value={updateResult.totalCount}
                    prefix={<InfoCircleOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="成功"
                    value={updateResult.successCount}
                    valueStyle={{ color: '#3f8600' }}
                    prefix={<CheckCircleOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="失败"
                    value={updateResult.failedCount}
                    valueStyle={{ color: '#cf1322' }}
                    prefix={<CloseCircleOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="跳过"
                    value={updateResult.skippedCount}
                    valueStyle={{ color: '#999' }}
                    prefix={<InfoCircleOutlined />}
                  />
                </Card>
              </Col>
            </Row>

            {updateResult.errorMessages.length > 0 && (
              <Alert
                message="错误信息汇总"
                description={
                  <ul style={{ margin: 0, paddingLeft: 20 }}>
                    {updateResult.errorMessages.map((msg, idx) => (
                      <li key={idx}>{msg}</li>
                    ))}
                  </ul>
                }
                type="error"
                showIcon
                style={{ marginBottom: 16 }}
              />
            )}

            <Table
              columns={resultColumns}
              dataSource={updateResult.details}
              rowKey="identifier"
              scroll={{ x: 900 }}
              pagination={{ pageSize: 10 }}
              style={{ marginBottom: 16 }}
            />

            <Space>
              <Button onClick={handleReset}>重新更新</Button>
              <Button type="primary" onClick={() => navigate('/users')}>
                返回用户列表
              </Button>
            </Space>
          </div>
        )}
      </Card>

      {/* 下载模板对话框 */}
      <Modal
        title="下载更新模板"
        open={templateModalVisible}
        onOk={handleTemplateDownload}
        onCancel={() => setTemplateModalVisible(false)}
        confirmLoading={loading}
        width={500}
        okText="下载"
        cancelText="取消"
      >
        <Alert
          message="模板将包含现有员工数据"
          description="您可以在Excel中直接修改需要更新的字段，然后重新导入"
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
        />

        <Form form={templateForm} layout="vertical">
          <Form.Item label="部门筛选" name="deptName">
            <Input placeholder="留空表示导出所有部门" allowClear />
          </Form.Item>

          <Form.Item label="角色筛选" name="role">
            <Select placeholder="留空表示导出所有角色" allowClear>
              <Option value="Admin">管理员</Option>
              <Option value="SeniorEngineer">高级工程师</Option>
              <Option value="FieldEngineer">现场工程师</Option>
              <Option value="CS">客服</Option>
            </Select>
          </Form.Item>

          <Form.Item name="onlyInactivated" valuePropName="checked" initialValue={false}>
            <Checkbox>只导出未开通账户</Checkbox>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default EmployeeUpdate;
