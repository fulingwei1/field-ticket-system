"use client";

/**
 * AI增强的问诊式补全缺失信息组件
 * 支持AI深度分析和多轮对话
 */
import React, { useState, useEffect } from 'react';
import {
  Card,
  Form,
  Radio,
  Input,
  InputNumber,
  Select,
  Upload,
  Button,
  message,
  Space,
  Alert,
  Spin,
  Tag,
  Divider,
  Switch,
  Typography,
  Timeline,
  Collapse,
  List,
  Descriptions,
} from 'antd';
import {
  UploadOutlined,
  RobotOutlined,
  MessageOutlined,
  CheckCircleOutlined,
  ReloadOutlined,
  ArrowRightOutlined,
} from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import {
  aiDeepAnalysisService,
  DeepAnalysisResult,
  ConversationResult,
  PersonalizedQuestion,
  ConversationHistoryItem,
} from '../../services/aiDeepAnalysisService';
import { ticketService, type CompleteMissingInfoRequest } from '../../services/ticketService';

const { TextArea } = Input;
const { Text, Paragraph } = Typography;
const { Panel } = Collapse;

interface AIEnhancedMissingInfoQuestionnaireProps {
  ticketId: string;
  jcCode?: string;
  onComplete?: (ticketId: string) => void;
  onSkip?: () => void;
  enableAI?: boolean; // 是否启用AI深度分析
}

