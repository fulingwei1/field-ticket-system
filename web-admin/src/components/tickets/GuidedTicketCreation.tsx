import React, { useState, useEffect } from 'react';
import {
  Card,
  Form,
  Input,
  Button,
  Upload,
  message,
  Spin,
  Space,
  Typography,
  Alert,
  Radio,
  Select,
  Divider,
  Tag,
  List,
  Image,
} from 'antd';
import {
  UploadOutlined,
  SendOutlined,
  RobotOutlined,
  UserOutlined,
  CheckCircleOutlined,
  LoadingOutlined,
} from '@ant-design/icons';
import { guidedTicketCreationService, GuidedTicketCreationSession, GuidedQuestion, GuidedQuestionResponse, GuidedTicketContent } from '../../services/guidedTicketCreationService';
import { deviceService, DeviceDto } from '../../services/deviceService';

const { TextArea } = Input;
const { Title, Paragraph, Text } = Typography;
const { Option } = Select;

interface GuidedTicketCreationProps {
  onComplete?: (ticketId: string) => void;
  onCancel?: () => void;
}

/**
 * AI引导式工单创建组件
 */
export const GuidedTicketCreation: React.FC<GuidedTicketCreationProps> = ({
  onComplete,
  onCancel,
}) => {
  const [session, setSession] = useState<GuidedTicketCreationSession | null>(null);
  const [loading, setLoading] = useState(false);
  const [currentStep, setCurrentStep] = useState<'initial' | 'guiding' | 'review' | 'complete'>('initial');
  const [form] = Form.useForm();
  const [imageList, setImageList] = useState<File[]>([]);
  const [devices, setDevices] = useState<DeviceDto[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string>('');
  const [ticketContent, setTicketContent] = useState<GuidedTicketContent | null>(null);

  // 初始化会话
  useEffect(() => {
    initializeSession();
    loadDevices();
    
    // 定期检查会话有效性（每5分钟）
    const sessionCheckInterval = setInterval(async () => {
      if (session && session.sessionId) {
        try {
          await guidedTicketCreationService.getSession(session.sessionId);
        } catch (error: any) {
          console.warn('会话检查失败，会话可能已过期:', error);
          // 如果会话不存在，重新创建
          if (error.status === 404 || error.message?.includes('404') || error.message?.includes('Not Found')) {
            console.log('会话已失效，重新创建...');
            const newSession = await initializeSession();
            if (newSession) {
              message.info('会话已自动更新');
            }
          }
        }
      }
    }, 5 * 60 * 1000); // 5分钟
    
    return () => {
      clearInterval(sessionCheckInterval);
    };
  }, [session]);

  const initializeSession = async (): Promise<GuidedTicketCreationSession | null> => {
    try {
      setLoading(true);
      const newSession = await guidedTicketCreationService.createSession();
      console.log('会话创建成功:', newSession);
      
      // 确保sessionId存在
      if (!newSession || !newSession.sessionId) {
        throw new Error('会话创建失败：返回数据格式不正确');
      }
      
      setSession(newSession);
      return newSession;
    } catch (error: any) {
      console.error('初始化会话失败:', error);
      const errorMessage = error.message || '未知错误';
      const status = (error as any)?.status;
      
      if (status === 401 || errorMessage.includes('401') || errorMessage.includes('Unauthorized')) {
        message.error('请先登录后再使用AI引导创建功能');
      } else if (status === 404 || errorMessage.includes('404') || errorMessage.includes('Not Found')) {
        message.error('API端点未找到，请检查后端服务是否正常运行');
      } else {
        message.error('初始化会话失败：' + errorMessage);
      }
      return null;
    } finally {
      setLoading(false);
    }
  };

  const loadDevices = async () => {
    try {
      // deviceService.getDevices 返回 DeviceDto[]，不是分页结果
      const deviceList = await deviceService.getDevices(1, 100);
      setDevices(deviceList);
    } catch (error: any) {
      console.error('Failed to load devices:', error);
      // 设备列表加载失败不影响主流程，只记录错误
    }
  };

  // 提交初始信息
  const handleSubmitInitial = async () => {
    try {
      const values = await form.validateFields(['textDescription']);
      let activeSession = session;
      if (!activeSession) {
        activeSession = await initializeSession();
        if (!activeSession) {
          return;
        }
      }

      setLoading(true);
      const response = await guidedTicketCreationService.submitInitialInfo(
        activeSession.sessionId,
        values.textDescription,
        imageList.length > 0 ? imageList : undefined
      );

      setSession(response.session);
      
      if (response.isComplete) {
        // 信息已完整，直接进入审核阶段
        await handleGenerateContent();
      } else {
        // 需要继续回答问题
        setCurrentStep('guiding');
        form.resetFields(['textDescription']);
        setImageList([]);
      }
    } catch (error: any) {
      console.error('提交失败:', error);
      const errorMessage = error.message || '未知错误';
      const status = (error as any)?.status;
      
      // 如果会话不存在，尝试重新创建
      if (status === 404 || errorMessage.includes('404') || errorMessage.includes('Not Found') || errorMessage.includes('会话')) {
        message.warning('会话已失效，正在重新初始化...');
        const newSession = await initializeSession();
        if (newSession) {
          message.info('会话已重新创建，请重新提交信息');
        } else {
          message.error('无法重新初始化会话，请刷新页面重试');
        }
      } else {
        message.error('提交失败：' + errorMessage);
      }
    } finally {
      setLoading(false);
    }
  };

  // 回答问题
  const handleAnswerQuestion = async (questionId: string, answer: string, additionalImages?: File[]) => {
    if (!session) {
      message.warning('会话已失效，正在重新初始化...');
      const newSession = await initializeSession();
      if (!newSession) {
        message.error('无法重新初始化会话，请刷新页面重试');
        return;
      }
      message.info('会话已重新创建，请重新提交信息');
      return;
    }

    try {
      setLoading(true);
      const response = await guidedTicketCreationService.answerQuestion(
        session.sessionId,
        questionId,
        answer,
        additionalImages
      );

      setSession(response.session);

      if (response.isComplete) {
        // 信息已完整，生成工单内容
        await handleGenerateContent();
      }
      // 否则继续显示下一个问题
    } catch (error: any) {
      console.error('回答失败:', error);
      const errorMessage = error.message || '未知错误';
      const status = (error as any)?.status;
      
      // 如果会话不存在，尝试重新创建
      if (status === 404 || errorMessage.includes('404') || errorMessage.includes('Not Found') || errorMessage.includes('会话')) {
        message.warning('会话已失效，正在重新初始化...');
        const newSession = await initializeSession();
        if (newSession) {
          message.info('会话已重新创建，请重新提交信息');
          setCurrentStep('initial');
        } else {
          message.error('无法重新初始化会话，请刷新页面重试');
        }
      } else {
        message.error('回答失败：' + errorMessage);
      }
    } finally {
      setLoading(false);
    }
  };

  // 生成工单内容
  const handleGenerateContent = async () => {
    if (!session) {
      message.warning('会话已失效，正在重新初始化...');
      const newSession = await initializeSession();
      if (!newSession) {
        message.error('无法重新初始化会话，请刷新页面重试');
        return;
      }
      message.info('会话已重新创建，请重新提交信息');
      setCurrentStep('initial');
      return;
    }

    try {
      setLoading(true);
      const content = await guidedTicketCreationService.generateTicketContent(session.sessionId);
      setTicketContent(content);
      setCurrentStep('review');
    } catch (error: any) {
      console.error('生成工单内容失败:', error);
      const errorMessage = error.message || '未知错误';
      const status = (error as any)?.status;
      
      // 如果会话不存在，尝试重新创建
      if (status === 404 || errorMessage.includes('404') || errorMessage.includes('Not Found') || errorMessage.includes('会话')) {
        message.warning('会话已失效，正在重新初始化...');
        const newSession = await initializeSession();
        if (newSession) {
          message.info('会话已重新创建，请重新提交信息');
          setCurrentStep('initial');
        } else {
          message.error('无法重新初始化会话，请刷新页面重试');
        }
      } else {
        message.error('生成工单内容失败：' + errorMessage);
      }
    } finally {
      setLoading(false);
    }
  };

  // 创建工单
  const handleCreateTicket = async () => {
    if (!session || !selectedDeviceId) {
      if (!session) {
        message.warning('会话已失效，正在重新初始化...');
        const newSession = await initializeSession();
        if (!newSession) {
          message.error('无法重新初始化会话，请刷新页面重试');
          return;
        }
        message.info('会话已重新创建，请重新生成工单内容');
        setCurrentStep('initial');
      } else {
        message.error('请选择设备');
      }
      return;
    }

    try {
      setLoading(true);
      const ticket = await guidedTicketCreationService.createTicketFromSession(
        session.sessionId,
        selectedDeviceId
      );
      
      message.success('工单创建成功！');
      setCurrentStep('complete');
      
      if (onComplete) {
        onComplete(ticket.ticketId);
      }
    } catch (error: any) {
      console.error('创建工单失败:', error);
      const errorMessage = error.message || '未知错误';
      const status = (error as any)?.status;
      
      // 如果会话不存在，尝试重新创建
      if (status === 404 || errorMessage.includes('404') || errorMessage.includes('Not Found') || errorMessage.includes('会话')) {
        message.warning('会话已失效，正在重新初始化...');
        const newSession = await initializeSession();
        if (newSession) {
          message.info('会话已重新创建，请重新生成工单内容');
          setCurrentStep('initial');
        } else {
          message.error('无法重新初始化会话，请刷新页面重试');
        }
      } else {
        message.error('创建工单失败：' + errorMessage);
      }
    } finally {
      setLoading(false);
    }
  };

  if (loading && !session) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
        <p style={{ marginTop: '16px' }}>正在初始化AI引导...</p>
      </div>
    );
  }

  // 如果会话初始化失败，显示错误信息和重试按钮
  if (!loading && !session) {
    return (
      <div style={{ maxWidth: '1000px', margin: '0 auto', padding: '24px' }}>
        <Card>
          <Alert
            message="会话初始化失败"
            description="无法创建AI引导会话，请检查网络连接或稍后重试。如果问题持续，请联系管理员。"
            type="error"
            showIcon
            style={{ marginBottom: '16px' }}
          />
          <Space>
            <Button type="primary" onClick={initializeSession} loading={loading}>
              重试
            </Button>
            {onCancel && (
              <Button onClick={onCancel}>返回</Button>
            )}
          </Space>
        </Card>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '1000px', margin: '0 auto', padding: '24px' }}>
      <Card>
        <Title level={3}>
          <RobotOutlined /> AI引导式工单创建
        </Title>
        <Paragraph>
          通过AI智能引导，帮助您更专业地描述设备问题。请按照提示逐步提供信息。
        </Paragraph>

        {currentStep === 'initial' && (
          <InitialInfoStep
            form={form}
            imageList={imageList}
            setImageList={setImageList}
            onSubmit={handleSubmitInitial}
            loading={loading}
            onCancel={onCancel}
          />
        )}

        {currentStep === 'guiding' && session && (
          <GuidingStep
            session={session}
            onAnswer={handleAnswerQuestion}
            loading={loading}
            onSkip={handleGenerateContent}
          />
        )}

        {currentStep === 'review' && ticketContent && (
          <ReviewStep
            ticketContent={ticketContent}
            devices={devices}
            selectedDeviceId={selectedDeviceId}
            onDeviceChange={setSelectedDeviceId}
            onCreate={handleCreateTicket}
            onBack={() => setCurrentStep('guiding')}
            loading={loading}
          />
        )}

        {currentStep === 'complete' && (
          <CompleteStep onClose={onCancel} />
        )}
      </Card>
    </div>
  );
};

