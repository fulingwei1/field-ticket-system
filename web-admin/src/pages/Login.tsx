import React, { useEffect, useState } from 'react';
import { Button, Card, message } from 'antd';
import { WechatOutlined } from '@ant-design/icons';
import { authService, UserInfo } from '../services/authService';
import { useNavigate } from 'react-router-dom';

const Login: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    console.log('Login component mounted');
    // 如果已登录，跳转到首页
    if (authService.isAuthenticated()) {
      console.log('User already authenticated, redirecting to /');
      navigate('/');
    } else {
      console.log('User not authenticated, showing login page');
    }
  }, [navigate]);

  const handleWeComLogin = async () => {
    setLoading(true);
    try {
      // 生成 state（CSRF 防护）
      const state = crypto.randomUUID();

      // 获取企业微信登录URL
      const { url } = await authService.getWeComLoginUrl(state);

      // 保存 state 到 sessionStorage
      sessionStorage.setItem('wecom_login_state', state);

      // 跳转到企业微信授权页面
      window.location.href = url;
    } catch (error) {
      console.error('Failed to get WeCom login URL:', error);
      message.error('获取登录链接失败，请稍后重试');
      setLoading(false);
    }
  };

  // 处理回调（从 URL 参数中获取 code 和 state）
  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');

    if (code && state) {
      // 验证 state
      const savedState = sessionStorage.getItem('wecom_login_state');
      if (savedState !== state) {
        message.error('登录状态验证失败，请重新登录');
        return;
      }

      sessionStorage.removeItem('wecom_login_state');

      // 处理回调
      authService
        .handleWeComCallback(code, state)
        .then(() => {
          message.success('登录成功');
          navigate('/');
        })
        .catch((error) => {
          console.error('Failed to handle callback:', error);
          message.error('登录失败，请稍后重试');
        });
    }
  }, [navigate]);

  console.log('Login component rendering...');
  
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      }}
    >
      <Card
        style={{
          width: 400,
          boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
        }}
        title={
          <div style={{ textAlign: 'center', fontSize: '24px', fontWeight: 'bold' }}>
            现场问题反馈系统
          </div>
        }
      >
        <div style={{ textAlign: 'center', marginBottom: '24px' }}>
          <p style={{ color: '#666', marginBottom: '32px' }}>
            请使用企业微信扫码登录
          </p>
          <Button
            type="primary"
            size="large"
            icon={<WechatOutlined />}
            loading={loading}
            onClick={handleWeComLogin}
            style={{
              width: '100%',
              height: '48px',
              fontSize: '16px',
            }}
          >
            企业微信登录
          </Button>
        </div>
      </Card>
    </div>
  );
};

export default Login;


