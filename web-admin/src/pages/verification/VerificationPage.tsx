"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Form,
  InputNumber,
  Input,
  Button,
  Space,
  message,
  Spin,
  Descriptions,
  Tag,
  Checkbox,
  Alert,
  Table,
  Upload,
} from 'antd';
import { ArrowLeftOutlined, CheckOutlined, UploadOutlined } from '@ant-design/icons';
import { verificationService, SubmitVerificationRequest } from '../../services/verificationService';
import { ticketService, TicketDto } from '../../services/ticketService';
import { solutionService, SolutionDto } from '../../services/solutionService';
import { attachmentService } from '../../services/attachmentService';

const { TextArea } = Input;

export default function VerificationPage() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [ticket, setTicket] = useState<TicketDto | null>(null);
  const [solutions, setSolutions] = useState<SolutionDto[]>([]);
  const [selectedSolution, setSelectedSolution] = useState<SolutionDto | null>(null);
  const [checklistItems, setChecklistItems] = useState<any[]>([]);
  const [evidenceAttachmentIds, setEvidenceAttachmentIds] = useState<string[]>([]);
  const [loadingData, setLoadingData] = useState(true);

  useEffect(() => {
    loadData();
  }, [ticketId]);

  const loadData = async () => {
    if (!ticketId) return;
    
    try {
      setLoadingData(true);
      
      // 加载工单信息
      const ticketData = await ticketService.getTicket(ticketId);
      setTicket(ticketData);

      // 验证工单状态
      if (ticketData.status !== 'SolutionIssued') {
        message.warning('只能对 SolutionIssued 状态的工单进行验证');
        navigate('/tickets');
        return;
      }

      // 加载解决方案列表
      const solutionsData = await solutionService.getTicketSolutions(ticketId);
      setSolutions(solutionsData);
      
      // 如果有已发布的解决方案，默认选择第一个
      const publishedSolution = solutionsData.find(s => s.status === 'Published');
      if (publishedSolution) {
        setSelectedSolution(publishedSolution);
        loadChecklist(publishedSolution);
      }
    } catch (error: any) {
      message.error(error.message || '加载数据失败');
      navigate('/tickets');
    } finally {
      setLoadingData(false);
    }
  };

  const loadChecklist = (solution: SolutionDto) => {
    // 解析验证清单
    const checklist = solution.verificationChecklistJson;
    if (checklist && typeof checklist === 'object') {
      const items: any[] = [];
      
      // 处理前置检查
      if (checklist.precheck && Array.isArray(checklist.precheck)) {
        checklist.precheck.forEach((item: string, index: number) => {
          items.push({
            id: `precheck-${index}`,
            text: item,
            type: 'precheck',
            required: true,
          });
        });
      }
      
      // 处理步骤
      if (checklist.steps && Array.isArray(checklist.steps)) {
        checklist.steps.forEach((step: any) => {
          items.push({
            id: step.id || `step-${items.length}`,
            text: step.text || '',
            type: step.type || 'action',
            required: step.required !== false,
            acceptanceCriteria: step.acceptance_criteria,
          });
        });
      }
      
      setChecklistItems(items);
      
      // 初始化表单中的清单结果
      const checklistResult: Record<string, any> = {};
      items.forEach(item => {
        checklistResult[item.id] = {
          completed: false,
          required: item.required,
        };
      });
      form.setFieldsValue({ checklistResultJson: checklistResult });
    }
  };

  const handleSolutionChange = (solutionId: string) => {
    const solution = solutions.find(s => s.solutionId === solutionId);
    if (solution) {
      setSelectedSolution(solution);
      loadChecklist(solution);
    }
  };

  const handleSubmit = async (values: any) => {
    if (!ticketId) return;
    
    try {
      setLoading(true);
      
      // 计算失败次数
      const failCount = values.runCount - values.passCount;
      
      // 构建验证清单结果
      const checklistResult = values.checklistResultJson || {};
      
      const request: SubmitVerificationRequest = {
        solutionId: selectedSolution?.solutionId,
        runCount: values.runCount,
        passCount: values.passCount,
        failCount: failCount,
        checklistResultJson: checklistResult,
        evidenceAttachmentIds: evidenceAttachmentIds,
        note: values.note,
      };

      await verificationService.submitVerification(ticketId, request);
      message.success('验证结果已提交');
      navigate(`/tickets/${ticketId}`);
    } catch (error: any) {
      message.error(error.message || '提交验证结果失败');
    } finally {
      setLoading(false);
    }
  };

  const handleAttachmentUpload = async (file: File) => {
    if (!ticketId) return;
    
    try {
      const attachment = await attachmentService.uploadAttachment(ticketId, file, 'file');
      setEvidenceAttachmentIds([...evidenceAttachmentIds, attachment.attachmentId]);
      message.success('附件上传成功');
      return false; // 阻止默认上传行为
    } catch (error: any) {
      message.error(error.message || '附件上传失败');
      return false;
    }
  };

  if (loadingData) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!ticket) {
    return null;
  }

  const checklistColumns = [
    {
      title: '步骤',
      dataIndex: 'text',
      key: 'text',
      render: (text: string, record: any) => (
        <div>
          <div>{text}</div>
          {record.acceptanceCriteria && (
            <div style={{ color: '#666', fontSize: '12px', marginTop: '4px' }}>
              验收标准：{record.acceptanceCriteria}
            </div>
          )}
        </div>
      ),
    },
    {
      title: '类型',
      dataIndex: 'type',
      key: 'type',
      render: (type: string) => {
        const typeMap: Record<string, { label: string; color: string }> = {
          precheck: { label: '前置检查', color: 'blue' },
          action: { label: '操作', color: 'green' },
          test: { label: '测试', color: 'orange' },
        };
        const info = typeMap[type] || { label: type, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '必填',
      dataIndex: 'required',
      key: 'required',
      render: (required: boolean) => (
        <Tag color={required ? 'red' : 'default'}>
          {required ? '必填' : '可选'}
        </Tag>
      ),
    },
    {
      title: '完成状态',
      key: 'completed',
      render: (_: any, record: any) => (
        <Form.Item
          name={['checklistResultJson', record.id, 'completed']}
          valuePropName="checked"
          style={{ marginBottom: 0 }}
        >
          <Checkbox />
        </Form.Item>
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
        <Card title="工单信息">
          <Descriptions column={2} bordered>
            <Descriptions.Item label="工单编号">{ticket.ticketNo || '未生成'}</Descriptions.Item>
            <Descriptions.Item label="状态">
              <Tag color={ticket.status === 'SolutionIssued' ? 'blue' : 'default'}>
                {ticket.status}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="问题域">{ticket.domain}</Descriptions.Item>
            <Descriptions.Item label="步骤代码">{ticket.stepCode}</Descriptions.Item>
            <Descriptions.Item label="症状标题" span={2}>
              {ticket.symptomTitle}
            </Descriptions.Item>
          </Descriptions>
        </Card>

        {/* 解决方案选择 */}
        {solutions.length > 0 && (
          <Card title="选择解决方案">
            <Form.Item
              label="解决方案"
              name="solutionId"
              rules={[{ required: true, message: '请选择解决方案' }]}
            >
              <select
                className="ant-input"
                style={{ width: '100%', padding: '4px 11px' }}
                onChange={(e) => handleSolutionChange(e.target.value)}
                value={selectedSolution?.solutionId || ''}
              >
                <option value="">请选择解决方案</option>
                {solutions
                  .filter(s => s.status === 'Published')
                  .map(s => (
                    <option key={s.solutionId} value={s.solutionId}>
                      {s.solutionCode || '未发布'} - {s.title}
                    </option>
                  ))}
              </select>
            </Form.Item>
          </Card>
        )}

        {/* 验证清单 */}
        {selectedSolution && checklistItems.length > 0 && (
          <Card title="验证清单">
            <Form.Item name="checklistResultJson">
              <Table
                columns={checklistColumns}
                dataSource={checklistItems}
                rowKey="id"
                pagination={false}
                size="small"
              />
            </Form.Item>
          </Card>
        )}

        {/* 验证表单 */}
        <Card title="提交验证结果">
          <Form
            form={form}
            layout="vertical"
            onFinish={handleSubmit}
            initialValues={{
              runCount: 0,
              passCount: 0,
              failCount: 0,
            }}
          >
            <Form.Item
              name="runCount"
              label="验证次数"
              rules={[{ required: true, message: '请输入验证次数' }, { type: 'number', min: 1, message: '验证次数必须大于0' }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item
              name="passCount"
              label="通过次数"
              rules={[{ required: true, message: '请输入通过次数' }, { type: 'number', min: 0, message: '通过次数不能小于0' }]}
            >
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>

            <Alert
              message="验证结果说明"
              description="系统会根据验证次数、通过次数和验证清单完成情况自动计算验证结果（PASS/FAIL/PARTIAL）"
              type="info"
              style={{ marginBottom: '16px' }}
            />

            <Form.Item
              name="note"
              label="验证备注"
            >
              <TextArea
                rows={4}
                placeholder="填写验证备注（可选）"
              />
            </Form.Item>

            <Form.Item label="验证证据">
              <Upload
                beforeUpload={handleAttachmentUpload}
                showUploadList={false}
              >
                <Button icon={<UploadOutlined />}>上传证据</Button>
              </Upload>
              {evidenceAttachmentIds.length > 0 && (
                <div style={{ marginTop: '8px' }}>
                  <Tag>已上传 {evidenceAttachmentIds.length} 个附件</Tag>
                </div>
              )}
            </Form.Item>

            <Form.Item>
              <Space>
                <Button
                  type="primary"
                  htmlType="submit"
                  icon={<CheckOutlined />}
                  loading={loading}
                >
                  提交验证结果
                </Button>
                <Button onClick={() => navigate(`/tickets/${ticketId}`)}>
                  取消
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Card>
      </Space>
    </div>
  );
}