/**
 * 初始信息提交步骤
 */
const InitialInfoStep: React.FC<{
  form: any;
  imageList: File[];
  setImageList: (files: File[]) => void;
  onSubmit: () => void;
  loading: boolean;
  onCancel?: () => void;
}> = ({ form, imageList, setImageList, onSubmit, loading, onCancel }) => {
  const handleImageChange = (info: any) => {
    const fileList = info.fileList.map((item: any) => item.originFileObj).filter(Boolean);
    setImageList(fileList);
  };

  return (
    <>
      <Form form={form} layout="vertical">
        <Form.Item
          name="textDescription"
          label="问题描述"
          rules={[{ required: true, message: '请输入问题描述' }]}
          extra="请用简单的语言描述您遇到的问题，例如：机器不动了、报警灯亮了等"
        >
          <TextArea
            rows={6}
            placeholder="例如：机器不动了，报警灯亮了，屏幕上显示错误代码..."
            maxLength={1000}
            showCount
          />
        </Form.Item>

        <Form.Item label="上传图片（可选）" extra="可以上传设备照片、错误信息截图等，最多5张，每张不超过10MB">
          <Upload
            listType="picture-card"
            accept="image/*"
            multiple
            maxCount={5}
            beforeUpload={() => false} // 阻止自动上传
            onChange={handleImageChange}
            fileList={imageList.map((file, index) => ({
              uid: `${index}`,
              name: file.name,
              status: 'done',
              url: URL.createObjectURL(file),
            }))}
          >
            {imageList.length < 5 && (
              <div>
                <UploadOutlined />
                <div style={{ marginTop: 8 }}>上传</div>
              </div>
            )}
          </Upload>
        </Form.Item>

        <Form.Item>
          <Space>
            <Button type="primary" icon={<SendOutlined />} onClick={onSubmit} loading={loading}>
              提交并开始AI分析
            </Button>
            {onCancel && (
              <Button onClick={onCancel}>取消</Button>
            )}
          </Space>
        </Form.Item>
      </Form>
    </>
  );
};

