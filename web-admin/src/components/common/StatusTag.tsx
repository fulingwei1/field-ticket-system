import React from 'react';
import { Tag } from 'antd';

interface StatusTagProps {
  status: string;
  statusMap?: Record<string, { label: string; color: string }>;
}

/**
 * 状态标签组件
 */
export const StatusTag: React.FC<StatusTagProps> = ({ status, statusMap }) => {
  const defaultStatusMap: Record<string, { label: string; color: string }> = {
    active: { label: '进行中', color: 'processing' },
    completed: { label: '已完成', color: 'success' },
    pending: { label: '待处理', color: 'default' },
    failed: { label: '失败', color: 'error' },
    cancelled: { label: '已取消', color: 'default' },
  };

  const map = statusMap || defaultStatusMap;
  const statusInfo = map[status] || { label: status, color: 'default' };

  return <Tag color={statusInfo.color}>{statusInfo.label}</Tag>;
};

