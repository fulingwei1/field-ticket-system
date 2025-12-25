import React from 'react';
import { Progress } from 'antd';

interface ConfidenceBarProps {
  confidence: number;
  showLabel?: boolean;
  size?: 'small' | 'default';
  format?: (percent?: number) => string;
}

/**
 * 置信度进度条组件
 */
export const ConfidenceBar: React.FC<ConfidenceBarProps> = ({
  confidence,
  showLabel = true,
  size = 'default',
  format,
}) => {
  const getColor = (percent?: number) => {
    if (!percent) return '#108ee9';
    if (percent >= 80) return '#52c41a'; // 绿色 - 高置信度
    if (percent >= 60) return '#1890ff'; // 蓝色 - 中等置信度
    if (percent >= 40) return '#faad14'; // 橙色 - 低置信度
    return '#ff4d4f'; // 红色 - 很低置信度
  };

  const defaultFormat = (percent?: number) => {
    return `${percent?.toFixed(1)}%`;
  };

  return (
    <Progress
      percent={confidence * 100}
      format={format || defaultFormat}
      strokeColor={getColor(confidence * 100)}
      size={size}
      showInfo={showLabel}
    />
  );
};

