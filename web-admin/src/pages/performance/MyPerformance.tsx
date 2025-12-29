import React, { useEffect, useState } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  Select,
  DatePicker,
  Spin,
  message,
  Tag,
  Table,
  Progress,
  Space,
  Typography,
  Button,
  Modal,
} from 'antd';
import {
  TrophyOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  UserOutlined,
  CalculatorOutlined,
  RobotOutlined,
  BulbOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import { performanceService, PerformanceMetricsDto, PerformanceTrendDto } from '../../services/performanceService';
import { authService } from '../../services/authService';
import { aiAnalysisService, SkillLevelAnalysisDto, DevelopmentSuggestionDto, PerformanceEvaluationDto } from '../../services/aiAnalysisService';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text } = Typography;
const { Option } = Select;

const MyPerformance: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [metrics, setMetrics] = useState<PerformanceMetricsDto | null>(null);
  const [trends, setTrends] = useState<PerformanceTrendDto[]>([]);
  const [periodType, setPeriodType] = useState<string>('monthly');
  const [periodStart, setPeriodStart] = useState<Dayjs>(dayjs().startOf('month'));

  const user = authService.getUser();
  const [calculating, setCalculating] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);
  const [skillAnalysis, setSkillAnalysis] = useState<SkillLevelAnalysisDto | null>(null);
  const [developmentSuggestion, setDevelopmentSuggestion] = useState<DevelopmentSuggestionDto | null>(null);
  const [performanceEvaluation, setPerformanceEvaluation] = useState<PerformanceEvaluationDto | null>(null);

  useEffect(() => {
    if (user?.id) {
      loadPerformance();
      loadTrends();
    }
  }, [user, periodType, periodStart]);

  const loadPerformance = async () => {
    if (!user?.id) return;

    setLoading(true);
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const data = await performanceService.getEngineerPerformance(
        user.id,
        periodType,
        periodStartStr
      );
      setMetrics(data);
    } catch (error: any) {
      if (error.message === 'Performance metrics not found') {
        message.warning('该周期暂无绩效数据，请先计算绩效');
      } else if (error.message?.includes('认证失败') || error.message?.includes('401')) {
        message.error('认证失败，请重新登录');
        console.error('Authentication failed:', error);
      } else {
        message.error(`加载绩效数据失败: ${error.message || '未知错误'}`);
        console.error('Failed to load performance:', error);
      }
    } finally {
      setLoading(false);
    }
  };

  const loadTrends = async () => {
    if (!user?.id) return;

    try {
      const fromDate = periodStart
        .subtract(5, periodType === 'monthly' ? 'month' : 'week')
        .format('YYYY-MM-DD');
      const toDate = periodStart.format('YYYY-MM-DD');
      const data = await performanceService.getTrends({
        engineerId: user.id,
        periodType,
        fromDate,
        toDate,
      });
      setTrends(data);
    } catch (error: any) {
      if (error.message?.includes('认证失败') || error.message?.includes('401')) {
        message.error('认证失败，请重新登录');
        console.error('Authentication failed:', error);
      } else {
        console.error('Failed to load trends:', error);
        // 趋势数据加载失败不影响主页面显示，只记录日志
      }
    }
  };

  const handleCalculatePerformance = async () => {
    if (!user?.id) return;

    setCalculating(true);
    try {
      // 计算当前周期的绩效
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const periodEnd = periodStart.endOf(periodType === 'monthly' ? 'month' : periodType === 'yearly' ? 'year' : 'day');
      const periodEndStr = periodEnd.format('YYYY-MM-DD');

      await performanceService.calculatePerformance({
        engineerId: user.id,
        periodType,
        periodStart: periodStartStr,
        periodEnd: periodEndStr,
      });

      message.success('绩效计算成功');
      // 重新加载绩效数据
      await loadPerformance();
    } catch (error: any) {
      if (error.message?.includes('认证失败') || error.message?.includes('401')) {
        message.error('认证失败，请重新登录');
      } else if (error.message?.includes('403') || error.message?.includes('Forbid')) {
        message.error('权限不足，只有管理员可以计算绩效');
      } else {
        message.error(`计算绩效失败: ${error.message || '未知错误'}`);
        console.error('Failed to calculate performance:', error);
      }
    } finally {
      setCalculating(false);
    }
  };

  const handleAnalyzeSkillLevel = async () => {
    if (!user?.id) return;

    setAnalyzing(true);
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const result = await aiAnalysisService.analyzeSkillLevel({
        engineerId: user.id,
        periodType,
        periodStart: periodStartStr,
      });
      setSkillAnalysis(result.result);
      Modal.info({
        title: '技能水平分析',
        width: 800,
        content: (
          <div>
            <p><strong>总体技能水平：</strong>{result.result.overallSkillLevel}</p>
            <p><strong>技能评分：</strong>{result.result.skillScore.toFixed(1)}分</p>
            <p><strong>分析总结：</strong></p>
            <p style={{ whiteSpace: 'pre-wrap' }}>{result.result.summary}</p>
            <p><strong>关键优势：</strong></p>
            <ul>
              {result.result.strengths.map((s, i) => (
                <li key={i}>{s}</li>
              ))}
            </ul>
            <p><strong>需要改进的领域：</strong></p>
            <ul>
              {result.result.improvementAreas.map((a, i) => (
                <li key={i}>{a}</li>
              ))}
            </ul>
          </div>
        ),
      });
    } catch (error: any) {
      message.error(`分析技能水平失败: ${error.message || '未知错误'}`);
      console.error('Failed to analyze skill level:', error);
    } finally {
      setAnalyzing(false);
    }
  };

  const handleGenerateDevelopmentSuggestion = async () => {
    if (!user?.id) return;

    setAnalyzing(true);
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const result = await aiAnalysisService.generateDevelopmentSuggestion({
        engineerId: user.id,
        periodType,
        periodStart: periodStartStr,
      });
      setDevelopmentSuggestion(result.result);
      Modal.info({
        title: '个人发展建议',
        width: 900,
        content: (
          <div>
            <p><strong>个人发展特点：</strong></p>
            <p style={{ whiteSpace: 'pre-wrap' }}>{result.result.developmentCharacteristics}</p>
            <p><strong>发展建议：</strong></p>
            {result.result.suggestions.map((s, i) => (
              <div key={i} style={{ marginBottom: '16px', padding: '12px', background: '#f5f5f5', borderRadius: '4px' }}>
                <p><strong>{s.title}</strong> ({s.category}) - 优先级: {s.priority === 'high' ? '高' : s.priority === 'medium' ? '中' : '低'}</p>
                <p>{s.description}</p>
                <p><strong>行动项：</strong></p>
                <ul>
                  {s.actionItems.map((item, j) => (
                    <li key={j}>{item}</li>
                  ))}
                </ul>
                <p><strong>预期成果：</strong>{s.expectedOutcome}</p>
              </div>
            ))}
            <p><strong>短期目标（3个月）：</strong></p>
            <ul>
              {result.result.shortTermGoals.map((g, i) => (
                <li key={i}>{g}</li>
              ))}
            </ul>
            <p><strong>中期目标（6-12个月）：</strong></p>
            <ul>
              {result.result.mediumTermGoals.map((g, i) => (
                <li key={i}>{g}</li>
              ))}
            </ul>
            <p><strong>长期目标（1-2年）：</strong></p>
            <ul>
              {result.result.longTermGoals.map((g, i) => (
                <li key={i}>{g}</li>
              ))}
            </ul>
            <p><strong>推荐资源：</strong></p>
            <ul>
              {result.result.recommendedResources.map((r, i) => (
                <li key={i}>{r}</li>
              ))}
            </ul>
          </div>
        ),
      });
    } catch (error: any) {
      message.error(`生成发展建议失败: ${error.message || '未知错误'}`);
      console.error('Failed to generate development suggestion:', error);
    } finally {
      setAnalyzing(false);
    }
  };

  const handleGeneratePerformanceEvaluation = async () => {
    if (!user?.id) return;

    setAnalyzing(true);
    try {
      const periodStartStr = periodStart.format('YYYY-MM-DD');
      const result = await aiAnalysisService.generatePerformanceEvaluation({
        engineerId: user.id,
        periodType,
        periodStart: periodStartStr,
      });
      setPerformanceEvaluation(result.result);
      Modal.info({
        title: '综合绩效评价',
        width: 900,
        content: (
          <div>
            <p><strong>绩效等级：</strong>{result.result.performanceLevel}</p>
            <p><strong>综合评分：</strong>{result.result.overallScore.toFixed(1)}分</p>
            <p><strong>评价总结：</strong></p>
            <p style={{ whiteSpace: 'pre-wrap' }}>{result.result.evaluationSummary}</p>
            <p><strong>工作亮点：</strong></p>
            <ul>
              {result.result.highlights.map((h, i) => (
                <li key={i}>{h}</li>
              ))}
            </ul>
            <p><strong>需要改进的方面：</strong></p>
            <ul>
              {result.result.areasForImprovement.map((a, i) => (
                <li key={i}>{a}</li>
              ))}
            </ul>
            {result.result.comparison && (
              <>
                <p><strong>与团队平均水平对比：</strong></p>
                <p>团队平均分：{result.result.comparison.teamAverageScore.toFixed(1)}分</p>
                <p>{result.result.comparison.comparisonSummary}</p>
                {result.result.comparison.advantages.length > 0 && (
                  <>
                    <p><strong>相对优势：</strong></p>
                    <ul>
                      {result.result.comparison.advantages.map((a, i) => (
                        <li key={i}>{a}</li>
                      ))}
                    </ul>
                  </>
                )}
                {result.result.comparison.gaps.length > 0 && (
                  <>
                    <p><strong>与平均水平的差距：</strong></p>
                    <ul>
                      {result.result.comparison.gaps.map((g, i) => (
                        <li key={i}>{g}</li>
                      ))}
                    </ul>
                  </>
                )}
              </>
            )}
          </div>
        ),
      });
    } catch (error: any) {
      message.error(`生成绩效评价失败: ${error.message || '未知错误'}`);
      console.error('Failed to generate performance evaluation:', error);
    } finally {
      setAnalyzing(false);
    }
  };

  const getPerformanceLevelColor = (level?: string) => {
    switch (level) {
      case 'excellent':
        return 'success';
      case 'good':
        return 'processing';
      case 'average':
        return 'warning';
      case 'below_average':
        return 'error';
      case 'poor':
        return 'error';
      default:
        return 'default';
    }
  };

  const getPerformanceLevelText = (level?: string) => {
    switch (level) {
      case 'excellent':
        return '优秀';
      case 'good':
        return '良好';
      case 'average':
        return '合格';
      case 'below_average':
        return '待改进';
      case 'poor':
        return '不合格';
      default:
        return '未知';
    }
  };

  const formatTimeSpan = (timeStr?: string) => {
    if (!timeStr) return '-';
    // 解析 TimeSpan 格式 (例如: "02:30:00" 表示 2小时30分钟)
    const parts = timeStr.split(':');
    const hours = parseInt(parts[0]);
    const minutes = parseInt(parts[1]);
    if (hours > 0) {
      return `${hours}小时${minutes}分钟`;
    }
    return `${minutes}分钟`;
  };

  const metricColumns = [
    {
      title: '指标名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '数值',
      dataIndex: 'value',
      key: 'value',
      render: (value: any, record: any) => {
        if (record.type === 'percentage') {
          return <Progress percent={value} size="small" />;
        }
        if (record.type === 'time') {
          return formatTimeSpan(value);
        }
        return value;
      },
    },
    {
      title: '目标值',
      dataIndex: 'target',
      key: 'target',
    },
  ];

  const metricData = metrics
    ? [
        {
          key: '1',
          name: '工单创建完整度',
          value: metrics.ticketCreationCompleteness || 0,
          type: 'percentage',
          target: '≥ 95%',
        },
        {
          key: '2',
          name: '现场问题反馈及时率',
          value: metrics.fieldFeedbackTimelinessRate || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '3',
          name: '工单响应及时率',
          value: metrics.onTimeResponseRate || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '4',
          name: '一次解决率',
          value: metrics.firstTimeResolutionRate || 0,
          type: 'percentage',
          target: '≥ 80%',
        },
        {
          key: '5',
          name: '平均解决时间',
          value: metrics.averageResolutionTime,
          type: 'time',
          target: '≤ 4小时',
        },
        {
          key: '6',
          name: '验证通过率',
          value: metrics.verificationPassRate || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '7',
          name: '工作活动完整度',
          value: metrics.workActivityCompleteness || 0,
          type: 'percentage',
          target: '≥ 90%',
        },
        {
          key: '8',
          name: '客户满意度',
          value: metrics.customerSatisfactionScore ? (metrics.customerSatisfactionScore * 20).toFixed(1) : 0,
          type: 'score',
          target: '≥ 4.0分',
        },
      ]
    : [];

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2}>我的绩效</Title>
        <Space>
          <Select
            value={periodType}
            onChange={setPeriodType}
            style={{ width: 120 }}
          >
            <Option value="daily">日度</Option>
            <Option value="weekly">周度</Option>
            <Option value="monthly">月度</Option>
            <Option value="quarterly">季度</Option>
            <Option value="yearly">年度</Option>
          </Select>
          <DatePicker
            picker={periodType === 'monthly' ? 'month' : periodType === 'yearly' ? 'year' : 'date'}
            value={periodStart}
            onChange={(date) => date && setPeriodStart(date)}
            format={periodType === 'monthly' ? 'YYYY-MM' : periodType === 'yearly' ? 'YYYY' : 'YYYY-MM-DD'}
          />
          <Button
            type="primary"
            icon={<RobotOutlined />}
            onClick={handleAnalyzeSkillLevel}
            loading={analyzing}
          >
            技能水平分析
          </Button>
          <Button
            type="default"
            icon={<BulbOutlined />}
            onClick={handleGenerateDevelopmentSuggestion}
            loading={analyzing}
          >
            发展建议
          </Button>
          <Button
            type="default"
            icon={<FileTextOutlined />}
            onClick={handleGeneratePerformanceEvaluation}
            loading={analyzing}
          >
            绩效评价
          </Button>
        </Space>
      </div>

      <Spin spinning={loading}>
        {metrics ? (
          <>
            {/* 综合评分卡片 */}
            <Row gutter={16} style={{ marginBottom: '24px' }}>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="综合评分"
                    value={metrics.overallScore?.toFixed(1) || 0}
                    suffix="/ 100"
                    prefix={<TrophyOutlined />}
                    valueStyle={{ color: metrics.overallScore && metrics.overallScore >= 90 ? '#3f8600' : '#cf1322' }}
                  />
                  <div style={{ marginTop: '8px' }}>
                    <Tag color={getPerformanceLevelColor(metrics.performanceLevel)}>
                      {getPerformanceLevelText(metrics.performanceLevel)}
                    </Tag>
                  </div>
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="团队排名"
                    value={metrics.rankInTeam || '-'}
                    suffix={metrics.rankInTeam ? '名' : ''}
                    prefix={<UserOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="总工单数"
                    value={metrics.totalTickets}
                    prefix={<CheckCircleOutlined />}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title="已解决工单"
                    value={metrics.ticketsResolved}
                    suffix={`/ ${metrics.totalTickets}`}
                    prefix={<ClockCircleOutlined />}
                  />
                </Card>
              </Col>
            </Row>

            {/* 关键指标 */}
            <Card title="关键指标" style={{ marginBottom: '24px' }}>
              <Table
                columns={metricColumns}
                dataSource={metricData}
                pagination={false}
                size="small"
              />
            </Card>

            {/* 详细指标 */}
            <Row gutter={16} style={{ marginBottom: '24px' }}>
              <Col span={12}>
                <Card title="工单处理效率">
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <div>
                      <Text>工单创建完整度：</Text>
                      <Progress
                        percent={metrics.ticketCreationCompleteness || 0}
                        status={metrics.ticketCreationCompleteness && metrics.ticketCreationCompleteness >= 95 ? 'success' : 'active'}
                      />
                    </div>
                    <div>
                      <Text>现场问题反馈及时率：</Text>
                      <Progress
                        percent={metrics.fieldFeedbackTimelinessRate || 0}
                        status={metrics.fieldFeedbackTimelinessRate && metrics.fieldFeedbackTimelinessRate >= 90 ? 'success' : 'active'}
                      />
                    </div>
                    <div>
                      <Text>工单响应及时率：</Text>
                      <Progress
                        percent={metrics.onTimeResponseRate || 0}
                        status={metrics.onTimeResponseRate && metrics.onTimeResponseRate >= 90 ? 'success' : 'active'}
                      />
                    </div>
                  </Space>
                </Card>
              </Col>
              <Col span={12}>
                <Card title="问题解决能力">
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <div>
                      <Text>一次解决率：</Text>
                      <Progress
                        percent={metrics.firstTimeResolutionRate || 0}
                        status={metrics.firstTimeResolutionRate && metrics.firstTimeResolutionRate >= 80 ? 'success' : 'active'}
                      />
                    </div>
                    <div>
                      <Text>平均解决时间：</Text>
                      <Text strong>{formatTimeSpan(metrics.averageResolutionTime)}</Text>
                    </div>
                    <div>
                      <Text>验证通过率：</Text>
                      <Progress
                        percent={metrics.verificationPassRate || 0}
                        status={metrics.verificationPassRate && metrics.verificationPassRate >= 90 ? 'success' : 'active'}
                      />
                    </div>
                  </Space>
                </Card>
              </Col>
            </Row>

            {/* 其他维度 */}
            <Row gutter={16}>
              <Col span={8}>
                <Card title="知识贡献" size="small">
                  <Statistic
                    title="判断卡创建"
                    value={metrics.judgementCardsCreated}
                  />
                  <Statistic
                    title="解决方案贡献"
                    value={metrics.solutionsContributed}
                    style={{ marginTop: '16px' }}
                  />
                </Card>
              </Col>
              <Col span={8}>
                <Card title="客户服务" size="small">
                  <Statistic
                    title="客户满意度"
                    value={metrics.customerSatisfactionScore?.toFixed(1) || '-'}
                    suffix="/ 5.0"
                  />
                  <Statistic
                    title="客户反馈数"
                    value={metrics.customerFeedbackCount}
                    style={{ marginTop: '16px' }}
                  />
                </Card>
              </Col>
              <Col span={8}>
                <Card title="工作规范性" size="small">
                  <Statistic
                    title="工单信息完整性"
                    value={metrics.ticketInformationCompleteness?.toFixed(1) || 0}
                    suffix="%"
                  />
                  <Statistic
                    title="工作活动完整度"
                    value={metrics.workActivityCompleteness?.toFixed(1) || 0}
                    suffix="%"
                    style={{ marginTop: '16px' }}
                  />
                </Card>
              </Col>
            </Row>
          </>
        ) : (
          <Card>
            <div style={{ textAlign: 'center', padding: '40px' }}>
              <Text type="secondary" style={{ display: 'block', marginBottom: '16px' }}>
                {user?.role === 'Admin' || user?.role === 'admin' 
                  ? '该周期暂无绩效数据，请点击下方按钮计算绩效' 
                  : '暂无绩效数据，请联系管理员计算绩效'}
              </Text>
              {(user?.role === 'Admin' || user?.role === 'admin') && (
                <Button
                  type="primary"
                  icon={<CalculatorOutlined />}
                  loading={calculating}
                  onClick={handleCalculatePerformance}
                >
                  计算绩效
                </Button>
              )}
            </div>
          </Card>
        )}
      </Spin>
    </div>
  );
};

export default MyPerformance;

