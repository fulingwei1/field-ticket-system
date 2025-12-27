import React from 'react';
import { Spin } from 'antd';
import { LoadingOutlined } from '@ant-design/icons';

interface GlobalLoadingProps {
  tip?: string;
  size?: 'small' | 'default' | 'large';
}

/**
 * 全局加载组件
 * 用于页面级别的加载状态显示
 */
const GlobalLoading: React.FC<GlobalLoadingProps> = ({
  tip = '加载中...',
  size = 'large'
}) => {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        flexDirection: 'column',
      }}
    >
      <Spin
        indicator={<LoadingOutlined style={{ fontSize: 48 }} spin />}
        size={size}
        tip={tip}
      />
    </div>
  );
};

export default GlobalLoading;
