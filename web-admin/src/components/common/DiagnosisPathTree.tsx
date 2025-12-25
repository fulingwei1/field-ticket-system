import React from 'react';
import { Card, Tree, Tag, Space } from 'antd';
import { FileTextOutlined } from '@ant-design/icons';

interface DiagnosisPathNode {
  round: number;
  hypothesis?: string;
  confidence?: number;
  verificationSteps?: Array<{
    stepId: string;
    description: string;
    status: string;
    result?: string;
  }>;
  children?: DiagnosisPathNode[];
}

interface DiagnosisPathTreeProps {
  diagnosisPath: any;
}

/**
 * 诊断路径树形可视化组件
 */
export const DiagnosisPathTree: React.FC<DiagnosisPathTreeProps> = ({ diagnosisPath }) => {
  if (!diagnosisPath) {
    return null;
  }

  const convertToTreeData = (path: any): any[] => {
    if (Array.isArray(path)) {
      return path.map((item, index) => ({
        title: (
          <Space>
            <span>轮次 {item.round || index + 1}</span>
            {item.hypothesis && (
              <Tag color="blue">{item.hypothesis}</Tag>
            )}
            {item.confidence !== undefined && (
              <Tag color="green">置信度: {(item.confidence * 100).toFixed(1)}%</Tag>
            )}
          </Space>
        ),
        key: `round-${index}`,
        children: item.verificationSteps?.map((step: any, stepIndex: number) => ({
          title: (
            <Space>
              <span>{step.description || step.stepDescription}</span>
              <Tag color={step.status === 'completed' ? 'success' : 'default'}>
                {step.status === 'completed' ? '已完成' : '待执行'}
              </Tag>
              {step.result && <span style={{ color: '#666' }}>结果: {step.result}</span>}
            </Space>
          ),
          key: `step-${index}-${stepIndex}`,
        })),
      }));
    }

    if (typeof path === 'object') {
      return Object.entries(path).map(([key, value]) => ({
        title: (
          <Space>
            <span>{key}</span>
            {typeof value === 'object' && value !== null && (
              <Tag color="blue">{JSON.stringify(value)}</Tag>
            )}
            {typeof value !== 'object' && <Tag>{String(value)}</Tag>}
          </Space>
        ),
        key,
        children: typeof value === 'object' && value !== null && !Array.isArray(value)
          ? convertToTreeData(value)
          : undefined,
      }));
    }

    return [];
  };

  const treeData = convertToTreeData(diagnosisPath);

  if (treeData.length === 0) {
    return (
      <Card title="诊断路径" extra={<FileTextOutlined />}>
        <div style={{ color: '#999', textAlign: 'center', padding: '20px' }}>
          暂无诊断路径数据
        </div>
      </Card>
    );
  }

  return (
    <Card title="诊断路径" extra={<FileTextOutlined />}>
      <Tree
        treeData={treeData}
        defaultExpandAll
        showLine={{ showLeafIcon: false }}
      />
    </Card>
  );
};

