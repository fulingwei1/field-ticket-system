import React, { useRef, useEffect } from 'react';
import { Card } from 'antd';

interface GraphNode {
  id: string;
  label: string;
  type: string;
  x?: number;
  y?: number;
}

interface GraphEdge {
  id: string;
  source: string;
  target: string;
  type: string;
  weight: number;
}

interface SimpleGraphProps {
  nodes: GraphNode[];
  edges: GraphEdge[];
  width?: number;
  height?: number;
}

/**
 * 简单的知识图谱可视化组件（使用SVG）
 */
export const SimpleGraph: React.FC<SimpleGraphProps> = ({
  nodes,
  edges,
  width = 800,
  height = 600,
}) => {
  const svgRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!svgRef.current || nodes.length === 0) return;

    // 简单的力导向布局（圆形布局）
    const centerX = width / 2;
    const centerY = height / 2;
    const radius = Math.min(width, height) / 3;
    const angleStep = (2 * Math.PI) / nodes.length;

    const positionedNodes = nodes.map((node, index) => {
      const angle = index * angleStep;
      return {
        ...node,
        x: centerX + radius * Math.cos(angle),
        y: centerY + radius * Math.sin(angle),
      };
    });

    // 更新节点位置
    const nodeElements = svgRef.current.querySelectorAll('.graph-node');
    nodeElements.forEach((el, index) => {
      const node = positionedNodes[index];
      if (node && el instanceof SVGElement) {
        el.setAttribute('cx', String(node.x));
        el.setAttribute('cy', String(node.y));
      }
    });

    // 更新边的位置
    const edgeElements = svgRef.current.querySelectorAll('.graph-edge');
    edgeElements.forEach((el, index) => {
      const edge = edges[index];
      if (edge) {
        const sourceNode = positionedNodes.find((n) => n.id === edge.source);
        const targetNode = positionedNodes.find((n) => n.id === edge.target);
        if (sourceNode && targetNode && el instanceof SVGLineElement) {
          el.setAttribute('x1', String(sourceNode.x));
          el.setAttribute('y1', String(sourceNode.y));
          el.setAttribute('x2', String(targetNode.x));
          el.setAttribute('y2', String(targetNode.y));
        }
      }
    });
  }, [nodes, edges, width, height]);

  const getNodeColor = (type: string): string => {
    const colorMap: Record<string, string> = {
      symptom: '#1890ff',
      root_cause: '#f5222d',
      solution: '#52c41a',
      device: '#faad14',
      step: '#722ed1',
    };
    return colorMap[type] || '#666';
  };

  const getEdgeColor = (type: string): string => {
    const colorMap: Record<string, string> = {
      causes: '#f5222d',
      solves: '#52c41a',
      related_to: '#1890ff',
      depends_on: '#faad14',
    };
    return colorMap[type] || '#999';
  };

  if (nodes.length === 0) {
    return (
      <Card>
        <div style={{ textAlign: 'center', color: '#999', padding: '40px' }}>
          暂无图谱数据，请先构建知识图谱
        </div>
      </Card>
    );
  }

  return (
    <Card title="知识图谱可视化">
      <svg
        ref={svgRef}
        width={width}
        height={height}
        style={{ border: '1px solid #e8e8e8', borderRadius: '4px' }}
      >
        {/* 绘制边 */}
        <g className="edges">
          {edges.map((edge) => {
            const sourceNode = nodes.find((n) => n.id === edge.source);
            const targetNode = nodes.find((n) => n.id === edge.target);
            if (!sourceNode || !targetNode) return null;

            return (
              <line
                key={edge.id}
                className="graph-edge"
                x1={sourceNode.x || 0}
                y1={sourceNode.y || 0}
                x2={targetNode.x || 0}
                y2={targetNode.y || 0}
                stroke={getEdgeColor(edge.type)}
                strokeWidth={edge.weight * 2}
                opacity={0.6}
              />
            );
          })}
        </g>

        {/* 绘制节点 */}
        <g className="nodes">
          {nodes.map((node) => (
            <g key={node.id}>
              <circle
                className="graph-node"
                cx={node.x || 0}
                cy={node.y || 0}
                r={20}
                fill={getNodeColor(node.type)}
                stroke="#fff"
                strokeWidth={2}
              />
              <text
                x={node.x || 0}
                y={(node.y || 0) + 35}
                textAnchor="middle"
                fontSize="12"
                fill="#333"
              >
                {node.label.length > 10 ? node.label.substring(0, 10) + '...' : node.label}
              </text>
            </g>
          ))}
        </g>
      </svg>
      <div style={{ marginTop: '16px', fontSize: '12px', color: '#666' }}>
        <div>节点类型：</div>
        <div style={{ display: 'flex', gap: '16px', marginTop: '4px' }}>
          <span>
            <span
              style={{
                display: 'inline-block',
                width: '12px',
                height: '12px',
                backgroundColor: '#1890ff',
                borderRadius: '50%',
                marginRight: '4px',
              }}
            />
            症状
          </span>
          <span>
            <span
              style={{
                display: 'inline-block',
                width: '12px',
                height: '12px',
                backgroundColor: '#f5222d',
                borderRadius: '50%',
                marginRight: '4px',
              }}
            />
            根因
          </span>
          <span>
            <span
              style={{
                display: 'inline-block',
                width: '12px',
                height: '12px',
                backgroundColor: '#52c41a',
                borderRadius: '50%',
                marginRight: '4px',
              }}
            />
            解决方案
          </span>
        </div>
      </div>
    </Card>
  );
};

