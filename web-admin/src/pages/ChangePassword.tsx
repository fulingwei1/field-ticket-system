import React, { useState } from 'react';
import { Form, Input, Button, Card, Typography, message, Alert } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService } from '../services/authService';

const { Title, Text } = Typography;

interface ChangePasswordForm {
  oldPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/**
 * 修改密码页面
 */
const ChangePassword: React.FC = () => {
  const [form] = Form.useForm<ChangePasswordForm>();
  const navigate = useNavigate();
  const location = useLocation();

  const [loading, setLoading] = useState(false);
  const isFirstLogin = location.state?.firstLogin || false;

  const handleSubmit = async (values: ChangePasswordForm) => {
    setLoading(true);

    try {
      await authService.changePassword(values.oldPassword, values.newPassword);

      message.success('密码修改成功');

      // 清除认证信息，要求重新登录
      authService.clearAuth();

      // 跳转到登录页
      setTimeout(() => {
        navigate('/login');
      }, 1000);
    } catch (err: any) {
      console.error('Failed to change password:', err);
      message.error(err.message || '修改密码失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        padding: '20px',
      }}
    >
      <Card
        style={{
          width: 450,
          maxWidth: '100%',
          boxShadow: '0 8px 24px rgba(0,0,0,0.2)',
          borderRadius: '8px',
        }}
      >
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <Title level={2} style={{ margin: 0 }}>
            修改密码
          </Title>
          <Text type="secondary">Change Password</Text>
        </div>

        {isFirstLogin && (
          <Alert
            message="首次登录提示"
            description="检测到您是首次登录，为了账户安全，请立即修改密码。"
            type="warning"
            showIcon
            style={{ marginBottom: 24 }}
          />
        )}

        <Form
          form={form}
          name="change_password"
          onFinish={handleSubmit}
          size="large"
          layout="vertical"
        >
          <Form.Item
            name="oldPassword"
            label="当前密码"
            rules={[
              { required: true, message: '请输入当前密码' },
              { min: 6, message: '密码至少6个字符' },
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="请输入当前密码"
            />
          </Form.Item>

          <Form.Item
            name="newPassword"
            label="新密码"
            rules={[
              { required: true, message: '请输入新密码' },
              { min: 6, message: '密码至少6个字符' },
              {
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{8,}$/,
                message: '密码必须包含大小写字母和数字，至少8位',
              },
            ]}
            hasFeedback
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="请输入新密码"
            />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label="确认新密码"
            dependencies={['newPassword']}
            hasFeedback
            rules={[
              { required: true, message: '请确认新密码' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('两次输入的密码不一致'));
                },
              }),
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="请再次输入新密码"
            />
          </Form.Item>

          <div style={{ marginBottom: 24 }}>
            <Text type="secondary" style={{ fontSize: 12 }}>
              密码要求：
            </Text>
            <ul style={{ fontSize: 12, color: '#666', paddingLeft: 20, marginTop: 8 }}>
              <li>至少8个字符</li>
              <li>必须包含大写字母</li>
              <li>必须包含小写字母</li>
              <li>必须包含数字</li>
            </ul>
          </div>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              block
              size="large"
            >
              确认修改
            </Button>
          </Form.Item>

          {!isFirstLogin && (
            <Form.Item>
              <Button
                block
                size="large"
                onClick={() => navigate(-1)}
              >
                取消
              </Button>
            </Form.Item>
          )}
        </Form>
      </Card>
    </div>
  );
};

export default ChangePassword;
