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
  Descriptions,
  Tag,
  Modal,
} from 'antd';
import {
  UploadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  FileExcelOutlined,
} from '@ant-design/icons';
import type { UploadFile, UploadProps } from 'antd/es/upload';
import {
  excelImportService,
  type ExcelImportResult,
  type ProjectImportData,
  type ProblemImportData,
  type ImportError,
  type ValidationResult,
  type ImportExecutionResult,
} from '../../services/excelImportService';

const { Title, Text } = Typography;
const { Step } = Steps;

export default function ExcelImport() {
  const [currentStep, setCurrentStep] = useState(0);
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [parsing, setParsing] = useState(false);
  const [importResult, setImportResult] = useState<ExcelImportResult | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);
  const [executing, setExecuting] = useState(false);
  const [executionResult, setExecutionResult] = useState<ImportExecutionResult | null>(null);
  const [previewVisible, setPreviewVisible] = useState(false);
  const [previewData, setPreviewData] = useState<ProjectImportData | null>(null);

  // 步骤1: 上传文件
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

    setParsing(true);
    try {
      const result = await excelImportService.parseExcel(fileObj);
      setImportResult(result);
      onSuccess?.(result as any);

      if (result.errors.length > 0) {
        message.warning(`解析完成，但有 ${result.errors.length} 个错误`);
      } else {
        message.success('解析成功');
      }

      setCurrentStep(1);
    } catch (error: any) {
      onError?.(error);
      message.error(error.message || '解析失败');
    } finally {
      setParsing(false);
    }
  };

  // 步骤2: 验证数据
  const handleValidate = async () => {
    if (!importResult) return;

    try {
      const result = await excelImportService.validateImportData(importResult);
      setValidationResult(result);

      if (result.isValid) {
        message.success('验证通过');
        setCurrentStep(2);
      } else {
        message.warning(`验证失败，发现 ${result.errors.length} 个错误`);
      }
    } catch (error: any) {
      message.error(error.message || '验证失败');
    }
  };

  // 步骤3: 执行导入
  const handleExecute = async () => {
    if (!importResult) return;

    setExecuting(true);
    try {
      const result = await excelImportService.executeImport(importResult);
      setExecutionResult(result);

      if (result.success) {
        message.success(result.message);
        setCurrentStep(3);
      } else {
        message.error(result.message);
      }
    } catch (error: any) {
      message.error(error.message || '导入失败');
    } finally {
      setExecuting(false);
    }
  };

  // 预览项目详情
  const handlePreview = (project: ProjectImportData) => {
    setPreviewData(project);
    setPreviewVisible(true);
  };

  // 错误列定义
  const errorColumns = [
    {
      title: '行号',
      dataIndex: 'rowNumber',
      key: 'rowNumber',
      width: 80,
    },
    {
      title: '字段',
      dataIndex: 'field',
      key: 'field',
      width: 150,
    },
    {
      title: '错误类型',
      dataIndex: 'errorType',
      key: 'errorType',
      width: 120,
      render: (type: string) => {
        const colorMap: Record<string, string> = {
          Format: 'orange',
          Required: 'red',
          Business: 'purple',
          Relation: 'blue',
        };
        return <Tag color={colorMap[type] || 'default'}>{type}</Tag>;
      },
    },
    {
      title: '错误信息',
      dataIndex: 'message',
      key: 'message',
    },
    {
      title: '建议',
      dataIndex: 'suggestion',
      key: 'suggestion',
    },
  ];

  // 项目列定义
  const projectColumns = [
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
      title: '问题数量',
      key: 'problemCount',
      width: 100,
      render: (_: any, record: ProjectImportData) => record.problems.length,
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: any, record: ProjectImportData) => (
        <Button type="link" onClick={() => handlePreview(record)}>
          查看详情
        </Button>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Card>
        <Title level={2}>
          <FileExcelOutlined /> Excel批量导入
        </Title>

        <Steps current={currentStep} style={{ marginTop: 24, marginBottom: 32 }}>
          <Step title="上传文件" description="选择Excel文件并解析" />
          <Step title="验证数据" description="检查数据完整性和正确性" />
          <Step title="执行导入" description="将数据导入到系统" />
          <Step title="完成" description="导入结果" />
        </Steps>

        {/* 步骤1: 上传文件 */}
        {currentStep === 0 && (
          <Card>
            <Space direction="vertical" style={{ width: '100%' }} size="large">
              <Alert
                message="使用说明"
                description="请上传Excel文件（.xlsx或.xls格式）。系统会自动识别表头并解析数据。支持的列名包括：项目号、项目名称、客户名称、问题描述等。"
                type="info"
                showIcon
              />

              <Upload
                fileList={fileList}
                customRequest={handleUpload}
                onChange={({ fileList }) => setFileList(fileList)}
                accept=".xlsx,.xls"
                maxCount={1}
                disabled={parsing}
              >
                <Button icon={<UploadOutlined />} loading={parsing} size="large">
                  选择Excel文件
                </Button>
              </Upload>

              {importResult && (
                <Alert
                  message={`解析完成：共 ${importResult.totalRows} 行，成功 ${importResult.successRows} 行，错误 ${importResult.errorRows} 行`}
                  type={importResult.errorRows > 0 ? 'warning' : 'success'}
                  showIcon
                />
              )}
            </Space>
          </Card>
        )}

        {/* 步骤2: 验证数据 */}
        {currentStep === 1 && importResult && (
          <Card>
            <Space direction="vertical" style={{ width: '100%' }} size="large">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Title level={4}>数据预览</Title>
                <Button type="primary" onClick={handleValidate}>
                  验证数据
                </Button>
              </div>

              <Table
                columns={projectColumns}
                dataSource={importResult.projects}
                rowKey="projectNo"
                pagination={{ pageSize: 10 }}
                size="small"
              />

              {importResult.errors.length > 0 && (
                <div>
                  <Title level={5}>解析错误</Title>
                  <Table
                    columns={errorColumns}
                    dataSource={importResult.errors}
                    rowKey={(record, index) => `${record.rowNumber}-${index}`}
                    pagination={{ pageSize: 10 }}
                    size="small"
                  />
                </div>
              )}

              {validationResult && (
                <Alert
                  message={validationResult.isValid ? '验证通过' : '验证失败'}
                  description={
                    validationResult.isValid
                      ? '所有数据验证通过，可以执行导入'
                      : `发现 ${validationResult.errors.length} 个验证错误`
                  }
                  type={validationResult.isValid ? 'success' : 'error'}
                  showIcon
                />
              )}

              {validationResult && validationResult.errors.length > 0 && (
                <Table
                  columns={[
                    { title: '字段', dataIndex: 'field', key: 'field' },
                    { title: '错误代码', dataIndex: 'code', key: 'code' },
                    { title: '错误信息', dataIndex: 'message', key: 'message' },
                  ]}
                  dataSource={validationResult.errors}
                  rowKey={(record, index) => `${record.field}-${index}`}
                  pagination={{ pageSize: 10 }}
                  size="small"
                />
              )}
            </Space>
          </Card>
        )}

        {/* 步骤3: 执行导入 */}
        {currentStep === 2 && importResult && (
          <Card>
            <Space direction="vertical" style={{ width: '100%' }} size="large">
              <Alert
                message="准备导入"
                description={`将导入 ${importResult.projects.length} 个项目，${importResult.projects.reduce((sum, p) => sum + p.problems.length, 0)} 个问题`}
                type="info"
                showIcon
              />

              <Button
                type="primary"
                size="large"
                loading={executing}
                onClick={handleExecute}
                block
              >
                执行导入
              </Button>

              {executionResult && (
                <Alert
                  message={executionResult.success ? '导入成功' : '导入失败'}
                  description={executionResult.message}
                  type={executionResult.success ? 'success' : 'error'}
                  showIcon
                />
              )}
            </Space>
          </Card>
        )}

        {/* 步骤4: 完成 */}
        {currentStep === 3 && executionResult && (
          <Card>
            <Space direction="vertical" style={{ width: '100%' }} size="large">
              <Alert
                message="导入完成"
                description={executionResult.message}
                type="success"
                showIcon
              />

              <Descriptions title="导入统计" bordered column={2}>
                <Descriptions.Item label="新项目">
                  {executionResult.projectsCreated}
                </Descriptions.Item>
                <Descriptions.Item label="更新项目">
                  {executionResult.projectsUpdated}
                </Descriptions.Item>
                <Descriptions.Item label="新问题">
                  {executionResult.problemsCreated}
                </Descriptions.Item>
                <Descriptions.Item label="更新问题">
                  {executionResult.problemsUpdated}
                </Descriptions.Item>
              </Descriptions>

              <Button
                type="primary"
                onClick={() => {
                  setCurrentStep(0);
                  setFileList([]);
                  setImportResult(null);
                  setValidationResult(null);
                  setExecutionResult(null);
                }}
              >
                继续导入
              </Button>
            </Space>
          </Card>
        )}
      </Card>

      {/* 项目详情预览 */}
      <Modal
        title="项目详情"
        open={previewVisible}
        onCancel={() => setPreviewVisible(false)}
        footer={null}
        width={800}
      >
        {previewData && (
          <div>
            <Descriptions title="项目信息" bordered column={2}>
              <Descriptions.Item label="项目号">{previewData.projectNo}</Descriptions.Item>
              <Descriptions.Item label="项目名称">{previewData.projectName}</Descriptions.Item>
              <Descriptions.Item label="客户名称">{previewData.customerName}</Descriptions.Item>
              <Descriptions.Item label="设备类型">{previewData.deviceType}</Descriptions.Item>
              <Descriptions.Item label="行业类型">{previewData.industryType}</Descriptions.Item>
              <Descriptions.Item label="项目状态">{previewData.projectStatus}</Descriptions.Item>
              <Descriptions.Item label="销售金额">{previewData.salesAmount}</Descriptions.Item>
              <Descriptions.Item label="下单日期">{previewData.orderDate}</Descriptions.Item>
              <Descriptions.Item label="要求交货日期">{previewData.requiredDeliveryDate}</Descriptions.Item>
              <Descriptions.Item label="实际交货日期">{previewData.actualDeliveryDate}</Descriptions.Item>
            </Descriptions>

            <Title level={5} style={{ marginTop: 24 }}>问题列表</Title>
            <Table
              columns={[
                { title: '序号', dataIndex: 'problemSequence', key: 'problemSequence', width: 80 },
                { title: '分类', dataIndex: 'problemCategory', key: 'problemCategory', width: 100 },
                { title: '描述', dataIndex: 'problemDescription', key: 'problemDescription' },
                { title: '状态', dataIndex: 'status', key: 'status', width: 100 },
                { title: '满意度', dataIndex: 'satisfactionScore', key: 'satisfactionScore', width: 100 },
              ]}
              dataSource={previewData.problems}
              rowKey="problemSequence"
              pagination={false}
              size="small"
            />
          </div>
        )}
      </Modal>
    </div>
  );
}






