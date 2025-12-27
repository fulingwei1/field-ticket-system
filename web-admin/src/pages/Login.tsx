import React, { useState, useEffect } from 'react';
import { Form, Input, Button, Checkbox, Tabs, Alert, Typography, Card, Divider, message } from 'antd';
import { UserOutlined, LockOutlined, WechatOutlined } from '@ant-design/icons';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authService } from '../services/authService';

const { Title, Text } = Typography;

interface PasswordLoginForm {
  username: string;
  password: string;
  remember: boolean;
}

/**
 * 登录页面
 * 支持两种登录方式：用户名密码登录和企业微信登录
 */
const Login: React.FC = () => {
  const [form] = Form.useForm<PasswordLoginForm>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [activeTab, setActiveTab] = useState<string>('password');

  // 检查是否已登录
  useEffect(() => {
    if (authService.isAuthenticated()) {
      navigate('/');
    }
  }, [navigate]);

  // 处理企业微信回调
  useEffect(() => {
    const code = searchParams.get('code');
    const state = searchParams.get('state');

    if (code && state) {
      handleWeComCallback(code, state);
    }
  }, [searchParams]);

  // 用户名密码登录
  const handlePasswordLogin = async (values: PasswordLoginForm) => {
    setLoading(true);
    setError('');

    try {
      const result = await authService.passwordLogin(
        values.username,
        values.password,
        values.remember
      );

      // 检查是否需要修改密码
      if (result.user.mustChangePassword) {
        message.warning('检测到首次登录，请修改密码');
        navigate('/change-password', {
          state: { firstLogin: true },
        });
        return;
      }

      // 登录成功
      message.success('登录成功');
      navigate('/');
    } catch (err: any) {
      console.error('Login failed:', err);
      setError(err.message || '登录失败，请检查用户名和密码');
    } finally {
      setLoading(false);
    }
  };

  // 获取企业微信登录URL
  const handleWeComLogin = async () => {
    setLoading(true);
    setError('');

    try {
      const state = crypto.randomUUID();
      const result = await authService.getWeComLoginUrl(state);

      // 保存state到sessionStorage
      sessionStorage.setItem('wecom_login_state', state);

      // 跳转到企业微信登录
      window.location.href = result.url;
    } catch (err: any) {
      console.error('Failed to get WeCom login URL:', err);
      setError('获取企业微信登录链接失败');
      setLoading(false);
    }
  };

  // 处理企业微信回调
  const handleWeComCallback = async (code: string, state: string) => {
    setLoading(true);
    setError('');

    // 验证state
    const savedState = sessionStorage.getItem('wecom_login_state');
    if (savedState !== state) {
      setError('登录状态验证失败，请重新登录');
      setLoading(false);
      navigate('/login', { replace: true });
      return;
    }

    sessionStorage.removeItem('wecom_login_state');

    try {
      await authService.handleWeComCallback(code, state);

      // 登录成功
      message.success('登录成功');
      navigate('/');
    } catch (err: any) {
      console.error('WeCom callback failed:', err);
      setError('企业微信登录失败');
      setLoading(false);

      // 清除URL参数
      navigate('/login', { replace: true });
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
            现场工单管理系统
          </Title>
          <Text type="secondary">Field Ticket Management System</Text>
        </div>

        {error && (
          <Alert
            message={error}
            type="error"
            closable
            onClose={() => setError('')}
            style={{ marginBottom: 24 }}
          />
        )}

        <Tabs
          activeKey={activeTab}
          onChange={setActiveTab}
          centered
          items={[
            {
              key: 'password',
              label: (
                <span>
                  <UserOutlined />
                  账号登录
                </span>
              ),
              children: (
                <>
                  <Form
                    form={form}
                    name="password_login"
                    onFinish={handlePasswordLogin}
                    size="large"
                    initialValues={{ remember: true }}
                    style={{ marginTop: 24 }}
                  >
                    <Form.Item
                      name="username"
                      rules={[
                        { required: true, message: '请输入用户名' },
                        { min: 3, message: '用户名至少3个字符' },
                      ]}
                    >
                      <Input
                        prefix={<UserOutlined />}
                        placeholder="用户名"
                        autoComplete="username"
                      />
                    </Form.Item>

                    <Form.Item
                      name="password"
                      rules={[
                        { required: true, message: '请输入密码' },
                        { min: 6, message: '密码至少6个字符' },
                      ]}
                    >
                      <Input.Password
                        prefix={<LockOutlined />}
                        placeholder="密码"
                        autoComplete="current-password"
                      />
                    </Form.Item>

                    <Form.Item>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Form.Item name="remember" valuePropName="checked" noStyle>
                          <Checkbox>记住我（30天）</Checkbox>
                        </Form.Item>
                      </div>
                    </Form.Item>

                    <Form.Item>
                      <Button
                        type="primary"
                        htmlType="submit"
                        loading={loading}
                        block
                        size="large"
                      >
                        登录
                      </Button>
                    </Form.Item>
                  </Form>

                  <div style={{ textAlign: 'center', marginTop: 16 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      💡 提示：首次登录请使用管理员提供的账号密码
                    </Text>
                  </div>
                </>
              ),
            },
            {
              key: 'wecom',
              label: (
                <span>
                  <WechatOutlined />
                  企业微信
                </span>
              ),
              children: (
                <div style={{ textAlign: 'center', padding: '32px 0' }}>
                  <div style={{ marginBottom: 24 }}>
                    <WechatOutlined style={{ fontSize: 80, color: '#07c160' }} />
                  </div>

                  <Title level={4} style={{ marginBottom: 8 }}>
                    企业微信扫码登录
                  </Title>

                  <Text type="secondary" style={{ marginBottom: 32, display: 'block' }}>
                    使用企业微信扫描二维码登录系统
                  </Text>

                  <Button
                    type="primary"
                    size="large"
                    icon={<WechatOutlined />}
                    onClick={handleWeComLogin}
                    loading={loading}
                    block
                    style={{
                      backgroundColor: '#07c160',
                      borderColor: '#07c160',
                      height: 48,
                    }}
                  >
                    使用企业微信登录
                  </Button>

                  <Divider plain style={{ margin: '24px 0' }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      扫码后自动跳转
                    </Text>
                  </Divider>

                  <div style={{ textAlign: 'center', marginTop: 16 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      💡 提示：需要企业微信管理员授权后方可使用
                    </Text>
                  </div>
                </div>
              ),
            },
          ]}
        />

        <Divider />

        <div style={{ textAlign: 'center' }}>
          <Text type="secondary" style={{ fontSize: 12 }}>
            © 2025 现场工单管理系统 · 技术支持
          </Text>
        </div>
      </Card>
    </div>
  );
};

export default Login;
