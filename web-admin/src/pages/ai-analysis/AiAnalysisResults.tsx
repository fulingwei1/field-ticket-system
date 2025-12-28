import React, { useEffect, useState } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  Typography,
  DatePicker,
  Select,
  message,
  Spin,
  Descriptions,
  Collapse,
  Empty,
} from 'antd';
import {
  ReloadOutlined,
  FileTextOutlined,
  TeamOutlined,
  CalendarOutlined,
  BulbOutlined,
} from '@ant-design/icons';
import { aiAnalysisService, AiAnalysisResultDto } from '../../services/aiAnalysisService';
import { authService } from '../../services/authService';
import { useNavigate } from 'react-router-dom';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;
const { Panel } = Collapse;

const AiAnalysisResults: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<AiAnalysisResultDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [analysisType, setAnalysisType] = useState<string>('');
  const [dateFrom, setDateFrom] = useState<Dayjs | null>(null);
  const [dateTo, setDateTo] = useState<Dayjs | null>(null);
  const navigate = useNavigate();

  const user = authService.getUser();

  useEffect(() => {
    loadResults();
  }, [page, analysisType, dateFrom, dateTo]);

  const loadResults = async () => {
    setLoading(true);
    try {
      const params: any = {
        page,
        pageSize: 20,
      };

      if (analysisType) {
        params.analysisType = analysisType;
      }

      if (user?.role === 'FieldEngineer') {
        // 现场工程师只能查看自己的分析结果
        params.engineerId = user.id;
      }

      if (dateFrom) {
        params.analysisDateFrom = dateFrom.format('YYYY-MM-DD');
      }

      if (dateTo) {
        params.analysisDateTo = dateTo.format('YYYY-MM-DD');
      }

      const result = await aiAnalysisService.getAnalysisResults(params);
      setResults(result.items);
      setTotal(result.total);
    } catch (error: any) {
      message.error('加载分析结果失败');
      console.error('Failed to load analysis results:', error);
    } finally {
      setLoading(false);
    }
  };

  const getAnalysisTypeLabel = (type: string) => {
    const map: Record<string, { label: string; color: string }> = {
      daily_summary: { label: '每日总结', color: 'blue' },
      weekly_summary: { label: '每周总结', color: 'green' },
      team_analysis: { label: '团队分析', color: 'orange' },
      scheduling_suggestion: { label: '人员安排建议', color: 'purple' },
    };
    return map[type] || { label: type, color: 'default' };
  };

  const columns = [
    {
      title: '分析类型',
      dataIndex: 'analysisType',
      key: 'analysisType',
      render: (type: string) => {
        const info = getAnalysisTypeLabel(type);
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '分析日期',
      dataIndex: 'analysisDate',
      key: 'analysisDate',
      render: (date: string) => dayjs(date).format('YYYY-MM-DD'),
    },
    {
      title: '工程师',
      dataIndex: 'engineerName',
      key: 'engineerName',
      render: (name: string | undefined) => name || '-',
    },
    {
      title: '摘要',
      dataIndex: 'summary',
      key: 'summary',
      ellipsis: true,
      render: (text: string) => <Text ellipsis={{ tooltip: text }}>{text}</Text>,
    },
    {
      title: '置信度',
      dataIndex: 'confidenceScore',
      key: 'confidenceScore',
      render: (score: number | undefined) =>
        score ? `${(score * 100).toFixed(0)}%` : '-',
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => dayjs(text).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: AiAnalysisResultDto) => (
        <Button
          type="link"
          onClick={() => navigate(`/ai-analysis/${record.analysisId}`)}
        >
          查看详情
        </Button>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2}>AI分析结果</Title>
        <Space>
          <Select
            value={analysisType}
            onChange={setAnalysisType}
            placeholder="分析类型"
            allowClear
            style={{ width: 150 }}
          >
            <Option value="daily_summary">每日总结</Option>
            <Option value="weekly_summary">每周总结</Option>
            <Option value="team_analysis">团队分析</Option>
            <Option value="scheduling_suggestion">人员安排建议</Option>
          </Select>
          <DatePicker
            placeholder="开始日期"
            value={dateFrom}
            onChange={setDateFrom}
            format="YYYY-MM-DD"
          />
          <DatePicker
            placeholder="结束日期"
            value={dateTo}
            onChange={setDateTo}
            format="YYYY-MM-DD"
          />
          <Button icon={<ReloadOutlined />} onClick={loadResults}>
            刷新
          </Button>
        </Space>
      </div>

      <Spin spinning={loading}>
        {results.length > 0 ? (
          <Table
            columns={columns}
            dataSource={results}
            rowKey="analysisId"
            pagination={{
              current: page,
              pageSize: 20,
              total,
              onChange: (p) => setPage(p),
            }}
          />
        ) : (
          <Empty description="暂无分析结果" />
        )}
      </Spin>
    </div>
  );
};

export default AiAnalysisResults;


