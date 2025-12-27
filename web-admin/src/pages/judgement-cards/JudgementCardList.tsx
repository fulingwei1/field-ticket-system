import React, { useEffect, useState } from 'react';
import {
  Table,
  Button,
  Space,
  message,
  Modal,
  Form,
  Input,
  Select,
  Popconfirm,
  Tag,
  Typography,
  Tooltip,
  Rate,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined,
  EyeOutlined,
  CopyOutlined,
  HistoryOutlined,
  StarOutlined,
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { judgementCardService, JudgementCardDto, ProblemDomain } from '../../services/judgementCardService';

const { Title, Text } = Typography;
const { Search, TextArea } = Input;
const { Option } = Select;

/**
 * 判断卡管理页面
 * 提供判断卡的 CRUD、版本管理、质量评分等功能
 */
const JudgementCardList: React.FC = () => {
  const [cards, setCards] = useState<JudgementCardDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedDomain, setSelectedDomain] = useState<string | undefined>();
  const [modalVisible, setModalVisible] = useState(false);
  const [editingCard, setEditingCard] = useState<JudgementCardDto | null>(null);
  const [form] = Form.useForm();
  const navigate = useNavigate();

  const domainMap: Record<string, { label: string; color: string }> = {
    A: { label: '机械/动作', color: 'blue' },
    B: { label: '电气/IO', color: 'green' },
    C: { label: 'PLC/程序', color: 'orange' },
    D: { label: '测试/判定', color: 'purple' },
    E: { label: '系统/环境', color: 'red' },
  };

  useEffect(() => {
    loadCards();
  }, [page, pageSize, searchQuery, selectedDomain]);

  const loadCards = async () => {
    setLoading(true);
    try {
      const result = await judgementCardService.getCards({
        page,
        pageSize,
        searchQuery,
        domain: selectedDomain as ProblemDomain,
      });
      setCards(result.items);
      setTotal(result.total);
    } catch (error) {
      console.error('Failed to load cards:', error);
      message.error('加载判断卡列表失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingCard(null);
    form.resetFields();
    setModalVisible(true);
  };

  const handleEdit = (card: JudgementCardDto) => {
    setEditingCard(card);
    form.setFieldsValue({
      ...card,
      procedureSteps: card.procedureSteps?.join(', '),
    });
    setModalVisible(true);
  };

  const handleView = (cardId: string) => {
    // TODO: 跳转到详情页
    message.info('查看详情功能开发中...');
  };

  const handleCopy = async (card: JudgementCardDto) => {
    try {
      await judgementCardService.copyCard(card.cardId, `${card.cardTitle} - 副本`);
      message.success('复制成功');
      loadCards();
    } catch (error) {
      console.error('Failed to copy card:', error);
      message.error('复制失败');
    }
  };

  const handleDelete = async (cardId: string) => {
    try {
      await judgementCardService.deleteCard(cardId);
      message.success('删除成功');
      loadCards();
    } catch (error) {
      console.error('Failed to delete card:', error);
      message.error('删除失败');
    }
  };

  const handleViewVersionHistory = (cardId: string) => {
    navigate(`/judgement-cards/version?cardId=${cardId}`);
  };

  const handleViewQuality = (cardId: string) => {
    navigate(`/judgement-cards/quality?cardId=${cardId}`);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      // 处理步骤字符串转数组
      if (values.procedureSteps) {
        values.procedureSteps = values.procedureSteps.split(',').map((s: string) => s.trim());
      }

      // 处理置信度：Rate组件返回1-5，需要转换为0-1
      if (values.confidence) {
        values.confidence = values.confidence / 5;
      }

      if (editingCard) {
        // 更新
        await judgementCardService.updateCard(editingCard.cardId, values);
        message.success('更新成功');
      } else {
        // 创建
        await judgementCardService.createCard(values);
        message.success('创建成功');
      }

      setModalVisible(false);
      loadCards();
    } catch (error) {
      console.error('Failed to submit:', error);
      message.error(editingCard ? '更新失败' : '创建失败');
    }
  };

  const columns = [
    {
      title: '卡片标题',
      dataIndex: 'cardTitle',
      key: 'cardTitle',
      width: 250,
      fixed: 'left' as const,
      render: (title: string, record: JudgementCardDto) => (
        <Space direction="vertical" size={0}>
          <Text strong>{title}</Text>
          <Text type="secondary" style={{ fontSize: 12 }}>
            ID: {record.cardId}
          </Text>
        </Space>
      ),
    },
    {
      title: '问题域',
      dataIndex: 'domain',
      key: 'domain',
      width: 120,
      render: (domain: string) => {
        const info = domainMap[domain] || { label: domain, color: 'default' };
        return <Tag color={info.color}>{info.label}</Tag>;
      },
    },
    {
      title: '适用步骤',
      dataIndex: 'procedureSteps',
      key: 'procedureSteps',
      width: 150,
      render: (steps: string[]) => (
        <Space size={4} wrap>
          {steps?.map((step, idx) => (
            <Tag key={idx} color="default" style={{ fontSize: 11 }}>
              {step}
            </Tag>
          ))}
        </Space>
      ),
    },
    {
      title: '版本范围',
      key: 'versionRange',
      width: 150,
      render: (_: any, record: JudgementCardDto) => (
        <Space direction="vertical" size={0}>
          {record.plcVersionRange && (
            <Text style={{ fontSize: 11 }}>PLC: {record.plcVersionRange}</Text>
          )}
          {record.uiVersionRange && (
            <Text style={{ fontSize: 11 }}>UI: {record.uiVersionRange}</Text>
          )}
        </Space>
      ),
    },
    {
      title: '置信度',
      dataIndex: 'confidence',
      key: 'confidence',
      width: 100,
      sorter: (a, b) => a.confidence - b.confidence,
      render: (confidence: number) => (
        <Tooltip title={`置信度: ${(confidence * 100).toFixed(0)}%`}>
          <Rate
            disabled
            allowHalf
            value={(confidence * 5)}
            style={{ fontSize: 14 }}
          />
        </Tooltip>
      ),
    },
    {
      title: '质量评分',
      dataIndex: 'qualityScore',
      key: 'qualityScore',
      width: 100,
      sorter: (a, b) => (a.qualityScore || 0) - (b.qualityScore || 0),
      render: (score: number) => (
        <Space>
          <StarOutlined style={{ color: '#faad14' }} />
          <Text>{score ? score.toFixed(1) : '-'}</Text>
        </Space>
      ),
    },
    {
      title: '使用次数',
      dataIndex: 'usageCount',
      key: 'usageCount',
      width: 100,
      sorter: (a, b) => a.usageCount - b.usageCount,
    },
    {
      title: '成功率',
      dataIndex: 'successRate',
      key: 'successRate',
      width: 100,
      sorter: (a, b) => (a.successRate || 0) - (b.successRate || 0),
      render: (rate: number) => (
        <Text>{rate ? `${(rate * 100).toFixed(0)}%` : '-'}</Text>
      ),
    },
    {
      title: '创建人',
      dataIndex: 'createdByName',
      key: 'createdByName',
      width: 100,
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 80,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'success' : 'default'}>
          {isActive ? '启用' : '停用'}
        </Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 260,
      fixed: 'right' as const,
      render: (_: any, record: JudgementCardDto) => (
        <Space size="small">
          <Tooltip title="查看详情">
            <Button
              type="link"
              size="small"
              icon={<EyeOutlined />}
              onClick={() => handleView(record.cardId)}
            />
          </Tooltip>
          <Tooltip title="版本历史">
            <Button
              type="link"
              size="small"
              icon={<HistoryOutlined />}
              onClick={() => handleViewVersionHistory(record.cardId)}
            />
          </Tooltip>
          <Tooltip title="质量评估">
            <Button
              type="link"
              size="small"
              icon={<StarOutlined />}
              onClick={() => handleViewQuality(record.cardId)}
            />
          </Tooltip>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
          <Tooltip title="复制">
            <Button
              type="link"
              size="small"
              icon={<CopyOutlined />}
              onClick={() => handleCopy(record)}
            />
          </Tooltip>
          <Popconfirm
            title="确认删除?"
            description="删除后将无法恢复，确定要删除吗？"
            onConfirm={() => handleDelete(record.cardId)}
            okText="确定"
            cancelText="取消"
          >
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
            >
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      {/* 页面头部 */}
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2} style={{ margin: 0 }}>
          判断卡管理
        </Title>
        <Space>
          <Select
            placeholder="问题域筛选"
            allowClear
            style={{ width: 150 }}
            onChange={setSelectedDomain}
            value={selectedDomain}
          >
            {Object.entries(domainMap).map(([key, value]) => (
              <Option key={key} value={key}>
                <Tag color={value.color}>{value.label}</Tag>
              </Option>
            ))}
          </Select>
          <Search
            placeholder="搜索卡片标题或内容"
            allowClear
            style={{ width: 300 }}
            onSearch={setSearchQuery}
            enterButton={<SearchOutlined />}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            新增判断卡
          </Button>
        </Space>
      </div>

      {/* 判断卡列表表格 */}
      <Table
        columns={columns}
        dataSource={cards}
        rowKey="cardId"
        loading={loading}
        scroll={{ x: 1800 }}
        pagination={{
          current: page,
          pageSize: pageSize,
          total: total,
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 张判断卡`,
          onChange: (page, pageSize) => {
            setPage(page);
            setPageSize(pageSize);
          },
        }}
      />

      {/* 新增/编辑判断卡对话框 */}
      <Modal
        title={editingCard ? '编辑判断卡' : '新增判断卡'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={800}
        okText="保存"
        cancelText="取消"
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ isActive: true, confidence: 0.5 }}
        >
          <Form.Item
            label="卡片标题"
            name="cardTitle"
            rules={[{ required: true, message: '请输入卡片标题' }]}
          >
            <Input placeholder="如: 伺服电机不响应" />
          </Form.Item>

          <Form.Item
            label="卡片内容"
            name="cardContent"
            rules={[{ required: true, message: '请输入卡片内容' }]}
          >
            <TextArea
              placeholder="详细描述问题现象、排查步骤、解决方案..."
              rows={6}
            />
          </Form.Item>

          <Form.Item
            label="问题域"
            name="domain"
            rules={[{ required: true, message: '请选择问题域' }]}
          >
            <Select placeholder="请选择问题域">
              {Object.entries(domainMap).map(([key, value]) => (
                <Option key={key} value={key}>
                  <Tag color={value.color}>{value.label}</Tag>
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            label="适用步骤"
            name="procedureSteps"
            help="多个步骤用逗号分隔，如: 工位1, 工位2"
          >
            <Input placeholder="工位1, 工位2" />
          </Form.Item>

          <Form.Item label="PLC版本范围" name="plcVersionRange">
            <Input placeholder="如: >=2.0.0" />
          </Form.Item>

          <Form.Item label="UI版本范围" name="uiVersionRange">
            <Input placeholder="如: ^1.5.0" />
          </Form.Item>

          <Form.Item label="硬件版本范围" name="hwVersionRange">
            <Input placeholder="如: >=3.0.0" />
          </Form.Item>

          <Form.Item
            label="置信度"
            name="confidence"
            rules={[{ required: true, message: '请设置置信度' }]}
          >
            <Rate allowHalf count={5} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default JudgementCardList;
