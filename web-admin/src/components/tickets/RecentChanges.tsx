"use client";

import React, { useState, useEffect } from 'react';
import { Card, List, Tag, Descriptions, Empty, Spin, Alert, Tooltip, Space } from 'antd';
import {
  CodeOutlined,
  SettingOutlined,
  ToolOutlined,
  ControlOutlined,
  ClockCircleOutlined,
  UserOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons';
import { recentChangeService, RelatedChangeDto, ChangeType } from '../../services/recentChangeService';
import dayjs from 'dayjs';

interface RecentChangesProps {
  ticketId: string;
  daysBefore?: number;
  daysAfter?: number;
}

/**
 * 最近变更展示组件
 */
export default function RecentChanges({
  ticketId,
  daysBefore = 7,
  daysAfter = 7,
}: RecentChangesProps) {
  const [loading, setLoading] = useState(false);
  const [changes, setChanges] = useState<RelatedChangeDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (ticketId) {
      loadChanges();
    }
  }, [ticketId, daysBefore, daysAfter]);

  const loadChanges = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await recentChangeService.getRelatedChanges(ticketId, daysBefore, daysAfter);
      setChanges(data);
    } catch (err: any) {
      console.error('Failed to load recent changes:', err);
      setError(err.message || '加载最近变更失败');
    } finally {
      setLoading(false);
    }
  };

  const getChangeTypeInfo = (type: ChangeType) => {
    const map: Record<ChangeType, { label: string; color: string; icon: React.ReactNode }> = {
      program_upgrade: {
        label: '程序升级',
        color: 'blue',
        icon: <CodeOutlined />,
      },
      param_change: {
        label: '参数变更',
        color: 'orange',
        icon: <SettingOutlined />,
      },
      component_replacement: {
        label: '换件记录',
        color: 'green',
        icon: <ToolOutlined />,
      },
      config_change: {
        label: '配置变更',
        color: 'purple',
        icon: <ControlOutlined />,
      },
    };
    return map[type] || { label: type, color: 'default', icon: <InfoCircleOutlined /> };
  };

  const getRelevanceColor = (score: number) => {
    if (score >= 70) return 'red';
    if (score >= 50) return 'orange';
    if (score >= 30) return 'blue';
    return 'default';
  };

  const formatChangeDetail = (detail: Record<string, any>) => {
    const items: React.ReactNode[] = [];

    // 版本变更
    if (detail.sw_version) {
      const sw = detail.sw_version;
      if (sw.old && sw.new) {
        items.push(
          <Descriptions.Item key="sw" label="软件版本">
            {sw.old} → {sw.new}
          </Descriptions.Item>
        );
      }
    }
    if (detail.plc_version) {
      const plc = detail.plc_version;
      if (plc.old && plc.new) {
        items.push(
          <Descriptions.Item key="plc" label="PLC版本">
            {plc.old} → {plc.new}
          </Descriptions.Item>
        );
      }
    }
    if (detail.param_version) {
      const param = detail.param_version;
      if (param.old && param.new) {
        items.push(
          <Descriptions.Item key="param" label="参数版本">
            {param.old} → {param.new}
          </Descriptions.Item>
        );
      }
    }

    // 换件信息
    if (detail.component) {
      const comp = detail.component;
      items.push(
        <Descriptions.Item key="component" label="换件信息">
          {comp.type}: {comp.old} → {comp.new}
        </Descriptions.Item>
      );
    }

    // 描述
    if (detail.description) {
      items.push(
        <Descriptions.Item key="desc" label="变更描述" span={2}>
          {detail.description}
        </Descriptions.Item>
      );
    }

    return items.length > 0 ? (
      <Descriptions column={2} size="small" bordered>
        {items}
      </Descriptions>
    ) : null;
  };

  if (loading) {
    return (
      <Card title="最近变更" style={{ marginBottom: 16 }}>
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Spin />
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card title="最近变更" style={{ marginBottom: 16 }}>
        <Alert message="加载失败" description={error} type="error" showIcon />
      </Card>
    );
  }

  if (changes.length === 0) {
    return (
      <Card title="最近变更" style={{ marginBottom: 16 }}>
        <Empty description="未找到相关变更记录" />
      </Card>
    );
  }

  return (
    <Card
      title={
        <Space>
          <span>最近变更</span>
          <Tag color="blue">
            {daysBefore}天前 ~ {daysAfter}天后
          </Tag>
        </Space>
      }
      style={{ marginBottom: 16 }}
    >
      <List
        dataSource={changes}
        renderItem={(change) => {
          const typeInfo = getChangeTypeInfo(change.changeType);
          const relevanceColor = getRelevanceColor(change.relevanceScore);

          return (
            <List.Item
              key={change.changeId}
              style={{
                borderLeft: `4px solid ${
                  relevanceColor === 'red'
                    ? '#ff4d4f'
                    : relevanceColor === 'orange'
                    ? '#ff9800'
                    : relevanceColor === 'blue'
                    ? '#1890ff'
                    : '#d9d9d9'
                }`,
                padding: '16px',
                marginBottom: '12px',
                backgroundColor: '#fafafa',
              }}
            >
              <div style={{ width: '100%' }}>
                <Space style={{ marginBottom: '12px', width: '100%', justifyContent: 'space-between' }}>
                  <Space>
                    <Tag icon={typeInfo.icon} color={typeInfo.color}>
                      {typeInfo.label}
                    </Tag>
                    {change.deviceSn && (
                      <Tag>设备: {change.deviceSn}</Tag>
                    )}
                    <Tooltip title={`相关性评分: ${change.relevanceScore}分`}>
                      <Tag color={relevanceColor}>
                        相关性: {change.relevanceScore}分
                      </Tag>
                    </Tooltip>
                  </Space>
                  <Space>
                    <ClockCircleOutlined /> {dayjs(change.changeDate).format('YYYY-MM-DD HH:mm')}
                  </Space>
                </Space>

                {change.relevanceReasons.length > 0 && (
                  <Alert
                    message="相关性原因"
                    description={
                      <ul style={{ margin: 0, paddingLeft: '20px' }}>
                        {change.relevanceReasons.map((reason, index) => (
                          <li key={index}>{reason}</li>
                        ))}
                      </ul>
                    }
                    type="info"
                    showIcon
                    style={{ marginBottom: '12px' }}
                  />
                )}

                {formatChangeDetail(change.changeDetail)}

                {change.impactScope && (
                  <div style={{ marginTop: '12px' }}>
                    <strong>影响范围：</strong>
                    {change.impactScope.affected_domains && (
                      <Tag style={{ marginLeft: '8px' }}>
                        问题域: {Array.isArray(change.impactScope.affected_domains)
                          ? change.impactScope.affected_domains.join(', ')
                          : change.impactScope.affected_domains}
                      </Tag>
                    )}
                    {change.impactScope.affected_steps && (
                      <Tag style={{ marginLeft: '8px' }}>
                        步骤: {Array.isArray(change.impactScope.affected_steps)
                          ? change.impactScope.affected_steps.join(', ')
                          : change.impactScope.affected_steps}
                      </Tag>
                    )}
                    {change.impactScope.risk_level && (
                      <Tag color="orange" style={{ marginLeft: '8px' }}>
                        风险等级: {change.impactScope.risk_level}
                      </Tag>
                    )}
                  </div>
                )}

                {change.createdByName && (
                  <div style={{ marginTop: '8px', color: '#999', fontSize: '12px' }}>
                    <UserOutlined /> 记录人: {change.createdByName}
                  </div>
                )}
              </div>
            </List.Item>
          );
        }}
      />
    </Card>
  );
}














