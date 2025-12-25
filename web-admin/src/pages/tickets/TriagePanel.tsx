"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Form,
  Input,
  Select,
  Button,
  Space,
  message,
  Spin,
  Descriptions,
  Tag,
  Alert,
  Rate,
} from 'antd';
import { ArrowLeftOutlined, CheckOutlined } from '@ant-design/icons';
import { triageService, JudgementCardDto, TriageTicketRequest } from '../../services/triageService';
import { ticketService, TicketDto } from '../../services/ticketService';
import { judgementCardRecommendationService, JudgementCardRecommendationDto } from '../../services/judgementCardRecommendationService';
import AIAssistedTriagePanel from '../../components/ai/AIAssistedTriagePanel';
import { AIAssistedTriageResult } from '../../services/aiAssistedTriageService';
import AIAssistedTriagePanel from '../../components/ai/AIAssistedTriagePanel';
import { AIAssistedTriageResult } from '../../services/aiAssistedTriageService';

const { TextArea } = Input;
const { Option } = Select;

export default function TriagePanel() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [ticket, setTicket] = useState<TicketDto | null>(null);
  const [judgementCards, setJudgementCards] = useState<JudgementCardDto[]>([]);
  const [selectedJc, setSelectedJc] = useState<JudgementCardDto | null>(null);
  const [loadingTicket, setLoadingTicket] = useState(true);
  const [loadingJc, setLoadingJc] = useState(true);
  const [recommendations, setRecommendations] = useState<JudgementCardRecommendationDto[]>([]);
  const [loadingRecommendations, setLoadingRecommendations] = useState(false);

  useEffect(() => {
    loadTicket();
    loadJudgementCards();
  }, [ticketId]);

  useEffect(() => {
    if (ticket) {
      loadRecommendations();
    }
  }, [ticket]);

  const loadTicket = async () => {
    if (!ticketId) return;
    try {
      setLoadingTicket(true);
      const ticketData = await ticketService.getTicket(ticketId);
      setTicket(ticketData);
      
      // 验证工单状态
      if (ticketData.status !== 'Submitted') {
        message.warning('只能对 Submitted 状态的工单进行分诊');
        navigate('/tickets');
        return;
      }
    } catch (error: any) {
      message.error(error.message || '加载工单失败');
      navigate('/tickets');
    } finally {
      setLoadingTicket(false);
    }
  };

  const loadJudgementCards = async () => {
    try {
      setLoadingJc(true);
      const cards = await triageService.getJudgementCards({
        domain: ticket?.domain,
        status: 'Active',
      });
      setJudgementCards(cards);
    } catch (error: any) {
      message.error(error.message || '加载判断卡列表失败');
    } finally {
      setLoadingJc(false);
    }
  };

  const loadRecommendations = async () => {
    if (!ticket) return;
    try {
      setLoadingRecommendations(true);
      const response = await judgementCardRecommendationService.recommendJudgementCards({
        domain: ticket.domain,
        stepCode: ticket.stepCode,
        symptomTitle: ticket.symptomTitle,
        topK: 5,
      });
      setRecommendations(response.recommendations);
    } catch (error: any) {
      console.error('Failed to load recommendations:', error);
      // 不显示错误，推荐功能是可选的
    } finally {
      setLoadingRecommendations(false);
    }
  };

  const handleJcChange = async (jcCode: string) => {
    try {
      const jc = await triageService.getJudgementCard(jcCode);
      setSelectedJc(jc);
      
      // 如果判断卡有模板，自动填充
      if (jc.hypothesisTemplate) {
        form.setFieldsValue({ currentHypothesis: jc.hypothesisTemplate });
      }
      if (jc.nextActionTemplate) {
        form.setFieldsValue({ nextAction: jc.nextActionTemplate });
      }
    } catch (error: any) {
      message.error(error.message || '加载判断卡详情失败');
    }
  };

  const handleSubmit = async (values: TriageTicketRequest) => {
    if (!ticketId) return;
    
    try {
      setLoading(true);
      const result = await triageService.triageTicket(ticketId, {
        jcCode: values.jcCode,
        currentHypothesis: values.currentHypothesis,
        nextAction: values.nextAction,
        confidence: values.confidence,
        note: values.note,
      });

      if (result.escalationRequired) {
        message.warning('分诊完成，但置信度较低，已自动升级');
      } else {
        message.success('分诊完成');
      }

      // 返回工单列表
      navigate('/tickets');
    } catch (error: any) {
      message.error(error.message || '分诊失败');
    } finally {
      setLoading(false);
    }
  };

  if (loadingTicket) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!ticket) {
    return null;
  }

  return (
    <div style={{ padding: '24px' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 返回按钮 */}
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/tickets')}
        >
          返回工单列表
        </Button>

        {/* 工单信息 */}
        <Card title="工单信息">
          <Descriptions column={2} bordered>
            <Descriptions.Item label="工单编号">{ticket.ticketNo || '未生成'}</Descriptions.Item>
            <Descriptions.Item label="状态">
              <Tag color={ticket.status === 'Submitted' ? 'blue' : 'default'}>
                {ticket.status}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="问题域">{ticket.domain}</Descriptions.Item>
            <Descriptions.Item label="步骤代码">{ticket.stepCode}</Descriptions.Item>
            <Descriptions.Item label="症状标题" span={2}>
              {ticket.symptomTitle}
            </Descriptions.Item>
            {ticket.symptomDetail && (
              <Descriptions.Item label="症状详情" span={2}>
                {ticket.symptomDetail}
              </Descriptions.Item>
            )}
            <Descriptions.Item label="软件版本">{ticket.swVersion}</Descriptions.Item>
            <Descriptions.Item label="PLC版本">{ticket.plcVersion}</Descriptions.Item>
          </Descriptions>
        </Card>

        {/* 事实表 */}
        {ticket.factsJson && Object.keys(ticket.factsJson).length > 0 && (
          <Card title="事实表">
            <pre style={{ background: '#f5f5f5', padding: '16px', borderRadius: '4px' }}>
              {JSON.stringify(ticket.factsJson, null, 2)}
            </pre>
          </Card>
        )}

        {/* AI辅助分诊面板 */}
        {ticketId && (
          <AIAssistedTriagePanel
            ticketId={ticketId}
            jcCode={selectedJc?.jcCode}
            onAcceptRecommendation={(result) => {
              if (result.recommendedJcCode) {
                form.setFieldsValue({ jcCode: result.recommendedJcCode });
                handleJcChange(result.recommendedJcCode);
              }
              if (result.recommendedHypothesis) {
                form.setFieldsValue({ currentHypothesis: result.recommendedHypothesis });
              }
              if (result.recommendedNextAction) {
                form.setFieldsValue({ nextAction: result.recommendedNextAction });
              }
              if (result.confidence) {
                form.setFieldsValue({ confidence: result.confidence });
              }
            }}
          />
        )}

        {/* AI推荐判断卡 */}
        {recommendations.length > 0 && (
          <Card
            title="AI推荐判断卡"
            extra={
              <Button
                size="small"
                icon={<ReloadOutlined />}
                onClick={loadRecommendations}
                loading={loadingRecommendations}
              >
                刷新推荐
              </Button>
            }
          >
            <List
              size="small"
              dataSource={recommendations}
              renderItem={(rec) => (
                <List.Item
                  style={{
                    backgroundColor: selectedJc?.jcCode === rec.judgementCard.jcCode ? '#f0f0f0' : 'transparent',
                    cursor: 'pointer',
                    padding: '12px',
                    borderRadius: '4px',
                    marginBottom: '8px',
                  }}
                  onClick={() => {
                    form.setFieldsValue({ jcCode: rec.judgementCard.jcCode });
                    handleJcChange(rec.judgementCard.jcCode);
                  }}
                >
                  <List.Item.Meta
                    title={
                      <Space>
                        <span>{rec.judgementCard.jcCode} - {rec.judgementCard.title}</span>
                        <Tag color="blue">匹配度: {(rec.matchScore).toFixed(0)}分</Tag>
                      </Space>
                    }
                    description={
                      <div>
                        {rec.matchReasons.length > 0 && (
                          <div style={{ marginTop: '4px', fontSize: '12px', color: '#666' }}>
                            {rec.matchReasons.map((reason, idx) => (
                              <Tag key={idx} size="small" style={{ marginRight: '4px' }}>
                                {reason}
                              </Tag>
                            ))}
                          </div>
                        )}
                        {rec.judgementCard.usageCount > 0 && (
                          <div style={{ marginTop: '4px', fontSize: '12px', color: '#999' }}>
                            已使用 {rec.judgementCard.usageCount} 次
                          </div>
                        )}
                      </div>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        )}

        {/* 分诊表单 */}
        <Card title="分诊工单">
          <Form
            form={form}
            layout="vertical"
            onFinish={handleSubmit}
            initialValues={{
              confidence: 3,
            }}
          >
            <Form.Item
              name="jcCode"
              label="判断卡（必填，硬规则HR-001）"
              rules={[{ required: true, message: '请选择判断卡' }]}
            >
              <Select
                placeholder="请选择判断卡"
                showSearch
                optionFilterProp="children"
                loading={loadingJc}
                onChange={handleJcChange}
                filterOption={(input, option) =>
                  (option?.children as unknown as string)?.toLowerCase().includes(input.toLowerCase())
                }
              >
                {judgementCards.map((jc) => (
                  <Option key={jc.jcCode} value={jc.jcCode}>
                    {jc.jcCode} - {jc.title}
                    {jc.description && ` (${jc.description})`}
                  </Option>
                ))}
              </Select>
            </Form.Item>

            {selectedJc && (
              <Alert
                message={`已选择判断卡：${selectedJc.title}`}
                description={selectedJc.description || '无描述'}
                type="info"
                style={{ marginBottom: '16px' }}
              />
            )}

            <Form.Item
              name="currentHypothesis"
              label="当前假设"
            >
              <TextArea
                rows={4}
                placeholder="填写当前假设（可选，判断卡模板会自动填充）"
              />
            </Form.Item>

            <Form.Item
              name="nextAction"
              label="下一步动作"
            >
              <TextArea
                rows={4}
                placeholder="填写下一步动作（可选，判断卡模板会自动填充）"
              />
            </Form.Item>

            <Form.Item
              name="confidence"
              label="置信度（1-5，必填）"
              rules={[
                { required: true, message: '请设置置信度' },
                { type: 'number', min: 1, max: 5, message: '置信度必须在1-5之间' },
              ]}
            >
              <Rate count={5} />
            </Form.Item>

            <Alert
              message="置信度说明"
              description="置信度 ≤ 2 时，系统会自动升级工单（硬规则HR-002）"
              type="warning"
              style={{ marginBottom: '16px' }}
            />

            <Form.Item
              name="note"
              label="分诊备注"
            >
              <TextArea
                rows={3}
                placeholder="填写分诊备注（可选）"
              />
            </Form.Item>

            <Form.Item>
              <Space>
                <Button
                  type="primary"
                  htmlType="submit"
                  icon={<CheckOutlined />}
                  loading={loading}
                >
                  提交分诊
                </Button>
                <Button onClick={() => navigate('/tickets')}>
                  取消
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Card>
      </Space>
    </div>
  );
}
