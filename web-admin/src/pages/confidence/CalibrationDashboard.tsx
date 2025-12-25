"use client";

import { useState, useEffect } from 'react';
import { Card, Button, Space, message, Spin, Statistic, Row, Col, Table, DatePicker, Select, Tag } from 'antd';
import { ReloadOutlined, LineChartOutlined } from '@ant-design/icons';
import { confidenceCalibrationService, CalibrationModelDto, ConfidenceDistributionDto, CalibrationEvaluationDto } from '../../services/confidenceCalibrationService';
import { DistributionChart } from '../../components/common/DistributionChart';
import { generateConfidenceDistributionData } from '../../utils/chartUtils';
import dayjs, { Dayjs } from 'dayjs';

const { RangePicker } = DatePicker;

export default function CalibrationDashboard() {
  const [loading, setLoading] = useState(false);
  const [activeModel, setActiveModel] = useState<CalibrationModelDto | null>(null);
  const [distribution, setDistribution] = useState<ConfidenceDistributionDto | null>(null);
  const [evaluation, setEvaluation] = useState<CalibrationEvaluationDto | null>(null);
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      await Promise.all([loadActiveModel(), loadDistribution()]);
    } catch (error: any) {
      message.error(error.message || '加载数据失败');
    } finally {
      setLoading(false);
    }
  };

  const loadActiveModel = async () => {
    try {
      const model = await confidenceCalibrationService.getActiveModel();
      setActiveModel(model);
    } catch (error: any) {
      console.error('Failed to load active model:', error);
    }
  };

  const loadDistribution = async (fromDate?: string, toDate?: string) => {
    try {
      const dist = await confidenceCalibrationService.getConfidenceDistribution(fromDate, toDate);
      setDistribution(dist);
    } catch (error: any) {
      console.error('Failed to load distribution:', error);
    }
  };

  const loadEvaluation = async (modelId: string, fromDate?: string, toDate?: string) => {
    try {
      setLoading(true);
      const evalResult = await confidenceCalibrationService.evaluateCalibration({
        modelId,
        fromDate,
        toDate,
      });
      setEvaluation(evalResult);
    } catch (error: any) {
      message.error(error.message || '评估失败');
    } finally {
      setLoading(false);
    }
  };

  const handleDateRangeChange = (dates: [Dayjs, Dayjs] | null) => {
    setDateRange(dates);
    if (dates) {
      loadDistribution(dates[0].format('YYYY-MM-DD'), dates[1].format('YYYY-MM-DD'));
    } else {
      loadDistribution();
    }
  };

  const handleEvaluate = () => {
    if (!activeModel) {
      message.warning('没有活跃的校准模型');
      return;
    }
    const fromDate = dateRange?.[0]?.format('YYYY-MM-DD');
    const toDate = dateRange?.[1]?.format('YYYY-MM-DD');
    loadEvaluation(activeModel.modelId, fromDate, toDate);
  };

  const distributionColumns = [
    {
      title: '置信度范围',
      dataIndex: 'range',
      key: 'range',
    },
    {
      title: '数量',
      dataIndex: 'count',
      key: 'count',
    },
    {
      title: '占比',
      dataIndex: 'percentage',
      key: 'percentage',
      render: (text: number) => `${(text * 100).toFixed(1)}%`,
    },
  ];

  const distributionData = distribution
    ? Object.entries(distribution.distributionByRange).map(([range, count]) => ({
        range,
        count,
        percentage: distribution.totalCount > 0 ? count / distribution.totalCount : 0,
      }))
    : [];

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 头部 */}
        <Card>
          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <h2 style={{ margin: 0 }}>置信度校准系统</h2>
            <Button icon={<ReloadOutlined />} onClick={loadData} loading={loading}>
              刷新
            </Button>
          </Space>
        </Card>

        {/* 日期范围选择 */}
        <Card title="筛选条件" size="small">
          <Space>
            <RangePicker
              value={dateRange}
              onChange={handleDateRangeChange}
              format="YYYY-MM-DD"
            />
            <Button onClick={() => setDateRange(null)}>清除</Button>
          </Space>
        </Card>

        {/* 活跃模型信息 */}
        {activeModel ? (
          <Card title="活跃校准模型">
            <Row gutter={16}>
              <Col span={6}>
                <Statistic title="模型版本" value={activeModel.modelVersion} />
              </Col>
              <Col span={6}>
                <Statistic title="模型类型" value={activeModel.modelType} />
              </Col>
              <Col span={6}>
                <Statistic
                  title="准确率"
                  value={activeModel.accuracy ? (activeModel.accuracy * 100).toFixed(2) : 'N/A'}
                  suffix="%"
                />
              </Col>
              <Col span={6}>
                <Statistic
                  title="F1分数"
                  value={activeModel.f1Score ? (activeModel.f1Score * 100).toFixed(2) : 'N/A'}
                  suffix="%"
                />
              </Col>
            </Row>
            {activeModel.trainedAt && (
              <div style={{ marginTop: '16px' }}>
                <Tag>训练时间: {dayjs(activeModel.trainedAt).format('YYYY-MM-DD HH:mm:ss')}</Tag>
                <Tag>训练数据量: {activeModel.trainingDataCount || 0}</Tag>
              </div>
            )}
            <div style={{ marginTop: '16px' }}>
              <Button type="primary" icon={<LineChartOutlined />} onClick={handleEvaluate} loading={loading}>
                评估模型性能
              </Button>
            </div>
          </Card>
        ) : (
          <Card>
            <Alert message="没有活跃的校准模型" type="warning" />
          </Card>
        )}

        {/* 置信度分布 */}
        {distribution && (
          <>
            <Row gutter={16} style={{ marginBottom: '16px' }}>
              <Col span={8}>
                <Statistic title="总记录数" value={distribution.totalCount} />
              </Col>
              <Col span={8}>
                <Statistic
                  title="平均置信度"
                  value={(distribution.averageConfidence * 100).toFixed(2)}
                  suffix="%"
                />
              </Col>
              <Col span={8}>
                <Statistic
                  title="中位数置信度"
                  value={(distribution.medianConfidence * 100).toFixed(2)}
                  suffix="%"
                />
              </Col>
            </Row>
            <Row gutter={16}>
              <Col span={12}>
                <DistributionChart
                  title="置信度分布"
                  distribution={distribution.distributionByRange}
                  total={distribution.totalCount}
                />
              </Col>
              <Col span={12}>
                <Card title="详细数据" size="small">
                  <Table
                    columns={distributionColumns}
                    dataSource={distributionData}
                    pagination={false}
                    size="small"
                  />
                </Card>
              </Col>
            </Row>
          </>
        )}

        {/* 评估结果 */}
        {evaluation && (
          <Card title="模型评估结果">
            <Row gutter={16} style={{ marginBottom: '16px' }}>
              <Col span={6}>
                <Statistic
                  title="准确率"
                  value={(evaluation.accuracy * 100).toFixed(2)}
                  suffix="%"
                />
              </Col>
              <Col span={6}>
                <Statistic
                  title="精确度"
                  value={(evaluation.precision * 100).toFixed(2)}
                  suffix="%"
                />
              </Col>
              <Col span={6}>
                <Statistic
                  title="召回率"
                  value={(evaluation.recall * 100).toFixed(2)}
                  suffix="%"
                />
              </Col>
              <Col span={6}>
                <Statistic
                  title="F1分数"
                  value={(evaluation.f1Score * 100).toFixed(2)}
                  suffix="%"
                />
              </Col>
            </Row>
            <div style={{ marginTop: '16px' }}>
              <Tag>测试数据量: {evaluation.testDataCount}</Tag>
            </div>
            {Object.keys(evaluation.metricsByConfidenceRange).length > 0 && (
              <div style={{ marginTop: '16px' }}>
                <h4>按置信度范围的指标：</h4>
                <Table
                  columns={[
                    { title: '置信度范围', dataIndex: 'range', key: 'range' },
                    {
                      title: '准确率',
                      dataIndex: 'accuracy',
                      key: 'accuracy',
                      render: (text: number) => `${(text * 100).toFixed(2)}%`,
                    },
                  ]}
                  dataSource={Object.entries(evaluation.metricsByConfidenceRange).map(([range, accuracy]) => ({
                    range,
                    accuracy,
                  }))}
                  pagination={false}
                  size="small"
                />
              </div>
            )}
          </Card>
        )}
      </Space>
    </div>
  );
}

