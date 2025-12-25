import React, { useState, useEffect } from 'react';
import {
  Form,
  Input,
  Select,
  Button,
  Card,
  Steps,
  message,
  Checkbox,
  InputNumber,
  Space,
  Divider,
  Alert,
  Spin,
} from 'antd';
import { SaveOutlined, SendOutlined, WarningOutlined, DiffOutlined } from '@ant-design/icons';
import { ticketService, CreateTicketRequest, ValidationError } from '../../services/ticketService';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { MissingInfoQuestionnaire } from '../../components/tickets/MissingInfoQuestionnaire';
import { ticketTemplateService } from '../../services/ticketTemplateService';
import { deviceConfigSnapshotService, ConfigDifference } from '../../services/deviceConfigSnapshotService';

const { TextArea } = Input;
const { Option } = Select;
const { Step } = Steps;

// 问题域选项（移到组件外部，供所有组件使用）
const domainOptions = [
  { value: 'A', label: 'A - 机械/动作' },
  { value: 'B', label: 'B - 电气/IO' },
  { value: 'C', label: 'C - PLC/程序流程' },
  { value: 'D', label: 'D - 测试/判定' },
  { value: 'E', label: 'E - 系统/偶发/环境' },
];

const CreateTicket: React.FC = () => {
  const [form] = Form.useForm();
  const [currentStep, setCurrentStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [ticketId, setTicketId] = useState<string | null>(null);
  const [domain, setDomain] = useState<string>('');
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const templateId = searchParams.get('templateId');

  // 从模板加载数据
  useEffect(() => {
    if (templateId) {
      loadTemplate(templateId);
    }
  }, [templateId]);

  const loadTemplate = async (id: string) => {
    try {
      setLoading(true);
      const createRequest = await ticketTemplateService.applyTemplate(id);
      
      // 填充表单
      form.setFieldsValue({
        domain: createRequest.domain,
        stepCode: createRequest.stepCode,
        stepName: createRequest.stepName,
        symptomTitle: createRequest.symptomTitle,
        symptomDetail: createRequest.symptomDetail,
        reproRate: createRequest.reproRate,
        rebootRecovers: createRequest.rebootRecovers,
        envRelated: createRequest.envRelated,
        swVersion: createRequest.swVersion,
        plcVersion: createRequest.plcVersion,
        paramVersion: createRequest.paramVersion,
        factsJson: createRequest.factsJson,
        actionsTaken: createRequest.actionsTaken,
        actionsTakenNote: createRequest.actionsTakenNote,
        alarmCode: createRequest.alarmCode,
      });

      if (createRequest.domain) {
        setDomain(createRequest.domain);
      }

      message.success('模板已加载，请检查并完善信息');
    } catch (error: any) {
      message.error(error.message || '加载模板失败');
    } finally {
      setLoading(false);
    }
  };

  const steps = [
    {
      title: '基本信息',
      content: <BasicInfoForm form={form} domain={domain} setDomain={setDomain} />,
    },
    {
      title: '事实表',
      content: <FactsForm form={form} domain={domain} />,
    },
    {
      title: '版本信息',
      content: <VersionForm form={form} />,
    },
    {
      title: '确认提交',
      content: <PreviewForm form={form} ticketId={ticketId} />,
    },
  ];

  const handleSaveDraft = async () => {
    try {
      const values = await form.validateFields();
      const request = buildCreateRequest(values);

      setLoading(true);
      const ticket = await ticketService.createDraft(request);
      setTicketId(ticket.ticketId);
      message.success('草稿保存成功');
    } catch (error: any) {
      console.error('Failed to save draft:', error);
      message.error(error.message || '保存草稿失败');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      if (!values.confirmedAsFact) {
        message.error('请确认以上为现场事实，不包含个人判断');
        return;
      }

      // 如果还没有创建草稿，先创建
      if (!ticketId) {
        const request = buildCreateRequest(values);
        setLoading(true);
        const ticket = await ticketService.createDraft(request);
        setTicketId(ticket.ticketId);
        setLoading(false);
        message.info('工单草稿已创建，请检查缺失信息后提交');
        return; // 创建草稿后，让用户先补全信息
      }

      // 提交工单
      setLoading(true);
      const result = await ticketService.submitTicket(ticketId);

      if (!result.success && result.errors) {
        // 显示校验错误
        const errorMessages = result.errors.map((e: ValidationError) => e.message).join('\n');
        message.error(`提交失败：\n${errorMessages}`);
        return;
      }

      message.success(`工单提交成功！工单编号：${result.ticketNo}`);
      navigate(`/tickets/${ticketId}`);
    } catch (error: any) {
      console.error('Failed to submit ticket:', error);
      message.error(error.message || '提交工单失败');
    } finally {
      setLoading(false);
    }
  };

  const buildCreateRequest = (values: any): CreateTicketRequest => {
    // 构建 factsJson
    const factsJson: Record<string, any> = {
      mechanical: {
        action_completed: values.mechanical_action_completed || 'NA',
        position_reliable: values.mechanical_position_reliable || 'NA',
        jam_or_noise: values.mechanical_jam_or_noise || 'NA',
        manual_help_recovers: values.mechanical_manual_help_recovers || 'NA',
      },
      electrical: {
        sensor_physical_ok: values.electrical_sensor_physical_ok || 'NA',
        plc_io_changes: values.electrical_plc_io_changes || 'NA',
        similar_points_ok: values.electrical_similar_points_ok || 'NA',
      },
      plc: {
        stuck_step_code: values.plc_stuck_step_code || '',
        stuck_fixed: values.plc_stuck_fixed || 'NA',
        manual_single_step_pass: values.plc_manual_single_step_pass || 'NA',
        alarm_code: values.plc_alarm_code || '',
      },
      test: {
        same_unit_repeat_consistent: values.test_same_unit_repeat_consistent || 'NA',
        swap_unit_recovers: values.test_swap_unit_recovers || 'NA',
        near_limits: values.test_near_limits || 'NA',
      },
      environment: {
        repro_rate: values.repro_rate || 0,
        reboot_recovers: values.reboot_recovers || false,
        env_related: values.env_related || false,
      },
    };

    return {
      deviceId: values.deviceId || '', // TODO: 从设备选择获取
      stationId: values.stationId,
      domain: values.domain,
      stepCode: values.stepCode,
      stepName: values.stepName,
      symptomTitle: values.symptomTitle,
      symptomDetail: values.symptomDetail,
      reproRate: values.repro_rate,
      rebootRecovers: values.reboot_recovers,
      envRelated: values.env_related,
      swVersion: values.swVersion,
      plcVersion: values.plcVersion,
      paramVersion: values.paramVersion,
      factsJson,
      actionsTaken: values.actionsTaken || [],
      actionsTakenNote: values.actionsTakenNote,
      alarmCode: values.alarmCode,
      confirmedAsFact: values.confirmedAsFact || false,
    };
  };

  return (
    <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      <Card>
        <h2>创建工单</h2>
        <Steps current={currentStep} style={{ marginBottom: '32px' }}>
          {steps.map((step, index) => (
            <Step key={index} title={step.title} />
          ))}
        </Steps>

        <Form
          form={form}
          layout="vertical"
          initialValues={{
            domain: '',
            repro_rate: 100,
            reboot_recovers: false,
            env_related: false,
            confirmedAsFact: false,
          }}
        >
          {steps[currentStep].content}

          <Divider />

          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <div>
              {currentStep > 0 && (
                <Button onClick={() => setCurrentStep(currentStep - 1)}>上一步</Button>
              )}
            </div>
            <div>
              <Button
                icon={<SaveOutlined />}
                onClick={handleSaveDraft}
                loading={loading}
                style={{ marginRight: '8px' }}
              >
                保存草稿
              </Button>
              {currentStep < steps.length - 1 ? (
                <Button type="primary" onClick={() => setCurrentStep(currentStep + 1)}>
                  下一步
                </Button>
              ) : (
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  onClick={handleSubmit}
                  loading={loading}
                >
                  提交工单
                </Button>
              )}
            </div>
          </Space>
        </Form>
      </Card>
    </div>
  );
};

// 基本信息表单
const BasicInfoForm: React.FC<{ form: any; domain: string; setDomain: (d: string) => void }> = ({
  form,
  domain,
  setDomain,
}) => {
  return (
    <>
      <Form.Item
        name="deviceId"
        label="设备ID"
        rules={[{ required: true, message: '请选择设备' }]}
      >
        <Input placeholder="请输入或选择设备" />
      </Form.Item>

      <Form.Item
        name="domain"
        label="问题域"
        rules={[{ required: true, message: '请选择问题域' }]}
      >
        <Select
          placeholder="请选择问题域"
          onChange={(value) => {
            setDomain(value);
            form.setFieldsValue({ domain: value });
          }}
        >
          {domainOptions.map((opt) => (
            <Option key={opt.value} value={opt.value}>
              {opt.label}
            </Option>
          ))}
        </Select>
      </Form.Item>

      <Form.Item
        name="stepCode"
        label="步骤代码"
        rules={[{ required: true, message: '请输入步骤代码' }]}
      >
        <Input placeholder="如：Step_120_Clamp_Check" />
      </Form.Item>

      <Form.Item name="stepName" label="步骤名称">
        <Input placeholder="如：夹具到位检测" />
      </Form.Item>

      <Form.Item
        name="symptomTitle"
        label="问题描述（一句话）"
        rules={[
          { required: true, message: '请输入问题描述' },
          { min: 10, message: '问题描述至少10个字符' },
          { max: 200, message: '问题描述最多200个字符' },
        ]}
      >
        <Input placeholder="一句话描述问题现象，不含判断" />
      </Form.Item>

      <Form.Item name="symptomDetail" label="详细描述（可选）">
        <TextArea rows={4} placeholder="详细描述问题现象" />
      </Form.Item>

      <Form.Item
        name="repro_rate"
        label="复现率 (%)"
        rules={[
          { required: true, message: '请输入复现率' },
          { type: 'number', min: 0, max: 100, message: '复现率必须在0-100之间' },
        ]}
      >
        <InputNumber style={{ width: '100%' }} min={0} max={100} />
      </Form.Item>

      <Form.Item name="reboot_recovers" valuePropName="checked">
        <Checkbox>重启后是否恢复</Checkbox>
      </Form.Item>

      <Form.Item name="env_related" valuePropName="checked">
        <Checkbox>是否与环境相关</Checkbox>
      </Form.Item>
    </>
  );
};

// 事实表表单
const FactsForm: React.FC<{ form: any; domain: string }> = ({ form, domain }) => {
  const yesNoOptions = [
    { value: 'YES', label: '是' },
    { value: 'NO', label: '否' },
    { value: 'NA', label: '不适用' },
  ];

  return (
    <>
      {/* 机械/动作 */}
      {(domain === 'A' || domain === '') && (
        <Card size="small" title="机械/动作" style={{ marginBottom: '16px' }}>
          <Form.Item name="mechanical_action_completed" label="动作是否完成">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="mechanical_position_reliable" label="到位是否可靠">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="mechanical_jam_or_noise" label="是否卡滞/异音">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="mechanical_manual_help_recovers" label="人工辅助后是否恢复">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
        </Card>
      )}

      {/* 电气/IO */}
      {(domain === 'B' || domain === '') && (
        <Card size="small" title="电气/IO" style={{ marginBottom: '16px' }}>
          <Form.Item name="electrical_sensor_physical_ok" label="传感器物理状态正常">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="electrical_plc_io_changes" label="PLC中IO有变化">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="electrical_similar_points_ok" label="同类点位是否正常">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
        </Card>
      )}

      {/* PLC/程序 */}
      {(domain === 'C' || domain === '') && (
        <Card size="small" title="PLC/程序" style={{ marginBottom: '16px' }}>
          <Form.Item
            name="plc_stuck_step_code"
            label="卡在步骤代码"
            rules={domain === 'C' ? [{ required: true, message: '请输入卡在步骤代码' }] : []}
          >
            <Input placeholder="如：Step_120" />
          </Form.Item>
          <Form.Item name="plc_stuck_fixed" label="是否固定卡在该步骤">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="plc_manual_single_step_pass" label="手动/单步是否可通过">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="plc_alarm_code" label="报警代码">
            <Input placeholder="如：E001" />
          </Form.Item>
        </Card>
      )}

      {/* 测试/判定 */}
      {(domain === 'D' || domain === '') && (
        <Card size="small" title="测试/判定" style={{ marginBottom: '16px' }}>
          <Form.Item name="test_same_unit_repeat_consistent" label="同一产品重复测试结果一致">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="test_swap_unit_recovers" label="更换产品是否恢复">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="test_near_limits" label="测试值接近上下限">
            <Select>
              {yesNoOptions.map((opt) => (
                <Option key={opt.value} value={opt.value}>
                  {opt.label}
                </Option>
              ))}
            </Select>
          </Form.Item>
        </Card>
      )}

      {/* 环境 */}
      <Card size="small" title="环境/复现性" style={{ marginBottom: '16px' }}>
        <Form.Item name="repro_rate" label="复现率 (%)">
          <InputNumber style={{ width: '100%' }} min={0} max={100} />
        </Form.Item>
        <Form.Item name="reboot_recovers" valuePropName="checked">
          <Checkbox>重启后是否恢复</Checkbox>
        </Form.Item>
        <Form.Item name="env_related" valuePropName="checked">
          <Checkbox>是否与环境相关</Checkbox>
        </Form.Item>
      </Card>

      <Form.Item name="actionsTakenNote" label="已执行动作说明">
        <TextArea rows={3} placeholder="描述现场已执行的动作" />
      </Form.Item>
    </>
  );
};

// 版本信息表单
const VersionForm: React.FC<{ form: any }> = ({ form }) => {
  const [checkingDiff, setCheckingDiff] = useState(false);
  const [differences, setDifferences] = useState<ConfigDifference[]>([]);
  const [showDiffAlert, setShowDiffAlert] = useState(false);

  // 监听版本信息变化，自动检测配置差异
  const handleVersionChange = async () => {
    const deviceId = form.getFieldValue('deviceId');
    const swVersion = form.getFieldValue('swVersion');
    const plcVersion = form.getFieldValue('plcVersion');
    const paramVersion = form.getFieldValue('paramVersion');

    // 如果设备ID和版本信息都填写了，自动检测差异
    if (deviceId && swVersion && plcVersion && paramVersion) {
      try {
        setCheckingDiff(true);
        const currentConfig = {
          sw_version: swVersion,
          plc_version: plcVersion,
          param_version: paramVersion,
        };
        const diff = await deviceConfigSnapshotService.checkDifferences(deviceId, currentConfig);
        setDifferences(diff);
        setShowDiffAlert(diff.length > 0);
      } catch (error: any) {
        // 如果设备没有标准配置，不显示错误
        if (error.status !== 404) {
          console.error('检查配置差异失败:', error);
        }
        setDifferences([]);
        setShowDiffAlert(false);
      } finally {
        setCheckingDiff(false);
      }
    } else {
      setDifferences([]);
      setShowDiffAlert(false);
    }
  };

  return (
    <>
      <Form.Item
        name="swVersion"
        label="软件版本"
        rules={[{ required: true, message: '请输入软件版本' }]}
      >
        <Input 
          placeholder="如：v2.1.3" 
          onChange={handleVersionChange}
        />
      </Form.Item>

      <Form.Item
        name="plcVersion"
        label="PLC版本"
        rules={[{ required: true, message: '请输入PLC版本' }]}
      >
        <Input 
          placeholder="如：v1.2.6" 
          onChange={handleVersionChange}
        />
      </Form.Item>

      <Form.Item
        name="paramVersion"
        label="参数版本"
        rules={[{ required: true, message: '请输入参数版本' }]}
      >
        <Input 
          placeholder="如：v3.4" 
          onChange={handleVersionChange}
        />
      </Form.Item>

      {/* 配置差异提示 */}
      {checkingDiff && (
        <Spin tip="正在检查配置差异...">
          <div style={{ minHeight: '40px' }} />
        </Spin>
      )}

      {showDiffAlert && differences.length > 0 && (
        <Alert
          message="检测到配置差异"
          description={
            <div>
              <p>发现 {differences.length} 处配置差异，可能与标准配置不一致：</p>
              <ul style={{ marginTop: '8px', marginBottom: 0 }}>
                {differences.slice(0, 3).map((diff, index) => (
                  <li key={index}>
                    <strong>{diff.field}</strong>: 
                    {diff.standard && <span style={{ textDecoration: 'line-through', marginLeft: '8px' }}>标准: {diff.standard}</span>}
                    {diff.actual && <span style={{ marginLeft: '8px' }}>实际: {diff.actual}</span>}
                    {diff.impact && <span style={{ color: '#faad14', marginLeft: '8px' }}>({diff.impact})</span>}
                  </li>
                ))}
                {differences.length > 3 && <li>...还有 {differences.length - 3} 处差异</li>}
              </ul>
              <p style={{ marginTop: '8px', marginBottom: 0 }}>
                <Button 
                  type="link" 
                  size="small" 
                  icon={<DiffOutlined />}
                  onClick={() => {
                    // 跳转到设备配置快照页面查看详细差异
                    window.open(`/devices/config-snapshot?deviceId=${form.getFieldValue('deviceId')}`, '_blank');
                  }}
                >
                  查看详细差异
                </Button>
              </p>
            </div>
          }
          type="warning"
          showIcon
          icon={<WarningOutlined />}
          style={{ marginTop: '16px' }}
          closable
          onClose={() => setShowDiffAlert(false)}
        />
      )}
    </>
  );
};

// 预览确认表单
const PreviewForm: React.FC<{ form: any; ticketId: string | null }> = ({ form, ticketId }) => {
  const values = form.getFieldsValue();
  const [missingInfoCompleted, setMissingInfoCompleted] = useState(false);

  return (
    <>
      <Card size="small" title="基本信息" style={{ marginBottom: '16px' }}>
        <p>
          <strong>问题域：</strong>
          {values.domain ? domainOptions.find((opt) => opt.value === values.domain)?.label : '-'}
        </p>
        <p>
          <strong>步骤代码：</strong>
          {values.stepCode || '-'}
        </p>
        <p>
          <strong>问题描述：</strong>
          {values.symptomTitle || '-'}
        </p>
        <p>
          <strong>复现率：</strong>
          {values.repro_rate || 0}%
        </p>
      </Card>

      <Card size="small" title="版本信息" style={{ marginBottom: '16px' }}>
        <p>
          <strong>软件版本：</strong>
          {values.swVersion || '-'}
        </p>
        <p>
          <strong>PLC版本：</strong>
          {values.plcVersion || '-'}
        </p>
        <p>
          <strong>参数版本：</strong>
          {values.paramVersion || '-'}
        </p>
      </Card>

      {/* 问诊式补全缺失信息 */}
      {ticketId && (
        <div style={{ marginBottom: '24px' }}>
          <MissingInfoQuestionnaire
            ticketId={ticketId}
            jcCode={values.currentJcCode}
            onComplete={(ticketId) => {
              setMissingInfoCompleted(true);
              message.success('信息补全成功，可以继续提交工单');
            }}
            onSkip={() => {
              message.info('已跳过信息补全');
            }}
          />
        </div>
      )}

      <Form.Item
        name="confirmedAsFact"
        valuePropName="checked"
        rules={[{ required: true, message: '请确认以上为现场事实' }]}
      >
        <Checkbox>我确认以上为现场事实，不包含个人判断</Checkbox>
      </Form.Item>

      <div style={{ color: '#ff4d4f', fontSize: '12px', marginTop: '-16px', marginBottom: '16px' }}>
        ⚠️ 请确保已上传至少1个证据附件（附件上传功能在 Issue #003 中实现）
      </div>
    </>
  );
};

export default CreateTicket;