export const AIEnhancedMissingInfoQuestionnaire: React.FC<AIEnhancedMissingInfoQuestionnaireProps> = ({
  ticketId,
  jcCode,
  onComplete,
  onSkip,
  enableAI = true,
}) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [aiEnabled, setAiEnabled] = useState(enableAI);
  const [aiAnalyzing, setAiAnalyzing] = useState(false);
  const [aiAnalysisResult, setAiAnalysisResult] = useState<DeepAnalysisResult | null>(null);
  const [conversationMode, setConversationMode] = useState(false);
  const [conversationResult, setConversationResult] = useState<ConversationResult | null>(null);
  const [conversationHistory, setConversationHistory] = useState<ConversationHistoryItem[]>([]);
  const [currentQuestion, setCurrentQuestion] = useState<PersonalizedQuestion | null>(null);
  const [currentAnswer, setCurrentAnswer] = useState<string>('');
  const [collectedAnswers, setCollectedAnswers] = useState<Record<string, any>>({});

  useEffect(() => {
    if (aiEnabled) {
      loadAIAnalysis();
    } else {
      loadTraditionalMissingInfo();
    }
  }, [ticketId, jcCode, aiEnabled]);

  /**
   * 加载AI深度分析
   */
  const loadAIAnalysis = async () => {
    try {
      setAiAnalyzing(true);
      const result = await aiDeepAnalysisService.analyzeTicket(ticketId, jcCode);
      setAiAnalysisResult(result);

      // 显示分析结果，让用户选择是否进入对话模式

      // 加载对话历史
      await loadConversationHistory();
    } catch (error: any) {
      console.error('AI分析失败，降级到传统模式:', error);
      message.warning('AI分析失败，已切换到传统模式');
      setAiEnabled(false);
      loadTraditionalMissingInfo();
    } finally {
      setAiAnalyzing(false);
    }
  };

  /**
   * 加载传统缺失信息（降级方案）
   */
  const loadTraditionalMissingInfo = async () => {
    try {
      setLoading(true);
      const result = await ticketService.getMissingInfo(ticketId, jcCode);
      // 这里可以转换为 PersonalizedQuestion 格式
      const questions: PersonalizedQuestion[] = result.questions.map((q) => ({
        questionId: q.questionId,
        question: q.question,
        type: q.type,
        required: q.required || false,
        priority: q.isCritical ? 5 : 3,
        options: q.options,
      }));

      if (questions.length > 0) {
        setConversationMode(true);
        setCurrentQuestion(questions[0]);
      }
    } catch (error: any) {
      message.error(error.message || '加载缺失信息失败');
    } finally {
      setLoading(false);
    }
  };

  /**
   * 加载对话历史
   */
  const loadConversationHistory = async () => {
    try {
      const history = await aiDeepAnalysisService.getConversationHistory(ticketId);
      setConversationHistory(history);
    } catch (error: any) {
      console.error('加载对话历史失败:', error);
    }
  };

  /**
   * 提交当前问题的答案并继续对话
   */
  const handleAnswerSubmit = async () => {
    if (!currentQuestion || !currentAnswer.trim()) {
      message.warning('请先回答问题');
      return;
    }

    try {
      setLoading(true);
      const result = await aiDeepAnalysisService.continueConversation(ticketId, {
        questionId: currentQuestion.questionId,
        userAnswer: currentAnswer,
      });

      // 保存答案
      setCollectedAnswers((prev) => ({
        ...prev,
        [currentQuestion.questionId]: currentAnswer,
      }));

      setConversationResult(result);

      if (result.isComplete) {
        // 对话完成，提交所有收集的信息
        await submitAllAnswers();
      } else if (result.nextQuestion) {
        // 继续下一个问题
        setCurrentQuestion(result.nextQuestion);
        setCurrentAnswer('');
        await loadConversationHistory();
      } else {
        // 没有下一个问题，完成对话
        message.info('所有问题已回答，完成补全');
        await submitAllAnswers();
      }
    } catch (error: any) {
      message.error(error.message || '提交答案失败');
    } finally {
      setLoading(false);
    }
  };

  /**
   * 提交所有收集的答案
   */
  const submitAllAnswers = async () => {
    try {
      setLoading(true);
      const request: CompleteMissingInfoRequest = {
        answers: collectedAnswers,
      };

      await ticketService.completeMissingInfo(ticketId, request);
      message.success('信息补全成功');

      if (onComplete) {
        onComplete(ticketId);
      }
    } catch (error: any) {
      message.error(error.message || '补全信息失败');
    } finally {
      setLoading(false);
    }
  };

  /**
   * 跳过当前问题
   */
  const handleSkipQuestion = () => {
    if (!currentQuestion) return;

    if (currentQuestion.required) {
      message.warning('此问题为必填项，不能跳过');
      return;
    }

    // 标记为跳过
    setCollectedAnswers((prev) => ({
      ...prev,
      [currentQuestion.questionId]: null,
    }));

    // 继续下一个问题或完成
    if (conversationResult && conversationResult.nextQuestion) {
      setCurrentQuestion(conversationResult.nextQuestion);
      setCurrentAnswer('');
    } else {
      submitAllAnswers();
    }
  };

  /**
   * 渲染问题输入
   */
  const renderQuestionInput = (question: PersonalizedQuestion) => {
    const { questionId, question: questionText, type: questionType, required: isRequired, options: suggestedAnswers } = question;

    switch (questionType) {
      case 'yes_no':
        return (
          <Form.Item
            name="answer"
            label={questionText}
            rules={isRequired ? [{ required: true, message: '请选择' }] : []}
          >
            <Radio.Group
              value={currentAnswer}
              onChange={(e) => setCurrentAnswer(e.target.value)}
            >
              <Radio value="yes">是</Radio>
              <Radio value="no">否</Radio>
              <Radio value="unknown">不清楚</Radio>
            </Radio.Group>
          </Form.Item>
        );

      case 'number':
        return (
          <Form.Item
            name="answer"
            label={questionText}
            rules={isRequired ? [{ required: true, message: '请输入数字' }] : []}
          >
            <InputNumber
              style={{ width: '100%' }}
              placeholder="请输入数字"
              value={currentAnswer ? Number(currentAnswer) : undefined}
              onChange={(value) => setCurrentAnswer(value?.toString() || '')}
            />
          </Form.Item>
        );

      case 'text':
        return (
          <Form.Item
            name="answer"
            label={questionText}
            rules={isRequired ? [{ required: true, message: '请输入' }] : []}
          >
            <TextArea
              rows={4}
              placeholder="请输入文本"
              value={currentAnswer}
              onChange={(e) => setCurrentAnswer(e.target.value)}
            />
          </Form.Item>
        );

      case 'select':
        return (
          <Form.Item
            name="answer"
            label={questionText}
            rules={isRequired ? [{ required: true, message: '请选择' }] : []}
          >
            <Select
              placeholder="请选择"
              value={currentAnswer || undefined}
              onChange={(value) => setCurrentAnswer(value)}
              options={suggestedAnswers?.map((opt) => ({ label: opt, value: opt }))}
            />
          </Form.Item>
        );

      default:
        return null;
    }
  };

  if (aiAnalyzing) {
    return (
      <Card>
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Spin size="large" tip="AI正在深度分析工单内容..." />
        </div>
      </Card>
    );
  }

  // AI分析结果展示
  if (aiAnalysisResult && !conversationMode) {
    return (
      <Card
        title={
          <Space>
            <RobotOutlined />
            <span>AI深度分析结果</span>
            <Switch
              checked={aiEnabled}
              onChange={setAiEnabled}
              checkedChildren="AI模式"
              unCheckedChildren="传统模式"
            />
          </Space>
        }
      >
        {aiAnalysisResult.analysisSummary && (
          <Alert
            message="分析摘要"
            description={aiAnalysisResult.analysisSummary}
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />
        )}

        <Collapse defaultActiveKey={['implicit']}>
          {aiAnalysisResult.implicitMissing.length > 0 && (
            <Panel header={`隐含缺失信息 (${aiAnalysisResult.implicitMissing.length})`} key="implicit">
              <List
                dataSource={aiAnalysisResult.implicitMissing}
                renderItem={(item) => (
                  <List.Item>
                    <Space direction="vertical" style={{ width: '100%' }}>
                      <Text strong>{item.description}</Text>
                      <Space>
                        <Tag color={item.priority === 'high' ? 'red' : item.priority === 'medium' ? 'orange' : 'blue'}>
                          {item.priority}
                        </Tag>
                        {item.reason && <Text type="secondary">{item.reason}</Text>}
                      </Space>
                    </Space>
                  </List.Item>
                )}
              />
            </Panel>
          )}

          {aiAnalysisResult.personalizedQuestions.length > 0 && (
            <Panel header={`个性化问题 (${aiAnalysisResult.personalizedQuestions.length})`} key="questions">
              <List
                dataSource={aiAnalysisResult.personalizedQuestions}
                renderItem={(item, index) => (
                  <List.Item>
                    <Space direction="vertical" style={{ width: '100%' }}>
                      <Text strong>{item.question}</Text>
                      <Space>
                        <Tag color={item.priority >= 4 ? 'red' : item.priority >= 3 ? 'orange' : 'blue'}>
                          优先级: {item.priority}
                        </Tag>
                        {item.required && <Tag color="red">必填</Tag>}
                        {item.personalizationReason && (
                          <Text type="secondary">{item.personalizationReason}</Text>
                        )}
                      </Space>
                    </Space>
                  </List.Item>
                )}
              />
              <Button
                type="primary"
                icon={<MessageOutlined />}
                onClick={() => {
                  setConversationMode(true);
                  setCurrentQuestion(aiAnalysisResult.personalizedQuestions[0]);
                }}
                style={{ marginTop: 16 }}
              >
                开始对话式补全
              </Button>
            </Panel>
          )}
        </Collapse>
      </Card>
    );
  }

  // 对话模式
  if (conversationMode && currentQuestion) {
    return (
      <Card
        title={
          <Space>
            <MessageOutlined />
            <span>AI对话式信息补全</span>
            {conversationHistory.length > 0 && (
              <Tag color="blue">第 {conversationHistory.length + 1} 轮</Tag>
            )}
          </Space>
        }
        extra={
          <Space>
            <Switch
              checked={aiEnabled}
              onChange={setAiEnabled}
              checkedChildren="AI"
              unCheckedChildren="传统"
              size="small"
            />
            {onSkip && (
              <Button size="small" onClick={onSkip}>
                跳过
              </Button>
            )}
          </Space>
        }
      >
        {/* 对话历史 */}
        {conversationHistory.length > 0 && (
          <Collapse style={{ marginBottom: 16 }}>
            <Panel header={`对话历史 (${conversationHistory.length} 条)`} key="history">
              <Timeline>
                {conversationHistory.map((item, index) => (
                  <Timeline.Item key={index}>
                    <Space direction="vertical" size="small">
                      <Text strong>{item.question}</Text>
                      {item.isAnswered && item.userAnswer && (
                        <Text type="secondary">回答: {item.userAnswer}</Text>
                      )}
                      {item.isSkipped && <Tag color="default">已跳过</Tag>}
                    </Space>
                  </Timeline.Item>
                ))}
              </Timeline>
            </Panel>
          </Collapse>
        )}

        {/* 当前问题 */}
        <Card
          type="inner"
          title={
            <Space>
              <span>{currentQuestion.question}</span>
              {currentQuestion.required && <Tag color="red">必填</Tag>}
              <Tag color={currentQuestion.priority >= 4 ? 'red' : currentQuestion.priority >= 3 ? 'orange' : 'blue'}>
                优先级: {currentQuestion.priority}
              </Tag>
            </Space>
          }
        >
          <Form form={form} layout="vertical">
            {renderQuestionInput(currentQuestion)}

            {currentQuestion.hint && (
              <Alert
                message="提示"
                description={currentQuestion.hint}
                type="info"
                showIcon
                style={{ marginTop: 16 }}
              />
            )}
            {currentQuestion.personalizationReason && (
              <Alert
                message="个性化原因"
                description={currentQuestion.personalizationReason}
                type="info"
                showIcon
                style={{ marginTop: 16 }}
              />
            )}

            <Form.Item style={{ marginTop: 16 }}>
              <Space>
                <Button
                  type="primary"
                  icon={<ArrowRightOutlined />}
                  onClick={handleAnswerSubmit}
                  loading={loading}
                >
                  提交并继续
                </Button>
                {!currentQuestion.required && (
                  <Button onClick={handleSkipQuestion}>跳过</Button>
                )}
              </Space>
            </Form.Item>
          </Form>
        </Card>

        {/* 已收集信息摘要 */}
        {Object.keys(collectedAnswers).length > 0 && (
          <Card type="inner" title="已收集信息" style={{ marginTop: 16 }}>
            <Descriptions column={1} size="small">
              {Object.entries(collectedAnswers).map(([key, value]) => (
                <Descriptions.Item key={key} label={key}>
                  {value?.toString() || '-'}
                </Descriptions.Item>
              ))}
            </Descriptions>
          </Card>
        )}

        {/* 完成提示 */}
        {conversationResult?.isComplete && (
          <Alert
            message="对话完成"
            description="所有必要信息已收集，点击下方按钮完成补全"
            type="success"
            showIcon
            style={{ marginTop: 16 }}
            action={
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={submitAllAnswers}
                loading={loading}
              >
                完成补全
              </Button>
            }
          />
        )}
      </Card>
    );
  }

  // 没有缺失信息
  return (
    <Card>
      <Alert message="没有缺失信息，可以直接提交工单" type="success" showIcon />
    </Card>
  );
};

