"use client";

import { Form, Input, Select, InputNumber, Switch, Space } from 'antd';
import { CreateSolutionRequest, UpdateSolutionRequest } from '../../services/solutionService';

const { TextArea } = Input;
const { Option } = Select;

interface SolutionFormProps {
  form: any;
  initialValues?: Partial<CreateSolutionRequest>;
  isEdit?: boolean;
}

export default function SolutionForm({ form, initialValues, isEdit = false }: SolutionFormProps) {
  return (
    <Form
      form={form}
      layout="vertical"
      initialValues={{
        solutionType: 'software_update',
        releaseType: 'SOFTWARE',
        riskLevel: 'medium',
        rollbackPossible: true,
        ...initialValues,
      }}
    >
      <Form.Item
        name="title"
        label="解决方案标题"
        rules={[{ required: true, message: '请输入解决方案标题' }]}
      >
        <Input placeholder="请输入解决方案标题" />
      </Form.Item>

      <Form.Item
        name="description"
        label="解决方案描述"
      >
        <TextArea rows={4} placeholder="请输入解决方案描述（可选）" />
      </Form.Item>

      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 方案类型 */}
        <Form.Item
          name="solutionType"
          label="方案类型"
          rules={[{ required: true, message: '请选择方案类型' }]}
        >
          <Select>
            <Option value="software_update">软件更新</Option>
            <Option value="parameter_change">参数调整</Option>
            <Option value="hardware_replacement">硬件更换</Option>
            <Option value="procedure">操作流程</Option>
          </Select>
        </Form.Item>

        {/* 发布类型 */}
        <Form.Item
          name="releaseType"
          label="发布类型"
          rules={[{ required: true, message: '请选择发布类型' }]}
        >
          <Select>
            <Option value="PLC">PLC</Option>
            <Option value="SOFTWARE">软件</Option>
            <Option value="PARAM">参数</Option>
            <Option value="MIXED">混合</Option>
          </Select>
        </Form.Item>

        {/* 版本信息 */}
        <div>
          <h4>当前版本（必填）</h4>
          <Space>
            <Form.Item
              name="requiredSwVersion"
              label="软件版本"
              style={{ marginBottom: 0 }}
            >
              <Input placeholder="例如：v1.2.3" />
            </Form.Item>
            <Form.Item
              name="requiredPlcVersion"
              label="PLC版本"
              style={{ marginBottom: 0 }}
            >
              <Input placeholder="例如：v1.2.3" />
            </Form.Item>
            <Form.Item
              name="requiredParamVersion"
              label="参数版本"
              style={{ marginBottom: 0 }}
            >
              <Input placeholder="例如：v1.2.3" />
            </Form.Item>
          </Space>
        </div>

        <div>
          <h4>新版本（可选）</h4>
          <Space>
            <Form.Item
              name="newSwVersion"
              label="新软件版本"
              style={{ marginBottom: 0 }}
            >
              <Input placeholder="例如：v1.2.4" />
            </Form.Item>
            <Form.Item
              name="newPlcVersion"
              label="新PLC版本"
              style={{ marginBottom: 0 }}
            >
              <Input placeholder="例如：v1.2.4" />
            </Form.Item>
            <Form.Item
              name="newParamVersion"
              label="新参数版本"
              style={{ marginBottom: 0 }}
            >
              <Input placeholder="例如：v1.2.4" />
            </Form.Item>
          </Space>
        </div>

        {/* 修改内容（JSON） */}
        <Form.Item
          name="changeDetailJson"
          label="修改内容（JSON格式）"
          rules={[
            { required: true, message: '请输入修改内容' },
            {
              validator: (_, value) => {
                if (!value) return Promise.resolve();
                try {
                  JSON.parse(typeof value === 'string' ? value : JSON.stringify(value));
                  return Promise.resolve();
                } catch {
                  return Promise.reject(new Error('请输入有效的JSON格式'));
                }
              },
            },
          ]}
        >
          <TextArea
            rows={8}
            placeholder='请输入修改内容，JSON格式，例如：{"changes": [{"type": "PLC_LOGIC", "location": "Step_120", "before": "无滤波", "after": "增加50ms滤波", "reason": "消除信号抖动"}]}'
          />
        </Form.Item>

        {/* 验证清单（JSON） */}
        <Form.Item
          name="verificationChecklistJson"
          label="验证清单（JSON格式）"
          rules={[
            { required: true, message: '请输入验证清单' },
            {
              validator: (_, value) => {
                if (!value) return Promise.resolve();
                try {
                  JSON.parse(typeof value === 'string' ? value : JSON.stringify(value));
                  return Promise.resolve();
                } catch {
                  return Promise.reject(new Error('请输入有效的JSON格式'));
                }
              },
            },
          ]}
        >
          <TextArea
            rows={8}
            placeholder='请输入验证清单，JSON格式，例如：{"precheck": ["确认设备处于停止状态"], "steps": [{"id": "S1", "text": "下载新版本", "type": "action", "required": true}]}'
          />
        </Form.Item>

        {/* 实施步骤 */}
        <Form.Item
          name="implementationSteps"
          label="实施步骤"
        >
          <TextArea rows={6} placeholder="请输入实施步骤（可选）" />
        </Form.Item>

        {/* 预计实施时间 */}
        <Form.Item
          name="estimatedImplementationTime"
          label="预计实施时间（分钟）"
        >
          <InputNumber min={1} placeholder="预计实施时间" style={{ width: '100%' }} />
        </Form.Item>

        {/* 风险评估 */}
        <div>
          <h4>风险评估</h4>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Form.Item
              name="riskLevel"
              label="风险等级"
              rules={[{ required: true, message: '请选择风险等级' }]}
            >
              <Select>
                <Option value="low">低</Option>
                <Option value="medium">中</Option>
                <Option value="high">高</Option>
              </Select>
            </Form.Item>
            <Form.Item
              name="riskDescription"
              label="风险描述"
            >
              <TextArea rows={3} placeholder="请输入风险描述（可选）" />
            </Form.Item>
          </Space>
        </div>

        {/* 回滚方案 */}
        <div>
          <h4>回滚方案</h4>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Form.Item
              name="rollbackPossible"
              label="是否可回滚"
              valuePropName="checked"
            >
              <Switch />
            </Form.Item>
            <Form.Item
              name="rollbackProcedure"
              label="回滚步骤"
              noStyle
            >
              <TextArea
                rows={4}
                placeholder="请输入回滚步骤（可选）"
                disabled={!form.getFieldValue('rollbackPossible')}
              />
            </Form.Item>
          </Space>
        </div>
      </Space>
    </Form>
  );
}


