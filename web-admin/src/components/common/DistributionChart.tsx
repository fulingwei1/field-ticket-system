import React from 'react';
import { Card, Row, Col, Progress, Tag } from 'antd';
import { getColorByIndex, formatPercentage } from '../../utils/chartUtils';

interface DistributionChartProps {
  title: string;
  distribution: Record<string, number>;
  total: number;
  showPercentage?: boolean;
}

/**
 * 分布图表组件（使用进度条模拟柱状图）
 */
export const DistributionChart: React.FC<DistributionChartProps> = ({
  title,
  distribution,
  total,
  showPercentage = true,
}) => {
  const entries = Object.entries(distribution).sort((a, b) => b[1] - a[1]);

  return (
    <Card title={title} size="small">
      {entries.map(([key, value], index) => {
        const percentage = total > 0 ? (value / total) * 100 : 0;
        return (
          <div key={key} style={{ marginBottom: '16px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
              <span>
                <Tag color={getColorByIndex(index)}>{key}</Tag>
                <span style={{ marginLeft: '8px' }}>{value} 条</span>
              </span>
              {showPercentage && (
                <span style={{ color: '#666' }}>{formatPercentage(percentage / 100)}</span>
              )}
            </div>
            <Progress
              percent={percentage}
              strokeColor={getColorByIndex(index)}
              showInfo={false}
            />
          </div>
        );
      })}
      {entries.length === 0 && (
        <div style={{ textAlign: 'center', color: '#999', padding: '20px' }}>暂无数据</div>
      )}
    </Card>
  );
};

