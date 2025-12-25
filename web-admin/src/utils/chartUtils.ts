/**
 * 图表工具函数
 */

/**
 * 生成置信度分布数据（用于柱状图）
 */
export const generateConfidenceDistributionData = (
  distribution: Record<string, number>
): Array<{ range: string; count: number; percentage: number }> => {
  const total = Object.values(distribution).reduce((sum, count) => sum + count, 0);
  
  return Object.entries(distribution).map(([range, count]) => ({
    range,
    count,
    percentage: total > 0 ? (count / total) * 100 : 0,
  }));
};

/**
 * 生成趋势数据（用于折线图）
 */
export const generateTrendData = (
  trends: Array<{ date: string; value: number }>
): Array<{ date: string; value: number; formattedDate: string }> => {
  return trends.map((trend) => ({
    ...trend,
    formattedDate: new Date(trend.date).toLocaleDateString('zh-CN', {
      month: 'short',
      day: 'numeric',
    }),
  }));
};

/**
 * 生成责任分布数据（用于饼图）
 */
export const generateResponsibilityDistributionData = (
  distribution: Record<string, number>
): Array<{ name: string; value: number; percentage: number }> => {
  const total = Object.values(distribution).reduce((sum, count) => sum + count, 0);
  
  return Object.entries(distribution).map(([name, value]) => ({
    name,
    value,
    percentage: total > 0 ? (value / total) * 100 : 0,
  }));
};

/**
 * 获取颜色（根据索引）
 */
export const getColorByIndex = (index: number): string => {
  const colors = [
    '#1890ff', // 蓝色
    '#52c41a', // 绿色
    '#faad14', // 橙色
    '#f5222d', // 红色
    '#722ed1', // 紫色
    '#13c2c2', // 青色
    '#eb2f96', // 粉色
    '#fa8c16', // 橙红色
  ];
  return colors[index % colors.length];
};

/**
 * 格式化百分比
 */
export const formatPercentage = (value: number, decimals: number = 1): string => {
  return `${(value * 100).toFixed(decimals)}%`;
};

