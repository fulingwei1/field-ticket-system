"use client";

import React, { useState, useEffect } from 'react';
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Space,
  message,
  Spin,
  Progress,
  Typography,
  List,
  Alert,
} from 'antd';
import {
  UserOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import { userProfileService, UserProfileDto } from '../../services/userProfileService';
import { authService } from '../../services/authService';

const { Title, Text } = Typography;

export default function UserProfile() {
  const [loading, setLoading] = useState(false);
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const [building, setBuilding] = useState(false);

  const user = authService.getUser();

  useEffect(() => {
    if (user?.id) {
      loadProfile();
    }
  }, [user]);

  const loadProfile = async () => {
    if (!user?.id) return;

    setLoading(true);
    try {
      const data = await userProfileService.getUserProfile(user.id);
      setProfile(data);
    } catch (error: any) {
      if (error.message.includes('404')) {
        // 用户画像不存在，可以构建
        message.info('用户画像不存在，点击"构建画像"按钮创建');
      } else {
        message.error('加载用户画像失败');
        console.error('Failed to load profile:', error);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleBuildProfile = async () => {
    if (!user?.id) return;

    setBuilding(true);
    try {
      const data = await userProfileService.buildUserProfile(user.id);
      setProfile(data);
      message.success('用户画像构建成功');
    } catch (error: any) {
      message.error('构建用户画像失败');
      console.error('Failed to build profile:', error);
    } finally {
      setBuilding(false);
    }
  };

  const getExpertiseLevelColor = (level?: string) => {
    switch (level) {
      case 'expert':
        return 'success';
      case 'intermediate':
        return 'warning';
      case 'beginner':
        return 'default';
      default:
        return 'default';
    }
  };

  const getExpertiseLevelText = (level?: string) => {
    switch (level) {
      case 'expert':
        return '专家';
      case 'intermediate':
        return '中级';
      case 'beginner':
        return '初级';
      default:
        return '未知';
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!profile) {
    return (
      <Card>
        <Alert
          message="用户画像不存在"
          description="点击下方按钮构建您的用户画像，系统将分析您的历史工单数据。"
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button
          type="primary"
          icon={<UserOutlined />}
          loading={building}
          onClick={handleBuildProfile}
        >
          构建用户画像
        </Button>
      </Card>
    );
  }

  const commonFields = profile.commonFields || {};
  const frequentlyUsed = commonFields.frequently_used || {};
  const preferences = commonFields.preferences || {};
  const commonMistakes = profile.commonMistakes || {};

  return (
    <div>
      <Card>
        <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
          <Title level={4}>
            <UserOutlined /> 我的用户画像
          </Title>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadProfile}>
              刷新
            </Button>
            <Button type="primary" icon={<UserOutlined />} onClick={handleBuildProfile} loading={building}>
              重新构建
            </Button>
          </Space>
        </Space>

        <Descriptions bordered column={2}>
          <Descriptions.Item label="专业度等级">
            <Tag color={getExpertiseLevelColor(profile.expertiseLevel)}>
              {getExpertiseLevelText(profile.expertiseLevel)}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="专业度评分">
            {profile.expertiseScore !== undefined ? (
              <Progress
                percent={Math.round(profile.expertiseScore * 100)}
                format={(percent) => `${percent}%`}
                status={profile.expertiseScore >= 0.8 ? 'success' : profile.expertiseScore >= 0.5 ? 'normal' : 'exception'}
              />
            ) : (
              '未计算'
            )}
          </Descriptions.Item>
          <Descriptions.Item label="总工单数">
            {profile.totalTickets}
          </Descriptions.Item>
          <Descriptions.Item label="平均完成时间">
            {profile.averageCompletionTime ? (
              <Space>
                <ClockCircleOutlined />
                {Math.round(profile.averageCompletionTime / 60)} 分钟
              </Space>
            ) : (
              '暂无数据'
            )}
          </Descriptions.Item>
          <Descriptions.Item label="最后更新时间" span={2}>
            {new Date(profile.updatedAt).toLocaleString('zh-CN')}
          </Descriptions.Item>
        </Descriptions>

        {Object.keys(frequentlyUsed).length > 0 && (
          <Card title="常用字段" style={{ marginTop: 16 }}>
            <Descriptions column={2}>
              {frequentlyUsed.device_model && (
                <Descriptions.Item label="常用设备型号">
                  {frequentlyUsed.device_model}
                </Descriptions.Item>
              )}
              {frequentlyUsed.problem_domain && (
                <Descriptions.Item label="常用问题域">
                  {frequentlyUsed.problem_domain}
                </Descriptions.Item>
              )}
              {frequentlyUsed.common_symptoms && Array.isArray(frequentlyUsed.common_symptoms) && (
                <Descriptions.Item label="常见症状" span={2}>
                  {frequentlyUsed.common_symptoms.map((symptom: string, index: number) => (
                    <Tag key={index} style={{ marginBottom: 4 }}>
                      {symptom}
                    </Tag>
                  ))}
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>
        )}

        {Object.keys(preferences).length > 0 && (
          <Card title="填写偏好" style={{ marginTop: 16 }}>
            <Descriptions column={2}>
              {preferences.question_style && (
                <Descriptions.Item label="问题风格">
                  {preferences.question_style === 'detailed' ? '详细' : '简洁'}
                </Descriptions.Item>
              )}
              {preferences.detail_level && (
                <Descriptions.Item label="详细程度">
                  {preferences.detail_level === 'high' ? '高' : '低'}
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>
        )}

        {commonMistakes && Object.keys(commonMistakes).length > 0 && (
          <Card title="常见错误和改进建议" style={{ marginTop: 16 }}>
            {commonMistakes.mistake_types && Array.isArray(commonMistakes.mistake_types) && (
              <Alert
                message="发现的问题"
                description={
                  <List
                    size="small"
                    dataSource={commonMistakes.mistake_types}
                    renderItem={(item: string) => <List.Item>{item}</List.Item>}
                  />
                }
                type="warning"
                showIcon
                style={{ marginBottom: 16 }}
              />
            )}
            {commonMistakes.improvement_suggestions && Array.isArray(commonMistakes.improvement_suggestions) && (
              <Alert
                message="改进建议"
                description={
                  <List
                    size="small"
                    dataSource={commonMistakes.improvement_suggestions}
                    renderItem={(item: string) => (
                      <List.Item>
                        <CheckCircleOutlined style={{ color: '#52c41a', marginRight: 8 }} />
                        {item}
                      </List.Item>
                    )}
                  />
                }
                type="success"
                showIcon
              />
            )}
          </Card>
        )}
      </Card>
    </div>
  );
}













