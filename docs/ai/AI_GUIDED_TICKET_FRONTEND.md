# AI引导式工单创建 - 前端集成文档

## 概述

AI引导式工单创建功能已成功集成到前端工单创建页面，用户可以通过AI智能引导，更专业地描述设备问题。

## 功能特性

### 1. 多步骤引导流程

- **初始信息提交**：用户输入文字描述和上传图片
- **AI引导对话**：AI根据初始信息生成引导性问题，用户逐步回答
- **内容审核**：AI生成专业工单内容，用户确认并选择设备
- **完成创建**：工单创建成功

### 2. 核心组件

#### GuidedTicketCreation 组件
位置：`web-admin/src/components/tickets/GuidedTicketCreation.tsx`

主要功能：
- 管理整个引导式创建流程
- 处理会话状态
- 协调各步骤组件

#### 服务层
位置：`web-admin/src/services/guidedTicketCreationService.ts`

提供API调用方法：
- `createSession()` - 创建会话
- `submitInitialInfo()` - 提交初始信息
- `answerQuestion()` - 回答问题
- `generateTicketContent()` - 生成工单内容
- `createTicketFromSession()` - 创建工单

## 使用方法

### 方式一：从工单创建页面进入

1. 访问 `/tickets/create` 或 `/tickets/new`
2. 点击页面右上角的 **"使用AI引导创建"** 按钮
3. 进入AI引导式创建流程

### 方式二：通过URL参数直接进入

访问 `/tickets/create?guided=true` 直接进入引导模式

### 方式三：切换模式

在引导模式下，可以点击 **"切换到传统模式"** 按钮返回传统表单模式

## 使用流程

### 步骤1：提交初始信息

1. 在文本框中输入问题描述（必填）
   - 可以用简单语言描述，例如："机器不动了"、"报警灯亮了"
   - 最多1000字

2. 上传图片（可选）
   - 支持上传设备照片、错误信息截图等
   - 最多5张，每张不超过10MB
   - 支持的格式：JPG、PNG、GIF、WebP等

3. 点击 **"提交并开始AI分析"**

### 步骤2：回答引导性问题

AI会根据您提供的信息，生成2-3个引导性问题，帮助完善问题描述：

- **是/否问题**：快速确认某些情况
- **选择题**：从预设选项中选择
- **文本输入**：详细描述
- **数字输入**：输入具体数值

每个问题都会显示：
- 问题本身
- 为什么重要（whyImportant）
- 专业术语示例（professionalTermExample）
- 提示信息（hint）

**操作**：
- 回答当前问题
- 可以上传补充图片
- 点击 **"提交并继续"** 进入下一题
- 点击 **"跳过并生成工单"** 直接生成工单内容

### 步骤3：审核生成的内容

AI会生成专业的工单内容，包括：

- **问题描述**：标题和详细描述
- **问题域**：自动识别（A-E）
- **步骤代码**：如果可识别
- **版本信息**：软件版本、PLC版本、参数版本
- **专业术语建议**：将口语化描述转换为专业术语
- **AI置信度**：显示AI对生成内容的信心程度

**操作**：
1. 检查生成的内容
2. **选择设备**（必填）
3. 点击 **"创建工单"** 完成创建
4. 如需修改，点击 **"返回修改"**

### 步骤4：完成

工单创建成功后，会自动跳转到工单详情页面。

## 技术实现

### 文件结构

```
web-admin/src/
├── components/
│   └── tickets/
│       └── GuidedTicketCreation.tsx    # 主组件
├── services/
│   └── guidedTicketCreationService.ts  # API服务
└── pages/
    └── tickets/
        └── CreateTicket.tsx            # 集成页面
```

### 关键代码

#### 1. 集成到 CreateTicket 页面

```typescript
// 添加状态
const [useGuidedMode, setUseGuidedMode] = useState(false);

// 根据URL参数启用引导模式
const guidedMode = searchParams.get('guided') === 'true';

// 条件渲染
if (useGuidedMode) {
  return <GuidedTicketCreation onComplete={...} onCancel={...} />;
}
```

#### 2. FormData 上传支持

httpClient 已更新支持 FormData：

```typescript
// 自动检测 FormData，不设置 Content-Type
const body = data instanceof FormData ? data : JSON.stringify(data);
```

#### 3. 设备列表加载

```typescript
const loadDevices = async () => {
  const deviceList = await deviceService.getDevices(1, 100);
  setDevices(deviceList);
};
```

## 用户体验优化

### 1. 加载状态
- 初始化时显示加载动画
- 提交时显示加载状态，防止重复提交

### 2. 错误处理
- 友好的错误提示
- 自动重试机制（401时刷新token）

### 3. 对话历史
- 显示完整的对话历史
- 区分用户和AI的消息

### 4. 图片预览
- 上传后立即显示预览
- 支持删除已上传的图片

## 注意事项

1. **文件大小限制**：每张图片不超过10MB
2. **图片数量限制**：初始提交最多5张，回答问题时可补充3张
3. **会话过期**：会话24小时后自动清理
4. **设备选择**：创建工单时必须选择设备
5. **网络要求**：需要稳定的网络连接，AI分析可能需要几秒钟

## 后续优化建议

1. **离线支持**：支持离线保存草稿
2. **语音输入**：支持语音转文字
3. **历史记录**：保存历史会话，支持继续编辑
4. **模板学习**：根据用户习惯学习，提供更精准的引导
5. **多语言支持**：支持多语言界面和AI分析

## 故障排查

### 问题：图片上传失败
- 检查文件大小是否超过10MB
- 检查文件格式是否支持
- 检查网络连接

### 问题：AI分析超时
- 检查后端服务是否正常运行
- 检查Gemini API是否可用
- 尝试减少图片数量或大小

### 问题：设备列表为空
- 检查是否有权限访问设备列表
- 检查设备服务是否正常

## 相关文档

- [后端API文档](./AI_GUIDED_TICKET_USAGE.md)
- [完整实施文档](./AI_GUIDED_TICKET_COMPLETE.md)

