"use client";

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Button,
  Space,
  message,
  Spin,
  Steps,
  Tag,
  Descriptions,
  List,
  Input,
  Alert,
  Progress,
} from 'antd';
import {
  ArrowLeftOutlined,
  CheckOutlined,
  ReloadOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import { conversationalDiagnosisService, DiagnosisConversationDto, HypothesisDto, VerificationStepDto } from '../../services/conversationalDiagnosisService';
import { ticketService } from '../../services/ticketService';
import { ConfidenceBar } from '../../components/common/ConfidenceBar';
import { DiagnosisPathTree } from '../../components/common/DiagnosisPathTree';

const { TextArea } = Input;

export default function ConversationalDiagnosis() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [conversation, setConversation] = useState<DiagnosisConversationDto | null>(null);
  const [hypotheses, setHypotheses] = useState<HypothesisDto[]>([]);
  const [verificationSteps, setVerificationSteps] = useState<VerificationStepDto[]>([]);
  const [selectedHypothesis, setSelectedHypothesis] = useState<string | null>(null);
  const [currentStep, setCurrentStep] = useState(0);
  const [ticketInfo, setTicketInfo] = useState<any>(null);
  const [conversationHistory, setConversationHistory] = useState<Array<{
    round: number;
    hypothesis?: string;
    confidence?: number;
    timestamp: string;
  }>>([]);

  useEffect(() => {
    if (ticketId) {
      loadTicketInfo();
      startConversation();
    }
  }, [ticketId]);

  const loadTicketInfo = async () => {
    if (!ticketId) return;
    try {
      const ticket = await ticketService.getTicket(ticketId);
      setTicketInfo(ticket);
    } catch (error) {
      console.error('Failed to load ticket:', error);
    }
  };

  const startConversation = async () => {
    if (!ticketId) return;
    try {
      setLoading(true);
      const conv = await conversationalDiagnosisService.startConversation(ticketId);
      setConversation(conv);
      await generateHypotheses();
    } catch (error: any) {
      message.error(error.message || '启动诊断对话失败');
    } finally {
      setLoading(false);
    }
  };

  const generateHypotheses = async () => {
    if (!ticketId) return;
    try {
      setLoading(true);
      const hyps = await conversationalDiagnosisService.generateInitialHypotheses(ticketId);
      setHypotheses(hyps);
      if (hyps.length > 0) {
        setSelectedHypothesis(hyps[0].hypothesisId);
      }
    } catch (error: any) {
      message.error(error.message || '生成假设失败');
    } finally {
      setLoading(false);
    }
  };

  const generateSteps = async () => {
    if (!conversation || !selectedHypothesis) return;
    try {
      setLoading(true);
      const steps = await conversationalDiagnosisService.generateVerificationSteps(
        conversation.conversationId,
        selectedHypothesis
      );
      setVerificationSteps(steps);
      setCurrentStep(0);
    } catch (error: any) {
      message.error(error.message || '生成验证步骤失败');
    } finally {
      setLoading(false);
    }
  };

  const submitVerificationResult = async (stepId: string, actualResult: string, notes?: string) => {
    if (!conversation) return;
    try {
      setLoading(true);
      const updated = await conversationalDiagnosisService.submitVerificationResult(conversation.conversationId, {
        stepId,
        actualResult,
        verificationNotes: notes,
      });
      setConversation(updated);
      
      // 更新对话历史
      if (updated.currentHypothesis) {
        setConversationHistory((prev) => [
          ...prev,
          {
            round: updated.conversationRound,
            hypothesis: updated.currentHypothesis,
            confidence: updated.currentConfidence,
            timestamp: new Date().toLocaleString(),
          },
        ]);
      }
      
      // 更新当前步骤状态
      setVerificationSteps((prev) =>
        prev.map((step) =>
          step.stepId === stepId
            ? { ...step, actualResult, verificationNotes: notes, verificationStatus: 'completed' }
            : step
        )
      );

      // 移动到下一步
      const currentIndex = verificationSteps.findIndex((s) => s.stepId === stepId);
      if (currentIndex < verificationSteps.length - 1) {
        setCurrentStep(currentIndex + 1);
      } else {
        message.success('所有验证步骤已完成');
      }
    } catch (error: any) {
      message.error(error.message || '提交验证结果失败');
    } finally {
      setLoading(false);
    }
  };

  const completeDiagnosis = async () => {
    if (!conversation) return;
    try {
      setLoading(true);
      const completed = await conversationalDiagnosisService.completeDiagnosis(conversation.conversationId);
      setConversation(completed);
      message.success('诊断完成');
    } catch (error: any) {
      message.error(error.message || '完成诊断失败');
    } finally {
      setLoading(false);
    }
  };

  const steps = [
    { title: '生成假设', description: 'AI生成初始假设' },
    { title: '验证步骤', description: '生成验证步骤' },
    { title: '执行验证', description: '执行验证并收集结果' },
    { title: '完成诊断', description: '完成诊断流程' },
  ];

  if (loading && !conversation) {
    return (
      <div style={{ padding: '24px', textAlign: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        {/* 头部 */}
        <Card>
          <Space style={{ width: '100%', justifyContent: 'space-between' }}>
            <Space>
              <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
                返回
              </Button>
              <h2 style={{ margin: 0 }}>多轮对话式诊断</h2>
            </Space>
            {conversation && (
              <Tag color={conversation.status === 'completed' ? 'success' : 'processing'}>
                {conversation.status === 'completed' ? '已完成' : '进行中'}
              </Tag>
            )}
          </Space>
        </Card>

        {/* 工单信息 */}
        {ticketInfo && (
          <Card title="工单信息" size="small">
            <Descriptions column={3} size="small">
              <Descriptions.Item label="工单编号">{ticketInfo.ticketNo}</Descriptions.Item>
              <Descriptions.Item label="症状">{ticketInfo.symptomTitle}</Descriptions.Item>
              <Descriptions.Item label="状态">{ticketInfo.status}</Descriptions.Item>
            </Descriptions>
          </Card>
        )}

        {/* 诊断步骤 */}
        <Card title="诊断流程">
          <Steps current={currentStep} items={steps} />
        </Card>

        {/* 假设列表 */}
        {hypotheses.length > 0 && (
          <Card
            title="AI生成的假设"
            extra={
              <Button icon={<ReloadOutlined />} onClick={generateHypotheses} loading={loading}>
                重新生成
              </Button>
            }
          >
            <List
              dataSource={hypotheses}
              renderItem={(hypothesis) => (
                <List.Item
                  style={{
                    backgroundColor: selectedHypothesis === hypothesis.hypothesisId ? '#f0f0f0' : 'transparent',
                    cursor: 'pointer',
                    padding: '16px',
                    borderRadius: '4px',
                    marginBottom: '8px',
                  }}
                  onClick={() => setSelectedHypothesis(hypothesis.hypothesisId)}
                >
                  <List.Item.Meta
                    title={
                      <Space>
                        <span>{hypothesis.description}</span>
                        <ConfidenceBar confidence={hypothesis.confidence} size="small" />
                      </Space>
                    }
                    description={
                      <div>
                        <div style={{ marginTop: '8px' }}>
                          <strong>证据：</strong>
                          {hypothesis.evidence.length > 0 ? (
                            <ul style={{ margin: '4px 0', paddingLeft: '20px' }}>
                              {hypothesis.evidence.map((ev, idx) => (
                                <li key={idx}>{ev}</li>
                              ))}
                            </ul>
                          ) : (
                            <span>无</span>
                          )}
                        </div>
                        {hypothesis.reasoning && (
                          <div style={{ marginTop: '8px' }}>
                            <strong>推理：</strong>
                            <span>{hypothesis.reasoning}</span>
                          </div>
                        )}
                      </div>
                    }
                  />
                </List.Item>
              )}
            />
            {selectedHypothesis && (
              <div style={{ marginTop: '16px' }}>
                <Button type="primary" onClick={generateSteps} loading={loading}>
                  为选中假设生成验证步骤
                </Button>
              </div>
            )}
          </Card>
        )}

        {/* 验证步骤 */}
        {verificationSteps.length > 0 && (
          <Card title="验证步骤">
            <List
              dataSource={verificationSteps}
              renderItem={(step, index) => (
                <List.Item>
                  <Card
                    size="small"
                    style={{ width: '100%' }}
                    title={
                      <Space>
                        <span>步骤 {index + 1}</span>
                        <Tag color={step.verificationStatus === 'completed' ? 'success' : 'default'}>
                          {step.verificationStatus === 'completed' ? '已完成' : '待执行'}
                        </Tag>
                      </Space>
                    }
                  >
                    <Descriptions column={1} size="small">
                      <Descriptions.Item label="描述">{step.stepDescription}</Descriptions.Item>
                      <Descriptions.Item label="类型">{step.stepType}</Descriptions.Item>
                      {step.expectedResult && (
                        <Descriptions.Item label="预期结果">{step.expectedResult}</Descriptions.Item>
                      )}
                      {step.actualResult && (
                        <Descriptions.Item label="实际结果">{step.actualResult}</Descriptions.Item>
                      )}
                      {step.verificationNotes && (
                        <Descriptions.Item label="备注">{step.verificationNotes}</Descriptions.Item>
                      )}
                    </Descriptions>
                    {step.verificationStatus !== 'completed' && (
                      <div style={{ marginTop: '16px' }}>
                        <Space direction="vertical" style={{ width: '100%' }}>
                          <TextArea
                            placeholder="输入实际结果"
                            rows={3}
                            id={`result-${step.stepId}`}
                          />
                          <TextArea
                            placeholder="输入备注（可选）"
                            rows={2}
                            id={`notes-${step.stepId}`}
                          />
                          <Button
                            type="primary"
                            onClick={() => {
                              const result = (document.getElementById(`result-${step.stepId}`) as HTMLTextAreaElement)
                                ?.value;
                              const notes = (document.getElementById(`notes-${step.stepId}`) as HTMLTextAreaElement)
                                ?.value;
                              if (result) {
                                submitVerificationResult(step.stepId, result, notes);
                              } else {
                                message.warning('请输入实际结果');
                              }
                            }}
                          >
                            提交结果
                          </Button>
                        </Space>
                      </div>
                    )}
                  </Card>
                </List.Item>
              )}
            />
            {verificationSteps.every((s) => s.verificationStatus === 'completed') && (
              <div style={{ marginTop: '16px' }}>
                <Button type="primary" size="large" onClick={completeDiagnosis} loading={loading}>
                  完成诊断
                </Button>
              </div>
            )}
          </Card>
        )}

        {/* 对话历史 */}
        {conversationHistory.length > 0 && (
          <Card title="对话历史">
            <List
              size="small"
              dataSource={conversationHistory}
              renderItem={(item) => (
                <List.Item>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <Space>
                      <Tag>轮次 {item.round}</Tag>
                      <span style={{ color: '#999' }}>{item.timestamp}</span>
                    </Space>
                    {item.hypothesis && (
                      <div>
                        <strong>假设：</strong>
                        {item.hypothesis}
                      </div>
                    )}
                    {item.confidence !== undefined && (
                      <ConfidenceBar confidence={item.confidence} size="small" />
                    )}
                  </Space>
                </List.Item>
              )}
            />
          </Card>
        )}

        {/* 诊断路径 */}
        {conversation && conversation.diagnosisPath && (
          <DiagnosisPathTree diagnosisPath={conversation.diagnosisPath} />
        )}

        {/* 当前对话状态 */}
        {conversation && (
          <Card title="对话状态" size="small">
            <Descriptions column={2} size="small">
              <Descriptions.Item label="对话轮数">{conversation.conversationRound}</Descriptions.Item>
              <Descriptions.Item label="状态">{conversation.status}</Descriptions.Item>
              {conversation.currentHypothesis && (
                <Descriptions.Item label="当前假设" span={2}>
                  {conversation.currentHypothesis}
                </Descriptions.Item>
              )}
              {conversation.currentConfidence !== undefined && (
                <Descriptions.Item label="当前置信度">
                  <ConfidenceBar confidence={conversation.currentConfidence} />
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>
        )}
      </Space>
    </div>
  );
}