/**
 * 引导对话步骤
 */
const GuidingStep: React.FC<{
  session: GuidedTicketCreationSession;
  onAnswer: (questionId: string, answer: string, images?: File[]) => void;
  loading: boolean;
  onSkip: () => void;
}> = ({ session, onAnswer, loading, onSkip }) => {
  const [answerForm] = Form.useForm();
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [additionalImages, setAdditionalImages] = useState<File[]>([]);

  const currentTurn = session.conversationHistory
    .filter((turn) => turn.role === 'assistant')
    .pop();

  const currentQuestions = currentTurn?.questions || [];

  const handleSubmitAnswer = async () => {
    if (currentQuestions.length === 0) return;

    const question = currentQuestions[currentQuestionIndex];
    const values = await answerForm.validateFields([`answer_${question.questionId}`]);
    const answer = values[`answer_${question.questionId}`];

    await onAnswer(question.questionId, answer, additionalImages.length > 0 ? additionalImages : undefined);
    
    // 重置表单和图片
    answerForm.resetFields();
    setAdditionalImages([]);
    
    // 移动到下一个问题或完成
    if (currentQuestionIndex < currentQuestions.length - 1) {
      setCurrentQuestionIndex(currentQuestionIndex + 1);
    }
  };

  if (currentQuestions.length === 0) {
    return (
      <div>
        <Alert
          message="信息收集完成"
          description="AI已完成信息分析，正在生成工单内容..."
          type="success"
          showIcon
        />
        <Button type="primary" onClick={onSkip} style={{ marginTop: '16px' }}>
          查看生成的工单内容
        </Button>
      </div>
    );
  }

  const currentQuestion = currentQuestions[currentQuestionIndex];

  return (
    <div>
      {/* 对话历史 */}
      <Card size="small" title="对话历史" style={{ marginBottom: '16px', maxHeight: '300px', overflowY: 'auto' }}>
        {session.conversationHistory.map((turn, index) => (
          <div key={index} style={{ marginBottom: '12px' }}>
            <Space>
              {turn.role === 'user' ? <UserOutlined /> : <RobotOutlined />}
              <Text strong>{turn.role === 'user' ? '您' : 'AI助手'}</Text>
            </Space>
            <Paragraph style={{ marginTop: '8px', marginLeft: '24px' }}>{turn.content}</Paragraph>
          </div>
        ))}
      </Card>

      {/* 当前问题 */}
      <Card title={`问题 ${currentQuestionIndex + 1}/${currentQuestions.length}`}>
        <div style={{ marginBottom: '16px' }}>
          <Title level={4}>{currentQuestion.question}</Title>
          {currentQuestion.whyImportant && (
            <Alert
              message={currentQuestion.whyImportant}
              type="info"
              showIcon
              style={{ marginTop: '8px' }}
            />
          )}
          {currentQuestion.professionalTermExample && (
            <Alert
              message={`专业术语示例：${currentQuestion.professionalTermExample}`}
              type="success"
              showIcon
              style={{ marginTop: '8px' }}
            />
          )}
        </div>

        <Form form={answerForm} layout="vertical">
          {currentQuestion.type === 'yes_no' && (
            <Form.Item
              name={`answer_${currentQuestion.questionId}`}
              rules={[{ required: currentQuestion.required, message: '请选择答案' }]}
            >
              <Radio.Group>
                <Radio value="是">是</Radio>
                <Radio value="否">否</Radio>
              </Radio.Group>
            </Form.Item>
          )}

          {currentQuestion.type === 'select' && currentQuestion.options && (
            <Form.Item
              name={`answer_${currentQuestion.questionId}`}
              rules={[{ required: currentQuestion.required, message: '请选择答案' }]}
            >
              <Select placeholder="请选择">
                {currentQuestion.options.map((option) => (
                  <Option key={option} value={option}>
                    {option}
                  </Option>
                ))}
              </Select>
            </Form.Item>
          )}

          {(currentQuestion.type === 'text' || currentQuestion.type === 'number') && (
            <Form.Item
              name={`answer_${currentQuestion.questionId}`}
              rules={[{ required: currentQuestion.required, message: '请输入答案' }]}
            >
              {currentQuestion.type === 'number' ? (
                <Input type="number" placeholder="请输入数字" />
              ) : (
                <TextArea rows={3} placeholder="请输入您的回答" />
              )}
            </Form.Item>
          )}

          {currentQuestion.hint && (
            <Alert message={currentQuestion.hint} type="info" showIcon style={{ marginBottom: '16px' }} />
          )}

          {/* 额外图片上传 */}
          <Form.Item label="补充图片（可选）">
            <Upload
              listType="picture-card"
              accept="image/*"
              multiple
              maxCount={3}
              beforeUpload={() => false}
              onChange={(info) => {
                const fileList = info.fileList.map((item: any) => item.originFileObj).filter(Boolean);
                setAdditionalImages(fileList);
              }}
            >
              {additionalImages.length < 3 && (
                <div>
                  <UploadOutlined />
                  <div style={{ marginTop: 8 }}>上传</div>
                </div>
              )}
            </Upload>
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" onClick={handleSubmitAnswer} loading={loading}>
                {currentQuestionIndex < currentQuestions.length - 1 ? '提交并继续' : '提交并完成'}
              </Button>
              {currentQuestionIndex > 0 && (
                <Button onClick={() => setCurrentQuestionIndex(currentQuestionIndex - 1)}>
                  上一题
                </Button>
              )}
              <Button onClick={onSkip}>跳过并生成工单</Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

/**
 * 审核步骤
 */
const ReviewStep: React.FC<{
  ticketContent: GuidedTicketContent;
  devices: DeviceDto[];
  selectedDeviceId: string;
  onDeviceChange: (deviceId: string) => void;
  onCreate: () => void;
  onBack: () => void;
  loading: boolean;
}> = ({ ticketContent, devices, selectedDeviceId, onDeviceChange, onCreate, onBack, loading }) => {
  const summary = ticketContent.summary;

  return (
    <div>
      <Alert
        message="AI已生成工单内容"
        description="请检查并确认以下信息，选择设备后即可创建工单"
        type="success"
        showIcon
        style={{ marginBottom: '24px' }}
      />

      {/* 设备选择 */}
      <Card title="选择设备" style={{ marginBottom: '16px' }}>
        <Select
          style={{ width: '100%' }}
          placeholder="请选择设备"
          value={selectedDeviceId}
          onChange={onDeviceChange}
          showSearch
          filterOption={(input, option) =>
            (option?.children as unknown as string)?.toLowerCase().includes(input.toLowerCase())
          }
        >
          {devices.map((device) => (
            <Option key={device.deviceId} value={device.deviceId}>
              {device.deviceSn} - {device.deviceName}
            </Option>
          ))}
        </Select>
      </Card>

      {/* 生成的内容预览 */}
      <Card title="生成的问题描述" style={{ marginBottom: '16px' }}>
        <p>
          <strong>问题标题：</strong>
          {summary.symptomTitle}
        </p>
        <p>
          <strong>详细描述：</strong>
          {summary.symptomDetail}
        </p>
        <p>
          <strong>问题域：</strong>
          <Tag>{summary.domain}</Tag>
        </p>
        {summary.stepCode && (
          <p>
            <strong>步骤代码：</strong>
            {summary.stepCode}
          </p>
        )}
      </Card>

      {/* 版本信息 */}
      {summary.versionInfo && (
        <Card title="版本信息" style={{ marginBottom: '16px' }}>
          {summary.versionInfo.swVersion && (
            <p>
              <strong>软件版本：</strong>
              {summary.versionInfo.swVersion}
            </p>
          )}
          {summary.versionInfo.plcVersion && (
            <p>
              <strong>PLC版本：</strong>
              {summary.versionInfo.plcVersion}
            </p>
          )}
          {summary.versionInfo.paramVersion && (
            <p>
              <strong>参数版本：</strong>
              {summary.versionInfo.paramVersion}
            </p>
          )}
        </Card>
      )}

      {/* 专业术语建议 */}
      {ticketContent.terminologySuggestions.length > 0 && (
        <Card title="专业术语建议" style={{ marginBottom: '16px' }}>
          <List
            dataSource={ticketContent.terminologySuggestions}
            renderItem={(item) => (
              <List.Item>
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Text>
                    <Text delete>{item.original}</Text> → <Text strong>{item.professional}</Text>
                  </Text>
                  <Text type="secondary">{item.explanation}</Text>
                </Space>
              </List.Item>
            )}
          />
        </Card>
      )}

      {/* 置信度 */}
      <Card title="AI置信度" style={{ marginBottom: '16px' }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <div>
            <Text>整体置信度：</Text>
            <Tag color={summary.confidence.overall >= 4 ? 'green' : summary.confidence.overall >= 3 ? 'orange' : 'red'}>
              {summary.confidence.overall}/5
            </Tag>
          </div>
          <div>
            <Text>标题置信度：</Text>
            <Tag>{summary.confidence.title}/5</Tag>
          </div>
          <div>
            <Text>详情置信度：</Text>
            <Tag>{summary.confidence.detail}/5</Tag>
          </div>
        </Space>
      </Card>

      <Divider />

      <Space>
        <Button onClick={onBack}>返回修改</Button>
        <Button
          type="primary"
          icon={<CheckCircleOutlined />}
          onClick={onCreate}
          loading={loading}
          disabled={!selectedDeviceId}
        >
          创建工单
        </Button>
      </Space>
    </div>
  );
};

/**
 * 完成步骤
 */
const CompleteStep: React.FC<{ onClose?: () => void }> = ({ onClose }) => {
  return (
    <div style={{ textAlign: 'center', padding: '40px' }}>
      <CheckCircleOutlined style={{ fontSize: '64px', color: '#52c41a', marginBottom: '24px' }} />
      <Title level={3}>工单创建成功！</Title>
      <Paragraph>AI已成功帮您创建了专业的工单，您可以继续完善或直接提交。</Paragraph>
      {onClose && (
        <Button type="primary" onClick={onClose} style={{ marginTop: '24px' }}>
          关闭
        </Button>
      )}
    </div>
  );
};
